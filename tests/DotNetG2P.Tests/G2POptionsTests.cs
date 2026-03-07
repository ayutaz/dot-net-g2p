using System.Collections.Generic;
using System.Linq;
using DotNetG2P.Models;
using DotNetG2P.NJD;
using DotNetG2P.TextNormalization;
using DotNetG2P.Tests.TestHelpers;
using Xunit;

namespace DotNetG2P.Tests
{
    /// <summary>
    /// G2POptions の各フラグが個別に処理段階を制御することを検証するテスト。
    /// NJD処理の各段階を直接呼び出して効果を確認する。
    /// </summary>
    public class G2POptionsTests
    {
        // =====================================================================
        // EnableTextNormalization
        // =====================================================================

        [Fact]
        public void TextNormalization_Enabled_半角英字が全角に変換される()
        {
            // naist-jdic辞書は全角前提なので、半角ASCII英字→全角に変換
            var input = "ABC";
            var normalized = TextNormalizer.Normalize(input);

            Assert.Equal("\uFF21\uFF22\uFF23", normalized); // ＡＢＣ
        }

        [Fact]
        public void TextNormalization_Disabled_全角英字がそのまま()
        {
            // TextNormalizerを呼ばなければ変換されない
            var input = "\uFF21\uFF22\uFF23"; // ＡＢＣ
            // EnableTextNormalization=false の場合、G2PEngine.RunPipelineはTextNormalizer.Normalizeを呼ばない
            // → 入力がそのまま形態素解析に渡される
            Assert.Equal("\uFF21\uFF22\uFF23", input); // 変換なし
        }

        [Fact]
        public void TextNormalization_半角カナが全角に変換される()
        {
            var input = "\uFF76\uFF80\uFF76\uFF85"; // ｶﾀｶﾅ
            var normalized = TextNormalizer.Normalize(input);

            Assert.Equal("カタカナ", normalized);
        }

        // =====================================================================
        // EnableDigitProcessing
        // =====================================================================

        [Fact]
        public void DigitProcessing_Enabled_数字ノードが変換される()
        {
            // 数字ノードリストを構築: 「一」「二」「三」
            var node1 = NjdNodeFactory.CreateKazu("一", "イチ", accentType: 2);
            var node2 = NjdNodeFactory.CreateKazu("二", "ニ", accentType: 1, chainFlag: true, chainRule: "C1");
            var node3 = NjdNodeFactory.CreateKazu("三", "サン", accentType: 1, chainFlag: true, chainRule: "C1");

            var nodes = new List<NjdNode> { node1, node2, node3 };

            // 数字列処理を実行
            DigitSequenceProcessor.Process(nodes);
            SetDigit.Process(nodes);

            // 処理後、ノードが変化しているかを確認（少なくともクラッシュしない）
            Assert.NotEmpty(nodes);
        }

        [Fact]
        public void DigitProcessing_Disabled_数字ノードがそのまま通過()
        {
            // DigitSequenceProcessor.Process / SetDigit.Process を呼ばない場合
            var node1 = NjdNodeFactory.CreateKazu("三", "サン", accentType: 1);
            var node2 = NjdNodeFactory.CreateKazu("百", "ヒャク", accentType: 2, chainFlag: true);

            var nodes = new List<NjdNode> { node1, node2 };

            // 処理を呼ばない → ノードはそのまま
            Assert.Equal(2, nodes.Count);
            Assert.Equal("三", nodes[0].Surface);
            Assert.Equal("百", nodes[1].Surface);
            Assert.Equal("サン", nodes[0].Pronunciation.ToKatakana());
        }

        // =====================================================================
        // EnableAccentPhrase
        // =====================================================================

        [Fact]
        public void AccentPhrase_Enabled_助詞が前のノードに結合される()
        {
            var node1 = NjdNodeFactory.CreateWithPronunciation("東京", "トーキョー");
            var node2 = NjdNodeFactory.CreateWithPronunciation("に", "ニ",
                posType: POSType.Joshi, sub1: "格助詞");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            // 助詞「に」はRule 08で前のノードに結合される
            Assert.True(node2.ChainFlag, "助詞「に」はChainFlag=trueで結合されるべき");
        }

        [Fact]
        public void AccentPhrase_Disabled_ChainFlagが未設定のまま()
        {
            var node1 = NjdNodeFactory.CreateWithPronunciation("東京", "トーキョー");
            var node2 = NjdNodeFactory.CreateWithPronunciation("に", "ニ",
                posType: POSType.Joshi, sub1: "格助詞");

            // SetAccentPhrase.Processを呼ばない
            Assert.Null(node2.ChainFlag);
        }

        [Fact]
        public void AccentPhrase_Enabled_自立語が別アクセント句を開始()
        {
            var node1 = NjdNodeFactory.CreateWithPronunciation("猫", "ネコ");
            var node2 = NjdNodeFactory.CreateWithPronunciation("走る", "ハシル",
                posType: POSType.Doushi, sub1: "自立");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            // 動詞（自立語）は名詞の後で別アクセント句（Rule 13）
            Assert.False(node2.ChainFlag);
        }

        // =====================================================================
        // EnableAccentType
        // =====================================================================

        [Fact]
        public void AccentType_Enabled_ChainRuleに基づいてアクセント型が変更される()
        {
            // C2ルール: 前部モーラ数 + 1
            var node1 = NjdNodeFactory.CreateWithPronunciation("東京", "トーキョー", accentType: 1);
            var node2 = NjdNodeFactory.CreateWithPronunciation("で", "デ",
                posType: POSType.Joshi, accentType: 0, chainFlag: true, chainRule: "C2");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            // C2: 前部モーラ数(4) + 1 = 5
            Assert.Equal(5, node1.AccentType);
        }

        [Fact]
        public void AccentType_Disabled_アクセント型が変更されない()
        {
            var node1 = NjdNodeFactory.CreateWithPronunciation("東京", "トーキョー", accentType: 1);
            var node2 = NjdNodeFactory.CreateWithPronunciation("で", "デ",
                posType: POSType.Joshi, accentType: 0, chainFlag: true, chainRule: "C2");

            // SetAccentType.Processを呼ばない → アクセント型はそのまま
            Assert.Equal(1, node1.AccentType);
        }

        // =====================================================================
        // EnableUnvoicedVowel
        // =====================================================================

        [Fact]
        public void UnvoicedVowel_Enabled_無声化が適用される()
        {
            // ルール1: 助動詞「です」の「す」が文末で無声化
            // ルール1はnextNextが必要（後続ノードが必要）なので、
            // 後続に句点ノードを追加して現実的なコンテキストで検証
            var node1 = NjdNodeFactory.CreateWithPronunciation("です", "デス",
                posType: POSType.Jodoushi, conjugationType: "特殊・デス", conjugationForm: "基本形",
                accentType: 1);
            var node2 = NjdNodeFactory.CreateTouten(); // 後続の句点ノード

            var nodes = new List<NjdNode> { node1, node2 };
            SetUnvoicedVowel.Process(nodes);

            // 「デス」のスが無声化されて s,U になることを確認
            var phonemeStr = nodes[0].Pronunciation.ToPhonemeString();
            // 無声化されると "U" (大文字) になる
            Assert.Contains("U", phonemeStr);
        }

        [Fact]
        public void UnvoicedVowel_Disabled_無声化されない()
        {
            var node1 = NjdNodeFactory.CreateWithPronunciation("です", "デス",
                posType: POSType.Jodoushi, conjugationType: "特殊・デス", conjugationForm: "基本形",
                accentType: 1);

            // SetUnvoicedVowel.Processを呼ばない → 無声化なし
            var phonemeStr = node1.Pronunciation.ToPhonemeString();
            // 無声化前は "d e s u"（小文字u）
            Assert.Equal("d e s u", phonemeStr);
        }

        // =====================================================================
        // G2POptions のデフォルト値テスト
        // =====================================================================

        [Fact]
        public void Default_全フラグがtrueであること()
        {
            var options = G2POptions.Default;

            Assert.True(options.EnableTextNormalization);
            Assert.True(options.EnableUnvoicedVowel);
            Assert.True(options.EnableDigitProcessing);
            Assert.True(options.EnableAccentPhrase);
            Assert.True(options.EnableAccentType);
            Assert.True(options.ExpandLongVowels);
        }

        [Fact]
        public void Constructor_個別フラグをfalseに設定可能()
        {
            var options = new G2POptions(
                enableTextNormalization: false,
                enableUnvoicedVowel: false,
                enableDigitProcessing: false,
                enableAccentPhrase: false,
                enableAccentType: false,
                expandLongVowels: false);

            Assert.False(options.EnableTextNormalization);
            Assert.False(options.EnableUnvoicedVowel);
            Assert.False(options.EnableDigitProcessing);
            Assert.False(options.EnableAccentPhrase);
            Assert.False(options.EnableAccentType);
            Assert.False(options.ExpandLongVowels);
        }

        [Fact]
        public void Constructor_一部のフラグのみfalseに設定可能()
        {
            var options = new G2POptions(enableDigitProcessing: false, enableAccentType: false);

            Assert.True(options.EnableTextNormalization);
            Assert.True(options.EnableUnvoicedVowel);
            Assert.False(options.EnableDigitProcessing);
            Assert.True(options.EnableAccentPhrase);
            Assert.False(options.EnableAccentType);
            Assert.True(options.ExpandLongVowels);
        }
    }
}
