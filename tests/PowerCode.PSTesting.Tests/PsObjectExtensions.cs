using System.Management.Automation;

namespace PowerCode.PSTesting.Tests;

public static class PowerShellExtensions
{
    public static PSObject AddProperty(this PSObject psObject, string name, object? value)
    {
        psObject.Members.Add(new PSNoteProperty(name, value));
        return psObject;
    }

    public static PSObject AddScriptProperty(this PSObject psObject, string name, ScriptBlock value)
    {
        psObject.Members.Add(new PSScriptProperty(name, value));
        return psObject;
    }

    public static PSObject AddTypeName(this PSObject psObject, string typename)
    {
        psObject.TypeNames.Insert(0, typename);
        return psObject;
    }

    public static PowerShell AddParameterIf(this PowerShell powerShell, string name, object? value, bool shouldAdd)
    {
        return shouldAdd ? powerShell.AddParameter(name, value) : powerShell;
    }
}

