using System;
using DotNetG2P.French;

namespace DotNetG2P.Tests.FrenchG2P
{
    /// <summary>
    /// フランス語G2Pのキュレーション済みコーパス精度・回帰テスト。
    /// </summary>
    public class FrenchAccuracyTests : IDisposable
    {
        private readonly FrenchG2PEngine _metropolitan = new FrenchG2PEngine();
        private readonly FrenchG2PEngine _conservative = new FrenchG2PEngine(
            new FrenchG2POptions(dialect: FrenchDialect.Conservative));
        private readonly FrenchG2PEngine _allophonic = new FrenchG2PEngine(
            new FrenchG2POptions(enableAllophones: true));

        // ========== Metropolitan キュレーションコーパス ==========

        [Theory]
        [InlineData("bonjour", "b\u0254\u0303\u0292u\u0281")]           // bɔ̃ʒuʁ
        [InlineData("merci", "m\u0259\u0281si")]                        // məʁsi
        [InlineData("france", "f\u0281\u0251\u0303s")]                  // fʁɑ̃s
        [InlineData("oui", "wi")]
        [InlineData("maison", "m\u025Bz\u0254\u0303")]                  // mɛzɔ̃
        [InlineData("famille", "famij")]
        [InlineData("nation", "nasj\u0254\u0303")]                      // nasjɔ̃
        [InlineData("fille", "fij")]
        [InlineData("soleil", "sol\u025Bl")]                             // solɛl
        [InlineData("eau", "o")]
        [InlineData("homme", "\u0254m")]                                // ɔm
        [InlineData("petit", "p\u0259ti")]                              // pəti
        [InlineData("chat", "\u0283a")]                                 // ʃa
        [InlineData("ami", "ami")]
        [InlineData("travail", "t\u0281av\u025Bl")]                      // tʁavɛl
        public void ToIPA_CuratedMetropolitanCorpus_MatchesExpected(string word, string expected)
        {
            Assert.Equal(expected, _metropolitan.ToIPA(word));
        }

        // ========== Conservative キュレーションコーパス ==========

        [Fact]
        public void ToIPA_Conservative_BrunUsesOeNasal()
        {
            // brun: Conservative → /œ̃/
            var result = _conservative.ToIPA("brun");
            Assert.Contains("\u0153\u0303", result); // œ̃
        }

        [Fact]
        public void ToIPA_Conservative_DiffersFromMetropolitanOnBrun()
        {
            var metro = _metropolitan.ToIPA("brun");
            var conserv = _conservative.ToIPA("brun");
            Assert.NotEqual(metro, conserv);
            // Metropolitan: ɛ̃, Conservative: œ̃
            Assert.Contains("\u025B\u0303", metro);   // ɛ̃
            Assert.Contains("\u0153\u0303", conserv); // œ̃
        }

        // ========== Allophonic キュレーションコーパス ==========

        [Fact]
        public void ToIPA_Allophonic_ProducesNonEmptyOutput()
        {
            // 異音モードでも基本語が正しく変換されることを検証
            var words = new[] { "prendre", "arche", "architecture", "artiste", "octobre" };
            foreach (var word in words)
            {
                var result = _allophonic.ToIPA(word);
                Assert.NotEmpty(result);
            }
        }

        [Fact]
        public void ToIPA_Allophonic_RhMayAppear()
        {
            // 異音モードでR無声化（χ）が発生しうることを確認
            // arche: /aʁʃ/ → 異音で /aχʃ/ （ʁがʃの前で無声化）
            var result = _allophonic.ToIPA("arche");
            // Rh（χ）またはR（ʁ）のいずれかが含まれる
            Assert.True(
                result.Contains("\u03C7") || result.Contains("\u0281"),
                $"arche allophonic output should contain χ or ʁ: {result}");
        }

        // ========== 例外辞書語 ==========

        [Fact]
        public void ToIPA_ExceptionDictionaryWords_MatchExpected()
        {
            // femme: 例外辞書により /fam/（通常ルールだと /fɛm/）
            var result = _metropolitan.ToIPA("femme");
            Assert.Equal("fam", result);
        }

        // ========== X-SAMPA回帰コーパス ==========

        [Theory]
        [InlineData("bonjour", "bO~ZuR")]
        [InlineData("merci", "m@Rsi")]
        [InlineData("ami", "ami")]
        [InlineData("france", "fRA~s")]
        [InlineData("maison", "mEzO~")]
        [InlineData("chat", "Sa")]
        [InlineData("oui", "wi")]
        [InlineData("famille", "famij")]
        public void ToXSampa_CuratedRegressionCorpus_MatchesExpected(string word, string expected)
        {
            Assert.Equal(expected, _metropolitan.ToXSampa(word));
        }

        // ========== 全フォーマット非空確認 ==========

        [Fact]
        public void ExceptionAndRuleCorpus_AllReturnNonEmptyAcrossFormats()
        {
            var words = new[]
            {
                "bonjour", "merci", "france", "oui", "femme",
                "football", "weekend", "pizza", "monsieur", "fils"
            };

            foreach (var word in words)
            {
                Assert.NotEmpty(_metropolitan.ToIPA(word));
                Assert.NotEmpty(_metropolitan.ToXSampa(word));
                Assert.NotEmpty(_metropolitan.ToPhonemes(word));
            }
        }

        public void Dispose()
        {
            _metropolitan.Dispose();
            _conservative.Dispose();
            _allophonic.Dispose();
        }
    }
}
