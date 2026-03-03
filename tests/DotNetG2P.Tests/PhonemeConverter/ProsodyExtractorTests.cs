using System.Collections.Generic;
using DotNetG2P.Models;
using DotNetG2P.PhonemeConverter;

namespace DotNetG2P.Tests.PhonemeConverter
{
    public class ProsodyExtractorTests
    {
        // ===== ヘルパーメソッド =====

        /// <summary>
        /// カタカナとアクセント型からNjdNodeを簡易生成する。
        /// </summary>
        private static NjdNode CreateNode(string katakana, int accentType)
        {
            var pron = Pronunciation.FromKatakana(katakana, accentType);
            var details = new WordDetails(new POS(POSType.Meishi), "*", "*", "*", "*", null);
            var node = new NjdNode(katakana, details)
            {
                AccentType = accentType,
                Pronunciation = pron
            };
            return node;
        }

        /// <summary>
        /// Toutenノードを生成する。
        /// </summary>
        private static NjdNode CreateToutenNode()
        {
            var details = new WordDetails(new POS(POSType.Kigou), "*", "*", "*", "*", null);
            var node = new NjdNode("、", details)
            {
                Pronunciation = new Pronunciation(
                    new List<Mora> { new Mora(null, null, MoraKind.Touten) }, 0)
            };
            return node;
        }

        /// <summary>
        /// Questionノードを生成する。
        /// </summary>
        private static NjdNode CreateQuestionNode()
        {
            var details = new WordDetails(new POS(POSType.Kigou), "*", "*", "*", "*", null);
            var node = new NjdNode("？", details)
            {
                Pronunciation = new Pronunciation(
                    new List<Mora> { new Mora(null, null, MoraKind.Question) }, 0)
            };
            return node;
        }

        // ===== 空入力テスト =====

        [Fact]
        public void Extract_NullInput_ReturnsStartEnd()
        {
            var result = ProsodyExtractor.Extract(null);
            Assert.Equal("^ $", result);
        }

        [Fact]
        public void Extract_EmptyList_ReturnsStartEnd()
        {
            var result = ProsodyExtractor.Extract(new List<NjdNode>());
            Assert.Equal("^ $", result);
        }

        // ===== 平板型（accent=0）テスト =====

        [Fact]
        public void Extract_Heiban_SingleMora_NoAccentMarkers()
        {
            // ア (accent=0, 1モーラ) → "^ a $"
            // 1モーラの場合、第2モーラがないので [ は出ない
            var nodes = new List<NjdNode> { CreateNode("ア", 0) };
            var result = ProsodyExtractor.Extract(nodes);
            Assert.Equal("^ a $", result);
        }

        [Fact]
        public void Extract_Heiban_TwoMoras_RiseAtSecond()
        {
            // コレ (accent=0) → "^ k o [ r e $"
            // 平板型: 第2モーラから高い、下降なし
            var nodes = new List<NjdNode> { CreateNode("コレ", 0) };
            var result = ProsodyExtractor.Extract(nodes);
            Assert.Equal("^ k o [ r e $", result);
        }

        [Fact]
        public void Extract_Heiban_FiveMoras()
        {
            // コンニチワ (accent=0, 平板型) → "^ k o [ N _ n i _ ch i _ w a $"
            var nodes = new List<NjdNode> { CreateNode("コンニチワ", 0) };
            var result = ProsodyExtractor.Extract(nodes);
            Assert.Equal("^ k o [ N _ n i _ ch i _ w a $", result);
        }

        // ===== 頭高型（accent=1）テスト =====

        [Fact]
        public void Extract_Atamadaka_SingleMora()
        {
            // カ (accent=1) → "^ [ k a ] $"
            var nodes = new List<NjdNode> { CreateNode("カ", 1) };
            var result = ProsodyExtractor.Extract(nodes);
            Assert.Equal("^ [ k a ] $", result);
        }

        [Fact]
        public void Extract_Atamadaka_TwoMoras()
        {
            // ハシ (accent=1, 箸) → "^ [ h a ] sh i $"
            var nodes = new List<NjdNode> { CreateNode("ハシ", 1) };
            var result = ProsodyExtractor.Extract(nodes);
            Assert.Equal("^ [ h a ] sh i $", result);
        }

        [Fact]
        public void Extract_Atamadaka_ThreeMoras()
        {
            // イノチ (accent=1) → "^ [ i ] n o _ ch i $"
            var nodes = new List<NjdNode> { CreateNode("イノチ", 1) };
            var result = ProsodyExtractor.Extract(nodes);
            Assert.Equal("^ [ i ] n o _ ch i $", result);
        }

        // ===== 中高型（accent=2, 3）テスト =====

        [Fact]
        public void Extract_Nakadaka_Accent2_ThreeMoras()
        {
            // コトバ (accent=2) → "^ k o [ t o ] b a $"
            var nodes = new List<NjdNode> { CreateNode("コトバ", 2) };
            var result = ProsodyExtractor.Extract(nodes);
            Assert.Equal("^ k o [ t o ] b a $", result);
        }

        [Fact]
        public void Extract_Nakadaka_Accent3_FourMoras()
        {
            // オトート (accent=3) → "^ o [ t o _ o ] t o $"（長音展開）
            var nodes = new List<NjdNode> { CreateNode("オトート", 3) };
            var result = ProsodyExtractor.Extract(nodes);
            Assert.Equal("^ o [ t o _ o ] t o $", result);
        }

        [Fact]
        public void Extract_Nakadaka_Accent2_FiveMoras()
        {
            // コンニチワ (accent=2) → "^ k o [ N ] n i _ ch i _ w a $"
            var nodes = new List<NjdNode> { CreateNode("コンニチワ", 2) };
            var result = ProsodyExtractor.Extract(nodes);
            Assert.Equal("^ k o [ N ] n i _ ch i _ w a $", result);
        }

        // ===== 尾高型テスト =====

        [Fact]
        public void Extract_Odaka_TwoMoras()
        {
            // ハシ (accent=2, 橋) → "^ h a [ sh i ] $"
            var nodes = new List<NjdNode> { CreateNode("ハシ", 2) };
            var result = ProsodyExtractor.Extract(nodes);
            Assert.Equal("^ h a [ sh i ] $", result);
        }

        // ===== 複数アクセント句（ポーズ挿入）テスト =====

        [Fact]
        public void Extract_MultipleNodes_WithTouten()
        {
            // "コレ、ソレ" → "^ k o [ r e # s o [ r e $"
            var nodes = new List<NjdNode>
            {
                CreateNode("コレ", 0),
                CreateToutenNode(),
                CreateNode("ソレ", 0),
            };
            var result = ProsodyExtractor.Extract(nodes);
            Assert.Equal("^ k o [ r e # s o [ r e $", result);
        }

        [Fact]
        public void Extract_MultipleNodes_WithoutTouten()
        {
            // 2ノード連続（Toutenなし）
            // "コレ" (accent=0) + "デス" (accent=1)
            // → "^ k o [ r e [ d e ] s u $"
            var nodes = new List<NjdNode>
            {
                CreateNode("コレ", 0),
                CreateNode("デス", 1),
            };
            var result = ProsodyExtractor.Extract(nodes);
            Assert.Equal("^ k o [ r e [ d e ] s u $", result);
        }

        // ===== 疑問ノードテスト =====

        [Fact]
        public void Extract_QuestionNode()
        {
            var nodes = new List<NjdNode>
            {
                CreateNode("ナニ", 1),
                CreateQuestionNode(),
            };
            var result = ProsodyExtractor.Extract(nodes);
            Assert.Equal("^ [ n a ] n i ? $", result);
        }

        // ===== 特殊モーラテスト =====

        [Fact]
        public void Extract_WithSokuon()
        {
            // ガッコー (accent=0) → "^ g a [ cl _ k o _ o $"（長音展開）
            var nodes = new List<NjdNode> { CreateNode("ガッコー", 0) };
            var result = ProsodyExtractor.Extract(nodes);
            Assert.Equal("^ g a [ cl _ k o _ o $", result);
        }

        [Fact]
        public void Extract_WithHatsuon()
        {
            // センセー (accent=3) → "^ s e [ N _ s e ] e $"（長音展開）
            var nodes = new List<NjdNode> { CreateNode("センセー", 3) };
            var result = ProsodyExtractor.Extract(nodes);
            Assert.Equal("^ s e [ N _ s e ] e $", result);
        }

        // ===== ExpandLongVowels=false テスト =====

        [Fact]
        public void Extract_ExpandLongVowelsFalse_ReturnsDash()
        {
            // expandLongVowels=false の場合、長音は "-" のまま出力される
            var nodes = new List<NjdNode> { CreateNode("ガッコー", 0) };
            var result = ProsodyExtractor.Extract(nodes, expandLongVowels: false);
            Assert.Equal("^ g a [ cl _ k o _ - $", result);
        }
    }
}
