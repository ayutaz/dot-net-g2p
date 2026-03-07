// Copyright (c) 2026 DotNetG2P Contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using DotNetG2P.English;
using DotNetG2P.English.Normalization;
using DotNetG2P.English.Homograph;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Integration
{
    /// <summary>
    /// 英語G2P正規化・同綴異音語解決の詳細検証テスト。
    /// EnglishNormalizer直接テスト + EnglishG2PEngine統合テスト + HomographResolver検証。
    /// </summary>
    public class EnglishNormalizationVerificationTests : IDisposable
    {
        private readonly EnglishG2PEngine _engine;

        public EnglishNormalizationVerificationTests()
        {
            _engine = new EnglishG2PEngine(new EnglishG2POptions(
                includeStress: true,
                enableNormalization: true,
                enableHomographResolution: true,
                enableLts: true));
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =================================================================
        // 正規化: 数字→英語読み (EnglishNormalizer直接テスト)
        // =================================================================

        [Theory]
        [InlineData("0", "zero")]
        [InlineData("42", "forty two")]
        [InlineData("1000", "one thousand")]
        [InlineData("100", "one hundred")]
        [InlineData("1234", "one thousand two hundred thirty four")]
        public void Normalize_Integer_ConvertsToWords(string input, string expected)
        {
            var result = EnglishNormalizer.Normalize(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("-5", "negative five")]
        [InlineData("-42", "negative forty two")]
        [InlineData("-1000", "negative one thousand")]
        public void Normalize_NegativeInteger_ConvertsToWords(string input, string expected)
        {
            var result = EnglishNormalizer.Normalize(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("3.14", "three point one four")]
        [InlineData("0.5", "zero point five")]
        [InlineData("10.01", "ten point zero one")]
        public void Normalize_Decimal_ConvertsToWords(string input, string expected)
        {
            var result = EnglishNormalizer.Normalize(input);
            Assert.Equal(expected, result);
        }

        // =================================================================
        // 正規化: 序数 (EnglishNormalizer直接テスト)
        // =================================================================

        [Theory]
        [InlineData("1st", "first")]
        [InlineData("2nd", "second")]
        [InlineData("3rd", "third")]
        [InlineData("4th", "fourth")]
        [InlineData("11th", "eleventh")]
        [InlineData("21st", "twenty first")]
        [InlineData("100th", "one hundredth")]
        public void Normalize_Ordinal_ConvertsToWords(string input, string expected)
        {
            var result = EnglishNormalizer.Normalize(input);
            Assert.Equal(expected, result);
        }

        // =================================================================
        // 正規化: 通貨 (EnglishNormalizer直接テスト)
        // =================================================================

        [Fact]
        public void Normalize_Dollar_Integer_ConvertsToWords()
        {
            var result = EnglishNormalizer.Normalize("$1");
            Assert.Equal("one dollar", result);
        }

        [Fact]
        public void Normalize_Dollar_Plural_ConvertsToWords()
        {
            var result = EnglishNormalizer.Normalize("$5");
            Assert.Equal("five dollars", result);
        }

        [Fact]
        public void Normalize_Dollar_WithCents_ConvertsToWords()
        {
            var result = EnglishNormalizer.Normalize("$1.50");
            Assert.Equal("one dollar fifty cents", result);
        }

        [Fact]
        public void Normalize_Dollar_LargeAmount_ConvertsToWords()
        {
            var result = EnglishNormalizer.Normalize("$100");
            Assert.Equal("one hundred dollars", result);
        }

        [Fact]
        public void Normalize_Pound_ConvertsToWords()
        {
            var result = EnglishNormalizer.Normalize("\u00a35");
            Assert.Equal("five pounds", result);
        }

        [Fact]
        public void Normalize_Euro_ConvertsToWords()
        {
            var result = EnglishNormalizer.Normalize("\u20ac10");
            Assert.Equal("ten euros", result);
        }

        [Fact]
        public void Normalize_Yen_ConvertsToWords()
        {
            var result = EnglishNormalizer.Normalize("\u00a5500");
            Assert.Equal("five hundred yen", result);
        }

        // =================================================================
        // 正規化: 時刻 (EnglishNormalizer直接テスト)
        // =================================================================

        [Fact]
        public void Normalize_Time_ThreeThirty()
        {
            var result = EnglishNormalizer.Normalize("3:30");
            Assert.Equal("three thirty", result);
        }

        [Fact]
        public void Normalize_Time_TwelveOClock()
        {
            var result = EnglishNormalizer.Normalize("12:00");
            Assert.Equal("twelve o'clock", result);
        }

        [Fact]
        public void Normalize_Time_EightFifteen()
        {
            var result = EnglishNormalizer.Normalize("8:15");
            Assert.Equal("eight fifteen", result);
        }

        [Fact]
        public void Normalize_Time_OneOhFive()
        {
            var result = EnglishNormalizer.Normalize("1:05");
            Assert.Equal("one oh five", result);
        }

        // =================================================================
        // 正規化: 略語 (EnglishNormalizer直接テスト)
        // =================================================================

        [Theory]
        [InlineData("Dr.", "Doctor")]
        [InlineData("Mr.", "Mister")]
        [InlineData("Mrs.", "Misses")]
        [InlineData("Ms.", "Miz")]
        [InlineData("Prof.", "Professor")]
        public void Normalize_Abbreviation_ExpandsCorrectly(string input, string expected)
        {
            var result = EnglishNormalizer.Normalize(input);
            Assert.Equal(expected, result);
        }

        // =================================================================
        // 正規化: 頭字語 (EnglishNormalizer直接テスト)
        // =================================================================

        [Theory]
        [InlineData("API", "A P I")]
        [InlineData("FBI", "F B I")]
        [InlineData("CPU", "C P U")]
        [InlineData("URL", "U R L")]
        public void Normalize_Acronym_SpellsOut(string input, string expected)
        {
            var result = EnglishNormalizer.Normalize(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Normalize_USA_TreatedAsAcronym()
        {
            // "USA" はヒューリスティックで1語読みと判定される（母音含み子音連続<3）
            // そのままパススルーされる
            var result = EnglishNormalizer.Normalize("USA");
            Assert.Equal("USA", result);
        }

        [Fact]
        public void Normalize_Acronym_NASA_PassesThrough()
        {
            // NASAは1語読み（acronymSet）なので、そのまま通過
            var result = EnglishNormalizer.Normalize("NASA");
            Assert.Equal("NASA", result);
        }

        // =================================================================
        // 正規化: 記号 (EnglishNormalizer直接テスト)
        // =================================================================

        [Theory]
        [InlineData("@", "at")]
        [InlineData("&", "and")]
        [InlineData("%", "percent")]
        [InlineData("+", "plus")]
        [InlineData("#", "hash")]
        public void Normalize_Symbol_ExpandsToName(string input, string expected)
        {
            var result = EnglishNormalizer.Normalize(input);
            Assert.Equal(expected, result);
        }

        // =================================================================
        // 正規化統合: EnglishG2PEngine経由テスト
        // =================================================================

        [Fact]
        public void Engine_NumberNormalization_ProducesPhonemes()
        {
            // "42" → "forty two" → 音素変換
            var result = _engine.ToPhonemes("42");
            Assert.NotEmpty(result);
            // "forty" にはF音素、"two" にはT音素が含まれる
            Assert.Contains("F", result);
            Assert.Contains("T", result);
        }

        [Fact]
        public void Engine_OrdinalNormalization_ProducesPhonemes()
        {
            // "1st" → "first" → 音素変換
            var result = _engine.ToPhonemes("1st");
            Assert.NotEmpty(result);
            Assert.Contains("F", result); // "first" の先頭音素
        }

        [Fact]
        public void Engine_CurrencyNormalization_ProducesPhonemes()
        {
            // "$5" → "five dollars"
            var result = _engine.ToPhonemes("$5");
            Assert.NotEmpty(result);
            // "five" の音素 F AY1 V が含まれる
            Assert.Contains("F", result);
            // "dollars" の音素 D AA1 L ER0 Z が含まれる
            Assert.Contains("D", result);
        }

        [Fact]
        public void Engine_TimeNormalization_ProducesPhonemes()
        {
            // "3:30" → "three thirty" → 音素変換
            var result = _engine.ToPhonemes("3:30");
            Assert.NotEmpty(result);
            Assert.Contains("TH", result); // "three", "thirty" 両方にTH音素
        }

        [Fact]
        public void Engine_AbbreviationNormalization_ProducesPhonemes()
        {
            // "Dr. Smith" → "Doctor Smith" → 音素変換
            var result = _engine.ToPhonemes("Dr. Smith");
            Assert.NotEmpty(result);
            // "Doctor" の音素 D が含まれる
            Assert.Contains("D", result);
        }

        [Fact]
        public void Engine_AcronymSpellOut_ProducesPerLetterPhonemes()
        {
            // "API" → "A P I" → 各文字が辞書検索される
            var result = _engine.ToPhonemes("API");
            Assert.NotEmpty(result);
            // A → AH0 (不定冠詞として), P → P IY1, I → AY1
            Assert.Contains("AH0", result); // A
            Assert.Contains("P IY1", result); // P
            Assert.Contains("AY1", result); // I
        }

        [Fact]
        public void Engine_SymbolNormalization_ProducesPhonemes()
        {
            // "test @ home" → "test at home"
            var result = _engine.ToPhonemes("test @ home");
            Assert.NotEmpty(result);
            // "at" の音素 AE1 T が含まれる
            Assert.Contains("AE", result);
        }

        [Fact]
        public void Engine_MixedNormalization_AllPartsProcessed()
        {
            // 複合入力: 略語 + 数字 + 通常単語
            var result = _engine.ToPhonemes("Dr. Smith has 3 cats");
            Assert.NotEmpty(result);
            // "Doctor"のD、"Smith"のS、"three"のTH、"cats"のK音素
            Assert.Contains("D", result);
            Assert.Contains("TH", result);
            Assert.Contains("K", result);
        }

        // =================================================================
        // 同綴異音語: HomographResolver + エンジン統合テスト
        // =================================================================

        // --- read: 現在形/過去形 ---

        [Fact]
        public void Homograph_Read_VerbContext_ReturnsCurrentTenseVariant()
        {
            // "to read" → 動詞(現在形): R IY1 D (variant 1)
            int variant = HomographResolver.ResolveVariantIndex(new[] { "to", "read" }, 1);
            Assert.Equal(1, variant);
        }

        [Fact]
        public void Homograph_Read_Engine_VerbContext_ProducesRIYD()
        {
            // "to read" → read=動詞: R IY1 D
            var result = _engine.ToPhonemes("to read");
            Assert.Contains("R IY1 D", result);
        }

        // --- live: 動詞/形容詞 ---

        [Fact]
        public void Homograph_Live_VerbContext_ReturnsVariant1()
        {
            // "I live" → live=動詞: variant 1
            int variant = HomographResolver.ResolveVariantIndex(new[] { "I", "live" }, 1);
            Assert.Equal(1, variant);
        }

        [Fact]
        public void Homograph_Live_Engine_VerbVsAdjective()
        {
            // "I live here" → 動詞: L IH1 V
            var verbResult = _engine.ToPhonemes("I live here");
            Assert.Contains("L IH1 V", verbResult);

            // "a live concert" → Phase 2: 冠詞+形容詞+名詞パターン → Adjective → variant 0
            // live: 形容詞 → L AY1 V（ライブの）
            var adjResult = _engine.ToPhonemes("a live concert");
            Assert.Contains("L AY1 V", adjResult);
        }

        // --- present: 名詞/動詞 ---

        [Fact]
        public void Homograph_Present_NounVsVerb_Resolver()
        {
            // "the present" → 名詞: variant 0
            int nounVariant = HomographResolver.ResolveVariantIndex(new[] { "the", "present" }, 1);
            Assert.Equal(0, nounVariant);

            // "will present" → 動詞: variant 1
            int verbVariant = HomographResolver.ResolveVariantIndex(new[] { "will", "present" }, 1);
            Assert.Equal(1, verbVariant);
        }

        [Fact]
        public void Homograph_Present_Engine_NounContext()
        {
            // "the present" → 名詞: P R EH1 Z AH0 N T (variant 0)
            var result = _engine.ToPhonemes("the present");
            Assert.Contains("P R EH1 Z AH0 N T", result);
        }

        [Fact]
        public void Homograph_Present_Engine_VerbContext()
        {
            // "will present" → 動詞: P R IY0 Z EH1 N T (variant 1)
            var result = _engine.ToPhonemes("will present");
            Assert.Contains("P R IY0 Z EH1 N T", result);
        }

        [Fact]
        public void Homograph_Present_Engine_DifferentPhonemes()
        {
            var nounResult = _engine.ToPhonemes("the present");
            var verbResult = _engine.ToPhonemes("will present");
            Assert.NotEqual(nounResult, verbResult);
        }

        // --- record: 名詞/動詞 ---

        [Fact]
        public void Homograph_Record_NounVsVerb_Resolver()
        {
            // "the record" → 名詞: variant 1 (R EH1 K ER0 D)
            int nounVariant = HomographResolver.ResolveVariantIndex(new[] { "the", "record" }, 1);
            Assert.Equal(1, nounVariant);

            // "will record" → 動詞: variant 0 (R AH0 K AO1 R D)
            int verbVariant = HomographResolver.ResolveVariantIndex(new[] { "will", "record" }, 1);
            Assert.Equal(0, verbVariant);
        }

        [Fact]
        public void Homograph_Record_Engine_NounContext()
        {
            // "the record" → R EH1 K ER0 D
            var result = _engine.ToPhonemes("the record");
            Assert.Contains("R EH1 K ER0 D", result);
        }

        [Fact]
        public void Homograph_Record_Engine_VerbContext()
        {
            // "will record" → R AH0 K AO1 R D
            var result = _engine.ToPhonemes("will record");
            Assert.Contains("R AH0 K AO1 R D", result);
        }

        // --- -ate語尾: 名詞/動詞差 ---

        [Fact]
        public void Homograph_Estimate_NounVsVerb_Resolver()
        {
            // "the estimate" → 名詞: variant 0
            int nounVariant = HomographResolver.ResolveVariantIndex(new[] { "the", "estimate" }, 1);
            Assert.Equal(0, nounVariant);

            // "will estimate" → 動詞: variant 1
            int verbVariant = HomographResolver.ResolveVariantIndex(new[] { "will", "estimate" }, 1);
            Assert.Equal(1, verbVariant);
        }

        [Fact]
        public void Homograph_Estimate_Engine_DifferentPhonemes()
        {
            var nounResult = _engine.ToPhonemes("the estimate");
            var verbResult = _engine.ToPhonemes("will estimate");
            Assert.NotEqual(nounResult, verbResult);
        }

        [Fact]
        public void Homograph_Graduate_NounVsVerb_Resolver()
        {
            // "the graduate" → 名詞: variant 0
            int nounVariant = HomographResolver.ResolveVariantIndex(new[] { "the", "graduate" }, 1);
            Assert.Equal(0, nounVariant);

            // "will graduate" → 動詞: variant 1
            int verbVariant = HomographResolver.ResolveVariantIndex(new[] { "will", "graduate" }, 1);
            Assert.Equal(1, verbVariant);
        }

        [Fact]
        public void Homograph_Separate_VerbVsAdjective_Resolver()
        {
            // "will separate" → 動詞: variant 0
            int verbVariant = HomographResolver.ResolveVariantIndex(new[] { "will", "separate" }, 1);
            Assert.Equal(0, verbVariant);
        }

        // --- ストレス移動型: 名詞/動詞差 ---

        [Fact]
        public void Homograph_Object_NounVsVerb_Resolver()
        {
            // "the object" → 名詞: variant 0
            int nounVariant = HomographResolver.ResolveVariantIndex(new[] { "the", "object" }, 1);
            Assert.Equal(0, nounVariant);

            // "will object" → 動詞: variant 1
            int verbVariant = HomographResolver.ResolveVariantIndex(new[] { "will", "object" }, 1);
            Assert.Equal(1, verbVariant);
        }

        [Fact]
        public void Homograph_Object_Engine_DifferentPhonemes()
        {
            var nounResult = _engine.ToPhonemes("the object");
            var verbResult = _engine.ToPhonemes("will object");
            Assert.NotEqual(nounResult, verbResult);
        }

        [Fact]
        public void Homograph_Project_NounVsVerb_Resolver()
        {
            // "the project" → 名詞: variant 0
            int nounVariant = HomographResolver.ResolveVariantIndex(new[] { "the", "project" }, 1);
            Assert.Equal(0, nounVariant);

            // "will project" → 動詞: variant 1
            int verbVariant = HomographResolver.ResolveVariantIndex(new[] { "will", "project" }, 1);
            Assert.Equal(1, verbVariant);
        }

        [Fact]
        public void Homograph_Project_Engine_DifferentPhonemes()
        {
            var nounResult = _engine.ToPhonemes("the project");
            var verbResult = _engine.ToPhonemes("will project");
            Assert.NotEqual(nounResult, verbResult);
        }

        [Fact]
        public void Homograph_Permit_NounVsVerb_Resolver()
        {
            // "the permit" → 名詞: variant 1
            int nounVariant = HomographResolver.ResolveVariantIndex(new[] { "the", "permit" }, 1);
            Assert.Equal(1, nounVariant);

            // "will permit" → 動詞: variant 0
            int verbVariant = HomographResolver.ResolveVariantIndex(new[] { "will", "permit" }, 1);
            Assert.Equal(0, verbVariant);
        }

        [Fact]
        public void Homograph_Conduct_NounVsVerb_Resolver()
        {
            // "the conduct" → 名詞: variant 0
            int nounVariant = HomographResolver.ResolveVariantIndex(new[] { "the", "conduct" }, 1);
            Assert.Equal(0, nounVariant);

            // "will conduct" → 動詞: variant 1
            int verbVariant = HomographResolver.ResolveVariantIndex(new[] { "will", "conduct" }, 1);
            Assert.Equal(1, verbVariant);
        }

        [Fact]
        public void Homograph_Desert_NounVsVerb_Resolver()
        {
            // "the desert" → 名詞: variant 0
            int nounVariant = HomographResolver.ResolveVariantIndex(new[] { "the", "desert" }, 1);
            Assert.Equal(0, nounVariant);

            // "will desert" → 動詞: variant 1
            int verbVariant = HomographResolver.ResolveVariantIndex(new[] { "will", "desert" }, 1);
            Assert.Equal(1, verbVariant);
        }

        // --- 母音変化型 ---

        [Fact]
        public void Homograph_Wind_NounVsVerb_Resolver()
        {
            // "the wind" → 名詞: variant 1 (W IH1 N D)
            int nounVariant = HomographResolver.ResolveVariantIndex(new[] { "the", "wind" }, 1);
            Assert.Equal(1, nounVariant);

            // "will wind" → 動詞: variant 0 (W AY1 N D)
            int verbVariant = HomographResolver.ResolveVariantIndex(new[] { "will", "wind" }, 1);
            Assert.Equal(0, verbVariant);
        }

        [Fact]
        public void Homograph_Wind_Engine_DifferentPhonemes()
        {
            var nounResult = _engine.ToPhonemes("the wind");
            var verbResult = _engine.ToPhonemes("will wind");
            Assert.NotEqual(nounResult, verbResult);
        }

        [Fact]
        public void Homograph_Close_VerbVsAdjective_Resolver()
        {
            // "please close" → 動詞: variant 1
            int verbVariant = HomographResolver.ResolveVariantIndex(new[] { "please", "close" }, 1);
            Assert.Equal(1, verbVariant);
        }

        // --- 同綴異音語解決無効時の比較 ---

        [Fact]
        public void Homograph_Disabled_AlwaysUsesFirstVariant()
        {
            using var engineNoHomograph = new EnglishG2PEngine(new EnglishG2POptions(
                enableHomographResolution: false));

            // record: HomographResolution無効時は常にpronunciations[0]
            var verb = engineNoHomograph.ToPhonemes("will record");
            var noun = engineNoHomograph.ToPhonemes("the record");
            Assert.Equal(verb.Substring(verb.IndexOf("R", StringComparison.Ordinal)),
                         noun.Substring(noun.IndexOf("R", StringComparison.Ordinal)));
        }

        // =================================================================
        // 正規化+同綴異音語の複合テスト
        // =================================================================

        [Fact]
        public void NormalizationAndHomograph_Combined()
        {
            // 正規化（数字展開）と同綴異音語解決が同時に動作
            // "the record is 100" → "the record is one hundred"
            // recordは名詞文脈で R EH1 K ER0 D
            var result = _engine.ToPhonemes("the record is 100");
            Assert.Contains("R EH1 K ER0 D", result);
        }

        [Fact]
        public void NormalizationAndHomograph_AbbreviationPlusHomograph()
        {
            // "Dr. Smith will present" → "Doctor Smith will present"
            // presentは動詞文脈で P R IY0 Z EH1 N T
            var result = _engine.ToPhonemes("Dr. Smith will present");
            Assert.Contains("P R IY0 Z EH1 N T", result);
        }

        // =================================================================
        // ピリオド区切り頭字語テスト
        // =================================================================

        [Fact]
        public void Normalize_PeriodAcronym_US_SpellsOut()
        {
            // "U.S." → "US" → 2文字なので常にスペルアウト → "U S"
            var result = EnglishNormalizer.Normalize("U.S.");
            Assert.Equal("U S", result);
        }

        [Fact]
        public void Normalize_PeriodAcronym_USA_TreatedAsAcronym()
        {
            // "U.S.A." → "USA" → ヒューリスティックで1語読みと判定される
            var result = EnglishNormalizer.Normalize("U.S.A.");
            Assert.Equal("USA", result);
        }

        // =================================================================
        // エッジケース: 正規化
        // =================================================================

        [Fact]
        public void Normalize_EmptyString_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, EnglishNormalizer.Normalize(""));
        }

        [Fact]
        public void Normalize_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, EnglishNormalizer.Normalize(null!));
        }

        [Fact]
        public void Normalize_PureText_PassesThrough()
        {
            var result = EnglishNormalizer.Normalize("hello world");
            Assert.Equal("hello world", result);
        }

        [Fact]
        public void Normalize_CommaNumber_ConvertsToWords()
        {
            var result = EnglishNormalizer.Normalize("1,000");
            Assert.Equal("one thousand", result);
        }

        [Fact]
        public void Normalize_LargeCommaNumber_ConvertsToWords()
        {
            var result = EnglishNormalizer.Normalize("1,000,000");
            Assert.Equal("one million", result);
        }
    }
}
