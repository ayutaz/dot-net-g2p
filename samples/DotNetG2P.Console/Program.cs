using System;
using DotNetG2P;
using DotNetG2P.MeCab;

// naist-jdic辞書のパスを指定
// 環境変数 NAIST_JDIC_PATH または第1引数で辞書パスを指定可能
var dicPath = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");

if (string.IsNullOrEmpty(dicPath))
{
    Console.WriteLine("使用方法: DotNetG2P.Console <naist-jdic辞書ディレクトリパス>");
    Console.WriteLine("  または環境変数 NAIST_JDIC_PATH を設定してください。");
    Console.WriteLine();
    Console.WriteLine("辞書なしでMoraMappingの動作確認を行います...");
    Console.WriteLine();

    // 辞書なしでもMoraMappingの動作確認が可能
    var testCases = new[] { "コンニチワ", "オハヨウゴザイマス", "アリガトウ", "セカイ" };
    foreach (var kana in testCases)
    {
        var moras = DotNetG2P.PhonemeConverter.MoraMapping.KatakanaToMoras(kana);
        var phonemes = DotNetG2P.PhonemeConverter.MoraMapping.MorasToPhonemeString(moras);
        Console.WriteLine($"{kana} → {phonemes}");
    }

    return;
}

// === NJDパイプライン統合動作確認 ===

Console.WriteLine("=== DotNetG2P NJDパイプライン動作確認 ===");
Console.WriteLine();

// デフォルトオプション（全処理有効）で動作確認
using var tokenizer = new MeCabTokenizer(dicPath);
using var engine = new G2PEngine(tokenizer);

var samples = new[]
{
    // 基本テスト
    "こんにちは",
    "今日は良い天気です",
    "東京タワーに行きたい",
    "音声合成の研究",
    // 数字読み変換テスト
    "３個のりんご",
    "１２３円",
    "２０２５年",
    // 無声音化テスト
    "すきです",
    // 複合テスト
    "私は東京に住んでいます",
};

Console.WriteLine("--- 全処理有効（デフォルト）---");
Console.WriteLine();

foreach (var text in samples)
{
    var phonemes = engine.ToPhonemes(text);
    var kana = engine.ToKana(text);
    Console.WriteLine($"入力: {text}");
    Console.WriteLine($"カナ: {kana}");
    Console.WriteLine($"音素: {phonemes}");
    Console.WriteLine();
}

// オプション指定の動作確認: 無声音化OFF
Console.WriteLine("--- 無声音化OFF ---");
Console.WriteLine();

using var tokenizer2 = new MeCabTokenizer(dicPath);
var optionsNoUnvoiced = new G2POptions(enableUnvoicedVowel: false);
using var engine2 = new G2PEngine(tokenizer2, optionsNoUnvoiced);

var unvoicedSamples = new[] { "すきです", "東京タワー" };
foreach (var text in unvoicedSamples)
{
    var phonemes = engine2.ToPhonemes(text);
    Console.WriteLine($"入力: {text}");
    Console.WriteLine($"音素: {phonemes}");
    Console.WriteLine();
}

// Analyze APIのデモ
Console.WriteLine("--- Analyze API（NjdNode詳細出力）---");
Console.WriteLine();

var analyzeText = "東京タワーに行きたい";
Console.WriteLine($"入力: {analyzeText}");
var nodes = engine.Analyze(analyzeText);
for (int i = 0; i < nodes.Count; i++)
{
    var node = nodes[i];
    Console.WriteLine($"  [{i}] 表層: {node.Surface}");
    Console.WriteLine($"       品詞: {node.Details?.PartOfSpeech}");
    Console.WriteLine($"       発音: {node.Pronunciation}");
    Console.WriteLine($"       Acc:  {node.AccentType}");
    Console.WriteLine($"       Chain: {node.ChainFlag}");
}

// === 出力形式API動作確認 ===

Console.WriteLine();
Console.WriteLine("=== DotNetG2P 出力形式API動作確認 ===");
Console.WriteLine();

var m3Samples = new[] { "こんにちは", "今日は良い天気です", "東京タワーに行きたい" };

// ToProsody
Console.WriteLine("--- ToProsody（ESPnet韻律記号付き）---");
Console.WriteLine();
foreach (var text in m3Samples)
{
    var prosody = engine.ToProsody(text);
    Console.WriteLine($"入力: {text}");
    Console.WriteLine($"韻律: {prosody}");
    Console.WriteLine();
}

// ToAccentPhrases
Console.WriteLine("--- ToAccentPhrases（VOICEVOX互換）---");
Console.WriteLine();
foreach (var text in m3Samples)
{
    var phrases = engine.ToAccentPhrases(text);
    Console.WriteLine($"入力: {text}");
    Console.WriteLine($"  アクセント句数: {phrases.Count}");
    for (int i = 0; i < phrases.Count; i++)
    {
        var p = phrases[i];
        Console.WriteLine($"  [{i}] モーラ数={p.Moras.Count}, アクセント={p.Accent}, 疑問={p.IsInterrogative}, ポーズ={p.PauseMora != null}");
    }
    Console.WriteLine();
}

// ToFullContextLabels
Console.WriteLine("--- ToFullContextLabels（HTSフルコンテキストラベル）---");
Console.WriteLine();
foreach (var text in m3Samples)
{
    var labels = engine.ToFullContextLabels(text);
    Console.WriteLine($"入力: {text}");
    Console.WriteLine($"  ラベル数: {labels.Count}");
    var showCount = Math.Min(5, labels.Count);
    for (int i = 0; i < showCount; i++)
    {
        Console.WriteLine($"  [{i}] {labels[i]}");
    }
    if (labels.Count > showCount)
    {
        Console.WriteLine($"  ... (残り {labels.Count - showCount} 行)");
    }
    Console.WriteLine();
}
