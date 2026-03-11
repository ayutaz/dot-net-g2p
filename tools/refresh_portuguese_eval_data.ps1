param(
    [string]$SampleOutputDir = (Join-Path $PSScriptRoot "..\tests\TestData\PortugueseG2P"),
    [string]$FullOutputDir = (Join-Path $PSScriptRoot "..\artifacts\portuguese-eval\corpora"),
    [string]$CacheDir = (Join-Path $PSScriptRoot "..\.cache\portuguese-eval"),
    [ValidateSet("Sample", "Full", "Both")]
    [string]$Mode = "Both",
    [int]$SampleSize = 500,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# ポルトガル語の文字コード: àáâãçéêíóôõúü + 基本ASCII文字
$wordExtraUpperCodes = @(192, 193, 194, 195, 199, 200, 201, 202, 205, 211, 212, 213, 218, 220)
# À=192 Á=193 Â=194 Ã=195 Ç=199 È=200 É=201 Ê=202 Í=205 Ó=211 Ô=212 Õ=213 Ú=218 Ü=220
$wordExtraLowerCodes = @(224, 225, 226, 227, 231, 232, 233, 234, 237, 243, 244, 245, 250, 252)
# à=224 á=225 â=226 ã=227 ç=231 è=232 é=233 ê=234 í=237 ó=243 ô=244 õ=245 ú=250 ü=252

# ipa-dict のIPA転写で許可する文字コード
$ipaDictAllowedCodes = @(
    32, 46, 47,    # スペース, ., /
    97, 98, 100, 101, 102, 103, 105, 106, 107, 108, 109, 110, 111, 112, 114, 115, 116, 117, 118, 119, 120, 122,
    245,   # õ
    250,   # ɐ (U+0250)
    259,   # ə (稀)
    272,   # ɲ
    283,   # ʃ
    292,   # ʒ
    331,   # ŋ
    360,   # ɨ (U+0268)
    596,   # ɔ
    603,   # ɛ
    609,   # ɡ (U+0261)
    638,   # ɾ
    641,   # ʁ
    654,   # ʎ
    712,   # ˈ
    716,   # ˌ
    771,   # ̃ (結合ティルダ)
    865    # ͡ (結合タイバー)
)

# WikiPron のIPA転写で許可する文字コード
$wikiPronAllowedCodes = @(
    32, 124,  # スペース, パイプ
    97, 98, 100, 101, 102, 103, 105, 106, 107, 108, 109, 110, 111, 112, 114, 115, 116, 117, 118, 119, 120, 122,
    245,   # õ
    250,   # ɐ (U+0250)
    259,   # ə
    272,   # ɲ
    283,   # ʃ
    292,   # ʒ
    331,   # ŋ
    360,   # ɨ (U+0268)
    596,   # ɔ
    603,   # ɛ
    609,   # ɡ (U+0261)
    638,   # ɾ
    641,   # ʁ
    654,   # ʎ
    771,   # ̃ (結合ティルダ)
    810,   # ̯ (非音節マーカー)
    865    # ͡ (結合タイバー)
)

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
        # ハイフン許可（複合語）
        if ($code -eq 45) {
            continue
        }

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

function Contains-LowercasePortugueseLetter([string]$text) {
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
        if (-not (Contains-LowercasePortugueseLetter $word)) { continue }
        if ($word.Length -lt 2 -or $word.Length -gt 30) { continue }
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
        if (-not (Contains-LowercasePortugueseLetter $word)) { continue }
        if ($word.Length -lt 2 -or $word.Length -gt 30) { continue }
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
        Name = "ipa_dict_pt_br"
        SampleName = "ipa_dict_pt_br_sample.tsv"
        FullName = "ipa_dict_pt_br_full.tsv"
        Url = "https://raw.githubusercontent.com/open-dict-data/ipa-dict/master/data/pt_BR.txt"
        CacheName = "ipa_dict_pt_br.txt"
        Converter = "IpaDict"
        Source = "ipa-dict"
        Dialect = "brazilian"
    },
    @{
        Name = "wikipron_por_latn_br_broad_filtered"
        SampleName = "wikipron_por_latn_br_broad_filtered_sample.tsv"
        FullName = "wikipron_por_latn_br_broad_filtered_full.tsv"
        Url = "https://raw.githubusercontent.com/CUNY-CL/wikipron/master/data/scrape/tsv/por_latn_br_broad_filtered.tsv"
        CacheName = "wikipron_por_latn_br_broad_filtered.tsv"
        Converter = "WikiPron"
        Source = "wikipron"
        Dialect = "brazilian"
    },
    @{
        Name = "wikipron_por_latn_pt_broad_filtered"
        SampleName = "wikipron_por_latn_pt_broad_filtered_sample.tsv"
        FullName = "wikipron_por_latn_pt_broad_filtered_full.tsv"
        Url = "https://raw.githubusercontent.com/CUNY-CL/wikipron/master/data/scrape/tsv/por_latn_pt_broad_filtered.tsv"
        CacheName = "wikipron_por_latn_pt_broad_filtered.tsv"
        Converter = "WikiPron"
        Source = "wikipron"
        Dialect = "european"
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
    Save-Json (Join-Path $SampleOutputDir "portuguese_eval_manifest.json") $manifest
}

if ($Mode -eq "Full" -or $Mode -eq "Both") {
    Save-Json (Join-Path $FullOutputDir "portuguese_eval_manifest.json") $manifest
}
