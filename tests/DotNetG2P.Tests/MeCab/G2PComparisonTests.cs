using System;
using System.Collections.Generic;
using System.IO;
using DotNetG2P;
using DotNetG2P.MeCab;
using DotNetG2P.NMeCab;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.MeCab
{
    /// <summary>
    /// G2Pパイプライン全体でNMeCabTokenizerとMeCabTokenizerの結果を比較するテスト。
    /// ToPhonemes, ToKana, ToProsody で同一結果を返すかを検証する。
    /// </summary>
    public class G2PComparisonTests : IDisposable
    {
        private static string? DicPath => Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
        private static bool DictionaryExists => !string.IsNullOrEmpty(DicPath) && Directory.Exists(DicPath);

        private readonly ITestOutputHelper _output;
        private readonly G2PEngine? _nmecabEngine;
        private readonly G2PEngine? _mecabEngine;
        private readonly NMeCabTokenizer? _nmecab;
        private readonly MeCabTokenizer? _mecab;

        public G2PComparisonTests(ITestOutputHelper output)
        {
            _output = output;
            if (DictionaryExists)
            {
                _nmecab = new NMeCabTokenizer(DicPath!);
                _mecab = new MeCabTokenizer(DicPath!);
                _nmecabEngine = new G2PEngine(_nmecab);
                _mecabEngine = new G2PEngine(_mecab);
            }
        }

        public void Dispose()
        {
            _nmecabEngine?.Dispose();
            _mecabEngine?.Dispose();
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!DictionaryExists, "naist-jdic辞書が見つかりません（環境変数 NAIST_JDIC_PATH を設定してください）");
        }

        private static readonly string[] TestTexts = new[]
        {
            "こんにちは",
            "東京タワー",
            "今日は天気がいいですね",
            "私の名前は田中です",
            "美味しいラーメンを食べました",
            "東京から大阪まで新幹線で行きます",
            "明日の天気予報を教えてください",
            "音声合成の研究",
            "人工知能",
            "おはようございます",
            "お疲れ様でした",
            "12月25日はクリスマスです",
            "３本のペンがあります",
            "テスト",
            "ありがとう",
            "富士山は日本一高い山です",
            "桜の花が咲きました",
            "毎朝ジョギングをしています",
            "この問題は難しいですね",
            "今夜は星がきれいに見えます",
        };

        public static IEnumerable<object[]> GetTestTexts()
        {
            foreach (var t in TestTexts)
            {
                yield return new object[] { t };
            }
        }

        // =====================================================================
        // 1. ToPhonemes 比較
        // =====================================================================

        [SkippableTheory]
        [MemberData(nameof(GetTestTexts))]
        public void ToPhonemes_NMeCabと同一出力(string text)
        {
            SkipIfNoDictionary();

            var expected = _nmecabEngine!.ToPhonemes(text);
            var actual = _mecabEngine!.ToPhonemes(text);

            _output.WriteLine($"入力: \"{text}\"");
            _output.WriteLine($"NMeCab: \"{expected}\"");
            _output.WriteLine($"MeCab:  \"{actual}\"");

            Assert.Equal(expected, actual);
        }

        // =====================================================================
        // 2. ToKana 比較
        // =====================================================================

        [SkippableTheory]
        [MemberData(nameof(GetTestTexts))]
        public void ToKana_NMeCabと同一出力(string text)
        {
            SkipIfNoDictionary();

            var expected = _nmecabEngine!.ToKana(text);
            var actual = _mecabEngine!.ToKana(text);

            _output.WriteLine($"入力: \"{text}\"");
            _output.WriteLine($"NMeCab: \"{expected}\"");
            _output.WriteLine($"MeCab:  \"{actual}\"");

            Assert.Equal(expected, actual);
        }

        // =====================================================================
        // 3. ToProsody 比較
        // =====================================================================

        [SkippableTheory]
        [MemberData(nameof(GetTestTexts))]
        public void ToProsody_NMeCabと同一出力(string text)
        {
            SkipIfNoDictionary();

            var expected = _nmecabEngine!.ToProsody(text);
            var actual = _mecabEngine!.ToProsody(text);

            _output.WriteLine($"入力: \"{text}\"");
            _output.WriteLine($"NMeCab: \"{expected}\"");
            _output.WriteLine($"MeCab:  \"{actual}\"");

            Assert.Equal(expected, actual);
        }

        // =====================================================================
        // 4. ToAccentPhrases 比較
        // =====================================================================

        [SkippableTheory]
        [MemberData(nameof(GetTestTexts))]
        public void ToAccentPhrases_NMeCabと同一出力(string text)
        {
            SkipIfNoDictionary();

            var expected = _nmecabEngine!.ToAccentPhrases(text);
            var actual = _mecabEngine!.ToAccentPhrases(text);

            Assert.Equal(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].Accent, actual[i].Accent);
                Assert.Equal(expected[i].Moras.Count, actual[i].Moras.Count);

                for (int m = 0; m < expected[i].Moras.Count; m++)
                {
                    Assert.Equal(expected[i].Moras[m].Kind, actual[i].Moras[m].Kind);
                    Assert.Equal(expected[i].Moras[m].Consonant, actual[i].Moras[m].Consonant);
                    Assert.Equal(expected[i].Moras[m].Vowel, actual[i].Moras[m].Vowel);
                }
            }
        }

        // =====================================================================
        // 5. ToFullContextLabels 比較
        // =====================================================================

        [SkippableTheory]
        [MemberData(nameof(GetTestTexts))]
        public void ToFullContextLabels_NMeCabと同一出力(string text)
        {
            SkipIfNoDictionary();

            var expected = _nmecabEngine!.ToFullContextLabels(text);
            var actual = _mecabEngine!.ToFullContextLabels(text);

            Assert.Equal(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        // =====================================================================
        // 6. Analyze 比較
        // =====================================================================

        [SkippableTheory]
        [MemberData(nameof(GetTestTexts))]
        public void Analyze_NMeCabと同一ノード数(string text)
        {
            SkipIfNoDictionary();

            var expected = _nmecabEngine!.Analyze(text);
            var actual = _mecabEngine!.Analyze(text);

            Assert.Equal(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].Surface, actual[i].Surface);
            }
        }
    }
}
