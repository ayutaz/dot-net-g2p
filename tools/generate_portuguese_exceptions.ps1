# generate_portuguese_exceptions.ps1
# ポルトガル語例外辞書マスターTSVの検証・整形スクリプト
#
# 機能:
#   - TSVフォーマット検証（列数、必須フィールド、方言値、カテゴリ値）
#   - 重複エントリの検出
#   - surface列でソート済みの出力生成
#   - カテゴリ別・方言別エントリ数統計の表示
#
# 使い方:
#   pwsh tools/generate_portuguese_exceptions.ps1
#   pwsh tools/generate_portuguese_exceptions.ps1 -Validate
#   pwsh tools/generate_portuguese_exceptions.ps1 -MasterPath path/to/master.tsv

param(
    [string]$MasterPath = "src/DotNetG2P.Portuguese/Data/portuguese_exceptions.master.tsv",
    [switch]$Validate,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$master = Join-Path $repoRoot $MasterPath

if (-not (Test-Path $master)) {
    throw "マスター例外辞書が見つかりません: $master"
}

# --- 定数 ---
$requiredColumns = 7
$headerLine = "surface`tdialect`tcategory`tstress_index`tphonemes`tsource`tnote"
$validDialects = @("*", "brazilian", "european")
$validCategories = @(
    "loanword",
    "proper_noun",
    "brand",
    "irregular",
    "hiato_override",
    "verb_3pl",
    "academic",
    "homograph",
    "abbreviation",
    "archaic",
    "colloquial",
    "toponym",
    "nasal_override",
    "stress_override"
)

# --- 読み込み ---
$rawLines = [System.IO.File]::ReadAllLines($master, [System.Text.UTF8Encoding]::new($false))

$errors = New-Object 'System.Collections.Generic.List[string]'
$warnings = New-Object 'System.Collections.Generic.List[string]'
$entries = New-Object 'System.Collections.Generic.List[object]'
$commentLines = New-Object 'System.Collections.Generic.List[string]'
$headerFound = $false

$duplicateTracker = New-Object 'System.Collections.Generic.Dictionary[string,int]'

for ($i = 0; $i -lt $rawLines.Length; $i++) {
    $lineNum = $i + 1
    $line = $rawLines[$i]

    # 空行
    if ([string]::IsNullOrWhiteSpace($line)) {
        continue
    }

    # コメント行
    if ($line.StartsWith("#")) {
        $commentLines.Add($line) | Out-Null
        continue
    }

    # ヘッダ行
    if ($line.StartsWith("surface`t")) {
        if ($headerFound) {
            $warnings.Add("行 ${lineNum}: ヘッダ行が重複しています") | Out-Null
        }
        $headerFound = $true

        # ヘッダ列数チェック
        $headerParts = $line.Split("`t")
        if ($headerParts.Length -lt $requiredColumns) {
            $errors.Add("行 ${lineNum}: ヘッダの列数が不足しています (${($headerParts.Length)} < $requiredColumns)") | Out-Null
        }
        continue
    }

    # データ行
    $parts = $line.Split("`t")

    # 列数チェック
    if ($parts.Length -lt $requiredColumns) {
        $errors.Add("行 ${lineNum}: 列数不足 (${($parts.Length)} < $requiredColumns): $($parts[0])") | Out-Null
        continue
    }

    $surface = $parts[0].Trim()
    $dialect = $parts[1].Trim()
    $category = $parts[2].Trim()
    $stressIndex = $parts[3].Trim()
    $phonemes = $parts[4].Trim()
    $source = $parts[5].Trim()
    $note = $parts[6].Trim()

    # 必須フィールド確認
    if ([string]::IsNullOrWhiteSpace($surface)) {
        $errors.Add("行 ${lineNum}: surfaceが空です") | Out-Null
        continue
    }
    if ([string]::IsNullOrWhiteSpace($phonemes)) {
        $errors.Add("行 ${lineNum}: phonemesが空です: $surface") | Out-Null
        continue
    }
    if ([string]::IsNullOrWhiteSpace($source)) {
        $warnings.Add("行 ${lineNum}: sourceが空です: $surface") | Out-Null
    }

    # 方言チェック
    if ($validDialects -notcontains $dialect) {
        $errors.Add("行 ${lineNum}: 無効な方言値 '$dialect': $surface (有効値: $($validDialects -join ', '))") | Out-Null
    }

    # カテゴリチェック
    if ($validCategories -notcontains $category) {
        $warnings.Add("行 ${lineNum}: 未知のカテゴリ '$category': $surface") | Out-Null
    }

    # stress_indexが整数であるか
    if (-not [int]::TryParse($stressIndex, [ref]$null)) {
        $errors.Add("行 ${lineNum}: stress_indexが整数ではありません '$stressIndex': $surface") | Out-Null
    }

    # 重複チェック（surface + dialect の組み合わせ）
    $dupKey = "${surface}`t${dialect}"
    if ($duplicateTracker.ContainsKey($dupKey)) {
        $prevLine = $duplicateTracker[$dupKey]
        $warnings.Add("行 ${lineNum}: 重複エントリ (surface='$surface', dialect='$dialect') - 最初の出現: 行 $prevLine") | Out-Null
    }
    else {
        $duplicateTracker[$dupKey] = $lineNum
    }

    $entries.Add([pscustomobject]@{
        Surface    = $surface
        Dialect    = $dialect
        Category   = $category
        StressIndex = $stressIndex
        Phonemes   = $phonemes
        Source     = $source
        Note       = $note
        LineNum    = $lineNum
    }) | Out-Null
}

# --- ヘッダチェック ---
if (-not $headerFound) {
    $errors.Add("ヘッダ行が見つかりません (期待: $headerLine)") | Out-Null
}

# --- 統計表示 ---
Write-Host ""
Write-Host "=== ポルトガル語例外辞書統計 ==="
Write-Host "ファイル: $master"
Write-Host "総エントリ数: $($entries.Count)"
Write-Host ""

# カテゴリ別統計
Write-Host "--- カテゴリ別 ---"
$categoryGroups = $entries | Group-Object -Property Category | Sort-Object -Property Count -Descending
foreach ($group in $categoryGroups) {
    Write-Host ("  {0,-20} {1,5}" -f $group.Name, $group.Count)
}
Write-Host ""

# 方言別統計
Write-Host "--- 方言別 ---"
$dialectGroups = $entries | Group-Object -Property Dialect | Sort-Object -Property Name
foreach ($group in $dialectGroups) {
    $label = switch ($group.Name) {
        "*"           { "* (共通)" }
        "brazilian"   { "brazilian (ブラジル)" }
        "european"    { "european (ヨーロッパ)" }
        default       { $group.Name }
    }
    Write-Host ("  {0,-30} {1,5}" -f $label, $group.Count)
}
Write-Host ""

# --- エラー・警告表示 ---
if ($warnings.Count -gt 0) {
    Write-Host "--- 警告 ($($warnings.Count)件) ---" -ForegroundColor Yellow
    foreach ($w in $warnings) {
        Write-Host "  WARNING: $w" -ForegroundColor Yellow
    }
    Write-Host ""
}

if ($errors.Count -gt 0) {
    Write-Host "--- エラー ($($errors.Count)件) ---" -ForegroundColor Red
    foreach ($e in $errors) {
        Write-Host "  ERROR: $e" -ForegroundColor Red
    }
    Write-Host ""
    throw "$($errors.Count)件の検証エラーが見つかりました。修正してから再実行してください。"
}

# --- Validateモードなら検証のみで終了 ---
if ($Validate) {
    Write-Host "検証完了: エラーなし ($($entries.Count)エントリ)" -ForegroundColor Green
    exit 0
}

# --- surface列でソートして書き戻し ---
$sortedEntries = $entries | Sort-Object -Property Surface, Dialect

$outputLines = New-Object 'System.Collections.Generic.List[string]'
$outputLines.Add($headerLine) | Out-Null

# コメント行をカテゴリヘッダとして再構築
$currentCategory = ""
foreach ($entry in $sortedEntries) {
    if ($entry.Category -ne $currentCategory) {
        $currentCategory = $entry.Category
        $outputLines.Add("# ===== $currentCategory =====") | Out-Null
    }

    $outputLines.Add(("{0}`t{1}`t{2}`t{3}`t{4}`t{5}`t{6}" -f `
        $entry.Surface,
        $entry.Dialect,
        $entry.Category,
        $entry.StressIndex,
        $entry.Phonemes,
        $entry.Source,
        $entry.Note
    )) | Out-Null
}

if ($DryRun) {
    Write-Host "--- DryRun: 以下の内容を書き込みます ---"
    foreach ($line in $outputLines) {
        Write-Host $line
    }
}
else {
    [System.IO.File]::WriteAllLines($master, $outputLines.ToArray(), [System.Text.UTF8Encoding]::new($false))
    Write-Host "ソート済みマスターTSVを書き込みました: $master" -ForegroundColor Green
    Write-Host "エントリ数: $($sortedEntries.Count)" -ForegroundColor Green
}
