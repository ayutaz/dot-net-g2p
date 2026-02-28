# DotNetG2P ロードマップ

## 概要

OpenJTalk互換の日本語G2P（書記素→音素変換）パイプラインをC#/.NETで再実装する。
jpreprocess (Rust) の設計をベースに、6つのマイルストーンで段階的に完成させる。

---

## マイルストーン一覧

| MS | 名称 | 完了条件 | 依存 |
|----|------|---------|------|
| **M1** | 最小動作プロトタイプ | `g2p("こんにちは")` → `"k o N n i ch i w a"` が動作 | - |
| **M2** | NJD処理パイプライン完成 | pyopenjtalk と同等のNJD処理6段階が動作 | M1 |
| **M3** | 出力形式の充実 | カタカナ/韻律記号/AccentPhrase/フルコンテキストラベル出力 | M2 |
| **M4** | テスト・品質保証 | jpreprocess/pyopenjtalkとの比較テスト合格 | M2 |
| **M5** | パッケージング | NuGet/UPMパッケージとして配布可能 | M3, M4 |
| **M6** | 独自MeCabエンジン | NMeCab (LGPL) 依存排除、完全BSD化 | M5 |

---

## M1: 最小動作プロトタイプ

**ゴール**: テキスト入力 → NMeCab形態素解析 → カタカナ読み取得 → 音素列出力

### タスク

| # | タスク | 難易度 | 参考実装 | 依存 |
|---|--------|--------|---------|------|
| 1.1 | ソリューション・プロジェクト作成 | 低 | docs/design.md のプロジェクト構成 | - |
| 1.2 | Phoneme enum定義 | 低 | jpreprocess `pronunciation/phoneme.rs` (13KB) | - |
| 1.3 | MoraKind enum定義（~150種） | 中 | jpreprocess `pronunciation/mora_enum.rs` (4KB) | - |
| 1.4 | Mora構造体 | 低 | jpreprocess `pronunciation/mora.rs` (3KB) | 1.2, 1.3 |
| 1.5 | POS enum定義（ネスト構造） | 中 | jpreprocess `pos/` (6KB + サブファイル群 ~20KB) | - |
| 1.6 | Pronunciation構造体 | 低 | jpreprocess `pronunciation/mod.rs` (11KB) | 1.4 |
| 1.7 | WordDetails構造体 | 低 | jpreprocess `word_details.rs` (4KB) | 1.5, 1.6 |
| 1.8 | NjdNode構造体 | 中 | jpreprocess `njd/node.rs` (5KB) | 1.7 |
| 1.9 | ITokenizer / IToken インターフェース | 低 | docs/design.md のAPI定義 | 1.5 |
| 1.10 | NMeCabTokenizer実装 | 中 | research/06, research/14 (naist-jdic 15フィールドパース) | 1.9 |
| 1.11 | MoraMapping（247種カタカナ⇔音素） | 低 | jpreprocess `mora_dict.rs` (16KB), VOICEVOX `mora_mapping.py` | 1.3, 1.4 |
| 1.12 | SetPronunciation（最小版） | 中 | jpreprocess `open_jtalk/pronunciation.rs` (4KB) | 1.8, 1.11 |
| 1.13 | G2PEngine（最小版: ToPhonemes()のみ） | 中 | - | 1.10, 1.12 |

### 並列実装可能なグループ

```
グループA（データ構造）: 1.2, 1.3, 1.5, 1.8 → 並列可
グループB（インフラ）:    1.9, 1.10, 1.11     → グループA完了後に並列可
グループC（パイプライン）: 1.12, 1.13           → グループB完了後
```

### クリティカルパス

```
1.1 → 1.3 → 1.4 → 1.6 → 1.7 → 1.8 → 1.12 → 1.13
                                         ↑
                            1.9 → 1.10 ──┘
                            1.11 ────────┘
```

### 検証方法

```csharp
using var tokenizer = new NMeCabTokenizer("path/to/naist-jdic");
using var engine = new G2PEngine(tokenizer);
Debug.Assert(engine.ToPhonemes("こんにちは") == "k o N n i ch i w a");
```

---

## M2: NJD処理パイプライン完成

**ゴール**: 6段階のNJD処理が正しく動作し、数字・アクセント・無声音化が処理される

### タスク（複雑度降順で記載）

| # | タスク | 難易度 | 推定規模 | 参考実装 (jpreprocess) | 依存 |
|---|--------|--------|---------|----------------------|------|
| 2.1 | TextNormalizer | 中 | ~300行 | `normalize_text.rs` (8KB) | - |
| 2.2 | DigitSequence（数字列検出） | 中 | ~400行 | `digit_sequence/` (14KB, 3ファイル) | M1 |
| 2.3 | **SetDigit（数字読み変換）** | **高** | **~1500行** | `digit/` (35KB, 7ファイル) ※LUTテーブル含む | 2.2 |
| 2.4 | **SetAccentPhrase（18ルール）** | **高** | **~600行** | `accent_phrase.rs` (5KB) + research/01ルール表 | M1 |
| 2.5 | **SetAccentType（C1-C5, F1-F5, P系列）** | **高** | **~800行** | `accent_type.rs` (6KB) + research/11計算式 | 2.4 |
| 2.6 | SetUnvoicedVowel（6ルール） | 中 | ~200行 | `unvoiced_vowel.rs` (9KB) | 2.5 |
| 2.7 | SetPronunciation完全版 | 中 | ~400行 | `pronunciation.rs` (4KB) | M1 |
| 2.8 | G2PEngine パイプライン統合 | 中 | ~200行 | - | 2.1〜2.7 |

### NJDパイプライン実行順序（厳守）

```
テキスト
  → TextNormalizer (2.1)
  → ITokenizer.Tokenize()
  → NjdNode構築
  → SetPronunciation (2.7)     ← 1. 発音生成
  → DigitSequence (2.2)        ← 2a. 数字列検出
  → SetDigit (2.3)             ← 2b. 数字読み変換
  → SetAccentPhrase (2.4)      ← 3. アクセント句結合
  → SetAccentType (2.5)        ← 4. アクセント結合型
  → SetUnvoicedVowel (2.6)     ← 5. 無声音化
```

### SetDigitの実装詳細（最大タスク）

| サブタスク | 内容 | 参考ファイル |
|-----------|------|------------|
| 基数読みテーブル | 0-9の読み（イチ/ニ/サン...） | `lut/numeral.rs` (1KB) |
| 位取りテーブル | 十/百/千/万/億/兆 | `lut/numeral.rs` |
| 助数詞クラス1 | 年/人/時間/日 等11分類 | `lut/class1.rs` (7KB) |
| 助数詞クラス2 | 分/本/匹 等5分類 | `lut/class2.rs` (2KB) |
| 助数詞クラス3 | 60+エントリ | `lut/class3.rs` (2KB) |
| 音便変化 | サンビャク/ロッピャク等 | `lut/others.rs` (3KB) |
| 日付特殊読み | 1日ツイタチ〜20日ハツカ | `mod.rs` 内 |
| 小数点処理 | 3.14→サンテンイチヨン | `mod.rs` 内 |

### 検証方法

```csharp
// 数字読み
Debug.Assert(engine.ToPhonemes("100円") contains "hy a k u e N");
// アクセント句
var phrases = engine.ToAccentPhrases("東京都港区");
Debug.Assert(phrases.Count >= 2);
// 無声音化
// 「き」のiが無声化されることを確認
```

---

## M3: 出力形式の充実

**ゴール**: 5種類の出力形式を提供

### タスク

| # | タスク | 難易度 | 推定規模 | 参考実装 | 依存 |
|---|--------|--------|---------|---------|------|
| 3.1 | ToKana()（カタカナ出力） | 低 | ~50行 | NjdNodeのpronunciation連結 | M2 |
| 3.2 | AccentPhrase構造体出力（VOICEVOX互換） | 中 | ~200行 | VOICEVOX AccentPhrase/Moraモデル | M2 |
| 3.3 | ProsodyExtractor（韻律記号付き出力） | 中 | ~300行 | piper-plus `japanese.py` (382行), uPiper `OpenJTalkPhonemizer.cs` | M2 |
| 3.4 | JPCommon: Utterance/BreathGroup階層構築 | **高** | ~800行 | jpreprocess `jpcommon/label/` (9KB) | M2 |
| 3.5 | JPCommon: フルコンテキストラベル生成 | **高** | ~1200行 | jpreprocess `jpcommon/feature/mod.rs` (31KB) ※最大ファイル | 3.4 |
| 3.6 | G2PEngineに全出力メソッド追加 | 低 | ~100行 | - | 3.1〜3.5 |

### 出力形式一覧

| メソッド | 出力例 | 用途 |
|---------|--------|------|
| `ToPhonemes()` | `"k o N n i ch i w a"` | 基本音素列（M1で実装済み） |
| `ToKana()` | `"コンニチワ"` | カタカナ読み |
| `ToProsody()` | `"^ k o [ N n i ch i w a $"` | ESPnet韻律記号付き |
| `ToAccentPhrases()` | `[AccentPhrase{...}]` | VOICEVOX互換構造体 |
| `ToFullContextLabels()` | `["xx^xx-k+o=N/A:..."]` | HTSフルコンテキストラベル |

### 検証方法

```csharp
// 韻律記号
var prosody = engine.ToProsody("こんにちは");
Debug.Assert(prosody.StartsWith("^") && prosody.EndsWith("$"));

// VOICEVOX互換
var phrases = engine.ToAccentPhrases("今日は天気がいいですね");
Debug.Assert(phrases[0].Moras.Count > 0);
Debug.Assert(phrases[0].Accent > 0);

// フルコンテキストラベル
var labels = engine.ToFullContextLabels("こんにちは");
Debug.Assert(labels[0].Contains("/A:"));
```

---

## M4: テスト・品質保証

**ゴール**: jpreprocess/pyopenjtalkとの比較テストに合格し、エッジケースを網羅

### タスク

| # | タスク | 難易度 | 参考 | 依存 |
|---|--------|--------|------|------|
| 4.1 | pyopenjtalkテストデータ生成（Pythonスクリプト） | 低 | research/02 | - |
| 4.2 | NJD各処理の単体テスト | 中 | jpreprocess テスト (~30件) | M2 |
| 4.3 | MoraMapping全パターンテスト | 低 | 247マッピングの全数検証 | M1 |
| 4.4 | piper-plusテストケース移植 | 中 | `test_phonemize.py` (~52件) | M2 |
| 4.5 | pyopenjtalk出力との統合比較テスト | 中 | 4.1で生成したデータ | M3 |
| 4.6 | エッジケーステスト | 中 | 記号/英字/空文字/長文/混在スクリプト | M2 |
| 4.7 | 数字読みテスト（網羅的） | 中 | 日付/電話番号/金額/小数 | M2 |

### テストデータカテゴリ

| カテゴリ | テストケース例 | 検証対象 |
|---------|-------------|---------|
| 基本 | こんにちは、おはようございます | SetPronunciation |
| 数字 | 100円、2024年3月15日、3.14 | SetDigit |
| 漢字 | 東京都港区、日本語 | 辞書読み |
| カタカナ | コンピュータ、プログラミング | MoraMapping |
| 混在 | 今日はDocker入門 | テキスト正規化 + 辞書 |
| 記号 | こんにちは！、えっ？ | 記号処理 |
| 長文 | 50文字以上のテキスト | パイプライン安定性 |
| エッジ | 空文字列、数字のみ、記号のみ | エラーハンドリング |

---

## M5: パッケージング

**ゴール**: NuGetとUPMの両方で配布可能

### タスク

| # | タスク | 難易度 | 参考 | 依存 |
|---|--------|--------|------|------|
| 5.1 | Directory.Build.props設定 | 低 | docs/design.md | M4 |
| 5.2 | DotNetG2P.NetCore.csproj（Compile Include方式） | 中 | UniTask方式 | M4 |
| 5.3 | DotNetG2P.NMeCab.csproj | 低 | - | M4 |
| 5.4 | NuGetパッケージ設定・テスト | 中 | UniTask `Directory.Build.props` | 5.2, 5.3 |
| 5.5 | UPM package.json | 低 | UniTask `package.json` | M4 |
| 5.6 | asmdef作成（Runtime/Editor/Tests） | 低 | docs/design.md | 5.5 |
| 5.7 | naist-jdic辞書バンドル戦略実装 | 中 | StreamingAssets + ダウンローダー | 5.5 |
| 5.8 | GitHub Actions CI/CD | 中 | UniTask `build-release.yaml` | 5.4, 5.6 |
| 5.9 | コンソールサンプル | 低 | - | 5.4 |
| 5.10 | README・APIドキュメント | 低 | - | 5.9 |

### パッケージ構成

| パッケージ | ライセンス | 配布先 |
|-----------|-----------|-------|
| `DotNetG2P` (NuGet) | MIT/BSD | nuget.org |
| `DotNetG2P.NMeCab` (NuGet) | LGPL | nuget.org |
| `com.dotnetg2p.core` (UPM) | MIT/BSD | GitHub URL / OpenUPM |

### 検証方法

```bash
# NuGet
dotnet new console
dotnet add package DotNetG2P
dotnet add package DotNetG2P.NMeCab
dotnet run  # サンプルコードが動作

# Unity
# Package ManagerからGit URLで追加
# StreamingAssetsに辞書配置
# IL2CPPビルドでテスト
```

---

## M6: 独自MeCabエンジン（LGPL依存排除）

**ゴール**: NMeCab依存を完全排除し、全コンポーネントをBSD/MITライセンスにする

### タスク

| # | タスク | 難易度 | 推定規模 | 参考 | 依存 |
|---|--------|--------|---------|------|------|
| 6.1 | ダブル配列Trie（DARTS）実装 | **高** | ~1500行 | MeCab DARTS実装, lindera (Rust) | M5 |
| 6.2 | MeCabバイナリ辞書読み込み | **高** | ~800行 | research/14 (sys.dic仕様), jpreprocess-dictionary | 6.1 |
| 6.3 | ラティス構築 | **高** | ~600行 | MeCab lattice.cpp | 6.2 |
| 6.4 | ビタビデコーディング | **高** | ~400行 | MeCab viterbi.cpp | 6.3 |
| 6.5 | matrix.bin読み込み（遷移コスト行列） | 中 | ~200行 | research/14 | 6.2 |
| 6.6 | char.bin + unk.dic（未知語処理） | **高** | ~500行 | MeCab char_property.cpp | 6.2 |
| 6.7 | ITokenizer実装（MeCabTokenizer） | 中 | ~300行 | M1のNMeCabTokenizerを参考 | 6.4, 6.5, 6.6 |
| 6.8 | NMeCab完全排除・テスト | 中 | ~200行 | - | 6.7 |

### リスク

| リスク | 影響 | 対策 |
|--------|------|------|
| 実装工数が大きい（数人月規模） | スケジュール遅延 | M5までをまず完成させ、M6は独立フェーズ |
| MeCab完全互換の品質担保が困難 | 精度低下 | NMeCab版との比較テストを継続実行 |
| 辞書バイナリ仕様の理解不足 | 実装ブロック | research/14の仕様書 + NMeCabソースコード参照 |

### 検証方法

```csharp
// NMeCab版と独自実装版の出力一致を検証
var nmecab = new NMeCabTokenizer("path/to/dict");
var custom = new MeCabTokenizer("path/to/dict");

foreach (var text in testTexts) {
    var expected = nmecab.Tokenize(text);
    var actual = custom.Tokenize(text);
    Assert.Equal(expected, actual);
}
```

---

## 全体スケジュール概観

```
M1 最小動作プロトタイプ
├─ 1.2-1.5 データ構造 enum/struct ──────┐
├─ 1.9-1.10 ITokenizer + NMeCab ────────┤
├─ 1.11 MoraMapping ────────────────────┤
└─ 1.12-1.13 SetPronunciation + Engine ─┘→ ✅ "こんにちは" → 音素
    │
    ▼
M2 NJD処理パイプライン
├─ 2.1 TextNormalizer ──────────────────┐
├─ 2.2-2.3 Digit処理 ──────────────────┤
├─ 2.4 SetAccentPhrase ────────────────┤
├─ 2.5 SetAccentType ──────────────────┤
├─ 2.6 SetUnvoicedVowel ──────────────┤
└─ 2.7-2.8 パイプライン統合 ────────────┘→ ✅ 完全なNJD処理
    │
    ├──────────────────┐
    ▼                  ▼
M3 出力形式         M4 テスト
├─ ToKana           ├─ 単体テスト
├─ ToProsody        ├─ pyopenjtalk比較
├─ AccentPhrase     └─ エッジケース
└─ FullContext          │
    │                   │
    └────────┬──────────┘
             ▼
M5 パッケージング
├─ NuGet設定
├─ UPM設定
├─ 辞書バンドル
└─ CI/CD
    │
    ▼
M6 独自MeCabエンジン
├─ ダブル配列Trie
├─ ラティス+ビタビ
├─ 辞書読み込み
└─ LGPL依存排除 → ✅ 完全BSD/MIT
```

---

## コード規模見積もり（jpreprocess対応表）

| dot-net-g2p モジュール | jpreprocess crate | Rustサイズ | C#推定行数 |
|----------------------|-------------------|-----------|-----------|
| Models/ | jpreprocess-core | 102KB (~3,000行) | ~3,500行 |
| NJD/ | jpreprocess-njd | 82KB (~2,500行) | ~4,000行 |
| JPCommon/ | jpreprocess-jpcommon | 53KB (~1,600行) | ~2,000行 |
| PhonemeConverter/ | (core内) | ~30KB | ~500行 |
| TextNormalization/ | (jpreprocess内) | ~8KB | ~300行 |
| Tokenizer/ (インターフェース) | - | - | ~100行 |
| NMeCabTokenizer | - | - | ~300行 |
| G2PEngine | (jpreprocess/lib.rs) | ~10KB | ~400行 |
| **合計 (M1-M3)** | | **~285KB** | **~11,100行** |

---

## ライセンスマイルストーン

| フェーズ | DotNetG2P.Core | 形態素解析 | Unity Asset Store |
|---------|----------------|-----------|-------------------|
| M1-M5 | BSD/MIT | NMeCab (LGPL) → 別パッケージ | Core のみ配布可 |
| M6完了 | BSD/MIT | 自前実装 (BSD) | **全パッケージ配布可** |

---

## リスク一覧

| リスク | 影響度 | 発生MS | 対策 |
|--------|--------|--------|------|
| SetDigitの複雑さ過小評価 | 高 | M2 | jpreprocess LUTテーブルを忠実に移植。テスト駆動で進める |
| SetAccentType計算式の誤り | 高 | M2 | jpreprocess + OpenJTalk両方を照合。pyopenjtalk出力と比較 |
| NMeCab naist-jdic 15フィールドパース | 中 | M1 | research/14の仕様書を参照。フィールド14-15のアクセント情報パースを早期検証 |
| Unity IL2CPP非互換 | 中 | M5 | M1段階からAOT安全設計。IL2CPPビルドテストをCIに組み込む |
| 辞書サイズ (80MB) | 中 | M5 | StreamingAssets配置。将来的に圧縮/分割ロード検討 |
| JPCommon feature/mod.rs の複雑さ | 高 | M3 | jpreprocessのテストケース6件を先に移植し、テスト駆動で実装 |
| 独自MeCab実装の品質 | 高 | M6 | NMeCab版との出力一致テストを網羅的に実行 |
