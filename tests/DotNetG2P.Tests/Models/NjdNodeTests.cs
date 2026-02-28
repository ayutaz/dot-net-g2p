using DotNetG2P.Models;

namespace DotNetG2P.Tests.Models
{
    public class NjdNodeTests
    {
        /// <summary>
        /// テスト用にNjdNodeを構築するヘルパー。
        /// </summary>
        private static NjdNode CreateNode(string surface, string katakana, int accentType = 0)
        {
            var pos = new POS(POSType.Meishi);
            var pron = Pronunciation.FromKatakana(katakana, 0);
            var details = new WordDetails(pos, "*", "*", surface, katakana, pron);
            return new NjdNode(surface, details)
            {
                AccentType = accentType,
                Pronunciation = pron,
            };
        }

        // ===== MergeFrom テスト =====

        [Fact]
        public void MergeFrom_TwoNodes_MergesSurfaceAndMoras()
        {
            var node1 = CreateNode("東京", "トーキョー");
            var node2 = CreateNode("都", "ト");

            node1.MergeFrom(node2);

            // 表層形が連結される
            Assert.Equal("東京都", node1.Surface);

            // モーラが連結される: トーキョー(4) + ト(1) = 5
            Assert.Equal(5, node1.Pronunciation.Moras.Count);

            // 統合元ノードはResetされる
            Assert.True(node2.IsEmpty);
            Assert.Equal("", node2.Surface);
        }

        [Fact]
        public void MergeFrom_ReadingsConcatenated()
        {
            var node1 = CreateNode("東京", "トーキョー");
            node1.Reading = "トーキョー";
            var node2 = CreateNode("都", "ト");
            node2.Reading = "ト";

            node1.MergeFrom(node2);

            Assert.Equal("トーキョート", node1.Reading);
        }

        [Fact]
        public void MergeFrom_Null_DoesNothing()
        {
            var node = CreateNode("東京", "トーキョー");
            var originalSurface = node.Surface;
            var originalMoraCount = node.Pronunciation.Moras.Count;

            node.MergeFrom(null!);

            Assert.Equal(originalSurface, node.Surface);
            Assert.Equal(originalMoraCount, node.Pronunciation.Moras.Count);
        }

        [Fact]
        public void MergeFrom_OtherNodeIsReset()
        {
            var node1 = CreateNode("朝", "アサ");
            var node2 = CreateNode("日", "ヒ");

            node1.MergeFrom(node2);

            // otherはReset済み
            Assert.True(node2.IsEmpty);
            Assert.Equal("", node2.Surface);
            Assert.Equal(0, node2.MoraCount);
            Assert.Equal("*", node2.Reading);
            Assert.Equal("*", node2.ChainRule);
            Assert.Null(node2.ChainFlag);
        }

        // ===== Reset テスト =====

        [Fact]
        public void Reset_ClearsAllFields()
        {
            var node = CreateNode("東京", "トーキョー");
            node.AccentType = 3;
            node.ChainFlag = true;
            node.ChainRule = "C1";
            node.Reading = "トーキョー";

            node.Reset();

            Assert.Equal("", node.Surface);
            Assert.Equal(0, node.AccentType);
            Assert.Null(node.ChainFlag);
            Assert.Equal("*", node.ChainRule);
            Assert.Equal("*", node.Reading);
            Assert.Equal(0, node.MoraCount);
            Assert.NotNull(node.Details);
            Assert.Equal(POSType.Meishi, node.PartOfSpeech.Type);
        }

        // ===== IsEmpty テスト =====

        [Fact]
        public void IsEmpty_AfterReset_ReturnsTrue()
        {
            var node = CreateNode("東京", "トーキョー");
            node.Reset();
            Assert.True(node.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WithContent_ReturnsFalse()
        {
            var node = CreateNode("東京", "トーキョー");
            Assert.False(node.IsEmpty);
        }

        [Fact]
        public void IsEmpty_EmptySurfaceButHasMoras_ReturnsFalse()
        {
            var node = CreateNode("", "ア");
            // Surface is empty but has moras
            Assert.False(node.IsEmpty);
        }

        [Fact]
        public void IsEmpty_SurfaceSetButNoMoras_ReturnsFalse()
        {
            var pos = new POS(POSType.Kigou);
            var details = new WordDetails(pos, "*", "*", "、", "*");
            var node = new NjdNode("、", details);
            // Surface is non-empty, no moras
            Assert.False(node.IsEmpty);
        }

        // ===== RemoveEmpty テスト =====

        [Fact]
        public void RemoveEmpty_RemovesResetNodes()
        {
            var node1 = CreateNode("東京", "トーキョー");
            var node2 = CreateNode("は", "ワ");
            var node3 = CreateNode("都市", "トシ");

            // node2をnode1にマージ → node2はResetされる
            node1.MergeFrom(node2);

            var nodes = new List<NjdNode> { node1, node2, node3 };
            NjdNode.RemoveEmpty(nodes);

            // node2はResetで空になったので除去される
            Assert.Equal(2, nodes.Count);
            Assert.Equal("東京は", nodes[0].Surface);
            Assert.Equal("都市", nodes[1].Surface);
        }

        [Fact]
        public void RemoveEmpty_NoEmptyNodes_PreservesAll()
        {
            var node1 = CreateNode("東京", "トーキョー");
            var node2 = CreateNode("都", "ト");

            var nodes = new List<NjdNode> { node1, node2 };
            NjdNode.RemoveEmpty(nodes);

            Assert.Equal(2, nodes.Count);
        }

        [Fact]
        public void RemoveEmpty_AllEmpty_ReturnsEmptyList()
        {
            var node1 = CreateNode("東京", "トーキョー");
            var node2 = CreateNode("都", "ト");
            node1.Reset();
            node2.Reset();

            var nodes = new List<NjdNode> { node1, node2 };
            NjdNode.RemoveEmpty(nodes);

            Assert.Empty(nodes);
        }

        // ===== コンストラクタ・アクセサテスト =====

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            var pos = new POS(POSType.Doushi, "自立");
            var details = new WordDetails(pos, "五段", "連用形", "走る", "ハシリ");
            var node = new NjdNode("走り", details);

            Assert.Equal("走り", node.Surface);
            Assert.Equal(0, node.AccentType);
            Assert.Null(node.ChainFlag);
            Assert.Equal("*", node.ChainRule);
            Assert.Equal("ハシリ", node.Reading);
            Assert.Equal(POSType.Doushi, node.PartOfSpeech.Type);
            Assert.Equal("自立", node.PartOfSpeech.SubCategory1);
            Assert.Equal("五段", node.ConjugationType);
            Assert.Equal("連用形", node.ConjugationForm);
            Assert.Equal("走る", node.OriginalForm);
            Assert.True(node.IsRenyou);
        }

        [Fact]
        public void IsRenyou_RenyouTaForm_ReturnsTrue()
        {
            var pos = new POS(POSType.Doushi);
            var details = new WordDetails(pos, "*", "連用タ接続", "書く", "カイ");
            var node = new NjdNode("書い", details);
            Assert.True(node.IsRenyou);
        }

        [Fact]
        public void IsRenyou_ShuuryouForm_ReturnsFalse()
        {
            var pos = new POS(POSType.Doushi);
            var details = new WordDetails(pos, "*", "基本形", "走る", "ハシル");
            var node = new NjdNode("走る", details);
            Assert.False(node.IsRenyou);
        }

        [Fact]
        public void MoraCount_DelegatesToPronunciation()
        {
            var node = CreateNode("東京", "トーキョー");
            // トーキョー → ト, ー, キョ, ー = 4モーラ (Toutenを除外するが、ここはLong)
            // Longもカウントに含まれる
            Assert.Equal(4, node.MoraCount);
        }
    }
}
