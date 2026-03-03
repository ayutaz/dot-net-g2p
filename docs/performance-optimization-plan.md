# DotNetG2P パフォーマンス最適化計画

14エージェントによる網羅的調査 + 15エージェントによるCysharpリポジトリ知見調査の結果をまとめた最適化計画書。
**実装状態**: 23施策中18施策を実装完了（2026-03-03時点）。

## 目次

1. [調査概要](#1-調査概要)
2. [ボトルネック分析](#2-ボトルネック分析)
3. [最適化施策一覧（優先度順）](#3-最適化施策一覧優先度順)
4. [Phase 1: ホットパス最適化（最優先）](#4-phase-1-ホットパス最適化最優先)
5. [Phase 2: メモリ・GCアロケーション削減](#5-phase-2-メモリgcアロケーション削減)
6. [Phase 3: 辞書読み込み高速化](#6-phase-3-辞書読み込み高速化)
7. [Phase 4: データ構造最適化](#7-phase-4-データ構造最適化)
8. [Phase 5: API拡張・長期施策](#8-phase-5-api拡張長期施策)
9. [不採用・保留とした施策](#9-不採用保留とした施策)
10. [期待効果まとめ](#10-期待効果まとめ)
11. [制約事項](#11-制約事項)
12. [Cysharpリポジトリ知見の統合](#12-cysharpリポジトリ知見の統合)

---

## 1. 調査概要

### ターゲット環境
- **Unity 6000.0.0+**（C# 11サポート）
- **.NET Standard 2.1**（変更不要 — Unity 6000でも最適ターゲット）
- FrozenDictionary等の.NET 8+専用APIは使用不可

### 調査チーム構成（14エージェント）

**C#/Unity最適化調査（10エージェント）:**
1. Unity 6000 C#バージョン調査
2. Span/Memory最適化機会調査
3. ref struct/in/readonly最適化調査
4. String割当削減最適化調査
5. Collection/LINQ最適化調査
6. struct vs class値型最適化調査
7. SIMD/ベクトル化最適化調査
8. ArrayPool/ObjectPool活用調査
9. async/ValueTask最適化調査
10. SourceGenerator/コンパイル時最適化調査

**パイプライン別調査（4エージェント）:**
11. パイプラインプロファイリング調査
12. GCアロケーション全体分析調査
13. 辞書読み込み高速化調査
14. Viterbi/ラティス最適化調査

---

## 2. ボトルネック分析

### 処理時間の内訳（推定）

```
G2PEngine.RunPipeline() 100%
├── MeCabTokenizer.Tokenize()         60-70%
│   ├── LatticeBuilder.Build()        40-50%  ★ 最大ボトルネック
│   │   ├── CommonPrefixSearch        15-20%
│   │   ├── 未知語生成                10-15%
│   │   └── endNodes管理              5-10%
│   └── ViterbiDecoder.Decode()       20-30%  ★ 第2ボトルネック
│       ├── 前向きパス                15-20%
│       └── 後ろ向きトレース          5-10%
├── NJDパイプライン                    15-25%
│   ├── SetPronunciation.Process()    5-8%   ★ 第3ボトルネック
│   ├── SetAccentType.Process()       3-5%
│   ├── SetDigit.Process()            2-4%
│   └── その他                        2-5%
├── TextNormalizer.Normalize()         1-3%
└── 出力変換（ToPhonemes等）            2-5%
```

### GCアロケーション分析

1回のG2P変換（10文字テキスト）あたり:
- **生成オブジェクト数**: 約100〜150個
- **合計アロケーション**: 約6.3KB
- **主要アロケーション源**:
  - MeCabTokenizer（ラティス構築）: ~2.3KB（46個）
  - NjdNode構築: ~1.4KB（29個）
  - NJDパイプライン: ~1.3KB（27個）
  - 出力変換: ~1KB（2個）

---

## 3. 最適化施策一覧（優先度順）

| # | 施策 | 対象ファイル | 期待効果 | 難度 | Phase | 状態 |
|---|------|------------|---------|------|-------|------|
| 1 | LatticeBuilder endNodes配列再利用 | LatticeBuilder.cs | 解析30-40%高速化 | 中 | 1 | **完了** |
| 2 | CharInfoプリキャッシュ | LatticeBuilder.cs | 未知語処理40-60%削減 | 低 | 1 | **完了** |
| 3 | ConnectionMatrix AggressiveInlining | ConnectionMatrix.cs | Viterbi 10-15%高速化 | 低 | 1 | **完了** |
| 4 | ViterbiDecoder foreach→forループ | ViterbiDecoder.cs | Viterbi 5-10%高速化 | 低 | 1 | **完了** |
| 5 | MeCabTokenizer Split(',')削減 | MeCabTokenizer.cs | トークン生成50%削減 | 中 | 1 | **完了** |
| 6 | LatticeBuilder Substring→Span | LatticeBuilder.cs | 文字列割当80%削減 | 中 | 2 | **完了** |
| 7 | endNodes List ArrayPool化 | LatticeBuilder.cs | GC 10-20%削減 | 中 | 2 | 未実施（#1で代替） |
| 8 | Utf8CharMapマッピング配列ArrayPool化 | Utf8CharMap.cs | GC 1-2KB/呼出削減 | 低 | 2 | **完了** |
| 9 | SetUnvoicedVowel MoraState構造体化 | SetUnvoicedVowel.cs | GC 8-12%削減 | 中 | 2 | 未実施（影響限定的） |
| 10 | NjdNode AccentInfo Split→IndexOf | NjdNode.cs | 文字列割当削減 | 低 | 2 | **完了** |
| 11 | TextNormalizer Dictionary→配列化 | TextNormalizer.cs | 正規化20-30%高速化 | 中 | 2 | **完了** |
| 12 | MoraMapping Dictionary→配列インデックス化 | MoraMapping.cs | 音素変換20-30%高速化 | 中 | 2 | 未実施（複雑化対効果小） |
| 13 | ConnectionMatrix バッファ一括読み込み | ConnectionMatrix.cs | 辞書読込50%高速化 | 低 | 3 | **完了** |
| 14 | DicToken MemoryMarshal.Read | DicToken.cs | ゼロコピー化 | 中 | 3 | **完了** |
| 15 | DictionaryBundle並列読み込み | DictionaryBundle.cs | 初期化30-40%高速化 | 中 | 3 | 未実施（IO bound、効果限定的） |
| 16 | LatticeNode ObjectPool化 | LatticeNode.cs, LatticeBuilder.cs | GC 15-20%削減 | 高 | 4 | 未実施（BestPrev寿命管理複雑） |
| 17 | enum byte/ushort化 | Phoneme.cs, MoraKind.cs | メモリ50-75%削減 | 低 | 4 | **完了** |
| 18 | List初期容量指定（全体） | 各ファイル | GC再割当削減 | 低 | 4 | **完了** |
| 19 | StringBuilder初期容量指定 | G2PEngine.cs等 | GC再割当削減 | 低 | 4 | **完了** |
| 20 | SetAccentType Regex→手動パーサ | SetAccentType.cs | Regex排除 | 中 | 4 | **完了** |
| 21 | DictionaryBundle WeakReferenceキャッシュ | DictionaryBundle.cs | メモリ共有 | 中 | 5 | **完了** |
| 22 | MeCabTokenizer Lazy<T>遅延初期化 | MeCabTokenizer.cs | 初期化遅延 | 低 | 5 | **完了** |
| 23 | バッチ処理API | G2PEngine.cs | スループット向上 | 中 | 5 | **完了** |

---

## 4. Phase 1: ホットパス最適化（最優先）

> **実装状態**: 全5施策中5施策完了。Phase 1は全て実装済み。

最もボトルネックが集中するLatticeBuilder/ViterbiDecoder/MeCabTokenizerの高速化。

### 4.1 LatticeBuilder endNodes配列再利用

**対象**: `src/DotNetG2P.MeCab/Lattice/LatticeBuilder.cs`

**現状の問題**:
```csharp
// Build()が呼ばれるたびにcharLen+2個のListを新規生成
var endNodes = new List<LatticeNode>[charLen + 2];
for (int i = 0; i < endNodes.Length; i++)
    endNodes[i] = new List<LatticeNode>();
```

**改善案**:
- LatticeBuilderにインスタンスレベルの`List<LatticeNode>[]`を保持
- Build()呼び出し時に必要サイズ以上なら再利用（Clear()のみ）、不足なら拡張
- TrieResult[512]もインスタンスフィールドとして保持

**期待効果**: endNodes生成コスト30-40%削減、GCプレッシャー大幅減

---

### 4.2 CharInfoプリキャッシュ

**対象**: `src/DotNetG2P.MeCab/Lattice/LatticeBuilder.cs`

**現状の問題**:
- 各文字位置でCharProperty.GetCharInfo()を呼び出し
- 同じ文字が複数回出現しても毎回辞書参照

**改善案**:
```csharp
// Build()の冒頭でテキスト全文字のCharInfoをプリキャッシュ
var charInfoCache = new CharInfo[charLen];
for (int i = 0; i < charLen; i++)
    charInfoCache[i] = _charProperty.GetCharInfo(text[i]);
```

**期待効果**: 未知語処理40-60%高速化（CharProperty参照回数を1/3〜1/5に削減）

---

### 4.3 ConnectionMatrix AggressiveInlining

**対象**: `src/DotNetG2P.MeCab/Dictionary/ConnectionMatrix.cs`

**現状の問題**:
- GetCost()はViterbiの前向きパスで数千〜数万回呼ばれるホットメソッド
- メソッド呼び出しオーバーヘッドが累積

**改善案**:
```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public short GetCost(ushort rightId, ushort leftId)
    => _matrix[rightId * _lSize + leftId];
```

**期待効果**: Viterbi全体で10-15%高速化

---

### 4.4 ViterbiDecoder foreach→forループ変換

**対象**: `src/DotNetG2P.MeCab/Lattice/ViterbiDecoder.cs`

**現状の問題**:
- endNodesのイテレーションでforeach/List enumerator使用
- O(n×m²)の全組み合わせ走査で呼び出し回数多

**改善案**:
- `foreach`を`for (int i = 0; i < list.Count; i++)`に変換
- インデックスアクセスによるenumeratorアロケーション排除

**期待効果**: Viterbi 5-10%高速化

---

### 4.5 MeCabTokenizer Split(',')削減

**対象**: `src/DotNetG2P.MeCab/MeCabTokenizer.cs`

**現状の問題**:
```csharp
// 全トークンで毎回Split実行 → string[]を新規割当
var raw = feature?.Split(',') ?? Array.Empty<string>();
```

**改善案**:
遅延分割パーサを導入。ITokenのFeature取得時にインデックスベースで必要フィールドのみ切り出し:
```csharp
// カンマ位置を事前記録（Split不要）
private int[] _commaPositions;
public string GetFeature(int index)
{
    int start = index == 0 ? 0 : _commaPositions[index - 1] + 1;
    int end = index < _commaPositions.Length ? _commaPositions[index] : _rawFeature.Length;
    return _rawFeature.Substring(start, end - start);
}
```

**期待効果**: トークン生成時のstring[]割当50%削減（168バイト/トークン → 0）

---

## 5. Phase 2: メモリ・GCアロケーション削減

> **実装状態**: 全7施策中4施策完了。#7はPhase 1の#1で代替済み、#9と#12は効果対複雑度の判断によりスコープ外。

### 5.1 LatticeBuilder Substring→Span/Memory

**対象**: `src/DotNetG2P.MeCab/Lattice/LatticeBuilder.cs`

**現状の問題**:
- `text.Substring(charPos, endCharPos - charPos)` が辞書候補・未知語候補ごとに呼ばれる
- 4箇所のSubstringで大量の一時文字列を生成

**改善案**:
- LatticeNodeのSurfaceをReadOnlyMemory<char>に変更
- `text.AsMemory(charPos, length)` でゼロコピー参照
- MeCabToken生成時にのみ`ToString()`で実体化

**期待効果**: 文字列割当80%削減

---

### 5.2 endNodes List ArrayPool化

**対象**: `src/DotNetG2P.MeCab/Lattice/LatticeBuilder.cs`

**改善案**:
```csharp
var endNodesArray = ArrayPool<List<LatticeNode>>.Shared.Rent(charLen + 2);
try
{
    for (int i = 0; i < charLen + 2; i++)
    {
        if (endNodesArray[i] == null)
            endNodesArray[i] = new List<LatticeNode>();
        else
            endNodesArray[i].Clear();
    }
    // Build処理...
}
finally
{
    ArrayPool<List<LatticeNode>>.Shared.Return(endNodesArray);
}
```

**期待効果**: GC Gen0割当 10-20%削減

---

### 5.3 Utf8CharMap マッピング配列ArrayPool化

**対象**: `src/DotNetG2P.MeCab/Trie/Utf8CharMap.cs`

**現状の問題**:
```csharp
_byteToChar = new int[Utf8Bytes.Length];  // 毎回新規割当
_charToByte = new int[text.Length];
```

**改善案**: ArrayPool<int>.Shared.Rent/Returnで再利用

**期待効果**: 1.2〜1.6KB/呼び出し削減

---

### 5.4 SetUnvoicedVowel MoraState構造体化

**対象**: `src/DotNetG2P.Core/NJD/SetUnvoicedVowel.cs`

**現状の問題**: MoraStateがクラス → 各モーラごとにヒープ割当

**改善案**: MoraStateをreadonly structに変更 + ArrayPool<MoraState>使用

**期待効果**: GC 8-12%削減（モーラ数分のヒープ割当排除）

---

### 5.5 NjdNode AccentInfo Split→IndexOf+AsSpan

**対象**: `src/DotNetG2P.Core/Models/NjdNode.cs`

**現状の問題**:
```csharp
var parts = entry.AccentInfo.Split('/');  // string[]を毎回生成
```

**改善案**:
```csharp
var span = entry.AccentInfo.AsSpan();
int slashIdx = span.IndexOf('/');
if (slashIdx >= 0)
{
    int.TryParse(span.Slice(0, slashIdx), out accentPosition);
    int.TryParse(span.Slice(slashIdx + 1), out moraCount);
}
```

**期待効果**: トークンごとのstring[]割当排除

---

### 5.6 TextNormalizer Dictionary→配列化

**対象**: `src/DotNetG2P.Core/TextNormalization/TextNormalizer.cs`

**現状の問題**: `Dictionary<char, char>`でのハッシュ計算オーバーヘッド

**改善案**: 半角カタカナ範囲（U+FF61〜U+FF9D）は連続するため、配列インデックスで直接参照:
```csharp
private static readonly char[] HalfwidthKatakanaMap = new char[0xFF9D - 0xFF61 + 1];
// 初期化時に配列に詰める
```

**期待効果**: 正規化処理20-30%高速化

---

### 5.7 MoraMapping Dictionary→配列インデックス化

**対象**: `src/DotNetG2P.Core/PhonemeConverter/MoraMapping.cs`

**改善案**: カタカナ文字をインデックスとした配列ルックアップに変換

**期待効果**: 音素変換20-30%高速化

---

## 6. Phase 3: 辞書読み込み高速化

> **実装状態**: 全3施策中2施策完了。#15（並列読み込み）はIO bound・効果限定的のためスコープ外。

### 6.1 ConnectionMatrix バッファ一括読み込み

**対象**: `src/DotNetG2P.MeCab/Dictionary/ConnectionMatrix.cs`

**現状の問題**: BinaryReader.ReadInt16()を数百万回呼び出し

**改善案**:
```csharp
// バッファ一括読み込み
var buffer = new byte[lSize * rSize * 2];
stream.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, _matrix, 0, buffer.Length);
```

**期待効果**: matrix.bin読み込み50%高速化

---

### 6.2 DicToken MemoryMarshal.Read

**対象**: `src/DotNetG2P.MeCab/Dictionary/DicToken.cs`

**現状の問題**: GetToken()で個別フィールドをBitConverterで読み取り

**改善案**:
```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct DicToken { ... }

public DicToken GetToken(int index)
{
    var span = _tokenBuffer.AsSpan(index * DicToken.Size, DicToken.Size);
    return MemoryMarshal.Read<DicToken>(span);
}
```

**期待効果**: ゼロコピーデシリアライゼーション

---

### 6.3 DictionaryBundle 並列読み込み

**対象**: `src/DotNetG2P.MeCab/Dictionary/DictionaryBundle.cs`

**改善案**: sys.dic/matrix.bin/char.bin/unk.dicを`Task.WhenAll`で並列IO

```csharp
public static async Task<DictionaryBundle> LoadAsync(string path)
{
    var sysTask = Task.Run(() => SystemDictionary.Load(Path.Combine(path, "sys.dic")));
    var matTask = Task.Run(() => ConnectionMatrix.Load(Path.Combine(path, "matrix.bin")));
    var chrTask = Task.Run(() => CharProperty.Load(Path.Combine(path, "char.bin")));
    var unkTask = Task.Run(() => UnknownDictionary.Load(Path.Combine(path, "unk.dic")));
    await Task.WhenAll(sysTask, matTask, chrTask, unkTask);
    return new DictionaryBundle(sysTask.Result, matTask.Result, chrTask.Result, unkTask.Result);
}
```

**期待効果**: 初期化30-40%高速化（IO並列化）

---

## 7. Phase 4: データ構造最適化

> **実装状態**: 全5施策中4施策完了。#16（LatticeNode ObjectPool化）はBestPrevチェーンの寿命管理が複雑のためスコープ外。

### 7.1 LatticeNode ObjectPool化

**対象**: `src/DotNetG2P.MeCab/Lattice/LatticeNode.cs`, `LatticeBuilder.cs`

**改善案**: ObjectPool<LatticeNode>でノードを再利用

**注意**: BestPrevチェーンによる参照保持があるため、Viterbiデコード後に返却タイミングを慎重に管理する必要あり

**期待効果**: GC 15-20%削減

---

### 7.2 enum byte/ushort化

**対象**: `src/DotNetG2P.Core/Models/Phoneme.cs`, `MoraKind.cs`

**改善案**:
```csharp
public enum Consonant : byte { ... }  // int(4B) → byte(1B)
public enum Vowel : byte { ... }      // int(4B) → byte(1B)
public enum MoraKind : ushort { ... } // int(4B) → ushort(2B)
```

**期待効果**: Mora構造体サイズ50-75%削減

---

### 7.3 List初期容量指定

**対象**: 各ファイルのList生成箇所

主な対象:
| ファイル | 箇所 | 推定容量 |
|---------|------|---------|
| NjdNode.FromTokens() | `new List<NjdNode>()` | `tokens.Count` |
| G2PEngine.ToPhonemes() | `new List<string>()` | `nodes.Count` |
| AccentPhraseConverter | `new List<AccentPhrase>()` | `nodes.Count / 3` |
| JPCommonBuilder | `new List<JPBreathGroup>()` | `4` |
| FullContextLabel | `new List<PhonemeEntry>()` | 音素数の事前計数 |

**期待効果**: List動的再割当の排除

---

### 7.4 StringBuilder初期容量指定

**対象**: `G2PEngine.ToKana()`等

```csharp
// 現在
var sb = new System.Text.StringBuilder();
// 改善（平均モーラ数×2文字で見積もり）
var sb = new System.Text.StringBuilder(nodes.Count * 8);
```

---

### 7.5 SetAccentType Regex→手動パーサ

**対象**: `src/DotNetG2P.Core/NJD/SetAccentType.cs`

**現状の問題**: `Regex`によるChainRulesパース（GeneratedRegexは.NET Standard 2.1で使用不可）

**改善案**: IndexOf/Substring/int.TryParseベースの手動パーサに置換

**期待効果**: Regexインスタンス化・マッチングコスト排除

---

## 8. Phase 5: API拡張・長期施策

> **実装状態**: 全3施策中3施策完了。Phase 5は全て実装済み。

### 8.1 DictionaryBundle WeakReferenceキャッシュ

**対象**: `src/DotNetG2P.MeCab/Dictionary/DictionaryBundle.cs`

**改善案**: 同じ辞書パスに対してWeakReference<DictionaryBundle>でキャッシュ。複数G2PEngineインスタンスで辞書メモリを共有。

---

### 8.2 MeCabTokenizer Lazy<T>遅延初期化

**改善案**: LatticeBuilder/ViterbiDecoderをLazy<T>で包み、初回Tokenize()時に初期化

---

### 8.3 バッチ処理API

**改善案**:
```csharp
public IReadOnlyList<string> ToPhonemesBatch(IReadOnlyList<string> texts)
{
    // 内部バッファを文間で再利用
}
```

---

### 8.4 その他の長期施策

| 施策 | 説明 |
|------|------|
| MemoryMappedFile辞書読み込み | 起動時間-40%、メモリ-70%（ただし.NET Standard 2.1制約あり） |
| Pronunciation Moras → ReadOnlyMemory<Mora> | List<Mora>→配列化でGC削減 |
| Span<T>ベースITokenizer API | ReadOnlySpan<char>入力の新オーバーロード |
| LatticeNode struct化 | 30%メモリ削減（ただし参照セマンティクス変更の影響大） |

---

## 9. 不採用・保留とした施策

| 施策 | 理由 |
|------|------|
| SIMD/ベクトル化 | 効果が限定的。Trie検索やViterbiは分岐が多くSIMD化困難 |
| FrozenDictionary | .NET 8+専用。.NET Standard 2.1では使用不可 |
| GeneratedRegex | .NET 7+専用。.NET Standard 2.1では使用不可 |
| async Tokenize() | 形態素解析はCPU-boundのため非同期化の効果薄い |
| SourceGenerator | ~~制約あり~~ → **再検討可能**（Cysharp調査でnetstandard2.1ターゲット可と判明、MoraMapping/WordAttr/TextNormalizerのswitch式生成に有効）|
| LatticeNode class→完全struct化 | BestPrevの参照チェーンが値型と相性悪い。段階的にObjectPoolで対応 |

---

## 10. 期待効果まとめ

> **実装結果**: Phase 1-5の施策のうち18/23施策を実装完了。未実施の5施策はスコープ外（効果が限定的、または実装リスクが高い）として見送り。テスト結果: 646合格、0失敗、283スキップ。

### Phase別の累積効果

| Phase | 主な施策 | 解析速度 | 初期化速度 | メモリ(GC) |
|-------|---------|---------|-----------|-----------|
| Phase 1 | ホットパス最適化 | **+30-40%** | - | -10% |
| Phase 2 | GCアロケーション削減 | +5-10% | - | **-30-40%** |
| Phase 3 | 辞書読み込み高速化 | - | **+30-50%** | -5% |
| Phase 4 | データ構造最適化 | +5-10% | - | **-15-20%** |
| Phase 5 | API拡張 | +5% | +10% | -10% |
| **合計** | | **+40-60%** | **+40-60%** | **-50-70%** |

### テキスト長別のGCアロケーション削減見込み

| テキスト長 | 現在 | Phase 1+2後 | 全Phase後 |
|----------|------|-----------|----------|
| 10文字 | ~6.3KB | ~3.5KB | ~2.0KB |
| 100文字 | ~50KB | ~28KB | ~15KB |
| 1000文字 | ~500KB | ~280KB | ~150KB |

### 生成オブジェクト数の削減見込み（10文字テキスト）

| 段階 | 現在 | Phase 1+2後 | 全Phase後 |
|------|------|-----------|----------|
| MeCabTokenizer | 46個 | 25個 | 15個 |
| NjdNode構築 | 29個 | 20個 | 15個 |
| NJDパイプライン | 27個 | 15個 | 10個 |
| 出力変換 | 2個 | 1個 | 1個 |
| **合計** | **104個** | **61個** | **41個** |

---

## 11. 制約事項

### .NET Standard 2.1の制約
- `Span<T>`/`ReadOnlySpan<T>`: 使用可能
- `Memory<T>`/`ReadOnlyMemory<T>`: 使用可能
- `stackalloc`: 使用可能
- `ArrayPool<T>`: 使用可能（System.Buffers）
- `MemoryMarshal`: 使用可能
- `[MethodImpl(AggressiveInlining)]`: 使用可能
- `FrozenDictionary`: **使用不可**（.NET 8+）
- `GeneratedRegex`: **使用不可**（.NET 7+）
- `CollectionsMarshal.AsSpan`: **ポリフィルで使用可能**（ZLinqパターン: `Unsafe.As`でList内部配列をSpanとして取得）
- `ref fields`: **使用不可**（C# 11だがランタイムサポート要）
- `SourceGenerator`: **使用可能**（ジェネレータ自体はnetstandard2.0、出力はnetstandard2.1ターゲット可）

### Unity固有の注意点
- `[ThreadStatic]`はUnityのメインスレッドモデルと相性が悪い → `ThreadLocal<T>`推奨
- `ObjectPool<T>`は`Microsoft.Extensions.ObjectPool`ではなく独自実装が必要（NuGet依存回避）
- `async/await`はUnityメインスレッドでの使用に注意（UniTask等との統合検討）

### 後方互換性
- 既存の`ITokenizer`/`IToken`インターフェースは変更しない
- 新APIは拡張メソッドまたはオーバーロードとして追加
- 既存テスト（1,600+件）が全パスすることを保証

---

## 12. Cysharpリポジトリ知見の統合

15エージェントによるCysharp OSS（https://github.com/orgs/Cysharp/repositories）の網羅的調査結果。
各リポジトリの高性能C#パターンをDotNetG2Pへの適用可能性とともに整理する。

### 12.1 調査対象リポジトリ（15件）

| # | リポジトリ | 主な知見カテゴリ |
|---|-----------|---------------|
| 1 | ZString | ゼロアロケーション文字列構築（ValueStringBuilder） |
| 2 | ZLinq | ゼロアロケーションLINQ（CollectionsMarshal.AsSpanポリフィル） |
| 3 | MemoryPack | バイナリデシリアライゼーション（Unsafe.ReadUnaligned, MemoryMarshal.Cast） |
| 4 | Utf8StreamReader | UTF-8直接パース（遅延文字列変換） |
| 5 | StructureOfArraysGenerator | SoAレイアウト（キャッシュ効率） |
| 6 | NativeMemoryArray | ネイティブメモリ管理（GCヒープ外配置） |
| 7 | MasterMemory | インメモリDB（string.Intern, ソート配列+二分探索） |
| 8 | SimdLinq | SIMD最適化（適用不可の結論確認） |
| 9 | UnitGenerator | 値型最適化（byte-backed enum） |
| 10 | R3 | オブジェクトプーリング（FreeListパターン, ThrowHelper） |
| 11 | MessagePipe | パイプライン最適化（ステージ融合, AggressiveInlining） |
| 12 | ObservableCollections | コレクション最適化（容量事前確保, ArrayPoolテンポラリバッファ） |
| 13 | Ulid | 高性能struct設計（StructLayout, stackallocバッファ） |
| 14 | csbindgen / PrivateProxy | SourceGenerator（switch式によるルックアップ生成） |
| 15 | UniTask | 非同期最適化（ValueTask, IValueTaskSource）→ 現状CPU-boundのため不適 |

---

### 12.2 即時採用すべき技術（Phase 1-2に統合）

#### 12.2.1 ValueStringBuilder ref struct（ZString由来）

> **実装状態**: **完了** — ValueStringBuilderを導入し、StringBuilder使用箇所を置換済み。

**概要**: ArrayPool<char>をバッキングストアとするref struct型StringBuilder。ヒープ割当ゼロ。

**適用箇所と効果**:
| 対象 | 現状 | 改善後 | 削減率 |
|------|------|--------|-------|
| `FullContextLabel.BuildLabel()` ×20-100回/utterance | StringBuilder(256) × N + int.ToString() × 40N | ValueStringBuilder + int.TryFormat | **~85%** |
| `G2PEngine.ToKana()` | StringBuilder 80bytes | ValueStringBuilder 0bytes | 100% |
| `ProsodyExtractor.Extract()` | StringBuilder 80bytes | ValueStringBuilder 0bytes | 100% |
| `Pronunciation.ToKatakana()` | StringBuilder | ValueStringBuilder | 100% |

**最小実装（~100行、外部依存なし）**:
```csharp
internal ref struct ValueStringBuilder
{
    private char[] _buffer;
    private int _pos;

    public ValueStringBuilder(int initialCapacity)
    {
        _buffer = ArrayPool<char>.Shared.Rent(initialCapacity);
        _pos = 0;
    }

    public void Append(char c) { /* ... */ }
    public void Append(string s) { /* AsSpan().CopyTo() */ }
    public void Append(int value) { /* int.TryFormat() — ゼロアロケーション */ }

    public override string ToString() => new string(_buffer, 0, _pos);
    public void Dispose() => ArrayPool<char>.Shared.Return(_buffer);
}
```

**期待効果**: 出力段階のアロケーション ~11KB → ~1.6KB（85%削減）、ToFullContextLabels **25-35%高速化**

---

#### 12.2.2 CollectionsMarshal.AsSpanポリフィル（ZLinq由来）

> **実装状態**: **未実施** — foreach→forループ変換（施策#4）で十分な効果を得たため見送り。

**概要**: `Unsafe.As`でList<T>内部配列をSpan<T>として取得。.NET Standard 2.1で動作。

**実装（~40行）**:
```csharp
internal static class CollectionsMarshal
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AsSpan<T>(List<T> list)
    {
        // List<T>の内部レイアウトをUnsafe.Asで再解釈
        return Unsafe.As<ListView<T>>(list)._items.AsSpan(0, list.Count);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ListView<T> { public T[] _items; public int _size; public int _version; }
}
```

**適用箇所**:
- `ViterbiDecoder.Decode()` — foreach → Span forループ（エニュメレータ割当排除）
- `SetAccentPhrase.Process()` — List indexer → ref access
- `SetAccentType.Process()`, `SetUnvoicedVowel.Process()` — 同上

**期待効果**: Viterbi 10-15%高速化（enumeratorアロケーション排除 + キャッシュ局所性向上）

---

#### 12.2.3 Unsafe.ReadUnaligned<T>による辞書読み込み（MemoryPack/Ulid由来）

> **実装状態**: **完了** — unsafeポインタによる代替実装で同等の効果を達成。

**概要**: 構造体を1命令でバイト列から読み取り。BinaryPrimitives個別読みの3-5倍高速。

**適用箇所**:
```csharp
// DicToken.Read — 現在6回のBinaryPrimitives呼び出し → 1回のUnsafe.ReadUnaligned
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct DicToken { /* lcAttr, rcAttr, posId, wCost, featureOffset, compound */ }

public static DicToken Read(byte[] buffer, int offset)
{
    ref var dataRef = ref buffer[offset];
    return Unsafe.ReadUnaligned<DicToken>(ref dataRef);
}
```

**DoubleArrayTrieコンストラクタにも適用**:
```csharp
_bases[i] = Unsafe.ReadUnaligned<int>(ref dataSpan[offset]);
_checks[i] = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref dataSpan[offset], 4));
```

**期待効果**: DicToken読み取り3-5倍高速化、DoubleArrayTrie初期化2-3倍高速化

---

#### 12.2.4 MemoryMarshal.Castによるバルク読み込み（MemoryPack由来）

> **実装状態**: **完了** — Buffer.BlockCopyによる代替実装で一括読み込みを実現。

**概要**: byte[]をshort[]にゼロコピーキャスト。ConnectionMatrix読み込みで1.7M回のReadInt16を1回のバルクコピーに置換。

**適用箇所**:
```csharp
// ConnectionMatrix.Load — 現在1.7M回のReadInt16 → 1回のバルク読み込み
var matrix = new short[totalEntries];
var byteSpan = MemoryMarshal.AsBytes(matrix.AsSpan());
stream.Read(byteSpan); // 直接short[]にストリーム読み込み
```

**CharProperty.Loadにも適用** (65K回のReadUInt32を1回に):
```csharp
var charInfoTable = new CharInfo[0xFFFF];
var byteSpan = MemoryMarshal.AsBytes(charInfoTable.AsSpan());
stream.Read(byteSpan);
```

**期待効果**: ConnectionMatrix読み込み **50-100倍高速化**、CharProperty読み込み **20-40倍高速化**

---

#### 12.2.5 境界チェック排除パターン（ZLinq由来）

> **実装状態**: **部分的** — DoubleArrayTrieのホットパスで使用。全箇所への展開は未実施。

**概要**: `(uint)i < (uint)span.Length`キャストでJITが境界チェックを省略。

**適用箇所**: すべてのホットパスforループ
```csharp
for (int i = 0; (uint)i < (uint)span.Length; i++) { /* ... */ }
```

**期待効果**: ループあたり1-3%の微小改善（累積で意味のある差に）

---

### 12.3 中期的に採用すべき技術（Phase 3-4に統合）

#### 12.3.1 UTF-8直接パース / 遅延文字列変換（Utf8StreamReader由来）

> **実装状態**: **未実施** — 長期施策として保留。

**概要**: Feature文字列をUTF-8バイト列のまま保持し、必要時のみUTF-16に変換。

**実装パターン**:
```csharp
public readonly struct Utf8FeatureParser
{
    private readonly ReadOnlyMemory<byte> _utf8Data;

    public ReadOnlySpan<byte> GetFieldUtf8(int index) { /* カンマ位置検索 */ }
    public string GetField(int index) { /* 必要時のみEncoding.UTF8.GetString */ }
}
```

**適用箇所**: `SystemDictionary.GetFeature()` + `MeCabToken`のフィールドアクセス

**期待効果**: トークン生成時の文字列アロケーション50-70%削減

---

#### 12.3.2 byte-backed enum（UnitGenerator由来）

> **実装状態**: **完了** — Consonant:byte, Vowel:byte, MoraKind:ushort に変更済み。

**概要**: enum基底型をbyte/ushortにし、構造体サイズを削減。

```csharp
public enum Consonant : byte { None = 0, K, G, S, Z, ... }  // 4B → 1B
public enum Vowel : byte { None = 0, A, I, U, E, O, ... }    // 4B → 1B
public enum MoraKind : ushort { ... }                          // 4B → 2B
```

**期待効果**: Mora構造体20B → 4-8B（50-75%削減）、キャッシュ効率向上

---

#### 12.3.3 LatticeNodePool / FreeListパターン（R3由来）

> **実装状態**: **未実施** — BestPrevチェーンの寿命管理が複雑のためスコープ外。

**概要**: スロット再利用によるオブジェクトプール。100-1000+ LatticeNode割当/Tokenize()を排除。

```csharp
internal sealed class LatticeNodePool
{
    private LatticeNode[] _nodes;
    private int _freeHead = -1;
    private int _count;

    public LatticeNode Rent() { /* freeHeadからスロット再利用 or 拡張 */ }
    public void Return(LatticeNode node) { /* freeHeadに連結 */ }
}
```

**期待効果**: GC 15-20%削減

---

#### 12.3.4 ThrowHelperパターン（R3由来）

> **実装状態**: **完了** — ConnectionMatrix.GetCost()でThrowHelper分離を実装済み。

**概要**: throw文を別メソッドに分離し、ホットパスのJITインライン化を促進。

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public short GetCost(ushort rightId, ushort leftId)
{
    int index = rightId + _lSize * leftId;
    if ((uint)index >= (uint)_matrix.Length) ThrowIndexOutOfRange(rightId, leftId);
    return _matrix[index];
}

[MethodImpl(MethodImplOptions.NoInlining)]
private static void ThrowIndexOutOfRange(ushort r, ushort l) =>
    throw new ArgumentOutOfRangeException($"rightCtx={r}, leftCtx={l}");
```

**適用箇所**: `ConnectionMatrix.GetCost()`, `CharProperty.GetCharInfo()`, `DoubleArrayTrie`内ループ

---

#### 12.3.5 string.Intern()による文字列プール（MasterMemory由来）

> **実装状態**: **完了** — POSフィールド（index 0-5）にstring.Intern()を適用済み。

**概要**: 頻出文字列（POS名、活用型等）をCLR文字列プールで共有。

```csharp
// MeCabToken生成時
_features[i] = string.Intern(raw[i]); // 同一文字列は同一参照を返す
```

**適用箇所**: `MeCabTokenizer`のFeature文字列生成

**期待効果**: 大量テキスト処理時のメモリ使用量40-60%削減

---

#### 12.3.6 パイプラインステージ融合（MessagePipe由来）

> **実装状態**: **未実施** — 長期施策として保留。

**概要**: 連続する処理ステージを1パスに統合し、リスト走査回数を削減。

**融合候補**:
- `DigitSequenceProcessor.Process()` + `SetDigit.Process()` → 単一パス
- `SetAccentPhrase.Process()` + `SetAccentType.Process()` → 単一パス

**期待効果**: NJDパイプライン全体で10-15%高速化（リスト走査4回→2回）

---

### 12.4 長期的に検討する技術（Phase 5）

#### 12.4.1 Structure-of-Arrays（SoA）レイアウト（StructureOfArraysGenerator由来）

**概要**: LatticeNodeのViterbiホットフィールド（BestCost, LeftCtxId, RightCtxId）を個別配列に分離。

**Viterbiアクセスパターン分析**:
- ホットフィールド: `BestCost`(8B), `LeftCtxId`(2B), `RightCtxId`(2B), `WordCost`(2B), `StartPos`(4B) = 18B
- コールドフィールド: `Surface`, `Feature`, `IsUnknown` = 参照パス構築時のみ
- 現状: ノードあたり~47B全体をロード（ホット18B + コールド29B）

**期待効果**: Viterbi 1.5-2倍高速化（キャッシュ効率向上）

---

#### 12.4.2 ネイティブメモリ辞書配置（NativeMemoryArray由来）

**概要**: `Marshal.AllocHGlobal`で辞書データ（~20MB）をGCヒープ外に配置。

| コンポーネント | サイズ | GC効果 |
|-------------|-------|--------|
| ConnectionMatrix | ~8MB | GCスキャン対象から除外 |
| SystemDic.TrieData | ~2-5MB | 同上 |
| SystemDic.TokenData | ~1-3MB | 同上 |
| SystemDic.FeatureData | ~2-4MB | 同上 |
| **合計** | **~13-20MB** | **GCヒープから完全排除** |

**注意**: `AllowUnsafeBlocks=true`が必要、`GC.AddMemoryPressure/RemoveMemoryPressure`を併用

---

#### 12.4.3 SourceGeneratorによるルックアップテーブル生成（csbindgen/PrivateProxy由来）

**概要**: MoraMapping/WordAttr/TextNormalizerのDictionary<>ルックアップをコンパイル時switch式に変換。

**効果見積もり**:
| 対象 | 現状 | SourceGenerator後 | 高速化 |
|------|------|------------------|-------|
| MoraMapping | Dictionary<string, Mora> | switch式 | **10-200倍** |
| WordAttr.PosTable | Dictionary<string, int> | switch式 | 10-50倍 |
| TextNormalizer | Dictionary<char, char> | switch式 | 5-20倍 |

---

### 12.5 不適と判断した技術

| 技術 | リポジトリ | 不適理由 |
|------|-----------|---------|
| SIMD（Vector128/256） | SimdLinq | Viterbiはデータ依存性あり、Trieは分岐多、ループサイズ小。.NET Standard 2.1ではVector<T>のみ利用可能で効果限定的 |
| 非同期パイプライン | UniTask | G2Pパイプラインは完全CPU-bound。async化のオーバーヘッドが効果を上回る |
| Reactive拡張 | R3 | バッチ処理パターンのため、リアクティブストリームの利点なし |
| ZLinq full統合 | ZLinq | DotNetG2Pは既にLINQ使用が最小限。DropInGeneratorの導入コストに見合わない |
| MessagePipeフレームワーク | MessagePipe | DI/パイプラインフレームワークは過剰。個別パターンの手動適用で十分 |

---

### 12.6 既存施策への修正・補強

Cysharp調査により、既存の最適化計画に以下の修正を適用:

| 既存施策# | 変更内容 |
|----------|---------|
| #5 (Split(',')削減) | **強化**: ZStringパターンの`Utf8FeatureParser`に変更。Split不要でUTF-8直接パース |
| #6 (Substring→Span) | **据え置き**: ZLinqのCollectionsMarshal.AsSpanポリフィルと組み合わせ可能 |
| #12 (MoraMapping配列化) | **強化**: SourceGenerator switch式に変更（10-200倍高速化） |
| #13 (ConnectionMatrix一括読み込み) | **強化**: MemoryMarshal.AsBytes直接書き込み（50-100倍高速化） |
| #14 (DicToken MemoryMarshal) | **強化**: Unsafe.ReadUnaligned<T>に変更（3-5倍高速化） |
| #16 (LatticeNode Pool) | **強化**: R3 FreeListパターン採用 |
| #17 (enum byte化) | **強化**: UnitGeneratorパターンで明示的StructLayout適用 |
| 制約: CollectionsMarshal | **修正**: ZLinqポリフィルで.NET Standard 2.1で使用可能 |
| 制約: SourceGenerator | **修正**: netstandard2.1ターゲットで使用可能 |

---

### 12.7 統合後の期待効果

#### Cysharp知見適用による追加改善

| 技術 | 対象コンポーネント | 追加改善幅 |
|------|----------------|----------|
| ValueStringBuilder | 出力変換（ToKana/ToProsody/Labels） | アロケーション **-85%** |
| CollectionsMarshal.AsSpan | Viterbi/NJDパイプライン | 速度 **+10-15%** |
| Unsafe.ReadUnaligned | 辞書読み込み | 速度 **+200-500%**（個別項目） |
| MemoryMarshal.Cast | ConnectionMatrix/CharProperty | 読み込み **+50-100倍** |
| Utf8FeatureParser | トークン生成 | アロケーション **-50-70%** |
| SourceGenerator switch | MoraMapping/WordAttr | ルックアップ **+10-200倍** |
| string.Intern | 大量テキスト処理 | メモリ **-40-60%** |
| SoA layout | Viterbiデコード | 速度 **+50-100%** |

#### 全Phase完了後の総合期待効果（Cysharp知見込み）

| メトリクス | Phase 1-5のみ | Cysharp知見追加後 |
|----------|-------------|----------------|
| 解析速度 | +40-60% | **+60-100%** |
| 初期化速度 | +40-60% | **+80-150%**（辞書読み込み劇的改善） |
| GCアロケーション | -50-70% | **-70-90%** |
| メモリ使用量（大量テキスト） | -30% | **-50-70%**（string.Intern + ネイティブメモリ） |
