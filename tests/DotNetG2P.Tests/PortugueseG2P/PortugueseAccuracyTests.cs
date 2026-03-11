using System;
using DotNetG2P.Portuguese;

namespace DotNetG2P.Tests.PortugueseG2P
{
    /// <summary>
    /// ポルトガル語G2Pの精度・回帰テスト。
    /// BP/EP各15語以上の高頻度語について具体的IPA期待値を検証する。
    /// </summary>
    public class PortugueseAccuracyTests : IDisposable
    {
        private readonly PortugueseG2PEngine _bp = new PortugueseG2PEngine();
        private readonly PortugueseG2PEngine _ep = new PortugueseG2PEngine(
            new PortugueseG2POptions(dialect: PortugueseDialect.European));
        private readonly PortugueseG2PEngine _bpAllo = new PortugueseG2PEngine(
            new PortugueseG2POptions(enableAllophones: true));
        private readonly PortugueseG2PEngine _epAllo = new PortugueseG2PEngine(
            new PortugueseG2POptions(dialect: PortugueseDialect.European, enableAllophones: true));

        // ========== BP 高頻度語 基本ルール ==========

        [Theory]
        [InlineData("casa", "\u02C8kaza")]
        [InlineData("gato", "\u02C8\u0261ato")]
        [InlineData("mundo", "\u02C8mu\u0303do")]
        [InlineData("tempo", "\u02C8te\u0303po")]
        [InlineData("bonito", "bo\u02C8nito")]
        [InlineData("feliz", "fe\u02C8liz")]
        [InlineData("sol", "\u02C8sol")]
        [InlineData("lua", "\u02C8lwa")]
        [InlineData("amor", "a\u02C8mo\u027E")]
        [InlineData("caf\u00E9", "k\u0250\u02C8f\u025B")]
        [InlineData("ol\u00E1", "o\u02C8la")]
        [InlineData("noite", "\u02C8nojt\u0361\u0283i")]
        [InlineData("porta", "\u02C8p\u0254\u027Et\u0250")]
        [InlineData("leite", "\u02C8lejt\u0361\u0283i")]
        [InlineData("escola", "\u0268s\u02C8k\u0254l\u0250")]
        public void BP_BaseRules_HighFrequencyWords(string word, string expected)
        {
            Assert.Equal(expected, _bp.ToIPA(word));
        }

        // ========== EP 高頻度語 基本ルール ==========

        [Theory]
        [InlineData("casa", "\u02C8kaza")]
        [InlineData("gato", "\u02C8\u0261ato")]
        [InlineData("mundo", "\u02C8mu\u0303do")]
        [InlineData("tempo", "\u02C8te\u0303po")]
        [InlineData("bonito", "bu\u02C8nitu")]
        [InlineData("feliz", "fe\u02C8liz")]
        [InlineData("tipo", "\u02C8tipo")]
        [InlineData("sol", "\u02C8sol")]
        [InlineData("gente", "\u02C8\u0292e\u0303te")]
        [InlineData("nome", "\u02C8nome")]
        [InlineData("porta", "\u02C8p\u0254\u027Et\u0250")]
        [InlineData("leite", "\u02C8lejte")]
        [InlineData("cidade", "si\u02C8dade")]
        [InlineData("pessoa", "pes\u02C8oa")]
        [InlineData("menino", "m\u0268\u02C8ninu")]
        public void EP_BaseRules_HighFrequencyWords(string word, string expected)
        {
            Assert.Equal(expected, _ep.ToIPA(word));
        }

        // ========== 鼻母音・鼻二重母音 ==========

        [Theory]
        [InlineData("n\u00E3o", "\u02C8n\u0250\u0303w\u0303")]
        [InlineData("p\u00E3o", "p\u0250\u0303w\u0303")]
        [InlineData("bom", "\u02C8b\u00F5w\u0303")]
        [InlineData("grande", "\u02C8\u0261\u027E\u0250\u0303de")]
        [InlineData("m\u00E3e", "m\u0250\u0303j\u0303")]
        [InlineData("ch\u00E3o", "\u0283\u0250\u0303w\u0303")]
        [InlineData("cora\u00E7\u00E3o", "ku\u027E\u0250\u02C8s\u0250\u0303w\u0303")]
        public void NasalVowels_MatchExpected(string word, string expected)
        {
            Assert.Equal(expected, _bp.ToIPA(word));
        }

        // ========== BP 異音規則適用時 ==========

        [Theory]
        [InlineData("tipo", "\u02C8t\u0361\u0283ipu")]
        [InlineData("cidade", "si\u02C8dad\u0361\u0292i")]
        [InlineData("gato", "\u02C8\u0261atu")]
        [InlineData("bonito", "bu\u02C8nitu")]
        [InlineData("mesmo", "\u02C8mezmu")]
        [InlineData("sol", "\u02C8sow")]
        [InlineData("escola", "\u0268s\u02C8k\u0254l\u0250")]
        public void BP_Allophones_HighFrequencyWords(string word, string expected)
        {
            Assert.Equal(expected, _bpAllo.ToIPA(word));
        }

        // ========== EP 異音規則適用時 ==========

        [Theory]
        [InlineData("tipo", "\u02C8tipu")]
        [InlineData("cidade", "si\u02C8\u00F0a\u00F0\u0268")]
        [InlineData("mundo", "\u02C8mu\u0303\u00F0u")]
        [InlineData("feliz", "f\u0268\u02C8li\u0292")]
        [InlineData("sol", "\u02C8so\u026B")]
        [InlineData("mesmo", "\u02C8me\u0292mu")]
        [InlineData("escola", "\u0268\u0283\u02C8k\u0254l\u0250")]
        public void EP_Allophones_HighFrequencyWords(string word, string expected)
        {
            Assert.Equal(expected, _epAllo.ToIPA(word));
        }

        // ========== 特殊文字・外来語 ==========

        [Theory]
        [InlineData("fam\u00EDlia", "fa\u02C8milja")]
        [InlineData("m\u00FAsica", "\u02C8muzik\u0250")]
        [InlineData("trabalho", "t\u027Ea\u02C8ba\u028Eo")]
        [InlineData("governo", "\u0261o\u02C8ve\u027Eno")]
        [InlineData("problema", "p\u027Eo\u02C8blema")]
        public void SpecialWords_MatchExpected(string word, string expected)
        {
            Assert.Equal(expected, _bp.ToIPA(word));
        }

        // ========== 全出力形式の一貫性 ==========

        [Fact]
        public void AllFormats_ConsistentForSameWord()
        {
            var words = new[] { "casa", "gato", "mundo", "sol", "amor" };
            foreach (var word in words)
            {
                var ipa = _bp.ToIPA(word);
                var noStress = _bp.ToIPAWithoutStress(word);

                // ストレスなし版はストレスマーカーを除いた文字列と一致
                Assert.Equal(ipa.Replace("\u02C8", "").Replace("\u02CC", ""), noStress);
            }
        }

        // ========== 回帰テスト: 複数語フレーズ ==========

        [Theory]
        [InlineData("o gato", "\u02C8o \u02C8\u0261ato")]
        [InlineData("ol\u00E1 mundo", "o\u02C8la \u02C8mu\u0303do")]
        public void MultiWord_Phrases_MatchExpected(string phrase, string expected)
        {
            Assert.Equal(expected, _bp.ToIPA(phrase));
        }

        public void Dispose()
        {
            _bp.Dispose();
            _ep.Dispose();
            _bpAllo.Dispose();
            _epAllo.Dispose();
        }
    }
}
