<#
.SYNOPSIS
    共有 Internal ファイルの同期チェック・自動修正スクリプト。

.DESCRIPTION
    複数パッケージにコピーされている Internal ファイル（BatchConversionHelper.cs, PreserveAttribute.cs）を
    マスターファイルと比較し、差分を検出・修正する。

.PARAMETER Check
    差分チェックのみ実行し、差分があれば exit 1（CI向け）。

.PARAMETER Fix
    マスターファイルから各コピー先へ自動コピーして同期する。

.EXAMPLE
    # 差分レポートのみ表示
    pwsh tools/sync-shared-internals.ps1

    # CI用チェック（差分があれば exit 1）
    pwsh tools/sync-shared-internals.ps1 -Check

    # 自動修正
    pwsh tools/sync-shared-internals.ps1 -Fix
#>
param(
    [switch]$Check,
    [switch]$Fix,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"

# ── 同期対象定義 ──────────────────────────────────────────────
# マスター → コピー先のマッピング
$SyncTargets = @(
    @{
        Master = "src/DotNetG2P.Core/Internal/BatchConversionHelper.cs"
        Copies = @(
            "src/DotNetG2P.English/Internal/BatchConversionHelper.cs"
            "src/DotNetG2P.Chinese/Internal/BatchConversionHelper.cs"
            "src/DotNetG2P.Korean/Internal/BatchConversionHelper.cs"
            "src/DotNetG2P.Spanish/Internal/BatchConversionHelper.cs"
            "src/DotNetG2P.French/Internal/BatchConversionHelper.cs"
            "src/DotNetG2P.Portuguese/Internal/BatchConversionHelper.cs"
        )
    }
    @{
        Master = "src/DotNetG2P.Chinese/Internal/PreserveAttribute.cs"
        Copies = @(
            "src/DotNetG2P.Korean/Internal/PreserveAttribute.cs"
            "src/DotNetG2P.English/Internal/PreserveAttribute.cs"
            "src/DotNetG2P.Spanish/Internal/PreserveAttribute.cs"
            "src/DotNetG2P.French/Internal/PreserveAttribute.cs"
            "src/DotNetG2P.Portuguese/Internal/PreserveAttribute.cs"
        )
    }
)

# ── メイン処理 ────────────────────────────────────────────────
$driftCount = 0
$totalChecked = 0

foreach ($target in $SyncTargets) {
    $masterRel = $target.Master
    $masterPath = Join-Path $RepoRoot $masterRel

    if (-not (Test-Path $masterPath)) {
        Write-Error "マスターファイルが見つかりません: $masterRel"
        exit 1
    }

    $masterContent = Get-Content -Path $masterPath -Raw

    foreach ($copyRel in $target.Copies) {
        $totalChecked++
        $copyPath = Join-Path $RepoRoot $copyRel

        if (-not (Test-Path $copyPath)) {
            Write-Host "[MISSING] $copyRel (マスター: $masterRel)" -ForegroundColor Red
            $driftCount++

            if ($Fix) {
                $copyDir = Split-Path $copyPath -Parent
                if (-not (Test-Path $copyDir)) {
                    New-Item -ItemType Directory -Path $copyDir -Force | Out-Null
                }
                Copy-Item -Path $masterPath -Destination $copyPath -Force
                Write-Host "  -> 作成しました" -ForegroundColor Green
            }
            continue
        }

        $copyContent = Get-Content -Path $copyPath -Raw

        if ($masterContent -eq $copyContent) {
            Write-Host "[OK]      $copyRel" -ForegroundColor Green
        }
        else {
            Write-Host "[DRIFT]   $copyRel (マスター: $masterRel)" -ForegroundColor Yellow
            $driftCount++

            if ($Fix) {
                Copy-Item -Path $masterPath -Destination $copyPath -Force
                Write-Host "  -> マスターからコピーしました" -ForegroundColor Green
            }
        }
    }
}

# ── 結果サマリー ──────────────────────────────────────────────
Write-Host ""
Write-Host "チェック完了: $totalChecked ファイル中 $driftCount 件の差分" -ForegroundColor Cyan

if ($driftCount -gt 0) {
    if ($Fix) {
        Write-Host "すべての差分を修正しました。" -ForegroundColor Green
    }
    elseif ($Check) {
        Write-Host "差分が検出されました。'pwsh tools/sync-shared-internals.ps1 -Fix' で修正してください。" -ForegroundColor Red
        exit 1
    }
    else {
        Write-Host "差分があります。'-Fix' で自動修正、'-Check' でCI用チェックが可能です。" -ForegroundColor Yellow
    }
}
else {
    Write-Host "すべてのファイルが同期済みです。" -ForegroundColor Green
}
