using DotNetG2P.Models;
using DotNetG2P.NJD;

namespace DotNetG2P.Tests.NJD
{
    public class DigitSequenceTests
    {
        // ===== ヘルパー =====

        /// <summary>
        /// 数字の名詞-数ノードを作成するヘルパー。
        /// </summary>
        private static NjdNode CreateKazuNode(string surface, string katakana, int accentType = 0)
        {
            var pos = new POS(POSType.Meishi, "数");
            var pron = Pronunciation.FromKatakana(katakana, accentType);
            var details = new WordDetails(pos, "*", "*", surface, katakana, pron);
            var node = new NjdNode(surface, details)
            {
                AccentType = accentType,
                Pronunciation = pron,
            };
            return node;
        }

        /// <summary>
        /// 一般名詞ノードを作成するヘルパー。
        /// </summary>
        private static NjdNode CreateMeishiNode(string surface, string katakana, string sub1 = "*", string sub2 = "*", int accentType = 0)
        {
            var pos = new POS(POSType.Meishi, sub1, sub2);
            var pron = Pronunciation.FromKatakana(katakana, accentType);
            var details = new WordDetails(pos, "*", "*", surface, katakana, pron);
            var node = new NjdNode(surface, details)
            {
                AccentType = accentType,
                Pronunciation = pron,
            };
            return node;
        }

        // ===== 空リストテスト =====

        [Fact]
        public void Process_空リスト_例外が発生しない()
        {
            var nodes = new List<NjdNode>();
            DigitSequenceProcessor.Process(nodes);
            Assert.Empty(nodes);
        }

        // ===== 非数字ノードは変更されない =====

        [Fact]
        public void Process_非数字ノードのみ_変更されない()
        {
            var node = CreateMeishiNode("東京", "トウキョウ");
            var nodes = new List<NjdNode> { node };

            DigitSequenceProcessor.Process(nodes);

            Assert.Single(nodes);
            Assert.Equal("東京", nodes[0].Surface);
            Assert.Equal("トウキョウ", nodes[0].Pronunciation.ToKatakana());
        }

        // ===== 数字正規化テスト =====

        [Theory]
        [InlineData("１", "一")]
        [InlineData("２", "二")]
        [InlineData("３", "三")]
        [InlineData("○", "〇")]
        public void Process_全角数字を漢数字に正規化(string input, string expected)
        {
            var node = CreateKazuNode(input, "イチ");
            var nodes = new List<NjdNode> { node };

            DigitSequenceProcessor.Process(nodes);

            // 単一の数字は正規化のみ（1桁はシーケンスとみなされないため変換されない）
            Assert.Equal(expected, nodes[0].Surface);
        }

        // ===== 隣接数字のグループ化（数値読み） =====

        [Fact]
        public void Process_二桁数字_位取りノードが挿入される()
        {
            // 一二 → 十二（数値読み判定時）
            var node1 = CreateKazuNode("一", "イチ", 2);
            var node2 = CreateKazuNode("二", "ニ", 1);

            // 後続に助数詞を置いて数値読みスコアを上げる
            var josuushi = CreateMeishiNode("個", "コ", "接尾", "助数詞", 1);

            var nodes = new List<NjdNode> { node1, node2, josuushi };

            DigitSequenceProcessor.Process(nodes);

            // 数値読みの場合: 一(十の位=1) → 十に置換、二はそのまま
            // ノード数が変わりうるが、「十」を含むノードがあるはず
            bool hasTen = false;
            foreach (var n in nodes)
            {
                if (n.Surface == "十") hasTen = true;
            }
            Assert.True(hasTen, "位取り「十」ノードが挿入されるべき");
        }

        [Fact]
        public void Process_三桁数字_百の位取りノードが挿入される()
        {
            // 一二三 → 百二十三（数値読み）
            var node1 = CreateKazuNode("一", "イチ", 2);
            var node2 = CreateKazuNode("二", "ニ", 1);
            var node3 = CreateKazuNode("三", "サン", 1);

            // 助数詞で数値読みスコアを上げる
            var josuushi = CreateMeishiNode("個", "コ", "接尾", "助数詞", 1);

            var nodes = new List<NjdNode> { node1, node2, node3, josuushi };

            DigitSequenceProcessor.Process(nodes);

            // 百が挿入されているはず
            bool hasHyaku = false;
            foreach (var n in nodes)
            {
                if (n.Surface == "百") hasHyaku = true;
            }
            Assert.True(hasHyaku, "位取り「百」ノードが挿入されるべき");
        }

        // ===== 順序読み（0始まりの場合） =====

        [Fact]
        public void Process_ゼロ始まり_順序読みになる()
        {
            // 〇一二 → ゼロ・イチ・ニー（電話番号等）
            var node0 = CreateKazuNode("〇", "ゼロ", 1);
            var node1 = CreateKazuNode("一", "イチ", 2);
            var node2 = CreateKazuNode("二", "ニ", 1);

            var nodes = new List<NjdNode> { node0, node1, node2 };

            DigitSequenceProcessor.Process(nodes);

            // 0始まりは順序読み → 位取りノードは挿入されない
            // ゼロの発音に変換される
            Assert.Equal("ゼロ", nodes[0].Pronunciation.ToKatakana());
        }

        // ===== 0の桁が無音化される =====

        [Fact]
        public void Process_数値読みで0の桁は無音化される()
        {
            // 一〇 → 十（10）
            var node1 = CreateKazuNode("一", "イチ", 2);
            var node0 = CreateKazuNode("〇", "ゼロ", 1);
            var josuushi = CreateMeishiNode("個", "コ", "接尾", "助数詞", 1);

            var nodes = new List<NjdNode> { node1, node0, josuushi };

            DigitSequenceProcessor.Process(nodes);

            // 数値読みなら1→十に置換、0は無音化されて除去される
            // 「十」が残るはず
            bool hasTen = false;
            foreach (var n in nodes)
            {
                if (n.Surface == "十") hasTen = true;
            }
            Assert.True(hasTen, "10の数値読みでは「十」ノードが生成されるべき");
        }

        // ===== 単一数字は変換されない =====

        [Fact]
        public void Process_単一数字ノード_変換されない()
        {
            // 1桁の数字はシーケンスとして認識されない
            var node = CreateKazuNode("三", "サン", 1);
            var nodes = new List<NjdNode> { node };

            DigitSequenceProcessor.Process(nodes);

            Assert.Single(nodes);
            Assert.Equal("三", nodes[0].Surface);
        }

        // ===== 数字と非数字が混在 =====

        [Fact]
        public void Process_数字と非数字の混在_非数字は影響されない()
        {
            var meishi = CreateMeishiNode("東京", "トウキョウ");
            var kazu1 = CreateKazuNode("一", "イチ");
            var kazu2 = CreateKazuNode("二", "ニ");
            var josuushi = CreateMeishiNode("回", "カイ", "副詞可能");

            var nodes = new List<NjdNode> { meishi, kazu1, kazu2, josuushi };

            DigitSequenceProcessor.Process(nodes);

            // 先頭の東京ノードは変更されない
            Assert.Equal("東京", nodes[0].Surface);
            Assert.Equal("トウキョウ", nodes[0].Pronunciation.ToKatakana());
        }

        // ===== 順序読みでの二の長音化 =====

        [Fact]
        public void Process_順序読みで二はニーに長音化()
        {
            // 〇二 → ゼロ・ニー（順序読み: 0始まり）
            var node0 = CreateKazuNode("〇", "ゼロ", 1);
            var node2 = CreateKazuNode("二", "ニ", 1);

            var nodes = new List<NjdNode> { node0, node2 };

            DigitSequenceProcessor.Process(nodes);

            // 二 → "ニー" に長音化
            Assert.Equal("ニー", nodes[1].Pronunciation.ToKatakana());
        }

        // ===== 順序読みでの五の長音化 =====

        [Fact]
        public void Process_順序読みで五はゴーに長音化()
        {
            // 〇五 → ゼロ・ゴー
            var node0 = CreateKazuNode("〇", "ゼロ", 1);
            var node5 = CreateKazuNode("五", "ゴ", 1);

            var nodes = new List<NjdNode> { node0, node5 };

            DigitSequenceProcessor.Process(nodes);

            Assert.Equal("ゴー", nodes[1].Pronunciation.ToKatakana());
        }
    }
}
