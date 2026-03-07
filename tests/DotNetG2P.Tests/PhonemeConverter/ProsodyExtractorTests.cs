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

        // ===== 尾高型 + ChainFlag=false afterFallリセットテスト =====

        [Fact]
        public void Extract_Odaka_FollowedByNewAccentPhrase_AfterFallIsReset()
        {
            // 尾高型アクセント句（最終モーラでアクセント下降）の直後に
            // ChainFlag=false の新しいアクセント句が続く場合、
            // afterFallがリセットされ、新アクセント句の先頭で _ が正しく出力される。
            //
            // ハシ (accent=2, 尾高型) → "h a [ sh i ]"
            // デス (accent=1, 頭高型, ChainFlag=false) → "[ d e ] s u"
            //
            // もしafterFallがリセットされないと、] の直後の [ の前に _ が
            // 不要にスキップされる可能性がある。
            // ChainFlag=false により afterFall がリセットされるため、
            // 新アクセント句の先頭で [ が正しく挿入される。

            var node1 = CreateNode("ハシ", 2); // 尾高型: h a [ sh i ]
            var node2 = CreateNode("デス", 1); // 頭高型: [ d e ] s u
            node2.ChainFlag = false; // 新アクセント句開始

            var nodes = new List<NjdNode> { node1, node2 };
            var result = ProsodyExtractor.Extract(nodes);

            // 期待値: "^ h a [ sh i ] [ d e ] s u $"
            // ハシ(尾高): h a [ sh i ] → ] でafterFall=true
            // デス(頭高, ChainFlag=false): afterFallリセット → [ d e ] s u
            // ] の直後に [ が来る（afterFallリセット後なので _ は不要で [ がセパレータ）
            Assert.Equal("^ h a [ sh i ] [ d e ] s u $", result);
        }

        [Fact]
        public void Extract_Odaka_FollowedByHeibanNewAccentPhrase_AfterFallIsReset()
        {
            // 尾高型の後に平板型（ChainFlag=false）が続くケース。
            // afterFallリセットにより、新アクセント句の第1モーラの前に _ が出力され、
            // 第2モーラの前に [ が出力される。
            //
            // ハシ (accent=2, 尾高型) → "h a [ sh i ]"
            // コレ (accent=0, 平板型, ChainFlag=false) → "_ k o [ r e"

            var node1 = CreateNode("ハシ", 2); // 尾高型
            var node2 = CreateNode("コレ", 0); // 平板型
            node2.ChainFlag = false; // 新アクセント句開始

            var nodes = new List<NjdNode> { node1, node2 };
            var result = ProsodyExtractor.Extract(nodes);

            // 期待値: "^ h a [ sh i ] _ k o [ r e $"
            // ハシ(尾高): h a [ sh i ] → afterFall=true
            // コレ(平板, ChainFlag=false): afterFallリセット
            //   第1モーラ(k o): hasPrevMora=true, afterFall=false, needRise=false → _ k o
            //   第2モーラ(r e): needRise=true → [ r e
            Assert.Equal("^ h a [ sh i ] _ k o [ r e $", result);
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
