param(
    [string]$SampleOutputDir = (Join-Path $PSScriptRoot "..\tests\TestData\FrenchG2P"),
    [string]$FullOutputDir = (Join-Path $PSScriptRoot "..\artifacts\french-eval\corpora"),
    [string]$CacheDir = (Join-Path $PSScriptRoot "..\.cache\french-eval"),
    [ValidateSet("Sample", "Full", "Both")]
    [string]$Mode = "Both",
    [int]$SampleSize = 500,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# フランス語の文字コード: àâæçéèêëïîôùûüœ + 基本ASCII文字
$wordExtraUpperCodes = @(192, 194, 198, 199, 200, 201, 202, 203, 206, 207, 212, 217, 219, 220, 338)
# À=192 Â=194 Æ=198 Ç=199 È=200 É=201 Ê=202 Ë=203 Î=206 Ï=207 Ô=212 Ù=217 Û=219 Ü=220 Œ=338
$wordExtraLowerCodes = @(224, 226, 230, 231, 232, 233, 234, 235, 238, 239, 244, 249, 251, 252, 339)
# à=224 â=226 æ=230 ç=231 è=232 é=233 ê=234 ë=235 î=238 ï=239 ô=244 ù=249 û=251 ü=252 œ=339

# ipa-dict のIPA転写で許可する文字コード
# スペース=32, .=46, /=47, a-z(一部), 各種IPA記号
$ipaDictAllowedCodes = @(
    32, 46, 47,
    97, 98, 100, 101, 102, 103, 105, 106, 107, 108, 109, 110, 111, 112, 114, 115, 116, 117, 118, 119, 120, 121, 122,
    248,   # ø
    249,   # (使わないかもだが安全のため)
    259,   # ə
    265,   # ɥ
    281,   # ʁ
    283,   # ʃ
    292,   # ʒ
    272,   # ɲ
    331,   # ŋ
    339,   # œ
    596,   # ɔ
    603,   # ɛ
    609,   # ɑ (U+0251)
    712,   # ˈ
    716,   # ˌ
    771,   # ̃ (結合ティルダ)
    768,   # ̀ (結合グレーブ、稀)
    769    # ́ (結合アキュート、稀)
)

# WikiPron のIPA転写で許可する文字コード（スペース区切り）
$wikiPronAllowedCodes = @(
    32, 124,  # スペース, パイプ
    97, 98, 100, 101, 102, 103, 105, 106, 107, 108, 109, 110, 111, 112, 114, 115, 116, 117, 118, 119, 120, 121, 122,
    248,   # ø
    259,   # ə
    265,   # ɥ
    281,   # ʁ
    283,   # ʃ
    292,   # ʒ
    272,   # ɲ
    331,   # ŋ
    339,   # œ
    596,   # ɔ
    603,   # ɛ
    609,   # ɑ (U+0251)
    771,   # ̃ (結合ティルダ)
    810    # ̯ (非音節マーカー)
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
        # ハイフン許可（複合語: peut-être 等）
        if ($code -eq 45) {
            continue
        }

        # アポストロフィ許可（l'homme 等）
        if ($code -eq 39 -or $code -eq 8217) {
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

function Contains-LowercaseFrenchLetter([string]$text) {
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
        if (-not (Contains-LowercaseFrenchLetter $word)) { continue }
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
        if (-not (Contains-LowercaseFrenchLetter $word)) { continue }
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
        Name = "ipa_dict_fr_fr"
        SampleName = "ipa_dict_fr_fr_sample.tsv"
        FullName = "ipa_dict_fr_fr_full.tsv"
        Url = "https://raw.githubusercontent.com/open-dict-data/ipa-dict/master/data/fr.txt"
        CacheName = "ipa_dict_fr_fr.txt"
        Converter = "IpaDict"
        Source = "ipa-dict"
        Dialect = "metropolitan"
    },
    @{
        Name = "wikipron_fra_latn_broad_filtered"
        SampleName = "wikipron_fra_latn_broad_filtered_sample.tsv"
        FullName = "wikipron_fra_latn_broad_filtered_full.tsv"
        Url = "https://raw.githubusercontent.com/CUNY-CL/wikipron/master/data/scrape/tsv/fra_latn_broad_filtered.tsv"
        CacheName = "wikipron_fra_latn_broad_filtered.tsv"
        Converter = "WikiPron"
        Source = "wikipron"
        Dialect = "metropolitan"
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
    Save-Json (Join-Path $SampleOutputDir "french_eval_manifest.json") $manifest
}

if ($Mode -eq "Full" -or $Mode -eq "Both") {
    Save-Json (Join-Path $FullOutputDir "french_eval_manifest.json") $manifest
}
