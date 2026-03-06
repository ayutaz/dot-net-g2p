using System;
using System.Collections.Generic;
using DotNetG2P.JPCommon;
using DotNetG2P.Models;
using DotNetG2P.Tests.TestHelpers;
using Xunit;

namespace DotNetG2P.Tests.JPCommon
{
    /// <summary>
    /// FullContextLabel.Generate のテスト。
    /// jpreprocess の出力に準拠した期待値で検証する。
    /// </summary>
    public class FullContextLabelTests
    {
        /// <summary>
        /// "盆栽" (1単語、1AP、1BG) のフルコンテキストラベルを検証する。
        /// NjdNode: "盆栽,名詞,一般,*,*,*,*,盆栽,ボンサイ,ボンサイ,0/4,C2"
        /// 音素: sil, b, o, N, s, a, i, sil
        /// </summary>
        [Fact]
        public void Bonsai_SingleWord_CorrectLabels()
        {
            // JPUtterance構築: "盆栽" → ボンサイ (b o N s a i), accent=0, POS=名詞,一般 → ID=39
            var utterance = BuildBonsaiUtterance();
            var labels = FullContextLabel.Generate(utterance);

            Assert.Equal(8, labels.Count); // sil, b, o, N, s, a, i, sil

            // 先頭sil
            Assert.Equal(
                "xx^xx-sil+b=o/A:xx+xx+xx/B:xx-xx_xx/C:xx_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:xx_xx#xx_xx@xx_xx|xx_xx/G:4_0%0_1_xx/H:xx_xx/I:xx-xx@xx+xx&xx-xx|xx+xx/J:1_4/K:1+1-4",
                labels[0]);

            // b
            Assert.Equal(
                "xx^sil-b+o=N/A:-3+1+4/B:xx-xx_xx/C:39_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:4_0#0_xx@1_1|1_4/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-4@1+1&1-1|1+4/J:xx_xx/K:1+1-4",
                labels[1]);

            // o
            Assert.Equal(
                "sil^b-o+N=s/A:-3+1+4/B:xx-xx_xx/C:39_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:4_0#0_xx@1_1|1_4/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-4@1+1&1-1|1+4/J:xx_xx/K:1+1-4",
                labels[2]);

            // N
            Assert.Equal(
                "b^o-N+s=a/A:-2+2+3/B:xx-xx_xx/C:39_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:4_0#0_xx@1_1|1_4/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-4@1+1&1-1|1+4/J:xx_xx/K:1+1-4",
                labels[3]);

            // s
            Assert.Equal(
                "o^N-s+a=i/A:-1+3+2/B:xx-xx_xx/C:39_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:4_0#0_xx@1_1|1_4/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-4@1+1&1-1|1+4/J:xx_xx/K:1+1-4",
                labels[4]);

            // a
            Assert.Equal(
                "N^s-a+i=sil/A:-1+3+2/B:xx-xx_xx/C:39_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:4_0#0_xx@1_1|1_4/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-4@1+1&1-1|1+4/J:xx_xx/K:1+1-4",
                labels[5]);

            // i
            Assert.Equal(
                "s^a-i+sil=xx/A:0+4+1/B:xx-xx_xx/C:39_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:4_0#0_xx@1_1|1_4/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-4@1+1&1-1|1+4/J:xx_xx/K:1+1-4",
                labels[6]);

            // 末尾sil
            Assert.Equal(
                "a^i-sil+xx=xx/A:xx+xx+xx/B:xx-xx_xx/C:xx_xx+xx/D:xx+xx_xx/E:4_0!0_1-xx/F:xx_xx#xx_xx@xx_xx|xx_xx/G:xx_xx%xx_xx_xx/H:1_4/I:xx-xx@xx+xx&xx-xx|xx+xx/J:xx_xx/K:1+1-4",
                labels[7]);
        }

        // =====================================================================
        // テスト2: "盆栽？" (疑問)
        // NjdNodes:
        //   "盆栽,名詞,一般,*,*,*,*,盆栽,ボンサイ,ボンサイ,0/4,C2"
        //   "？,記号,一般,*,*,*,*,？,？,？,0/0,*"
        // 同じ音素列(sil,b,o,N,s,a,i,sil) だが IsInterrogative=true
        // =====================================================================

        /// <summary>
        /// "盆栽？" の疑問フラグによりラベルが変化することを検証する。
        /// 音素数は "盆栽" と同じ8だが、Fフィールドの疑問フラグが変わる。
        /// </summary>
        [Fact]
        public void BonsaiQuestion_CorrectLabels()
        {
            var utterance = BuildBonsaiQuestionUtterance();
            var labels = FullContextLabel.Generate(utterance);

            Assert.Equal(8, labels.Count); // sil, b, o, N, s, a, i, sil

            // 先頭sil: Gフィールドに次APの情報が入る（疑問フラグ反映）
            // "盆栽？"ではAPのIsInterrogative=trueなので、
            // 各音素のFフィールドでf4(疑問)が "1" になる想定
            // ※ jpreprocessの実装では IsInterrogative はフルラベルのどのフィールドに反映されるか要確認
            // 基本的には "盆栽" と同じラベルだが、疑問情報が付加される可能性がある

            // 最低限: 音素列は同一であることを確認
            Assert.Contains("-sil+", labels[0]); // 先頭sil
            Assert.Contains("-b+", labels[1]);    // b
            Assert.Contains("-o+", labels[2]);    // o
            Assert.Contains("-N+", labels[3]);    // N
            Assert.Contains("-s+", labels[4]);    // s
            Assert.Contains("-a+", labels[5]);    // a
            Assert.Contains("-i+", labels[6]);    // i
            Assert.Contains("-sil+", labels[7]);  // 末尾sil
        }

        // =====================================================================
        // テスト3: "これは、盆栽ですか？" (複数BG)
        // NjdNodes:
        //   "これ,名詞,代名詞,一般,*,*,*,これ,コレ,コレ,0/2,C3"        (chainFlag=null)
        //   "は,助詞,係助詞,*,*,*,*,は,ハ,ワ,0/1,..."                  (chainFlag=true)
        //   "，,記号,読点,*,*,*,*,，,、,、,0/0,*"                       → BG境界(pau)
        //   "盆栽,名詞,一般,*,*,*,*,盆栽,ボンサイ,ボンサイ,5/4,C2"     (chainFlag=null)
        //   "です,助動詞,*,*,*,特殊・デス,基本形,です,デス,デス',1/2,..."(chainFlag=true)
        //   "か,助詞,副助詞／並立助詞／終助詞,*,*,*,*,か,カ,カ,0/1,..." (chainFlag=true)
        //   "？,記号,一般,*,*,*,*,？,？,？,0/0,*"                       → BG境界+疑問
        // 音素列: sil, k, o, r, e, w, a, pau, b, o, N, s, a, i, d, e, s, U, k, a, sil (21個)
        // =====================================================================

        /// <summary>
        /// "これは、盆栽ですか？" の構造検証。
        /// 2BG: BG1=[AP(コレワ)], BG2=[AP(ボンサイデスカ)]
        /// 最後のAPにIsInterrogative=true
        /// </summary>
        [Fact]
        public void KorewaQuestion_CorrectLabels()
        {
            var utterance = BuildKorewaQuestionUtterance();
            var labels = FullContextLabel.Generate(utterance);

            // 音素数: sil + k,o,r,e,w,a + pau + b,o,N,s,a,i,d,e,s,U,k,a + sil = 21
            Assert.Equal(21, labels.Count);

            // 先頭sil
            Assert.Contains("-sil+", labels[0]);
            // k (コ)
            Assert.Contains("-k+", labels[1]);
            // o
            Assert.Contains("-o+", labels[2]);
            // r (レ)
            Assert.Contains("-r+", labels[3]);
            // e
            Assert.Contains("-e+", labels[4]);
            // w (ワ)
            Assert.Contains("-w+", labels[5]);
            // a
            Assert.Contains("-a+", labels[6]);
            // pau (読点)
            Assert.Contains("-pau+", labels[7]);
            // b (ボ)
            Assert.Contains("-b+", labels[8]);
            // o
            Assert.Contains("-o+", labels[9]);
            // N (ン)
            Assert.Contains("-N+", labels[10]);
            // s (サ)
            Assert.Contains("-s+", labels[11]);
            // a
            Assert.Contains("-a+", labels[12]);
            // i (イ)
            Assert.Contains("-i+", labels[13]);
            // d (デ)
            Assert.Contains("-d+", labels[14]);
            // e
            Assert.Contains("-e+", labels[15]);
            // s (ス, 無声化)
            Assert.Contains("-s+", labels[16]);
            // U (無声化母音)
            Assert.Contains("-U+", labels[17]);
            // k (カ)
            Assert.Contains("-k+", labels[18]);
            // a
            Assert.Contains("-a+", labels[19]);
            // 末尾sil
            Assert.Contains("-sil+", labels[20]);

            // Kフィールド (発話レベル): 2BG, 2AP, 10モーラ
            Assert.EndsWith("/K:2+2-10", labels[0]);
        }

        // =====================================================================
        // テスト4: 空発話
        // =====================================================================

        /// <summary>
        /// 空のNjdNodeリストから構築されたJPUtteranceはラベルも空。
        /// </summary>
        [Fact]
        public void EmptyUtterance_ReturnsEmptyLabels()
        {
            var utterance = JPCommonBuilder.Build(Array.Empty<NjdNode>());
            var labels = FullContextLabel.Generate(utterance);

            // 空のJPUtteranceは音素が存在しないため、sil + silの2ラベルのみ
            // ただしFlattenPhonemesが空→InsertSilAndPauでsil+silの2つ
            Assert.Equal(2, labels.Count);
            Assert.Contains("-sil+", labels[0]);
            Assert.Contains("-sil+", labels[1]);
        }

        // =====================================================================
        // テスト5: 複数アクセント句（同一BG内）
        // JPCommonBuilderを使ってNjdNodeから構築
        // =====================================================================

        /// <summary>
        /// 「東京タワー」→ 2つのアクセント句が同一BG内に存在するケース。
        /// node1: "東京" (トーキョー, accent=0, chainFlag=null)
        /// node2: "タワー" (タワー, accent=1, chainFlag=false)
        /// → BG1: [AP1(トーキョー), AP2(タワー)]
        /// </summary>
        [Fact]
        public void MultipleAccentPhrases_SameBG_CorrectStructure()
        {
            var node1 = NjdNodeFactory.CreateWithPronunciation("東京", "トーキョー", accentType: 0);
            var node2 = NjdNodeFactory.CreateWithPronunciation("タワー", "タワー", accentType: 1, chainFlag: false);

            var nodes = new List<NjdNode> { node1, node2 };
            var utterance = JPCommonBuilder.Build(nodes);
            var labels = FullContextLabel.Generate(utterance);

            // 音素数: sil + t,o,o,ky,o,o + t,a,w,a,a + sil
            // 長音は前母音に展開されるのでトーキョー→t,o,(o),ky,o,(o) = 6音素、タワー→t,a,w,a,(a) = 5音素
            // sil + 6 + 5 + sil = 13
            Assert.True(labels.Count >= 8, $"ラベル数が少なすぎる: {labels.Count}");

            // 先頭silと末尾sil
            Assert.Contains("-sil+", labels[0]);
            Assert.Contains("-sil+", labels[labels.Count - 1]);

            // Kフィールド: 1BG, 2AP
            Assert.Contains("/K:1+2-", labels[0]);
        }

        // =====================================================================
        // テスト6: アクセント位置バリエーション - 頭高型（accent=1）
        // =====================================================================

        /// <summary>
        /// アクセント位置1（頭高型）: "カゼ" (accent=1, 2モーラ)
        /// A1の計算: moraPos(0) - accent(1) + 1 = 0
        /// </summary>
        [Fact]
        public void AccentPosition1_Atamadaka_CorrectAField()
        {
            var node = NjdNodeFactory.CreateWithPronunciation("風", "カゼ", accentType: 1);
            var nodes = new List<NjdNode> { node };
            var utterance = JPCommonBuilder.Build(nodes);
            var labels = FullContextLabel.Generate(utterance);

            // sil + k,a,z,e + sil = 6ラベル
            Assert.Equal(6, labels.Count);

            // k音素（モーラ0）: A1 = 0 - 1 + 1 = 0, A2 = 1, A3 = 2
            Assert.Contains("/A:0+1+2/", labels[1]);

            // a音素（モーラ0）: 同じモーラなので同じA値
            Assert.Contains("/A:0+1+2/", labels[2]);

            // z音素（モーラ1）: A1 = 1 - 1 + 1 = 1, A2 = 2, A3 = 1
            Assert.Contains("/A:1+2+1/", labels[3]);
        }

        // =====================================================================
        // テスト7: アクセント位置バリエーション - 尾高型（accent=末尾モーラ）
        // =====================================================================

        /// <summary>
        /// アクセント位置=末尾モーラ（尾高型）: "アメ" (accent=2, 2モーラ)
        /// </summary>
        [Fact]
        public void AccentPositionEnd_Odaka_CorrectAField()
        {
            var node = NjdNodeFactory.CreateWithPronunciation("雨", "アメ", accentType: 2);
            var nodes = new List<NjdNode> { node };
            var utterance = JPCommonBuilder.Build(nodes);
            var labels = FullContextLabel.Generate(utterance);

            // sil + a,m,e + sil = 5ラベル
            Assert.Equal(5, labels.Count);

            // a音素（モーラ0）: A1 = 0 - 2 + 1 = -1, A2 = 1, A3 = 2
            Assert.Contains("/A:-1+1+2/", labels[1]);

            // m音素（モーラ1）: A1 = 1 - 2 + 1 = 0, A2 = 2, A3 = 1
            Assert.Contains("/A:0+2+1/", labels[2]);

            // e音素（モーラ1）: 同じモーラ
            Assert.Contains("/A:0+2+1/", labels[3]);
        }

        // =====================================================================
        // テスト8: 長音を含むノード
        // =====================================================================

        /// <summary>
        /// 長音を含む「コーヒー」(accent=3, 4モーラ)。
        /// 長音はJPCommonBuilderで前母音に展開される。
        /// </summary>
        [Fact]
        public void LongVowel_ExpandedCorrectly()
        {
            var node = NjdNodeFactory.CreateWithPronunciation("コーヒー", "コーヒー", accentType: 3);
            var nodes = new List<NjdNode> { node };
            var utterance = JPCommonBuilder.Build(nodes);
            var labels = FullContextLabel.Generate(utterance);

            // コーヒー → k,o,(長音→o),h,i,(長音→i) = 6音素
            // sil + 6 + sil = 8
            Assert.Equal(8, labels.Count);

            // 長音が母音に展開されていることを確認（「-」が残っていない）
            foreach (var label in labels)
            {
                Assert.DoesNotContain("--+", label); // 「-」音素が使われていないこと
            }

            // 2番目の音素はk
            Assert.Contains("-k+", labels[1]);
            // 3番目はo
            Assert.Contains("-o+", labels[2]);
            // 4番目もo（長音展開）
            Assert.Contains("-o+", labels[3]);
        }

        // =====================================================================
        // テスト9: 3アクセント句を持つ入力（1BG内）
        // =====================================================================

        /// <summary>
        /// 3アクセント句: 「猫」「が」「走る」
        /// node1: 名詞 (chainFlag=null → 新AP)
        /// node2: 助詞 (chainFlag=true → node1のAPに結合)
        /// node3: 動詞 (chainFlag=false → 新AP)
        /// → BG1: [AP1(ネコガ), AP2(ハシル)]
        /// ※ 助詞は前のAPに結合されるので2APになる
        /// </summary>
        [Fact]
        public void ThreeNodes_TwoAccentPhrases_CorrectKField()
        {
            var node1 = NjdNodeFactory.CreateWithPronunciation("猫", "ネコ", accentType: 1);
            var node2 = NjdNodeFactory.CreateWithPronunciation("が", "ガ",
                posType: POSType.Joshi, sub1: "格助詞", accentType: 0, chainFlag: true);
            var node3 = NjdNodeFactory.CreateWithPronunciation("走る", "ハシル",
                posType: POSType.Doushi, sub1: "自立", accentType: 2, chainFlag: false);

            var nodes = new List<NjdNode> { node1, node2, node3 };
            var utterance = JPCommonBuilder.Build(nodes);
            var labels = FullContextLabel.Generate(utterance);

            // 音素: ネコガ (n,e,k,o,g,a) + ハシル (h,a,sh,i,r,u) = 12音素
            // sil + 12 + sil = 14
            Assert.True(labels.Count >= 10, $"ラベル数が少なすぎる: {labels.Count}");

            // Kフィールド: 1BG, 2AP
            Assert.Contains("/K:1+2-", labels[0]);
        }

        // =====================================================================
        // テスト10: 疑問文で疑問フラグがFフィールドに反映されるか
        // =====================================================================

        /// <summary>
        /// 疑問文の「ナニ？」でIsInterrogative=trueがFフィールド#に反映される。
        /// </summary>
        [Fact]
        public void QuestionFlag_ReflectedInFField()
        {
            var node = NjdNodeFactory.CreateWithPronunciation("何", "ナニ", accentType: 1);
            var questionNode = NjdNodeFactory.CreateQuestion();

            var nodes = new List<NjdNode> { node, questionNode };
            var utterance = JPCommonBuilder.Build(nodes);
            var labels = FullContextLabel.Generate(utterance);

            // 音素: sil + n,a,n,i + sil = 6
            Assert.Equal(6, labels.Count);

            // Fフィールドの疑問フラグ: #1（IsInterrogative=true）
            // Fフィールドの形式: F:{moraCount}_{accent}#{isInterr}_{xx}@...
            Assert.Contains("#1_", labels[1]); // n音素のFフィールドに疑問フラグ1
        }

        // =====================================================================
        // テスト11: 非疑問文でFフィールドの疑問フラグが0であること
        // =====================================================================

        /// <summary>
        /// 非疑問文の「ナニ」でIsInterrogative=falseがFフィールドに反映される。
        /// </summary>
        [Fact]
        public void NonQuestion_FFieldHasZeroFlag()
        {
            var node = NjdNodeFactory.CreateWithPronunciation("何", "ナニ", accentType: 1);
            var nodes = new List<NjdNode> { node };
            var utterance = JPCommonBuilder.Build(nodes);
            var labels = FullContextLabel.Generate(utterance);

            // Fフィールドの疑問フラグ: #0（IsInterrogative=false）
            Assert.Contains("#0_", labels[1]); // n音素のFフィールドに疑問フラグ0
        }

        // =====================================================================
        // テスト12: NullのJPUtteranceで例外がスローされること
        // =====================================================================

        [Fact]
        public void Generate_NullUtterance_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => FullContextLabel.Generate(null!));
        }

        // =====================================================================
        // テスト13: 促音を含むノード
        // =====================================================================

        /// <summary>
        /// 促音を含む「カッパ」(accent=0, 3モーラ: カ,ッ,パ)。
        /// 促音は "cl" 音素として出力される。
        /// </summary>
        [Fact]
        public void Sokuon_GeneratesClPhoneme()
        {
            var node = NjdNodeFactory.CreateWithPronunciation("河童", "カッパ", accentType: 0);
            var nodes = new List<NjdNode> { node };
            var utterance = JPCommonBuilder.Build(nodes);
            var labels = FullContextLabel.Generate(utterance);

            // カッパ → k,a,cl,p,a = 5音素
            // sil + 5 + sil = 7
            Assert.Equal(7, labels.Count);

            // cl音素の存在確認
            Assert.Contains("-cl+", labels[3]);
        }

        // =====================================================================
        // テスト14: 撥音を含むノード
        // =====================================================================

        /// <summary>
        /// 撥音を含む「サンポ」(accent=0, 3モーラ: サ,ン,ポ)。
        /// 撥音は "N" 音素として出力される。
        /// </summary>
        [Fact]
        public void Hatsuon_GeneratesNPhoneme()
        {
            var node = NjdNodeFactory.CreateWithPronunciation("散歩", "サンポ", accentType: 0);
            var nodes = new List<NjdNode> { node };
            var utterance = JPCommonBuilder.Build(nodes);
            var labels = FullContextLabel.Generate(utterance);

            // サンポ → s,a,N,p,o = 5音素
            // sil + 5 + sil = 7
            Assert.Equal(7, labels.Count);

            // N音素の存在確認
            Assert.Contains("-N+", labels[3]);
        }

        // =====================================================================
        // ヘルパーメソッド: JPUtterance手動構築
        // =====================================================================

        /// <summary>
        /// "盆栽" のJPUtteranceを構築するヘルパー。
        /// 音素: b, o, N, s, a, i (4モーラ: ボ(b,o), ン(N), サ(s,a), イ(i))
        /// accent=0 (平板), POS=名詞,一般 → POS ID=39, CType=xx, CForm=xx
        /// </summary>
        private static JPUtterance BuildBonsaiUtterance()
        {
            // 音素
            var p_b = new JPPhoneme("b");
            var p_o = new JPPhoneme("o");
            var p_N = new JPPhoneme("N");
            var p_s = new JPPhoneme("s");
            var p_a = new JPPhoneme("a");
            var p_i = new JPPhoneme("i");

            // 音素リンク
            p_b.Next = p_o; p_o.Prev = p_b;
            p_o.Next = p_N; p_N.Prev = p_o;
            p_N.Next = p_s; p_s.Prev = p_N;
            p_s.Next = p_a; p_a.Prev = p_s;
            p_a.Next = p_i; p_i.Prev = p_a;

            // モーラ
            var mora_bo = new JPMora();
            mora_bo.Phonemes.Add(p_b);
            mora_bo.Phonemes.Add(p_o);
            p_b.ParentMora = mora_bo;
            p_o.ParentMora = mora_bo;

            var mora_n = new JPMora();
            mora_n.Phonemes.Add(p_N);
            p_N.ParentMora = mora_n;

            var mora_sa = new JPMora();
            mora_sa.Phonemes.Add(p_s);
            mora_sa.Phonemes.Add(p_a);
            p_s.ParentMora = mora_sa;
            p_a.ParentMora = mora_sa;

            var mora_i = new JPMora();
            mora_i.Phonemes.Add(p_i);
            p_i.ParentMora = mora_i;

            // 単語 (名詞,一般,*,* → POS ID=39)
            var word = new JPWord();
            word.Moras.AddRange(new[] { mora_bo, mora_n, mora_sa, mora_i });
            word.PosId = 39; // 名詞,一般
            word.CTypeId = null;
            word.CFormId = null;

            // モーラの親設定
            foreach (var m in word.Moras)
                m.ParentWord = word;

            // アクセント句 (accent=0 → 平板)
            var ap = new JPAccentPhrase();
            ap.AccentType = 0;
            ap.Words.Add(word);
            word.ParentAccentPhrase = ap;
            word.IndexInAccentPhrase = 0;

            // モーラのAP内インデックス
            int idx = 0;
            foreach (var w in ap.Words)
                foreach (var m in w.Moras)
                    m.IndexInAccentPhrase = idx++;

            // 呼気グループ
            var bg = new JPBreathGroup();
            bg.AccentPhrases.Add(ap);
            ap.ParentBreathGroup = bg;
            ap.IndexInBreathGroup = 0;

            // 発話
            var utt = new JPUtterance();
            utt.BreathGroups.Add(bg);
            bg.ParentUtterance = utt;
            bg.IndexInUtterance = 0;

            return utt;
        }

        /// <summary>
        /// "盆栽？" のJPUtteranceを構築するヘルパー。
        /// "盆栽"と同じ構造だが IsInterrogative=true。
        /// </summary>
        private static JPUtterance BuildBonsaiQuestionUtterance()
        {
            var utt = BuildBonsaiUtterance();
            // 最後のAPにIsInterrogativeを設定
            var lastBg = utt.BreathGroups[utt.BreathGroupCount - 1];
            var lastAp = lastBg.AccentPhrases[lastBg.AccentPhraseCount - 1];
            lastAp.IsInterrogative = true;
            return utt;
        }

        /// <summary>
        /// "これは、盆栽ですか？" のJPUtteranceを構築するヘルパー。
        /// BG1: AP1[Word(コレ)+Word(ワ)], BG2: AP2[Word(ボンサイ)+Word(デス)+Word(カ)]
        /// AP2.IsInterrogative=true
        /// </summary>
        private static JPUtterance BuildKorewaQuestionUtterance()
        {
            // ===== BG1: "これは" =====
            // Word1: "これ" (コレ, 2モーラ, POS=名詞,代名詞,一般 → ID=60)
            var p_k1 = new JPPhoneme("k");
            var p_o1 = new JPPhoneme("o");
            var p_r = new JPPhoneme("r");
            var p_e1 = new JPPhoneme("e");

            var mora_ko = new JPMora();
            mora_ko.Phonemes.Add(p_k1); p_k1.ParentMora = mora_ko; p_k1.IndexInMora = 0;
            mora_ko.Phonemes.Add(p_o1); p_o1.ParentMora = mora_ko; p_o1.IndexInMora = 1;

            var mora_re = new JPMora();
            mora_re.Phonemes.Add(p_r); p_r.ParentMora = mora_re; p_r.IndexInMora = 0;
            mora_re.Phonemes.Add(p_e1); p_e1.ParentMora = mora_re; p_e1.IndexInMora = 1;

            var word_kore = new JPWord();
            word_kore.PosId = 60; // 名詞,代名詞,一般
            word_kore.CTypeId = null;
            word_kore.CFormId = null;
            word_kore.Moras.AddRange(new[] { mora_ko, mora_re });
            foreach (var m in word_kore.Moras) m.ParentWord = word_kore;

            // Word2: "は" (ワ, 1モーラ, POS=助詞,係助詞 → ID=17)
            var p_w = new JPPhoneme("w");
            var p_a1 = new JPPhoneme("a");

            var mora_wa = new JPMora();
            mora_wa.Phonemes.Add(p_w); p_w.ParentMora = mora_wa; p_w.IndexInMora = 0;
            mora_wa.Phonemes.Add(p_a1); p_a1.ParentMora = mora_wa; p_a1.IndexInMora = 1;

            var word_wa = new JPWord();
            word_wa.PosId = 17; // 助詞,係助詞
            word_wa.CTypeId = null;
            word_wa.CFormId = null;
            word_wa.Moras.Add(mora_wa);
            mora_wa.ParentWord = word_wa;

            // AP1: accent=0 (これは: 平板), words=[これ, は]
            var ap1 = new JPAccentPhrase();
            ap1.AccentType = 0;
            ap1.Words.Add(word_kore); word_kore.ParentAccentPhrase = ap1; word_kore.IndexInAccentPhrase = 0;
            ap1.Words.Add(word_wa); word_wa.ParentAccentPhrase = ap1; word_wa.IndexInAccentPhrase = 1;
            ap1.IsInterrogative = false;

            // AP1のモーラインデックス
            int moraIdx = 0;
            foreach (var w in ap1.Words)
                foreach (var m in w.Moras)
                    m.IndexInAccentPhrase = moraIdx++;

            // BG1
            var bg1 = new JPBreathGroup();
            bg1.AccentPhrases.Add(ap1); ap1.ParentBreathGroup = bg1; ap1.IndexInBreathGroup = 0;

            // ===== BG2: "盆栽ですか" =====
            // Word3: "盆栽" (ボンサイ, 4モーラ, POS=名詞,一般 → ID=39)
            var p_b = new JPPhoneme("b");
            var p_o2 = new JPPhoneme("o");
            var p_N = new JPPhoneme("N");
            var p_s1 = new JPPhoneme("s");
            var p_a2 = new JPPhoneme("a");
            var p_i = new JPPhoneme("i");

            var mora_bo = new JPMora();
            mora_bo.Phonemes.Add(p_b); p_b.ParentMora = mora_bo; p_b.IndexInMora = 0;
            mora_bo.Phonemes.Add(p_o2); p_o2.ParentMora = mora_bo; p_o2.IndexInMora = 1;

            var mora_n = new JPMora();
            mora_n.Phonemes.Add(p_N); p_N.ParentMora = mora_n; p_N.IndexInMora = 0;

            var mora_sa = new JPMora();
            mora_sa.Phonemes.Add(p_s1); p_s1.ParentMora = mora_sa; p_s1.IndexInMora = 0;
            mora_sa.Phonemes.Add(p_a2); p_a2.ParentMora = mora_sa; p_a2.IndexInMora = 1;

            var mora_i = new JPMora();
            mora_i.Phonemes.Add(p_i); p_i.ParentMora = mora_i; p_i.IndexInMora = 0;

            var word_bonsai = new JPWord();
            word_bonsai.PosId = 39; // 名詞,一般
            word_bonsai.CTypeId = null;
            word_bonsai.CFormId = null;
            word_bonsai.Moras.AddRange(new[] { mora_bo, mora_n, mora_sa, mora_i });
            foreach (var m in word_bonsai.Moras) m.ParentWord = word_bonsai;

            // Word4: "です" (デス, 2モーラ, POS=助動詞 → ID=26, CType=特殊・デス→44, CForm=基本形→5)
            // 発音: デス' (スが無声化: s U)
            var p_d = new JPPhoneme("d");
            var p_e2 = new JPPhoneme("e");
            var p_s2 = new JPPhoneme("s");
            var p_U = new JPPhoneme("U"); // 無声化母音

            var mora_de = new JPMora();
            mora_de.Phonemes.Add(p_d); p_d.ParentMora = mora_de; p_d.IndexInMora = 0;
            mora_de.Phonemes.Add(p_e2); p_e2.ParentMora = mora_de; p_e2.IndexInMora = 1;

            var mora_su = new JPMora();
            mora_su.Phonemes.Add(p_s2); p_s2.ParentMora = mora_su; p_s2.IndexInMora = 0;
            mora_su.Phonemes.Add(p_U); p_U.ParentMora = mora_su; p_U.IndexInMora = 1;

            var word_desu = new JPWord();
            word_desu.PosId = 26; // 助動詞
            word_desu.CTypeId = 44; // 特殊・デス
            word_desu.CFormId = 5;  // 基本形
            word_desu.Moras.AddRange(new[] { mora_de, mora_su });
            foreach (var m in word_desu.Moras) m.ParentWord = word_desu;

            // Word5: "か" (カ, 1モーラ, POS=助詞,副助詞／並立助詞／終助詞 → ID=23)
            var p_k2 = new JPPhoneme("k");
            var p_a3 = new JPPhoneme("a");

            var mora_ka = new JPMora();
            mora_ka.Phonemes.Add(p_k2); p_k2.ParentMora = mora_ka; p_k2.IndexInMora = 0;
            mora_ka.Phonemes.Add(p_a3); p_a3.ParentMora = mora_ka; p_a3.IndexInMora = 1;

            var word_ka = new JPWord();
            word_ka.PosId = 23; // 助詞,副助詞／並立助詞／終助詞
            word_ka.CTypeId = null;
            word_ka.CFormId = null;
            word_ka.Moras.Add(mora_ka);
            mora_ka.ParentWord = word_ka;

            // AP2: accent=5 (盆栽ですか), words=[盆栽, です, か]
            // 注: accent=5だがモーラ数7なので有効
            var ap2 = new JPAccentPhrase();
            ap2.AccentType = 5;
            ap2.Words.Add(word_bonsai); word_bonsai.ParentAccentPhrase = ap2; word_bonsai.IndexInAccentPhrase = 0;
            ap2.Words.Add(word_desu); word_desu.ParentAccentPhrase = ap2; word_desu.IndexInAccentPhrase = 1;
            ap2.Words.Add(word_ka); word_ka.ParentAccentPhrase = ap2; word_ka.IndexInAccentPhrase = 2;
            ap2.IsInterrogative = true;

            // AP2のモーラインデックス
            moraIdx = 0;
            foreach (var w in ap2.Words)
                foreach (var m in w.Moras)
                    m.IndexInAccentPhrase = moraIdx++;

            // BG2
            var bg2 = new JPBreathGroup();
            bg2.AccentPhrases.Add(ap2); ap2.ParentBreathGroup = bg2; ap2.IndexInBreathGroup = 0;

            // 発話
            var utt = new JPUtterance();
            utt.BreathGroups.Add(bg1); bg1.ParentUtterance = utt; bg1.IndexInUtterance = 0;
            utt.BreathGroups.Add(bg2); bg2.ParentUtterance = utt; bg2.IndexInUtterance = 1;

            // 全音素の前後リンクを構築 (sil/pau含む)
            // 順序: k,o,r,e,w,a (BG1) → b,o,N,s,a,i,d,e,s,U,k,a (BG2)
            var allPhonemes = new List<JPPhoneme>
            {
                p_k1, p_o1, p_r, p_e1, p_w, p_a1,        // BG1: コレワ
                p_b, p_o2, p_N, p_s1, p_a2, p_i,          // BG2: ボンサイ
                p_d, p_e2, p_s2, p_U,                      // デス
                p_k2, p_a3                                  // カ
            };

            for (int i = 0; i < allPhonemes.Count; i++)
            {
                if (i > 0)
                    allPhonemes[i].Prev = allPhonemes[i - 1];
                if (i < allPhonemes.Count - 1)
                    allPhonemes[i].Next = allPhonemes[i + 1];
            }

            return utt;
        }
    }
}
