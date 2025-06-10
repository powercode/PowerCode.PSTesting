[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = 'Release',
    [switch] $ShowCodeCoverage,
    [switch] $NoLogo,
    [switch] $Detailed,
    [switch] $NoRestore
)

$repoRoot = $PSScriptRoot

$buildParams = @(
    'build'
    '--configuration', $Configuration
    if ($NoRestore) { '--no-restore' }
    "$repoRoot\PowerCode.PSTesting.slnx"
)

Write-Debug "dotnet $buildParams"
dotnet $buildParams

$runArguments = @(
    'run'
    "--configuration", $Configuration
    "--no-restore"
    '--coverage'
    '--coverage-output-format', 'Cobertura'
    '--coverage-output', 'Cobertura.xml'
    '--project', "$repoRoot\tests\PowerCode.PSTesting.Tests\PowerCode.PSTesting.Tests.csproj"
    if ($Nologo) { '--disable-logo' }
    if ($Detailed) { '--output', 'Detailed' }
    if ($NoRestore) { '--no-restore' }
)
Write-Debug "dotnet $runArguments"
dotnet $runArguments

$coverageReport = "$repoRoot\artifacts\PowerCode.PSTesting.Tests\release\TestResults\Cobertura.xml"

[xml] $cc = Get-Content -Raw -LiteralPath:$coverageReport
[double] $lineRate = $cc.coverage.'line-rate'
$percentCoverage = $lineRate * 100

Write-Host "Code coverage: $($percentCoverage.ToString('N0'))%"

$reportArgs = @(
    "-reports:$coverageReport"
    "-reporttypes:html"
    "-targetdir:$repoRoot\Artifacts\CC"  
    "-sourcedirs:src"
    "-verbosity:Info"
)

Write-Debug "reportgenerator $reportArgs"
reportgenerator $reportArgs -license:$env:REPORTGENERATOR_LICENSEKEY

if ($ShowCodeCoverage) {
    $reportPath = Join-Path $repoRoot 'Artifacts\CC\index.html'
    Invoke-Item $reportPath
}
