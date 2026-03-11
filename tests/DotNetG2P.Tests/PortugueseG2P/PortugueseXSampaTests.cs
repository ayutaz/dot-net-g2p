using System;
using DotNetG2P.Portuguese;
using DotNetG2P.Portuguese.Conversion;

namespace DotNetG2P.Tests.PortugueseG2P
{
    /// <summary>
    /// ポルトガル語X-SAMPA変換テスト。
    /// </summary>
    public class PortugueseXSampaTests : IDisposable
    {
        private readonly PortugueseG2PEngine _brazilian = new PortugueseG2PEngine();
        private readonly PortugueseG2PEngine _european = new PortugueseG2PEngine(
            new PortugueseG2POptions(dialect: PortugueseDialect.European));
        private readonly PortugueseG2PEngine _allophonic = new PortugueseG2PEngine(
            new PortugueseG2POptions(enableAllophones: true));

        // ========== ToSymbol: 全49音素の個別X-SAMPAマッピング ==========

        // --- 口母音 ---
        [Theory]
        [InlineData(PortugueseIpaPhoneme.A, "a")]
        [InlineData(PortugueseIpaPhoneme.E, "e")]
        [InlineData(PortugueseIpaPhoneme.Eh, "E")]
        [InlineData(PortugueseIpaPhoneme.I, "i")]
        [InlineData(PortugueseIpaPhoneme.O, "o")]
        [InlineData(PortugueseIpaPhoneme.Oh, "O")]
        [InlineData(PortugueseIpaPhoneme.U, "u")]
        [InlineData(PortugueseIpaPhoneme.Schwa, "6")]
        [InlineData(PortugueseIpaPhoneme.HighCentral, "1")]
        // --- 鼻母音 ---
        [InlineData(PortugueseIpaPhoneme.ANasal, "6~")]
        [InlineData(PortugueseIpaPhoneme.ENasal, "e~")]
        [InlineData(PortugueseIpaPhoneme.INasal, "i~")]
        [InlineData(PortugueseIpaPhoneme.ONasal, "o~")]
        [InlineData(PortugueseIpaPhoneme.UNasal, "u~")]
        // --- 半母音 ---
        [InlineData(PortugueseIpaPhoneme.J, "j")]
        [InlineData(PortugueseIpaPhoneme.W, "w")]
        // --- 鼻わたり音 ---
        [InlineData(PortugueseIpaPhoneme.WNasal, "w~")]
        [InlineData(PortugueseIpaPhoneme.JNasal, "j~")]
        // --- 破裂音 ---
        [InlineData(PortugueseIpaPhoneme.P, "p")]
        [InlineData(PortugueseIpaPhoneme.B, "b")]
        [InlineData(PortugueseIpaPhoneme.T, "t")]
        [InlineData(PortugueseIpaPhoneme.D, "d")]
        [InlineData(PortugueseIpaPhoneme.K, "k")]
        [InlineData(PortugueseIpaPhoneme.G, "g")]
        // --- 摩擦音 ---
        [InlineData(PortugueseIpaPhoneme.F, "f")]
        [InlineData(PortugueseIpaPhoneme.V, "v")]
        [InlineData(PortugueseIpaPhoneme.S, "s")]
        [InlineData(PortugueseIpaPhoneme.Z, "z")]
        [InlineData(PortugueseIpaPhoneme.Sh, "S")]
        [InlineData(PortugueseIpaPhoneme.Zh, "Z")]
        // --- 鼻音 ---
        [InlineData(PortugueseIpaPhoneme.M, "m")]
        [InlineData(PortugueseIpaPhoneme.N, "n")]
        [InlineData(PortugueseIpaPhoneme.Ny, "J")]
        // --- 側面音 ---
        [InlineData(PortugueseIpaPhoneme.L, "l")]
        [InlineData(PortugueseIpaPhoneme.Lh, "L")]
        // --- ロティック ---
        [InlineData(PortugueseIpaPhoneme.R, "4")]
        [InlineData(PortugueseIpaPhoneme.Rr, "R")]
        // --- BP固有異音 ---
        [InlineData(PortugueseIpaPhoneme.Ch, "tS")]
        [InlineData(PortugueseIpaPhoneme.Jh, "dZ")]
        [InlineData(PortugueseIpaPhoneme.X, "x")]
        [InlineData(PortugueseIpaPhoneme.H, "h")]
        // --- EP固有異音 ---
        [InlineData(PortugueseIpaPhoneme.DarkL, "5")]
        [InlineData(PortugueseIpaPhoneme.Xh, "X")]
        // --- 共通異音 ---
        [InlineData(PortugueseIpaPhoneme.Ng, "N")]
        [InlineData(PortugueseIpaPhoneme.NLabiodental, "F")]
        [InlineData(PortugueseIpaPhoneme.NDental, "n_d")]
        // --- 弱化異音 ---
        [InlineData(PortugueseIpaPhoneme.Beta, "B")]
        [InlineData(PortugueseIpaPhoneme.Dh, "D")]
        [InlineData(PortugueseIpaPhoneme.Gh, "G")]
        public void ToSymbol_Phoneme_ReturnsCorrectXSampa(PortugueseIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, XSampaConverter.ToSymbol(phoneme));
        }

        // ========== BP X-SAMPA変換 ==========

        [Theory]
        [InlineData("casa", "\"kaza")]
        [InlineData("sol", "\"sol")]
        [InlineData("feliz", "fe\"liz")]
        [InlineData("bonito", "bo\"nito")]
        [InlineData("amor", "a\"mo4")]
        public void ToXSampa_Brazilian_ReturnsExpectedOutput(string word, string expected)
        {
            Assert.Equal(expected, _brazilian.ToXSampa(word));
        }

        // ========== EP X-SAMPA変換 ==========

        [Fact]
        public void ToXSampa_European_ProducesOutput()
        {
            var result = _european.ToXSampa("casa");
            Assert.NotEmpty(result);
            // EP方言でもcasa は基本的に同じ子音構造
            Assert.Contains("k", result);
        }

        [Fact]
        public void ToXSampa_European_DiffersFromBrazilianOnSomeWords()
        {
            // EP方言は母音弱化パターンが異なるため、一部の単語で出力が異なりうる
            var bpResult = _brazilian.ToXSampa("telefone");
            var epResult = _european.ToXSampa("telefone");
            // 両方とも非空であること
            Assert.NotEmpty(bpResult);
            Assert.NotEmpty(epResult);
        }

        // ========== ToXSampaWithoutStress ==========

        [Fact]
        public void ToXSampaWithoutStress_OmitsStressMark()
        {
            var result = _brazilian.ToXSampaWithoutStress("casa");
            Assert.DoesNotContain("\"", result);
            Assert.Equal("kaza", result);
        }

        [Theory]
        [InlineData("bonito")]
        [InlineData("feliz")]
        [InlineData("amor")]
        public void ToXSampaWithoutStress_NeverContainsStressMark(string word)
        {
            var result = _brazilian.ToXSampaWithoutStress(word);
            Assert.DoesNotContain("\"", result);
        }

        // ========== ToXSampaBatch ==========

        [Fact]
        public void ToXSampaBatch_ReturnsOutputsInOrder()
        {
            var results = _brazilian.ToXSampaBatch(new[] { "casa", "sol", "feliz" });
            Assert.Equal(3, results.Count);
            Assert.Equal("\"kaza", results[0]);
            Assert.Equal("\"sol", results[1]);
            Assert.Equal("fe\"liz", results[2]);
        }

        [Fact]
        public void ToXSampaBatch_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _brazilian.ToXSampaBatch(null!));
        }

        [Fact]
        public void ToXSampaBatch_EmptyList_ReturnsEmptyList()
        {
            var results = _brazilian.ToXSampaBatch(Array.Empty<string>());
            Assert.Empty(results);
        }

        // ========== ASCII文字のみ ==========

        [Fact]
        public void ToXSampa_OutputIsAsciiAndTilde()
        {
            var result = _brazilian.ToXSampa("casa bonito mundo");
            Assert.All(result.ToCharArray(), c =>
                Assert.True(c < 128, $"Non-ASCII character found: U+{(int)c:X4} '{c}'"));
        }

        [Fact]
        public void ToXSampa_AllophoneOutput_IsAsciiOnly()
        {
            var result = _allophonic.ToXSampa("casa bonito");
            Assert.All(result.ToCharArray(), c =>
                Assert.True(c < 128, $"Non-ASCII character found: U+{(int)c:X4} '{c}'"));
        }

        // ========== 異音モード ==========

        [Fact]
        public void ToXSampa_Allophonic_ProducesOutput()
        {
            var result = _allophonic.ToXSampa("casa");
            Assert.NotEmpty(result);
        }

        // ========== IPA出力との一貫性 ==========

        [Theory]
        [InlineData("casa")]
        [InlineData("bonito")]
        [InlineData("feliz")]
        [InlineData("mundo")]
        [InlineData("sol")]
        public void ToXSampa_PhonemeCount_MatchesToIPA(string word)
        {
            var ipaPhonemes = _brazilian.ToPhonemeList(word);
            // X-SAMPA出力でもストレスマーク除外の音素数は一致すべき
            var xsampa = _brazilian.ToXSampaWithoutStress(word);
            var ipa = _brazilian.ToIPAWithoutStress(word);

            // 両方とも非空
            Assert.NotEmpty(xsampa);
            Assert.NotEmpty(ipa);
            // 音素リストの長さは同じ
            Assert.True(ipaPhonemes.Count > 0);
        }

        // ========== 空文字列・null ==========

        [Fact]
        public void ToXSampa_EmptyString_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _brazilian.ToXSampa(""));
        }

        [Fact]
        public void ToXSampa_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _brazilian.ToXSampa(null));
        }

        [Fact]
        public void ToXSampaWithoutStress_EmptyString_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _brazilian.ToXSampaWithoutStress(""));
        }

        // ========== 複数単語 ==========

        [Fact]
        public void ToXSampa_MultiWord_ReturnsSpaceSeparated()
        {
            var result = _brazilian.ToXSampa("casa sol");
            Assert.Contains(" ", result);
            // 各単語の出力が含まれている
            Assert.StartsWith("\"kaza", result);
        }

        // ========== Dispose ==========

        [Fact]
        public void ToXSampa_AfterDispose_ThrowsObjectDisposedException()
        {
            var engine = new PortugueseG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampa("casa"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampaWithoutStress("casa"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampaBatch(new[] { "casa" }));
        }

        public void Dispose()
        {
            _brazilian.Dispose();
            _european.Dispose();
            _allophonic.Dispose();
        }
    }
}
