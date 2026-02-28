# C#/.NET/Unity向け日本語処理ライブラリ調査

## 調査日: 2026-02-28

本ドキュメントでは、C#/.NET/Unity環境で利用可能な日本語処理ライブラリを網羅的に調査した結果をまとめる。

---

## 1. 形態素解析ライブラリ

### 1.1 LibNMeCab (NMeCab)

- **リポジトリ**: https://github.com/komutan/NMeCab
- **NuGetパッケージ**:
  - `LibNMeCab` (v0.10.2) - 本体のみ（辞書別途）
  - `LibNMeCab.IpaDicBin` (v0.10.0) - IPAdic辞書バイナリ
  - `NMeCab` (v0.6.4) - 旧バージョン
  - `NMeCabNetStandard` (v0.7.3) - 非公式.NET Standard移植
- **ターゲット**: .NET Standard 2.0（.NET 5+、.NET Core 2.0+、.NET Framework 4.6.1+対応）
- **ライセンス**: GPL-2.0-or-later / LGPL-2.1-or-later（デュアルライセンス）
- **GitHub Stars**: 98
- **Unity対応**: 可能（LibNMeCab.dllをPluginsフォルダに配置する手順の記事あり）
- **メンテナンス**: 最終更新2024年1月頃。125コミット
- **特徴**:
  - MeCabのC#ネイティブ再実装（C/C++バイナリ不要）
  - 辞書はIPAdicバイナリ形式（naist-jdicも同形式のため互換性あり）
  - 純粋なマネージドコードで動作
- **G2Pプロジェクトとの関連**: 形態素解析の基盤として最有力候補。naist-jdic辞書を使用することでアクセント情報も取得可能

### 1.2 MeCab.DotNet

- **リポジトリ**: https://github.com/kekyo/MeCab.DotNet
- **NuGetパッケージ**: `MeCab.DotNet` (v1.2.0)
- **ターゲット**: .NET Standard 2.1〜1.3、.NET 8〜5、.NET Core 3.1〜2.0、.NET Framework 4.8.1〜2.0
- **ライセンス**: GPL-2.0 / LGPL-2.1（NMeCab由来）
- **GitHub Stars**: 60
- **Unity対応**: .NET Standard 1.3以上をサポートしているため理論上可能だが、明示的なUnity対応ドキュメントなし
- **メンテナンス**: 59コミット
- **特徴**:
  - NMeCabのフォーク・リパッケージ版
  - 名前空間が`NMeCab`から`MeCab`に変更
  - IPADIC辞書が同梱されるため、インストール直後から利用可能
  - 幅広い.NETプラットフォームをサポート
- **G2Pプロジェクトとの関連**: LibNMeCabと同等の機能。辞書同梱で導入が容易だが、naist-jdicへの切替が必要

### 1.3 Lucene.Net.Analysis.Kuromoji

- **リポジトリ**: https://github.com/apache/lucenenet
- **NuGetパッケージ**: `Lucene.Net.Analysis.Kuromoji` (v4.8.0-beta00017)
- **ターゲット**: .NET Standard 2.0、.NET Framework 4.6.2、.NET 6.0
- **ライセンス**: Apache License 2.0
- **Unity対応**: .NET Standard 2.0対応のため理論上可能だが、Lucene.NET全体への依存が大きい
- **メンテナンス**: Apache Software Foundationが管理。ただし長期間betaステータス
- **特徴**:
  - Java版Kuromojiの.NETポート
  - 品詞タグ付け、レンマ化、複合語分析をサポート
  - 全文検索エンジン向けに最適化されている
  - 内蔵辞書（IPAdic系）を含む
- **G2Pプロジェクトとの関連**: 全文検索向けの設計のためG2P用途には過剰。辞書フォーマットもnaist-jdicとは異なる。依存関係が大きくUnityには不向き

### 1.4 Sudachi.NET

- **存在**: C#/.NET向けの公式・非公式実装は**確認できず**
- **本家**: https://github.com/WorksApplications/Sudachi （Java実装）
- **備考**: SudachiPy（Python版）は存在するが、C#ポートは2026年2月時点で見つからない

### 比較表: 形態素解析ライブラリ

| 項目 | LibNMeCab | MeCab.DotNet | Lucene.Net Kuromoji |
|------|-----------|-------------|---------------------|
| **NuGetバージョン** | 0.10.2 | 1.2.0 | 4.8.0-beta00017 |
| **ターゲット** | .NET Standard 2.0 | .NET Standard 2.1〜1.3 | .NET Standard 2.0 |
| **ライセンス** | GPL/LGPL | GPL/LGPL | Apache 2.0 |
| **辞書同梱** | 別パッケージ | IPADIC同梱 | 内蔵 |
| **naist-jdic対応** | 可能（同形式） | 可能（差替え要） | 不可 |
| **Unity適性** | 高（実績あり） | 中 | 低（依存大） |
| **ネイティブ依存** | なし | なし | なし |
| **G2P適性** | **最高** | **高** | 低 |

---

## 2. かな変換ライブラリ

### 2.1 Kawazu

- **リポジトリ**: https://github.com/Cutano/Kawazu
- **NuGetパッケージ**: `Kawazu` (v1.1.4)
- **ターゲット**: .NET Standard 2.0、.NET 5.0、.NET Core 3.1
- **ライセンス**: MIT
- **GitHub Stars**: 64
- **Unity対応**: .NET Standard 2.0対応のため可能だが、パッケージサイズが50MB超（辞書ファイル含む）
- **メンテナンス**: 最終更新2021年4月
- **特徴**:
  - 漢字→ひらがな/カタカナ/ローマ字変換
  - ふりがな・おくりがなモード対応
  - 内部でMeCabを使用して形態素解析
  - Kuroshiro（JavaScript版）にインスパイア
- **G2Pプロジェクトとの関連**: 読み生成の参考実装として有用。ただし辞書サイズが大きく、G2Pパイプラインに組み込むには形態素解析と重複する

### 2.2 WanaKanaSharp

- **リポジトリ**: https://github.com/caguiclajmg/WanaKanaSharp
- **NuGetパッケージ**: `WanaKanaSharp` (v0.2.0 / v0.3.0-alpha)
- **ターゲット**: .NET Standard 2.0
- **ライセンス**: MIT
- **GitHub Stars**: 25
- **Unity対応**: .NET Standard 2.0対応のため可能
- **メンテナンス**: 85コミット
- **特徴**:
  - WanaKana.js の.NETポート
  - ひらがな/カタカナ/ローマ字の相互変換
  - 文字種判定（IsHiragana, IsKatakana, IsJapanese等）
  - 漢字の読み生成機能は**なし**（かな・ローマ字間の変換のみ）

### 2.3 WanaKanaShaapu

- **リポジトリ**: https://github.com/kmoroz/WanaKanaShaapu
- **NuGetパッケージ**: `WanaKanaShaapu` (v2.0.2)
- **ターゲット**: .NET Standard 2.1
- **ライセンス**: MIT
- **GitHub Stars**: 4
- **Unity対応**: .NET Standard 2.1対応のためUnity 2021.2+で可能
- **メンテナンス**: 最終更新2024年11月
- **特徴**:
  - WanaKana JS v5.0.0のC#ポート
  - WanaKanaSharpの代替（より新しいバージョンのポート）
  - かな・ローマ字間の変換・判定

### 2.4 WanaKana-net

- **NuGetパッケージ**: `WanaKana-net` (v1.0.0)
- **リポジトリ**: https://github.com/MartinZikmund/WanaKana-net
- **ターゲット**: .NET Standard 2.0
- **ライセンス**: MIT
- **特徴**: WanaKanaのさらに別の.NETポート

### 2.5 Kana.NET

- **リポジトリ**: https://github.com/rucio-rucio/Kana.NET
- **NuGetパッケージ**: `Kana.NET` (v1.0.6)
- **ターゲット**: .NET 5.0、.NET Core 3.1
- **ライセンス**: MIT
- **Unity対応**: .NET Standard未対応のため直接は困難（ソースコード取り込みなら可能）
- **特徴**:
  - ひらがな⇔カタカナ変換
  - 半角（Hankaku）⇔全角（Zenkaku）変換
  - .NETのみに依存（Windows API不使用のためLinuxでも動作）
  - 依存パッケージなし

### 2.6 JPNKanaConv

- **NuGetパッケージ**: `JPNKanaConv` (v1.0.0)
- **特徴**:
  - ローマ字→ひらがな変換
  - ひらがな⇔カタカナ変換
  - 全角⇔半角カタカナ変換

### 2.7 MyNihongo.KanaConverter

- **NuGetパッケージ**: `MyNihongo.KanaConverter` (v1.0.5)
- **特徴**:
  - ひらがな/カタカナ→ローマ字変換（`ToRomaji()`）
  - ローマ字→ひらがな/カタカナ変換（`ToHiragana()`, `ToKatakana()`）

### 比較表: かな変換ライブラリ

| 項目 | Kawazu | WanaKanaSharp | WanaKanaShaapu | Kana.NET |
|------|--------|---------------|----------------|----------|
| **漢字→かな** | 対応 | 非対応 | 非対応 | 非対応 |
| **かな⇔ローマ字** | 対応 | 対応 | 対応 | 非対応 |
| **かな⇔かな** | 対応 | 対応 | 対応 | 対応 |
| **全角⇔半角** | 非対応 | 非対応 | 非対応 | 対応 |
| **.NET Standard** | 2.0 | 2.0 | 2.1 | 非対応 |
| **ライセンス** | MIT | MIT | MIT | MIT |
| **MeCab依存** | あり | なし | なし | なし |
| **G2P適性** | 参考実装 | 音素変換補助 | 音素変換補助 | テキスト前処理 |

---

## 3. 音声合成関連ライブラリ

### 3.1 KokoroSharp

- **リポジトリ**: https://github.com/Lyrcaxis/KokoroSharp
- **NuGetパッケージ**:
  - `KokoroSharp` (v0.6.2)
  - `KokoroSharp.CPU` (v0.6.2)
  - `KokoroSharp.GPU` (v0.6.2)
  - `KokoroSharp.GPU.Windows` (v0.6.4)
- **ターゲット**: .NET 8.0（推定）
- **ライセンス**: MIT（ライブラリ本体）/ Apache License 2.0（Kokoroモデル・音声）/ GPLv3（eSpeak NG）
- **GitHub Stars**: 202
- **Unity対応**: 現時点では未対応（ロードマップに「Unity & mobile support」が記載）
- **メンテナンス**: 活発に開発中
- **日本語対応**: 対応済み（ただし一部不安定との記載あり）
- **特徴**:
  - ONNX Runtimeベースの高速ローカルTTS推論エンジン
  - マルチスピーカー・多言語対応（日本語含む）
  - テキストセグメントストリーミング
  - ボイスミキシング
  - Kokoro 82M パラメータモデル使用
- **G2Pプロジェクトとの関連**: G2P処理の「下流」に位置するTTSエンジン。本プロジェクトのG2P出力をKokoroSharpの入力として連携する可能性あり

### 3.2 System.Speech.Synthesis

- **パッケージ**: `System.Speech` (v10.0.3) - .NET Framework / Windows限定
- **ターゲット**: .NET Framework、Windows上の.NET（クロスプラットフォーム非対応）
- **ライセンス**: MIT（.NETランタイムの一部）
- **日本語対応**: Windows上にインストールされた日本語TTSボイスに依存
- **Unity対応**: Windows Standaloneビルドでのみ利用可能
- **特徴**:
  - Microsoft Speech API (SAPI) のマネージドラッパー
  - `SpeechSynthesizer`クラスでテキスト読み上げ
  - 音声の選択・速度・ピッチの調整
  - SSMLサポート
- **制約**: Windows専用。Linux/macOS/モバイルでは動作しない

### 3.3 Unity向けTTSプラグイン（外部サービス）

| プラグイン | 特徴 | 日本語 | 料金 |
|-----------|------|--------|------|
| ReadSpeaker AI | 90以上の音声、30以上の言語 | 対応 | 商用ライセンス |
| ElevenLabs | AI音声生成、API統合 | 対応 | サブスクリプション |
| Eden AI Plugin | 複数AIサービス統合 | 対応 | 従量課金 |
| OpenAI TTS | OpenAI API経由 | 対応 | API課金 |

これらはいずれも**外部APIに依存**するため、オフライン環境やネイティブ処理には不向き。

---

## 4. テキスト処理

### 4.1 全角/半角変換

#### Kana.NET（再掲）
- `ToWide()`: 半角→全角変換
- `ToNarrow()`: 全角→半角変換
- .NET依存のみでクロスプラットフォーム

#### .NET標準ライブラリ
- `string.Normalize(NormalizationForm.FormKC)`: Unicode NFKC正規化で互換等価文字の統一が可能
- 半角カタカナ (U+FF61〜U+FF9F) → 全角カタカナへの変換に利用可能

#### Windows API (P/Invoke)
- `LCMapString` API の `LCMAP_HALFWIDTH` / `LCMAP_FULLWIDTH` フラグ
- Windows専用のためUnityクロスプラットフォームには不向き

### 4.2 Unicode正規化

- **組み込みサポート**: `System.String.Normalize()` メソッド
  - `NormalizationForm.FormC` (NFC): 正準合成
  - `NormalizationForm.FormD` (NFD): 正準分解
  - `NormalizationForm.FormKC` (NFKC): 互換合成（全角→半角統一に有用）
  - `NormalizationForm.FormKD` (NFKD): 互換分解
- .NET Standard 2.0で利用可能（Unity対応）
- 追加パッケージ不要

### 4.3 日本語辞書ライブラリ

#### Wacton.Desu

- **リポジトリ**: https://github.com/waacton/Desu
- **NuGetパッケージ**: `Wacton.Desu` (v6.2.0)
- **ターゲット**: .NET Standard 2.0
- **ライセンス**: MIT
- **特徴**:
  - JMdict、JMnedict、KANJIDIC、RADKFILE/KRADFILE、KanjiVGを統合
  - 日本語辞書検索、漢字情報、名前辞書
  - ローマ字変換機能あり
  - 依存パッケージなし
  - パッケージサイズが大きい（辞書リソース埋め込み）
- **G2Pプロジェクトとの関連**: 辞書に載っていない単語のフォールバック検索に利用可能だが、形態素解析辞書（naist-jdic）とは用途が異なる

---

## 5. 本プロジェクト（dot-net-g2p）への推奨事項

### 5.1 推奨スタック

| 処理段階 | 推奨ライブラリ | 理由 |
|---------|--------------|------|
| **形態素解析** | **LibNMeCab** | .NET Standard 2.0対応、Unity実績あり、naist-jdic互換、純粋マネージド |
| **辞書** | naist-jdic（バイナリ辞書） | OpenJTalk互換、アクセント情報フィールド付き、BSD License |
| **テキスト正規化** | .NET標準 `string.Normalize()` + 自前実装 | 追加依存なし、.NET Standard 2.0標準 |
| **かな→音素変換** | 自前実装 | 既存ライブラリにカタカナ→音素列変換（`コンニチワ`→`k o N n i ch i w a`）を行うものがない |
| **ローマ字変換参考** | WanaKanaShaapu | カタカナ→ローマ字変換のロジックが参考になる（ただし音素体系が異なる） |
| **全角/半角正規化** | Kana.NET のロジック参考 or 自前実装 | MIT LicenseでシンプルなためG2P前処理に統合しやすい |

### 5.2 ライセンス上の注意

| ライブラリ | ライセンス | 商用利用 | Unity配布 |
|-----------|-----------|---------|----------|
| LibNMeCab | GPL/LGPL | LGPL選択で可（DLL分離条件あり） | 条件付き可 |
| MeCab.DotNet | GPL/LGPL | 同上 | 条件付き可 |
| naist-jdic辞書 | BSD | 自由 | 可 |
| Kawazu | MIT | 自由 | 可 |
| WanaKanaShaapu | MIT | 自由 | 可 |
| Kana.NET | MIT | 自由 | 可 |
| KokoroSharp | MIT/Apache/GPLv3混合 | 注意が必要 | 現時点で未対応 |

**重要**: LibNMeCab/MeCab.DotNetのGPL/LGPLライセンスは、本プロジェクトのライブラリとしての配布方法に影響する。LGPLを選択し、DLLとして動的リンクする形式であれば、本体プロジェクトを別ライセンスで配布可能。

### 5.3 既存ライブラリでカバーできない領域

以下は本プロジェクトで**自前実装が必要**な領域:

1. **NJD処理（日本語ルール処理）**: OpenJTalk固有の処理（数字読み変換、アクセント句結合等）を行う既存C#ライブラリは存在しない
2. **カタカナ→音素列変換**: カタカナ読みからOpenJTalk音素体系への変換ライブラリは存在しない
3. **アクセント情報処理**: naist-jdic辞書のアクセントフィールド（フィールド14・15）を解釈する処理
4. **naist-jdic辞書のバイナリコンパイル**: LibNMeCabが読める形式へのコンパイル（MeCabの`mecab-dict-index`相当の処理）

---

## 6. 参考リンク

- [NMeCab GitHub](https://github.com/komutan/NMeCab)
- [MeCab.DotNet GitHub](https://github.com/kekyo/MeCab.DotNet)
- [Lucene.Net.Analysis.Kuromoji NuGet](https://www.nuget.org/packages/Lucene.Net.Analysis.Kuromoji/)
- [Kawazu GitHub](https://github.com/Cutano/Kawazu)
- [WanaKanaSharp GitHub](https://github.com/caguiclajmg/WanaKanaSharp)
- [WanaKanaShaapu GitHub](https://github.com/kmoroz/WanaKanaShaapu)
- [Kana.NET GitHub](https://github.com/rucio-rucio/Kana.NET)
- [KokoroSharp GitHub](https://github.com/Lyrcaxis/KokoroSharp)
- [Wacton.Desu GitHub](https://github.com/waacton/Desu)
- [NuGet Japanese Packages](https://nugetmusthaves.com/tag/Japanese)
- [UnityでのNMeCab導入手順](https://www.hanachiru-blog.com/entry/2021/01/18/120000)
