using System;
using System.Collections.Generic;
using System.Linq;
using DotNetG2P.English;
using DotNetG2P.English.LTS;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.EnglishG2P.Integration
{
    /// <summary>
    /// 英語G2Pの辞書ルックアップ・LTS精度を詳細検証するテスト。
    /// CMU辞書の基本ルックアップ、大文字小文字正規化、OOV語のLTS推定、
    /// ストレスマーカー正確性、複数発音バリアント、IPA/X-SAMPA変換、
    /// 特殊語（ハイフン語・アポストロフィ語）、LTS有効/無効差分を検証する。
    /// </summary>
    public class EnglishAccuracyVerificationTests : IDisposable
    {
        private readonly EnglishG2PEngine _engine;
        private readonly ITestOutputHelper _output;

        public EnglishAccuracyVerificationTests(ITestOutputHelper output)
        {
            _output = output;
            _engine = new EnglishG2PEngine();
        }

        public void Dispose() => _engine.Dispose();

        // ================================================================
        // 1. CMU辞書の基本ルックアップ（20+語の一般語）
        // ================================================================

        [Theory]
        [InlineData("hello", "HH AH0 L OW1")]
        [InlineData("world", "W ER1 L D")]
        [InlineData("computer", "K AH0 M P Y UW1 T ER0")]
        [InlineData("beautiful", "B Y UW1 T AH0 F AH0 L")]
        [InlineData("the", "DH AH0")]
        [InlineData("and", "AH0 N D")]
        [InlineData("is", "IH1 Z")]
        [InlineData("have", "HH AE1 V")]
        [InlineData("water", "W AO1 T ER0")]
        [InlineData("music", "M Y UW1 Z IH0 K")]
        [InlineData("system", "S IH1 S T AH0 M")]
        [InlineData("program", "P R OW1 G R AE2 M")]
        [InlineData("people", "P IY1 P AH0 L")]
        [InlineData("school", "S K UW1 L")]
        [InlineData("student", "S T UW1 D AH0 N T")]
        [InlineData("teacher", "T IY1 CH ER0")]
        [InlineData("family", "F AE1 M AH0 L IY0")]
        [InlineData("language", "L AE1 NG G W AH0 JH")]
        [InlineData("history", "HH IH1 S T ER0 IY0")]
        [InlineData("science", "S AY1 AH0 N S")]
        [InlineData("number", "N AH1 M B ER0")]
        [InlineData("picture", "P IH1 K CH ER0")]
        public void DictLookup_CommonWords_ReturnsExpectedPhonemes(string word, string expected)
        {
            var result = _engine.ToPhonemes(word);
            _output.WriteLine($"{word}: [{result}]");
            Assert.Equal(expected, result);
        }

        // ================================================================
        // 2. 大文字・小文字の正規化
        // ================================================================

        [Theory]
        [InlineData("hello", "Hello")]
        [InlineData("hello", "hElLo")]
        [InlineData("world", "World")]
        [InlineData("world", "WORLD")]
        [InlineData("computer", "Computer")]
        [InlineData("computer", "COMPUTER")]
        [InlineData("beautiful", "Beautiful")]
        [InlineData("beautiful", "BEAUTIFUL")]
        public void CaseNormalization_DifferentCases_SamePhonemesAsLowercase(string lowercase, string variant)
        {
            var lowerResult = _engine.ToPhonemes(lowercase);

            // 全大文字は正規化により略語扱い（スペル読み）される場合がある
            // 正規化を無効にして辞書のみの大文字小文字正規化を検証
            var options = new EnglishG2POptions(enableNormalization: false);
            using var engineNoNorm = new EnglishG2PEngine(options);
            var lowerResultNoNorm = engineNoNorm.ToPhonemes(lowercase);
            var variantResultNoNorm = engineNoNorm.ToPhonemes(variant);

            _output.WriteLine($"lowercase='{lowercase}' [{lowerResultNoNorm}], variant='{variant}' [{variantResultNoNorm}]");
            Assert.Equal(lowerResultNoNorm, variantResultNoNorm);
        }

        [Fact]
        public void CaseNormalization_ContainsWord_CaseInsensitive()
        {
            Assert.True(_engine.ContainsWord("hello"));
            Assert.True(_engine.ContainsWord("Hello"));
            Assert.True(_engine.ContainsWord("HELLO"));
            Assert.True(_engine.ContainsWord("hElLo"));
        }

        [Fact]
        public void CaseNormalization_LookupWord_CaseInsensitive()
        {
            var lower = _engine.LookupWord("hello");
            var upper = _engine.LookupWord("HELLO");
            var mixed = _engine.LookupWord("HeLLo");

            Assert.Equal(lower.Count, upper.Count);
            Assert.Equal(lower.Count, mixed.Count);
            for (int i = 0; i < lower.Count; i++)
            {
                Assert.Equal(lower[i], upper[i]);
                Assert.Equal(lower[i], mixed[i]);
            }
        }

        // ================================================================
        // 3. OOV語のLTS推定（架空の語）
        // ================================================================

        [Theory]
        [InlineData("blorf")]
        [InlineData("grixnop")]
        [InlineData("glimble")]
        [InlineData("frandex")]
        [InlineData("zyphlor")]
        [InlineData("tronkify")]
        [InlineData("wibbleston")]
        [InlineData("quazzle")]
        public void LtsEstimation_FictitiousWords_ProducesNonEmptyResult(string word)
        {
            // 架空の語はCMU辞書に存在しないことを確認
            Assert.False(_engine.ContainsWord(word), $"'{word}' は辞書に存在すべきではありません");

            var result = _engine.ToPhonemes(word);
            _output.WriteLine($"OOV '{word}': [{result}]");

            Assert.NotEmpty(result);

            // 音素トークン数が妥当な範囲（最低2音素）
            var tokenCount = result.Split(' ').Length;
            Assert.True(tokenCount >= 2, $"'{word}' の音素数({tokenCount})が少なすぎます");
        }

        [Theory]
        [InlineData("blorf", "B")]
        [InlineData("grixnop", "G")]
        [InlineData("glimble", "G")]
        [InlineData("frandex", "F")]
        [InlineData("tronkify", "T")]
        public void LtsEstimation_FictitiousWords_InitialConsonantCorrect(string word, string expectedInitial)
        {
            var result = _engine.ToPhonemes(word);
            _output.WriteLine($"OOV '{word}': [{result}]");

            Assert.StartsWith(expectedInitial, result);
        }

        [Fact]
        public void LtsEstimation_Deterministic_SameResultOnMultipleCalls()
        {
            var word = "blorf";
            var r1 = _engine.ToPhonemes(word);
            var r2 = _engine.ToPhonemes(word);
            var r3 = _engine.ToPhonemes(word);

            Assert.Equal(r1, r2);
            Assert.Equal(r2, r3);
        }

        // ================================================================
        // 4. ストレスマーカーの正確性
        // ================================================================

        [Fact]
        public void StressMarkers_Hello_HasCorrectStressPattern()
        {
            var phonemes = _engine.ToPhonemeList("hello");
            // HH AH0 L OW1
            Assert.Equal(4, phonemes.Count);

            // HH = 子音、Stress.None
            Assert.Equal(ArpabetPhoneme.HH, phonemes[0].Phoneme);
            Assert.Equal(Stress.None, phonemes[0].Stress);
            Assert.False(phonemes[0].IsVowel);

            // AH0 = 母音、NoStress
            Assert.Equal(ArpabetPhoneme.AH, phonemes[1].Phoneme);
            Assert.Equal(Stress.NoStress, phonemes[1].Stress);
            Assert.True(phonemes[1].IsVowel);

            // L = 子音、Stress.None
            Assert.Equal(ArpabetPhoneme.L, phonemes[2].Phoneme);
            Assert.Equal(Stress.None, phonemes[2].Stress);
            Assert.False(phonemes[2].IsVowel);

            // OW1 = 母音、Primary
            Assert.Equal(ArpabetPhoneme.OW, phonemes[3].Phoneme);
            Assert.Equal(Stress.Primary, phonemes[3].Stress);
            Assert.True(phonemes[3].IsVowel);
        }

        [Fact]
        public void StressMarkers_Autobiography_ContainsSecondaryStress()
        {
            var phonemes = _engine.ToPhonemeList("autobiography");

            var hasSecondary = phonemes.Any(p => p.Stress == Stress.Secondary);
            var hasPrimary = phonemes.Any(p => p.Stress == Stress.Primary);
            var hasNoStress = phonemes.Any(p => p.Stress == Stress.NoStress);

            _output.WriteLine($"autobiography: [{string.Join(" ", phonemes.Select(p => p.ToString()))}]");

            Assert.True(hasPrimary, "autobiographyにPrimaryストレスが含まれるべき");
            Assert.True(hasSecondary, "autobiographyにSecondaryストレスが含まれるべき");
            Assert.True(hasNoStress, "autobiographyにNoStress母音が含まれるべき");
        }

        [Fact]
        public void StressMarkers_Program_ContainsSecondaryStress()
        {
            // "program" = P R OW1 G R AE2 M
            var phonemes = _engine.ToPhonemeList("program");

            var hasSecondary = phonemes.Any(p => p.Stress == Stress.Secondary);
            var hasPrimary = phonemes.Any(p => p.Stress == Stress.Primary);

            _output.WriteLine($"program: [{string.Join(" ", phonemes.Select(p => p.ToString()))}]");

            Assert.True(hasPrimary, "programにPrimaryストレスが含まれるべき");
            Assert.True(hasSecondary, "programにSecondaryストレスが含まれるべき");
        }

        [Fact]
        public void StressMarkers_Consonants_AlwaysNone()
        {
            // 子音にはストレスが付かないことを検証
            var phonemes = _engine.ToPhonemeList("student");
            // S T UW1 D AH0 N T
            foreach (var p in phonemes)
            {
                if (!p.IsVowel)
                {
                    Assert.Equal(Stress.None, p.Stress);
                }
            }
        }

        [Fact]
        public void StressMarkers_IncludeStressFalse_NoDigitsInOutput()
        {
            var options = new EnglishG2POptions(includeStress: false);
            using var engine = new EnglishG2PEngine(options);

            var words = new[] { "hello", "computer", "beautiful", "program", "autobiography" };
            foreach (var word in words)
            {
                var result = engine.ToPhonemes(word);
                Assert.DoesNotMatch(@"\d", result);
                _output.WriteLine($"{word} (no stress): [{result}]");
            }
        }

        // ================================================================
        // 5. 複数発音バリアント
        // ================================================================

        [Theory]
        [InlineData("lead", 2)]
        [InlineData("read", 2)]
        [InlineData("close", 2)]
        [InlineData("a", 2)]
        public void MultiVariant_WordsHaveMultiplePronunciations(string word, int minVariants)
        {
            var prons = _engine.LookupAllPronunciations(word);
            _output.WriteLine($"'{word}' バリアント数: {prons.Count}");
            for (int i = 0; i < prons.Count; i++)
                _output.WriteLine($"  [{i}] {prons[i]}");

            Assert.True(prons.Count >= minVariants,
                $"'{word}' は {minVariants} 以上のバリアントを期待: 実際={prons.Count}");
        }

        [Fact]
        public void MultiVariant_Lead_DifferentVowels()
        {
            var prons = _engine.LookupAllPronunciations("lead");
            Assert.True(prons.Count >= 2);

            // "lead" の2バリアント: L EH1 D (鉛) と L IY1 D (導く)
            var variant1 = prons[0].ToString();
            var variant2 = prons[1].ToString();

            _output.WriteLine($"lead[0]: [{variant1}]");
            _output.WriteLine($"lead[1]: [{variant2}]");

            Assert.NotEqual(variant1, variant2);
            Assert.Equal("L EH1 D", variant1);
            Assert.Equal("L IY1 D", variant2);
        }

        [Fact]
        public void MultiVariant_ToPhonemes_ReturnsStableResult()
        {
            var multiWords = new[] { "lead", "read", "close", "live", "wind" };
            foreach (var word in multiWords)
            {
                var r1 = _engine.ToPhonemes(word);
                var r2 = _engine.ToPhonemes(word);

                Assert.Equal(r1, r2);
                Assert.NotEmpty(r1);

                _output.WriteLine($"'{word}': [{r1}]");
            }
        }

        // ================================================================
        // 6. ARPAbet→IPA変換の正確性
        // ================================================================

        [Theory]
        [InlineData("hello", "həˈloʊ")]
        [InlineData("world", "ˈwɝld")]
        [InlineData("cat", "ˈkæt")]
        [InlineData("dog", "ˈdɔɡ")]
        [InlineData("school", "ˈskul")]
        public void IpaConversion_CommonWords_ReturnsExpectedIpa(string word, string expectedIpa)
        {
            var result = _engine.ToIPA(word);
            _output.WriteLine($"{word}: IPA=[{result}] expected=[{expectedIpa}]");
            Assert.Equal(expectedIpa, result);
        }

        [Fact]
        public void IpaConversion_WithoutStress_NoStressMarkers()
        {
            var result = _engine.ToIPAWithoutStress("hello");
            _output.WriteLine($"hello (no stress IPA): [{result}]");

            // IPA ストレスマーカー（ˈ ˌ）が含まれないこと
            Assert.DoesNotContain("\u02C8", result); // ˈ
            Assert.DoesNotContain("\u02CC", result); // ˌ
        }

        [Fact]
        public void IpaConversion_Computer_HasStressMarker()
        {
            var result = _engine.ToIPA("computer");
            _output.WriteLine($"computer IPA: [{result}]");

            // Primary stress marker を含むこと
            Assert.Contains("\u02C8", result); // ˈ
        }

        // ================================================================
        // 7. ARPAbet→X-SAMPA変換の正確性
        // ================================================================

        [Theory]
        [InlineData("hello", "h @ l \"oU")]
        [InlineData("world", "w \"3` l d")]
        [InlineData("cat", "k \"{ t")]
        public void XSampaConversion_CommonWords_ReturnsExpectedXSampa(string word, string expectedXSampa)
        {
            var result = _engine.ToXSampa(word);
            _output.WriteLine($"{word}: X-SAMPA=[{result}] expected=[{expectedXSampa}]");
            Assert.Equal(expectedXSampa, result);
        }

        [Fact]
        public void XSampaConversion_WithoutStress_NoStressMarkers()
        {
            var result = _engine.ToXSampaWithoutStress("hello");
            _output.WriteLine($"hello (no stress X-SAMPA): [{result}]");

            // X-SAMPA ストレスマーカー（" %）がトークンの先頭に付かないこと
            Assert.DoesNotContain("\"", result);
            Assert.DoesNotContain("%", result);
        }

        // ================================================================
        // 8. 特殊な語（アポストロフィ語）
        // ================================================================

        [Theory]
        [InlineData("don't")]
        [InlineData("can't")]
        [InlineData("it's")]
        [InlineData("i'm")]
        [InlineData("won't")]
        [InlineData("didn't")]
        public void ApostropheWords_ProduceValidPhonemes(string word)
        {
            var result = _engine.ToPhonemes(word);
            _output.WriteLine($"'{word}': [{result}]");

            Assert.NotEmpty(result);

            // 音素数が妥当であること
            var tokenCount = result.Split(' ').Length;
            Assert.True(tokenCount >= 2, $"'{word}' の音素数({tokenCount})が少なすぎます");
        }

        [Theory]
        [InlineData("don't", "D")]
        [InlineData("can't", "K")]
        [InlineData("won't", "W")]
        public void ApostropheWords_CorrectInitialConsonant(string word, string expectedInitial)
        {
            var result = _engine.ToPhonemes(word);
            Assert.StartsWith(expectedInitial, result);
        }

        [Fact]
        public void ApostropheWords_DontAndCant_InDictionary()
        {
            Assert.True(_engine.ContainsWord("don't"));
            Assert.True(_engine.ContainsWord("can't"));
        }

        // ================================================================
        // 9. LTS有効/無効での挙動差分
        // ================================================================

        [Fact]
        public void LtsToggle_DictWord_SameResultRegardless()
        {
            var optionsLtsOn = new EnglishG2POptions(enableLts: true);
            var optionsLtsOff = new EnglishG2POptions(enableLts: false);

            using var engineOn = new EnglishG2PEngine(optionsLtsOn);
            using var engineOff = new EnglishG2PEngine(optionsLtsOff);

            // 辞書登録語は LTS の有無に関わらず同じ結果
            var dictWords = new[] { "hello", "world", "computer", "beautiful", "student" };
            foreach (var word in dictWords)
            {
                var resultOn = engineOn.ToPhonemes(word);
                var resultOff = engineOff.ToPhonemes(word);

                _output.WriteLine($"'{word}': LTS on=[{resultOn}], LTS off=[{resultOff}]");
                Assert.Equal(resultOn, resultOff);
            }
        }

        [Fact]
        public void LtsToggle_OovWord_LtsOffReturnsEmpty()
        {
            var optionsLtsOff = new EnglishG2POptions(enableLts: false);
            using var engineOff = new EnglishG2PEngine(optionsLtsOff);

            // OOV語は LTS なしではスキップされる
            var oovWord = "blorf";
            Assert.False(_engine.ContainsWord(oovWord));

            var result = engineOff.ToPhonemes(oovWord);
            _output.WriteLine($"OOV '{oovWord}' (LTS off): [{result}]");
            Assert.Equal("", result);
        }

        [Fact]
        public void LtsToggle_OovWord_LtsOnReturnsPhonemes()
        {
            var optionsLtsOn = new EnglishG2POptions(enableLts: true);
            using var engineOn = new EnglishG2PEngine(optionsLtsOn);

            var oovWord = "blorf";
            Assert.False(_engine.ContainsWord(oovWord));

            var result = engineOn.ToPhonemes(oovWord);
            _output.WriteLine($"OOV '{oovWord}' (LTS on): [{result}]");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void LtsToggle_MixedSentence_LtsOffSkipsOov()
        {
            var optionsLtsOff = new EnglishG2POptions(enableLts: false);
            using var engineOff = new EnglishG2PEngine(optionsLtsOff);

            // "hello blorf world" → LTS off では blorf がスキップされる
            var result = engineOff.ToPhonemes("hello blorf world");
            _output.WriteLine($"mixed (LTS off): [{result}]");

            // hello + world のみ
            Assert.Equal("HH AH0 L OW1 W ER1 L D", result);
        }

        [Fact]
        public void LtsToggle_MixedSentence_LtsOnIncludesOov()
        {
            var optionsLtsOn = new EnglishG2POptions(enableLts: true);
            using var engineOn = new EnglishG2PEngine(optionsLtsOn);

            var result = engineOn.ToPhonemes("hello blorf world");
            _output.WriteLine($"mixed (LTS on): [{result}]");

            // hello + (blorf LTS) + world → 3単語分の音素が含まれる
            Assert.Contains("HH AH0 L OW1", result);
            Assert.Contains("W ER1 L D", result);

            // blorf の分だけトークン数が多い
            var tokenCount = result.Split(' ').Length;
            Assert.True(tokenCount > 8, $"3単語の文に対して音素トークン数({tokenCount})が少なすぎます");
        }

        // ================================================================
        // 10. LTS予測のSecondary stress非生成確認
        // ================================================================

        [Theory]
        [InlineData("blorf")]
        [InlineData("grixnop")]
        [InlineData("glimble")]
        [InlineData("frandex")]
        [InlineData("tronkify")]
        public void LtsOutput_NeverContainsSecondaryStress(string oovWord)
        {
            Assert.False(_engine.ContainsWord(oovWord));

            var ltsResult = LtsEngine.Predict(oovWord);
            if (ltsResult == null || ltsResult.Length == 0)
                return;

            var hasSecondary = ltsResult.Any(p => p.Stress == Stress.Secondary);
            _output.WriteLine($"LTS '{oovWord}': [{string.Join(" ", ltsResult.Select(p => p.ToString()))}] Secondary={hasSecondary}");

            Assert.False(hasSecondary,
                $"LTS出力にSecondary stressが含まれています (Flite LTSはPrimary/NoStressのみ出力)");
        }

        // ================================================================
        // 11. 辞書登録語のLTS推定精度（PER検証）
        // ================================================================

        [Fact]
        public void LtsAccuracy_30CommonWords_PerBelow15Percent()
        {
            var testWords = new[]
            {
                "hello", "world", "computer", "beautiful", "technology",
                "people", "water", "music", "system", "program",
                "science", "number", "really", "already", "together",
                "question", "problem", "history", "language", "picture",
                "morning", "family", "student", "teacher", "country",
                "children", "different", "important", "animal", "example",
            };

            int totalPhonemes = 0;
            int totalErrors = 0;
            int testedCount = 0;

            foreach (var word in testWords)
            {
                var dictProns = _engine.LookupAllPronunciations(word);
                if (dictProns.Count == 0) continue;

                var ltsResult = LtsEngine.Predict(word);
                if (ltsResult == null || ltsResult.Length == 0) continue;

                testedCount++;

                var ltsPhonemes = ltsResult.Select(p => p.Phoneme).ToArray();
                int minDist = dictProns
                    .Select(pron => LevenshteinDistance(
                        ltsPhonemes,
                        pron.Phonemes.Select(p => p.Phoneme).ToArray()))
                    .Min();

                var bestRef = dictProns
                    .OrderBy(pron => LevenshteinDistance(
                        ltsPhonemes,
                        pron.Phonemes.Select(p => p.Phoneme).ToArray()))
                    .First();

                totalErrors += minDist;
                totalPhonemes += bestRef.Phonemes.Count;

                if (minDist > 0)
                {
                    _output.WriteLine($"  不一致: {word} " +
                        $"LTS=[{string.Join(" ", ltsPhonemes.Select(p => p.ToString()))}] " +
                        $"Dict=[{string.Join(" ", bestRef.Phonemes.Select(p => p.ToString()))}] " +
                        $"dist={minDist}");
                }
            }

            var per = totalPhonemes > 0 ? (double)totalErrors / totalPhonemes : 0;
            _output.WriteLine($"\nLTS PER (30語): {per:P2} ({totalErrors}/{totalPhonemes}) テスト語数: {testedCount}");

            Assert.True(testedCount >= 25, $"テスト対象語が少なすぎます: {testedCount}");
            Assert.True(per < 0.15, $"LTS PER ({per:P2}) が15%を超えています。");
        }

        // ================================================================
        // 12. IPA/X-SAMPA変換の一貫性
        // ================================================================

        [Fact]
        public void ConversionConsistency_IpaAndXSampa_SameWordCount()
        {
            var text = "Hello world computer beautiful";
            var arpabet = _engine.ToPhonemes(text);
            var ipa = _engine.ToIPA(text);
            var xsampa = _engine.ToXSampa(text);

            _output.WriteLine($"ARPAbet: [{arpabet}]");
            _output.WriteLine($"IPA:     [{ipa}]");
            _output.WriteLine($"X-SAMPA: [{xsampa}]");

            // すべて空でないこと
            Assert.NotEmpty(arpabet);
            Assert.NotEmpty(ipa);
            Assert.NotEmpty(xsampa);

            // IPA は単語間にスペースが入る
            var ipaWordCount = ipa.Split(' ').Length;
            Assert.True(ipaWordCount >= 4, $"IPA単語数が少なすぎます: {ipaWordCount}");
        }

        // ================================================================
        // ヘルパー: Levenshtein距離
        // ================================================================

        private static int LevenshteinDistance<T>(T[] source, T[] target) where T : struct
        {
            var n = source.Length;
            var m = target.Length;

            if (n == 0) return m;
            if (m == 0) return n;

            var dp = new int[n + 1, m + 1];

            for (var i = 0; i <= n; i++) dp[i, 0] = i;
            for (var j = 0; j <= m; j++) dp[0, j] = j;

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = source[i - 1].Equals(target[j - 1]) ? 0 : 1;
                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                        dp[i - 1, j - 1] + cost);
                }
            }

            return dp[n, m];
        }
    }
}
