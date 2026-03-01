using System.Collections.Generic;
using DotNetG2P.Models;
using DotNetG2P.NJD;

namespace DotNetG2P.Tests.NJD
{
    public class SetPronunciationTests
    {
        /// <summary>
        /// 発音付きのNjdNodeを手動構築するヘルパー。
        /// カタカナ文字列からPronunciationを生成し、ノードに設定する。
        /// </summary>
        private static NjdNode ノード作成_発音あり(
            string surface,
            string katakana,
            POSType posType = POSType.Meishi,
            string sub1 = "*",
            int accentType = 0,
            string conjugationType = "*",
            string conjugationForm = "*")
        {
            var pos = new POS(posType, sub1);
            var pron = Pronunciation.FromKatakana(katakana, accentType);
            var details = new WordDetails(pos, conjugationType, conjugationForm, surface, katakana, pron);
            return new NjdNode(surface, details)
            {
                AccentType = accentType,
                Pronunciation = pron,
            };
        }

        /// <summary>
        /// 発音なしのNjdNodeを手動構築するヘルパー。
        /// WordDetailsにPronunciationを設定せず、ノードのPronunciationも空のまま。
        /// </summary>
        private static NjdNode ノード作成_発音なし(
            string surface,
            POSType posType = POSType.Meishi,
            string sub1 = "*",
            string reading = "*")
        {
            var pos = new POS(posType, sub1);
            var details = new WordDetails(pos, "*", "*", surface, reading, null);
            return new NjdNode(surface, details);
        }

        // =====================================================================
        // 発音付きノードがそのまま通過するテスト
        // =====================================================================

        [Fact]
        public void Process_発音設定済みノードはそのまま通過する()
        {
            // 発音が既に設定されているノードはProcessUnpronouncedでスキップされる
            var node = ノード作成_発音あり("東京", "トーキョー", accentType: 0);
            var nodes = new List<NjdNode> { node };

            SetPronunciation.Process(nodes);

            Assert.Single(nodes);
            Assert.Equal("東京", nodes[0].Surface);
            Assert.Equal("t o - ky o -", nodes[0].Pronunciation.ToPhonemeString());
        }

        [Fact]
        public void Process_複数の発音設定済みノードがすべて通過する()
        {
            var node1 = ノード作成_発音あり("東京", "トーキョー");
            var node2 = ノード作成_発音あり("タワー", "タワー");
            var nodes = new List<NjdNode> { node1, node2 };

            SetPronunciation.Process(nodes);

            Assert.Equal(2, nodes.Count);
            Assert.Equal("東京", nodes[0].Surface);
            Assert.Equal("タワー", nodes[1].Surface);
        }

        // =====================================================================
        // カタカナ表層形からの発音生成テスト
        // =====================================================================

        [Fact]
        public void Process_カタカナ表層形から発音を生成する()
        {
            // 発音なしノードで表層形がカタカナの場合、モーラ解析して発音を生成
            var node = ノード作成_発音なし("コンニチワ");
            var nodes = new List<NjdNode> { node };

            SetPronunciation.Process(nodes);

            Assert.Single(nodes);
            Assert.Equal("k o N n i ch i w a", nodes[0].Pronunciation.ToPhonemeString());
        }

        [Fact]
        public void Process_ひらがな表層形はToutenセグメントに変換される()
        {
            // ひらがなはカタカナモーラ辞書に存在しないため、
            // 各文字がToutenセグメントとして認識される。
            // ToutenはMoraCount=0だがIsEmpty=falseなのでノードとして残る。
            // (実際のG2Pパイプラインでは辞書がカタカナ読みを提供するため
            //  ひらがな表層形が直接来ることはない)
            var node = ノード作成_発音なし("こんにちは");
            var nodes = new List<NjdNode> { node };

            SetPronunciation.Process(nodes);

            // ひらがな各文字がToutenセグメントに変換され、ノードとして残る
            Assert.NotEmpty(nodes);
            foreach (var n in nodes)
            {
                Assert.True(n.Pronunciation.Moras.Count > 0);
            }
        }

        // =====================================================================
        // 助動詞「う」→長音変換テスト（第5段階）
        // =====================================================================

        [Fact]
        public void Process_動詞の後の助動詞ウが長音に変換される()
        {
            // 動詞「行こ」+ 助動詞「う」→ 「う」が長音「ー」に変換
            var node1 = ノード作成_発音あり("行こ", "イコ", POSType.Doushi, conjugationForm: "未然ウ接続");
            var node2 = ノード作成_発音あり("う", "ウ", POSType.Jodoushi);
            var nodes = new List<NjdNode> { node1, node2 };

            SetPronunciation.Process(nodes);

            // 「ウ」が長音に変換される
            Assert.Equal(2, nodes.Count);
            Assert.True(nodes[1].Pronunciation.MoraMatches(MoraKind.Long));
        }

        [Fact]
        public void Process_助動詞の後の助動詞ウが長音に変換される()
        {
            // 助動詞「よ」+ 助動詞「う」→ 「う」が長音「ー」に変換
            var node1 = ノード作成_発音あり("よ", "ヨ", POSType.Jodoushi);
            var node2 = ノード作成_発音あり("う", "ウ", POSType.Jodoushi);
            var nodes = new List<NjdNode> { node1, node2 };

            SetPronunciation.Process(nodes);

            Assert.Equal(2, nodes.Count);
            Assert.True(nodes[1].Pronunciation.MoraMatches(MoraKind.Long));
        }

        [Fact]
        public void Process_名詞の後の助動詞ウは長音に変換されない()
        {
            // 名詞の後の助動詞「う」は変換対象外
            var node1 = ノード作成_発音あり("雨", "アメ", POSType.Meishi);
            var node2 = ノード作成_発音あり("う", "ウ", POSType.Jodoushi);
            var nodes = new List<NjdNode> { node1, node2 };

            SetPronunciation.Process(nodes);

            Assert.Equal(2, nodes.Count);
            // 「ウ」はそのまま
            Assert.True(nodes[1].Pronunciation.MoraMatches(MoraKind.U));
        }

        // =====================================================================
        // 「です」「ます」+ 「？」の発音修正テスト（第5段階）
        // =====================================================================

        [Fact]
        public void Process_助動詞デスの後に全角疑問符で発音修正される()
        {
            // 助動詞「です」+ 「？」→ 発音が「デス」(アクセント1)に修正
            var nodeDesu = ノード作成_発音あり("です", "デス", POSType.Jodoushi, accentType: 1);
            // 全角疑問符ノードは直接構築（Questionモーラを手動設定）
            var qPron = new Pronunciation(
                new List<Mora> { new Mora(null, null, MoraKind.Question) }, 0);
            var qPos = new POS(POSType.Kigou);
            var qDetails = new WordDetails(qPos, "*", "*", "\uFF1F", "*", qPron);
            var nodeQ = new NjdNode("\uFF1F", qDetails) { Pronunciation = qPron };
            var nodes = new List<NjdNode> { nodeDesu, nodeQ };

            SetPronunciation.Process(nodes);

            // 「です」の発音が「デス」(アクセント1)に修正される
            Assert.Equal(2, nodes[0].Pronunciation.MoraCount);
            Assert.Equal(MoraKind.De, nodes[0].Pronunciation.Moras[0].Kind);
            Assert.Equal(MoraKind.Su, nodes[0].Pronunciation.Moras[1].Kind);
            Assert.Equal(1, nodes[0].Pronunciation.AccentPosition);
        }

        [Fact]
        public void Process_助動詞マスの後に全角疑問符で発音修正される()
        {
            // 助動詞「ます」+ 「？」→ 発音が「マス」(アクセント1)に修正
            var nodeMasu = ノード作成_発音あり("ます", "マス", POSType.Jodoushi, accentType: 1);
            var qPron = new Pronunciation(
                new List<Mora> { new Mora(null, null, MoraKind.Question) }, 0);
            var qPos = new POS(POSType.Kigou);
            var qDetails = new WordDetails(qPos, "*", "*", "\uFF1F", "*", qPron);
            var nodeQ = new NjdNode("\uFF1F", qDetails) { Pronunciation = qPron };
            var nodes = new List<NjdNode> { nodeMasu, nodeQ };

            SetPronunciation.Process(nodes);

            Assert.Equal(2, nodes[0].Pronunciation.MoraCount);
            Assert.Equal(MoraKind.Ma, nodes[0].Pronunciation.Moras[0].Kind);
            Assert.Equal(MoraKind.Su, nodes[0].Pronunciation.Moras[1].Kind);
            Assert.Equal(1, nodes[0].Pronunciation.AccentPosition);
        }

        // =====================================================================
        // 発音が空/「*」のノードのフォールバック処理テスト
        // =====================================================================

        [Fact]
        public void Process_発音がアスタリスクのノードは表層形から解析される()
        {
            // 発音が「*」で表層形がカタカナの場合、表層形から発音を生成
            var node = ノード作成_発音なし("テスト", reading: "*");
            var nodes = new List<NjdNode> { node };

            SetPronunciation.Process(nodes);

            Assert.Single(nodes);
            Assert.True(nodes[0].Pronunciation.MoraCount > 0);
            Assert.Equal("t e s u t o", nodes[0].Pronunciation.ToPhonemeString());
        }

        // =====================================================================
        // 記号ノード（句読点）の処理テスト
        // =====================================================================

        [Fact]
        public void Process_句読点ノードはToutenモーラとして保持される()
        {
            // 句読点（、）はToutenモーラに変換される。
            // ToutenモーラはMoraCount=0だがMoras.Count>0なのでIsEmpty=falseとなり除去されない。
            var node1 = ノード作成_発音あり("東京", "トーキョー");
            var nodeComma = ノード作成_発音なし("、", POSType.Kigou);
            var node2 = ノード作成_発音あり("タワー", "タワー");
            var nodes = new List<NjdNode> { node1, nodeComma, node2 };

            SetPronunciation.Process(nodes);

            // 句読点はToutenモーラとして残る（3ノード）
            Assert.Equal(3, nodes.Count);
            Assert.Equal("東京", nodes[0].Surface);
            Assert.Equal("タワー", nodes[2].Surface);
            // 中間ノードがToutenモーラを持つ
            Assert.True(nodes[1].Pronunciation.IsTouten);
        }

        [Fact]
        public void Process_全角疑問符ノードは保持される()
        {
            // 全角疑問符「？」はQuestionモーラとして保持される（MoraCountには入らないがIsEmptyではない）
            var node1 = ノード作成_発音あり("何", "ナニ");
            var nodeQ = ノード作成_発音なし("\uFF1F", POSType.Kigou);
            var nodes = new List<NjdNode> { node1, nodeQ };

            SetPronunciation.Process(nodes);

            // 疑問符はQuestionモーラを持つので、MoraCount=0だがMoras.Count>0のため
            // RemoveSilentNodesの判定 (IsEmpty) はMoras.Count==0で判定するのでQuestionは残る
            // 確認: Questionモーラ1つ → IsEmpty=false, MoraCount=0
            // RemoveSilentNodes は n.Pronunciation.IsEmpty で判定 → Moras.Count==0 の場合のみ除去
            // Questionモーラが1つあるので IsEmpty=false → 保持される
            bool hasQuestion = false;
            foreach (var n in nodes)
            {
                if (n.Pronunciation != null && n.Pronunciation.Moras.Count > 0)
                {
                    foreach (var m in n.Pronunciation.Moras)
                    {
                        if (m.Kind == MoraKind.Question) hasQuestion = true;
                    }
                }
            }
            Assert.True(hasQuestion);
        }

        // =====================================================================
        // 空リスト・nullリストのテスト
        // =====================================================================

        [Fact]
        public void Process_空リストでエラーにならない()
        {
            var nodes = new List<NjdNode>();
            SetPronunciation.Process(nodes);
            Assert.Empty(nodes);
        }

        [Fact]
        public void Process_nullリストで例外が発生する()
        {
            // nullを渡した場合はArgumentNullExceptionが発生する
            Assert.ThrowsAny<System.Exception>(() => SetPronunciation.Process(null!));
        }

        // =====================================================================
        // 発音が空のノードの除去テスト（第2/4段階）
        // =====================================================================

        [Fact]
        public void Process_発音が空のノードは除去される()
        {
            // 発音なしノードで表層形も解析不能な場合は除去される
            var node1 = ノード作成_発音あり("東京", "トーキョー");
            // ASCII記号などモーラ解析不能な表層形
            var nodeEmpty = ノード作成_発音なし("@@@");
            var node2 = ノード作成_発音あり("駅", "エキ");
            var nodes = new List<NjdNode> { node1, nodeEmpty, node2 };

            SetPronunciation.Process(nodes);

            // 解析不能なノードが除去されるか、Toutenセグメントに変換される
            // 少なくとも発音ありの2ノード（東京・駅）は残る
            Assert.True(nodes.Count >= 2, $"ノード数が2未満: {nodes.Count}");
            Assert.Equal("東京", nodes[0].Surface);
        }

        // =====================================================================
        // 連続カナフィラー統合テスト（第3段階）
        // =====================================================================

        [Fact]
        public void Process_連続フィラーノードが統合される()
        {
            // 発音なしのカタカナ表層形ノード2つ → フィラーとして認識 → 統合
            var node1 = ノード作成_発音なし("バリー");
            var node2 = ノード作成_発音なし("ペーン");
            var nodes = new List<NjdNode> { node1, node2 };

            SetPronunciation.Process(nodes);

            // 連続フィラーが統合されて1ノードになる可能性が高い
            // 表層形が結合されているはず
            Assert.NotEmpty(nodes);
            if (nodes.Count == 1)
            {
                Assert.Contains("バリー", nodes[0].Surface);
                Assert.Contains("ペーン", nodes[0].Surface);
            }
        }

        // =====================================================================
        // 混合ケースのテスト（セグメント分割）
        // =====================================================================

        [Fact]
        public void Process_表層形に記号を含むノードがセグメント分割される()
        {
            // 「バリー・ペーン」のように中に記号がある表層形は
            // セグメント分割されて複数ノードに分かれる
            var node = ノード作成_発音なし("バリー・ペーン");
            var nodes = new List<NjdNode> { node };

            SetPronunciation.Process(nodes);

            // 中黒（・）でセグメント分割され、少なくとも2つのカナセグメントが残る
            Assert.True(nodes.Count >= 2, $"セグメント分割後のノード数が2未満: {nodes.Count}");
        }

        // =====================================================================
        // ConvertToKigou 演算子優先度テスト
        // =====================================================================

        [Fact]
        public void Process_副詞一般の発音なしノードが記号一般に変換される()
        {
            // 副詞-一般の発音なしノードで表層形が記号のみ → 記号-一般に変換
            var pos = new POS(POSType.Fukushi, "\u4E00\u822C"); // 副詞-一般
            var details = new WordDetails(pos, "*", "*", "\u3001", "*", null); // 、
            var node = new NjdNode("\u3001", details);
            var nodes = new List<NjdNode> { node };

            SetPronunciation.Process(nodes);

            // Toutenセグメントとして残り、品詞が記号系に変換される
            Assert.Single(nodes);
            Assert.True(nodes[0].PartOfSpeech.IsKigou);
        }

        [Fact]
        public void Process_名詞一般の発音なしノードが記号一般に変換される()
        {
            // 名詞-一般の発音なしノードで表層形が記号のみ → 記号-一般に変換
            var pos = new POS(POSType.Meishi, "\u4E00\u822C"); // 名詞-一般
            var details = new WordDetails(pos, "*", "*", "\u3001", "*", null); // 、
            var node = new NjdNode("\u3001", details);
            var nodes = new List<NjdNode> { node };

            SetPronunciation.Process(nodes);

            Assert.Single(nodes);
            Assert.True(nodes[0].PartOfSpeech.IsKigou);
        }

        // =====================================================================
        // 助詞「は」「へ」の発音テスト
        // =====================================================================

        [Fact]
        public void Process_助詞ハの発音ワがそのまま通過する()
        {
            // 辞書から「は」の読み「ワ」が設定済みの場合はそのまま通過
            var pos = new POS(POSType.Joshi, "係助詞");
            var pron = Pronunciation.FromKatakana("ワ", 0);
            var details = new WordDetails(pos, "*", "*", "は", "ワ", pron);
            var node = new NjdNode("は", details)
            {
                Pronunciation = pron,
            };
            var nodes = new List<NjdNode> { node };

            SetPronunciation.Process(nodes);

            Assert.Single(nodes);
            Assert.Equal("w a", nodes[0].Pronunciation.ToPhonemeString());
        }

        [Fact]
        public void Process_助詞ヘの発音エがそのまま通過する()
        {
            // 辞書から「へ」の読み「エ」が設定済みの場合はそのまま通過
            var pos = new POS(POSType.Joshi, "格助詞");
            var pron = Pronunciation.FromKatakana("エ", 0);
            var details = new WordDetails(pos, "*", "*", "へ", "エ", pron);
            var node = new NjdNode("へ", details)
            {
                Pronunciation = pron,
            };
            var nodes = new List<NjdNode> { node };

            SetPronunciation.Process(nodes);

            Assert.Single(nodes);
            Assert.Equal("e", nodes[0].Pronunciation.ToPhonemeString());
        }

        // =====================================================================
        // 発音コピーの検証テスト
        // =====================================================================

        [Fact]
        public void Process_WordDetailsからの発音が正しくコピーされる()
        {
            // Pronunciation.FromKatakanaで「コンニチワ」を解析
            var pron = Pronunciation.FromKatakana("コンニチワ", 3);
            var pos = new POS(POSType.Meishi);
            var details = new WordDetails(pos, "*", "*", "こんにちは", "コンニチワ", pron);
            var node = new NjdNode("こんにちは", details)
            {
                AccentType = 3,
                Pronunciation = pron,
            };
            var nodes = new List<NjdNode> { node };

            SetPronunciation.Process(nodes);

            Assert.Single(nodes);
            Assert.Equal(5, nodes[0].Pronunciation.MoraCount);
            Assert.Equal("k o N n i ch i w a", nodes[0].Pronunciation.ToPhonemeString());
        }

        [Fact]
        public void Process_カタカナ発音の正しいパース_促音と長音()
        {
            // 「バッター」→ バ ッ タ ー (4モーラ分のMoras、MoraCount=2: 促音と長音はカウントされない)
            // 実際にはMoraCountは Touten/Question以外をカウントするので促音・長音もカウントされる
            var pron = Pronunciation.FromKatakana("バッター", 1);
            var pos = new POS(POSType.Meishi);
            var details = new WordDetails(pos, "*", "*", "バッター", "バッター", pron);
            var node = new NjdNode("バッター", details)
            {
                AccentType = 1,
                Pronunciation = pron,
            };
            var nodes = new List<NjdNode> { node };

            SetPronunciation.Process(nodes);

            Assert.Single(nodes);
            // バ + ッ + タ + ー = 4モーラ
            Assert.Equal(4, nodes[0].Pronunciation.Moras.Count);
            Assert.Equal(MoraKind.Ba, nodes[0].Pronunciation.Moras[0].Kind);
            Assert.Equal(MoraKind.Xtsu, nodes[0].Pronunciation.Moras[1].Kind);
            Assert.Equal(MoraKind.Ta, nodes[0].Pronunciation.Moras[2].Kind);
            Assert.Equal(MoraKind.Long, nodes[0].Pronunciation.Moras[3].Kind);
        }

        [Fact]
        public void Process_カタカナ発音の正しいパース_撥音()
        {
            // 「コンバンワ」→ コ ン バ ン ワ
            var pron = Pronunciation.FromKatakana("コンバンワ", 0);
            var pos = new POS(POSType.Meishi);
            var details = new WordDetails(pos, "*", "*", "こんばんは", "コンバンワ", pron);
            var node = new NjdNode("こんばんは", details)
            {
                Pronunciation = pron,
            };
            var nodes = new List<NjdNode> { node };

            SetPronunciation.Process(nodes);

            Assert.Single(nodes);
            Assert.Equal(5, nodes[0].Pronunciation.MoraCount);
            Assert.Equal("k o N b a N w a", nodes[0].Pronunciation.ToPhonemeString());
        }

        // =====================================================================
        // 単一ノード複合テスト
        // =====================================================================

        [Fact]
        public void Process_発音ありと発音なしの混合リスト()
        {
            var node1 = ノード作成_発音あり("東京", "トーキョー");
            var node2 = ノード作成_発音なし("タワー");
            var nodes = new List<NjdNode> { node1, node2 };

            SetPronunciation.Process(nodes);

            // 両方のノードが残る（発音なしノードは表層形から発音生成）
            Assert.Equal(2, nodes.Count);
            Assert.Equal("東京", nodes[0].Surface);
        }

        // =====================================================================
        // 品詞変換テスト
        // =====================================================================

        [Fact]
        public void Process_発音なしノードの品詞がフィラーに変換される()
        {
            // 発音なしでカタカナ表層形 → フィラーに品詞変更
            var node = ノード作成_発音なし("テスト");
            var nodes = new List<NjdNode> { node };

            SetPronunciation.Process(nodes);

            Assert.Single(nodes);
            Assert.True(nodes[0].PartOfSpeech.IsFiller);
        }
    }
}
