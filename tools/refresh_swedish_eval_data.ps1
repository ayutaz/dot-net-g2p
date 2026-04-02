param(
    [string]$SampleOutputDir = (Join-Path $PSScriptRoot "..\tests\TestData\SwedishG2P"),
    [string]$FullOutputDir = (Join-Path $PSScriptRoot "..\artifacts\swedish-eval\corpora"),
    [string]$CacheDir = (Join-Path $PSScriptRoot "..\.cache\swedish-eval"),
    [ValidateSet("Sample", "Full", "Both")]
    [string]$Mode = "Both",
    [int]$SampleSize = 256,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# スウェーデン語の文字コード: àáäåéèëíïóöúü + 基本ASCII文字
# Swedish uses å, ä, ö primarily; others appear in loanwords
$wordExtraUpperCodes = @(192, 193, 196, 197, 200, 201, 203, 205, 207, 211, 214, 218, 220)
# À=192 Á=193 Ä=196 Å=197 È=200 É=201 Ë=203 Í=205 Ï=207 Ó=211 Ö=214 Ú=218 Ü=220
$wordExtraLowerCodes = @(224, 225, 228, 229, 232, 233, 235, 237, 239, 243, 246, 250, 252)
# à=224 á=225 ä=228 å=229 è=232 é=233 ë=235 í=237 ï=239 ó=243 ö=246 ú=250 ü=252

# ipa-dict のIPA転写で許可する文字コード
# スウェーデン語IPA: 母音 a e i o u y ɑ ɛ ɪ ɔ ʊ ʉ ɵ ø ɧ ɜ œ æ、子音 b d f g h j k l m n p r s t v ŋ ɕ ɖ ɡ ɳ ɭ ʂ ʈ、
# 超分節 ˈ ˌ ː
$ipaDictAllowedCodes = @(
    32, 46, 47,    # スペース, ., /
    # 基本ラテン小文字（IPA基本子音・母音）
    97, 98, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 114, 115, 116, 117, 118, 119, 120, 121, 122,
    # IPA拡張母音
    230,   # æ (U+00E6)
    248,   # ø (U+00F8)
    593,   # ɑ (U+0251)
    603,   # ɛ (U+025B)
    604,   # ɜ (U+025C)
    618,   # ɪ (U+026A)
    596,   # ɔ (U+0254)
    649,   # ʉ (U+0289)
    650,   # ʊ (U+028A)
    339,   # œ (U+0153)
    629,   # ɵ (U+0275)
    # IPA拡張子音
    331,   # ŋ (U+014B)
    597,   # ɕ (U+0255)
    598,   # ɖ (U+0256)
    609,   # ɡ (U+0261)
    611,   # ɣ (U+0263) — 一部転写で出現
    615,   # ɧ (U+0267) — スウェーデン語特有のsje-ljud
    638,   # ɾ (U+027E)
    641,   # ʁ (U+0281)
    643,   # ʂ (U+0282)
    648,   # ʈ (U+0288)
    653,   # ʍ (U+028D)
    668,   # ɳ (U+0273) — 巻き舌鼻音
    621,   # ɭ (U+026D) — 巻き舌側面音
    600,   # ɘ (U+0258)
    # 超分節記号
    712,   # ˈ (U+02C8) — 第一強勢
    716,   # ˌ (U+02CC) — 第二強勢
    720,   # ː (U+02D0) — 長音
    # 結合記号
    776,   # ̈ (U+0308) — 結合分音記号
    771,   # ̃ (U+0303) — 結合ティルダ
    809,   # ̩ (U+0329) — 音節主音マーカー
    810,   # ̯ (U+032F) — 非音節マーカー
    865    # ͡ (U+0361) — 結合タイバー
)

# WikiPron のIPA転写で許可する文字コード（スラッシュなし、パイプあり）
$wikiPronAllowedCodes = @(
    32, 124,  # スペース, パイプ
    # 基本ラテン小文字
    97, 98, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 114, 115, 116, 117, 118, 119, 120, 121, 122,
    # IPA拡張母音
    230,   # æ
    248,   # ø
    593,   # ɑ
    603,   # ɛ
    604,   # ɜ
    618,   # ɪ
    596,   # ɔ
    649,   # ʉ
    650,   # ʊ
    339,   # œ
    629,   # ɵ
    # IPA拡張子音
    331,   # ŋ
    597,   # ɕ
    598,   # ɖ
    609,   # ɡ
    611,   # ɣ
    615,   # ɧ
    638,   # ɾ
    641,   # ʁ
    643,   # ʂ
    648,   # ʈ
    653,   # ʍ
    668,   # ɳ
    621,   # ɭ
    600,   # ɘ
    # 超分節記号
    712,   # ˈ
    716,   # ˌ
    720,   # ː
    # 結合記号
    776,   # ̈
    771,   # ̃
    809,   # ̩
    810,   # ̯
    865    # ͡
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

function Contains-LowercaseSwedishLetter([string]$text) {
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

        # 複数発音（カンマ区切り）→ 最初のもののみ
        if ($pronunciation.Contains(",")) {
            $pronunciation = $pronunciation.Split(",")[0].Trim()
        }

        if (-not (Is-AllowedWord $word)) { continue }
        if (-not (Contains-LowercaseSwedishLetter $word)) { continue }
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
        if (-not (Contains-LowercaseSwedishLetter $word)) { continue }
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
        Name = "ipa_dict_sv_se"
        SampleName = "ipa_dict_sv_se_sample.tsv"
        FullName = "ipa_dict_sv_se_full.tsv"
        Url = "https://raw.githubusercontent.com/open-dict-data/ipa-dict/master/data/sv.txt"
        CacheName = "ipa_dict_sv.txt"
        Converter = "IpaDict"
        Source = "ipa-dict"
        Dialect = "standard"
    },
    @{
        Name = "wikipron_swe_latn_broad"
        SampleName = "wikipron_swe_latn_broad_filtered_sample.tsv"
        FullName = "wikipron_swe_latn_broad_filtered_full.tsv"
        Url = "https://raw.githubusercontent.com/CUNY-CL/wikipron/master/data/scrape/tsv/swe_latn_broad.tsv"
        CacheName = "wikipron_swe_latn_broad.tsv"
        Converter = "WikiPron"
        Source = "wikipron"
        Dialect = "standard"
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
    Save-Json (Join-Path $SampleOutputDir "swedish_eval_manifest.json") $manifest
}

if ($Mode -eq "Full" -or $Mode -eq "Both") {
    Save-Json (Join-Path $FullOutputDir "swedish_eval_manifest.json") $manifest
}
