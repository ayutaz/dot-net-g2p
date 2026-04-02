param(
    [string]$CorpusDir = (Join-Path $PSScriptRoot "..\artifacts\swedish-eval\corpora"),
    [string]$ReportDir = (Join-Path $PSScriptRoot "..\artifacts\swedish-eval\reports"),
    [switch]$Refresh,
    [switch]$ForceRefresh,
    [switch]$EnforceThresholds
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($Refresh -or -not (Test-Path $CorpusDir)) {
    $refreshArgs = @(
        "-ExecutionPolicy", "Bypass",
        "-File", (Join-Path $PSScriptRoot "refresh_swedish_eval_data.ps1"),
        "-Mode", "Full",
        "-FullOutputDir", $CorpusDir
    )

    if ($ForceRefresh) {
        $refreshArgs += "-Force"
    }

    & powershell @refreshArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to refresh full Swedish evaluation corpora."
    }
}

$evalArgs = @(
    "run",
    "--project", (Join-Path $PSScriptRoot "DotNetG2P.SwedishEval\DotNetG2P.SwedishEval.csproj"),
    "--",
    "--dataset-set", "full",
    "--input-root", $CorpusDir,
    "--output-root", $ReportDir
)

if ($EnforceThresholds) {
    $evalArgs += "--enforce-thresholds"
}

& dotnet @evalArgs
exit $LASTEXITCODE
