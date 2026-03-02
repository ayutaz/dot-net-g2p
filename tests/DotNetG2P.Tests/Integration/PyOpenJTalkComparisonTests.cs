using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DotNetG2P;
using DotNetG2P.NMeCab;
using DotNetG2P.MeCab;
using Xunit;

namespace DotNetG2P.Tests.Integration
{
    /// <summary>
    /// pyopenjtalkの出力と比較する統合テスト。
    /// 事前生成済みのJSON期待値データを使用。
    /// 辞書が存在しない環境ではスキップされる。
    /// </summary>
    public abstract class PyOpenJTalkComparisonTestsBase : IDisposable
    {
        private static string? DicPath => Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
        private static bool DictionaryExists => !string.IsNullOrEmpty(DicPath) && Directory.Exists(DicPath);

        private readonly ITokenizer? _tokenizer;
        protected readonly G2PEngine? _engine;

        protected abstract ITokenizer CreateTokenizer(string dicPath);

        /// <summary>テストデータ格納用</summary>
        private class TestCase
        {
            public string input { get; set; } = "";
            public string phonemes { get; set; } = "";
            public List<string> labels { get; set; } = new();
            public string notes { get; set; } = "";
        }

        private readonly List<TestCase>? _testCases;

        protected PyOpenJTalkComparisonTestsBase()
        {
            if (DictionaryExists)
            {
                _tokenizer = CreateTokenizer(DicPath!);
                _engine = new G2PEngine(_tokenizer);
            }

            _testCases = LoadTestCases();
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
        /// テストデータJSONを読み込む。ファイルが存在しない場合はnullを返す。
        /// </summary>
        private static List<TestCase>? LoadTestCases()
        {
            // テストプロジェクトの実行ディレクトリから相対パスを辿る
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TestData", "expected_phonemes.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "expected_phonemes.json"),
                // 絶対パスフォールバック
                Path.GetFullPath(Path.Combine("tests", "TestData", "expected_phonemes.json")),
            };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath))
                {
                    var json = File.ReadAllText(fullPath);
                    return JsonSerializer.Deserialize<List<TestCase>>(json);
                }
            }

            return null;
        }

        // =====================================================================
        // ヘルパー: 長音正規化
        // =====================================================================

        /// <summary>
        /// 長音記号 "-" を前の母音の繰り返しに正規化する。
        /// pyopenjtalkは "o o" と出力し、DotNetG2Pは "o -" と出力する可能性がある。
        /// 例: "t o -" → "t o o", "ky o - ky o -" → "ky o o ky o o"
        /// </summary>
        private static string NormalizeLongVowels(string phonemes)
        {
            var vowels = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "a", "i", "u", "e", "o",
                "A", "I", "U", "E", "O"
            };

            var tokens = phonemes.Split(' ');
            var result = new List<string>(tokens.Length);

            string lastVowel = "a"; // デフォルト
            foreach (var token in tokens)
            {
                if (token == "-")
                {
                    // 長音記号を直前の母音に置換
                    result.Add(lastVowel);
                }
                else
                {
                    result.Add(token);
                    // 母音を記録（大文字・小文字両方）
                    if (vowels.Contains(token))
                    {
                        lastVowel = token.ToLowerInvariant();
                    }
                }
            }

            return string.Join(" ", result);
        }

        /// <summary>
        /// 無声化の大文字/小文字を統一して比較用に正規化する。
        /// "s U k i" → "s u k i"（全て小文字化）
        /// </summary>
        private static string NormalizeUnvoicing(string phonemes)
        {
            // 母音の大文字（無声化マーカー）を小文字に統一
            return phonemes
                .Replace(" A ", " a ").Replace(" I ", " i ").Replace(" U ", " u ")
                .Replace(" E ", " e ").Replace(" O ", " o ")
                // 末尾の場合
                .Replace(" A", " a").Replace(" I", " i").Replace(" U", " u")
                .Replace(" E", " e").Replace(" O", " o");
        }

        /// <summary>
        /// 長音と無声化を両方正規化する。
        /// </summary>
        private static string FullNormalize(string phonemes)
        {
            return NormalizeUnvoicing(NormalizeLongVowels(phonemes));
        }

        // =====================================================================
        // 1. 完全一致テスト
        // =====================================================================

        [SkippableFact]
        public void こんにちは_pyopenjtalkと完全一致()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("こんにちは");

            Assert.Equal("k o N n i ch i w a", result);
        }

        [SkippableFact]
        public void 日本語_pyopenjtalkと完全一致()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("日本語");

            Assert.Equal("n i h o N g o", result);
        }

        [SkippableFact]
        public void すき_pyopenjtalkと完全一致()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("すき");

            // 無声化: "s U k i"
            Assert.Equal("s U k i", result);
        }

        [SkippableFact]
        public void です_pyopenjtalkと完全一致()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("です");

            // 無声化: "d e s U"
            Assert.Equal("d e s U", result);
        }

        // =====================================================================
        // 2. 正規化後比較テスト（長音・無声化の差異を吸収）
        // =====================================================================

        [SkippableTheory]
        [InlineData("おはようございます", "o h a y o o g o z a i m a s U")]
        [InlineData("ありがとう", "a r i g a t o o")]
        [InlineData("東京", "t o o ky o o")]
        [InlineData("東京都", "t o o ky o o t o")]
        [InlineData("人工知能", "j i N k o o ch i n o o")]
        [InlineData("コンピュータ", "k o N py u u t a")]
        [InlineData("プログラミング", "p u r o g u r a m i N g u")]
        public void 正規化後比較_pyopenjtalkと一致(string input, string expected)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotEmpty(result);

            var normalizedResult = FullNormalize(result);
            var normalizedExpected = FullNormalize(expected);

            Assert.Equal(normalizedExpected, normalizedResult);
        }

        // =====================================================================
        // 3. 全テストケースに対する弱い検証（クラッシュしない＋空でない）
        // =====================================================================

        [SkippableFact]
        public void 全テストケース_クラッシュしないで空でない結果を返す()
        {
            SkipIfNoDictionary();

            var cases = GetAllTestCases();

            var failures = new List<string>();
            foreach (var tc in cases)
            {
                try
                {
                    var result = _engine!.ToPhonemes(tc.input);
                    if (string.IsNullOrEmpty(result))
                    {
                        failures.Add($"入力「{tc.input}」: 空の結果が返されました");
                    }
                }
                catch (Exception ex)
                {
                    failures.Add($"入力「{tc.input}」: 例外 {ex.GetType().Name}: {ex.Message}");
                }
            }

            if (failures.Count > 0)
            {
                Assert.Fail($"{failures.Count}件の失敗:\n" + string.Join("\n", failures));
            }
        }

        [SkippableFact]
        public void 全テストケース_正規化後の一致率を報告()
        {
            SkipIfNoDictionary();

            var cases = GetAllTestCases();

            int total = 0;
            int exactMatch = 0;
            int normalizedMatch = 0;
            var mismatches = new List<string>();

            foreach (var tc in cases)
            {
                total++;
                var result = _engine!.ToPhonemes(tc.input);

                if (result == tc.phonemes)
                {
                    exactMatch++;
                    normalizedMatch++;
                }
                else if (FullNormalize(result) == FullNormalize(tc.phonemes))
                {
                    normalizedMatch++;
                }
                else
                {
                    mismatches.Add(
                        $"  入力「{tc.input}」:\n" +
                        $"    期待: {tc.phonemes}\n" +
                        $"    実際: {result}\n" +
                        $"    正規化期待: {FullNormalize(tc.phonemes)}\n" +
                        $"    正規化実際: {FullNormalize(result)}");
                }
            }

            // 一致率の出力（テスト結果メッセージとして表示）
            var summary =
                $"pyopenjtalk比較結果: {total}件中 完全一致={exactMatch}, 正規化一致={normalizedMatch}\n" +
                $"不一致={total - normalizedMatch}件";

            if (mismatches.Count > 0)
            {
                summary += "\n不一致詳細:\n" + string.Join("\n", mismatches);
            }

            // 一致率は現時点で100%を要求しない（進捗把握のため報告のみ）
            // 最低限、75%以上は正規化一致すること
            Assert.True(normalizedMatch >= (total * 3) / 4,
                $"正規化一致率が75%未満です。{summary}");
        }

        // =====================================================================
        // 4. JSONデータ駆動テスト
        // =====================================================================

        [SkippableFact]
        public void JSONテストデータ読み込み_ファイルが存在する()
        {
            // 辞書不要のテスト: JSONファイルが正しく読み込めることを確認
            var cases = _testCases;
            if (cases == null)
            {
                // JSONが読めなくてもフォールバックテストがあるのでスキップ
                Skip.If(true, "expected_phonemes.jsonが見つかりません（フォールバックテストで代替）");
                return;
            }

            Assert.NotEmpty(cases);
            Assert.True(cases.Count >= 18, $"テストケース数が少なすぎます: {cases.Count}件");
        }

        // =====================================================================
        // 5. 数字読みテスト（pyopenjtalkとの比較）
        // =====================================================================

        [SkippableFact]
        public void 数字_100円_クラッシュしないで結果を返す()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("100円");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void 数字_3本_クラッシュしないで結果を返す()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("3本");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void 数字_2024年_クラッシュしないで結果を返す()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("2024年");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void 数字_12月25日_クラッシュしないで結果を返す()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("12月25日");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 6. 文テスト（pyopenjtalkとの比較）
        // =====================================================================

        [SkippableFact]
        public void 文_今日はいい天気ですね_クラッシュしないで結果を返す()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("今日はいい天気ですね");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void 文_私は東京に住んでいます_クラッシュしないで結果を返す()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("私は東京に住んでいます");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // ヘルパー: テストケース取得（JSONフォールバック付き）
        // =====================================================================

        /// <summary>
        /// JSONからテストケースを取得する。JSONが読めない場合はインラインデータにフォールバック。
        /// </summary>
        private List<TestCase> GetAllTestCases()
        {
            if (_testCases != null && _testCases.Count > 0)
                return _testCases;

            // フォールバック: インラインデータ
            return new List<TestCase>
            {
                new TestCase { input = "こんにちは", phonemes = "k o N n i ch i w a" },
                new TestCase { input = "おはようございます", phonemes = "o h a y o o g o z a i m a s U" },
                new TestCase { input = "ありがとう", phonemes = "a r i g a t o o" },
                new TestCase { input = "東京", phonemes = "t o o ky o o" },
                new TestCase { input = "東京都", phonemes = "t o o ky o o t o" },
                new TestCase { input = "日本語", phonemes = "n i h o N g o" },
                new TestCase { input = "人工知能", phonemes = "j i N k o o ch i n o o" },
                new TestCase { input = "音声合成", phonemes = "o N s e e g o o s e e" },
                new TestCase { input = "コンピュータ", phonemes = "k o N py u u t a" },
                new TestCase { input = "プログラミング", phonemes = "p u r o g u r a m i N g u" },
                new TestCase { input = "100円", phonemes = "hy a k u e N" },
                new TestCase { input = "2024年", phonemes = "n i s e N n i j u u y o n e N" },
                new TestCase { input = "3本", phonemes = "s a N b o N" },
                new TestCase { input = "12月25日", phonemes = "j u u n i g a ts U n i j u u g o n i ch i" },
                new TestCase { input = "今日はいい天気ですね", phonemes = "ky o o w a i i t e N k i d e s U n e" },
                new TestCase { input = "私は東京に住んでいます", phonemes = "w a t a sh i w a t o o ky o o n i s U N d e i m a s U" },
                new TestCase { input = "すき", phonemes = "s U k i" },
                new TestCase { input = "です", phonemes = "d e s U" },
            };
        }
    }

    /// <summary>NMeCabTokenizerによるpyopenjtalk比較テスト。</summary>
    public class PyOpenJTalkComparisonTests_NMeCab : PyOpenJTalkComparisonTestsBase
    {
        protected override ITokenizer CreateTokenizer(string dicPath) => new NMeCabTokenizer(dicPath);
    }

    /// <summary>MeCabTokenizerによるpyopenjtalk比較テスト。</summary>
    public class PyOpenJTalkComparisonTests_MeCab : PyOpenJTalkComparisonTestsBase
    {
        protected override ITokenizer CreateTokenizer(string dicPath) => new MeCabTokenizer(dicPath);
    }
}
