using System;
using DotNetG2P.Chinese;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// ChineseG2PEngine の統合テスト。
    /// 埋め込み辞書を使用したピンイン変換の基本動作・スタイル変換・エッジケース・Disposeパターンを検証する。
    /// </summary>
    public class ChineseG2PEngineTests : IDisposable
    {
        private readonly ChineseG2PEngine _engine;

        public ChineseG2PEngineTests()
        {
            // C1基本動作テスト: 声調変調を無効にして基本変換のみを検証
            var options = new ChineseG2POptions(enableToneSandhi: false);
            _engine = new ChineseG2PEngine(options);
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =====================================================================
        // 基本変換テスト（デフォルト: ToneMarked）
        // =====================================================================

        [Fact]
        public void ToPinyin_你好_声調記号付き()
        {
            var result = _engine.ToPinyin("你好");
            Assert.Equal("nǐ hǎo", result);
        }

        [Fact]
        public void ToPinyin_中国_声調記号付き()
        {
            var result = _engine.ToPinyin("中国");
            Assert.Equal("zhōng guó", result);
        }

        [Fact]
        public void ToPinyin_世界_声調記号付き()
        {
            var result = _engine.ToPinyin("世界");
            Assert.Equal("shì jiè", result);
        }

        [Fact]
        public void ToPinyin_你好世界_声調記号付き()
        {
            var result = _engine.ToPinyin("你好世界");
            Assert.Equal("nǐ hǎo shì jiè", result);
        }

        [Fact]
        public void ToPinyin_一二三_声調記号付き()
        {
            var result = _engine.ToPinyin("一二三");
            Assert.Equal("yī èr sān", result);
        }

        // =====================================================================
        // スタイル変換テスト
        // =====================================================================

        [Fact]
        public void ToPinyin_中国_ToneNumber()
        {
            var result = _engine.ToPinyin("中国", PinyinStyle.ToneNumber);
            Assert.Equal("zhong1 guo2", result);
        }

        [Fact]
        public void ToPinyin_中国_Normal()
        {
            var result = _engine.ToPinyin("中国", PinyinStyle.Normal);
            Assert.Equal("zhong guo", result);
        }

        [Fact]
        public void ToPinyin_你好_ToneNumber()
        {
            var result = _engine.ToPinyin("你好", PinyinStyle.ToneNumber);
            Assert.Equal("ni3 hao3", result);
        }

        [Fact]
        public void ToPinyin_你好_Normal()
        {
            var result = _engine.ToPinyin("你好", PinyinStyle.Normal);
            Assert.Equal("ni hao", result);
        }

        // =====================================================================
        // ToPinyinList テスト
        // =====================================================================

        [Fact]
        public void ToPinyinList_你好_2要素()
        {
            var result = _engine.ToPinyinList("你好");
            Assert.Equal(new[] { "nǐ", "hǎo" }, result);
        }

        [Fact]
        public void ToPinyinList_中国_2要素()
        {
            var result = _engine.ToPinyinList("中国");
            Assert.Equal(new[] { "zhōng", "guó" }, result);
        }

        [Fact]
        public void ToPinyinList_ToneNumber()
        {
            var result = _engine.ToPinyinList("中国", PinyinStyle.ToneNumber);
            Assert.Equal(new[] { "zhong1", "guo2" }, result);
        }

        [Fact]
        public void ToPinyinList_空文字列_空配列()
        {
            var result = _engine.ToPinyinList("");
            Assert.Empty(result);
        }

        // =====================================================================
        // 非漢字混在テスト
        // =====================================================================

        [Fact]
        public void ToPinyin_非漢字はそのまま出力()
        {
            // 非漢字の英字はそのままappendされ、needsSeparator=falseになるため
            // 英字と漢字ピンインの間にセパレータは入らない
            var result = _engine.ToPinyin("Hello世界");
            Assert.Equal("Helloshì jiè", result);
        }

        [Fact]
        public void ToPinyin_数字のみ_そのまま()
        {
            var result = _engine.ToPinyin("123");
            Assert.Equal("123", result);
        }

        [Fact]
        public void ToPinyin_空文字列_空文字列()
        {
            var result = _engine.ToPinyin("");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPinyin_null_空文字列()
        {
            // 実装: string.IsNullOrEmpty(text) で空文字列を返す
            var result = _engine.ToPinyin(null!);
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPinyinList_非漢字混在_各文字ごとの配列()
        {
            // "AB中" → ["A", "B", "zhōng"]
            var result = _engine.ToPinyinList("AB中");
            Assert.Equal(3, result.Length);
            Assert.Equal("A", result[0]);
            Assert.Equal("B", result[1]);
            Assert.Equal("zhōng", result[2]);
        }

        // =====================================================================
        // ContainsChar / LookupChar テスト
        // =====================================================================

        [Fact]
        public void ContainsChar_漢字_true()
        {
            Assert.True(_engine.ContainsChar('中'));
        }

        [Fact]
        public void ContainsChar_非漢字_false()
        {
            Assert.False(_engine.ContainsChar('A'));
        }

        [Fact]
        public void ContainsChar_你_true()
        {
            Assert.True(_engine.ContainsChar('你'));
        }

        [Fact]
        public void LookupChar_中_最優先ピンインを返す()
        {
            var result = _engine.LookupChar('中');
            Assert.NotEmpty(result);
            Assert.Equal("zhōng", result[0]);
        }

        [Fact]
        public void LookupChar_非漢字_空配列()
        {
            var result = _engine.LookupChar('A');
            Assert.Empty(result);
        }

        // =====================================================================
        // バッチAPI テスト
        // =====================================================================

        [Fact]
        public void ToPinyinBatch_複数テキスト_各テキスト変換()
        {
            var result = _engine.ToPinyinBatch(new[] { "你好", "世界" });
            Assert.Equal(2, result.Length);
            Assert.Equal("nǐ hǎo", result[0]);
            Assert.Equal("shì jiè", result[1]);
        }

        [Fact]
        public void ToPinyinBatch_空配列_空配列()
        {
            var result = _engine.ToPinyinBatch(Array.Empty<string>());
            Assert.Empty(result);
        }

        [Fact]
        public void ToPinyinBatch_null_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToPinyinBatch(null!));
        }

        // =====================================================================
        // コンストラクタテスト
        // =====================================================================

        [Fact]
        public void コンストラクタ_デフォルト_正常動作()
        {
            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("你好");
            Assert.Equal("nǐ hǎo", result);
        }

        [Fact]
        public void コンストラクタ_オプション指定_スタイル変更()
        {
            var options = new ChineseG2POptions(defaultStyle: PinyinStyle.ToneNumber, enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("你好");
            Assert.Equal("ni3 hao3", result);
        }

        [Fact]
        public void コンストラクタ_カスタムセパレータ()
        {
            var options = new ChineseG2POptions(separator: "-", enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("你好");
            Assert.Equal("nǐ-hǎo", result);
        }

        // =====================================================================
        // Dispose テスト
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

            Assert.Throws<ObjectDisposedException>(() =>
                engine.ToPinyinBatch(new[] { "你好" }));
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
