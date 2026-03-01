#!/usr/bin/env python3
"""pyopenjtalkの出力を事前生成するスクリプト

使い方:
  pip install pyopenjtalk
  python generate_expected.py

出力:
  expected_phonemes.json - テストケースごとの音素列とフルコンテキストラベル
"""
import json
try:
    import pyopenjtalk
except ImportError:
    print("pip install pyopenjtalk が必要です")
    exit(1)

# テストケース一覧
test_cases = [
    "こんにちは",
    "おはようございます",
    "ありがとう",
    "東京",
    "東京都",
    "日本語",
    "人工知能",
    "音声合成",
    "コンピュータ",
    "プログラミング",
    "100円",
    "2024年",
    "3本",
    "12月25日",
    "今日はいい天気ですね",
    "私は東京に住んでいます",
    "すき",
    "です",
]

results = []
for text in test_cases:
    phonemes = pyopenjtalk.g2p(text)
    labels = pyopenjtalk.extract_fullcontext(text)
    results.append({
        "input": text,
        "phonemes": phonemes,
        "labels": labels
    })

with open("expected_phonemes.json", "w", encoding="utf-8") as f:
    json.dump(results, f, ensure_ascii=False, indent=2)

print(f"{len(results)} 件のテストケースを生成しました")
