param(
    [string]$MasterPath = "src/DotNetG2P.Spanish/Data/spanish_exceptions.master.tsv",
    [string]$OutputPath = "src/DotNetG2P.Spanish/Data/spanish_exceptions.txt"
)

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$master = Join-Path $repoRoot $MasterPath
$output = Join-Path $repoRoot $OutputPath

if (-not (Test-Path $master)) {
    throw "Master exception file not found: $master"
}

$lines = [System.IO.File]::ReadAllLines($master, [System.Text.UTF8Encoding]::new($false)) | Where-Object {
    $_.Trim().Length -gt 0 -and -not $_.StartsWith("#") -and -not $_.StartsWith("surface`t")
}

$rendered = foreach ($line in $lines) {
    $parts = $line -split "`t"
    if ($parts.Length -lt 5) {
        continue
    }

    "{0}`t{1}`t{2}" -f $parts[0], $parts[3], $parts[4]
}

[System.IO.File]::WriteAllLines($output, $rendered, [System.Text.UTF8Encoding]::new($false))
Write-Host "Generated $output from $master"
