# MeCab系C#ライブラリ詳細調査

## 1. MeCabの内部アルゴリズム

### 1.1 全体アーキテクチャ

MeCab（和布蕪）は、工藤拓氏によって開発された高速な日本語形態素解析エンジンである。入力テキストを形態素（最小の意味単位）に分割し、各形態素に品詞・読み・原形などの情報を付与する。MeCabの解析は以下の3段階で行われる:

1. **辞書引き（共通接頭辞検索）**: ダブル配列Trieによる高速な単語候補の取得
2. **ラティス（格子）構築**: 入力文の全位置における全候補単語をグラフとして構築
3. **ビタビデコーディング**: 動的計画法による最小コスト経路の探索

### 1.2 ダブル配列Trie（DARTS）

MeCabは辞書の検索にDARTS（Double-ARray Trie System）を採用している。

**トライ（Trie）とは:**
- 文字列の集合を効率的に格納・検索するための木構造
- 共通の接頭辞を持つ文字列群を効率よく扱える

**ダブル配列の特徴:**
- `base[]`と`check[]`の2つの配列でトライ構造を表現
- 遷移: `base[s] + c = t` かつ `check[t] = s`（状態sから文字cで状態tに遷移）
- メモリ効率が高く、検索が高速（O(キー長)で検索可能）
- **共通接頭辞検索（Common Prefix Search）** に特化
  - 例: 「東京都」→「東」「東京」「東京都」を一度に検索

**辞書引きの流れ:**
1. 入力文の各位置iから始まる文字列に対してCommon Prefix Searchを実行
2. 位置iから始まる全ての候補単語を一括取得
3. 取得した候補をラティスのノードとして追加

### 1.3 ラティス（格子）構築

ラティスは入力文の全ての分割可能性を表現した有向非巡回グラフ（DAG）である。

```
BOS → [東京/名詞] → [都/接尾辞] → [に/助詞] → [行く/動詞] → EOS
   └→ [東京都/名詞] ─────────→ [に/助詞] → [行く/動詞] → EOS
   └→ [東/名詞] → [京都/名詞] → [に/助詞] → [行く/動詞] → EOS
```

**ノードの情報:**
- 表層形（Surface）
- 品詞情報（左文脈ID、右文脈ID）
- 単語生起コスト（wcost）
- 入力文中の開始・終了位置

**エッジの情報:**
- 連接コスト: 前の形態素の右文脈IDと次の形態素の左文脈IDの組み合わせで決定
- `matrix.bin`に格納された連接コスト表から取得

### 1.4 コスト計算

MeCabのコスト体系は2種類:

**単語生起コスト（Generation Cost）:**
- 各単語が出現する確率を反映したコスト値
- 辞書ファイル（sys.dic）に格納
- 値が小さいほど出現確率が高い

**連接コスト（Connection Cost）:**
- 品詞間の遷移確率を反映したコスト値
- `matrix.bin`に格納
- 前の形態素の右文脈ID × 次の形態素の左文脈IDの行列
- 例: 「名詞→助詞」は低コスト（自然な接続）、「助詞→助詞」は高コスト（不自然）

**コスト学習:**
- MeCab 0.90以降はCRF（Conditional Random Fields: 条件付き確率場）でモデルパラメータを学習
- 正解コーパスから「正解のコスト < その他の候補のコスト」となるよう最適化
- 素性テンプレート（feature.def）で品詞・原形などの組み合わせを定義

### 1.5 ビタビ（Viterbi）デコーディング

ビタビアルゴリズムは動的計画法に基づく最適経路探索アルゴリズムである。

**アルゴリズムの手順:**

1. **前向き処理（Forward Pass）:**
   ```
   各ノードnについて:
     累積コスト(n) = min{累積コスト(prev) + 連接コスト(prev→n) + 生起コスト(n)}
     最良前ノード(n) = argmin{上記}
   ```

2. **後ろ向き処理（Backward Pass）:**
   - EOSノードから最良前ノードを辿り、最適パスを復元

**計算量:**
- O(N × M^2): N=入力文長, M=各位置の平均候補数
- 実際にはラティスが疎なため、これより高速に動作

**N-Best解の取得:**
- A*探索アルゴリズムを用い、2番目以降の候補経路も取得可能

### 1.6 未知語処理

辞書に登録されていない語（未知語）の処理:
- 文字種（ひらがな・カタカナ・漢字・英字等）に基づくルール
- `unk.dic`（未知語辞書）から文字種別の生起コストを取得
- `char.def`で文字種の定義・グループ化を設定

---

## 2. NMeCab（komutan/NMeCab）

### 2.1 概要

| 項目 | 内容 |
|------|------|
| リポジトリ | https://github.com/komutan/NMeCab |
| 作者 | Tsuyoshi Komuta (komutan) |
| 言語 | C#（フルマネージド実装） |
| 最新バージョン | 0.10.2（LibNMeCab） |
| NuGet | `LibNMeCab` (0.10.2) |
| ターゲット | .NET Standard 2.0 |
| ライセンス | GPL-2.0 / LGPL-2.1（デュアルライセンス） |
| 元実装 | MeCab (C++) の C# 再実装 |

### 2.2 C#実装の品質・完成度

**実装の特徴:**
- MeCabのコアアルゴリズムをC#で完全再実装（ネイティブバイナリ依存なし）
- ダブル配列Trie、ラティス構築、ビタビデコーディングを全てマネージドコードで実装
- MeCabと同一の辞書バイナリフォーマットを使用可能

**ソースコード構造:**
```
src/LibNMeCab/
  Core/           # コア処理（Trie、Tokenizer、Viterbi等）
  Specialized/    # 辞書フォーマット別のTagger/Node実装
  MeCabTagger.cs        # 汎用タガー
  MeCabTaggerBase.cs    # タガー基底クラス
  MeCabNode.cs          # 汎用ノード
  MeCabNodeBase.cs      # ノード基底クラス
  MeCabLattice.cs       # ラティス構造
  MeCabPath.cs          # パス管理
  NBestGenerator.cs     # N-best生成
  MeCabParam.cs         # パラメータ管理
  MeCabDictionaryType.cs # 辞書型定義
```

**完成度の評価:**
- コアの形態素解析機能は十分に安定
- MeCab本体と同一の辞書を使用するため、解析結果の互換性が高い
- スレッドセーフなTaggerインスタンス設計
- N-Best解析、マージナル確率によるソフト分割にも対応
- 0.10.0で大幅リファクタリング、LINQ対応の配列戻り値に変更

### 2.3 API設計

**タガークラス体系:**

| クラス | 名前空間 | 対応辞書 |
|--------|---------|---------|
| `MeCabTagger` | NMeCab | 汎用（Feature文字列） |
| `MeCabIpaDicTagger` | NMeCab.Specialized | IPA辞書 |
| `MeCabUniDic21Tagger` | NMeCab.Specialized | UniDic 2.1.x |
| `MeCabUniDic22Tagger` | NMeCab.Specialized | UniDic 2.2.x |

**基本的な使用例:**

```csharp
// 汎用タガー
using (var tagger = MeCabTagger.Create())
{
    var nodes = tagger.Parse("今日は良い天気です");
    foreach (var node in nodes)
    {
        Console.WriteLine($"{node.Surface}\t{node.Feature}");
    }
}

// IPA辞書専用タガー（型付きプロパティアクセス）
using (var tagger = MeCabIpaDicTagger.Create())
{
    var nodes = tagger.Parse("今日は良い天気です");
    foreach (var node in nodes)
    {
        Console.WriteLine($"{node.Surface}\t{node.PartsOfSpeech}\t{node.Reading}");
    }
}
```

**主要API:**

| メソッド | 説明 |
|---------|------|
| `Parse(string)` | 形態素解析（ノード配列を返す） |
| `ParseNBest(string)` | N-Best解析（複数候補を列挙） |
| `ParseSoftWakachi(string, float)` | マージナル確率によるソフト分割 |

**ノードプロパティ:**

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `Surface` | string | 表層形 |
| `Feature` | string | 素性情報（CSV文字列） |
| `BestCost` | long | 累積最小コスト |
| `Prev` / `Next` | MeCabNodeBase | リンクリスト互換 |
| `GetFeatureAt(int)` | string | Feature文字列のn番目フィールド |

### 2.4 対応辞書フォーマット

NMeCabはMeCab互換のバイナリ辞書フォーマットを使用する。辞書はCSVテキストから`mecab-dict-index`コマンドでバイナリに変換したものを読み込む。

**公式提供の辞書パッケージ（NuGet）:**

| パッケージ | 辞書 |
|-----------|------|
| `LibNMeCab.IpaDicBin` | IPA辞書 |
| `LibNMeCab.UniDic21Bin` | UniDic 2.1.x |
| `LibNMeCab.UniDic22Bin` | UniDic 2.2.x |

**辞書バイナリの構成ファイル:**
- `sys.dic` - システム辞書（単語エントリ + ダブル配列Trie）
- `matrix.bin` - 連接コスト行列
- `char.bin` - 文字種定義
- `unk.dic` - 未知語辞書
- `dicrc` - 辞書設定ファイル

**カスタム辞書の使用:**
- ユーザー辞書を追加可能（MeCab標準の`mecab-dict-index`で作成）
- `MeCabParam.UserDicFiles`プロパティで指定

### 2.5 メンテナンス状況

| 時期 | 状況 |
|------|------|
| 初期 | OSDN上で開発、長期間更新停止 |
| 2020年頃 | GitHubに移行、0.10.0で大幅リファクタリング |
| 2021年 | 0.10.1リリース |
| 2022年以降 | 0.10.2リリース（BOS Nodeのバグ修正） |
| 現在 | 低頻度の更新、安定フェーズ |

- Issueでの質問対応は行われている
- MeCab.DotNetとの統合が計画されていたが、進捗は不明

---

## 3. MeCab.DotNet（kekyo/MeCab.DotNet）

### 3.1 概要

| 項目 | 内容 |
|------|------|
| リポジトリ | https://github.com/kekyo/MeCab.DotNet |
| 作者 | Kouji Matsui (kekyo) |
| 言語 | C# |
| 最新バージョン | 1.2.0 |
| NuGet | `MeCab.DotNet` |
| ターゲット | .NET Standard 1.3 ～ 2.1, .NET Framework 2.0+, .NET Core 2.0+, .NET 5～8 |
| ライセンス | GPL-2.0 / LGPL-2.1（NMeCabから継承） |
| Stars | 60 |
| 元実装 | NMeCabのフォーク＋改良 |

### 3.2 NMeCabとの違い

MeCab.DotNetはNMeCabをベースに以下の改良を施したプロジェクトである:

| 項目 | NMeCab (LibNMeCab) | MeCab.DotNet |
|------|-------------------|--------------|
| 名前空間 | `NMeCab` | `MeCab` |
| ターゲット | .NET Standard 2.0 | .NET Standard 1.3～2.1 + 個別TFM |
| 辞書 | 別途NuGetパッケージ | IPADIC同梱（自動配置） |
| 設定 | `MeCabParam`クラス | `MeCabParam`クラス（App.config廃止） |
| API | 配列返却 + リンクリスト | `ParseToNodes()`等の追加メソッド |
| NuGet | `LibNMeCab` | `MeCab.DotNet`（辞書同梱） |
| PCL対応 | なし（廃止） | なし（廃止） |

**MeCab.DotNetの主な改善点:**
1. **幅広いプラットフォーム対応**: .NET Standard 1.3からサポート
2. **辞書の自動配置**: NuGetインストール時にIPADIC辞書が`dic`フォルダに自動コピー
3. **App.config依存の排除**: プログラム内で全設定可能
4. **ユーティリティメソッドの追加**: より使いやすいAPIを提供

**基本的な使用例（MeCab.DotNet）:**

```csharp
var parameter = new MeCabParam();
var tagger = MeCabTagger.Create(parameter);

foreach (var node in tagger.ParseToNodes("今日は良い天気です"))
{
    if (node.CharType > 0)
    {
        var features = node.Feature.Split(',');
        Console.WriteLine($"{node.Surface}\t{features[0]}\t{features[7]}");
    }
}
```

### 3.3 .NET Standard対応状況

**対応フレームワーク一覧:**

| フレームワーク | バージョン |
|--------------|-----------|
| .NET Standard | 1.3, 2.0, 2.1 |
| .NET | 5, 6, 7, 8 |
| .NET Core | 2.0, 2.1, 3.0, 3.1 |
| .NET Framework | 2.0 ～ 4.8.1 |

### 3.4 Unity対応可能性

**対応状況の評価:**

| 観点 | 評価 |
|------|------|
| .NET Standard互換性 | .NET Standard 2.0/2.1対応でUnity 2021.2+で動作可能 |
| マネージドコード | 完全マネージド（ネイティブ依存なし）でクロスプラットフォーム対応 |
| NuGet統合 | NuGetForUnityで導入可能 |
| 辞書配置 | **要手動対応**: dicフォルダをUnityプロジェクトルートにコピーが必要 |
| IL2CPP | リフレクション使用が限定的なため、概ね対応可能と推定 |
| WebGL | ファイルI/O依存のため追加対応が必要な可能性 |

**Unity導入手順:**

1. NuGetForUnity（https://github.com/GlitchEnzo/NuGetForUnity）をインストール
2. NuGetから`MeCab.DotNet`をインストール
3. **重要**: MeCab.DotNetパッケージ内のdicフォルダをUnityプロジェクトルートにコピー
   - そのままではパスエラーが発生する
4. `MeCabParam`で辞書パスを指定して使用

**Unity使用時の注意点:**
- 辞書ファイル（IPADIC: 約50MB）がビルドサイズに影響
- StreamingAssetsまたはAddressablesで辞書を管理する方が望ましい
- モバイル環境では辞書読み込み時のメモリ消費に注意
- IL2CPP環境での動作確認が推奨される

---

## 4. OpenJTalk用naist-jdic辞書の互換性

### 4.1 辞書フォーマットの違い

OpenJTalk用naist-jdicはMeCab標準のIPA辞書フォーマットを拡張している。

**標準IPADIC（13フィールド）:**
```
表層形,左文脈ID,右文脈ID,コスト,品詞,品詞細分類1,品詞細分類2,品詞細分類3,活用型,活用形,原形,読み,発音
```

**OpenJTalk用naist-jdic（15フィールド）:**
```
表層形,左文脈ID,右文脈ID,コスト,品詞,品詞細分類1,品詞細分類2,品詞細分類3,活用型,活用形,原形,読み,発音,アクセント核位置/モーラ数,アクセント結合タイプ
```

**追加フィールド:**
- **フィールド14**: `アクセント核位置/モーラ数`（例: `3/4`, `0/2`）
  - 0はアクセントなし（平板型）を意味
- **フィールド15**: アクセント結合タイプ（C1〜C5）
  - C1: 自立語結合保存型
  - C2: 自立語結合生起型
  - C3: 接辞結合標準型
  - C4: 接辞結合平板化型
  - C5: 従属型

### 4.2 NMeCab/MeCab.DotNetでの使用可能性

**結論: 使用可能だが、カスタム対応が必要**

**バイナリ辞書レベル:**
- NMeCab/MeCab.DotNetはMeCab互換のバイナリ辞書フォーマット（sys.dic, matrix.bin等）を読み込む
- OpenJTalk用naist-jdicもMeCab互換の`mecab-dict-index`でコンパイルされたバイナリ辞書
- したがって、**バイナリ辞書としてはそのまま読み込み可能**

**Feature文字列の解析:**
- NMeCabの`MeCabIpaDicTagger`はIPADIC標準の13フィールドを前提としており、アクセント情報フィールド（14, 15）にはアクセスできない
- **対応方法1**: 汎用の`MeCabTagger`を使用し、`Feature`文字列を自前でパースする
  ```csharp
  var features = node.Feature.Split(',');
  var accentInfo = features[13]; // "3/4"
  var accentType = features[14]; // "C2"
  ```
- **対応方法2**: `MeCabNodeBase`を継承したカスタムノードクラスを作成し、`NMeCab.Specialized`の仕組みでnaist-jdic専用Taggerを実装

**辞書の準備:**
1. naist-jdicのCSVソースを入手（jpreprocess/naist-jdicリポジトリ等）
2. `mecab-dict-index`でUTF-8バイナリ辞書にコンパイル
3. NMeCab/MeCab.DotNetのカスタム辞書として設定
   - MeCab.DotNet: `MeCabUseDefaultDictionary`をFalseに設定し、辞書パスを指定

### 4.3 実装上の推奨アプローチ

本プロジェクト（dot-net-g2p）向けの推奨:

```csharp
// naist-jdic専用のノードクラス例
public class NaistJdicNode : MeCabNodeBase<NaistJdicNode>
{
    // IPADIC標準フィールド
    public string PartsOfSpeech => GetFeatureAt(0);
    public string PartsOfSpeechSection1 => GetFeatureAt(1);
    public string PartsOfSpeechSection2 => GetFeatureAt(2);
    public string PartsOfSpeechSection3 => GetFeatureAt(3);
    public string ConjugatedForm => GetFeatureAt(4);
    public string Inflection => GetFeatureAt(5);
    public string OriginalForm => GetFeatureAt(6);
    public string Reading => GetFeatureAt(7);
    public string Pronunciation => GetFeatureAt(8);

    // OpenJTalk拡張フィールド
    public string AccentInfo => GetFeatureAt(9);      // "3/4"
    public string AccentType => GetFeatureAt(10);      // "C2"

    // ヘルパーメソッド
    public int AccentPosition => ParseAccentPosition();
    public int MoraCount => ParseMoraCount();

    private int ParseAccentPosition()
    {
        var info = AccentInfo?.Split('/');
        return info != null && info.Length >= 1 && int.TryParse(info[0], out var pos) ? pos : -1;
    }

    private int ParseMoraCount()
    {
        var info = AccentInfo?.Split('/');
        return info != null && info.Length >= 2 && int.TryParse(info[1], out var count) ? count : -1;
    }
}
```

> **注意**: 上記のフィールドインデックスはFeature文字列の分割後のインデックスであり、CSVの列番号（表層形・左文脈ID等を含む）とは異なる。実際のインデックスは辞書フォーマットに応じて検証が必要。

---

## 5. ライセンス問題の整理

### 5.1 各ソフトウェアのライセンス

| ソフトウェア | ライセンス | 備考 |
|------------|-----------|------|
| MeCab (本家) | GPL / LGPL / BSD のトリプルライセンス | BSDを選択可能 |
| NMeCab (LibNMeCab) | GPL-2.0 / LGPL-2.1 のデュアルライセンス | BSDなし |
| MeCab.DotNet | GPL-2.0 / LGPL-2.1（NMeCabから継承） | BSDなし |
| naist-jdic辞書 | BSD License | NAIST提供、自由に使用可能 |
| IPADIC辞書 | ICOT条項付き（旧）/ BSD互換 | naist-jdicで解消済み |

### 5.2 ライセンスの影響分析

**MeCab本家をC#で独自再実装する場合:**
- MeCab本家のBSDライセンスを選択すれば、独自実装への制約なし
- アルゴリズム自体は論文公開されており、特許制約もない

**NMeCabを使用する場合（LGPL-2.1選択時）:**
- LGPLはDLL（動的リンク）として使用すれば、利用側のソースコード公開義務なし
- ただし、LGPL DLL自体の改変は公開必要
- **Unity Asset Storeでの配布は制限あり**（後述）

**MeCab.DotNetを使用する場合:**
- NMeCabと同様のLGPL制約

### 5.3 Unity Asset Storeとの関係

**重要な制約:**
- Unity Asset StoreではLGPLライセンスのアセットは**配布禁止**
- Provider Agreement（旧Section 5.10.4）でLGPL依存のパッケージが明確に禁止
- GPLはさらに厳しく禁止

**影響:**
- NMeCab/MeCab.DotNetをそのまま含んだUnityアセットはAsset Storeで公開不可
- **代替策**: MeCabアルゴリズムをBSDライセンスの範囲で独自再実装する

### 5.4 本プロジェクトへの推奨事項

| 選択肢 | ライセンス | Asset Store | 推奨度 |
|--------|-----------|-------------|--------|
| A: NMeCab/MeCab.DotNetを依存として使用 | LGPL-2.1 | 不可 | 中 |
| B: MeCab互換エンジンを独自C#実装（BSD） | BSD選択可 | 可能 | 高 |
| C: NMeCabのコードを参考にBSD互換で再実装 | 要注意 | 条件付き可能 | 中〜高 |

**推奨: 選択肢B**
- MeCab本家のBSDライセンスの範囲で、アルゴリズム（ダブル配列Trie + ビタビ）を独自にC#実装
- naist-jdic辞書（BSD）と組み合わせて完全BSD互換のライブラリを構築
- これにより、Unity Asset Storeでの配布を含む商用利用が自由になる

---

## 6. パフォーマンス比較

### 6.1 公式ベンチマーク

NMeCab/MeCab.DotNetの公式な包括的ベンチマークは公開されていない。以下は各種情報源からの推定:

### 6.2 MeCab（C++）との比較

| 項目 | MeCab (C++) | NMeCab/MeCab.DotNet (C#) |
|------|------------|--------------------------|
| 辞書読み込み | メモリマップドI/O | ファイルI/O + メモリ展開 |
| Trie検索 | ネイティブポインタ操作 | マネージド配列アクセス |
| メモリ管理 | 手動管理 | GC管理 |
| 推定速度差 | 基準 | 約2〜5倍遅い（推定） |

### 6.3 パフォーマンス特性

**NMeCab/MeCab.DotNetの長所:**
- 完全マネージドで安定性が高い
- GCチューニングによる最適化余地あり
- .NET 5以降のJIT最適化の恩恵を受けられる
- Span<T>等の高速APIの活用余地（将来）

**想定ボトルネック:**
- 辞書読み込み（初回のみ）: 辞書サイズに依存（IPADIC: 約50MB）
- Trie検索: マネージド配列アクセスはネイティブポインタより遅い
- GCパウス: 大量テキスト処理時に影響する可能性

**Unity環境での考慮事項:**
- IL2CPP: JIT最適化は受けられないが、AOTコンパイルで安定した性能
- Mono: JIT最適化が限定的でパフォーマンスが低下する可能性
- メモリ: 辞書をメモリに展開するため、50MB程度のメモリ消費
- GC: Unityの増分GCを活用してスパイクを軽減

### 6.4 最適化の方向性

本プロジェクトで独自実装する場合の最適化指針:

1. **Span<T>/Memory<T>の活用**: 文字列コピーの削減
2. **辞書のメモリマップ**: `MemoryMappedFile`による辞書アクセス
3. **オブジェクトプール**: ノード・パスオブジェクトの再利用でGC負荷を軽減
4. **辞書のReadOnlyMemory化**: 辞書データを不変として扱い、安全な共有を実現

---

## 7. 総合評価と推奨事項

### 7.1 NMeCab vs MeCab.DotNet 比較まとめ

| 比較軸 | NMeCab (LibNMeCab) | MeCab.DotNet |
|--------|-------------------|--------------|
| 成熟度 | 高い（長い歴史） | 高い（NMeCabベース） |
| プラットフォーム | .NET Standard 2.0 | .NET Standard 1.3+ |
| 辞書管理 | 別途パッケージ | 同梱（便利） |
| API設計 | 新しいAPI（v0.10+） | NMeCab + 追加メソッド |
| 辞書種別対応 | IPA/UniDic 2.1/2.2 | IPADIC |
| Unity適合性 | DLL手動配置が必要 | DLL+辞書の手動配置が必要 |
| ライセンス | GPL/LGPL | GPL/LGPL |
| 活発さ | 低頻度更新 | 低頻度更新 |

### 7.2 本プロジェクト（dot-net-g2p）への推奨

**短期的（プロトタイプ段階）:**
- MeCab.DotNet（またはLibNMeCab）をそのまま使用してプロトタイプ開発
- naist-jdic辞書を`mecab-dict-index`でコンパイルしてカスタム辞書として使用
- Feature文字列からアクセント情報を解析するカスタムノードクラスを実装

**中長期的（本番実装）:**
- ライセンスの自由度を確保するため、MeCab互換のC#形態素解析エンジンを独自実装
  - ダブル配列Trie（DARTS互換）の実装
  - ビタビデコーディングの実装
  - MeCabバイナリ辞書の読み込み機能
- naist-jdic辞書（BSD）+ 独自エンジン（BSD）の組み合わせで完全BSDライセンスを実現
- Unity固有の最適化（メモリ管理、辞書ロード方式）を適用

**辞書戦略:**
- naist-jdic（OpenJTalk用拡張版）をプライマリ辞書として採用
- BSD Licenseで自由に再配布可能
- アクセント情報（フィールド14, 15）がG2Pに必須
- 辞書サイズの最適化（不要エントリの削除、圧縮）を検討

---

## 参考リンク

- [NMeCab GitHub](https://github.com/komutan/NMeCab)
- [MeCab.DotNet GitHub](https://github.com/kekyo/MeCab.DotNet)
- [MeCab公式サイト](https://taku910.github.io/mecab/)
- [DARTS (Double-ARray Trie System)](http://chasen.org/~taku/software/darts/)
- [darts-clone](https://github.com/s-yata/darts-clone)
- [jpreprocess/naist-jdic](https://github.com/jpreprocess/naist-jdic)
- [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)
- [MeCab ソースコードリーディング（クックパッド）](https://techlife.cookpad.com/entry/2016/05/11/170000)
- [LibNMeCab NuGet](https://www.nuget.org/packages/LibNMeCab)
- [MeCab.DotNet NuGet](https://www.nuget.org/packages/MeCab.DotNet)
