namespace PowerCode.PSTesting;

public class IncludeExcludeMatcher(IEnumerable<string>? include, IEnumerable<string>? exclude)
{
    private readonly HashSet<string> _include = include != null ? [.. include] : [];
    private readonly HashSet<string> _exclude = exclude != null ? [.. exclude] : [];

    public bool IncludeInResult(string name)
    {
        return !_exclude.Contains(name) && (_include.Count == 0 || _include.Contains(name));
    }
}