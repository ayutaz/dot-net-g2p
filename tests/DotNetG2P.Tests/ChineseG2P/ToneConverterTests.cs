using System;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// ToneConverter の単体テスト。
    /// ExtractTone / RemoveTone / ToToneNumber / ToToneMarked の各メソッドを検証する。
    /// </summary>
    public class ToneConverterTests
    {
        // ===== ExtractTone: 各声調の検出 =====

        [Theory]
        [InlineData("zhōng", Tone.First)]
        [InlineData("guó", Tone.Second)]
        [InlineData("nǐ", Tone.Third)]
        [InlineData("shì", Tone.Fourth)]
        [InlineData("de", Tone.Neutral)]
        public void ExtractTone_BasicTones_ReturnsExpected(string pinyin, Tone expected)
        {
            Assert.Equal(expected, ToneConverter.ExtractTone(pinyin));
        }

        [Theory]
        [InlineData("ā", Tone.First)]
        [InlineData("é", Tone.Second)]
        [InlineData("ǐ", Tone.Third)]
        [InlineData("ò", Tone.Fourth)]
        [InlineData("ū", Tone.First)]
        [InlineData("ǖ", Tone.First)]
        [InlineData("ǘ", Tone.Second)]
        [InlineData("ǚ", Tone.Third)]
        [InlineData("ǜ", Tone.Fourth)]
        public void ExtractTone_SingleVowel_ReturnsExpected(string pinyin, Tone expected)
        {
            Assert.Equal(expected, ToneConverter.ExtractTone(pinyin));
        }

        [Theory]
        [InlineData("lǜ", Tone.Fourth)]
        [InlineData("nǚ", Tone.Third)]
        public void ExtractTone_UmlautVowel_ReturnsExpected(string pinyin, Tone expected)
        {
            Assert.Equal(expected, ToneConverter.ExtractTone(pinyin));
        }

        [Fact]
        public void ExtractTone_EmptyString_ReturnsNeutral()
        {
            Assert.Equal(Tone.Neutral, ToneConverter.ExtractTone(""));
        }

        [Fact]
        public void ExtractTone_Null_ReturnsNeutral()
        {
            Assert.Equal(Tone.Neutral, ToneConverter.ExtractTone(null!));
        }

        [Theory]
        [InlineData("ma")]
        [InlineData("shi")]
        [InlineData("bcd")]
        public void ExtractTone_NoToneMark_ReturnsNeutral(string pinyin)
        {
            Assert.Equal(Tone.Neutral, ToneConverter.ExtractTone(pinyin));
        }

        // ===== RemoveTone: 声調記号の除去 =====

        [Theory]
        [InlineData("zhōng", "zhong")]
        [InlineData("guó", "guo")]
        [InlineData("nǐ", "ni")]
        [InlineData("shì", "shi")]
        [InlineData("de", "de")]
        public void RemoveTone_BasicCases_ReturnsExpected(string pinyin, string expected)
        {
            Assert.Equal(expected, ToneConverter.RemoveTone(pinyin));
        }

        [Theory]
        [InlineData("lǜ", "l\u00fc")]
        [InlineData("nǚ", "n\u00fc")]
        [InlineData("lǘ", "l\u00fc")]
        public void RemoveTone_UmlautVowel_ReturnsUmlaut(string pinyin, string expected)
        {
            Assert.Equal(expected, ToneConverter.RemoveTone(pinyin));
        }

        [Fact]
        public void RemoveTone_EmptyString_ReturnsEmpty()
        {
            Assert.Equal("", ToneConverter.RemoveTone(""));
        }

        [Fact]
        public void RemoveTone_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, ToneConverter.RemoveTone(null!));
        }

        [Theory]
        [InlineData("zhong")]
        [InlineData("guo")]
        [InlineData("ni")]
        public void RemoveTone_NoToneMark_ReturnsSameString(string pinyin)
        {
            Assert.Equal(pinyin, ToneConverter.RemoveTone(pinyin));
        }

        // ===== ToToneNumber: 声調記号付き → 数字末尾 =====

        [Theory]
        [InlineData("zhōng", "zhong1")]
        [InlineData("guó", "guo2")]
        [InlineData("nǐ", "ni3")]
        [InlineData("shì", "shi4")]
        public void ToToneNumber_TonedPinyin_ReturnsNumberSuffix(string pinyin, string expected)
        {
            Assert.Equal(expected, ToneConverter.ToToneNumber(pinyin));
        }

        [Theory]
        [InlineData("de", "de")]
        [InlineData("ma", "ma")]
        public void ToToneNumber_NeutralTone_ReturnsWithoutNumber(string pinyin, string expected)
        {
            Assert.Equal(expected, ToneConverter.ToToneNumber(pinyin));
        }

        [Theory]
        [InlineData("lǜ", "l\u00fc4")]
        [InlineData("nǚ", "n\u00fc3")]
        public void ToToneNumber_UmlautVowel_ReturnsCorrectNumber(string pinyin, string expected)
        {
            Assert.Equal(expected, ToneConverter.ToToneNumber(pinyin));
        }

        [Fact]
        public void ToToneNumber_EmptyString_ReturnsEmpty()
        {
            Assert.Equal("", ToneConverter.ToToneNumber(""));
        }

        [Fact]
        public void ToToneNumber_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, ToneConverter.ToToneNumber(null!));
        }

        // ===== ToToneMarked: 数字末尾 → 声調記号付き =====

        [Theory]
        [InlineData("zhong1", "zhōng")]
        [InlineData("guo2", "guó")]
        [InlineData("ni3", "nǐ")]
        [InlineData("shi4", "shì")]
        public void ToToneMarked_NumberSuffix_ReturnsToneMark(string pinyin, string expected)
        {
            Assert.Equal(expected, ToneConverter.ToToneMarked(pinyin));
        }

        [Theory]
        [InlineData("de", "de")]
        [InlineData("ma", "ma")]
        public void ToToneMarked_NoNumber_ReturnsSame(string pinyin, string expected)
        {
            Assert.Equal(expected, ToneConverter.ToToneMarked(pinyin));
        }

        [Fact]
        public void ToToneMarked_EmptyString_ReturnsEmpty()
        {
            Assert.Equal("", ToneConverter.ToToneMarked(""));
        }

        [Fact]
        public void ToToneMarked_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, ToneConverter.ToToneMarked(null!));
        }

        [Theory]
        [InlineData("a1", "ā")]
        [InlineData("a2", "\u00e1")]
        [InlineData("a3", "ǎ")]
        [InlineData("a4", "à")]
        public void ToToneMarked_SingleVowel_AllTones(string pinyin, string expected)
        {
            Assert.Equal(expected, ToneConverter.ToToneMarked(pinyin));
        }

        // ===== ToToneMarked: 声調配置ルール =====

        [Theory]
        [InlineData("bai1", "bāi")]   // a がある場合は a に配置
        [InlineData("mei2", "méi")]   // e がある場合は e に配置
        [InlineData("gou3", "gǒu")]   // ou の場合は o に配置
        [InlineData("liu2", "liú")]   // それ以外は最後の母音に配置
        public void ToToneMarked_PlacementRules_ReturnsCorrectPosition(string pinyin, string expected)
        {
            Assert.Equal(expected, ToneConverter.ToToneMarked(pinyin));
        }

        // ===== ToToneNumber → ToToneMarked ラウンドトリップ =====

        [Theory]
        [InlineData("zhōng")]
        [InlineData("guó")]
        [InlineData("nǐ")]
        [InlineData("shì")]
        [InlineData("ā")]
        public void Roundtrip_ToneMarked_ToNumber_ToMarked(string original)
        {
            var numbered = ToneConverter.ToToneNumber(original);
            var roundtripped = ToneConverter.ToToneMarked(numbered);
            Assert.Equal(original, roundtripped);
        }

        // ===== 数字5/0は末尾でも声調変換されない =====

        [Theory]
        [InlineData("de5")]
        [InlineData("de0")]
        public void ToToneMarked_Tone5Or0_ReturnsSame(string pinyin)
        {
            // 末尾が5や0は1-4の範囲外なのでそのまま返る
            Assert.Equal(pinyin, ToneConverter.ToToneMarked(pinyin));
        }
    }
}
