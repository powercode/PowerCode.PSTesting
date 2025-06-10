using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.String;
using TUnit.Assertions.Helpers;

namespace PowerCode.PSTesting.Tests;

public class StringLineEqualsExpectedValueAssertCondition(string expected, StringComparison stringComparison)
#pragma warning disable CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
    : StringEqualsExpectedValueAssertCondition(expected, stringComparison)
#pragma warning restore CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
{
    protected override ValueTask<AssertionResult> GetResult(string? actualValue, string? expectedValue)
    {
        if (actualValue is null)
        {
            return AssertionResult
                .FailIf(expectedValue is not null,
                    "it was null");
        }
        actualValue = actualValue.ReplaceLineEndings();
        expectedValue = expectedValue?.ReplaceLineEndings();
        return AssertionResult
            .FailIf(!string.Equals(actualValue, expectedValue, stringComparison),
                $"found {Formatter.Format(actualValue).TruncateWithEllipsis(100)} which {new StringDifference(actualValue, expectedValue)}");
    }
}