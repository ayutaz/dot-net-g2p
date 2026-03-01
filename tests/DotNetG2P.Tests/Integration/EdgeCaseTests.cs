using System;
using System.IO;
using System.Linq;
using DotNetG2P;
using DotNetG2P.Models;
using DotNetG2P.NMeCab;
using Xunit;

namespace DotNetG2P.Tests.Integration
{
    /// <summary>
    /// 各種エッジケースの網羅テスト。
    /// 主目的は「クラッシュしないこと」の検証（堅牢性テスト）。
    /// 辞書が存在しない環境ではスキップされる。
    /// </summary>
    public class EdgeCaseTests : IDisposable
    {
        private const string DictionaryPath = "C:/Users/yuta/Desktop/Private/piper-plus/src/wasm/openjtalk-web/assets/dict/";

        private static readonly bool DictionaryExists =
            Directory.Exists(DictionaryPath) && File.Exists(Path.Combine(DictionaryPath, "sys.dic"));

        private readonly NMeCabTokenizer? _tokenizer;
        private readonly G2PEngine? _engine;

        public EdgeCaseTests()
        {
            if (DictionaryExists)
            {
                _tokenizer = new NMeCabTokenizer(DictionaryPath);
                _engine = new G2PEngine(_tokenizer);
            }
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!DictionaryExists, "naist-jdic辞書が見つかりません: " + DictionaryPath);
        }

        // =====================================================================
        // 1. 記号のみ
        // =====================================================================

        [SkippableTheory]
        [InlineData("。、！？「」（）【】")]
        [InlineData("...")]
        [InlineData("〜")]
        [InlineData("・")]
        [InlineData("―")]
        public void ToPhonemes_記号のみ_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
        }

        // =====================================================================
        // 2. 英字
        // =====================================================================

        [SkippableTheory]
        [InlineData("ABC")]
        [InlineData("hello")]
        [InlineData("AI")]
        [InlineData("x86")]
        [InlineData("MP3")]
        public void ToPhonemes_英字_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
        }

        // =====================================================================
        // 3. 空白系
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_空文字列_空を返す()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("");

            Assert.Equal("", result);
        }

        [SkippableFact]
        public void ToKana_空文字列_空を返す()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("");

            Assert.Equal("", result);
        }

        [SkippableFact]
        public void ToProsody_空文字列_空を返す()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToProsody("");

            Assert.Equal("", result);
        }

        [SkippableFact]
        public void ToPhonemes_null_空を返す()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(null!);

            Assert.Equal("", result);
        }

        [SkippableFact]
        public void ToKana_null_空を返す()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana(null!);

            Assert.Equal("", result);
        }

        [SkippableFact]
        public void ToProsody_null_空を返す()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToProsody(null!);

            Assert.Equal("", result);
        }

        [SkippableTheory]
        [InlineData(" ")]
        [InlineData("\u3000")]
        [InlineData("\t")]
        [InlineData("\n")]
        public void ToPhonemes_空白文字_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
        }

        // =====================================================================
        // 4. 長文
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_長文_クラッシュしない_結果が空でない()
        {
            SkipIfNoDictionary();

            // 100文字以上のテキスト
            var longText = "今日は天気がとても良いです。明日も晴れるといいですね。東京タワーに行って景色を見ました。富士山がきれいに見えました。日本語の音声合成技術はとても進歩しています。人工知能の発展により様々な分野で革新が起きています。";
            Assert.True(longText.Length >= 100, $"テスト用テキストが100文字未満です: {longText.Length}文字");

            var result = _engine!.ToPhonemes(longText);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 5. 混在スクリプト
        // =====================================================================

        [SkippableTheory]
        [InlineData("今日はDocker入門")]
        [InlineData("Python3.12をインストール")]
        [InlineData("AIとは何か？")]
        public void ToPhonemes_混在スクリプト_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
        }

        // =====================================================================
        // 6. 繰り返し
        // =====================================================================

        [SkippableTheory]
        [InlineData("ああああああああああ")]
        [InlineData("アアアアアアアアアア")]
        public void ToPhonemes_繰り返し_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
        }

        // =====================================================================
        // 7. 句読点パターン
        // =====================================================================

        [SkippableTheory]
        [InlineData("あ。い。う。")]
        [InlineData("あ、い、う")]
        [InlineData("。")]
        public void ToPhonemes_句読点パターン_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
        }

        // =====================================================================
        // 8. 単一文字
        // =====================================================================

        [SkippableTheory]
        [InlineData("あ")]
        [InlineData("ア")]
        [InlineData("漢")]
        [InlineData("1")]
        [InlineData("A")]
        public void ToPhonemes_単一文字_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
        }

        // =====================================================================
        // 9. 全API共通テスト
        // =====================================================================

        [SkippableTheory]
        [InlineData("こんにちは")]
        [InlineData("東京タワー")]
        [InlineData("音声合成")]
        public void ToPhonemes_代表入力_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
        }

        [SkippableTheory]
        [InlineData("こんにちは")]
        [InlineData("東京タワー")]
        [InlineData("音声合成")]
        public void ToKana_代表入力_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana(input);

            Assert.NotNull(result);
        }

        [SkippableTheory]
        [InlineData("こんにちは")]
        [InlineData("東京タワー")]
        [InlineData("音声合成")]
        public void ToProsody_代表入力_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToProsody(input);

            Assert.NotNull(result);
        }

        [SkippableTheory]
        [InlineData("こんにちは")]
        [InlineData("東京タワー")]
        [InlineData("音声合成")]
        public void ToAccentPhrases_代表入力_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToAccentPhrases(input);

            Assert.NotNull(result);
        }

        [SkippableTheory]
        [InlineData("こんにちは")]
        [InlineData("東京タワー")]
        [InlineData("音声合成")]
        public void ToFullContextLabels_代表入力_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToFullContextLabels(input);

            Assert.NotNull(result);
        }

        [SkippableTheory]
        [InlineData("こんにちは")]
        [InlineData("東京タワー")]
        [InlineData("音声合成")]
        public void Analyze_代表入力_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.Analyze(input);

            Assert.NotNull(result);
        }

        // =====================================================================
        // 10. 全API × エッジケース入力
        // =====================================================================

        [SkippableTheory]
        [InlineData("。、！？")]
        [InlineData("ABC")]
        [InlineData(" ")]
        [InlineData("あ")]
        [InlineData("ああああああああああ")]
        public void 全API_エッジケース入力_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            // ToPhonemes
            var phonemes = _engine!.ToPhonemes(input);
            Assert.NotNull(phonemes);

            // ToKana
            var kana = _engine.ToKana(input);
            Assert.NotNull(kana);

            // ToProsody
            var prosody = _engine.ToProsody(input);
            Assert.NotNull(prosody);

            // ToAccentPhrases
            var accentPhrases = _engine.ToAccentPhrases(input);
            Assert.NotNull(accentPhrases);

            // ToFullContextLabels
            var labels = _engine.ToFullContextLabels(input);
            Assert.NotNull(labels);

            // Analyze
            var nodes = _engine.Analyze(input);
            Assert.NotNull(nodes);
        }
    }
}
