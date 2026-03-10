param(
    [string]$SampleOutputDir = (Join-Path $PSScriptRoot "..\tests\TestData\SpanishG2P"),
    [string]$FullOutputDir = (Join-Path $PSScriptRoot "..\artifacts\spanish-eval\corpora"),
    [string]$CacheDir = (Join-Path $PSScriptRoot "..\.cache\spanish-eval"),
    [ValidateSet("Sample", "Full", "Both")]
    [string]$Mode = "Both",
    [int]$SampleSize = 256,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$wordExtraUpperCodes = @(193, 201, 205, 209, 211, 218, 220)
$wordExtraLowerCodes = @(225, 233, 237, 241, 243, 250, 252)
$ipaDictAllowedCodes = @(32, 46, 47, 97, 98, 100, 101, 102, 103, 105, 106, 107, 108, 109, 110, 111, 112, 114, 115, 116, 117, 119, 120, 122, 240, 331, 609, 611, 625, 626, 638, 643, 654, 669, 712, 716, 810, 946, 952)
$wikiPronAllowedCodes = @(32, 97, 98, 100, 101, 102, 103, 105, 106, 107, 108, 109, 110, 111, 112, 114, 115, 116, 117, 119, 120, 122, 124, 240, 331, 609, 611, 625, 626, 638, 643, 654, 669, 810, 946, 952)
$allowedIpaDictCodeSet = New-Object 'System.Collections.Generic.HashSet[int]'
$allowedWikiPronCodeSet = New-Object 'System.Collections.Generic.HashSet[int]'

foreach ($code in $ipaDictAllowedCodes) { [void]$allowedIpaDictCodeSet.Add($code) }
foreach ($code in $wikiPronAllowedCodes) { [void]$allowedWikiPronCodeSet.Add($code) }

function Get-RemoteLines([string]$url, [string]$cachePath, [switch]$forceDownload) {
    if ($forceDownload -or -not (Test-Path $cachePath)) {
        New-Item -ItemType Directory -Force -Path ([System.IO.Path]::GetDirectoryName($cachePath)) | Out-Null
        & curl.exe --fail --silent --location --user-agent codex --output $cachePath $url
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to download $url"
        }
    }

    return [System.IO.File]::ReadAllLines($cachePath)
}

function Select-DeterministicSample($entries, [int]$sampleSize) {
    if ($entries.Count -le $sampleSize) {
        return $entries
    }

    $selected = New-Object 'System.Collections.Generic.List[object]'
    $seen = New-Object 'System.Collections.Generic.HashSet[int]'
    for ($i = 0; $i -lt $sampleSize; $i++) {
        $index = [int][Math]::Round($i * ($entries.Count - 1) / [double]($sampleSize - 1))
        if ($seen.Add($index)) {
            $selected.Add($entries[$index])
        }
    }

    for ($i = 0; $selected.Count -lt $sampleSize -and $i -lt $entries.Count; $i++) {
        if ($seen.Add($i)) {
            $selected.Add($entries[$i])
        }
    }

    return $selected
}

function Save-Tsv([string]$path, $entries) {
    $lines = foreach ($entry in $entries) {
        "{0}`t{1}" -f $entry.Word, $entry.Pronunciation
    }

    [System.IO.File]::WriteAllLines($path, $lines, [System.Text.UTF8Encoding]::new($false))
}

function Save-Json([string]$path, $value) {
    $json = $value | ConvertTo-Json -Depth 8
    [System.IO.File]::WriteAllText($path, $json, [System.Text.UTF8Encoding]::new($false))
}

function Contains-OnlyAllowedChars([string]$text, [System.Collections.Generic.HashSet[int]]$allowedChars) {
    foreach ($char in $text.ToCharArray()) {
        if (-not $allowedChars.Contains([int][char]$char)) {
            return $false
        }
    }

    return $true
}

function Is-AllowedWord([string]$text) {
    if ([string]::IsNullOrWhiteSpace($text)) {
        return $false
    }

    foreach ($char in $text.ToCharArray()) {
        $code = [int][char]$char
        $isAsciiLetter = ($code -ge 65 -and $code -le 90) -or ($code -ge 97 -and $code -le 122)
        if ($isAsciiLetter) {
            continue
        }

        if ($wordExtraUpperCodes -contains $code -or $wordExtraLowerCodes -contains $code) {
            continue
        }

        return $false
    }

    return $true
}

function Contains-LowercaseSpanishLetter([string]$text) {
    foreach ($char in $text.ToCharArray()) {
        $code = [int][char]$char
        if (($code -ge 97 -and $code -le 122) -or ($wordExtraLowerCodes -contains $code)) {
            return $true
        }
    }

    return $false
}

function Convert-IpaDict([string[]]$lines) {
    $map = [ordered]@{}
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $parts = $line.Split("`t")
        if ($parts.Length -lt 2) { continue }

        $word = $parts[0].Trim()
        $pronunciation = $parts[1].Trim()
        if (-not (Is-AllowedWord $word)) { continue }
        if (-not (Contains-LowercaseSpanishLetter $word)) { continue }
        if ($word.Length -lt 3 -or $word.Length -gt 24) { continue }
        if (-not (Contains-OnlyAllowedChars $pronunciation $allowedIpaDictCodeSet)) { continue }

        $key = $word.ToLowerInvariant()
        if (-not $map.Contains($key)) {
            $map[$key] = [pscustomobject]@{
                Word = $key
                Pronunciation = $pronunciation
            }
        }
    }

    return $map.Values | Sort-Object Word
}

function Convert-WikiPron([string[]]$lines) {
    $map = [ordered]@{}
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $parts = $line.Split("`t")
        if ($parts.Length -lt 2) { continue }

        $word = $parts[0].Trim()
        $pronunciation = $parts[1].Trim()
        if (-not (Is-AllowedWord $word)) { continue }
        if (-not (Contains-LowercaseSpanishLetter $word)) { continue }
        if ($word.Length -lt 3 -or $word.Length -gt 24) { continue }
        if (-not (Contains-OnlyAllowedChars $pronunciation $allowedWikiPronCodeSet)) { continue }

        $key = $word.ToLowerInvariant()
        if (-not $map.Contains($key)) {
            $map[$key] = [pscustomobject]@{
                Word = $key
                Pronunciation = $pronunciation
            }
        }
    }

    return $map.Values | Sort-Object Word
}

New-Item -ItemType Directory -Force -Path $SampleOutputDir | Out-Null
New-Item -ItemType Directory -Force -Path $FullOutputDir | Out-Null
New-Item -ItemType Directory -Force -Path $CacheDir | Out-Null

$datasets = @(
    @{
        Name = "ipa_dict_es_es"
        SampleName = "ipa_dict_es_es_sample.tsv"
        FullName = "ipa_dict_es_es_full.tsv"
        Url = "https://raw.githubusercontent.com/open-dict-data/ipa-dict/master/data/es_ES.txt"
        CacheName = "ipa_dict_es_es.txt"
        Converter = "IpaDict"
        Source = "ipa-dict"
        Dialect = "castilian"
    },
    @{
        Name = "ipa_dict_es_mx"
        SampleName = "ipa_dict_es_mx_sample.tsv"
        FullName = "ipa_dict_es_mx_full.tsv"
        Url = "https://raw.githubusercontent.com/open-dict-data/ipa-dict/master/data/es_MX.txt"
        CacheName = "ipa_dict_es_mx.txt"
        Converter = "IpaDict"
        Source = "ipa-dict"
        Dialect = "latin_american"
    },
    @{
        Name = "wikipron_spa_latn_ca_broad_filtered"
        SampleName = "wikipron_spa_latn_ca_broad_filtered_sample.tsv"
        FullName = "wikipron_spa_latn_ca_broad_filtered_full.tsv"
        Url = "https://raw.githubusercontent.com/CUNY-CL/wikipron/master/data/scrape/tsv/spa_latn_ca_broad_filtered.tsv"
        CacheName = "wikipron_spa_latn_ca_broad_filtered.tsv"
        Converter = "WikiPron"
        Source = "wikipron"
        Dialect = "castilian"
    },
    @{
        Name = "wikipron_spa_latn_la_broad_filtered"
        SampleName = "wikipron_spa_latn_la_broad_filtered_sample.tsv"
        FullName = "wikipron_spa_latn_la_broad_filtered_full.tsv"
        Url = "https://raw.githubusercontent.com/CUNY-CL/wikipron/master/data/scrape/tsv/spa_latn_la_broad_filtered.tsv"
        CacheName = "wikipron_spa_latn_la_broad_filtered.tsv"
        Converter = "WikiPron"
        Source = "wikipron"
        Dialect = "latin_american"
    }
)

$manifest = New-Object 'System.Collections.Generic.List[object]'

foreach ($dataset in $datasets) {
    $cachePath = Join-Path $CacheDir $dataset.CacheName
    $lines = Get-RemoteLines -url $dataset.Url -cachePath $cachePath -forceDownload:$Force
    $entries =
        if ($dataset.Converter -eq "IpaDict") { Convert-IpaDict $lines }
        else { Convert-WikiPron $lines }

    if ($Mode -eq "Full" -or $Mode -eq "Both") {
        $fullPath = Join-Path $FullOutputDir $dataset.FullName
        Save-Tsv $fullPath $entries
        Write-Host ("Wrote {0} filtered entries to {1}" -f $entries.Count, $fullPath)
    }

    if ($Mode -eq "Sample" -or $Mode -eq "Both") {
        $sample = Select-DeterministicSample $entries $SampleSize
        $samplePath = Join-Path $SampleOutputDir $dataset.SampleName
        Save-Tsv $samplePath $sample
        Write-Host ("Wrote {0} sample entries to {1}" -f $sample.Count, $samplePath)
    }

    $manifest.Add([pscustomobject]@{
        Name = $dataset.Name
        Source = $dataset.Source
        Dialect = $dataset.Dialect
        Url = $dataset.Url
        CachePath = $cachePath
        FilteredEntries = $entries.Count
        SampleEntries = [Math]::Min($SampleSize, $entries.Count)
        GeneratedAtUtc = [DateTime]::UtcNow.ToString("o")
        Mode = $Mode
    }) | Out-Null
}

if ($Mode -eq "Sample" -or $Mode -eq "Both") {
    Save-Json (Join-Path $SampleOutputDir "spanish_eval_manifest.json") $manifest
}

if ($Mode -eq "Full" -or $Mode -eq "Both") {
    Save-Json (Join-Path $FullOutputDir "spanish_eval_manifest.json") $manifest
}
