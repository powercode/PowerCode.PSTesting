using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace PowerCode.PSTesting.Tests;

public static class CustomStringAssertionExtensions
{
    /// <summary>
    /// Asserts that the value provided by the <paramref name="valueSource"/> is equal to the specified <paramref name="expected"/> string,
    /// comparing the strings line by line using <see cref="StringComparison.Ordinal"/>. Differences in line endings are ignored.
    /// </summary>
    /// <param name="valueSource">
    /// The source of the value to be asserted.
    /// </param>
    /// <param name="expected">
    /// The expected string value to compare against.
    /// </param>
    /// <param name="doNotPopulateThisValue1">
    /// The expression representing the <paramref name="expected"/> parameter. This is automatically populated by the compiler.
    /// </param>
    /// <returns>
    /// An <see cref="InvokableValueAssertionBuilder{T}"/> that allows further chaining of assertions.
    /// </returns>
    public static InvokableValueAssertionBuilder<string> IsLineEqualTo(this IValueSource<string> valueSource, string expected, [CallerArgumentExpression(nameof(expected))] string? doNotPopulateThisValue1 = null)
    {
        var assertionBuilder = valueSource.RegisterAssertion(
            new StringLineEqualsExpectedValueAssertCondition(expected, StringComparison.Ordinal),
            [doNotPopulateThisValue1!, nameof(StringComparison.Ordinal)]);
        return new InvokableValueAssertionBuilder<string>(assertionBuilder);
    }
}