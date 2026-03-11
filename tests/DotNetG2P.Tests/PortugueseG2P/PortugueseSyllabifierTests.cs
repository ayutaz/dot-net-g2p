using System.Linq;
using DotNetG2P.Portuguese.Rules;

namespace DotNetG2P.Tests.PortugueseG2P
{
    public class PortugueseSyllabifierTests
    {
        // ========== 基本CV構造 ==========

        [Theory]
        [InlineData("casa", "ca|sa")]
        [InlineData("gato", "ga|to")]
        [InlineData("mesa", "me|sa")]
        [InlineData("bolo", "bo|lo")]
        [InlineData("rua", "rua")]  // u+a は弱+強 → デフォルトで上昇二重母音（1音節）
        public void Syllabify_BasicCV_ReturnsExpectedSplit(string word, string expected)
        {
            var syllables = PortugueseSyllabifier.Syllabify(word);

            Assert.Equal(expected, string.Join("|", syllables.Select(s => s.Text)));
        }

        // ========== 子音クラスタ分割 ==========

        [Theory]
        [InlineData("parte", "par|te")]
        [InlineData("prato", "pra|to")]
        [InlineData("branco", "bran|co")]
        [InlineData("grande", "gran|de")]
        [InlineData("classe", "clas|se")]
        [InlineData("globo", "glo|bo")]
        [InlineData("plano", "pla|no")]
        public void Syllabify_ConsonantCluster_ReturnsExpectedSplit(string word, string expected)
        {
            var syllables = PortugueseSyllabifier.Syllabify(word);

            Assert.Equal(expected, string.Join("|", syllables.Select(s => s.Text)));
        }

        // ========== ダイグラフ (ch, lh, nh) ==========

        [Theory]
        [InlineData("filho", "fi|lho")]
        [InlineData("vinho", "vi|nho")]
        [InlineData("chave", "cha|ve")]
        [InlineData("chuva", "chu|va")]
        [InlineData("abelha", "a|be|lha")]
        public void Syllabify_Digraphs_NotSplit(string word, string expected)
        {
            var syllables = PortugueseSyllabifier.Syllabify(word);

            Assert.Equal(expected, string.Join("|", syllables.Select(s => s.Text)));
        }

        // ========== 二重母音（下降二重母音） ==========

        [Theory]
        [InlineData("pai", "pai")]
        [InlineData("mau", "mau")]
        [InlineData("rei", "rei")]
        [InlineData("meu", "meu")]
        [InlineData("boi", "boi")]
        [InlineData("fui", "fui")]
        public void Syllabify_FallingDiphthong_SingleSyllable(string word, string expected)
        {
            var syllables = PortugueseSyllabifier.Syllabify(word);

            Assert.Equal(expected, string.Join("|", syllables.Select(s => s.Text)));
        }

        // ========== 鼻二重母音 ==========

        [Theory]
        [InlineData("p\u00E3o", "p\u00E3o")]           // pão
        [InlineData("m\u00E3e", "m\u00E3e")]           // mãe
        [InlineData("can\u00E7\u00F5es", "can|\u00E7\u00F5es")] // canções
        public void Syllabify_NasalDiphthong_SamesSyllable(string word, string expected)
        {
            var syllables = PortugueseSyllabifier.Syllabify(word);

            Assert.Equal(expected, string.Join("|", syllables.Select(s => s.Text)));
        }

        // ========== 三重母音 ==========

        [Theory]
        [InlineData("Paraguai", "Pa|ra|guai")]
        public void Syllabify_Triphthong_InSameSyllable(string word, string expected)
        {
            var syllables = PortugueseSyllabifier.Syllabify(word);

            Assert.Equal(expected, string.Join("|", syllables.Select(s => s.Text)));
        }

        // ========== 分離母音 (hiatus) ==========

        [Theory]
        [InlineData("poeta", "po|e|ta")]
        [InlineData("sa\u00EDda", "sa|\u00ED|da")]         // saída
        [InlineData("ba\u00FA", "ba|\u00FA")]               // baú
        [InlineData("caos", "ca|os")]
        public void Syllabify_Hiatus_SplitsIntoSeparateSyllables(string word, string expected)
        {
            var syllables = PortugueseSyllabifier.Syllabify(word);

            Assert.Equal(expected, string.Join("|", syllables.Select(s => s.Text)));
        }

        // ========== 複合子音列 ==========

        [Theory]
        [InlineData("instrumento", "ins|tru|men|to")]
        [InlineData("abstrato", "abs|tra|to")]
        [InlineData("perspicaz", "pers|pi|caz")]
        public void Syllabify_ComplexConsonantCluster_ReturnsExpectedSplit(string word, string expected)
        {
            var syllables = PortugueseSyllabifier.Syllabify(word);

            Assert.Equal(expected, string.Join("|", syllables.Select(s => s.Text)));
        }

        // ========== 単音節語 ==========

        [Theory]
        [InlineData("sol", "sol")]
        [InlineData("mar", "mar")]
        [InlineData("luz", "luz")]
        [InlineData("p\u00E3o", "p\u00E3o")]  // pão
        [InlineData("flor", "flor")]
        public void Syllabify_SingleSyllable_ReturnsSingle(string word, string expected)
        {
            var syllables = PortugueseSyllabifier.Syllabify(word);

            Assert.Single(syllables);
            Assert.Equal(expected, syllables[0].Text);
        }

        // ========== 母音始まり ==========

        [Theory]
        [InlineData("amigo", "a|mi|go")]
        [InlineData("escola", "es|co|la")]
        [InlineData("outro", "ou|tro")]
        public void Syllabify_VowelInitial_ReturnsExpectedSplit(string word, string expected)
        {
            var syllables = PortugueseSyllabifier.Syllabify(word);

            Assert.Equal(expected, string.Join("|", syllables.Select(s => s.Text)));
        }

        // ========== qu/gu のサイレントu ==========

        [Theory]
        [InlineData("quero", "que|ro")]
        [InlineData("guerra", "guer|ra")]
        [InlineData("aquilo", "a|qui|lo")]
        public void Syllabify_SilentU_TreatedAsConsonant(string word, string expected)
        {
            var syllables = PortugueseSyllabifier.Syllabify(word);

            Assert.Equal(expected, string.Join("|", syllables.Select(s => s.Text)));
        }

        // ========== tl は不許容（ポルトガル語固有） ==========

        [Fact]
        public void Syllabify_TlCluster_NotValidOnset()
        {
            // atlas → at|las（tl は有効な onset ではない）
            var syllables = PortugueseSyllabifier.Syllabify("atlas");

            Assert.Equal("at|las", string.Join("|", syllables.Select(s => s.Text)));
        }

        // ========== rr/ss は分割される ==========

        [Theory]
        [InlineData("carro", "car|ro")]
        [InlineData("passo", "pas|so")]
        [InlineData("passado", "pas|sa|do")]
        public void Syllabify_DoubleConsonants_AreSplit(string word, string expected)
        {
            var syllables = PortugueseSyllabifier.Syllabify(word);

            Assert.Equal(expected, string.Join("|", syllables.Select(s => s.Text)));
        }

        // ========== 空文字列・null ==========

        [Fact]
        public void Syllabify_Null_ReturnsEmpty()
        {
            var syllables = PortugueseSyllabifier.Syllabify(null!);

            Assert.Empty(syllables);
        }

        [Fact]
        public void Syllabify_EmptyString_ReturnsEmpty()
        {
            var syllables = PortugueseSyllabifier.Syllabify("");

            Assert.Empty(syllables);
        }

        // ========== 単一母音 ==========

        [Fact]
        public void Syllabify_SingleVowel_ReturnsSingleSyllable()
        {
            var syllables = PortugueseSyllabifier.Syllabify("a");

            Assert.Single(syllables);
            Assert.Equal("a", syllables[0].Text);
        }

        // ========== StartIndex/Length 整合性 ==========

        [Fact]
        public void Syllabify_StartIndexAndLengthAreCorrect()
        {
            var word = "instrumento";
            var syllables = PortugueseSyllabifier.Syllabify(word);

            for (var i = 0; i < syllables.Count; i++)
            {
                var syllable = syllables[i];
                Assert.Equal(syllable.Text, word.Substring(syllable.StartIndex, syllable.Length));
            }
        }

        // ========== 音節連結で元の単語と一致 ==========

        [Theory]
        [InlineData("instrumento")]
        [InlineData("desenvolvimento")]
        [InlineData("universidade")]
        [InlineData("caf\u00E9")]  // café
        public void Syllabify_SyllablesCoverEntireWord(string word)
        {
            var syllables = PortugueseSyllabifier.Syllabify(word);

            Assert.Equal(word, string.Concat(syllables.Select(s => s.Text)));
        }

        // ========== 追加の一般的な単語 ==========

        [Theory]
        [InlineData("bonito", "bo|ni|to")]
        [InlineData("trabalho", "tra|ba|lho")]
        [InlineData("Brasil", "Bra|sil")]
        [InlineData("caf\u00E9", "ca|f\u00E9")]  // café
        [InlineData("cora\u00E7\u00E3o", "co|ra|\u00E7\u00E3o")]  // coração
        public void Syllabify_CommonWords_ReturnsExpectedSplit(string word, string expected)
        {
            var syllables = PortugueseSyllabifier.Syllabify(word);

            Assert.Equal(expected, string.Join("|", syllables.Select(s => s.Text)));
        }

        // ========== 上昇二重母音（デフォルトで同一音節） ==========

        [Theory]
        [InlineData("ciumento", "ciu|men|to")]
        [InlineData("piada", "pia|da")]
        public void Syllabify_RisingDiphthong_DefaultSameSyllable(string word, string expected)
        {
            var syllables = PortugueseSyllabifier.Syllabify(word);

            Assert.Equal(expected, string.Join("|", syllables.Select(s => s.Text)));
        }
    }
}
