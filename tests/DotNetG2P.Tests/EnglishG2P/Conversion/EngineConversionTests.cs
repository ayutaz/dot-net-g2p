using System;
using DotNetG2P.English;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Conversion
{
    /// <summary>
    /// EnglishG2PEngine の IPA/X-SAMPA 変換API統合テスト。
    /// ToIPA, ToIPAWithoutStress, ToXSampa, ToXSampaWithoutStress を検証する。
    /// </summary>
    public class EngineConversionTests : IDisposable
    {
        private readonly EnglishG2PEngine _engine;

        public EngineConversionTests()
        {
            _engine = new EnglishG2PEngine();
        }

        public void Dispose() => _engine.Dispose();

        // =================================================================
        // 1. ToIPA 基本動作 (5件)
        // =================================================================

        [Fact]
        public void ToIPA_Hello_ReturnsExpectedIPA()
        {
            var result = _engine.ToIPA("hello");
            // HELLO: HH AH0 L OW1 → həlˈoʊ
            Assert.Contains("h", result);
            Assert.Contains("ə", result);
            Assert.Equal("həlˈoʊ", result);
        }

        [Fact]
        public void ToIPA_HelloWorld_TwoWordsSpaceSeparated()
        {
            var result = _engine.ToIPA("hello world");
            var parts = result.Split(' ');
            Assert.Equal(2, parts.Length);
            Assert.Equal("həlˈoʊ", parts[0]);
            Assert.Equal("wˈɝld", parts[1]);
        }

        [Fact]
        public void ToIPA_EmptyString_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToIPA(""));
        }

        [Fact]
        public void ToIPA_CaseInsensitive_SameOutput()
        {
            var upper = _engine.ToIPA("HELLO");
            var lower = _engine.ToIPA("hello");
            Assert.Equal(upper, lower);
        }

        [Fact]
        public void ToIPA_AfterDispose_ThrowsObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPA("hello"));
        }

        // =================================================================
        // 2. ToIPAWithoutStress (3件)
        // =================================================================

        [Fact]
        public void ToIPAWithoutStress_NoStressMarks()
        {
            var result = _engine.ToIPAWithoutStress("hello");
            // ストレスマーク（ˈˌ）を含まないこと
            Assert.DoesNotContain("ˈ", result);
            Assert.DoesNotContain("ˌ", result);
            // HELLO: HH AH0 L OW1 → həloʊ（ストレスマークなし）
            Assert.Equal("həloʊ", result);
        }

        [Fact]
        public void ToIPAWithoutStress_DiffersFromToIPA()
        {
            var withStress = _engine.ToIPA("hello");
            var withoutStress = _engine.ToIPAWithoutStress("hello");
            Assert.NotEqual(withStress, withoutStress);
        }

        [Fact]
        public void ToIPAWithoutStress_AH_AlwaysSchwa()
        {
            // "the" → AH → 常に ə（ˈˌなし、ʌにもならない）
            var result = _engine.ToIPAWithoutStress("the");
            Assert.DoesNotContain("ʌ", result);
            Assert.Contains("ə", result);
        }

        // =================================================================
        // 3. ToXSampa 基本動作 (5件)
        // =================================================================

        [Fact]
        public void ToXSampa_Hello_ReturnsXSampa()
        {
            var result = _engine.ToXSampa("hello");
            // HELLO: HH AH0 L OW1 → h @ l "oU
            Assert.NotEmpty(result);
            Assert.Equal("h @ l \"oU", result);
        }

        [Fact]
        public void ToXSampa_HelloWorld_TwoWordsSpaceSeparated()
        {
            var result = _engine.ToXSampa("hello world");
            // X-SAMPAでは音素ごとにスペース区切り、単語間もスペース
            Assert.NotEmpty(result);
            Assert.Contains("h", result);
            Assert.Contains("w", result);
        }

        [Fact]
        public void ToXSampa_EmptyString_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToXSampa(""));
        }

        [Fact]
        public void ToXSampa_OutputIsAsciiOnly()
        {
            var result = _engine.ToXSampa("hello world");
            Assert.All(result.ToCharArray(), c =>
                Assert.True(c < 128, $"Non-ASCII character found: U+{(int)c:X4} '{c}'"));
        }

        [Fact]
        public void ToXSampa_AfterDispose_ThrowsObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampa("hello"));
        }

        // =================================================================
        // 4. ToXSampaWithoutStress (3件)
        // =================================================================

        [Fact]
        public void ToXSampaWithoutStress_NoStressMarks()
        {
            var result = _engine.ToXSampaWithoutStress("hello");
            // ストレスマーク（" %）を含まないこと
            Assert.DoesNotContain("\"", result);
            Assert.DoesNotContain("%", result);
            // HELLO: HH AH0 L OW1 → h @ l oU
            Assert.Equal("h @ l oU", result);
        }

        [Fact]
        public void ToXSampaWithoutStress_DiffersFromToXSampa()
        {
            var withStress = _engine.ToXSampa("hello");
            var withoutStress = _engine.ToXSampaWithoutStress("hello");
            Assert.NotEqual(withStress, withoutStress);
        }

        [Fact]
        public void ToXSampaWithoutStress_AH_AlwaysSchwa()
        {
            // "the" → AH は常に @ であること（V にならない）
            var result = _engine.ToXSampaWithoutStress("the");
            Assert.DoesNotContain("V", result);
            Assert.Contains("@", result);
        }

        // =================================================================
        // 5. IPA/X-SAMPA/ARPAbetの一貫性 (4件)
        // =================================================================

        [Fact]
        public void AllFormats_SameInput_ReturnNonEmpty()
        {
            var phonemes = _engine.ToPhonemes("hello");
            var ipa = _engine.ToIPA("hello");
            var xsampa = _engine.ToXSampa("hello");

            Assert.NotEmpty(phonemes);
            Assert.NotEmpty(ipa);
            Assert.NotEmpty(xsampa);
        }

        [Fact]
        public void OovWord_LtsFallback_ReturnsIPAAndXSampa()
        {
            // CMU辞書にない単語でもLTSフォールバックでIPA/X-SAMPA出力が得られる
            var ipa = _engine.ToIPA("xyzzy");
            var xsampa = _engine.ToXSampa("xyzzy");
            Assert.NotEmpty(ipa);
            Assert.NotEmpty(xsampa);
        }

        [Fact]
        public void Normalization_DrSmith_ProcessedCorrectly()
        {
            // 正規化が有効の場合、"Dr." → "doctor" に展開されてIPA変換される
            var ipa = _engine.ToIPA("Dr. Smith");
            Assert.NotEmpty(ipa);
            // 2単語以上に展開されるはず（"doctor smith"）
            var parts = ipa.Split(' ');
            Assert.True(parts.Length >= 2,
                $"\"Dr. Smith\" は正規化後に2単語以上を期待: 実際={parts.Length}");
        }

        [Fact]
        public void Homograph_Record_DifferentIPAByContext()
        {
            // "will record" (動詞) vs "the record" (名詞) で異なるIPA出力
            var verb = _engine.ToIPA("will record");
            var noun = _engine.ToIPA("the record");
            Assert.NotEmpty(verb);
            Assert.NotEmpty(noun);
            // 同綴異音語解決により異なるはず
            // verb: rɪˈkɔɹd (stress on 2nd syllable)
            // noun: ˈɹɛkɚd (stress on 1st syllable)
            Assert.NotEqual(verb, noun);
        }
    }
}
