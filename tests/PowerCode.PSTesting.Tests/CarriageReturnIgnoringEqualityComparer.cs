namespace PowerCode.PSTesting.Tests;

public class CarriageReturnIgnoringEqualityComparer : IEqualityComparer<string>
{
    public bool Equals(string? x, string? y)
    {
        if (x == null || y == null) return x == y;

        return x.Replace("\r", string.Empty) == y.Replace("\r", string.Empty);
    }

    public int GetHashCode(string obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        return obj.Replace("\r", string.Empty).GetHashCode();
    }

    private static CarriageReturnIgnoringEqualityComparer? _default;
    public static CarriageReturnIgnoringEqualityComparer Default => _default ??= new CarriageReturnIgnoringEqualityComparer();
}