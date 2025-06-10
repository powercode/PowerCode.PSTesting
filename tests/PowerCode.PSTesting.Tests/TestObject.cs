namespace PowerCode.PSTesting.Tests;

public class TestObject
{
    public string Name { get; set; } = "TestObject";
    public int Value { get; set; } = 42;

    public Uri Field = new("MyRelativeUri", UriKind.Relative);

    // This indexer is used to demonstrate that indexers are not converted to properties.
    public string this[int i] => $"Item {i}";

    public int WriteOnlyProperty
    {
        set => Name = $"WriteOnlyProperty set to {value}";
    }

    public override string ToString()
    {
        return $"{Name} ({Value})";
    }
}