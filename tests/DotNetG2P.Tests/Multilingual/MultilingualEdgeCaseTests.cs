using System;
using System.IO;
using DotNetG2P.Multilingual;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// 日英混在テキストのエッジケーステスト。
    /// MultilingualG2PEngineの堅牢性を検証する（辞書依存）。
    /// </summary>
    public class MultilingualEdgeCaseTests : IDisposable
    {
        private readonly MultilingualG2PEngine? _engine;
        private readonly bool _hasDictionary;

        private static string? FindDictPath()
        {
            var envPath = Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
                return envPath;
            var candidates = new[]
            {
                @"C:\naist-jdic",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "naist-jdic"),
                "/usr/local/share/naist-jdic",
                "/usr/share/naist-jdic",
            };
            foreach (var path in candidates)
                if (Directory.Exists(path))
                    return path;
            return null;
        }

        public MultilingualEdgeCaseTests()
        {
            var dictPath = FindDictPath();
            _hasDictionary = dictPath != null;
            if (_hasDictionary)
                _engine = new MultilingualG2PEngine(dictPath!);
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!_hasDictionary, "naist-jdic辞書が見つかりません（環境変数 NAIST_JDIC_PATH を設定してください）");
        }

        // =====================================================================
        // 1. 全角英数字混在
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_全角英数字混在_変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("Ｈｅｌｌｏ世界");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 2. 半角カナ混在
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_半角カナ混在_変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("ｱｲｳhello");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 3. 長文混在
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_長文混在_エラーなく変換される()
        {
            SkipIfNoDictionary();

            var longText = "今日はとても天気が良いですね。I went to the park and enjoyed the sunshine. " +
                           "桜がきれいに咲いていました。The cherry blossoms were beautiful. " +
                           "来年もまた行きたいです。I hope to visit again next year.";

            var result = _engine!.ToPhonemes(longText);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 4. 繰り返し変換（冪等性）
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_繰り返し変換_同じ結果()
        {
            SkipIfNoDictionary();

            var input = "Hello世界";
            var result1 = _engine!.ToPhonemes(input);
            var result2 = _engine.ToPhonemes(input);
            var result3 = _engine.ToPhonemes(input);

            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
        }

        // =====================================================================
        // 5. 絵文字含み
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_絵文字含み_エラーなく変換される()
        {
            SkipIfNoDictionary();

            // 絵文字はサロゲートペアだがクラッシュしないこと
            var result = _engine!.ToPhonemes("hello\U0001F600世界");

            Assert.NotNull(result);
        }

        // =====================================================================
        // 6. 改行含み
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_改行含み_エラーなく変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("hello\n世界");

            Assert.NotNull(result);
        }

        // =====================================================================
        // 7. タブ含み
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_タブ含み_エラーなく変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("hello\t世界");

            Assert.NotNull(result);
        }

        // =====================================================================
        // 8. 日本語のみ長文
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_日本語のみ長文_正常変換()
        {
            SkipIfNoDictionary();

            var longJapanese = "今日は天気がとても良いです。明日も晴れるといいですね。" +
                               "東京タワーに行って景色を見ました。富士山がきれいに見えました。";

            var result = _engine!.ToPhonemes(longJapanese);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 9. 英語のみ長文
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_英語のみ長文_正常変換()
        {
            SkipIfNoDictionary();

            var longEnglish = "The quick brown fox jumps over the lazy dog. " +
                              "This is a simple sentence for testing purposes. " +
                              "Natural language processing is an important field of study.";

            var result = _engine!.ToPhonemes(longEnglish);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 10. 連続言語切替
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_連続言語切替_正常変換()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("aあbいcう");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 11. 数字のみ
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_数字のみ_日本語デフォルトとして変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("12345");

            Assert.NotNull(result);
            // 数字のみの場合、デフォルトで日本語として処理される
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 12. 記号のみ
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_記号のみ_エラーなく変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("!@#");

            Assert.NotNull(result);
            // 記号のみでもクラッシュしないこと（空文字列でもOK）
        }

        // =====================================================================
        // 13. アポストロフィ含み
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_アポストロフィ含み_正常変換()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("I don't like 寿司");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 14. ハイフン含み
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_ハイフン含み英語_正常変換()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("well-known");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 15. 日本語カタカナ語と英語
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_カタカナ語と英語_変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("コンピューター is computer");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 16. 漢字のみ
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_漢字のみ_変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("東京大阪名古屋");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 17. ひらがなのみ
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_ひらがなのみ_変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("あいうえお");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 18. カタカナのみ
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_カタカナのみ_変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("カタカナ");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 19. 英語大文字
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_英語大文字_変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("HELLO WORLD");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 20. URL風テキスト
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_URL風テキスト_エラーなく変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("www.example.com");

            Assert.NotNull(result);
        }

        // =====================================================================
        // 21. 連続空白混在
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_連続空白混在_正常変換()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("hello   world   こんにちは");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 22. 先頭が記号
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_先頭が記号_エラーなく変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("!hello世界");

            Assert.NotNull(result);
        }

        // =====================================================================
        // 23. 末尾が記号
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_末尾が記号_エラーなく変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("hello世界!");

            Assert.NotNull(result);
        }

        // =====================================================================
        // 24. 日英英日交互
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_日英英日交互_正常変換()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("あaいbうc");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 25. 全角記号含み
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_全角記号含み_変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("Ｈｅｌｌｏ！！");

            Assert.NotNull(result);
        }
    }
}
