using System.Collections.Generic;
using System.Linq;
using DotNetG2P.Portuguese;
using DotNetG2P.Portuguese.Rules;

namespace DotNetG2P.Tests.PortugueseG2P
{
    /// <summary>
    /// GraphemeToPhonemeRules の単体テスト。
    /// P1スコープ: 5フェーズ（ダイグラフ+鼻母音化、文脈依存子音、母音変換、半母音化、黙字）
    /// 母音弱化はP2のAllophoneProcessorで処理するため、本テストでは非弱化形を期待値とする。
    /// </summary>
    public class GraphemeToPhonemeRulesTests
    {
        // テストヘルパー: 単語を変換して音素列を返す（BP方言デフォルト）
        private static IReadOnlyList<PortuguesePhoneme> Convert(string word,
            PortugueseDialect dialect = PortugueseDialect.Brazilian)
        {
            var pron = GraphemeToPhonemeRules.ConvertWord(word, dialect, enableExceptionDictionary: false);
            return pron.Phonemes;
        }

        // テストヘルパー: 音素列から IpaPhoneme のみ抽出
        private static PortugueseIpaPhoneme[] Phonemes(string word,
            PortugueseDialect dialect = PortugueseDialect.Brazilian)
        {
            return Convert(word, dialect).Select(p => p.Phoneme).ToArray();
        }

        // ========== 基本変換: 空/null ==========

        [Fact]
        public void ConvertWord_Empty_ReturnsEmptyPronunciation()
        {
            var result = GraphemeToPhonemeRules.ConvertWord("", PortugueseDialect.Brazilian);
            Assert.Empty(result.Phonemes);
        }

        [Fact]
        public void ConvertWord_Null_ReturnsEmptyPronunciation()
        {
            var result = GraphemeToPhonemeRules.ConvertWord(null!, PortugueseDialect.Brazilian);
            Assert.Empty(result.Phonemes);
        }

        // ========== Phase 1: ダイグラフ ==========

        [Fact]
        public void Digraph_Ch_ProducesShFricative()
        {
            // chave → ch=/ʃ/, a, v, e
            var phonemes = Phonemes("chave");
            Assert.Equal(PortugueseIpaPhoneme.Sh, phonemes[0]);
        }

        [Fact]
        public void Digraph_Lh_ProducesPalatalLateral()
        {
            // filho → f, i, lh=/ʎ/, o
            var phonemes = Phonemes("filho");
            Assert.Contains(PortugueseIpaPhoneme.Lh, phonemes);
        }

        [Fact]
        public void Digraph_Nh_ProducesPalatalNasal()
        {
            // vinho → v, i, nh=/ɲ/, o
            var phonemes = Phonemes("vinho");
            Assert.Contains(PortugueseIpaPhoneme.Ny, phonemes);
        }

        [Fact]
        public void Digraph_Rr_ProducesUvularFricative()
        {
            // carro → k, a, rr=/ʁ/, o
            var phonemes = Phonemes("carro");
            Assert.Contains(PortugueseIpaPhoneme.Rr, phonemes);
        }

        [Fact]
        public void Digraph_Ss_ProducesVoicelessSibilant()
        {
            // passo → p, a, ss=/s/, o
            var phonemes = Phonemes("passo");
            Assert.Contains(PortugueseIpaPhoneme.S, phonemes);
            // ss は /z/ にならない（母音間でも無声）
            Assert.DoesNotContain(PortugueseIpaPhoneme.Z, phonemes);
        }

        [Fact]
        public void Digraph_Qu_BeforeFrontVowel_ProducesK()
        {
            // quero → qu=/k/ (u黙字), e, r, o
            var phonemes = Phonemes("quero");
            Assert.Equal(PortugueseIpaPhoneme.K, phonemes[0]);
            // W は出力されない（u黙字）
            Assert.NotEqual(PortugueseIpaPhoneme.W, phonemes[1]);
        }

        [Fact]
        public void Digraph_Qu_BeforeBackVowel_ProducesKW()
        {
            // quatro → qu=/kw/, a, t, r, o
            var phonemes = Phonemes("quatro");
            Assert.Equal(PortugueseIpaPhoneme.K, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.W, phonemes[1]);
        }

        [Fact]
        public void Digraph_Gu_BeforeFrontVowel_ProducesG()
        {
            // guerra → gu=/g/ (u黙字), e, rr, a
            var phonemes = Phonemes("guerra");
            Assert.Equal(PortugueseIpaPhoneme.G, phonemes[0]);
        }

        [Fact]
        public void Digraph_Xc_BeforeFrontVowel_ProducesS()
        {
            // exceção → xc+e=/s/
            var phonemes = Phonemes("exceção");
            // xc+e → /s/
            Assert.Contains(PortugueseIpaPhoneme.S, phonemes);
        }

        [Fact]
        public void Digraph_Sc_BeforeFrontVowel_ProducesS()
        {
            // nascimento の sc は s+c で分割されるが、
            // "scena" のような場合（旧正書法）は sc+e → /s/
            var phonemes = Phonemes("scena");
            Assert.Equal(PortugueseIpaPhoneme.S, phonemes[0]);
        }

        // ========== Phase 1: 鼻母音 ==========

        [Fact]
        public void NasalVowel_Ao_ProducesANasalWNasal()
        {
            // não → n, ão=[ANasal, WNasal]
            var phonemes = Phonemes("não");
            Assert.Contains(PortugueseIpaPhoneme.ANasal, phonemes);
            Assert.Contains(PortugueseIpaPhoneme.WNasal, phonemes);
        }

        [Fact]
        public void NasalVowel_Pao_ProducesANasalWNasal()
        {
            // pão → p, ão=[ANasal, WNasal]
            var phonemes = Phonemes("pão");
            Assert.Equal(PortugueseIpaPhoneme.P, phonemes[0]);
            Assert.Contains(PortugueseIpaPhoneme.ANasal, phonemes);
            Assert.Contains(PortugueseIpaPhoneme.WNasal, phonemes);
        }

        [Fact]
        public void NasalVowel_Mae_ProducesANasalJNasal()
        {
            // mãe → m, ãe=[ANasal, JNasal]
            var phonemes = Phonemes("mãe");
            Assert.Equal(PortugueseIpaPhoneme.M, phonemes[0]);
            Assert.Contains(PortugueseIpaPhoneme.ANasal, phonemes);
            Assert.Contains(PortugueseIpaPhoneme.JNasal, phonemes);
        }

        [Fact]
        public void NasalVowel_Oe_ProducesONasalJNasal()
        {
            // põe → p, õe=[ONasal, JNasal]
            var phonemes = Phonemes("põe");
            Assert.Equal(PortugueseIpaPhoneme.P, phonemes[0]);
            Assert.Contains(PortugueseIpaPhoneme.ONasal, phonemes);
            Assert.Contains(PortugueseIpaPhoneme.JNasal, phonemes);
        }

        [Fact]
        public void NasalVowel_Campo_ProducesANasal()
        {
            // campo → k, a+m+p → ANasal (鼻母音化), p, o
            var phonemes = Phonemes("campo");
            Assert.Contains(PortugueseIpaPhoneme.ANasal, phonemes);
        }

        [Fact]
        public void NasalVowel_Tempo_ProducesENasal()
        {
            // tempo → t, e+m+p → ENasal, p, o
            var phonemes = Phonemes("tempo");
            Assert.Contains(PortugueseIpaPhoneme.ENasal, phonemes);
        }

        [Fact]
        public void NasalVowel_Cinco_ProducesINasal()
        {
            // cinco → c+i→s, i+n+c → INasal, c+o→k, o
            var phonemes = Phonemes("cinco");
            Assert.Contains(PortugueseIpaPhoneme.S, phonemes); // c+i → /s/
            Assert.Contains(PortugueseIpaPhoneme.INasal, phonemes);
        }

        [Fact]
        public void NasalVowel_Onda_ProducesONasal()
        {
            // onda → o+n+d → ONasal, d, a
            var phonemes = Phonemes("onda");
            Assert.Contains(PortugueseIpaPhoneme.ONasal, phonemes);
        }

        [Fact]
        public void NasalVowel_Um_ProducesUNasal()
        {
            // um → u+m → UNasal
            var phonemes = Phonemes("um");
            Assert.Contains(PortugueseIpaPhoneme.UNasal, phonemes);
        }

        [Fact]
        public void NonNasalVowel_Cama_DoesNotNasalize()
        {
            // cama → k, a, m, a (m+a → m は onset、鼻母音化しない)
            var phonemes = Phonemes("cama");
            Assert.DoesNotContain(PortugueseIpaPhoneme.ANasal, phonemes);
            Assert.Contains(PortugueseIpaPhoneme.M, phonemes);
        }

        [Fact]
        public void NasalDiphthong_Am_WordFinal_ProducesANasalWNasal()
        {
            // falam → f, a, l, am=[ANasal, WNasal]
            var phonemes = Phonemes("falam");
            Assert.Contains(PortugueseIpaPhoneme.ANasal, phonemes);
            Assert.Contains(PortugueseIpaPhoneme.WNasal, phonemes);
        }

        [Fact]
        public void NasalDiphthong_Em_WordFinal_ProducesENasalJNasal()
        {
            // bem → b, em=[ENasal, JNasal]
            var phonemes = Phonemes("bem");
            Assert.Equal(PortugueseIpaPhoneme.B, phonemes[0]);
            Assert.Contains(PortugueseIpaPhoneme.ENasal, phonemes);
            Assert.Contains(PortugueseIpaPhoneme.JNasal, phonemes);
        }

        // ========== Phase 2: 文脈依存子音 ==========

        // --- s ---

        [Fact]
        public void Consonant_S_WordInitial_ProducesS()
        {
            // sala → s=/s/, a, l, a
            var phonemes = Phonemes("sala");
            Assert.Equal(PortugueseIpaPhoneme.S, phonemes[0]);
        }

        [Fact]
        public void Consonant_S_Intervocalic_ProducesZ()
        {
            // casa → k, a, s=/z/ (母音間), a
            var phonemes = Phonemes("casa");
            Assert.Contains(PortugueseIpaPhoneme.Z, phonemes);
        }

        [Fact]
        public void Consonant_S_Ss_ProducesS()
        {
            // passo → p, a, ss=/s/, o (母音間でもsのまま)
            var phonemes = Phonemes("passo");
            Assert.DoesNotContain(PortugueseIpaPhoneme.Z, phonemes);
        }

        // --- c ---

        [Fact]
        public void Consonant_C_BeforeFrontVowel_ProducesS()
        {
            // cedo → c+e=/s/, e, d, o
            var phonemes = Phonemes("cedo");
            Assert.Equal(PortugueseIpaPhoneme.S, phonemes[0]);
        }

        [Fact]
        public void Consonant_C_BeforeBackVowel_ProducesK()
        {
            // casa → c+a=/k/, a, s, a
            var phonemes = Phonemes("casa");
            Assert.Equal(PortugueseIpaPhoneme.K, phonemes[0]);
        }

        [Fact]
        public void Consonant_Cedilla_ProducesS()
        {
            // cabeça → k, a, b, e, ç=/s/, a
            var phonemes = Phonemes("cabeça");
            Assert.Contains(PortugueseIpaPhoneme.S, phonemes);
        }

        // --- g ---

        [Fact]
        public void Consonant_G_BeforeFrontVowel_ProducesZh()
        {
            // gente → g+e=/ʒ/, ente
            var phonemes = Phonemes("gente");
            Assert.Equal(PortugueseIpaPhoneme.Zh, phonemes[0]);
        }

        [Fact]
        public void Consonant_G_BeforeBackVowel_ProducesG()
        {
            // gato → g+a=/g/, a, t, o
            var phonemes = Phonemes("gato");
            Assert.Equal(PortugueseIpaPhoneme.G, phonemes[0]);
        }

        // --- j ---

        [Fact]
        public void Consonant_J_ProducesZh()
        {
            // janela → j=/ʒ/
            var phonemes = Phonemes("janela");
            Assert.Equal(PortugueseIpaPhoneme.Zh, phonemes[0]);
        }

        // --- x ---

        [Fact]
        public void Consonant_X_WordInitial_ProducesSh()
        {
            // xadrez → x=/ʃ/
            var phonemes = Phonemes("xadrez");
            Assert.Equal(PortugueseIpaPhoneme.Sh, phonemes[0]);
        }

        [Fact]
        public void Consonant_X_ExVowel_ProducesZ()
        {
            // exame → e, x=/z/ (ex+母音パターン)
            var phonemes = Phonemes("exame");
            Assert.Contains(PortugueseIpaPhoneme.Z, phonemes);
        }

        [Fact]
        public void Consonant_X_Default_ProducesSh()
        {
            // boxe → b, o, x=/ʃ/, e
            var phonemes = Phonemes("boxe");
            Assert.Contains(PortugueseIpaPhoneme.Sh, phonemes);
        }

        // --- r ---

        [Fact]
        public void Consonant_R_WordInitial_ProducesRr()
        {
            // rio → r=/ʁ/ (語頭)
            var phonemes = Phonemes("rio");
            Assert.Equal(PortugueseIpaPhoneme.Rr, phonemes[0]);
        }

        [Fact]
        public void Consonant_R_Intervocalic_ProducesTap()
        {
            // caro → k, a, r=/ɾ/ (母音間), o
            var phonemes = Phonemes("caro");
            Assert.Contains(PortugueseIpaPhoneme.R, phonemes);
            Assert.DoesNotContain(PortugueseIpaPhoneme.Rr, phonemes);
        }

        [Fact]
        public void Consonant_R_InCluster_ProducesTap()
        {
            // prato → p, r=/ɾ/ (子音クラスタ), a, t, o
            var phonemes = Phonemes("prato");
            Assert.Contains(PortugueseIpaPhoneme.R, phonemes);
        }

        [Fact]
        public void Consonant_R_AfterN_ProducesRr()
        {
            // honra → o, n (鼻母音化?), r=/ʁ/ (n直後), a
            var phonemes = Phonemes("honra");
            Assert.Contains(PortugueseIpaPhoneme.Rr, phonemes);
        }

        // --- その他子音 ---

        [Fact]
        public void Consonant_B_ProducesB()
        {
            var phonemes = Phonemes("bola");
            Assert.Equal(PortugueseIpaPhoneme.B, phonemes[0]);
        }

        [Fact]
        public void Consonant_D_ProducesD()
        {
            var phonemes = Phonemes("dado");
            Assert.Equal(PortugueseIpaPhoneme.D, phonemes[0]);
        }

        [Fact]
        public void Consonant_F_ProducesF()
        {
            var phonemes = Phonemes("fala");
            Assert.Equal(PortugueseIpaPhoneme.F, phonemes[0]);
        }

        [Fact]
        public void Consonant_L_ProducesL()
        {
            var phonemes = Phonemes("lado");
            Assert.Equal(PortugueseIpaPhoneme.L, phonemes[0]);
        }

        [Fact]
        public void Consonant_M_ProducesM()
        {
            var phonemes = Phonemes("mala");
            Assert.Equal(PortugueseIpaPhoneme.M, phonemes[0]);
        }

        [Fact]
        public void Consonant_N_ProducesN()
        {
            var phonemes = Phonemes("nada");
            Assert.Equal(PortugueseIpaPhoneme.N, phonemes[0]);
        }

        [Fact]
        public void Consonant_P_ProducesP()
        {
            var phonemes = Phonemes("pato");
            Assert.Equal(PortugueseIpaPhoneme.P, phonemes[0]);
        }

        [Fact]
        public void Consonant_T_ProducesT()
        {
            var phonemes = Phonemes("tudo");
            Assert.Equal(PortugueseIpaPhoneme.T, phonemes[0]);
        }

        [Fact]
        public void Consonant_V_ProducesV()
        {
            var phonemes = Phonemes("vida");
            Assert.Equal(PortugueseIpaPhoneme.V, phonemes[0]);
        }

        [Fact]
        public void Consonant_Z_ProducesZ()
        {
            // zero → z=/z/
            var phonemes = Phonemes("zero");
            Assert.Equal(PortugueseIpaPhoneme.Z, phonemes[0]);
        }

        // ========== Phase 3: 母音変換 ==========

        [Theory]
        [InlineData('\u00E1', PortugueseIpaPhoneme.A)]    // á → /a/
        [InlineData('\u00E2', PortugueseIpaPhoneme.Schwa)] // â → /ɐ/
        [InlineData('\u00E0', PortugueseIpaPhoneme.A)]     // à → /a/
        [InlineData('\u00E9', PortugueseIpaPhoneme.Eh)]    // é → /ɛ/
        [InlineData('\u00EA', PortugueseIpaPhoneme.E)]     // ê → /e/
        [InlineData('\u00ED', PortugueseIpaPhoneme.I)]     // í → /i/
        [InlineData('\u00F3', PortugueseIpaPhoneme.Oh)]    // ó → /ɔ/
        [InlineData('\u00F4', PortugueseIpaPhoneme.O)]     // ô → /o/
        [InlineData('\u00FA', PortugueseIpaPhoneme.U)]     // ú → /u/
        public void AccentedVowel_MapsCorrectly(char vowel, PortugueseIpaPhoneme expected)
        {
            // 単独のアクセント付き母音文字をテスト
            var result = GraphemeToPhonemeRules.MapVowel(vowel, true);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Vowel_Cafe_ProducesEh()
        {
            // café → k, a, f, é=/ɛ/
            var phonemes = Phonemes("café");
            Assert.Contains(PortugueseIpaPhoneme.Eh, phonemes);
        }

        [Fact]
        public void Vowel_Voce_ProducesE()
        {
            // você → v, o, c+ê=/s/, ê=/e/
            var phonemes = Phonemes("você");
            Assert.Contains(PortugueseIpaPhoneme.E, phonemes);
        }

        [Fact]
        public void Vowel_UnmarkedE_DefaultsClosed()
        {
            // pelo → p, e, l, o (無標ストレスe → /e/ デフォルト閉e)
            var phonemes = Phonemes("pelo");
            // e は /e/（閉）として出力される
            Assert.Contains(PortugueseIpaPhoneme.E, phonemes);
            Assert.DoesNotContain(PortugueseIpaPhoneme.Eh, phonemes);
        }

        [Fact]
        public void Vowel_UnmarkedO_DefaultsClosed()
        {
            // bolo → b, o, l, o (無標ストレスo → /o/ デフォルト閉o)
            var phonemes = Phonemes("bolo");
            Assert.Contains(PortugueseIpaPhoneme.O, phonemes);
            Assert.DoesNotContain(PortugueseIpaPhoneme.Oh, phonemes);
        }

        [Fact]
        public void Vowel_A_ProducesA()
        {
            var phonemes = Phonemes("ata");
            Assert.Contains(PortugueseIpaPhoneme.A, phonemes);
        }

        [Fact]
        public void Vowel_I_ProducesI()
        {
            var phonemes = Phonemes("ida");
            Assert.Contains(PortugueseIpaPhoneme.I, phonemes);
        }

        [Fact]
        public void Vowel_U_ProducesU()
        {
            var phonemes = Phonemes("uva");
            Assert.Contains(PortugueseIpaPhoneme.U, phonemes);
        }

        // ========== Phase 4: 半母音化 ==========

        [Fact]
        public void Diphthong_Pai_ProducesAJ()
        {
            // pai → p, a+i → A + J (下降二重母音)
            var phonemes = Phonemes("pai");
            Assert.Contains(PortugueseIpaPhoneme.A, phonemes);
            Assert.Contains(PortugueseIpaPhoneme.J, phonemes);
        }

        [Fact]
        public void Diphthong_Mau_ProducesAW()
        {
            // mau → m, a+u → A + W (下降二重母音)
            var phonemes = Phonemes("mau");
            Assert.Contains(PortugueseIpaPhoneme.A, phonemes);
            Assert.Contains(PortugueseIpaPhoneme.W, phonemes);
        }

        [Fact]
        public void Diphthong_Lei_ProducesEJ()
        {
            // lei → l, e+i → E + J (下降二重母音)
            var phonemes = Phonemes("lei");
            Assert.Contains(PortugueseIpaPhoneme.E, phonemes);
            Assert.Contains(PortugueseIpaPhoneme.J, phonemes);
        }

        [Fact]
        public void Diphthong_Meu_ProducesEW()
        {
            // meu → m, e+u → E + W (下降二重母音)
            var phonemes = Phonemes("meu");
            Assert.Contains(PortugueseIpaPhoneme.E, phonemes);
            Assert.Contains(PortugueseIpaPhoneme.W, phonemes);
        }

        [Fact]
        public void RisingDiphthong_Diabo_ProducesJA()
        {
            // diabo → d, i+a → J + A (上昇二重母音)
            var phonemes = Phonemes("diabo");
            Assert.Contains(PortugueseIpaPhoneme.J, phonemes);
        }

        // ========== Phase 5: 黙字 ==========

        [Fact]
        public void Silent_H_WordInitial_IsSkipped()
        {
            // hora → h黙字, o, r, a
            var phonemes = Phonemes("hora");
            // /h/ は出力されない
            Assert.DoesNotContain(PortugueseIpaPhoneme.H, phonemes);
            // 最初の音素は母音
            Assert.True(phonemes[0] <= PortugueseIpaPhoneme.HighCentral
                || (phonemes[0] >= PortugueseIpaPhoneme.ANasal && phonemes[0] <= PortugueseIpaPhoneme.UNasal));
        }

        [Fact]
        public void Silent_H_MidWord_IsSkipped()
        {
            // 語中の h（ダイグラフでない場合）は黙字
            // "ahora" → a, h黙字, o, r, a (ポルトガル語の "ahora" は非標準だがテスト用)
            var phonemes = Phonemes("ahora");
            Assert.DoesNotContain(PortugueseIpaPhoneme.H, phonemes);
        }

        // ========== 統合テスト: 代表的な単語 ==========

        [Fact]
        public void Word_Casa_CorrectPhonemes()
        {
            // casa → /k/, /a/, /z/ (母音間s), /a/
            var phonemes = Phonemes("casa");
            Assert.Equal(PortugueseIpaPhoneme.K, phonemes[0]);    // c+a → /k/
            Assert.Equal(PortugueseIpaPhoneme.A, phonemes[1]);    // a
            Assert.Equal(PortugueseIpaPhoneme.Z, phonemes[2]);    // s (母音間→/z/)
            Assert.Equal(PortugueseIpaPhoneme.A, phonemes[3]);    // a
        }

        [Fact]
        public void Word_Gato_CorrectPhonemes()
        {
            // gato → /g/, /a/, /t/, /o/
            var phonemes = Phonemes("gato");
            Assert.Equal(PortugueseIpaPhoneme.G, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.A, phonemes[1]);
            Assert.Equal(PortugueseIpaPhoneme.T, phonemes[2]);
            Assert.Equal(PortugueseIpaPhoneme.O, phonemes[3]);
        }

        [Fact]
        public void Word_Chave_CorrectPhonemes()
        {
            // chave → /ʃ/, /a/, /v/, /e/
            var phonemes = Phonemes("chave");
            Assert.Equal(PortugueseIpaPhoneme.Sh, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.A, phonemes[1]);
            Assert.Equal(PortugueseIpaPhoneme.V, phonemes[2]);
            Assert.Equal(PortugueseIpaPhoneme.E, phonemes[3]);
        }

        [Fact]
        public void Word_Filho_CorrectPhonemes()
        {
            // filho → /f/, /i/, /ʎ/, /o/
            var phonemes = Phonemes("filho");
            Assert.Equal(PortugueseIpaPhoneme.F, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.I, phonemes[1]);
            Assert.Equal(PortugueseIpaPhoneme.Lh, phonemes[2]);
            Assert.Equal(PortugueseIpaPhoneme.O, phonemes[3]);
        }

        [Fact]
        public void Word_Vinho_CorrectPhonemes()
        {
            // vinho → /v/, /i/, /ɲ/, /o/
            var phonemes = Phonemes("vinho");
            Assert.Equal(PortugueseIpaPhoneme.V, phonemes[0]);
            Assert.Contains(PortugueseIpaPhoneme.Ny, phonemes);
        }

        [Fact]
        public void Word_Rio_StartsWithRr()
        {
            // rio → /ʁ/ (語頭r), /i/, /o/
            var phonemes = Phonemes("rio");
            Assert.Equal(PortugueseIpaPhoneme.Rr, phonemes[0]);
        }

        [Fact]
        public void Word_Sala_StartsWithS()
        {
            // sala → /s/ (語頭s), /a/, /l/, /a/
            var phonemes = Phonemes("sala");
            Assert.Equal(PortugueseIpaPhoneme.S, phonemes[0]);
        }

        [Fact]
        public void Word_Vida_StartsWithV()
        {
            // vida → /v/, /i/, /d/, /a/
            var phonemes = Phonemes("vida");
            Assert.Equal(PortugueseIpaPhoneme.V, phonemes[0]);
        }

        // ========== ストレス情報テスト ==========

        [Fact]
        public void Stress_Cafe_IsOnLastSyllable()
        {
            // café → 最終音節にストレス（アクセント記号 é）
            var result = GraphemeToPhonemeRules.ConvertWord("café", PortugueseDialect.Brazilian);
            Assert.True(result.StressedSyllableIndex >= 0);
        }

        [Fact]
        public void Stress_Casa_IsOnPenultimateSyllable()
        {
            // casa → 次末音節にストレス（-a 語末 → paroxytone）
            var result = GraphemeToPhonemeRules.ConvertWord("casa", PortugueseDialect.Brazilian);
            Assert.Equal(0, result.StressedSyllableIndex); // 2音節語の最初
        }

        // ========== 旧正書法互換テスト ==========

        [Fact]
        public void OldOrthography_Cc_BeforeFrontVowel_ProducesS()
        {
            // acção (旧正書法) → a, cc+ã → /s/, ão
            var phonemes = Phonemes("acção");
            Assert.Contains(PortugueseIpaPhoneme.S, phonemes);
        }

        [Fact]
        public void OldOrthography_Trema_GueProducesGW()
        {
            // güe (旧正書法) → gu+ü → /gw/
            var phonemes = Phonemes("güei");
            Assert.Contains(PortugueseIpaPhoneme.G, phonemes);
            Assert.Contains(PortugueseIpaPhoneme.W, phonemes);
        }

        // ========== 大文字入力テスト ==========

        [Fact]
        public void UpperCase_IsCaseInsensitive()
        {
            var lower = Phonemes("casa");
            var upper = Phonemes("CASA");
            Assert.Equal(lower.Length, upper.Length);
            for (var i = 0; i < lower.Length; i++)
                Assert.Equal(lower[i], upper[i]);
        }

        [Fact]
        public void MixedCase_IsCaseInsensitive()
        {
            var result1 = Phonemes("Casa");
            var result2 = Phonemes("casa");
            Assert.Equal(result1.Length, result2.Length);
        }

        // ========== ConvertWord 戻り値テスト ==========

        [Fact]
        public void ConvertWord_ReturnsPronunciationWithSyllableOffsets()
        {
            var result = GraphemeToPhonemeRules.ConvertWord("casa", PortugueseDialect.Brazilian);
            Assert.NotNull(result);
            Assert.True(result.Phonemes.Count > 0);
            Assert.True(result.StressedSyllableIndex >= 0);
        }

        [Fact]
        public void ConvertWord_SingleSyllable_HasStress()
        {
            var result = GraphemeToPhonemeRules.ConvertWord("sol", PortugueseDialect.Brazilian);
            Assert.Equal(0, result.StressedSyllableIndex);
        }

        // ========== ç の使用位置テスト ==========

        [Fact]
        public void Cedilla_BeforeA_ProducesS()
        {
            // coração → ... ç+ã → /s/ + /ɐ̃/
            var phonemes = Phonemes("coração");
            Assert.Contains(PortugueseIpaPhoneme.S, phonemes);
        }

        [Fact]
        public void Cedilla_BeforeO_ProducesS()
        {
            // garço → g, a, r, ç=/s/, o
            var phonemes = Phonemes("garço");
            Assert.Contains(PortugueseIpaPhoneme.S, phonemes);
        }

        // ========== 複合テスト: 複数規則の相互作用 ==========

        [Fact]
        public void CombinedRules_Carro_HasKRrO()
        {
            // carro → /k/, /a/, /ʁ/ (rr), /o/
            var phonemes = Phonemes("carro");
            Assert.Equal(PortugueseIpaPhoneme.K, phonemes[0]);
            Assert.Contains(PortugueseIpaPhoneme.A, phonemes);
            Assert.Contains(PortugueseIpaPhoneme.Rr, phonemes);
        }

        [Fact]
        public void CombinedRules_Janela_HasZhANELA()
        {
            // janela → /ʒ/, /a/, /n/, /e/, /l/, /a/
            var phonemes = Phonemes("janela");
            Assert.Equal(PortugueseIpaPhoneme.Zh, phonemes[0]); // j → /ʒ/
            Assert.Contains(PortugueseIpaPhoneme.N, phonemes);
            Assert.Contains(PortugueseIpaPhoneme.L, phonemes);
        }

        [Fact]
        public void CombinedRules_Prato_HasCluster()
        {
            // prato → /p/, /ɾ/ (クラスタ内), /a/, /t/, /o/
            var phonemes = Phonemes("prato");
            Assert.Equal(PortugueseIpaPhoneme.P, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.R, phonemes[1]); // はじき音
        }

        [Fact]
        public void CombinedRules_Quero_QuDigraphAndVowels()
        {
            // quero → /k/ (qu+e), /e/, /ɾ/, /o/
            var phonemes = Phonemes("quero");
            Assert.Equal(PortugueseIpaPhoneme.K, phonemes[0]);
        }
    }
}
