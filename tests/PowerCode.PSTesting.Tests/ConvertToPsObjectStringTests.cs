using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PowerCode.PSTesting.Tests;

public class ConvertToPsObjectStringTests
{
    [Test]
    public async Task ConvertsCustomTestObject()
    {

        var testObject = new TestObject();
        var res = InvokeConvertToPsObjectString(testObject, includeProperties: ["Name"], excludeProperties: ["Value"], noIndent: true);

        await Assert.That(res).IsLineEqualTo("[PSCustomObject] @{PSTypeName = 'Deserialized.PowerCode.PSTesting.Tests.TestObject'; Name = 'TestObject'}");
    }

    public static string InvokeConvertToPsObjectString(object? inputObject, string[]? includeProperties = null, string[]? excludeProperties = null, bool noIndent = false, int depth = 4)
    {
        using var ps = CreatePowerShellForTest();
        return ps.AddCommand("ConvertTo-PSObjectString")
            .AddParameterIf(nameof(ConvertToPsObjectString.IncludeProperties), includeProperties,
                includeProperties != null)
            .AddParameterIf(nameof(ConvertToPsObjectString.ExcludeProperties), excludeProperties,
                excludeProperties != null)
            .AddParameterIf(nameof(ConvertToPsObjectString.Compress), true, noIndent)
            .AddParameter(nameof(ConvertToPsObjectString.Depth), depth)
            .AddParameter(nameof(ConvertToPsObjectString.InputObject), inputObject)
            .Invoke<string>()
            .First();
    }

    public static PowerShell CreatePowerShellForTest()
    {
        var initialSessionState = InitialSessionState.CreateDefault();
        var assemblyLocation = typeof(ConvertToPsObjectString).Assembly.Location;
        var moduleManifest = Path.ChangeExtension(assemblyLocation, ".psd1");
        initialSessionState.ImportPSModule(moduleManifest);

        return PowerShell.Create(initialSessionState);
    }
}