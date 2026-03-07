using System;
using System.IO;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// ChineseG2PEngine C2統合テスト。
    /// 非漢字処理・句読点区切り・フレーズフォールバック・CJK拡張領域・LookupChar全候補・
    /// コンストラクタバリエーション・バッチAPI等の C2 で追加された機能を検証する。
    /// </summary>
    public class ChineseG2PEngineC2Tests : IDisposable
    {
        private readonly ChineseG2PEngine _engine;

        public ChineseG2PEngineC2Tests()
        {
            // C2機能テスト: 声調変調を無効にしてフレーズ辞書・非漢字処理のみを検証
            var options = new ChineseG2POptions(enableToneSandhi: false);
            _engine = new ChineseG2PEngine(options);
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =====================================================================
        // 1. 非漢字処理テスト (~15件)
        // =====================================================================

        [Fact]
        public void ToPinyin_ASCII英字のみ_そのまま出力()
        {
            var result = _engine.ToPinyin("ABC");
            Assert.Equal("ABC", result);
        }

        [Fact]
        public void ToPinyin_ASCII数字のみ_そのまま出力()
        {
            var result = _engine.ToPinyin("123");
            Assert.Equal("123", result);
        }

        [Fact]
        public void ToPinyin_英字と漢字混在_英字そのまま漢字ピンイン()
        {
            var result = _engine.ToPinyin("Hello世界");
            Assert.StartsWith("Hello", result);
            Assert.Contains("shì", result);
            Assert.Contains("jiè", result);
        }

        [Fact]
        public void ToPinyin_漢字の後に英字_出力される()
        {
            var result = _engine.ToPinyin("世界Hello");
            Assert.EndsWith("Hello", result);
        }

        [Fact]
        public void ToPinyin_数字混在_2024年()
        {
            var result = _engine.ToPinyin("2024年");
            Assert.StartsWith("2024", result);
            // '年' のピンインが含まれる
            Assert.Contains("nián", result);
        }

        [Fact]
        public void ToPinyin_英語ブランド名混在_iPhone手机()
        {
            var result = _engine.ToPinyin("iPhone手机");
            Assert.StartsWith("iPhone", result);
            Assert.Contains("shǒu", result);
        }

        [Fact]
        public void ToPinyin_プラス記号_IsAsciiPunctuationに含まれないのでそのまま()
        {
            // '+' はIsAsciiPunctuationに含まれない → そのまま出力
            var result = _engine.ToPinyin("A+B");
            Assert.Equal("A+B", result);
        }

        [Fact]
        public void ToPinyin_イコール記号_そのまま出力()
        {
            var result = _engine.ToPinyin("A=B");
            Assert.Equal("A=B", result);
        }

        [Fact]
        public void ToPinyin_アットマーク_そのまま出力()
        {
            var result = _engine.ToPinyin("test@example");
            Assert.Equal("test@example", result);
        }

        [Fact]
        public void ToPinyin_スラッシュ_そのまま出力()
        {
            var result = _engine.ToPinyin("A/B");
            Assert.Equal("A/B", result);
        }

        [Fact]
        public void ToPinyin_ハイフン_そのまま出力()
        {
            var result = _engine.ToPinyin("A-B");
            Assert.Equal("A-B", result);
        }

        [Fact]
        public void ToPinyin_アンダースコア_そのまま出力()
        {
            var result = _engine.ToPinyin("A_B");
            Assert.Equal("A_B", result);
        }

        [Fact]
        public void ToPinyin_漢字間に英字_セパレータリセット()
        {
            // 你(pinyin) + X(ASCII, needsSeparator=false) + 好(pinyin)
            var result = _engine.ToPinyin("你X好");
            Assert.Equal("nǐXhǎo", result);
        }

        [Fact]
        public void ToPinyin_漢字の前後に空白と英字()
        {
            var result = _engine.ToPinyin("Hello你好World");
            Assert.StartsWith("Hello", result);
            Assert.EndsWith("World", result);
            Assert.Contains("nǐ", result);
            Assert.Contains("hǎo", result);
        }

        [Fact]
        public void ToPinyinList_英字と漢字混在_各文字ごと()
        {
            var result = _engine.ToPinyinList("A中B");
            Assert.Equal(3, result.Length);
            Assert.Equal("A", result[0]);
            Assert.Equal("zhōng", result[1]);
            Assert.Equal("B", result[2]);
        }

        [Fact]
        public void ToPinyinList_数字と漢字混在_各文字ごと()
        {
            var result = _engine.ToPinyinList("1中2");
            Assert.Equal(3, result.Length);
            Assert.Equal("1", result[0]);
            Assert.Equal("zhōng", result[1]);
            Assert.Equal("2", result[2]);
        }

        // =====================================================================
        // 2. 句読点処理テスト (~10件)
        // =====================================================================

        [Fact]
        public void ToPinyin_CJKカンマ_区切りとして処理され出力に含まれない()
        {
            var result = _engine.ToPinyin("你好，世界");
            Assert.DoesNotContain("，", result);
            Assert.Contains("nǐ", result);
            Assert.Contains("shì", result);
        }

        [Fact]
        public void ToPinyin_ASCIIカンマ_区切りとして処理()
        {
            var result = _engine.ToPinyin("你好,世界");
            Assert.DoesNotContain(",", result);
        }

        [Fact]
        public void ToPinyin_CJK句点_区切りとして処理()
        {
            var result = _engine.ToPinyin("你好。世界");
            Assert.DoesNotContain("。", result);
        }

        [Fact]
        public void ToPinyin_ASCIIピリオド_区切りとして処理()
        {
            var result = _engine.ToPinyin("你好.世界");
            Assert.DoesNotContain(".", result);
        }

        [Fact]
        public void ToPinyin_CJK感嘆符_区切りとして処理()
        {
            var result = _engine.ToPinyin("你好！");
            Assert.DoesNotContain("！", result);
            Assert.Contains("nǐ", result);
        }

        [Fact]
        public void ToPinyin_CJK疑問符_区切りとして処理()
        {
            var result = _engine.ToPinyin("你好？");
            Assert.DoesNotContain("？", result);
        }

        [Fact]
        public void ToPinyin_CJK全角括弧_区切りとして処理()
        {
            var result = _engine.ToPinyin("（你好）世界");
            Assert.DoesNotContain("（", result);
            Assert.DoesNotContain("）", result);
            Assert.Contains("nǐ", result);
        }

        [Fact]
        public void ToPinyin_CJK鉤括弧_区切りとして処理()
        {
            var result = _engine.ToPinyin("「你好」世界");
            Assert.DoesNotContain("「", result);
            Assert.DoesNotContain("」", result);
        }

        [Fact]
        public void ToPinyin_CJK書名号_区切りとして処理()
        {
            var result = _engine.ToPinyin("《你好》");
            Assert.DoesNotContain("《", result);
            Assert.DoesNotContain("》", result);
        }

        [Fact]
        public void ToPinyin_CJK顿号_区切りとして処理()
        {
            var result = _engine.ToPinyin("中、国");
            Assert.DoesNotContain("、", result);
        }

        [Fact]
        public void ToPinyin_混合句読点_すべて除去()
        {
            var result = _engine.ToPinyin("你好,世界！中国。");
            Assert.DoesNotContain(",", result);
            Assert.DoesNotContain("！", result);
            Assert.DoesNotContain("。", result);
        }

        [Fact]
        public void ToPinyin_句読点後にセパレータなし_二重スペースなし()
        {
            var result = _engine.ToPinyin("你好，世界");
            Assert.DoesNotContain("  ", result);
        }

        // =====================================================================
        // 3. コンストラクタテスト (~8件)
        // =====================================================================

        [Fact]
        public void コンストラクタ_デフォルト_フレーズ辞書も読み込まれる()
        {
            using var engine = new ChineseG2PEngine();
            // デフォルトコンストラクタはフレーズ辞書も読み込む → 多音字解決可能
            var result = engine.ToPinyin("重要");
            Assert.Equal("zhòng yào", result);
        }

        [Fact]
        public void コンストラクタ_オプション付き_フレーズ辞書も読み込まれる()
        {
            var options = new ChineseG2POptions(defaultStyle: PinyinStyle.Normal);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("你好");
            Assert.Equal("ni hao", result);
        }

        [Fact]
        public void コンストラクタ_外部辞書パス_nullパス_ArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new ChineseG2PEngine((string)null!));
        }

        [Fact]
        public void コンストラクタ_外部辞書パス_空文字列_ArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new ChineseG2PEngine(""));
        }

        [Fact]
        public void コンストラクタ_外部辞書パス_存在しないファイル_FileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(
                () => new ChineseG2PEngine("/nonexistent/path/dict.txt"));
        }

        [Fact]
        public void コンストラクタ_外部辞書2パス_charDictNull_ArgumentException()
        {
            Assert.Throws<ArgumentException>(
                () => new ChineseG2PEngine(null!, "phrase.txt"));
        }

        [Fact]
        public void コンストラクタ_外部辞書2パス_charDictNotFound_FileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(
                () => new ChineseG2PEngine("nonexistent_char.txt", "nonexistent_phrase.txt"));
        }

        [Fact]
        public void コンストラクタ_外部辞書3パス_nullCharDict_ArgumentException()
        {
            var options = new ChineseG2POptions();
            Assert.Throws<ArgumentException>(
                () => new ChineseG2PEngine(null!, "phrase.txt", options));
        }

        [Fact]
        public void Dispose_二重呼び出し_例外なし()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            var exception = Record.Exception(() => engine.Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public void Dispose後_ToPinyin_ObjectDisposedException()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPinyin("你好"));
        }

        // =====================================================================
        // 4. CJK拡張領域テスト (~5件)
        // =====================================================================

        [Fact]
        public void ToPinyin_CJK_ExtensionA_U3400_CJK漢字として認識()
        {
            // CJK Extension A (U+3400-U+4DBF) の先頭文字
            var result = _engine.ToPinyin("\u3400");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToPinyin_CJK_ExtensionA_範囲内文字()
        {
            // U+4E00の直前 U+4DBF がExtension A最後
            var c = '\u4DBF';
            var result = _engine.ToPinyin(c.ToString());
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPinyin_CJK_Compatibility_U_F900()
        {
            // CJK Compatibility Ideographs (U+F900-U+FAFF)
            var result = _engine.ToPinyin("\uF900");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToPinyin_CJK_Compatibility_範囲末尾()
        {
            var c = '\uFAFF';
            var result = _engine.ToPinyin(c.ToString());
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPinyin_CJK基本領域と拡張混在()
        {
            // 基本CJK (中) + Extension A 文字
            var result = _engine.ToPinyin("中\u3400");
            Assert.Contains("zhōng", result);
        }

        [Fact]
        public void ToPinyin_ひらがな_CJK漢字ではないのでそのまま()
        {
            // ひらがな (U+3040-U+309F) はCJK統合漢字の範囲外
            var result = _engine.ToPinyin("あ");
            Assert.Equal("あ", result);
        }

        [Fact]
        public void ToPinyin_カタカナ_CJK漢字ではないのでそのまま()
        {
            var result = _engine.ToPinyin("ア");
            Assert.Equal("ア", result);
        }

        // =====================================================================
        // 5. LookupChar 全候補テスト (~5件)
        // =====================================================================

        [Fact]
        public void LookupChar_重_複数読みを返す()
        {
            // '重' は多音字: zhòng / chóng
            var result = _engine.LookupChar('重');
            Assert.NotEmpty(result);
            Assert.True(result.Length >= 2,
                $"'重' は2つ以上の読みが期待されるが {result.Length} 件: [{string.Join(", ", result)}]");
        }

        [Fact]
        public void LookupChar_行_複数読みを返す()
        {
            // '行' は多音字: háng / xíng
            var result = _engine.LookupChar('行');
            Assert.NotEmpty(result);
            Assert.True(result.Length >= 2,
                $"'行' は2つ以上の読みが期待されるが {result.Length} 件: [{string.Join(", ", result)}]");
        }

        [Fact]
        public void LookupChar_了_複数読みを返す()
        {
            // '了' は多音字: le / liǎo
            var result = _engine.LookupChar('了');
            Assert.NotEmpty(result);
            Assert.True(result.Length >= 2,
                $"'了' は2つ以上の読みが期待されるが {result.Length} 件: [{string.Join(", ", result)}]");
        }

        [Fact]
        public void LookupChar_你_単一読み()
        {
            var result = _engine.LookupChar('你');
            Assert.NotEmpty(result);
            Assert.Equal("nǐ", result[0]);
        }

        [Fact]
        public void LookupChar_非漢字_空配列()
        {
            var result = _engine.LookupChar('Z');
            Assert.Empty(result);
        }

        [Fact]
        public void LookupChar_数字_空配列()
        {
            var result = _engine.LookupChar('5');
            Assert.Empty(result);
        }

        // =====================================================================
        // 6. フレーズ→単字フォールバック動作テスト (~7件)
        // =====================================================================

        [Fact]
        public void ToPinyin_フレーズ辞書にあるフレーズ_フレーズ辞書のピンイン使用()
        {
            // "重要" はフレーズ辞書にある → zhòng yào
            var result = _engine.ToPinyin("重要");
            Assert.Equal("zhòng yào", result);
        }

        [Fact]
        public void ToPinyinList_フレーズ辞書にあるフレーズ_正しい要素数()
        {
            var result = _engine.ToPinyinList("重要");
            Assert.Equal(2, result.Length);
            Assert.Equal("zhòng", result[0]);
            Assert.Equal("yào", result[1]);
        }

        [Fact]
        public void ToPinyin_フレーズ辞書にない組合せ_単字辞書最優先読み()
        {
            // 存在しにくい漢字組合せ → 単字辞書の最優先読みが使われる
            var result = _engine.ToPinyin("龙虾");
            Assert.NotEmpty(result);
            // 各文字が個別にピンイン変換される
        }

        [Fact]
        public void ToPinyin_HandleHeteronyms無効_フレーズ辞書不使用()
        {
            var options = new ChineseG2POptions(handleHeteronyms: false);
            using var engine = new ChineseG2PEngine(options);

            // HandleHeteronyms=false → フレーズ辞書ルックアップスキップ、単字辞書のみ
            var resultWithout = engine.ToPinyin("中国");
            Assert.NotEmpty(resultWithout);
        }

        [Fact]
        public void ToPinyin_HandleHeteronyms有効と無効で結果が異なりうる()
        {
            var optionsEnabled = new ChineseG2POptions(handleHeteronyms: true);
            var optionsDisabled = new ChineseG2POptions(handleHeteronyms: false);
            using var engineEnabled = new ChineseG2PEngine(optionsEnabled);
            using var engineDisabled = new ChineseG2PEngine(optionsDisabled);

            // 多音字を含むフレーズでは結果が異なりうる
            var resultEnabled = engineEnabled.ToPinyin("重要");
            var resultDisabled = engineDisabled.ToPinyin("重要");

            // フレーズ辞書有効時は "zhòng yào" が期待される
            Assert.Equal("zhòng yào", resultEnabled);
            // 無効時は単字辞書の最優先読みが使われる（異なる可能性あり）
            Assert.NotEmpty(resultDisabled);
        }

        [Fact]
        public void ToPinyin_長い文_フレーズと単字の混在処理()
        {
            var result = _engine.ToPinyin("今天天气很好我们一起去公园");
            Assert.NotEmpty(result);
            // 複数のピンイン音節がスペース区切りで出力される
            var parts = result.Split(' ');
            Assert.True(parts.Length >= 5,
                $"長い文のピンイン音節数が不足: {parts.Length}, 結果: {result}");
        }

        [Fact]
        public void ToPinyin_フレーズ最長一致_中国人()
        {
            // "中国人" でフレーズ辞書の最長一致検索が動作する
            var result = _engine.ToPinyin("中国人");
            Assert.Contains("zhōng", result);
            Assert.Contains("guó", result);
            Assert.Contains("rén", result);
        }

        [Fact]
        public void ToPinyin_句読点によるフレーズ分断()
        {
            // 句読点がフレーズマッチを分断する
            // "中，国" は "中国" としてフレーズマッチしない
            var withPunct = _engine.ToPinyin("中，国");
            var withoutPunct = _engine.ToPinyin("中国");
            // 句読点ありの場合はセパレータ動作が異なるため、結果が異なる
            Assert.NotEqual(withPunct, withoutPunct);
        }

        [Fact]
        public void ToPinyin_単字辞書にない文字_文字そのまま出力()
        {
            // CJK範囲内だが辞書未登録の文字はそのまま出力
            // U+9FFF (CJK Unified Ideographs末尾付近、辞書に未登録の可能性)
            var c = '\u9FFF';
            var result = _engine.ToPinyin(c.ToString());
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 7. バッチAPIテスト (~5件)
        // =====================================================================

        [Fact]
        public void ToPinyinBatch_非漢字混在テキスト_正しく変換()
        {
            var result = _engine.ToPinyinBatch(new[] { "Hello你好", "123世界" });
            Assert.Equal(2, result.Count);
            Assert.Contains("nǐ", result[0]);
            Assert.Contains("shì", result[1]);
        }

        [Fact]
        public void ToPinyinBatch_句読点含むテキスト_句読点除去()
        {
            var result = _engine.ToPinyinBatch(new[] { "你好，世界", "中国！" });
            Assert.Equal(2, result.Count);
            Assert.DoesNotContain("，", result[0]);
            Assert.DoesNotContain("！", result[1]);
        }

        [Fact]
        public void ToPinyinBatch_フレーズ辞書対象テキスト_フレーズ辞書使用()
        {
            var result = _engine.ToPinyinBatch(new[] { "重要", "中国" });
            Assert.Equal(2, result.Count);
            Assert.Contains("zhòng", result[0]);
            Assert.Contains("zhōng", result[1]);
        }

        [Fact]
        public void ToPinyinBatch_空文字列含む_正しく処理()
        {
            var result = _engine.ToPinyinBatch(new[] { "", "你好", "" });
            Assert.Equal(3, result.Count);
            Assert.Equal("", result[0]);
            Assert.Equal("nǐ hǎo", result[1]);
            Assert.Equal("", result[2]);
        }

        [Fact]
        public void ToPinyinBatch_大量テキスト_正常完了()
        {
            var texts = new string[100];
            for (var i = 0; i < 100; i++)
                texts[i] = "你好世界";

            var result = _engine.ToPinyinBatch(texts);
            Assert.Equal(100, result.Count);
            foreach (var r in result)
                Assert.Equal("nǐ hǎo shì jiè", r);
        }

        // =====================================================================
        // 補足: スペース・改行処理テスト
        // =====================================================================

        [Fact]
        public void ToPinyin_スペース_区切りとして処理()
        {
            var result = _engine.ToPinyin("你好 世界");
            Assert.DoesNotContain("  ", result);
        }

        [Fact]
        public void ToPinyin_タブ_区切りとして処理()
        {
            var result = _engine.ToPinyin("你好\t世界");
            Assert.DoesNotContain("\t", result);
        }

        [Fact]
        public void ToPinyin_改行_区切りとして処理()
        {
            var result = _engine.ToPinyin("你好\n世界");
            Assert.DoesNotContain("\n", result);
        }

        [Fact]
        public void ToPinyin_先頭スペース_無視()
        {
            var result = _engine.ToPinyin(" 你好");
            Assert.Equal("nǐ hǎo", result);
        }

        [Fact]
        public void ToPinyin_末尾スペース_無視()
        {
            var result = _engine.ToPinyin("你好 ");
            Assert.Equal("nǐ hǎo", result);
        }

        // =====================================================================
        // 補足: スタイル変換 + C2機能の組み合わせテスト
        // =====================================================================

        [Fact]
        public void ToPinyin_句読点混在_ToneNumber()
        {
            var result = _engine.ToPinyin("你好，世界", PinyinStyle.ToneNumber);
            Assert.DoesNotContain("，", result);
            Assert.Contains("ni3", result);
            Assert.Contains("shi4", result);
        }

        [Fact]
        public void ToPinyin_句読点混在_Normal()
        {
            var result = _engine.ToPinyin("你好，世界", PinyinStyle.Normal);
            Assert.DoesNotContain("，", result);
            Assert.Contains("ni", result);
            Assert.Contains("shi", result);
        }

        [Fact]
        public void ToPinyin_カスタムセパレータ_句読点区切り()
        {
            var options = new ChineseG2POptions(separator: "-");
            using var engine = new ChineseG2PEngine(options);

            var result = engine.ToPinyin("你好，世界");
            Assert.DoesNotContain("，", result);
            Assert.Contains("-", result);
        }

        [Fact]
        public void ToPinyin_セパレータ空文字列_音節連結()
        {
            var options = new ChineseG2POptions(separator: "", enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);

            var result = engine.ToPinyin("你好");
            Assert.Equal("nǐhǎo", result);
        }

        // =====================================================================
        // 補足: エッジケーステスト
        // =====================================================================

        [Fact]
        public void ToPinyin_全句読点のみ_空文字列()
        {
            var result = _engine.ToPinyin("，。！？");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPinyin_ASCII句読点のみ_空文字列()
        {
            var result = _engine.ToPinyin(",.!?;:");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPinyin_連続句読点_正常処理()
        {
            var result = _engine.ToPinyin("你好，，世界");
            Assert.DoesNotContain("，", result);
        }

        [Fact]
        public void ToPinyinList_句読点混在_句読点含む配列()
        {
            // ToPinyinList は句読点もそのまま1文字として配列に含める
            var result = _engine.ToPinyinList("你好，世界");
            Assert.True(result.Length >= 4,
                $"ToPinyinListの結果要素数が不足: {result.Length}");
        }
    }
}
