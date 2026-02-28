# openjtalk-native リポジトリ調査結果

## 調査日: 2026-02-28

https://github.com/ayutaz/openjtalk-native を調査し、dot-net-g2pプロジェクトとの関連を整理した。

---

## 1. プロジェクト概要

- **名称**: openjtalk-native
- **目的**: OpenJTalkをクロスプラットフォームのネイティブ共有ライブラリとして提供。日本語テキスト→音素変換のC APIを公開
- **ライセンス**: BSD-3-Clause（Modified BSD License）
- **言語**: C (C99)
- **C#コードは含まれない**

### 対応プラットフォーム

| プラットフォーム | アーキテクチャ | 出力ファイル |
|---|---|---|
| Windows | x64 | `openjtalk_native.dll` |
| Linux | x86_64 | `libopenjtalk_native.so` |
| macOS | arm64 + x86_64 | `libopenjtalk_native.dylib` |
| Android | arm64-v8a, armeabi-v7a, x86, x86_64 | `libopenjtalk_native.so` |
| iOS | arm64 | `libopenjtalk_native.a`（静的） |

---

## 2. C API設計

### 主要構造体

```c
// 基本音素変換結果
typedef struct {
    char* phonemes;           // スペース区切り音素文字列
    int* phoneme_ids;         // 音素ID配列
    int phoneme_count;        // 音素数
    double* durations;        // 各音素の継続時間（秒）
    double total_duration;    // 全体継続時間
} OpenJTalkNativePhonemeResult;

// プロソディ情報付き結果
typedef struct {
    char* phonemes;           // スペース区切り音素文字列
    int* prosody_a1;          // アクセント核からの相対位置
    int* prosody_a2;          // アクセント句内位置（1-based）
    int* prosody_a3;          // アクセント句のモーラ数
    int phoneme_count;        // 音素数
} OpenJTalkNativeProsodyResult;
```

### 主要API

| 関数 | 説明 |
|---|---|
| `openjtalk_native_create(dict_path)` | インスタンス生成 |
| `openjtalk_native_destroy(handle)` | インスタンス破棄 |
| `openjtalk_native_phonemize(handle, text)` | テキスト→音素変換 |
| `openjtalk_native_phonemize_with_prosody(handle, text)` | 韻律情報付き変換 |
| `openjtalk_native_free_result(result)` | 結果メモリ解放 |
| `openjtalk_native_set_option(handle, key, value)` | オプション設定 |
| `openjtalk_native_get_last_error(handle)` | エラーコード取得 |
| `openjtalk_native_get_version()` | バージョン取得 |

### 設計上の特徴

- **ハンドルベースAPI**: `void*`ハンドルによるコンテキスト管理
- **メモリ管理の明確化**: 呼び出し元が`free_result()`で解放
- **スレッド安全性**: 各ハンドルは独立、異なるハンドルは複数スレッドから同時使用可
- **11種類のエラーコード**: 詳細なエラーハンドリング
- **入力制限**: UTF-8で4096バイト未満

---

## 3. NJD処理の実装

`run_njd_pipeline()` 関数で以下を順序実行（**全てOpenJTalkライブラリのC関数を呼び出し**）:

1. `njd_set_pronunciation()` - 発音設定
2. `njd_set_digit()` - 数字処理
3. `njd_set_accent_phrase()` - アクセント句設定
4. `njd_set_accent_type()` - アクセント型設定
5. `njd_set_unvoiced_vowel()` - 無声化処理
6. `njd_set_long_vowel()` - 長音処理

**→ NJD処理のC#再実装は含まれない**

---

## 4. テスト

### test_api.c
- APIライフサイクル（create/destroy）
- NULL引数処理
- 無効な辞書パス処理
- エラーコードの検証

### test_phonemization.c
- 基本的な日本語入力（「こんにちは」「おはようございます」）
- 数値処理（「123」）
- カタカナ入力
- 混合テキスト
- エッジケース（長文、特殊文字）

---

## 5. 辞書の扱い

- **リポジトリに辞書は含まれない**
- ユーザーが `open_jtalk_dic_utf_8-1.11.tar.gz` を別途取得・配置
- `openjtalk_native_create(dict_path)` で辞書ディレクトリを指定
- 必要ファイル: sys.dic, matrix.bin, char.bin, unk.dic

---

## 6. dot-net-g2pプロジェクトへの影響

### 参考にできるもの

| 対象 | 内容 |
|------|------|
| **API設計パターン** | ハンドルベース、明確なメモリ管理、エラーコード体系 |
| **テストケース** | 基本入力・数値・カタカナ・混合・エッジケースの網羅 |
| **パイプライン構造** | MeCab → NJD 6段階 → JPCommon → 音素 の処理順序 |
| **プロソディ情報** | A1/A2/A3の定義と使い方 |
| **ビルド・CI/CD** | マルチプラットフォーム対応のGitHub Actions |

### 参考にできないもの

| 対象 | 理由 |
|------|------|
| **NJD処理の実装** | OpenJTalkのCライブラリに完全依存、C#再実装が必要 |
| **形態素解析** | OpenJTalk内蔵MeCabを使用 |
| **ネイティブ依存設計** | dot-net-g2pは純粋C#で実装するため |

### dot-net-g2pのG2PEngine API設計への示唆

openjtalk-nativeのC APIは、dot-net-g2pのG2PEngine APIと対応関係がある:

| openjtalk-native (C) | dot-net-g2p (C#) |
|---|---|
| `openjtalk_native_create(dict_path)` | `new G2PEngine(tokenizer)` |
| `openjtalk_native_phonemize(handle, text)` | `engine.ToPhonemes(text)` |
| `openjtalk_native_phonemize_with_prosody(handle, text)` | `engine.ToAccentPhrases(text)` |
| `openjtalk_native_destroy(handle)` | `engine.Dispose()` |
| `OpenJTalkNativeProsodyResult.prosody_a1/a2/a3` | `AccentPhrase.Accent` / `Mora` 構造体 |

---

## 7. 総評

openjtalk-nativeはOpenJTalkをクリーンなC APIで提供する完成度の高いプロジェクト。
ただし、dot-net-g2pの目標である「ネイティブバイナリ依存の排除」とは方向性が異なる。

**価値**: API設計思想、テスト戦略、NJDパイプライン順序の確認として有用。
**限界**: NJD処理のC#再実装コードは含まれないため、jpreprocess (Rust) / OpenJTalk (C) を参考にした新規実装が引き続き必要。
