param(
    [string]$DestinationPath = (Join-Path $HOME "naist-jdic"),
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$releaseVersion = "1.11.1"
$archiveName = "open_jtalk_dic_utf_8-1.11.tar.gz"
$downloadUrl = "https://github.com/r9y9/open_jtalk/releases/download/v$releaseVersion/$archiveName"
$requiredFiles = @("sys.dic", "matrix.bin", "char.bin", "unk.dic")

function Test-DictionaryDirectory {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        return $false
    }

    foreach ($file in $requiredFiles) {
        if (-not (Test-Path -LiteralPath (Join-Path $Path $file) -PathType Leaf)) {
            return $false
        }
    }

    return $true
}

if ((Test-DictionaryDirectory -Path $DestinationPath) -and -not $Force) {
    Write-Host "naist-jdic is already installed at: $DestinationPath"
    exit 0
}

$tarCommand = Get-Command tar -ErrorAction SilentlyContinue
if ($null -eq $tarCommand) {
    throw "tar command is required to extract $archiveName."
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("dotnetg2p-naist-jdic-" + [Guid]::NewGuid().ToString("N"))
$archivePath = Join-Path $tempRoot $archiveName
$extractRoot = Join-Path $tempRoot "extract"

New-Item -ItemType Directory -Path $tempRoot | Out-Null
New-Item -ItemType Directory -Path $extractRoot | Out-Null

try {
    Write-Host "Downloading $downloadUrl"
    $curlCommand = Get-Command curl.exe -ErrorAction SilentlyContinue
    if ($null -ne $curlCommand) {
        & $curlCommand.Source -L $downloadUrl -o $archivePath
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to download $downloadUrl with curl.exe"
        }
    }
    else {
        Invoke-WebRequest -Uri $downloadUrl -OutFile $archivePath
    }

    if (-not (Test-Path -LiteralPath $archivePath -PathType Leaf)) {
        throw "Failed to download $downloadUrl"
    }

    Write-Host "Extracting $archiveName"
    & tar -xzf $archivePath -C $extractRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to extract $archiveName"
    }

    $innerDirectory = Join-Path $extractRoot "open_jtalk_dic_utf_8-1.11"
    if (-not (Test-DictionaryDirectory -Path $innerDirectory)) {
        throw "Extracted archive did not contain a valid dictionary directory."
    }

    if (Test-Path -LiteralPath $DestinationPath) {
        Remove-Item -LiteralPath $DestinationPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $DestinationPath | Out-Null
    Get-ChildItem -LiteralPath $innerDirectory -Force | Copy-Item -Destination $DestinationPath -Recurse -Force

    if (-not (Test-DictionaryDirectory -Path $DestinationPath)) {
        throw "Dictionary installation verification failed at $DestinationPath"
    }

    Write-Host "Installed naist-jdic to: $DestinationPath"
    Write-Host "You can now use:"
    Write-Host '  using var tokenizer = new MeCabTokenizer();'
    Write-Host '  using var multilingual = new MultilingualG2PEngine();'
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
