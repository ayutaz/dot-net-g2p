using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DotNetG2P;
using DotNetG2P.Models;
using DotNetG2P.MeCab;
using Xunit;

namespace DotNetG2P.Tests.Integration
{
    /// <summary>
    /// 各種エッジケースの網羅テスト。
    /// 主目的は「クラッシュしないこと」の検証（堅牢性テスト）。
    /// 辞書が存在しない環境ではスキップされる。
    /// </summary>
    public abstract class EdgeCaseTestsBase : IDisposable
    {
        private static string? DicPath => Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
        private static bool DictionaryExists => !string.IsNullOrEmpty(DicPath) && Directory.Exists(DicPath);

        private readonly ITokenizer? _tokenizer;
        protected readonly G2PEngine? _engine;

        protected abstract ITokenizer CreateTokenizer(string dicPath);

        protected EdgeCaseTestsBase()
        {
            if (DictionaryExists)
            {
                _tokenizer = CreateTokenizer(DicPath!);
                _engine = new G2PEngine(_tokenizer);
            }
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!DictionaryExists, "naist-jdic辞書が見つかりません（環境変数 NAIST_JDIC_PATH を設定してください）");
        }

        /// <summary>
        /// 音素文字列がスペース区切りの有効な音素列であることを検証する。
        /// 空でない場合、日本語音素（a-z, A-Z, -, cl, N等）のスペース区切りパターンに合致するか。
        /// </summary>
        private static void AssertValidPhonemeString(string phonemes)
        {
            if (string.IsNullOrEmpty(phonemes)) return;
            // 音素列は英字(大小)、ハイフン、スペースのみで構成される
            Assert.Matches(@"^[a-zA-Z\- ]+$", phonemes);
            // スペース区切りの各トークンが空でない
            var tokens = phonemes.Split(' ');
            foreach (var t in tokens)
                Assert.NotEmpty(t);
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
            // 記号のみの場合、空文字列か有効な音素列
            AssertValidPhonemeString(result);
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
            AssertValidPhonemeString(result);
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
            AssertValidPhonemeString(result);
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
            // 日本語部分が含まれているため、何らかの音素が出力されるはず
            Assert.NotEmpty(result);
            AssertValidPhonemeString(result);
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
            Assert.NotEmpty(result);
            AssertValidPhonemeString(result);
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
            AssertValidPhonemeString(result);
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
            AssertValidPhonemeString(result);
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
            Assert.NotEmpty(result);
            AssertValidPhonemeString(result);
            // 代表入力は複数音素を含むはず
            Assert.Contains(" ", result);
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
            Assert.NotEmpty(result);
            // カタカナ文字列であることを検証
            Assert.Matches(@"^[\u30A0-\u30FF\u30FC]+$", result);
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
            Assert.NotEmpty(result);
            // 韻律記号付き出力は ^ で始まり $ で終わる
            Assert.StartsWith("^", result);
            Assert.EndsWith("$", result);
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
            Assert.NotEmpty(result);
            // 少なくとも1つのアクセント句があり、モーラを持つ
            Assert.True(result[0].Moras.Count > 0);
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
            Assert.NotEmpty(result);
            // 先頭ラベルはsilを含む
            Assert.Contains("sil", result[0]);
            // 末尾ラベルもsilを含む
            Assert.Contains("sil", result[result.Count - 1]);
            // Kフィールドが存在する
            Assert.Contains("/K:", result[0]);
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
            Assert.NotEmpty(result);
            // 少なくとも1つのNjdNodeが発音を持つ
            Assert.True(result.Any(n => n.Pronunciation != null && n.Pronunciation.MoraCount > 0));
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

            // ToPhonemes: 結果が返りnull以外であること
            var phonemes = _engine!.ToPhonemes(input);
            Assert.NotNull(phonemes);
            // 音素文字列が有効な形式であること（空も許容）
            AssertValidPhonemeString(phonemes);

            // ToKana: 結果がnull以外であること
            var kana = _engine.ToKana(input);
            Assert.NotNull(kana);

            // ToProsody: 結果がnull以外であること。空でなければ ^...$ 形式
            var prosody = _engine.ToProsody(input);
            Assert.NotNull(prosody);
            if (!string.IsNullOrEmpty(prosody))
            {
                Assert.StartsWith("^", prosody);
                Assert.EndsWith("$", prosody);
            }

            // ToAccentPhrases: リストが返ること
            var accentPhrases = _engine.ToAccentPhrases(input);
            Assert.NotNull(accentPhrases);

            // ToFullContextLabels: リストが返ること
            var labels = _engine.ToFullContextLabels(input);
            Assert.NotNull(labels);
            // ラベルがあれば先頭/末尾はsil
            if (labels.Count > 0)
            {
                Assert.Contains("sil", labels[0]);
                Assert.Contains("sil", labels[labels.Count - 1]);
            }

            // Analyze: リストが返ること
            var nodes = _engine.Analyze(input);
            Assert.NotNull(nodes);
        }
    }

    /// <summary>MeCabTokenizerによるエッジケーステスト。</summary>
    public class EdgeCaseTests_MeCab : EdgeCaseTestsBase
    {
        protected override ITokenizer CreateTokenizer(string dicPath) => new MeCabTokenizer(dicPath);
    }
}
