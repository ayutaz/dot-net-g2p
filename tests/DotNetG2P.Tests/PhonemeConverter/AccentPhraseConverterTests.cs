using DotNetG2P.Models;
using DotNetG2P.PhonemeConverter;

namespace DotNetG2P.Tests.PhonemeConverter
{
    public class AccentPhraseConverterTests
    {
        // ===== ヘルパーメソッド =====

        /// <summary>テスト用のNjdNodeを作成する</summary>
        private static NjdNode CreateNode(string surface, string katakana, int accentType = 0, bool? chainFlag = null)
        {
            var pronunciation = Pronunciation.FromKatakana(katakana, accentType);
            var details = new WordDetails(
                new POS(POSType.Meishi),
                "*", "*", surface, katakana, pronunciation
            );
            var node = new NjdNode(surface, details)
            {
                AccentType = accentType,
                ChainFlag = chainFlag,
            };
            node.Pronunciation = pronunciation;
            return node;
        }

        /// <summary>句点ノードを作成する</summary>
        private static NjdNode CreateToutenNode(string surface = "、")
        {
            var moras = new List<Mora> { new Mora(null, null, MoraKind.Touten) };
            var pronunciation = new Pronunciation(moras, 0);
            var details = new WordDetails(
                new POS(POSType.Kigou),
                "*", "*", surface, surface, pronunciation
            );
            var node = new NjdNode(surface, details)
            {
                AccentType = 0,
                ChainFlag = false,
            };
            node.Pronunciation = pronunciation;
            return node;
        }

        /// <summary>疑問符ノードを作成する</summary>
        private static NjdNode CreateQuestionNode()
        {
            var moras = new List<Mora> { new Mora(null, null, MoraKind.Question) };
            var pronunciation = new Pronunciation(moras, 0);
            var details = new WordDetails(
                new POS(POSType.Kigou),
                "*", "*", "？", "？", pronunciation
            );
            var node = new NjdNode("？", details)
            {
                AccentType = 0,
                ChainFlag = true,
            };
            node.Pronunciation = pronunciation;
            return node;
        }

        // ===== 単一アクセント句のテスト =====

        [Fact]
        public void Convert_SingleNode_ReturnsSingleAccentPhrase()
        {
            // "コンニチワ" (5モーラ、アクセント0)
            var nodes = new List<NjdNode>
            {
                CreateNode("こんにちは", "コンニチワ", accentType: 0)
            };

            var result = AccentPhraseConverter.Convert(nodes);

            Assert.Single(result);
            Assert.Equal(5, result[0].Moras.Count);
            Assert.Equal(0, result[0].Accent);
            Assert.Null(result[0].PauseMora);
            Assert.False(result[0].IsInterrogative);
        }

        [Fact]
        public void Convert_SingleNode_MorasMatchPronunciation()
        {
            // "サクラ" (3モーラ、アクセント2)
            var nodes = new List<NjdNode>
            {
                CreateNode("桜", "サクラ", accentType: 2)
            };

            var result = AccentPhraseConverter.Convert(nodes);

            Assert.Single(result);
            Assert.Equal(3, result[0].Moras.Count);
            Assert.Equal(2, result[0].Accent);
            // サ
            Assert.Equal(MoraKind.Sa, result[0].Moras[0].Kind);
            // ク
            Assert.Equal(MoraKind.Ku, result[0].Moras[1].Kind);
            // ラ
            Assert.Equal(MoraKind.Ra, result[0].Moras[2].Kind);
        }

        // ===== 複数アクセント句のテスト =====

        [Fact]
        public void Convert_TwoSeparatePhrases_ReturnsTwoAccentPhrases()
        {
            // "キョウワ"(アクセント1) + "テンキデス"(アクセント1, ChainFlag=false)
            var nodes = new List<NjdNode>
            {
                CreateNode("今日は", "キョウワ", accentType: 1),
                CreateNode("天気です", "テンキデス", accentType: 1, chainFlag: false)
            };

            var result = AccentPhraseConverter.Convert(nodes);

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Accent);
            Assert.Equal(1, result[1].Accent);
        }

        [Fact]
        public void Convert_ChainedNodes_MergedIntoSinglePhrase()
        {
            // "トウキョウ"(アクセント0) + "ト"(ChainFlag=true, アクセント0)
            // → 1つのアクセント句にまとまる
            var nodes = new List<NjdNode>
            {
                CreateNode("東京", "トウキョウ", accentType: 3),
                CreateNode("と", "ト", accentType: 0, chainFlag: true)
            };

            var result = AccentPhraseConverter.Convert(nodes);

            Assert.Single(result);
            // 先頭ノードのアクセント型を使用
            Assert.Equal(3, result[0].Accent);
            // モーラ数: トウキョウ(4) + ト(1) = 5
            Assert.Equal(5, result[0].Moras.Count);
        }

        [Fact]
        public void Convert_MixedChainAndSeparate_ReturnsCorrectPhrases()
        {
            // [ワタシワ] + [ガクセイ + デス(chain)]
            var nodes = new List<NjdNode>
            {
                CreateNode("私は", "ワタシワ", accentType: 0),
                CreateNode("学生", "ガクセイ", accentType: 0, chainFlag: false),
                CreateNode("です", "デス", accentType: 1, chainFlag: true)
            };

            var result = AccentPhraseConverter.Convert(nodes);

            Assert.Equal(2, result.Count);
            Assert.Equal(4, result[0].Moras.Count); // ワタシワ
            Assert.Equal(6, result[1].Moras.Count); // ガクセイデス (6モーラ)
        }

        // ===== ポーズ（PauseMora）のテスト =====

        [Fact]
        public void Convert_ToutenBetweenPhrases_SetsPauseMoraOnPreviousPhrase()
        {
            // "キョウワ" + "、" + "テンキデス"
            var nodes = new List<NjdNode>
            {
                CreateNode("今日は", "キョウワ", accentType: 1),
                CreateToutenNode("、"),
                CreateNode("天気です", "テンキデス", accentType: 1, chainFlag: false)
            };

            var result = AccentPhraseConverter.Convert(nodes);

            Assert.Equal(2, result.Count);
            // 直前のアクセント句にPauseMoraが設定される
            Assert.NotNull(result[0].PauseMora);
            Assert.Equal(MoraKind.Touten, result[0].PauseMora!.Value.Kind);
            // 後続のアクセント句にはPauseMoraなし
            Assert.Null(result[1].PauseMora);
        }

        [Fact]
        public void Convert_ToutenAtEnd_SetsPauseMoraOnLastPhrase()
        {
            // "コンニチワ" + "。"
            var nodes = new List<NjdNode>
            {
                CreateNode("こんにちは", "コンニチワ", accentType: 0),
                CreateToutenNode("。")
            };

            var result = AccentPhraseConverter.Convert(nodes);

            Assert.Single(result);
            Assert.NotNull(result[0].PauseMora);
        }

        // ===== 疑問文のテスト =====

        [Fact]
        public void Convert_QuestionNode_SetsIsInterrogative()
        {
            // "ナニ" + "？"
            var nodes = new List<NjdNode>
            {
                CreateNode("何", "ナニ", accentType: 1),
                CreateQuestionNode()
            };

            var result = AccentPhraseConverter.Convert(nodes);

            Assert.Single(result);
            Assert.True(result[0].IsInterrogative);
        }

        // ===== 空リストのテスト =====

        [Fact]
        public void Convert_EmptyList_ReturnsEmptyList()
        {
            var result = AccentPhraseConverter.Convert(new List<NjdNode>());
            Assert.Empty(result);
        }

        [Fact]
        public void Convert_NullList_ReturnsEmptyList()
        {
            var result = AccentPhraseConverter.Convert(null);
            Assert.Empty(result);
        }

        [Fact]
        public void Convert_OnlyEmptyNodes_ReturnsEmptyList()
        {
            var node = new NjdNode("テスト", new WordDetails(
                new POS(POSType.Meishi), "*", "*", "テスト", "テスト"
            ));
            node.Reset(); // 空ノードにする

            var result = AccentPhraseConverter.Convert(new List<NjdNode> { node });
            Assert.Empty(result);
        }
    }
}
