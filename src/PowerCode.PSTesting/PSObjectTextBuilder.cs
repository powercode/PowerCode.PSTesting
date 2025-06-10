using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Management.Automation;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace PowerCode.PSTesting;


public interface IScopeIndentation
{
    void PushIndent(StringBuilder builder);
    void AddPropertySeparator(StringBuilder builder);
    void AddIndent(StringBuilder builder);
    void PopIndent(StringBuilder builder);
}


public class PsObjectTextBuilder(string[]? include, string[]? exclude, bool indented, CancellationToken cancellationToken)
{
    private static readonly System.Buffers.SearchValues<char> s_invalidPropertyNameCharacters = System.Buffers.SearchValues.Create(" =+-*/:;.,(){}[]");
    private readonly StringBuilder _builder = new();
    private readonly IncludeExcludeMatcher _includeExcludeMatcher = new(include, exclude);
    private readonly IScopeIndentation _indentation = indented ? new NewLineIndentation() : new SameLineIndentation();

    public void AppendObject(object? obj)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (PsObjectConverter.IsPrimitive(obj))
        {
            _builder.Append(ToPrimitiveStringRepresentation(obj));
            return;
        }
        if (obj is IEnumerable enumerable and not IDictionary)
        {
            AddArray(enumerable);
            return;
        }
        BeginObject(obj);
        AddObjectProperties(obj);
        EndObject();
    }
    private void BeginObject(object obj)
    {
        string prefix = obj switch
        {
            Hashtable => "",
            OrderedDictionary => "[ordered] ",
            _ => "[PSCustomObject] "
        };
        _builder.Append($"{prefix}@{{");
        _indentation.PushIndent(_builder);
        AddTypeName(obj);
    }

    private void AddTypeName(object obj)
    {
        if (obj is PSObject psObj && psObj.TypeNames.Count > 0)
        {
            AddPsObjectTypeName(psObj);
        }
    }

    private void AddPsObjectTypeName(PSObject psObj)
    {
        var typeName = psObj.TypeNames[0];
        if (typeName is not ("System.Management.Automation.PSCustomObject" or "System.Collections.Hashtable" or "System.Collections.Specialized.OrderedDictionary"))
        {
            AddProperty("PSTypeName", typeName);
            _indentation.AddPropertySeparator(_builder);
        }
    }

    private void EndObject()
    {
        _indentation.PopIndent(_builder);
        Append("}");
    }

    private void AddObjectProperties(object obj)
    {
        bool first = true;
        foreach (var (name, value) in GetProperties(obj))
        {
            if (!_includeExcludeMatcher.IncludeInResult(name))
                continue;
            if (!first)
                _indentation.AddPropertySeparator(_builder);
            first = false;
            AddProperty(name, value);
        }
    }

    private static IEnumerable<(string Name, object? Value)> GetProperties(object obj)
    {
        return obj switch
        {
            IDictionary dict => GetDictionaryProperties(dict),
            PSObject psObj => GetPsObjectProperties(psObj),
            _ => []
        };
    }

    private static IEnumerable<(string Name, object? Value)> GetPsObjectProperties(PSObject psObj) => psObj.Properties.Select(prop => GetPropertyValue(prop.Name, () => prop.Value));

    private static IEnumerable<(string Name, object? Value)> GetDictionaryProperties(IDictionary dict)
    {
        foreach (DictionaryEntry entry in dict)
        {
            yield return (QuoteName(entry.Key.ToString() ?? "<null>"), entry.Value);
        }
    }

    private static (string, object?) GetPropertyValue(string name, Func<object?> getter)
    {
        var value = getter();
        return (QuoteName(name), value);
    }

    private static string QuoteName(string name) => name.AsSpan().ContainsAny(s_invalidPropertyNameCharacters) ? $"'{name.Replace("'", "''")}'" : name;

    private void AddProperty(string name, object? value)
    {
        Append(name);
        _ =_builder.Append(" = ");
        if (value == null)
        {
            _ = _builder.Append("$null");
            return;
        }
        if (PsObjectConverter.IsPrimitive(value))
        {
            _ = _builder.Append(ToPrimitiveStringRepresentation(value));
            return;
        }
        if (value is IEnumerable enumerable and not IDictionary)
        {
            AddArray(enumerable);
            return;
        }
        AppendObject(value);
    }

    private void Append(string text)
    {
        _indentation.AddIndent(_builder);
        _builder.Append(text);
    }

    private void AddArray(IEnumerable array)
    {
        _builder.Append("@(");
        bool first = true;
        foreach (var item in array)
        {
            if (!first) _builder.Append(", ");
            else first = false;
            if (PsObjectConverter.IsPrimitive(item))
                _builder.Append(ToPrimitiveStringRepresentation(item));
            else
            {
                AppendObject(item);
            }
        }
        _builder.Append(')');
    }



    private static string? ToPrimitiveStringRepresentation(object? value) => value switch
    {
        null => "$null",
        char => $"[char]'{value}'",
        string s => ToEscapedString(s),
        bool b => b ? "$true" : "$false",
        byte by => $"{by}uy",
        sbyte by => $"{by}y",
        ushort us => $"{us}us",
        uint ui => $"{ui}u",
        long l => $"{l}l",
        ulong ul => $"{ul}ul",
        decimal d => string.Format(CultureInfo.InvariantCulture, "{0}d", d),
        Half d => d.ToString(CultureInfo.InvariantCulture),
        float d => d.ToString(CultureInfo.InvariantCulture),
        double d => d.ToString(CultureInfo.InvariantCulture),
        DateTime dt => GetDateTimeString(dt),
        DateTimeOffset dt => GetDateTimeOffsetString(dt),
        TimeSpan ts => GetTimeSpanString(ts),
        Guid g => $"[guid]::Parse('{g}')",
        Enum e => GetEnumValue(e),
        Version v => GetVersionValue(v),
        Uri u => GetUriValue(u),
        SemanticVersion v => $"[semver]::new({v.Major}, {v.Minor}, {v.Patch}, '{v.PreReleaseLabel}', '{v.BuildLabel}')",
        BigInteger bi => $"[bigint]::Parse('{bi}')",
        PSObject psObject => ToPrimitiveStringRepresentation(psObject.BaseObject),
        _ => value.ToString()
    };

    private static string GetUriValue(Uri uri)
    {
        if (uri.IsAbsoluteUri)
        {
            return $"[Uri]::new('{uri.AbsoluteUri}')";
        }
        return $"[Uri]::new('{uri.OriginalString}', [UriKind]::Relative)";
    }

    private static string? GetVersionValue(Version v)
    {
        return (v.Major, v.Minor, v.Build, v.Revision) switch
        {
             (var major, var minor, -1, -1) => $"[Version]::new({major}, {minor})",
             (var major, var minor, var build, -1) => $"[Version]::new({major}, {minor}, {build})",
             var (major, minor, build, revision) => $"[Version]::new({major}, {minor}, {build}, {revision})",
            
        };
    }

    private static string ToEscapedString(string value)
    {
        if (value.Contains('\n'))
        {
            return $"\"{value.Replace("\n", "`n").Replace("\r", "`r").Replace("\"", "`\"")}\"";
        }
        var escaped = value.Replace("\'", "\'\'");
        return $"'{escaped}'";
    }

    private static string GetDateTimeOffsetString(DateTimeOffset dateTimeOffset)
    {
        return $"[DateTimeOffset]::new({GetDateTimeString(dateTimeOffset.DateTime)}, {GetTimeSpanString(dateTimeOffset.Offset)})";
    }

    private static string GetTimeSpanString(TimeSpan timeSpan)
    {
        if (timeSpan.Microseconds is 0 && timeSpan.Milliseconds is 0)
        {
            return $"[timespan]::new({timeSpan.Days}, {timeSpan.Hours}, {timeSpan.Minutes}, {timeSpan.Seconds})";
        }
        return $"[timespan]::new({timeSpan.Days}, {timeSpan.Hours}, {timeSpan.Minutes}, {timeSpan.Seconds}, {timeSpan.Milliseconds}, {timeSpan.Microseconds})";
    }

    private static string GetDateTimeString(DateTime dateTime)
    {
        if (dateTime.Microsecond is 0 && dateTime.Millisecond is 0)
        {
            return $"[datetime]::new({dateTime.Year}, {dateTime.Month}, {dateTime.Day}, {dateTime.Hour}, {dateTime.Minute}, {dateTime.Second}, [DateTimeKind]::{dateTime.Kind})";
        }
        return $"[datetime]::new({dateTime.Year}, {dateTime.Month}, {dateTime.Day}, {dateTime.Hour}, {dateTime.Minute}, {dateTime.Second}, {dateTime.Millisecond}, {dateTime.Microsecond}, [DateTimeKind]::{dateTime.Kind})";
    }

    private static string GetEnumValue(Enum e)
    {
        var type = e.GetType();
        var intValue = ((IConvertible)Enum.ToObject(type, e)).ToInt32(null);
        if (intValue < 0)
        {
            return $"[Enum]::ToObject([{type.FullName}], {intValue})";
        }
        return Enum.GetName(type, e) is { } name
            ? $"[{type.FullName}]::{name}"
            : $"[{type.FullName}]'{e}'";
    }

    public override string ToString() => _builder.ToString();

    private class NewLineIndentation : IScopeIndentation
    {
        private string _indent = "";
        public void PushIndent(StringBuilder builder)
        {
            _indent += "  ";
            builder.AppendLine();
        }

        public void AddPropertySeparator(StringBuilder builder) => builder.AppendLine();
        public void AddIndent(StringBuilder builder) => builder.Append(_indent);

        public void PopIndent(StringBuilder builder)
        {
            builder.AppendLine();
            _indent = _indent[..^2];
        }
    }

    private class SameLineIndentation : IScopeIndentation
    {
        public void PushIndent(StringBuilder builder) { }

        public void AddPropertySeparator(StringBuilder builder) => builder.Append("; ");

        public void AddIndent(StringBuilder builder) { }

        public void PopIndent(StringBuilder builder) { }
    }
}