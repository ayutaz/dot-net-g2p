param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"

function Get-DeterministicGuid {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
    $md5 = [System.Security.Cryptography.MD5]::Create()
    try {
        $hash = $md5.ComputeHash($bytes)
        return ([System.BitConverter]::ToString($hash)).Replace("-", "").ToLowerInvariant()
    }
    finally {
        $md5.Dispose()
    }
}

function Get-MetaContent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,

        [Parameter(Mandatory = $true)]
        [bool]$IsDirectory
    )

    $normalizedPath = $RelativePath.Replace("\", "/")
    $guidPrefix = if ($IsDirectory) { "dir:" } else { "file:" }
    $guid = Get-DeterministicGuid -Value ($guidPrefix + $normalizedPath)

    if ($IsDirectory) {
        return @"
fileFormatVersion: 2
guid: $guid
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
"@
    }

    $extension = [System.IO.Path]::GetExtension($RelativePath)
    switch ($extension.ToLowerInvariant()) {
        ".cs" {
            return @"
fileFormatVersion: 2
guid: $guid
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
"@
        }
        ".asmdef" {
            return @"
fileFormatVersion: 2
guid: $guid
AssemblyDefinitionImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
"@
        }
        default {
            return @"
fileFormatVersion: 2
guid: $guid
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
"@
        }
    }
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,

        [Parameter(Mandatory = $true)]
        [string]$TargetPath
    )

    $baseUri = [System.Uri]((Resolve-Path $BasePath).Path.TrimEnd('\') + '\')
    $targetUri = [System.Uri](Resolve-Path $TargetPath).Path
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/', '\')
}

$packageDirectories = Get-ChildItem -Path (Join-Path $RepoRoot "src") -Directory |
    Where-Object { $_.Name -like "DotNetG2P.*" }

foreach ($packageDirectory in $packageDirectories) {
    $entries = Get-ChildItem -Path $packageDirectory.FullName -Recurse -Force |
        Where-Object {
            $_.FullName -notmatch "\\(bin|obj)\\" -and
            $_.Name -ne "package.json" -and
            $_.Name -notlike "*.meta"
        }

    foreach ($entry in $entries) {
        if ($entry.PSIsContainer -and $entry.FullName -eq $packageDirectory.FullName) {
            continue
        }

        $relativePath = Get-RelativePath -BasePath $RepoRoot -TargetPath $entry.FullName
        $metaPath = $entry.FullName + ".meta"
        $metaContent = Get-MetaContent -RelativePath $relativePath -IsDirectory $entry.PSIsContainer

        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        [System.IO.File]::WriteAllText($metaPath, $metaContent + [Environment]::NewLine, $utf8NoBom)
    }
}
