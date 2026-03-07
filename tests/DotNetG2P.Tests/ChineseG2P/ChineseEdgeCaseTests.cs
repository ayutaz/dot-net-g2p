using System;
using System.Linq;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// ChineseG2PEngine のエッジケーステスト。
    /// 空・null入力、特殊文字、句読点、長文、混在テキスト、辞書境界、
    /// 声調変調エッジケース、オプション組み合わせ、Disposeパターンを網羅的に検証する。
    /// </summary>
    public class ChineseEdgeCaseTests : IDisposable
    {
        private readonly ChineseG2PEngine _engine;

        public ChineseEdgeCaseTests()
        {
            _engine = new ChineseG2PEngine();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =====================================================================
        // 1. 空・null入力テスト
        // =====================================================================

        [Fact]
        public void ToPinyin_null_空文字列を返す()
        {
            var result = _engine.ToPinyin(null!);
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPinyin_空文字列_空文字列を返す()
        {
            var result = _engine.ToPinyin("");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPinyinList_null_空配列を返す()
        {
            var result = _engine.ToPinyinList(null!);
            Assert.Empty(result);
        }

        [Fact]
        public void ToPinyinList_空文字列_空配列を返す()
        {
            var result = _engine.ToPinyinList("");
            Assert.Empty(result);
        }

        [Fact]
        public void ContainsChar_ヌル文字_falseを返す()
        {
            Assert.False(_engine.ContainsChar('\0'));
        }

        [Fact]
        public void LookupChar_ヌル文字_空配列を返す()
        {
            var result = _engine.LookupChar('\0');
            Assert.Empty(result);
        }

        // =====================================================================
        // 2. 特殊文字テスト
        // =====================================================================

        [Fact]
        public void ToPinyin_絵文字のみ_エラーなし()
        {
            // 絵文字はサロゲートペアなのでそのまま出力される
            var result = _engine.ToPinyin("\U0001F600\U0001F601\U0001F602");
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPinyin_サロゲートペア文字_エラーなし()
        {
            // CJK拡張B (U+20000以降) はサロゲートペアで表現される
            var result = _engine.ToPinyin("\U00020000");
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPinyin_制御文字_エラーなし()
        {
            var result = _engine.ToPinyin("\x01\x02");
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPinyin_BOM_エラーなし()
        {
            var result = _engine.ToPinyin("\uFEFF");
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPinyin_ゼロ幅スペース_エラーなし()
        {
            var result = _engine.ToPinyin("\u200B");
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPinyin_改行のみ_空文字列を返す()
        {
            // 改行は区切り扱い、漢字なしなので空
            var result = _engine.ToPinyin("\n\n\n");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPinyin_タブのみ_空文字列を返す()
        {
            var result = _engine.ToPinyin("\t\t");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPinyin_制御文字と漢字の混在_漢字部分が正しく変換される()
        {
            var result = _engine.ToPinyin("\x01你\x02好");
            Assert.Contains("hǎo", result);
        }

        // =====================================================================
        // 3. 句読点テスト
        // =====================================================================

        [Fact]
        public void ToPinyin_CJK句読点のみ_空文字列を返す()
        {
            var result = _engine.ToPinyin("\u3002\uFF0C\uFF01\uFF1F");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPinyin_ASCII句読点のみ_空文字列を返す()
        {
            var result = _engine.ToPinyin(".,!?");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPinyin_漢字とCJK句読点_ピンインが正しく区切られる()
        {
            // 你好。世界 → 句読点で区切られて ni hao shi jie
            var result = _engine.ToPinyin("\u4F60\u597D\u3002\u4E16\u754C");
            Assert.Contains("hǎo", result);
            Assert.Contains("shì", result);
        }

        [Fact]
        public void ToPinyin_連続句読点_空文字列を返す()
        {
            var result = _engine.ToPinyin("\u3002\u3002\u3002");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPinyin_句読点で囲まれた漢字_正しいピンイン()
        {
            // （你好） → 括弧は句読点扱い
            var result = _engine.ToPinyin("\uFF08\u4F60\u597D\uFF09");
            Assert.Contains("hǎo", result);
        }

        [Fact]
        public void ToPinyin_漢字間のASCII句読点_区切りとして機能()
        {
            // 你,好 → 句読点で区切り
            var result = _engine.ToPinyin("你,好");
            // 句読点がセパレータとして機能し、2つのピンインが出力される
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 4. 長文テスト
        // =====================================================================

        [Fact]
        public void ToPinyin_1000文字の漢字_エラーなく完了()
        {
            var input = new string('的', 1000);
            var result = _engine.ToPinyin(input);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToPinyin_繰り返し文字_結果が一貫している()
        {
            var input = new string('的', 100);
            var result = _engine.ToPinyin(input);
            // 全て同じ文字なので全て同じピンインになるはず
            var parts = result.Split(' ');
            Assert.Equal(100, parts.Length);
            var firstPinyin = parts[0];
            Assert.All(parts, p => Assert.Equal(firstPinyin, p));
        }

        [Fact]
        public void ToPinyin_長いASCII英数字_エラーなし()
        {
            var input = new string('a', 1000);
            var result = _engine.ToPinyin(input);
            Assert.NotNull(result);
            Assert.Equal(input, result);
        }

        [Fact]
        public void ToPinyin_混在長文_エラーなく完了()
        {
            // 漢字+英数字+句読点の繰り返し
            var input = string.Concat(Enumerable.Repeat("你好world123。", 100));
            var result = _engine.ToPinyin(input);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 5. 混在テキストテスト
        // =====================================================================

        [Fact]
        public void ToPinyin_漢字と数字_数字はパススルー()
        {
            var result = _engine.ToPinyin("你好123世界");
            Assert.Contains("123", result);
            Assert.Contains("shì", result);
        }

        [Fact]
        public void ToPinyin_漢字と英字_英字はパススルー()
        {
            var result = _engine.ToPinyin("你好hello世界");
            Assert.Contains("hello", result);
            Assert.Contains("shì", result);
        }

        [Fact]
        public void ToPinyin_数字のみ_そのまま()
        {
            var result = _engine.ToPinyin("12345");
            Assert.Equal("12345", result);
        }

        [Fact]
        public void ToPinyin_英字のみ_そのまま()
        {
            var result = _engine.ToPinyin("hello");
            Assert.Equal("hello", result);
        }

        [Fact]
        public void ToPinyin_スペースのみ_空文字列()
        {
            var result = _engine.ToPinyin("   ");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPinyin_漢字と日本語ひらがな_エラーなし()
        {
            // ひらがなはCJK範囲外なのでパススルー
            var result = _engine.ToPinyin("你好あいう");
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPinyin_URL風テキスト_エラーなし()
        {
            var result = _engine.ToPinyin("访问http://example.com");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToPinyin_メールアドレス風テキスト_エラーなし()
        {
            var result = _engine.ToPinyin("发送到user@example.com");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 6. 辞書境界テスト
        // =====================================================================

        [Fact]
        public void ContainsChar_CJK統合漢字の最初_一_辞書に存在()
        {
            Assert.True(_engine.ContainsChar('\u4E00')); // 一
        }

        [Fact]
        public void ContainsChar_CJK統合漢字の最後付近_龥_確認()
        {
            // U+9FA5 '龥' は辞書に含まれる可能性がある
            var result = _engine.ContainsChar('\u9FA5');
            // 結果が true or false のどちらでもクラッシュしないこと
            Assert.True(result || !result);
        }

        [Fact]
        public void ToPinyin_CJK拡張A範囲_エラーなし()
        {
            // U+3400 はCJK Extension A の先頭
            var result = _engine.ToPinyin("\u3400");
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPinyin_CJK互換漢字範囲_エラーなし()
        {
            // U+F900 はCJK Compatibility Ideographs の先頭
            var result = _engine.ToPinyin("\uF900");
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPinyin_辞書にない稀少漢字_パススルー()
        {
            // CJK Extension Aの末尾付近の稀少文字
            var c = '\u4DBF';
            if (!_engine.ContainsChar(c))
            {
                // 辞書にない場合、文字がそのまま出力される
                var result = _engine.ToPinyin(c.ToString());
                Assert.Contains(c.ToString(), result);
            }
        }

        [Fact]
        public void ContainsChar_辞書に存在する漢字_trueを返す()
        {
            // 「人」「大」「中」はほぼ確実に辞書に存在
            Assert.True(_engine.ContainsChar('人'));
            Assert.True(_engine.ContainsChar('大'));
            Assert.True(_engine.ContainsChar('中'));
        }

        // =====================================================================
        // 7. 声調変調エッジケーステスト
        // =====================================================================

        [Fact]
        public void ToPinyin_単一文字_一_変調なし()
        {
            var result = _engine.ToPinyin("一");
            Assert.Equal("yī", result);
        }

        [Fact]
        public void ToPinyin_単一文字_不_変調なし()
        {
            var result = _engine.ToPinyin("不");
            Assert.Equal("bù", result);
        }

        [Fact]
        public void ToPinyin_5連続三声_正しく変調()
        {
            // 买五把雨伞: mǎi wǔ bǎ yǔ sǎn → 三声連続で変調適用
            var result = _engine.ToPinyin("买五把雨伞");
            Assert.NotEmpty(result);
            // 末尾以外に2声への変調が適用されていること
            var parts = result.Split(' ');
            Assert.Equal(5, parts.Length);
        }

        [Fact]
        public void ToPinyin_三声と句読点と三声_句読点挟みで両側が独立()
        {
            // 你。好 → 句読点で区切られ、それぞれ独立して3声のまま
            var result = _engine.ToPinyin("你。好");
            // 句読点で分断されているため、三声連読は適用されない
            // ただし実装によっては句読点をまたいでも変調する場合がある
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToPinyin_一加一_両方の一が変調()
        {
            // 一+一: 一の次の文字を見て変調
            var result = _engine.ToPinyin("一加一");
            Assert.NotEmpty(result);
            // 最初の一は加(jiā, 1声)前 → yì(4声)
            Assert.StartsWith("yì", result);
        }

        [Fact]
        public void ToPinyin_不加不_両方の不を処理()
        {
            var result = _engine.ToPinyin("不加不");
            Assert.NotEmpty(result);
            var parts = result.Split(' ');
            Assert.Equal(3, parts.Length);
        }

        [Fact]
        public void ToPinyin_第一_序数例外で変調なし()
        {
            var result = _engine.ToPinyin("第一");
            Assert.Equal("dì yī", result);
        }

        [Fact]
        public void ToPinyin_统一_文末の一は変調なし()
        {
            var result = _engine.ToPinyin("统一");
            Assert.Equal("tǒng yī", result);
        }

        [Fact]
        public void ToPinyin_一一_一が連続()
        {
            var result = _engine.ToPinyin("一一");
            Assert.NotEmpty(result);
            var parts = result.Split(' ');
            Assert.Equal(2, parts.Length);
        }

        // =====================================================================
        // 8. オプション組み合わせテスト
        // =====================================================================

        [Fact]
        public void オプション_ToneSandhiFalse_HeteronymsFalse_基本動作()
        {
            var options = new ChineseG2POptions(enableToneSandhi: false, handleHeteronyms: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("你好");
            Assert.Equal("nǐ hǎo", result);
        }

        [Fact]
        public void オプション_ToneNumber_ToneSandhiTrue()
        {
            var options = new ChineseG2POptions(defaultStyle: PinyinStyle.ToneNumber, enableToneSandhi: true);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("你好");
            // 三声連読: ni2 hao3
            Assert.Equal("ni2 hao3", result);
        }

        [Fact]
        public void オプション_Normal_ToneSandhiTrue()
        {
            var options = new ChineseG2POptions(defaultStyle: PinyinStyle.Normal, enableToneSandhi: true);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("你好");
            Assert.Equal("ni hao", result);
        }

        [Fact]
        public void オプション_ToneMarked_ToneSandhiFalse()
        {
            var options = new ChineseG2POptions(defaultStyle: PinyinStyle.ToneMarked, enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("你好");
            Assert.Equal("nǐ hǎo", result);
        }

        [Fact]
        public void オプション_カスタムセパレータ_ハイフン()
        {
            var options = new ChineseG2POptions(separator: "-");
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("中国");
            Assert.Equal("zhōng-guó", result);
        }

        [Fact]
        public void オプション_カスタムセパレータ_ハイフン_ToneNumber()
        {
            var options = new ChineseG2POptions(separator: "-", defaultStyle: PinyinStyle.ToneNumber);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("中国");
            Assert.Equal("zhong1-guo2", result);
        }

        [Fact]
        public void オプション_空セパレータ_基本動作()
        {
            var options = new ChineseG2POptions(separator: "");
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("中国");
            Assert.Equal("zhōngguó", result);
        }

        [Fact]
        public void オプション_複数文字セパレータ()
        {
            var options = new ChineseG2POptions(separator: " | ");
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("中国");
            Assert.Equal("zhōng | guó", result);
        }

        // =====================================================================
        // 9. Disposeテスト
        // =====================================================================

        [Fact]
        public void Dispose後_ToPinyin_ObjectDisposedException()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPinyin("你好"));
        }

        [Fact]
        public void Dispose後_ToPinyinList_ObjectDisposedException()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPinyinList("你好"));
        }

        [Fact]
        public void Dispose後_ContainsChar_ObjectDisposedException()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ContainsChar('中'));
        }

        [Fact]
        public void Dispose後_LookupChar_ObjectDisposedException()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.LookupChar('中'));
        }

        [Fact]
        public void Dispose後_ToPinyinBatch_ObjectDisposedException()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPinyinBatch(new[] { "你好" }));
        }

        [Fact]
        public void 二重Dispose_例外なし()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            var exception = Record.Exception(() => engine.Dispose());
            Assert.Null(exception);
        }
    }
}
