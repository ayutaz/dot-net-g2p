# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

C#/.NET（Unity対応）向けの日本語G2P（Grapheme-to-Phoneme: 書記素→音素変換）ライブラリ。
OpenJTalk/pyopenjtalkの処理パイプラインをC#でネイティブに再実装し、Pythonやネイティブバイナリへの依存を排除する。

## 背景・動機

- OpenJTalkやpyopenjtalkはC/C++/Python実装であり、C#/.NETやUnityから直接利用するのが困難
- 既存のC#向け日本語G2Pライブラリは存在しない
- Unity（ゲーム・VTuber・音声合成等）での日本語TTS前処理として需要がある

## アーキテクチャ方針

OpenJTalkの処理パイプラインに準拠した4段階処理:

1. **形態素解析**: MeCab互換の解析（NMeCab/MeCab.DotNet活用、またはカスタム実装）
2. **NJD処理（日本語ルール処理）**: 読み生成、数字読み変換、アクセント句結合、アクセント結合、無声音化、長音化
3. **音素変換**: カタカナ読み → 音素列（例: `コンニチワ` → `k o N n i ch i w a`）
4. **アクセント情報付与**（オプション）: モーラ数・アクセント核位置の出力

### 日本語音素体系

| 種別 | 音素 |
|------|------|
| 母音 | a, i, u, e, o |
| 半母音 | y, w |
| 子音 | k, g, s, z, t, d, n, h, b, p, m, r, ch, sh, j, f, ts, ky, gy, ny, hy, by, py, my, ry |
| 特殊 | N（撥音）, Q（促音）, R（長音） |

### 辞書

OpenJTalk用のnaist-jdic辞書フォーマット（IPADIC + アクセント情報2フィールド拡張）を使用:
- フィールド14: `アクセント核位置/モーラ数`（例: `3/4`）
- フィールド15: アクセント結合タイプ（C1〜C5）

## 技術スタック

- **言語**: C#
- **ターゲット**: .NET Standard 2.1（Unity 2021.2+互換）
- **形態素解析**: NMeCab系ライブラリ または 自前実装
- **辞書**: naist-jdic（BSD License）

## 開発言語

コード内コメント・ドキュメント・コミットメッセージ・PR・Issueはすべて**日本語**で記述する。
