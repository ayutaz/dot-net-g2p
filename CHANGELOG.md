# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- NJD処理パイプライン6段階すべて実装（M2完了）
  - TextNormalizer: 全角/半角変換、濁点結合
  - SetPronunciation: 発音生成（5段階処理）
  - DigitSequence + SetDigit: 数字読み変換
  - SetAccentPhrase: アクセント句結合（18ルール）
  - SetAccentType: アクセント結合型（C1-C5, F1-F5, P系列）
  - SetUnvoicedVowel: 無声音化（6ルール）
- 5種類の出力形式: 音素列、カタカナ、韻律記号付き、VOICEVOX互換AccentPhrase、HTSフルコンテキストラベル
- G2POptionsによる各処理段階のON/OFF制御
- Analyze APIでNJDノード列を取得可能
- ITokenizer抽象化で形態素解析エンジン差し替え可能

## [0.1.0] - 2025-01-01

### Added
- 初期リリース（M1: 最小動作プロトタイプ）
- `g2p("こんにちは")` → `"k o N n i ch i w a"` が動作
- naist-jdic辞書によるフルパイプライン（形態素解析→NJD→音素変換）
- LibNMeCab 0.10.2によるMeCab互換形態素解析
- .NET Standard 2.1対応（Unity 2021.2+互換）

[Unreleased]: https://github.com/dotnetg2p/DotNetG2P/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/dotnetg2p/DotNetG2P/releases/tag/v0.1.0
