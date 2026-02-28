# 日本語アクセント結合規則

## 1. 日本語アクセントの基本

### 1.1 高低アクセント（ピッチアクセント）

日本語（標準語・東京方言）は**高低アクセント**（pitch accent）を持つ言語である。英語のような**強弱アクセント**（stress accent）とは異なり、音の高さ（ピッチ）の変化によって語の意味を区別する。

- **箸** (はし): 高低 → 「は」が高い（頭高型）
- **橋** (はし): 低高 → 「し」が高い（平板型/尾高型）

### 1.2 アクセント核

**アクセント核**（accent nucleus, kaku）とは、ピッチが急激に下降する位置のことである。

- アクセント核がある → **起伏型**（有核）
- アクセント核がない → **平板型**（無核）

n拍（モーラ）の語には n+1 通りのアクセント型が存在しうる（0型〜n型）。

### 1.3 基本規則

1. **第1拍と第2拍は必ず高さが異なる**
2. **1語の中でピッチが急激に下がるのは1回だけ**
3. アクセント核の直後でピッチが下がる

## 2. 東京方言アクセントの4類型

nモーラの語のアクセント型は以下の4種に分類される:

| 型名 | アクセント核位置 | 例（3拍語） | ピッチパターン |
|------|------------------|-------------|----------------|
| **平板型**（0型） | なし | さくら | 低高高(高) |
| **頭高型**（1型） | 第1拍 | みかん | 高低低(低) |
| **中高型**（2型〜n-1型） | 第2拍〜第n-1拍 | おかし(2型) | 低高低(低) |
| **尾高型**（n型） | 最終拍 | おとこ | 低高高(低) |

※ 括弧内は助詞接続時のピッチ

### 2.1 アクセント値の表記

OpenJTalk/naist-jdicでは、アクセント型を整数値で表す:

- **0** = 平板型（アクセント核なし）
- **1** = 頭高型（第1モーラにアクセント核）
- **2** = 第2モーラにアクセント核
- **n** = 第nモーラにアクセント核

## 3. 複合語アクセント規則

複合語（2つ以上の形態素から構成される語）のアクセントは、単独語のアクセントとは異なる規則で決定される。

### 3.1 後部要素のモーラ数による規則

#### 後部要素が1〜2拍の場合

- **基本**: アクセント核は**前部要素の最後の拍**に置かれる
- **例外**: 前部要素の最後が特殊拍（促音・撥音・長音）の場合、1つ前の拍に移動
- **例**: 神戸市(こうべし) → 「べ」にアクセント核

一部の後部要素（「語」「色」「課」「中」「家」等）では平板型になる:
- 中国語(ちゅうごくご) → 平板型

#### 後部要素が3〜4拍の場合

- **後部要素が平板型・尾高型**: アクセント核は後部要素の**1拍目**
  - 女言葉(おんなことば) → 「こ」にアクセント核
- **後部要素が頭高型・中高型**: 後部要素の**元のアクセント核位置**を保持
  - 朝御飯(あさごはん) → 「ご」にアクセント核

#### 後部要素が5拍以上の場合

- 後部要素の**元のアクセント核位置**がそのまま保持される
  - 山田小学校(やまだしょうがっこう) → 「しょうが」の位置にアクセント核

### 3.2 言語学的研究

#### 窪薗（1995）の研究

窪薗晴夫の研究によると、複合語の2要素が格関係（主語+動詞、目的語+動詞等）を持つ場合、特に漢語を含むものは複合語アクセント規則の適用を受けにくい。

例: 消息不明、自信喪失、首位攻防 など

#### 藤崎モデル

藤崎モデルは、F0（基本周波数）パターンを2つの成分に分解する:

1. **フレーズ成分**: 甲状軟骨の平行移動運動に対応。文全体の大きなイントネーション曲線
2. **アクセント成分**: 甲状軟骨の回転運動に対応。個々のアクセント句内のピッチ変動

このモデルはアクセント情報からF0パターンを生成する際に使用され、TTSシステムの自然性向上に寄与する。

## 4. OpenJTalkのアクセント結合タイプ（C1〜C5、F1〜F5、P1〜P14）

naist-jdic辞書のフィールド15に記録される**アクセント結合タイプ**（chain_rule）は、複合語形成時のアクセント核位置の計算方法を規定する。

### 4.1 辞書フォーマット

naist-jdic辞書のエントリにおいて:

- **フィールド14**: `アクセント核位置/モーラ数`（例: `3/4`）
- **フィールド15**: アクセント結合タイプ（例: `C2`、`F1@3`）

chain_ruleは `%品詞条件@ルールタイプ/加算値` の形式でエンコードされ、`get_rule`関数でパース処理される。

### 4.2 C系列ルール（名詞結合型）

C系列は主に名詞の複合時に適用される:

| タイプ | 名称 | 計算式 | 説明 |
|--------|------|--------|------|
| **C1** | 自立語結合保存型 | `mora_size + node_acc` | 前部要素の蓄積モーラ数に後続要素のアクセント核位置を加算 |
| **C2** | 自立語結合生起型 | `mora_size + 1` | 前部要素の蓄積モーラ数+1の位置にアクセント核を設定（後部要素の1拍目） |
| **C3** | 接辞結合標準型 | `mora_size` | 前部要素の最後のモーラにアクセント核を設定 |
| **C4** | 接辞結合平板化型 | `0` | 複合語全体を平板型に設定 |
| **C5** | 従属型 | `top_node_acc`（変更なし） | 前部要素のアクセント型をそのまま保持 |

#### C系列の具体例

```
C1: 「東京」(0/4) + 「大学」(acc=3) → アクセント核 = 4 + 3 = 7 (「だい」)
C2: 「東京」(0/4) + 「駅」 → アクセント核 = 4 + 1 = 5 (「え」)
C3: 「東京」(0/4) + 「的」 → アクセント核 = 4 (「う」)
C4: 「東京」(0/4) + 「語」 → アクセント核 = 0 (平板型)
C5: 「お」 + 「茶」(acc=0) → アクセント核 = 0 (前部維持)
```

### 4.3 F系列ルール（付属語結合型）

F系列は助詞・助動詞などの付属語が接続する際に適用される:

| タイプ | 計算式 | 説明 |
|--------|--------|------|
| **F1** | `top_node_acc`（変更なし） | 前部のアクセント型を保持 |
| **F2** | 平板型の場合のみ `mora_size + add_type` | 前部が平板型(0)の場合のみアクセント核を生成 |
| **F3** | 有核の場合のみ `mora_size + add_type` | 前部が有核の場合のみ加算 |
| **F4** | 常に `mora_size + add_type` | 常にアクセント核位置を再計算 |
| **F5** | `0` | 常に平板化 |

#### F系列の条件ロジック

```
F1: return top_node_acc  // 何もしない
F2: if top_node_acc == 0 then mora_size + add_type else top_node_acc
F3: if top_node_acc != 0 then mora_size + add_type else top_node_acc
F4: return mora_size + add_type  // 常に再計算
F5: return 0  // 常に平板化
```

### 4.4 P系列ルール（その他の結合型）

P系列はより特殊な結合パターンを扱う:

| タイプ | 計算式 | 説明 |
|--------|--------|------|
| **P1** | 平板型なら `0`、有核なら `mora_size + node_acc` | 平板型保持 or 位置加算 |
| **P2** | 平板型なら `0`、有核なら `mora_size + node_acc` | P1と同様 |
| **P6** | `0` | 常に平板化 |
| **P14** | 有核なら `mora_size + node_acc`、無核なら変更なし | 有核時のみ位置加算 |

### 4.5 get_rule関数の処理フロー

```
入力: chain_rule文字列（辞書フィールド15）, 前接ノードの品詞
処理:
  1. chain_ruleを "%" で分割
  2. 各ルールについて:
     a. "@" で品詞条件とルール部分を分離
     b. 品詞条件が前接ノードの品詞とマッチするか確認
     c. マッチしたら "/" でルールタイプと加算値を分離
     d. ルールタイプ（C1, F2等）と加算値を返す
  3. マッチしない場合はデフォルト "*" を返す
出力: (rule_type, add_type)
```

### 4.6 数字のアクセント規則（calc_digit_accent）

連続する数字形態素には特別なアクセント規則が適用される:

| 先行数字 | 後続数字 | アクセント核位置 |
|----------|----------|-----------------|
| 5/6/8 | 十(10) + 一位の数 | 0（平板型） |
| 任意 | 十(10) | 1 |
| 7 | 百(100) | 2 |
| 3/4/9/何 | 百(100) | 1 |
| その他 | 百(100) | mora_sizeの合計 |
| 任意 | 千(1000)/万(10000) | mora_size + 1 |
| 1/6/7/8/幾 | 億(100000000) | 2 |
| その他 | 億(100000000) | 1 |
| 6/7 | 兆(1000000000000) | 2 |
| その他 | 兆(1000000000000) | 1 |

## 5. njd_set_accent_phraseの結合ルール（品詞ベース）

アクセント句の境界決定は18のルールに基づく。以下に全ルールを示す:

### 5.1 ルール一覧

| ルール | 条件 | 結合(chain) | 説明 |
|--------|------|-------------|------|
| **01** | デフォルト | true | 特に条件がなければくっつける |
| **02** | 名詞 + 名詞 | true | 連続する名詞はくっつける |
| **03** | 形容詞 + 名詞 | false | 別のアクセント句に |
| **04** | 名詞(形容動詞語幹) + 名詞 | false | 別のアクセント句に |
| **05** | 動詞 + 形容詞/名詞 | false | 別のアクセント句に |
| **06** | 副詞/接続詞/連体詞 | false | 単独のアクセント句に |
| **07** | 名詞(副詞可能) | false | 単独のアクセント句に（「すべて」等） |
| **08** | 任意 + 助詞/助動詞 | true | 付属語は前にくっつける |
| **09** | 助詞/助動詞 + 自立語 | false | 付属語の後の自立語は別のアクセント句に |
| **10** | *,接尾 + 名詞 | false | 接尾辞の後の名詞は別のアクセント句に |
| **11** | 動詞連用*/形容詞連用*/助詞(て/で) + 形容詞(非自立) | true | 非自立形容詞は特定条件で前にくっつける |
| **12** | 動詞連用*/名詞(サ変接続) + 動詞(非自立) | true | 非自立動詞は特定条件で前にくっつける |
| **13** | 名詞 + 動詞/形容詞/名詞(形容動詞語幹) | false | 別のアクセント句に |
| **14** | 記号 | false | 記号は単独のアクセント句に |
| **15** | 接頭詞 | false | 接頭詞は単独のアクセント句に |
| **16** | *,*,*,姓 + 名詞 | false | 姓の後の名詞は別のアクセント句に |
| **17** | 名詞 + *,*,*,名 | false | 名詞の後の名（人名）は別のアクセント句に |
| **18** | 任意 + *,接尾 | true | 接尾辞は前にくっつける |

### 5.2 ルール適用の優先順位

jpreprocess（Rust実装）では、Rustの`match`文で**上から順にマッチング**される。優先順位は以下の通り:

1. ルール18（接尾辞 → 結合）が最優先
2. ルール17, 16（人名関連）
3. ルール15（接頭詞）
4. ルール14（記号）
5. ルール13（名詞+自立語の分離）
6. ルール12, 11（非自立語の結合条件）
7. ルール10（接尾辞+名詞の分離）
8. ルール08, 09（助詞・助動詞の処理）
9. ルール07, 06（副詞等の分離）
10. ルール05, 04, 03（動詞/形容詞/形動+名詞）
11. ルール02（名詞連続の結合）
12. ルール01（デフォルト結合）

### 5.3 品詞分類（MeCab/IPADICベース）

| 品詞大分類 | 品詞細分類 | chain_flag | 備考 |
|-----------|-----------|------------|------|
| 名詞 | 一般/固有名詞/数 | 文脈依存 | ルール02, 13等 |
| 名詞 | 接尾 | true | ルール18で結合 |
| 名詞 | 副詞可能 | false | ルール07で分離 |
| 名詞 | 形容動詞語幹 | 文脈依存 | ルール04, 13 |
| 名詞 | サ変接続 | 文脈依存 | ルール12の条件 |
| 動詞 | 自立 | 文脈依存 | ルール05等 |
| 動詞 | 非自立 | 条件付き結合 | ルール12 |
| 動詞 | 接尾 | true | ルール18 |
| 形容詞 | 自立 | 文脈依存 | ルール03等 |
| 形容詞 | 非自立 | 条件付き結合 | ルール11 |
| 形容詞 | 接尾 | true | ルール18 |
| 助詞 | 各種 | true | ルール08 |
| 助動詞 | - | true | ルール08 |
| 副詞 | - | false | ルール06 |
| 接続詞 | - | false | ルール06 |
| 連体詞 | - | false | ルール06 |
| 接頭詞 | - | false | ルール15 |
| 記号 | - | false | ルール14 |

## 6. njd_set_accent_typeの実装ロジック

### 6.1 処理フロー全体

```
入力: NJDノードのリスト（chain_flag設定済み）
処理:
  for each node in nodes:
    if node == head or chain_flag != 1:
      // 新しいアクセント句の開始
      top_node = node
      mora_size = 0
    else if chain_flag == 1:
      // 前のアクセント句に結合
      rule = get_rule(node.chain_rule, prev_node.pos)
      new_acc = calc_top_node_accent(rule, top_node_acc, mora_size, node_acc)
      top_node.accent = new_acc

    mora_size += node.mora_size
出力: 各アクセント句のtop_nodeにアクセント核位置が設定される
```

### 6.2 mora_sizeの蓄積

`mora_size`はアクセント句内で**順次蓄積**される。各ノードのモーラ数が加算され、結合時のアクセント核位置計算に使用される。

```
例: 「東京」(4モーラ) + 「大学」(4モーラ)
  → mora_size = 4（「大学」結合時点）
  → C1ルール: accent = 4 + 3 = 7
```

### 6.3 calc_top_node_accent関数

```csharp
// C#実装の疑似コード
int CalcTopNodeAccent(string ruleType, int addType, int topNodeAcc, int moraSize, int nodeAcc)
{
    return ruleType switch
    {
        "C1" => moraSize + nodeAcc,     // 位置加算
        "C2" => moraSize + 1,            // 後部要素1拍目
        "C3" => moraSize,                // 前部末尾
        "C4" => 0,                       // 平板化
        "C5" => topNodeAcc,              // 前部保持

        "F1" => topNodeAcc,              // 変更なし
        "F2" => topNodeAcc == 0 ? moraSize + addType : topNodeAcc,
        "F3" => topNodeAcc != 0 ? moraSize + addType : topNodeAcc,
        "F4" => moraSize + addType,      // 常に再計算
        "F5" => 0,                       // 平板化

        "P1" or "P2" => topNodeAcc == 0 ? 0 : moraSize + nodeAcc,
        "P6" => 0,
        "P14" => topNodeAcc != 0 ? moraSize + nodeAcc : topNodeAcc,

        _ => topNodeAcc                  // デフォルト: 変更なし
    };
}
```

## 7. アクセント推定の精度向上手法

### 7.1 辞書ベースの改善

OpenJTalkの辞書ベースのアクセント推定には以下の限界がある:

- 辞書に登録されていない語（未知語）のアクセントは推定できない
- 複合語のアクセント規則は多くの例外を含む
- 文脈依存のアクセント変化（フォーカス、疑問文等）は扱えない

### 7.2 tdmelodic（ニューラルネットワークベース）

**tdmelodic**（PKSHA Technology Research）は、ニューラルネットワークを用いたアクセント辞書生成ツールである。

- **目的**: UniDic（高精度だが語彙が限定的）とNEologd（大語彙だがアクセント情報なし）を統合
- **手法**: 単語の表層形と読みからアクセント核位置を推定
- **アーキテクチャ**: 3つの学習可能サブモジュール
  - `fS()`: 表層形エンコーダ
  - `fY()`: 読み（ローマ字）エンコーダ
  - dot-product attentionによるアライメント
- **推論モード**:
  - `s2ya`: 表層形 → 読み + アクセント
  - `sy2a`: 表層形 + 読み → アクセント
- **出版**: ICASSP 2020, H. Tachibana & Y. Katayama

### 7.3 その他の研究

- **Googleの研究** (Sequence-to-sequence with 2D attention): Seq2seqモデルに2Dアテンションを導入し、日本語ピッチアクセントを学習
- **NICT** (Mora-Level Prosody Prediction): モーラレベルの韻律予測
- **助詞・助動詞アクセント研究** (郡 2020): 助詞・助動詞を「乗っとり型」「乗っとられ型」「協力型」の3種に分類

## 8. C#実装用ルール表

### 8.1 AccentRule列挙型

```csharp
/// <summary>
/// アクセント結合タイプ
/// </summary>
public enum AccentRuleType
{
    // C系列: 名詞結合型
    C1,  // 自立語結合保存型: mora_size + node_acc
    C2,  // 自立語結合生起型: mora_size + 1
    C3,  // 接辞結合標準型: mora_size
    C4,  // 接辞結合平板化型: 0
    C5,  // 従属型: top_node_acc (変更なし)

    // F系列: 付属語結合型
    F1,  // 保持型: top_node_acc
    F2,  // 平板時生起型: 平板なら mora_size + add_type
    F3,  // 有核時変更型: 有核なら mora_size + add_type
    F4,  // 常時再計算型: mora_size + add_type
    F5,  // 常時平板化型: 0

    // P系列: 特殊結合型
    P1,  // 平板保持/有核加算型
    P2,  // 平板保持/有核加算型 (P1と同等)
    P6,  // 常時平板化型
    P14, // 有核時加算型

    // デフォルト
    None // 変更なし
}
```

### 8.2 AccentPhraseRule（chain_flag決定用）

```csharp
/// <summary>
/// アクセント句結合判定
/// chain_flag: true=前のアクセント句に結合, false=新しいアクセント句を形成
/// </summary>
public static bool DetermineChainFlag(POS prevPos, POS currPos)
{
    // ルール18: 接尾辞は前にくっつける
    if (currPos.IsSetsubiji) return true;

    // ルール17: 名詞 + 名(人名) → 分離
    if (prevPos.IsMeishi && currPos.IsPersonMei) return false;

    // ルール16: 姓 + 名詞 → 分離
    if (prevPos.IsPersonSei && currPos.IsMeishi) return false;

    // ルール15: 接頭詞 → 分離
    if (currPos.IsSettoushi) return false;

    // ルール14: 記号 → 分離
    if (prevPos.IsKigou || currPos.IsKigou) return false;

    // ルール13: 名詞 + 動詞/形容詞/形動語幹 → 分離
    if (prevPos.IsMeishi && (currPos.IsDoushi || currPos.IsKeiyoushi
        || currPos.IsKeiyoudoushiGokan)) return false;

    // ルール12: 動詞連用/サ変接続 + 動詞(非自立) → 結合
    if ((prevPos.IsDoushi && prevPos.IsRenyou || prevPos.IsSahenSetsuzoku)
        && currPos.IsDoushiHijiritsu) return true;

    // ルール11: 動詞連用/形容詞連用/助詞(て/で) + 形容詞(非自立) → 結合
    if (((prevPos.IsDoushi || prevPos.IsKeiyoushi) && prevPos.IsRenyou
        || prevPos.IsSetsuzokuJoshiTeDe)
        && currPos.IsKeiyoushiHijiritsu) return true;

    // ルール10: 接尾 + 名詞 → 分離
    if (prevPos.IsSetsubiji && currPos.IsMeishi) return false;

    // ルール08-09: 助詞/助動詞の処理
    if ((prevPos.IsJoshi || prevPos.IsJodoushi) && (currPos.IsJoshi || currPos.IsJodoushi))
        return true;   // ルール08
    if (prevPos.IsJoshi || prevPos.IsJodoushi)
        return false;  // ルール09
    if (currPos.IsJoshi || currPos.IsJodoushi)
        return true;   // ルール08

    // ルール07: 副詞可能 → 分離
    if (prevPos.IsFukushiKanou || currPos.IsFukushiKanou) return false;

    // ルール06: 副詞/接続詞/連体詞 → 分離
    if (prevPos.IsFukushi || prevPos.IsSetsuzokushi || prevPos.IsRentaishi
        || currPos.IsFukushi || currPos.IsSetsuzokushi || currPos.IsRentaishi) return false;

    // ルール05: 動詞 + 形容詞/名詞 → 分離
    if (prevPos.IsDoushi && (currPos.IsKeiyoushi || currPos.IsMeishi)) return false;

    // ルール04: 形動語幹 + 名詞 → 分離
    if (prevPos.IsKeiyoudoushiGokan && currPos.IsMeishi) return false;

    // ルール03: 形容詞 + 名詞 → 分離
    if (prevPos.IsKeiyoushi && currPos.IsMeishi) return false;

    // ルール02: 名詞 + 名詞 → 結合
    if (prevPos.IsMeishi && currPos.IsMeishi) return true;

    // ルール01: デフォルト → 結合
    return true;
}
```

### 8.3 数字アクセント規則表

```csharp
/// <summary>
/// 数字形態素の特殊アクセント規則
/// </summary>
public static class DigitAccentRules
{
    // (先行数字, 後続位取り) → アクセント核位置
    // -1 = mora_size合計, -2 = mora_size + 1
    public static readonly Dictionary<(string, string), int> Rules = new()
    {
        // 十の位
        { ("五", "十"), 0 }, // + 一位数字がある場合のみ
        { ("六", "十"), 0 },
        { ("八", "十"), 0 },
        { ("*", "十"), 1 },  // それ以外 + 十

        // 百の位
        { ("七", "百"), 2 },
        { ("三", "百"), 1 },
        { ("四", "百"), 1 },
        { ("九", "百"), 1 },
        { ("何", "百"), 1 },
        { ("*", "百"), -1 },  // mora_size合計

        // 千・万の位
        { ("*", "千"), -2 },  // mora_size + 1
        { ("*", "万"), -2 },

        // 億の位
        { ("一", "億"), 2 },
        { ("六", "億"), 2 },
        { ("七", "億"), 2 },
        { ("八", "億"), 2 },
        { ("幾", "億"), 2 },
        { ("*", "億"), 1 },

        // 兆の位
        { ("六", "兆"), 2 },
        { ("七", "兆"), 2 },
        { ("*", "兆"), 1 },
    };
}
```

## 9. 参考資料・出典

- OpenJTalk ソースコード: `njd_set_accent_type.c`, `njd_set_accent_phrase.c`
  - https://open-jtalk.sourceforge.net/
  - https://sources.debian.org/src/open-jtalk/1.11-1.1/
- jpreprocess（Rust実装）: `accent_type.rs`, `accent_phrase.rs`
  - https://github.com/jpreprocess/jpreprocess
- tdmelodic（ニューラルネットワークベースのアクセント推定）:
  - https://github.com/PKSHATechnology-Research/tdmelodic
  - H. Tachibana & Y. Katayama, "Accent Estimation of Japanese Words from Their Surfaces and Romanizations for Building Large Vocabulary Accent Dictionaries," ICASSP 2020
- 東京外国語大学 言語モジュール（複合名詞アクセント）:
  - https://www.coelang.tufs.ac.jp/mt/ja/pmod/practical/02-07-01.php
- 東京式アクセント解説:
  - https://www.nihongo-appliedlinguistics.net/wp/archives/4519
- OpenJTalk解析資料:
  - https://www.negi.moe/negitalk/openjtalk.html
- 助詞・助動詞のアクセント（郡 2020）:
  - https://www.lang.osaka-u.ac.jp/~caris/articles/
- Google Research - Seq2Seq with 2D Attention for Japanese Pitch Accent:
  - https://research.google/pubs/sequence-to-sequence-neural-network-model-with-2d-attention-for-learning-japanese-pitch-accents/
- 窪薗晴夫 - 日本語のアクセントとアクセント類型論（科研費プロジェクト）
