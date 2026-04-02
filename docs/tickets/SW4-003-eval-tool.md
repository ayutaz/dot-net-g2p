# SW4-003: 評価ツール（SwedishEval）

> **マイルストーン**: Sw4 — Multilingual統合 + 評価ツール + リリース準備
> **前提チケット**: なし（Sw3完了が前提。SW4-001/002と並行可能）
> **後続チケット**: SW4-005

## 1. タスク目的とゴール

スウェーデン語G2Pのフルコーパス評価を自動化するツール群を整備する。ipa-dict（21,107件）および WikiPron（4,631件）のフルデータセットで PER を計測し、閾値内であることを検証可能にする。既存の SpanishEval / FrenchEval / PortugueseEval と同一の構成・パターンに従う。

**完了の定義:**
- `tools/DotNetG2P.SwedishEval/` プロジェクトが作成され `dotnet run` で評価を実行できる
- `tools/refresh_swedish_eval_data.ps1` がフルデータセット TSV を生成する
- `tools/run_swedish_full_evaluation.ps1` がフル評価を実行しレポートを出力する
- `tools/swedish_eval_thresholds.json` が PER 閾値を定義する
- フルデータセット: `ipa_dict_sv_se_full.tsv`（21,107件）、`wikipron_swe_latn_broad_filtered_full.tsv`（4,631件）が生成される

## 2. 実装内容の詳細

### 2.1 DotNetG2P.SwedishEval プロジェクト

```
tools/DotNetG2P.SwedishEval/
├── DotNetG2P.SwedishEval.csproj
└── Program.cs
```

#### DotNetG2P.SwedishEval.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\DotNetG2P.Swedish\DotNetG2P.Swedish.csproj" />
  </ItemGroup>
</Project>
```

#### Program.cs

既存の PortugueseEval/FrenchEval/SpanishEval の Program.cs をテンプレートとして使用する。

**主要機能:**
- TSV ファイル読み込み（surface \t ipa 形式）
- SwedishG2PEngine による変換実行
- Levenshtein 距離ベースの PER 計算
- プロファイル別（base / allophones / no_exceptions）評価
- 方言別（Central / FinlandSwedish）評価
- コンソールレポート出力（語数、PER、エラー上位語、処理時間）
- JSON レポート出力（CI連携用）

**コマンドライン引数:**
```
dotnet run -- <tsv-path> [--profile base|allophones|no_exceptions] [--dialect central|finland] [--top-errors 50] [--output-json <path>]
```

**PER 計算方法:**
```
PER = Σ(Levenshtein距離(predicted, reference)) / Σ(reference音素数)
```
- 比較前にストレスマーク（ˈ, ˌ）とアクセントマーク（¹, ²）を除去する base プロファイルと、含めるプロファイルの両方を計測
- IPA 正規化: 長音記号 ː の位置統一、そり舌音記号の正規化

### 2.2 refresh_swedish_eval_data.ps1

既存の `refresh_portuguese_eval_data.ps1` をテンプレートとして作成する。

**処理フロー:**
1. ipa-dict sv.txt をダウンロード（`https://raw.githubusercontent.com/open-dict-data/ipa-dict/master/data/sv.txt`）
2. WikiPron swe_latn_broad.tsv をダウンロード（`https://raw.githubusercontent.com/CUNY-CL/wikipron/master/data/scrape/tsv/swe_latn_broad.tsv`）
3. ipa-dict: タブ区切りの surface\tipa 形式に変換、重複排除 → `ipa_dict_sv_se_full.tsv`（21,107件）
4. WikiPron: フィルタリング（空行除去、非IPA行除去）、重複排除 → `wikipron_swe_latn_broad_filtered_full.tsv`（4,631件）
5. サンプルデータ抽出（各256件ランダム） → `*_sample.tsv`（テスト用、既存 Sw2 で作成済みのファイルと互換）

**出力先:**
- フル: `tests/TestData/SwedishG2P/ipa_dict_sv_se_full.tsv`
- フル: `tests/TestData/SwedishG2P/wikipron_swe_latn_broad_filtered_full.tsv`
- サンプル: `tests/TestData/SwedishG2P/ipa_dict_sv_se_sample.tsv`（更新）
- サンプル: `tests/TestData/SwedishG2P/wikipron_swe_latn_broad_filtered_sample.tsv`（更新）

**ipa-dict スウェーデン語固有の前処理:**
- 声調アクセントマーク `²` の保持（base プロファイルでは比較時に除去）
- 複数発音エントリ（`,` 区切り）の最初のエントリを採用
- IPA 正規化: NFC 形式に統一

### 2.3 run_swedish_full_evaluation.ps1

```powershell
# フル評価実行スクリプト
# 1. refresh_swedish_eval_data.ps1 でデータ更新
# 2. DotNetG2P.SwedishEval でフル評価実行
# 3. 結果を swedish_eval_thresholds.json の閾値と比較
# 4. 閾値超過時は非ゼロ終了コード

$profiles = @("base", "allophones", "no_exceptions")
$datasets = @(
    @{ Name = "ipa_dict_sv_se"; File = "ipa_dict_sv_se_full.tsv" },
    @{ Name = "wikipron_swe_latn_broad"; File = "wikipron_swe_latn_broad_filtered_full.tsv" }
)
```

### 2.4 swedish_eval_thresholds.json

```json
{
  "ipa_dict_sv_se": {
    "base": 0.04,
    "allophones": 0.03,
    "no_exceptions": 0.12
  },
  "wikipron_swe_latn_broad": {
    "base": 0.05,
    "allophones": 0.04,
    "no_exceptions": 0.15
  }
}
```

**閾値設定根拠:**
- ipa-dict base < 4%: スペイン語 1.69% より緩い。スウェーデン語は sj 音の65種類の綴りと複合語の分解が難しいため
- WikiPron base < 5%: WikiPron はユーザー寄稿のため品質にばらつきがある
- no_exceptions: milestones.md 付録A に従い ipa-dict < 12%、WikiPron < 15% に設定。例外辞書なしでの規則のみの精度を評価

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装担当 | 1名 | Program.cs, csproj, PowerShell スクリプト3本, thresholds JSON |
| テスト担当 | 1名 | ローカルでの評価実行確認、データ品質チェック |

**合計: 2名**

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

- `tools/DotNetG2P.SwedishEval/Program.cs` および `DotNetG2P.SwedishEval.csproj`
- `tools/refresh_swedish_eval_data.ps1`
- `tools/run_swedish_full_evaluation.ps1`
- `tools/swedish_eval_thresholds.json`
- フルデータセット TSV の生成検証

**スコープ外:**
- SwedishDatasetEvaluationTests のフル評価拡張（SW4-005）
- CI への評価ツール組み込み（SW4-004）
- Multilingual テスト（SW4-005）

### ユニットテスト

評価ツール自体のユニットテストは作成しない（既存パターンに従う）。代わりに以下の動作確認を実施する:

| 確認項目 | 内容 |
|---------|------|
| refresh スクリプト実行 | `refresh_swedish_eval_data.ps1` が正常終了しフルTSV が生成される |
| TSV 行数確認 | ipa-dict: 約21,107件、WikiPron: 約4,631件 |
| TSV フォーマット | タブ区切り、surface\tipa の2カラム |
| SwedishEval 実行 | `dotnet run` が正常終了し PER レポートを出力する |
| プロファイル別実行 | base / allophones / no_exceptions 各プロファイルが正常動作 |
| 閾値判定 | PER が閾値内であれば終了コード0、超過であれば非ゼロ |

### E2Eテスト

| テスト | 検証内容 |
|--------|---------|
| run_swedish_full_evaluation.ps1 実行 | 全データセット × 全プロファイルの評価が完了する |
| 閾値内確認 | 全 PER が swedish_eval_thresholds.json の閾値内 |

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **ipa-dict の声調アクセントマーク ²**: スウェーデン語の ipa-dict は accent 2 を `²` マークで表記する。base プロファイルの比較時にはこのマークを除去する必要があるが、allophones プロファイルでは保持する可能性がある。Sw3 で実装したアクセント出力形式との整合性を確認すること
2. **WikiPron データの品質**: WikiPron のスウェーデン語データはユーザー寄稿であり、声調表記やそり舌音表記にばらつきがある。フィルタリングで明らかに不正なエントリを除外するロジックが必要
3. **IPA 正規化の統一**: リファレンスデータと SwedishG2PEngine の出力で IPA 表記が微妙に異なる可能性がある（例: 長音記号の位置、合字の使用）。比較前に統一的な正規化を適用すること
4. **フルデータセット TSV のファイル名**: SwedishDatasetEvaluationTests（SW4-005）で参照するファイル名と完全に一致させること（MEMORY に記載のポルトガル語知見と同様）
5. **PowerShell スクリプトのクロスプラットフォーム**: CI は GitHub Actions (ubuntu-latest) で実行されるため、PowerShell Core 互換であることを確認

### レビューチェックリスト

- [ ] DotNetG2P.SwedishEval.csproj が net8.0 ターゲットで、IsPackable=false である
- [ ] Program.cs が既存の PortugueseEval/FrenchEval/SpanishEval と同一パターンに従っている
- [ ] PER 計算ロジックが Levenshtein 距離ベースで正しい
- [ ] refresh スクリプトのダウンロード URL が正しい
- [ ] refresh スクリプトの出力ファイル名が SwedishDatasetEvaluationTests の参照名と一致する
- [ ] 声調アクセントマーク `²` の取り扱いがプロファイル別に正しい
- [ ] WikiPron フィルタリングが不正エントリを除外する
- [ ] swedish_eval_thresholds.json の閾値がマイルストーン計画（付録A）の目標値と一致する（ipa-dict no_exceptions: 0.12、wikipron no_exceptions: 0.15）
- [ ] run スクリプトが閾値超過時に非ゼロ終了コードを返す
- [ ] PowerShell Core (pwsh) 互換である

## 6. ゼロから作り直すとしたら

既存の `tools/DotNetG2P.PortugueseEval/` をそのままコピーして Portuguese → Swedish に置換する。これが最も安全で効率的なアプローチ。

変更ポイントは以下に限定される:
1. `Program.cs`: エンジン初期化を `PortugueseG2PEngine` → `SwedishG2PEngine` に変更、IPA 正規化にスウェーデン語固有の処理（声調マーク `²` 除去オプション）を追加
2. `refresh_*.ps1`: ダウンロード URL をスウェーデン語データに変更、出力ファイル名を `sv_se` / `swe_latn` に変更
3. `run_*.ps1`: パス参照を Swedish に変更
4. `thresholds.json`: 閾値をスウェーデン語目標値に設定

PowerShell スクリプトは `refresh_portuguese_eval_data.ps1` / `run_portuguese_full_evaluation.ps1` のテンプレートから作成し、変数名・パス・URL のみ変更する。

## 7. 後続タスクへの連絡事項

- **SW4-004 へ**: `DotNetG2P.SwedishEval.csproj` を `DotNetG2P.slnx` の `/tools/` フォルダに追加すること。ci.yml のビルド対象にも含める
- **SW4-005 へ**: フルデータセット TSV のファイル名は以下の通り:
  - `tests/TestData/SwedishG2P/ipa_dict_sv_se_full.tsv`（21,107件）
  - `tests/TestData/SwedishG2P/wikipron_swe_latn_broad_filtered_full.tsv`（4,631件）
  - SwedishDatasetEvaluationTests でこれらのファイル名を正確に参照すること
- **閾値の最終調整**: Sw4 フル評価の結果に応じて `swedish_eval_thresholds.json` の閾値を微調整する可能性がある。SW4-005 の評価結果を受けて最終確定する
