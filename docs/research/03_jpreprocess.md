# jpreprocess 調査レポート

## 1. 概要

**jpreprocess** は、OpenJTalkの前処理部分（HTS Engine以外）をRustで書き直したプロジェクトである。

- **リポジトリ**: https://github.com/jpreprocess/jpreprocess
- **言語**: Rust（最低バージョン: 1.88.0）
- **ライセンス**: BSD-3-Clause
- **最新バージョン**: v0.13.2（2025年10月リリース）
- **GitHub Star数**: 52
- **目的**: 日本語テキストを解析し、フルコンテキストラベルを生成する（TTS前処理）

### 方針
- OpenJTalkの構造をそのまま移すのではなく、読みやすく書きやすい構造に再設計
- 独自の辞書形式により辞書ファイルサイズを削減しつつ、従来形式もサポート
- 一部のバグと思われる機能を除き、OpenJTalkと同じフルコンテキストラベル出力を得ることが可能
- HTS Engineは扱わない（別プロジェクト [jbonsai](https://github.com/jpreprocess/jbonsai) で対応）

---

## 2. ソースコード構造

### ワークスペース構成（Cargo workspace）

```
jpreprocess/
├── crates/
│   ├── jpreprocess/          # メインクレート（エントリーポイント）
│   ├── jpreprocess-core/     # コアデータ構造（音素、品詞、発音等）
│   ├── jpreprocess-dictionary/ # 辞書の生成・読み込み
│   ├── jpreprocess-jpcommon/  # JPCommon（フルコンテキストラベル生成）
│   ├── jpreprocess-naist-jdic/ # naist-jdic辞書の変換・組み込み
│   ├── jpreprocess-njd/       # NJD処理（日本語ルール適用）
│   └── jpreprocess-window/    # mutableウィンドウイテレータ
├── bindings/
│   └── python/               # Pythonバインディング
├── examples/
└── tests/
```

### 各クレートの役割と依存関係

```
jpreprocess（メイン）
  ├── jpreprocess-core（コアデータ構造）
  ├── jpreprocess-dictionary（辞書管理）
  │   └── lindera（形態素解析エンジン）
  ├── jpreprocess-njd（NJD処理）
  │   ├── jpreprocess-core
  │   └── jpreprocess-window（ウィンドウイテレータ）
  ├── jpreprocess-jpcommon（フルコンテキストラベル生成）
  │   ├── jpreprocess-core
  │   ├── jpreprocess-njd
  │   └── jlabel（ラベル構造体）
  └── jpreprocess-naist-jdic（辞書組み込み、オプション）
```

---

## 3. OpenJTalkのどの部分を再実装しているか

### 再実装されている部分

| OpenJTalkコンポーネント | jpreprocess対応クレート | 状態 |
|---|---|---|
| テキスト正規化（全角/半角変換等） | `jpreprocess` (normalize_text.rs) | 完全再実装 |
| MeCab形態素解析 | `jpreprocess-dictionary` + Lindera | Linderaに委譲 |
| NJD（NJDNode構造体） | `jpreprocess-njd` (node.rs) | 完全再実装 |
| njd_set_pronunciation | `jpreprocess-njd::open_jtalk::pronunciation` | 完全再実装 |
| njd_digit_sequence | `jpreprocess-njd::open_jtalk::digit_sequence` | 完全再実装 |
| njd_set_digit | `jpreprocess-njd::open_jtalk::digit` | 完全再実装 |
| njd_set_accent_phrase | `jpreprocess-njd::open_jtalk::accent_phrase` | 完全再実装 |
| njd_set_accent_type | `jpreprocess-njd::open_jtalk::accent_type` | 完全再実装 |
| njd_set_unvoiced_vowel | `jpreprocess-njd::open_jtalk::unvoiced_vowel` | 完全再実装 |
| njd_set_long_vowel | なし | **非推奨・未実装**（OpenJTalkでもコメントアウト） |
| JPCommon（フルコンテキストラベル生成） | `jpreprocess-jpcommon` | 完全再実装 |
| naist-jdic辞書 | `jpreprocess-naist-jdic` | 独自形式で再構築 |

### 再実装されていない部分

| コンポーネント | 対応 |
|---|---|
| HTS Engine | 別プロジェクト [jbonsai](https://github.com/jpreprocess/jbonsai) |
| MeCab本体 | Lindera（Rust製形態素解析エンジン）に委譲 |

---

## 4. アーキテクチャ設計

### 4.1 処理パイプライン

```
入力テキスト
    │
    ▼
normalize_text_for_naist_jdic()  ← テキスト正規化
    │
    ▼
Lindera.tokenize()               ← 形態素解析
    │
    ▼
NJD::from_tokens()               ← トークン→NJDNode変換
    │
    ▼
NJD::preprocess()                ← NJD処理（以下6段階）
    │  ├── njd_set_pronunciation()     1. 発音設定
    │  ├── njd_digit_sequence()        2. 数字列処理
    │  ├── njd_set_digit()             3. 数字読み変換
    │  ├── njd_set_accent_phrase()     4. アクセント句設定
    │  ├── njd_set_accent_type()       5. アクセント型設定
    │  └── njd_set_unvoiced_vowel()    6. 無声音化設定
    │
    ▼
Utterance::from(njd.nodes)       ← NJD→JPCommon変換
    │
    ▼
utterance_to_features()          ← フルコンテキストラベル生成
    │
    ▼
Vec<jlabel::Label>               ← 出力
```

### 4.2 主要データ構造

#### NJDNode（NJD単語ノード）
```rust
pub struct NJDNode {
    string: String,           // 表層形
    details: WordDetails,     // 品詞・発音等の詳細情報
}

pub struct WordDetails {
    pub pos: POS,             // 品詞（enum型で型安全）
    pub ctype: CType,         // 活用型
    pub cform: CForm,         // 活用形
    pub read: Option<String>, // 読み（カタカナ）
    pub pron: Pronunciation,  // 発音（モーラ列+アクセント）
    pub chain_rule: ChainRules, // アクセント結合規則
    pub chain_flag: Option<bool>, // 前のノードに結合するかどうか
}
```

#### Pronunciation（発音）
```rust
pub struct Pronunciation {
    pub moras: Cow<'static, [Mora]>,  // モーラ列
    pub accent: usize,                 // アクセント核位置
}

pub struct Mora {
    pub mora_enum: MoraEnum,  // モーラの種類（約150種類のenum）
    pub is_voiced: bool,       // 有声/無声フラグ
}
```

**設計ポイント**: OpenJTalkでは文字列で保持していた発音情報を、構造化されたenum型（`MoraEnum`）で保持。これにより型安全性が向上し、音素変換が高速かつ確実になっている。

#### POS（品詞 - Part of Speech）
```rust
pub enum POS {
    Meishi(Meishi),      // 名詞
    Doushi(Doushi),      // 動詞
    Keiyoushi(Keiyoushi), // 形容詞
    Joshi(Joshi),        // 助詞
    Jodoushi,            // 助動詞
    Fukushi(Fukushi),    // 副詞
    Setsuzokushi,        // 接続詞
    Rentaishi,           // 連体詞
    Kandoushi,           // 感動詞
    Settoushi(Settoushi), // 接頭詞
    Kigou(Kigou),        // 記号
    Filler,              // フィラー
    Others,              // その他
}
// 各品詞はさらにサブカテゴリを持つネストされたenum
pub enum Meishi {
    None,
    KoyuMeishi(KoyuMeishi), // 固有名詞
    Setsubi(Setsubi),       // 接尾
    FukushiKanou,           // 副詞可能
    KeiyoudoushiGokan,      // 形容動詞語幹
    // ...
}
```

**設計ポイント**: 品詞をネストされたenum（代数的データ型）で表現。OpenJTalkの文字列ベースの品詞判定をパターンマッチングで型安全に実現。

#### JPCommon階層構造
```
Utterance（発話）
  └── Vec<BreathGroup>（呼気グループ）
        └── Vec<AccentPhrase>（アクセント句）
              └── Vec<Word>（単語）
                    └── Pronunciation（発音・モーラ列）
```

#### フルコンテキストラベル生成
`jlabel`クレート（外部）で定義された`Label`構造体を生成する。音素列を5つのウィンドウ（p2, p1, c, n1, n2）でスライドさせ、各コンテキスト情報（A〜K）を付与。

### 4.3 Tokenizer抽象化

```rust
pub trait Tokenizer {
    fn tokenize<'a>(&'a self, text: &'a str) -> JPreprocessResult<Vec<impl 'a + Token>>;
}

pub trait Token {
    fn fetch(&mut self) -> JPreprocessResult<(&str, WordEntry)>;
}
```

形態素解析エンジンを`Tokenizer`/`Token`トレイトで抽象化し、Lindera以外のエンジンに差し替え可能。

### 4.4 ウィンドウイテレータ（jpreprocess-window）

NJD処理で前後のノードを参照しながら変更する必要があるため、最大5つの可変参照を同時に扱える独自のイテレータ`IterQuintMut`を実装。

```rust
pub enum Quintuple<T> {
    Single(T),
    Double(T, T),
    Triple(T, T, T),
    First(T, T, T, T),
    Full(T, T, T, T, T),    // 前2つ + 現在 + 後2つ
    ThreeLeft(T, T, T, T),
    TwoLeft(T, T, T),
    Last(T, T),
}
```

Rustの借用規則を満たしつつ複数の可変参照を扱うための設計パターン。C#では不要な設計（参照型ならそのまま変更可能）。

---

## 5. OpenJTalkとの互換性・差異

### 一致する出力
- フルコンテキストラベルの大部分はOpenJTalkと完全に一致
- 音素変換、アクセント句結合、無声音化のルールを忠実に再現

### 意図的な差異

1. **特殊助動詞の扱い**: OpenJTalkでは助動詞を「動詞」として扱う場合がある（アクセント結合規則の`動詞%F1`に助動詞がマッチ）が、jpreprocessでは助動詞は助動詞として扱う。テストコメントに「This is different from Open JTalk. Open JTalk treats "助動詞" as a match for "動詞%F1".」と明記。

2. **数字の読み方**: 紛らわしい2,2,3桁区切りの数字の読み方がOpenJTalkと異なる場合がある

3. **長音推定（njd_set_long_vowel）**: OpenJTalkでもコメントアウトされている機能であり、jpreprocessでは実装していない

4. **orig（原形）情報**: jpreprocessのNJDNodeはorig（原形）文字列を保持しない。`run_frontend()`の出力文字列はOpenJTalkと一致しない場合がある

### 独自拡張
- **通貨処理**: `contrib::currency`モジュールとして通貨記号の読み処理を追加（OpenJTalkにはない）
- **辞書形式**: 独自のバイナリ辞書形式をサポートし、辞書サイズを削減

---

## 6. 辞書フォーマットの扱い

### 入力辞書
- MeCab同様のCSV形式（naist-jdic）
- CSVの各行: `表層形,品詞,品詞細分類1,品詞細分類2,品詞細分類3,活用型,活用形,原形,読み,発音,アクセント核/モーラ数,アクセント結合規則,連接フラグ`

### WordEntry構造
```rust
pub enum WordEntry {
    Single(WordDetails),               // 通常の単語
    Multiple(Vec<(String, WordDetails)>), // 複合語（コロンで区切られた語）
}
```

辞書のCSVで読み・発音・アクセント情報にコロン（`:`）が含まれる場合、複数のサブワードに分割される。
例: `あーあ,感動詞,*,*,*,*,*,あー:あ,アー:ア,アー:ア,1/2:1/1,C1,` → 2つのNJDNodeに展開

### 辞書ビルドプロセス
1. CSVファイルからLindera用辞書を生成（`jpreprocess-dictionary`）
2. 文字列のパースを事前に行い、JPreprocess用辞書（バイナリ形式）を生成可能
3. 実行時に辞書形式を自動判別

### 辞書形式
- **従来形式（文字列辞書）**: すべての情報を文字列で保持（互換性重視）
- **JPreprocess辞書**: 事前にパース済みの構造化データ（高速・省メモリ）

### 辞書読み込み方法
```rust
// ファイルから読み込み
let system = SystemDictionaryConfig::File(path).load()?;

// ビルド時に組み込み（naist-jdic feature有効時）
let system = SystemDictionaryConfig::Bundled(JPreprocessDictionaryKind::NaistJdic).load()?;
```

---

## 7. 精度・パフォーマンス

### 精度
- OpenJTalkとの互換性を重視し、テストケースで厳密に出力を検証
- フルコンテキストラベルの各フィールド（A〜K）を1文字ずつ比較するテストが豊富
- 既知の差異（特殊助動詞、数字読み）以外はOpenJTalkと同一出力

### パフォーマンス
- 直接的なベンチマーク結果は公開されていないが、以下の設計要因から高速性が期待できる:
  - 文字列ベースの処理をenum型の構造化データに置き換え
  - Aho-Corasickアルゴリズム（`aho-corasick`クレート）による効率的なモーラ辞書検索
  - PHF（Perfect Hash Function、`phf`クレート）によるテキスト正規化テーブル
  - 辞書の事前パースによるランタイムの文字列解析の削減
  - Linderaの高速な形態素解析

### テスト
- 各モジュールに単体テストが充実
- 統合テスト（`tests/`ディレクトリ）でフルパイプラインを検証
- テストデータとして最小辞書（`min-dict`）を含む

---

## 8. jpreprocess-coreのAPI設計

### 主要な公開API

#### JPreprocess構造体
```rust
pub struct JPreprocess<T: Tokenizer> {
    tokenizer: T,
}

impl<T: Tokenizer> JPreprocess<T> {
    /// Tokenizerから生成
    pub fn from_tokenizer(tokenizer: T) -> Self;

    /// テキスト→NJD変換（前処理なし）
    pub fn text_to_njd(&self, text: &str) -> JPreprocessResult<NJD>;

    /// テキスト→前処理済みNJD文字列
    pub fn run_frontend(&self, text: &str) -> JPreprocessResult<Vec<String>>;

    /// NJD文字列→フルコンテキストラベル
    pub fn make_label(&self, njd_features: Vec<String>) -> Vec<jlabel::Label>;

    /// テキスト→フルコンテキストラベル（一括処理）
    pub fn extract_fullcontext(&self, text: &str) -> JPreprocessResult<Vec<jlabel::Label>>;
}

// DefaultTokenizer（Lindera）使用時
impl JPreprocess<DefaultTokenizer> {
    /// 辞書データからJPreprocessを生成
    pub fn with_dictionaries(dictionary: Dictionary, user_dictionary: Option<UserDictionary>) -> Self;
}
```

#### NJD構造体
```rust
pub struct NJD {
    pub nodes: Vec<NJDNode>,
}

impl NJD {
    /// トークンからNJDを生成
    pub fn from_tokens<T: Token>(tokens: impl IntoIterator<Item = T>) -> JPreprocessResult<Self>;

    /// NJD文字列から生成
    pub fn from_strings(njd_features: Vec<String>) -> Self;

    /// 前処理を一括実行
    pub fn preprocess(&mut self);

    /// 発音のないノードを除去
    pub fn remove_silent_node(&mut self);
}
```

#### フルコンテキストラベル生成
```rust
/// NJDNode列→フルコンテキストラベル
pub fn njdnodes_to_features(njd_nodes: &[NJDNode]) -> Vec<Label>;

/// Utterance→音素+コンテキストのVec
pub fn utterance_to_phoneme_vec(utterance: &Utterance) -> Vec<(String, FeatureBuilder)>;

/// 音素+コンテキスト→5音素窓付きラベル
pub fn overwrapping_phonemes(phoneme_vec: Vec<(String, FeatureBuilder)>) -> Vec<Label>;
```

---

## 9. C#実装への示唆

### 9.1 そのまま採用すべき設計パターン

#### (a) 品詞のenum型表現
jpreprocessの品詞をネストされたenumで表現する設計は優れている。C#ではdiscriminated unionがないが、以下の方法で実現可能:
- 基底クラス + 派生クラス（クラス階層）
- enumフラグ + switch式
- record型のパターンマッチング（C# 9.0+は.NET Standard 2.1ではC# 8.0まで）

**推奨**: C# enumに品詞大分類と品詞小分類を分けて定義し、`switch`式で分岐。

#### (b) 発音のモーラベース表現
文字列ではなく`MoraEnum`で構造化する設計は、音素変換の正確性と高速性に直結する。C#でも同様のenum定義が有効。

```csharp
public enum MoraKind { A, I, U, E, O, Ka, Ki, Ku, Ke, Ko, /* ... */ N, Xtsu, Long }

public struct Mora {
    public MoraKind Kind;
    public bool IsVoiced;
}

public class Pronunciation {
    public List<Mora> Moras;
    public int Accent;
}
```

#### (c) 処理パイプラインの分離
NJD処理の各ステップ（pronunciation → digit_sequence → digit → accent_phrase → accent_type → unvoiced_vowel）を独立したモジュールにする設計は、テスト容易性と保守性を高める。

#### (d) Tokenizer抽象化
C#でもインターフェースで抽象化すべき:
```csharp
public interface ITokenizer {
    IEnumerable<IToken> Tokenize(string text);
}
public interface IToken {
    string Surface { get; }
    WordEntry Entry { get; }
}
```

### 9.2 C#で簡略化できる部分

#### (a) ウィンドウイテレータ
Rustでは借用チェックのために`IterQuintMut`/`Quintuple`が必要だが、C#ではList<NJDNode>に対してインデックスアクセスで前後ノードを直接参照・変更できる。

```csharp
// C#ではシンプルにインデックスアクセス
for (int i = 0; i < nodes.Count; i++) {
    var prev = i > 0 ? nodes[i-1] : null;
    var current = nodes[i];
    var next = i < nodes.Count - 1 ? nodes[i+1] : null;
    // 処理...
}
```

#### (b) Cow<'static, [Mora]>
Rustの`Cow`（Clone on Write）は所有権管理のための型。C#ではList<Mora>で十分。

#### (c) WordEntryのSingle/Multiple区別
C#ではList<(string, WordDetails)>として統一し、要素数1の場合を通常ケースとして扱える。

### 9.3 特に参考になるロジック

#### (a) テキスト正規化（normalize_text.rs）
- 半角→全角変換のマッピングテーブル
- 半角カナの濁点・半濁点結合処理
- ASCII文字の全角変換（`char + 0xFEE0`）

C#実装:
```csharp
// ASCIIの全角変換はjpreprocessと同じロジックで実装可能
if (c > '\u0020' && c < '\u007F') {
    return (char)(c + 0xFEE0);
}
```

#### (b) アクセント句結合規則（accent_phrase.rs）
18のルールが明確にコード化されている。C#でもそのまま移植可能:
- Rule 01: デフォルトはくっつける
- Rule 02: 名詞の連続はくっつける
- Rule 08: 助詞・助動詞は前にくっつける
- Rule 14: 記号は単独のアクセント句に
- など

#### (c) 無声音化ルール（unvoiced_vowel.rs）
5つのルールが明確に実装されている:
1. 助動詞の「です」「ます」の「す」が無声化
2. 動詞・助動詞・助詞の「し」は無声化しやすい
3. 続けて無声化しない
4. アクセント核で無声化しない
5. 無声子音に囲まれた「i」「u」が無声化（例外あり）

#### (d) アクセント結合規則（accent_rule.rs, accent_type.rs）
- `ChainRules`構造体で品詞別のアクセント結合規則を管理
- `AccentType`（F1〜F5, C1〜C5, P1, P2, P6, P14）の計算ロジック
- 数字のアクセント計算（calc_digit_acc）

#### (e) 音素変換テーブル（phoneme.rs）
全モーラ→(子音, 母音)のマッピングテーブル。約150のMoraEnumエントリを子音・母音ペアに変換。C#でもDictionaryまたはswitch式で同様に実装可能。

### 9.4 辞書設計への示唆

- naist-jdic CSVのパースロジックをそのまま参考にできる
- WordEntry/WordDetailsのフィールド構成がC#実装の設計指針になる
- 辞書フィールド: 品詞(4分類), 活用型, 活用形, 原形, 読み, 発音, アクセント核/モーラ数, アクセント結合規則, 連接フラグ
- コロン区切りによる複合語の分割処理

---

## 10. 関連プロジェクト

| プロジェクト | 説明 |
|---|---|
| [jbonsai](https://github.com/jpreprocess/jbonsai) | HTS EngineのRust実装 |
| [jlabel](https://crates.io/crates/jlabel) | フルコンテキストラベルの構造体定義 |
| [Lindera](https://github.com/lindera-morphology/lindera) | Rust製形態素解析エンジン（MeCab互換） |

---

## 11. まとめ: C#移植における重要ポイント

### 移植の優先度

1. **最優先**: jpreprocess-coreのデータ構造（POS, Pronunciation, Mora, WordDetails）
2. **高優先**: NJD処理の6つの変換モジュール（pronunciation, digit_sequence, digit, accent_phrase, accent_type, unvoiced_vowel）
3. **高優先**: JPCommonのUtterance階層構造とフルコンテキストラベル生成
4. **中優先**: テキスト正規化（normalize_text）
5. **中優先**: 辞書読み込み（Lindera/NMeCab辞書形式対応）

### C#実装の推奨アーキテクチャ

```
DotNetG2P/
├── Core/
│   ├── POS.cs              # 品詞enum
│   ├── Mora.cs             # モーラ・音素
│   ├── Pronunciation.cs    # 発音（モーラ列+アクセント）
│   ├── WordDetails.cs      # 単語詳細情報
│   └── WordEntry.cs        # 辞書エントリ
├── NJD/
│   ├── NJDNode.cs          # NJDノード
│   ├── NJD.cs              # NJDコンテナ
│   ├── SetPronunciation.cs
│   ├── DigitSequence.cs
│   ├── SetDigit.cs
│   ├── SetAccentPhrase.cs
│   ├── SetAccentType.cs
│   └── SetUnvoicedVowel.cs
├── JPCommon/
│   ├── Utterance.cs
│   ├── BreathGroup.cs
│   ├── AccentPhrase.cs
│   ├── Word.cs
│   └── FullContextLabel.cs
├── Dictionary/
│   ├── IDictionary.cs
│   └── NaistJdicReader.cs
├── TextNormalization/
│   └── TextNormalizer.cs
└── JPreprocess.cs           # メインエントリーポイント
```

### jpreprocessの設計から学ぶべき最重要点

1. **型安全性**: 文字列ベースの処理をenumやstructによる構造化データに置き換えることで、バグを防止し可読性を向上
2. **モジュール分離**: 各NJD処理を独立したモジュールにし、個別テストを可能にする
3. **Tokenizer抽象化**: 形態素解析エンジンの差し替えを容易にする
4. **テストの充実**: フルコンテキストラベルの文字列を厳密に検証するテストケース
5. **OpenJTalkとの差異の明文化**: 意図的な差異をドキュメントとテストで明確に管理
