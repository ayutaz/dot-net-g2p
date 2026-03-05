using System.Collections.Generic;
using System.Text.RegularExpressions;
using DotNetG2P.JPCommon;
using DotNetG2P.Models;
using Xunit;

namespace DotNetG2P.Tests.JPCommon
{
    /// <summary>
    /// ProsodyFeatures（A1/A2/A3韻律特徴量）のテスト。
    /// </summary>
    public class ProsodyFeaturesTests
    {
        /// <summary>
        /// 基本テスト: "盆栽" のPhonemes/A1/A2/A3の長さが一致することを検証する。
        /// </summary>
        [Fact]
        public void Bonsai_ArrayLengthsMatch()
        {
            var utterance = BuildBonsaiUtterance();
            var features = FullContextLabel.ExtractProsodyFeatures(utterance);

            // 8音素: sil, b, o, N, s, a, i, sil
            Assert.Equal(8, features.Count);
            Assert.Equal(8, features.Phonemes.Count);
            Assert.Equal(8, features.A1.Count);
            Assert.Equal(8, features.A2.Count);
            Assert.Equal(8, features.A3.Count);
        }

        /// <summary>
        /// sil/pauのA値が0であることを検証する。
        /// </summary>
        [Fact]
        public void SilPau_AValuesAreZero()
        {
            var utterance = BuildBonsaiUtterance();
            var features = FullContextLabel.ExtractProsodyFeatures(utterance);

            // 先頭sil
            Assert.Equal("sil", features.Phonemes[0]);
            Assert.Equal(0, features.A1[0]);
            Assert.Equal(0, features.A2[0]);
            Assert.Equal(0, features.A3[0]);

            // 末尾sil
            Assert.Equal("sil", features.Phonemes[features.Count - 1]);
            Assert.Equal(0, features.A1[features.Count - 1]);
            Assert.Equal(0, features.A2[features.Count - 1]);
            Assert.Equal(0, features.A3[features.Count - 1]);
        }

        /// <summary>
        /// "盆栽" (accent=0, 4モーラ: ボンサイ) のA1/A2/A3の具体値を検証する。
        /// accent=0 → NormalizeAccentForA → accent=4
        /// b,o: moraPos=0 → A1=0-4+1=-3, A2=1, A3=4
        /// N:   moraPos=1 → A1=1-4+1=-2, A2=2, A3=3
        /// s,a: moraPos=2 → A1=2-4+1=-1, A2=3, A3=2
        /// i:   moraPos=3 → A1=3-4+1=0,  A2=4, A3=1
        /// </summary>
        [Fact]
        public void Bonsai_CorrectAValues()
        {
            var utterance = BuildBonsaiUtterance();
            var features = FullContextLabel.ExtractProsodyFeatures(utterance);

            // sil(0), b(1), o(2), N(3), s(4), a(5), i(6), sil(7)

            // b (モーラ0: ボ)
            Assert.Equal("b", features.Phonemes[1]);
            Assert.Equal(-3, features.A1[1]);
            Assert.Equal(1, features.A2[1]);
            Assert.Equal(4, features.A3[1]);

            // o (モーラ0: ボ)
            Assert.Equal("o", features.Phonemes[2]);
            Assert.Equal(-3, features.A1[2]);
            Assert.Equal(1, features.A2[2]);
            Assert.Equal(4, features.A3[2]);

            // N (モーラ1: ン)
            Assert.Equal("N", features.Phonemes[3]);
            Assert.Equal(-2, features.A1[3]);
            Assert.Equal(2, features.A2[3]);
            Assert.Equal(3, features.A3[3]);

            // s (モーラ2: サ)
            Assert.Equal("s", features.Phonemes[4]);
            Assert.Equal(-1, features.A1[4]);
            Assert.Equal(3, features.A2[4]);
            Assert.Equal(2, features.A3[4]);

            // a (モーラ2: サ)
            Assert.Equal("a", features.Phonemes[5]);
            Assert.Equal(-1, features.A1[5]);
            Assert.Equal(3, features.A2[5]);
            Assert.Equal(2, features.A3[5]);

            // i (モーラ3: イ)
            Assert.Equal("i", features.Phonemes[6]);
            Assert.Equal(0, features.A1[6]);
            Assert.Equal(4, features.A2[6]);
            Assert.Equal(1, features.A3[6]);
        }

        /// <summary>
        /// ToFullContextLabelsとの一貫性を検証する。
        /// 同じJPUtteranceに対して、HTSラベルからパースしたA値とProsodyFeaturesの値が一致することを確認する。
        /// </summary>
        [Fact]
        public void ConsistentWithFullContextLabels()
        {
            var utterance = BuildBonsaiUtterance();
            var labels = FullContextLabel.Generate(utterance);
            var features = FullContextLabel.ExtractProsodyFeatures(utterance);

            Assert.Equal(labels.Count, features.Count);

            // HTSラベルから /A: フィールドをパースしてProsodyFeaturesと比較
            var aRegex = new Regex(@"/A:(-?\d+|xx)\+(\d+|xx)\+(\d+|xx)/");
            for (int i = 0; i < labels.Count; i++)
            {
                var match = aRegex.Match(labels[i]);
                Assert.True(match.Success, $"Aフィールドのパースに失敗: {labels[i]}");

                string a1Str = match.Groups[1].Value;
                string a2Str = match.Groups[2].Value;
                string a3Str = match.Groups[3].Value;

                if (a1Str == "xx")
                {
                    // pause音素: A値は0
                    Assert.Equal(0, features.A1[i]);
                    Assert.Equal(0, features.A2[i]);
                    Assert.Equal(0, features.A3[i]);
                }
                else
                {
                    // 通常音素: クランプ前の値で比較
                    // HTSラベルではClampSigned/ClampUnsignedが適用されるため、
                    // クランプ範囲内であれば一致する
                    int expectedA1 = int.Parse(a1Str);
                    int expectedA2 = int.Parse(a2Str);
                    int expectedA3 = int.Parse(a3Str);

                    Assert.Equal(expectedA1, features.A1[i]);
                    Assert.Equal(expectedA2, features.A2[i]);
                    Assert.Equal(expectedA3, features.A3[i]);
                }
            }
        }

        /// <summary>
        /// 複数アクセント句の場合にA1/A2/A3の境界が正しいことを検証する。
        /// "これは、盆栽ですか？"
        /// BG1: AP1[コレワ] accent=0, 3モーラ → normalized accent=3
        /// BG2: AP2[ボンサイデスカ] accent=5, 7モーラ
        /// </summary>
        [Fact]
        public void MultipleAccentPhrases_CorrectBoundaries()
        {
            var utterance = BuildKorewaQuestionUtterance();
            var features = FullContextLabel.ExtractProsodyFeatures(utterance);

            // 21音素: sil + k,o,r,e,w,a + pau + b,o,N,s,a,i,d,e,s,U,k,a + sil
            Assert.Equal(21, features.Count);

            // sil(0): pause
            Assert.Equal("sil", features.Phonemes[0]);
            Assert.Equal(0, features.A1[0]);

            // AP1: コレワ (accent=0→normalized=3, 3モーラ)
            // k,o (モーラ0: コ) → A1=0-3+1=-2, A2=1, A3=3
            Assert.Equal("k", features.Phonemes[1]);
            Assert.Equal(-2, features.A1[1]);
            Assert.Equal(1, features.A2[1]);
            Assert.Equal(3, features.A3[1]);

            // r,e (モーラ1: レ) → A1=1-3+1=-1, A2=2, A3=2
            Assert.Equal("r", features.Phonemes[3]);
            Assert.Equal(-1, features.A1[3]);
            Assert.Equal(2, features.A2[3]);
            Assert.Equal(2, features.A3[3]);

            // w,a (モーラ2: ワ) → A1=2-3+1=0, A2=3, A3=1
            Assert.Equal("w", features.Phonemes[5]);
            Assert.Equal(0, features.A1[5]);
            Assert.Equal(3, features.A2[5]);
            Assert.Equal(1, features.A3[5]);

            // pau(7): pause
            Assert.Equal("pau", features.Phonemes[7]);
            Assert.Equal(0, features.A1[7]);
            Assert.Equal(0, features.A2[7]);
            Assert.Equal(0, features.A3[7]);

            // AP2: ボンサイデスカ (accent=5, 7モーラ)
            // b,o (モーラ0: ボ) → A1=0-5+1=-4, A2=1, A3=7
            Assert.Equal("b", features.Phonemes[8]);
            Assert.Equal(-4, features.A1[8]);
            Assert.Equal(1, features.A2[8]);
            Assert.Equal(7, features.A3[8]);

            // k,a (モーラ6: カ) → A1=6-5+1=2, A2=7, A3=1
            Assert.Equal("k", features.Phonemes[18]);
            Assert.Equal(2, features.A1[18]);
            Assert.Equal(7, features.A2[18]);
            Assert.Equal(1, features.A3[18]);

            // 末尾sil(20): pause
            Assert.Equal("sil", features.Phonemes[20]);
            Assert.Equal(0, features.A1[20]);
        }

        /// <summary>
        /// ToFullContextLabelsとの一貫性を複数アクセント句で検証する。
        /// </summary>
        [Fact]
        public void MultipleAccentPhrases_ConsistentWithLabels()
        {
            var utterance = BuildKorewaQuestionUtterance();
            var labels = FullContextLabel.Generate(utterance);
            var features = FullContextLabel.ExtractProsodyFeatures(utterance);

            Assert.Equal(labels.Count, features.Count);

            var aRegex = new Regex(@"/A:(-?\d+|xx)\+(\d+|xx)\+(\d+|xx)/");
            for (int i = 0; i < labels.Count; i++)
            {
                var match = aRegex.Match(labels[i]);
                Assert.True(match.Success);

                string a1Str = match.Groups[1].Value;
                if (a1Str == "xx")
                {
                    Assert.Equal(0, features.A1[i]);
                    Assert.Equal(0, features.A2[i]);
                    Assert.Equal(0, features.A3[i]);
                }
                else
                {
                    Assert.Equal(int.Parse(a1Str), features.A1[i]);
                    Assert.Equal(int.Parse(match.Groups[2].Value), features.A2[i]);
                    Assert.Equal(int.Parse(match.Groups[3].Value), features.A3[i]);
                }
            }
        }

        /// <summary>
        /// ProsodyFeaturesの音素列がHTSラベルの音素と一致することを検証する。
        /// </summary>
        [Fact]
        public void PhonemeSequence_MatchesLabels()
        {
            var utterance = BuildKorewaQuestionUtterance();
            var labels = FullContextLabel.Generate(utterance);
            var features = FullContextLabel.ExtractProsodyFeatures(utterance);

            // HTSラベルから現在音素（-c+のcの部分）を抽出
            var phonemeRegex = new Regex(@"-([a-zA-Z]+)\+");
            for (int i = 0; i < labels.Count; i++)
            {
                var match = phonemeRegex.Match(labels[i]);
                Assert.True(match.Success);
                Assert.Equal(match.Groups[1].Value, features.Phonemes[i]);
            }
        }

        // =====================================================================
        // ヘルパーメソッド（FullContextLabelTestsと同じ構築パターン）
        // =====================================================================

        private static JPUtterance BuildBonsaiUtterance()
        {
            var p_b = new JPPhoneme("b");
            var p_o = new JPPhoneme("o");
            var p_N = new JPPhoneme("N");
            var p_s = new JPPhoneme("s");
            var p_a = new JPPhoneme("a");
            var p_i = new JPPhoneme("i");

            p_b.Next = p_o; p_o.Prev = p_b;
            p_o.Next = p_N; p_N.Prev = p_o;
            p_N.Next = p_s; p_s.Prev = p_N;
            p_s.Next = p_a; p_a.Prev = p_s;
            p_a.Next = p_i; p_i.Prev = p_a;

            var mora_bo = new JPMora();
            mora_bo.Phonemes.Add(p_b); p_b.ParentMora = mora_bo;
            mora_bo.Phonemes.Add(p_o); p_o.ParentMora = mora_bo;

            var mora_n = new JPMora();
            mora_n.Phonemes.Add(p_N); p_N.ParentMora = mora_n;

            var mora_sa = new JPMora();
            mora_sa.Phonemes.Add(p_s); p_s.ParentMora = mora_sa;
            mora_sa.Phonemes.Add(p_a); p_a.ParentMora = mora_sa;

            var mora_i = new JPMora();
            mora_i.Phonemes.Add(p_i); p_i.ParentMora = mora_i;

            var word = new JPWord();
            word.Moras.AddRange(new[] { mora_bo, mora_n, mora_sa, mora_i });
            word.PosId = 39;
            word.CTypeId = null;
            word.CFormId = null;
            foreach (var m in word.Moras) m.ParentWord = word;

            var ap = new JPAccentPhrase();
            ap.AccentType = 0;
            ap.Words.Add(word);
            word.ParentAccentPhrase = ap;
            word.IndexInAccentPhrase = 0;

            int idx = 0;
            foreach (var w in ap.Words)
                foreach (var m in w.Moras)
                    m.IndexInAccentPhrase = idx++;

            var bg = new JPBreathGroup();
            bg.AccentPhrases.Add(ap);
            ap.ParentBreathGroup = bg;
            ap.IndexInBreathGroup = 0;

            var utt = new JPUtterance();
            utt.BreathGroups.Add(bg);
            bg.ParentUtterance = utt;
            bg.IndexInUtterance = 0;

            return utt;
        }

        private static JPUtterance BuildKorewaQuestionUtterance()
        {
            // BG1: "これは"
            var p_k1 = new JPPhoneme("k");
            var p_o1 = new JPPhoneme("o");
            var p_r = new JPPhoneme("r");
            var p_e1 = new JPPhoneme("e");
            var p_w = new JPPhoneme("w");
            var p_a1 = new JPPhoneme("a");

            var mora_ko = new JPMora();
            mora_ko.Phonemes.Add(p_k1); p_k1.ParentMora = mora_ko;
            mora_ko.Phonemes.Add(p_o1); p_o1.ParentMora = mora_ko;

            var mora_re = new JPMora();
            mora_re.Phonemes.Add(p_r); p_r.ParentMora = mora_re;
            mora_re.Phonemes.Add(p_e1); p_e1.ParentMora = mora_re;

            var mora_wa = new JPMora();
            mora_wa.Phonemes.Add(p_w); p_w.ParentMora = mora_wa;
            mora_wa.Phonemes.Add(p_a1); p_a1.ParentMora = mora_wa;

            var word_kore = new JPWord();
            word_kore.PosId = 60;
            word_kore.Moras.AddRange(new[] { mora_ko, mora_re });
            foreach (var m in word_kore.Moras) m.ParentWord = word_kore;

            var word_wa = new JPWord();
            word_wa.PosId = 17;
            word_wa.Moras.Add(mora_wa);
            mora_wa.ParentWord = word_wa;

            var ap1 = new JPAccentPhrase();
            ap1.AccentType = 0;
            ap1.Words.Add(word_kore); word_kore.ParentAccentPhrase = ap1; word_kore.IndexInAccentPhrase = 0;
            ap1.Words.Add(word_wa); word_wa.ParentAccentPhrase = ap1; word_wa.IndexInAccentPhrase = 1;

            int moraIdx = 0;
            foreach (var w in ap1.Words)
                foreach (var m in w.Moras)
                    m.IndexInAccentPhrase = moraIdx++;

            var bg1 = new JPBreathGroup();
            bg1.AccentPhrases.Add(ap1); ap1.ParentBreathGroup = bg1; ap1.IndexInBreathGroup = 0;

            // BG2: "盆栽ですか"
            var p_b = new JPPhoneme("b");
            var p_o2 = new JPPhoneme("o");
            var p_N = new JPPhoneme("N");
            var p_s1 = new JPPhoneme("s");
            var p_a2 = new JPPhoneme("a");
            var p_i = new JPPhoneme("i");
            var p_d = new JPPhoneme("d");
            var p_e2 = new JPPhoneme("e");
            var p_s2 = new JPPhoneme("s");
            var p_U = new JPPhoneme("U");
            var p_k2 = new JPPhoneme("k");
            var p_a3 = new JPPhoneme("a");

            var mora_bo = new JPMora();
            mora_bo.Phonemes.Add(p_b); p_b.ParentMora = mora_bo;
            mora_bo.Phonemes.Add(p_o2); p_o2.ParentMora = mora_bo;

            var mora_n = new JPMora();
            mora_n.Phonemes.Add(p_N); p_N.ParentMora = mora_n;

            var mora_sa = new JPMora();
            mora_sa.Phonemes.Add(p_s1); p_s1.ParentMora = mora_sa;
            mora_sa.Phonemes.Add(p_a2); p_a2.ParentMora = mora_sa;

            var mora_i = new JPMora();
            mora_i.Phonemes.Add(p_i); p_i.ParentMora = mora_i;

            var mora_de = new JPMora();
            mora_de.Phonemes.Add(p_d); p_d.ParentMora = mora_de;
            mora_de.Phonemes.Add(p_e2); p_e2.ParentMora = mora_de;

            var mora_su = new JPMora();
            mora_su.Phonemes.Add(p_s2); p_s2.ParentMora = mora_su;
            mora_su.Phonemes.Add(p_U); p_U.ParentMora = mora_su;

            var mora_ka = new JPMora();
            mora_ka.Phonemes.Add(p_k2); p_k2.ParentMora = mora_ka;
            mora_ka.Phonemes.Add(p_a3); p_a3.ParentMora = mora_ka;

            var word_bonsai = new JPWord();
            word_bonsai.PosId = 39;
            word_bonsai.Moras.AddRange(new[] { mora_bo, mora_n, mora_sa, mora_i });
            foreach (var m in word_bonsai.Moras) m.ParentWord = word_bonsai;

            var word_desu = new JPWord();
            word_desu.PosId = 26;
            word_desu.CTypeId = 44;
            word_desu.CFormId = 5;
            word_desu.Moras.AddRange(new[] { mora_de, mora_su });
            foreach (var m in word_desu.Moras) m.ParentWord = word_desu;

            var word_ka = new JPWord();
            word_ka.PosId = 23;
            word_ka.Moras.Add(mora_ka);
            mora_ka.ParentWord = word_ka;

            var ap2 = new JPAccentPhrase();
            ap2.AccentType = 5;
            ap2.Words.Add(word_bonsai); word_bonsai.ParentAccentPhrase = ap2; word_bonsai.IndexInAccentPhrase = 0;
            ap2.Words.Add(word_desu); word_desu.ParentAccentPhrase = ap2; word_desu.IndexInAccentPhrase = 1;
            ap2.Words.Add(word_ka); word_ka.ParentAccentPhrase = ap2; word_ka.IndexInAccentPhrase = 2;
            ap2.IsInterrogative = true;

            moraIdx = 0;
            foreach (var w in ap2.Words)
                foreach (var m in w.Moras)
                    m.IndexInAccentPhrase = moraIdx++;

            var bg2 = new JPBreathGroup();
            bg2.AccentPhrases.Add(ap2); ap2.ParentBreathGroup = bg2; ap2.IndexInBreathGroup = 0;

            var utt = new JPUtterance();
            utt.BreathGroups.Add(bg1); bg1.ParentUtterance = utt; bg1.IndexInUtterance = 0;
            utt.BreathGroups.Add(bg2); bg2.ParentUtterance = utt; bg2.IndexInUtterance = 1;

            var allPhonemes = new List<JPPhoneme>
            {
                p_k1, p_o1, p_r, p_e1, p_w, p_a1,
                p_b, p_o2, p_N, p_s1, p_a2, p_i,
                p_d, p_e2, p_s2, p_U,
                p_k2, p_a3
            };

            for (int i = 0; i < allPhonemes.Count; i++)
            {
                if (i > 0) allPhonemes[i].Prev = allPhonemes[i - 1];
                if (i < allPhonemes.Count - 1) allPhonemes[i].Next = allPhonemes[i + 1];
            }

            return utt;
        }
    }
}
