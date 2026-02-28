using System;
using DotNetG2P;
using DotNetG2P.NMeCab;

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

// G2Pエンジンの動作確認
using var tokenizer = new NMeCabTokenizer(dicPath);
using var engine = new G2PEngine(tokenizer);

var samples = new[]
{
    "こんにちは",
    "今日は良い天気です",
    "東京タワーに行きたい",
    "音声合成の研究",
};

Console.WriteLine("=== DotNetG2P M1 動作確認 ===");
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
