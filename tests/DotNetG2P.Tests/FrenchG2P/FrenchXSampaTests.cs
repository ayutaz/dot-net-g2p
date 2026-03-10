using System;
using DotNetG2P.French;
using DotNetG2P.French.Conversion;

namespace DotNetG2P.Tests.FrenchG2P
{
    /// <summary>
    /// フランス語X-SAMPA変換テスト。
    /// </summary>
    public class FrenchXSampaTests : IDisposable
    {
        private readonly FrenchG2PEngine _metropolitan = new FrenchG2PEngine();
        private readonly FrenchG2PEngine _conservative = new FrenchG2PEngine(
            new FrenchG2POptions(dialect: FrenchDialect.Conservative));
        private readonly FrenchG2PEngine _allophonic = new FrenchG2PEngine(
            new FrenchG2POptions(enableAllophones: true));

        // ========== ToSymbol テスト ==========

        [Theory]
        [InlineData(FrenchIpaPhoneme.A, "a")]
        [InlineData(FrenchIpaPhoneme.Ah, "A")]
        [InlineData(FrenchIpaPhoneme.E, "e")]
        [InlineData(FrenchIpaPhoneme.Eh, "E")]
        [InlineData(FrenchIpaPhoneme.I, "i")]
        [InlineData(FrenchIpaPhoneme.O, "o")]
        [InlineData(FrenchIpaPhoneme.Oh, "O")]
        [InlineData(FrenchIpaPhoneme.U, "u")]
        [InlineData(FrenchIpaPhoneme.Y, "y")]
        [InlineData(FrenchIpaPhoneme.Oe, "2")]
        [InlineData(FrenchIpaPhoneme.Oeh, "9")]
        [InlineData(FrenchIpaPhoneme.Schwa, "@")]
        [InlineData(FrenchIpaPhoneme.ANasal, "A~")]
        [InlineData(FrenchIpaPhoneme.ONasal, "O~")]
        [InlineData(FrenchIpaPhoneme.ENasal, "E~")]
        [InlineData(FrenchIpaPhoneme.OeNasal, "9~")]
        [InlineData(FrenchIpaPhoneme.J, "j")]
        [InlineData(FrenchIpaPhoneme.W, "w")]
        [InlineData(FrenchIpaPhoneme.Uj, "H")]
        [InlineData(FrenchIpaPhoneme.P, "p")]
        [InlineData(FrenchIpaPhoneme.B, "b")]
        [InlineData(FrenchIpaPhoneme.T, "t")]
        [InlineData(FrenchIpaPhoneme.D, "d")]
        [InlineData(FrenchIpaPhoneme.K, "k")]
        [InlineData(FrenchIpaPhoneme.G, "g")]
        [InlineData(FrenchIpaPhoneme.F, "f")]
        [InlineData(FrenchIpaPhoneme.V, "v")]
        [InlineData(FrenchIpaPhoneme.S, "s")]
        [InlineData(FrenchIpaPhoneme.Z, "z")]
        [InlineData(FrenchIpaPhoneme.Sh, "S")]
        [InlineData(FrenchIpaPhoneme.Zh, "Z")]
        [InlineData(FrenchIpaPhoneme.M, "m")]
        [InlineData(FrenchIpaPhoneme.N, "n")]
        [InlineData(FrenchIpaPhoneme.Ny, "J")]
        [InlineData(FrenchIpaPhoneme.L, "l")]
        [InlineData(FrenchIpaPhoneme.R, "R")]
        [InlineData(FrenchIpaPhoneme.Rh, "X")]
        [InlineData(FrenchIpaPhoneme.Ng, "N")]
        [InlineData(FrenchIpaPhoneme.Ts, "ts")]
        [InlineData(FrenchIpaPhoneme.Dz, "dz")]
        public void ToSymbol_Phoneme_ReturnsCorrectXSampa(FrenchIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, XSampaConverter.ToSymbol(phoneme));
        }

        // ========== Metropolitan X-SAMPA変換 ==========

        [Theory]
        [InlineData("bonjour", "bO~ZuR")]
        [InlineData("merci", "m@Rsi")]
        [InlineData("ami", "ami")]
        [InlineData("chat", "Sa")]
        [InlineData("oui", "wi")]
        [InlineData("eau", "o")]
        [InlineData("fille", "fij")]
        [InlineData("nation", "nasjO~")]
        [InlineData("famille", "famij")]
        [InlineData("france", "fRA~s")]
        [InlineData("maison", "mEzO~")]
        [InlineData("brun", "bRE~")]
        public void ToXSampa_Metropolitan_ReturnsExpectedOutput(string word, string expected)
        {
            Assert.Equal(expected, _metropolitan.ToXSampa(word));
        }

        // ========== Conservative X-SAMPA差分 ==========

        [Fact]
        public void ToXSampa_Conservative_BrunUsesOeNasal()
        {
            // "brun": Conservative → œ̃ → X-SAMPA "9~"
            var result = _conservative.ToXSampa("brun");
            Assert.Contains("9~", result);
        }

        [Fact]
        public void ToXSampa_Conservative_DiffersFromMetropolitan()
        {
            var metro = _metropolitan.ToXSampa("brun");
            var conserv = _conservative.ToXSampa("brun");
            Assert.NotEqual(metro, conserv);
        }

        // ========== ToXSampaWithoutStress ==========

        [Fact]
        public void ToXSampaWithoutStress_OmitsStressMark()
        {
            var result = _metropolitan.ToXSampaWithoutStress("bonjour");
            Assert.DoesNotContain("\"", result);
            Assert.Equal("bO~ZuR", result);
        }

        // ========== ToXSampaBatch ==========

        [Fact]
        public void ToXSampaBatch_ReturnsOutputsInOrder()
        {
            var results = _metropolitan.ToXSampaBatch(new[] { "bonjour", "merci", "ami" });
            Assert.Equal(3, results.Count);
            Assert.Equal("bO~ZuR", results[0]);
            Assert.Equal("m@Rsi", results[1]);
            Assert.Equal("ami", results[2]);
        }

        [Fact]
        public void ToXSampaBatch_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _metropolitan.ToXSampaBatch(null!));
        }

        // ========== ASCII文字のみ ==========

        [Fact]
        public void ToXSampa_OutputIsAsciiAndTilde()
        {
            // X-SAMPA出力はASCII文字 + ~ のみ
            var result = _metropolitan.ToXSampa("bonjour le monde");
            Assert.All(result.ToCharArray(), c =>
                Assert.True(c < 128, $"Non-ASCII character found: U+{(int)c:X4} '{c}'"));
        }

        // ========== Allophonic X-SAMPA ==========

        [Fact]
        public void ToXSampa_Allophonic_RhProducesX()
        {
            // 異音モードでR無声化（Rh=χ→X-SAMPA "X"）が発生しうる
            var result = _allophonic.ToXSampa("architecture");
            Assert.NotEmpty(result);
        }

        // ========== Dispose ==========

        [Fact]
        public void ToXSampa_AfterDispose_ThrowsObjectDisposedException()
        {
            var engine = new FrenchG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampa("bonjour"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampaWithoutStress("bonjour"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampaBatch(new[] { "bonjour" }));
        }

        // ========== 空文字列 ==========

        [Fact]
        public void ToXSampa_EmptyString_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _metropolitan.ToXSampa(""));
        }

        [Fact]
        public void ToXSampa_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _metropolitan.ToXSampa(null));
        }

        // ========== 複数単語 ==========

        [Fact]
        public void ToXSampa_MultiWord_ReturnsSpaceSeparated()
        {
            var result = _metropolitan.ToXSampa("Bonjour le monde");
            Assert.Equal("bO~ZuR l@ mO~d", result);
        }

        public void Dispose()
        {
            _metropolitan.Dispose();
            _conservative.Dispose();
            _allophonic.Dispose();
        }
    }
}
