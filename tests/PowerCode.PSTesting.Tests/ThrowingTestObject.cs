namespace PowerCode.PSTesting.Tests;

public class ThrowingTestObject
{
    public string Name => "TestObject";
    public string Value => throw new InvalidOperationException("Cannot access Value property");
}