using System;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// ChineseG2PEngine C4統合テスト。
    /// IPA出力、注音出力、バッチAPI拡張、およびDispose後の動作を検証する。
    /// </summary>
    public class ChineseG2PEngineC4Tests : IDisposable
    {
        private readonly ChineseG2PEngine _engine;

        public ChineseG2PEngineC4Tests()
        {
            _engine = new ChineseG2PEngine();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =====================================================================
        // 1. ToIPA基本テスト
        // =====================================================================

        [Fact]
        public void ToIPA_你好_三声連読後IPA出力()
        {
            // 你好: ní hǎo（三声連読後）
            // ní → n + i + ˧˥ = "ni˧˥"
            // hǎo → x + aʊ + ˨˩˦ = "xa\u028A˨˩˦"
            var result = _engine.ToIPA("你好");
            Assert.Equal("ni\u02E7\u02E5 xa\u028A\u02E8\u02E9\u02E6", result);
        }

        [Fact]
        public void ToIPA_第1声_妈()
        {
            // 妈 mā: m + a + ˥˥
            var result = _engine.ToIPA("妈");
            Assert.Equal("ma\u02E5\u02E5", result);
        }

        [Fact]
        public void ToIPA_第2声_麻()
        {
            // 麻 má: m + a + ˧˥
            var result = _engine.ToIPA("麻");
            Assert.Equal("ma\u02E7\u02E5", result);
        }

        [Fact]
        public void ToIPA_第3声_马()
        {
            // 马 mǎ: m + a + ˨˩˦
            var result = _engine.ToIPA("马");
            Assert.Equal("ma\u02E8\u02E9\u02E6", result);
        }

        [Fact]
        public void ToIPA_第4声_骂()
        {
            // 骂 mà: m + a + ˥˩
            var result = _engine.ToIPA("骂");
            Assert.Equal("ma\u02E5\u02E9", result);
        }

        [Fact]
        public void ToIPA_IncludeTonesFalse_声調なし()
        {
            // 妈 mā → includeTones=false → "ma"
            var result = _engine.ToIPA("妈", false);
            Assert.Equal("ma", result);
        }

        [Fact]
        public void ToIPA_空文字列_空を返す()
        {
            Assert.Equal("", _engine.ToIPA(""));
        }

        [Fact]
        public void ToIPA_Null_空を返す()
        {
            Assert.Equal("", _engine.ToIPA(null));
        }

        // =====================================================================
        // 2. ToZhuyin基本テスト
        // =====================================================================

        [Fact]
        public void ToZhuyin_你好_三声連読後注音出力()
        {
            // 你好: ní hǎo（三声連読後）
            // ní → ㄋㄧˊ
            // hǎo → ㄏㄠˇ
            var result = _engine.ToZhuyin("你好");
            Assert.Equal("\u310B\u3127\u02CA \u310F\u3120\u02C7", result);
        }

        [Fact]
        public void ToZhuyin_第1声_妈_声調省略()
        {
            // 妈 mā: ㄇㄚ（1声は注音では省略）
            var result = _engine.ToZhuyin("妈");
            Assert.Equal("\u3107\u311A", result);
        }

        [Fact]
        public void ToZhuyin_第2声_麻()
        {
            // 麻 má: ㄇㄚˊ
            var result = _engine.ToZhuyin("麻");
            Assert.Equal("\u3107\u311A\u02CA", result);
        }

        [Fact]
        public void ToZhuyin_第3声_马()
        {
            // 马 mǎ: ㄇㄚˇ
            var result = _engine.ToZhuyin("马");
            Assert.Equal("\u3107\u311A\u02C7", result);
        }

        [Fact]
        public void ToZhuyin_第4声_骂()
        {
            // 骂 mà: ㄇㄚˋ
            var result = _engine.ToZhuyin("骂");
            Assert.Equal("\u3107\u311A\u02CB", result);
        }

        [Fact]
        public void ToZhuyin_IncludeTonesFalse_声調なし()
        {
            // 麻 má → includeTones=false → ㄇㄚ
            var result = _engine.ToZhuyin("麻", false);
            Assert.Equal("\u3107\u311A", result);
        }

        [Fact]
        public void ToZhuyin_空文字列_空を返す()
        {
            Assert.Equal("", _engine.ToZhuyin(""));
        }

        [Fact]
        public void ToZhuyin_Null_空を返す()
        {
            Assert.Equal("", _engine.ToZhuyin(null));
        }

        // =====================================================================
        // 3. 声調変調 + IPA/注音
        // =====================================================================

        [Fact]
        public void ToIPA_三声連読_你也好()
        {
            // 你也好: ní yé hǎo（三声連読）
            var result = _engine.ToIPA("你也好");
            // ni˧˥ jɤ˧˥ xaʊ˨˩˦
            // yé: Initial.Y + Final.E → ShouldOmitSemivowel → y+e系ではないのでjを出力
            // Y + E: ShouldOmitSemivowel → false → j + ɤ + ˧˥
            Assert.Contains("ni\u02E7\u02E5", result);
            Assert.Contains("xa\u028A\u02E8\u02E9\u02E6", result);
        }

        [Fact]
        public void ToZhuyin_三声連読_了解()
        {
            // 了解: liáo jiě（三声連読後）
            // liáo → ㄌㄧㄠˊ
            // jiě → ㄐㄧㄝˇ
            var result = _engine.ToZhuyin("了解");
            Assert.Contains("\u310C\u3127\u3120\u02CA", result); // ㄌㄧㄠˊ
            Assert.Contains("\u3110\u3127\u311D\u02C7", result); // ㄐㄧㄝˇ
        }

        [Fact]
        public void ToIPA_一変調_一天()
        {
            // 一天: yì tiān → IPA
            // yì: Initial.Y + Final.I → ShouldOmitSemivowel → true → i + ˥˩
            // tiān: tʰ + iɛn + ˥˥
            var result = _engine.ToIPA("一天");
            Assert.Contains("i\u02E5\u02E9", result); // i˥˩
        }

        [Fact]
        public void ToZhuyin_一変調_一个()
        {
            // 一个: yí gè（一変調後）
            // yí → ㄧˊ
            // gè → ㄍㄜˋ
            var result = _engine.ToZhuyin("一个");
            Assert.Contains("\u3127\u02CA", result); // ㄧˊ
        }

        [Fact]
        public void ToIPA_不変調_不要()
        {
            // 不要: bú yào
            // bú → p + u + ˧˥
            // yào → j + aʊ + ˥˩  (Y + Ao → ShouldOmitSemivowel(Y, Ao)=false → j出力)
            var result = _engine.ToIPA("不要");
            Assert.Contains("pu\u02E7\u02E5", result); // pu˧˥
        }

        [Fact]
        public void ToZhuyin_不変調_不对()
        {
            // 不对: bú duì
            // bú → ㄅㄨˊ
            var result = _engine.ToZhuyin("不对");
            Assert.Contains("\u3105\u3128\u02CA", result); // ㄅㄨˊ
        }

        [Fact]
        public void ToIPA_EnableToneSandhiFalse_你好_変調なし()
        {
            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToIPA("你好");
            // nǐ → ni˨˩˦ (第3声のまま)
            Assert.Contains("ni\u02E8\u02E9\u02E6", result);
        }

        [Fact]
        public void ToZhuyin_EnableToneSandhiFalse_你好_変調なし()
        {
            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToZhuyin("你好");
            // nǐ → ㄋㄧˇ（第3声のまま）
            Assert.Contains("\u310B\u3127\u02C7", result); // ㄋㄧˇ
        }

        // =====================================================================
        // 4. バッチAPI
        // =====================================================================

        [Fact]
        public void ToPinyinBatch_WithStyle_ToneNumber()
        {
            var result = _engine.ToPinyinBatch(new[] { "你好", "中国" }, PinyinStyle.ToneNumber);
            Assert.Equal(2, result.Count);
            Assert.Equal("ni2 hao3", result[0]);
            Assert.Equal("zhong1 guo2", result[1]);
        }

        [Fact]
        public void ToPinyinBatch_WithStyle_Normal()
        {
            var result = _engine.ToPinyinBatch(new[] { "你好" }, PinyinStyle.Normal);
            Assert.Equal("ni hao", result[0]);
        }

        [Fact]
        public void ToPinyinListBatch_デフォルトスタイル()
        {
            var result = _engine.ToPinyinListBatch(new[] { "你好", "中" });
            Assert.Equal(2, result.Count);
            Assert.Equal(2, result[0].Length); // ní, hǎo
            Assert.Single(result[1]); // zhōng
        }

        [Fact]
        public void ToPinyinListBatch_WithStyle_ToneNumber()
        {
            var result = _engine.ToPinyinListBatch(new[] { "你好" }, PinyinStyle.ToneNumber);
            Assert.Equal(new[] { "ni2", "hao3" }, result[0]);
        }

        [Fact]
        public void ToIPABatch_基本()
        {
            var result = _engine.ToIPABatch(new[] { "妈", "麻" });
            Assert.Equal(2, result.Count);
            Assert.Equal("ma\u02E5\u02E5", result[0]);
            Assert.Equal("ma\u02E7\u02E5", result[1]);
        }

        [Fact]
        public void ToIPABatch_IncludeTonesFalse()
        {
            var result = _engine.ToIPABatch(new[] { "妈", "麻" }, false);
            Assert.Equal(2, result.Count);
            Assert.Equal("ma", result[0]);
            Assert.Equal("ma", result[1]);
        }

        [Fact]
        public void ToZhuyinBatch_基本()
        {
            var result = _engine.ToZhuyinBatch(new[] { "妈", "麻" });
            Assert.Equal(2, result.Count);
            Assert.Equal("\u3107\u311A", result[0]); // ㄇㄚ（1声省略）
            Assert.Equal("\u3107\u311A\u02CA", result[1]); // ㄇㄚˊ
        }

        [Fact]
        public void ToZhuyinBatch_IncludeTonesFalse()
        {
            var result = _engine.ToZhuyinBatch(new[] { "妈", "麻" }, false);
            Assert.Equal(2, result.Count);
            Assert.Equal("\u3107\u311A", result[0]); // ㄇㄚ
            Assert.Equal("\u3107\u311A", result[1]); // ㄇㄚ
        }

        [Fact]
        public void ToPinyinBatch_WithStyle_Null引数_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToPinyinBatch(null, PinyinStyle.Normal));
        }

        [Fact]
        public void ToPinyinListBatch_Null引数_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToPinyinListBatch(null));
        }

        [Fact]
        public void ToPinyinListBatch_WithStyle_Null引数_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToPinyinListBatch(null, PinyinStyle.ToneMarked));
        }

        [Fact]
        public void ToIPABatch_Null引数_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToIPABatch(null));
        }

        [Fact]
        public void ToIPABatch_WithTones_Null引数_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToIPABatch(null, true));
        }

        [Fact]
        public void ToZhuyinBatch_Null引数_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToZhuyinBatch(null));
        }

        [Fact]
        public void ToZhuyinBatch_WithTones_Null引数_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToZhuyinBatch(null, true));
        }

        [Fact]
        public void ToIPABatch_空配列_空配列を返す()
        {
            var result = _engine.ToIPABatch(Array.Empty<string>());
            Assert.Empty(result);
        }

        [Fact]
        public void ToZhuyinBatch_空配列_空配列を返す()
        {
            var result = _engine.ToZhuyinBatch(Array.Empty<string>());
            Assert.Empty(result);
        }

        [Fact]
        public void ToPinyinBatch_WithStyle_空配列_空配列を返す()
        {
            var result = _engine.ToPinyinBatch(Array.Empty<string>(), PinyinStyle.ToneMarked);
            Assert.Empty(result);
        }

        [Fact]
        public void ToPinyinListBatch_空配列_空配列を返す()
        {
            var result = _engine.ToPinyinListBatch(Array.Empty<string>());
            Assert.Empty(result);
        }

        // =====================================================================
        // 5. Dispose後テスト
        // =====================================================================

        [Fact]
        public void ToIPA_Dispose後_ObjectDisposedException()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPA("你好"));
        }

        [Fact]
        public void ToIPA_WithTones_Dispose後_ObjectDisposedException()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPA("你好", false));
        }

        [Fact]
        public void ToZhuyin_Dispose後_ObjectDisposedException()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToZhuyin("你好"));
        }

        [Fact]
        public void ToZhuyin_WithTones_Dispose後_ObjectDisposedException()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToZhuyin("你好", false));
        }

        [Fact]
        public void ToIPABatch_Dispose後_ObjectDisposedException()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPABatch(new[] { "你好" }));
        }

        [Fact]
        public void ToZhuyinBatch_Dispose後_ObjectDisposedException()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToZhuyinBatch(new[] { "你好" }));
        }

        [Fact]
        public void ToPinyinBatch_WithStyle_Dispose後_ObjectDisposedException()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPinyinBatch(new[] { "你好" }, PinyinStyle.Normal));
        }

        [Fact]
        public void ToPinyinListBatch_Dispose後_ObjectDisposedException()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPinyinListBatch(new[] { "你好" }));
        }
    }
}
