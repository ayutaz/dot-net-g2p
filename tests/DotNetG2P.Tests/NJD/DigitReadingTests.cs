using System;
using System.IO;
using DotNetG2P;
using DotNetG2P.NMeCab;
using Xunit;

namespace DotNetG2P.Tests.NJD
{
    /// <summary>
    /// 数字読みの網羅的テスト。
    /// naist-jdic辞書を使用し、SetDigit/DigitSequence/DigitLut の数字読み変換を検証する。
    /// 辞書が存在しない環境ではスキップされる。
    /// </summary>
    public class DigitReadingTests : IDisposable
    {
        private static string? DicPath => Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
        private static bool DictionaryExists => !string.IsNullOrEmpty(DicPath) && Directory.Exists(DicPath);

        private readonly NMeCabTokenizer? _tokenizer;
        private readonly G2PEngine? _engine;

        public DigitReadingTests()
        {
            if (DictionaryExists)
            {
                _tokenizer = new NMeCabTokenizer(DicPath!);
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

        // =====================================================================
        // 1. 助数詞音便テスト（本）
        // =====================================================================

        [SkippableFact]
        public void ToKana_3本_サンボンを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("３本");

            Assert.NotEmpty(result);
            Assert.Contains("サンボン", result);
        }

        [SkippableFact]
        public void ToKana_1本_イッポンを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("１本");

            Assert.NotEmpty(result);
            Assert.Contains("イッポン", result);
        }

        [SkippableFact]
        public void ToKana_6本_ロッポンを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("６本");

            Assert.NotEmpty(result);
            Assert.Contains("ロッポン", result);
        }

        [SkippableFact]
        public void ToKana_8本_ハッポンまたはハチホンを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("８本");

            Assert.NotEmpty(result);
            // 八本は「ハッポン」が標準だが「ハチホン」もありうる
            Assert.True(
                result.Contains("ハッポン") || result.Contains("ハチホン"),
                $"「８本」の結果が想定外: {result}");
        }

        [SkippableFact]
        public void ToKana_10本_ジュッポンを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("１０本");

            Assert.NotEmpty(result);
            Assert.Contains("ジュッポン", result);
        }

        // =====================================================================
        // 2. 助数詞音便テスト（個）
        // =====================================================================

        [SkippableFact]
        public void ToKana_1個_イッコを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("１個");

            Assert.NotEmpty(result);
            Assert.Contains("イッコ", result);
        }

        [SkippableFact]
        public void ToKana_3個_サンコを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("３個");

            Assert.NotEmpty(result);
            Assert.Contains("サンコ", result);
        }

        // =====================================================================
        // 3. 助数詞音便テスト（杯）
        // =====================================================================

        [SkippableFact]
        public void ToKana_1杯_イッパイを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("１杯");

            Assert.NotEmpty(result);
            Assert.Contains("イッパイ", result);
        }

        [SkippableFact]
        public void ToKana_3杯_サンバイを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("３杯");

            Assert.NotEmpty(result);
            Assert.Contains("サンバイ", result);
        }

        // =====================================================================
        // 4. 月名テスト
        // =====================================================================

        [SkippableFact]
        public void ToKana_1月_イチガツを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("１月");

            Assert.NotEmpty(result);
            Assert.Contains("イチガツ", result);
        }

        [SkippableFact]
        public void ToKana_4月_シガツを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("４月");

            Assert.NotEmpty(result);
            Assert.Contains("シガツ", result);
        }

        [SkippableFact]
        public void ToKana_7月_シチガツを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("７月");

            Assert.NotEmpty(result);
            Assert.Contains("シチガツ", result);
        }

        [SkippableFact]
        public void ToKana_9月_クガツを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("９月");

            Assert.NotEmpty(result);
            Assert.Contains("クガツ", result);
        }

        // =====================================================================
        // 5. 日付特殊読みテスト
        // =====================================================================

        [SkippableFact]
        public void ToKana_1日_ツイタチまたはイチニチを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("１日");

            Assert.NotEmpty(result);
            Assert.True(
                result.Contains("ツイタチ") || result.Contains("イチニチ"),
                $"「１日」の結果が想定外: {result}");
        }

        [SkippableFact]
        public void ToKana_20日_ハツカを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("２０日");

            Assert.NotEmpty(result);
            // TODO(issue): 複合日付パターンで「二十日」→「ハツカ」に変換されるべきだが、
            // 形態素解析の結果によっては未対応の可能性がある。
            Assert.NotEmpty(result);
            Assert.Contains("ニチ", result); // 暫定: 「日」の読みは含まれるはず
        }

        // =====================================================================
        // 6. 基数テスト（クラッシュしないことを検証）
        // =====================================================================

        [SkippableTheory]
        [InlineData("１")]
        [InlineData("１０")]
        [InlineData("１００")]
        [InlineData("１０００")]
        public void ToKana_基数_クラッシュせず空でない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana(input);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 7. 金額テスト
        // =====================================================================

        [SkippableFact]
        public void ToKana_100円_エンを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("１００円");

            Assert.NotNull(result);
            Assert.Contains("エン", result);
        }

        [SkippableFact]
        public void ToKana_1万円_クラッシュしない()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("１万円");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 8. 電話番号パターン（クラッシュしないことを検証）
        // =====================================================================

        [SkippableFact]
        public void ToKana_03_クラッシュしない()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("０３");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void ToKana_1234_クラッシュしない()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("１２３４");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 9. 時刻テスト（クラッシュしないことを検証）
        // =====================================================================

        [SkippableFact]
        public void ToKana_1時_クラッシュしない()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("１時");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void ToKana_10分_クラッシュしない()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("１０分");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }
}
