using System.Management.Automation;

namespace PowerCode.PSTesting;

[Cmdlet(VerbsData.ConvertTo, "PsObjectString")]
[OutputType(typeof(string))]
public class ConvertToPsObjectString : Cmdlet
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    [AllowNull]
    public object? InputObject { get; set; }

    [Parameter]
    public string[]? IncludeProperties { get; set; }

    [Parameter]
    public string[]? ExcludeProperties { get; set; }

    [Parameter]
    public SwitchParameter Compress { get; set; }

    [Parameter] public int Depth { get; set; } = 4;

    protected override void ProcessRecord()
    {
        try
        {
            var converted = PsObjectConverter.ConvertToString(InputObject, IncludeProperties, ExcludeProperties,
                Compress.IsPresent, Depth, _cancellationTokenSource.Token);
            WriteObject(converted);
        }
        catch (OperationCanceledException)
        {
        }
    }

    protected override void StopProcessing()
    {
        _cancellationTokenSource.Cancel();
        base.StopProcessing();
    }
}