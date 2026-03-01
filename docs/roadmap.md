# DotNetG2P ロードマップ

## 概要

OpenJTalk互換の日本語G2P（書記素→音素変換）パイプラインをC#/.NETで再実装する。
jpreprocess (Rust) の設計をベースに、6つのマイルストーンで段階的に完成させる。

---

## マイルストーン一覧

| MS | 名称 | 完了条件 | 依存 | 状態 |
|----|------|---------|------|------|
| **M1** | 最小動作プロトタイプ | `g2p("こんにちは")` → `"k o N n i ch i w a"` が動作 | - | **完了** |
| **M2** | NJD処理パイプライン完成 | pyopenjtalk と同等のNJD処理6段階が動作 | M1 | **完了** |
| **M3** | 出力形式の充実 | カタカナ/韻律記号/AccentPhrase/フルコンテキストラベル出力 | M2 | **完了** |
| **M4** | テスト・品質保証 | jpreprocess/pyopenjtalkとの比較テスト合格 | M2 | 未着手 |
| **M5** | パッケージング | NuGet/UPMパッケージとして配布可能 | M3, M4 | 未着手 |
| **M6** | 独自MeCabエンジン | NMeCab (LGPL) 依存排除、完全BSD化 | M5 | 未着手 |

---

## M1: 最小動作プロトタイプ **[完了]**

**ゴール**: テキスト入力 → NMeCab形態素解析 → カタカナ読み取得 → 音素列出力

### タスク

| # | タスク | 状態 | 参考実装 |
|---|--------|------|---------|
| 1.1 | ソリューション・プロジェクト作成（.slnx形式） | **完了** | - |
| 1.2 | Phoneme enum定義（Consonant 35種 + Vowel 10種） | **完了** | jpreprocess `pronunciation/phoneme.rs` |
| 1.3 | MoraKind enum定義（~165種） | **完了** | jpreprocess `pronunciation/mora_enum.rs` |
| 1.4 | Mora構造体（readonly struct） | **完了** | jpreprocess `pronunciation/mora.rs` |
| 1.5 | POS enum定義（POSType 14種 + ネスト構造） | **完了** | jpreprocess `pos/` |
| 1.6 | Pronunciation構造体 | **完了** | jpreprocess `pronunciation/mod.rs` |
| 1.7 | WordDetails構造体 + WordEntry | **完了** | jpreprocess `word_details.rs` |
| 1.8 | NjdNode構造体 + AccentPhrase | **完了** | jpreprocess `njd/node.rs` |
| 1.9 | ITokenizer / IToken インターフェース（15フィールド対応） | **完了** | docs/design.md |
| 1.10 | NMeCabTokenizer実装（LibNMeCab 0.10.2） | **完了** | research/06, research/14 |
| 1.11 | MoraMapping（162種カタカナ⇔音素マッピング） | **完了** | VOICEVOX `mora_mapping.py` |
| 1.12 | SetPronunciation（最小版） | **完了** | jpreprocess `pronunciation.rs` |
| 1.13 | G2PEngine（ToPhonemes() + ToKana()） | **完了** | - |

### 実装統計

- **ファイル数**: 22ファイル
- **コード行数**: 約2,758行
- **ソリューション形式**: .slnx（.NET 10新形式）

### 検証結果（naist-jdic辞書使用）

```
入力: こんにちは
カナ: コンニチワ
音素: k o N n i ch i w a

入力: 今日は良い天気です
カナ: キョーワヨイテンキデス
音素: ky o - w a y o i t e N k i d e s u

入力: 東京タワーに行きたい
カナ: トーキョータワーニイキタイ
音素: t o - ky o - t a w a - n i i k i t a i

入力: 音声合成の研究
カナ: オンセーゴーセーノケンキュー
音素: o N s e - g o - s e - n o k e N ky u -
```

### 既知の制限事項（M2以降で対応）

- NJD処理が SetPronunciation のみ（数字読み、アクセント句結合、無声音化は未実装）
- 長音「ー」は音素上「-」として出力（将来的にモーラ対応を改善）
- 一部トークンタイプで発音が欠落する場合がある

---

## M2: NJD処理パイプライン完成 **[完了]**

**ゴール**: 6段階のNJD処理が正しく動作し、数字・アクセント・無声音化が処理される

### タスク

| # | タスク | 状態 | 実装行数 | ファイル |
|---|--------|------|---------|---------|
| 2.1 | TextNormalizer | **完了** | 278行 | TextNormalization/TextNormalizer.cs |
| 2.2 | DigitSequence（数字列検出） | **完了** | 750行 | NJD/DigitSequence.cs |
| 2.3a | SetDigit LUTテーブル | **完了** | 619行 | NJD/DigitLut.cs |
| 2.3b | SetDigit メインロジック | **完了** | 637行 | NJD/SetDigit.cs |
| 2.4 | SetAccentPhrase（18ルール） | **完了** | 237行 | NJD/SetAccentPhrase.cs |
| 2.5 | SetAccentType（C1-C5, F1-F5, P系列） | **完了** | 475行 | NJD/SetAccentType.cs |
| 2.6 | SetUnvoicedVowel（6ルール） | **完了** | 389行 | NJD/SetUnvoicedVowel.cs |
| 2.7 | SetPronunciation完全版（5段階処理） | **完了** | 311行 | NJD/SetPronunciation.cs |
| 2.8 | NjdNode拡張（MergeFrom等） | **完了** | 183行 | Models/NjdNode.cs |
| 2.9 | G2PEngine パイプライン統合 | **完了** | 222行 | G2PEngine.cs + G2POptions.cs |

### 実装統計

- **M2新規コード**: 約3,900行（NJD 7ファイル + TextNormalizer + G2POptions）
- **M1からの変更**: NjdNode.cs拡張、Pronunciation.cs追加メソッド、SetPronunciation.cs全面改修
- **プロジェクト全体**: 約6,620行

### 検証結果（naist-jdic辞書使用）

```
入力: こんにちは
音素: k o N n i ch i w a

入力: 東京タワーに行きたい
音素: t o - ky o - t a w a - n i i k I t a i
（注: k I = 無声音化された「き」のi）

入力: ３個のりんご
音素: s a N k o n o r i N g o

入力: すきです（無声音化ON）
音素: s U k i
（注: s U = 無声音化された「す」のu）

入力: すきです（無声音化OFF）
音素: s u k i
```

### 既知の残課題（M4テストフェーズで対応）

- 「です」「ます」が一部ケースで句点扱いになる
- 数字の位取り読み（百/千/万）が一部不正確（"１２３円" → "ニサンエン"、正しくは"ヒャクニジュウサンエン"）
- nullable警告15件（機能には影響なし）

---

## M3: 出力形式の充実 **[完了]**

**ゴール**: 5種類の出力形式を提供

### タスク

| # | タスク | 難易度 | 実装行数 | ファイル | 状態 |
|---|--------|--------|---------|---------|------|
| 3.1 | ToKana()（カタカナ出力） | 低 | M1実装済 | G2PEngine.cs | **完了** |
| 3.2 | AccentPhrase構造体出力（VOICEVOX互換） | 中 | ~160行 | PhonemeConverter/AccentPhraseConverter.cs | **完了** |
| 3.3 | ProsodyExtractor（韻律記号付き出力） | 中 | ~132行 | PhonemeConverter/ProsodyExtractor.cs | **完了** |
| 3.4 | JPCommon: Utterance/BreathGroup階層構築 | **高** | ~621行 | JPCommon/Models.cs, JPCommon/JPCommonBuilder.cs | **完了** |
| 3.5 | JPCommon: フルコンテキストラベル生成 | **高** | ~552行 | JPCommon/FullContextLabel.cs, JPCommon/WordAttr.cs | **完了** |
| 3.6 | G2PEngineに全出力メソッド追加 | 低 | +51行 | G2PEngine.cs | **完了** |

### 実装統計

- **M3新規コード**: 約1,465行（JPCommon 4ファイル + PhonemeConverter 2ファイル + G2PEngine変更）
- **M1からの累計変更**: NjdNode.cs拡張、G2PEngine.csに3メソッド追加
- **テスト**: 310件成功（M3で約120件のテストを追加）
- **プロジェクト全体**: 約10,100行

### 検証結果（naist-jdic辞書使用）

```csharp
// ToProsody()
engine.ToProsody("こんにちは")
// => "^ k o [ N _ n i _ ch i _ w a $"

// ToAccentPhrases()
engine.ToAccentPhrases("今日は天気がいいですね")
// => [AccentPhrase{Moras=[...], Accent=1}, ...]

// ToFullContextLabels()
engine.ToFullContextLabels("盆栽")
// => ["xx^xx-sil+b=o/A:xx+xx+xx/B:xx-xx_xx/C:xx_xx+xx/D:...", ...]
```

### 出力形式一覧

| メソッド | 出力例 | 用途 |
|---------|--------|------|
| `ToPhonemes()` | `"k o N n i ch i w a"` | 基本音素列（M1で実装済み） |
| `ToKana()` | `"コンニチワ"` | カタカナ読み |
| `ToProsody()` | `"^ k o [ N n i ch i w a $"` | ESPnet韻律記号付き |
| `ToAccentPhrases()` | `[AccentPhrase{...}]` | VOICEVOX互換構造体 |
| `ToFullContextLabels()` | `["xx^xx-k+o=N/A:..."]` | HTSフルコンテキストラベル |

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
M1 最小動作プロトタイプ [完了]
├─ 1.2-1.5 データ構造 enum/struct ──────┐
├─ 1.9-1.10 ITokenizer + NMeCab ────────┤
├─ 1.11 MoraMapping ────────────────────┤
└─ 1.12-1.13 SetPronunciation + Engine ─┘→ ✅ "こんにちは" → "k o N n i ch i w a"
    │
    ▼
M2 NJD処理パイプライン [完了]
├─ 2.1 TextNormalizer ──────────────────┐
├─ 2.2-2.3 Digit処理 ──────────────────┤
├─ 2.4 SetAccentPhrase ────────────────┤
├─ 2.5 SetAccentType ──────────────────┤
├─ 2.6 SetUnvoicedVowel ──────────────┤
└─ 2.7-2.9 パイプライン統合 ────────────┘→ ✅ 完全なNJD処理
    │
    ├──────────────────┐
    ▼                  ▼
M3 出力形式 [完了]   M4 テスト
├─ AccentPhrase     ├─ 単体テスト
├─ ToProsody        ├─ pyopenjtalk比較
├─ JPCommon階層     └─ エッジケース
└─ FullContext ──────────→ ✅ 全5出力形式
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
