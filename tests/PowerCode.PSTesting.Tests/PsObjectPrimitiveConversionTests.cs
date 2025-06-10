using System.Management.Automation;
using System.Numerics;
using System.Reflection;

namespace PowerCode.PSTesting.Tests;

public class PsObjectPrimitiveConversionTests
{
    [Test]
    [Arguments("Hello, World", "'Hello, World'")]
    [Arguments("Hello, 'World'", "'Hello, ''World'''")]
    [Arguments("Hello\n\"World\"", "\"Hello`n`\"World`\"\"")]
    [Arguments("""
               Hello,
               'World'
               """, "\"Hello,`n'World'\"")]
    public async Task EscapesString(string input, string expected)
    {
        
        var result = PsObjectConverter.ConvertToString(input);
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ConvertsDateTimeUtc()
    {
        var objectToConvert = new DateTime(2023, 10, 1, 12, 0, 0, 0, 0, DateTimeKind.Utc);
        var result = PsObjectConverter.ConvertToString(PSObject.AsPSObject(objectToConvert));
        await Assert.That(result).IsEqualTo("[datetime]::new(2023, 10, 1, 12, 0, 0, [DateTimeKind]::Utc)");
    }

    [Test]
    public async Task ConvertsDateTimeLocal()
    {
        var objectToConvert = new DateTime(2023, 10, 1, 12, 0, 0, 0, 1, DateTimeKind.Local);
        var result = PsObjectConverter.ConvertToString(PSObject.AsPSObject(objectToConvert));
        await Assert.That(result).IsEqualTo("[datetime]::new(2023, 10, 1, 12, 0, 0, 0, 1, [DateTimeKind]::Local)");
    }

    [Test]
    public async Task ConvertsDateTimeUnspecified()
    {
        var objectToConvert = new DateTime(2023, 10, 1, 12, 0, 0, 1, 0, DateTimeKind.Unspecified);
        var result = PsObjectConverter.ConvertToString(PSObject.AsPSObject(objectToConvert));
        await Assert.That(result).IsEqualTo("[datetime]::new(2023, 10, 1, 12, 0, 0, 1, 0, [DateTimeKind]::Unspecified)");
    }

    [Test]
    public async Task ConvertsDateTimeOffset()
    {
        var date = new DateTime(2023, 10, 1, 12, 0, 0, 1, 0, DateTimeKind.Unspecified);
        var offset = TimeSpan.FromHours(2);
        var objectToConvert = new DateTimeOffset(date, offset);
        var result = PsObjectConverter.ConvertToString(PSObject.AsPSObject(objectToConvert));
        await Assert.That(result).IsEqualTo("[DateTimeOffset]::new([datetime]::new(2023, 10, 1, 12, 0, 0, 1, 0, [DateTimeKind]::Unspecified), [timespan]::new(0, 2, 0, 0))");
    }


    [Test]
    public async Task ConvertsTimeSpan()
    {
        var objectToConvert = new TimeSpan(1, 2, 3, 4);
        var result = PsObjectConverter.ConvertToString(PSObject.AsPSObject(objectToConvert));
        await Assert.That(result).IsEqualTo("[timespan]::new(1, 2, 3, 4)");
    }

    [Test]
    public async Task ConvertsDictionary()
    {
        var objectToConvert = new Dictionary<string, object>
        {
            { "name", "John Doe" }
        };
        var result = PsObjectConverter.ConvertToString(PSObject.AsPSObject(objectToConvert));
        await Assert.That(result).IsLineEqualTo("""
                                            [PSCustomObject] @{
                                              PSTypeName = 'Deserialized.System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Object, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]'
                                              name = 'John Doe'
                                            }
                                            """);
    }


    [Test]
    public async Task ConvertsNull()
    {
        var result = PsObjectConverter.ConvertToString(null);
        await Assert.That(result).IsEqualTo("$null");
    }

    [Test]
    [Arguments(1, "1")]
    [Arguments(1L, "1l")]
    [Arguments(1U, "1u")]
    [Arguments(1UL, "1ul")]
    public async Task ConvertsIntegers(object input, string expected)
    {
        var result = PsObjectConverter.ConvertToString(input);
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments(true, "$true")]
    [Arguments(false, "$false")]
    public async Task ConvertsBool(bool input, string expected)
    {
        var result = PsObjectConverter.ConvertToString(input);
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments(1, "1uy")]
    public async Task ConvertsByte(byte input, string expected)
    {
        var result = PsObjectConverter.ConvertToString(input);
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ConvertsSemanticVersion()
    {
        var input = new SemanticVersion(1, 2, 3, "alpha", "build");
        var result = PsObjectConverter.ConvertToString(input);
        await Assert.That(result).IsEqualTo("[semver]::new(1, 2, 3, 'alpha', 'build')");
    }

    [Test]
    [Arguments('c', "[char]'c'")]
    public async Task ConvertsChar(char input, string expected)
    {
        var result = PsObjectConverter.ConvertToString(input);
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments(1, "1us")]
    public async Task ConvertsUnsignedShort(ushort input, string expected)
    {
        var result = PsObjectConverter.ConvertToString(input);
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments(1.4, "1.4")]
    public async Task ConvertsHalf(Half half, string expected)
    {
        var result = PsObjectConverter.ConvertToString(half);
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments(1, "1y")]
    public async Task ConvertsSignedByte(sbyte input, string expected)
    {
        var result = PsObjectConverter.ConvertToString(input);
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments(1.1, "1.1")]
    [Arguments(2.1f, "2.1")]
    public async Task ConvertsReal(object input, string expected)
    {
        var result = PsObjectConverter.ConvertToString(input);
        await Assert.That(result).IsEqualTo(expected);
    }


    [Test]
    [Arguments(1.5, "1.5d")]
    [Arguments(-1.5, "-1.5d")]
    public async Task ConvertsDecimal(decimal input, string expected)
    {
        var result = PsObjectConverter.ConvertToString(input);
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments(2, "[bigint]::Parse('2')")]
    [Arguments(-2, "[bigint]::Parse('-2')")]
    public async Task ConvertsBigint(BigInteger input, string expected) 
    {
        var result = PsObjectConverter.ConvertToString(input);
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments("1.2", "[Version]::new(1, 2)")]
    [Arguments("1.2.3", "[Version]::new(1, 2, 3)")]
    [Arguments("1.2.3.4", "[Version]::new(1, 2, 3, 4)")]
    public async Task ConvertsVersion(string input, string expected)
    {
        var version = Version.Parse(input);
        var result = PsObjectConverter.ConvertToString(version);
        await Assert.That(result).IsEqualTo(expected);
    }
    
    [Test]
    [Arguments("https://url.nourl", "[Uri]::new('https://url.nourl/')")]
    [Arguments("a/b/c", "[Uri]::new('a/b/c', [UriKind]::Relative)")]
    public async Task ConvertsUri(string input, string expected)
    {
        var version = new Uri(input, UriKind.RelativeOrAbsolute);
        var result = PsObjectConverter.ConvertToString(version);
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ConvertsGuid()
    {
        var guid = Guid.Parse("e6105121-4092-41df-92bc-4763d1159f91");
        var result = PsObjectConverter.ConvertToString(guid);
        await Assert.That(result).IsEqualTo("[guid]::Parse('e6105121-4092-41df-92bc-4763d1159f91')");
    }

    [Test]
    public async Task ConvertsEnum()
    {
        var enumValue = DayOfWeek.Monday;
        var result = PsObjectConverter.ConvertToString(enumValue);
        await Assert.That(result).IsEqualTo("[System.DayOfWeek]::Monday");
    }

    [Test]
    public async Task ConvertsFlagsEnum()
    {
        var enumValue = BindingFlags.NonPublic | BindingFlags.Instance;
        var result = PsObjectConverter.ConvertToString(enumValue);
        await Assert.That(result).IsEqualTo("[System.Reflection.BindingFlags]'Instance, NonPublic'");
    }

    [Test]
    public async Task ConvertsEnumWithValueOutOfRange()
    {
        var enumValue = Enum.ToObject(typeof(UnixFileMode), -1);
        var result = PsObjectConverter.ConvertToString(enumValue);
        await Assert.That(result).IsEqualTo("[Enum]::ToObject([System.IO.UnixFileMode], -1)");
    }

}