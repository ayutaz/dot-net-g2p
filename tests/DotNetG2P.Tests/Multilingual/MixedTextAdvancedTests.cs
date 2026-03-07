using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetG2P.Multilingual;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// MultilingualG2PEngineの日英混在テキスト検証: 実用パターンとエッジケース。
    /// </summary>
    public class MixedTextAdvancedTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly MultilingualG2PEngine? _engine;
        private readonly bool _hasDictionary;
        private readonly string? _dictPath;

        private static string? FindDictPath()
        {
            var envPath = Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
                return envPath;
            var candidates = new[]
            {
                @"C:\Users\yuta\Desktop\Private\open_jtalk_dic_utf_8-1.11",
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

        public MixedTextAdvancedTests(ITestOutputHelper output)
        {
            _output = output;
            _dictPath = FindDictPath();
            _hasDictionary = _dictPath != null;
            if (_hasDictionary)
                _engine = new MultilingualG2PEngine(_dictPath!);
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
        // 実用パターン: IT用語混在
        // =====================================================================

        [SkippableFact]
        public void IT用語混在_APIはRESTfulなdesignです_セグメント分割と音素出力()
        {
            SkipIfNoDictionary();

            var text = "このAPIはRESTfulなdesignです";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);

            var segments = _engine.ToSegments(text);
            _output.WriteLine($"Segments: {segments.Count}");
            foreach (var seg in segments)
                _output.WriteLine($"  {seg}");

            // 日本語と英語の両セグメントが存在すること
            Assert.Contains(segments, s => s.Language == Language.Japanese);
            Assert.Contains(segments, s => s.Language == Language.English);
        }

        [SkippableFact]
        public void IT用語混在_serverのresponseがtimeoutした_変換()
        {
            SkipIfNoDictionary();

            var text = "serverのresponseがtimeoutした";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 実用パターン: 固有名詞混在
        // =====================================================================

        [SkippableFact]
        public void 固有名詞混在_AppleのiPhoneは日本で人気です_変換()
        {
            SkipIfNoDictionary();

            var text = "AppleのiPhoneは日本で人気です";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);

            var segments = _engine.ToSegments(text);
            Assert.Contains(segments, s => s.Language == Language.Japanese);
            Assert.Contains(segments, s => s.Language == Language.English);
        }

        [SkippableFact]
        public void 固有名詞混在_GoogleとMicrosoftが競争している_変換()
        {
            SkipIfNoDictionary();

            var text = "GoogleとMicrosoftが競争している";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);

            var segments = _engine.ToSegments(text);
            // Google(EN) + と(JP) + Microsoft(EN) + が競争している(JP) = 4セグメント以上
            Assert.True(segments.Count >= 3,
                $"セグメント数が3未満: {segments.Count}");
        }

        // =====================================================================
        // 実用パターン: 英語略語混在
        // =====================================================================

        [SkippableFact]
        public void 英語略語混在_AIとMLのtechnologyが進歩している_変換()
        {
            SkipIfNoDictionary();

            var text = "AIとMLのtechnologyが進歩している";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void 英語略語混在_HTTPSでSSL通信する_変換()
        {
            SkipIfNoDictionary();

            var text = "HTTPSでSSL通信する";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 実用パターン: プログラミング文脈
        // =====================================================================

        [SkippableFact]
        public void プログラミング文脈_pythonのlistをsortする_変換()
        {
            SkipIfNoDictionary();

            var text = "pythonのlistをsortする";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);

            var segments = _engine.ToSegments(text);
            Assert.Contains(segments, s => s.Language == Language.English);
            Assert.Contains(segments, s => s.Language == Language.Japanese);
        }

        [SkippableFact]
        public void プログラミング文脈_nullチェックをbypassする_変換()
        {
            SkipIfNoDictionary();

            var text = "nullチェックをbypassする";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 実用パターン: ビジネス文脈
        // =====================================================================

        [SkippableFact]
        public void ビジネス文脈_meetingのscheduleをconfirmしてください_変換()
        {
            SkipIfNoDictionary();

            var text = "meetingのscheduleをconfirmしてください";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);

            var segments = _engine.ToSegments(text);
            Assert.Contains(segments, s => s.Language == Language.English);
            Assert.Contains(segments, s => s.Language == Language.Japanese);
        }

        [SkippableFact]
        public void ビジネス文脈_deadlineまでにreportを提出する_変換()
        {
            SkipIfNoDictionary();

            var text = "deadlineまでにreportを提出する";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 実用パターン: 数字混在
        // =====================================================================

        [SkippableFact]
        public void 数字混在_100個のitemsを処理する_変換()
        {
            SkipIfNoDictionary();

            var text = "100個のitemsを処理する";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void 数字混在_version3のupdateが50回行われた_変換()
        {
            SkipIfNoDictionary();

            var text = "version3のupdateが50回行われた";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // エッジケース: 1文字の英語挟み込み
        // =====================================================================

        [SkippableFact]
        public void エッジケース_aを入力_1文字英語挟み込み()
        {
            SkipIfNoDictionary();

            var text = "aを入力";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void エッジケース_Iが主語_1文字英語挟み込み()
        {
            SkipIfNoDictionary();

            var text = "Iが主語";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void エッジケース_xの値_1文字英語挟み込み()
        {
            SkipIfNoDictionary();

            var text = "xの値";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // エッジケース: 半角カタカナと英語混在
        // =====================================================================

        [SkippableFact]
        public void エッジケース_半角カタカナと英語混在_変換される()
        {
            SkipIfNoDictionary();

            var text = "ｱｲｳhelloｴｵ";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);

            var segments = _engine.ToSegments(text);
            _output.WriteLine($"Segments: {segments.Count}");
            foreach (var seg in segments)
                _output.WriteLine($"  {seg}");

            // 半角カタカナは日本語として判定されるはず
            Assert.Contains(segments, s => s.Language == Language.Japanese);
            Assert.Contains(segments, s => s.Language == Language.English);
        }

        [SkippableFact]
        public void エッジケース_半角カタカナ単独_変換される()
        {
            SkipIfNoDictionary();

            var text = "ｶﾀｶﾅ";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            // 半角カタカナのみでもエラーにならないこと
        }

        // =====================================================================
        // エッジケース: 全角英字と半角英字混在
        // =====================================================================

        [SkippableFact]
        public void エッジケース_全角英字と半角英字混在_変換される()
        {
            SkipIfNoDictionary();

            var text = "Ｈｅｌｌｏとhello";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);

            var segments = _engine.ToSegments(text);
            _output.WriteLine($"Segments: {segments.Count}");
            foreach (var seg in segments)
                _output.WriteLine($"  {seg}");
        }

        [SkippableFact]
        public void エッジケース_全角英字のみ_変換される()
        {
            SkipIfNoDictionary();

            var text = "ＨＥＬＬＯ ＷＯＲＬＤ";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // エッジケース: 空白なし日英接続
        // =====================================================================

        [SkippableFact]
        public void エッジケース_空白なし日英接続_変換される()
        {
            SkipIfNoDictionary();

            var text = "日本語English日本語";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);

            var segments = _engine.ToSegments(text);
            Assert.True(segments.Count >= 3,
                $"空白なし日英日で3セグメント以上期待: 実際={segments.Count}");

            Assert.Equal(Language.Japanese, segments[0].Language);
            Assert.Equal(Language.English, segments[1].Language);
            Assert.Equal(Language.Japanese, segments[2].Language);
        }

        [SkippableFact]
        public void エッジケース_空白なし英日英接続_変換される()
        {
            SkipIfNoDictionary();

            var text = "HelloこんにちはWorld";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);

            var segments = _engine.ToSegments(text);
            Assert.True(segments.Count >= 3,
                $"空白なし英日英で3セグメント以上期待: 実際={segments.Count}");

            Assert.Equal(Language.English, segments[0].Language);
            Assert.Equal(Language.Japanese, segments[1].Language);
            Assert.Equal(Language.English, segments[2].Language);
        }

        // =====================================================================
        // エッジケース: 連続する短いセグメント
        // =====================================================================

        [SkippableFact]
        public void エッジケース_連続する短いセグメント_変換される()
        {
            SkipIfNoDictionary();

            // 短い英単語と日本語が交互に出現
            var text = "ABあCDいEFう";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);

            var segments = _engine.ToSegments(text);
            _output.WriteLine($"Segments: {segments.Count}");
            foreach (var seg in segments)
                _output.WriteLine($"  {seg}");

            // 多数のセグメントに分割されること
            Assert.True(segments.Count >= 3,
                $"連続短セグメントで3セグメント以上期待: 実際={segments.Count}");
        }

        [SkippableFact]
        public void エッジケース_1文字ずつ交互_変換される()
        {
            SkipIfNoDictionary();

            var text = "aあbいcう";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // エッジケース: Dispose後のアクセス
        // =====================================================================

        [SkippableFact]
        public void エッジケース_Dispose後のToPhonemes_ObjectDisposedException()
        {
            SkipIfNoDictionary();

            var engine = new MultilingualG2PEngine(_dictPath!);
            // 正常動作を確認
            var result = engine.ToPhonemes("テストtest");
            Assert.NotEmpty(result);

            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
                engine.ToPhonemes("このAPIはRESTfulなdesignです"));
        }

        [SkippableFact]
        public void エッジケース_Dispose後のToSegments_ObjectDisposedException()
        {
            SkipIfNoDictionary();

            var engine = new MultilingualG2PEngine(_dictPath!);
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
                engine.ToSegments("pythonのlistをsortする"));
        }

        [SkippableFact]
        public void エッジケース_Dispose後のBatchAPI_ObjectDisposedException()
        {
            SkipIfNoDictionary();

            var engine = new MultilingualG2PEngine(_dictPath!);
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
                engine.ToPhonemesBatch(new[] { "hello世界", "テスト" }));
            Assert.Throws<ObjectDisposedException>(() =>
                engine.ToSegmentsBatch(new[] { "hello世界", "テスト" }));
        }

        // =====================================================================
        // エッジケース: 並行アクセスの安全性
        // =====================================================================

        [SkippableFact]
        public void エッジケース_並行アクセス_スレッドセーフ()
        {
            SkipIfNoDictionary();

            // ウォームアップ
            _engine!.ToPhonemes("テストtest");

            var inputs = new[]
            {
                "このAPIはRESTfulなdesignです",
                "AppleのiPhoneは日本で人気です",
                "pythonのlistをsortする",
                "meetingのscheduleをconfirmしてください",
                "100個のitemsを処理する",
                "AIとMLのtechnologyが進歩している",
                "deadlineまでにreportを提出する",
                "Helloこんにちはworld",
            };

            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            Parallel.For(0, 50, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
            {
                try
                {
                    var text = inputs[i % inputs.Length];
                    var result = _engine.ToPhonemes(text);
                    Assert.NotNull(result);
                    Assert.NotEmpty(result);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            if (exceptions.Any())
            {
                var first = exceptions.First();
                _output.WriteLine($"並行アクセスで例外発生: {first}");
            }

            Assert.Empty(exceptions);
        }

        [SkippableFact]
        public void エッジケース_並行アクセス_ToSegmentsもスレッドセーフ()
        {
            SkipIfNoDictionary();

            _engine!.ToSegments("テストtest");

            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            Parallel.For(0, 30, new ParallelOptions { MaxDegreeOfParallelism = 4 }, i =>
            {
                try
                {
                    var result = _engine.ToSegments("日本語English日本語");
                    Assert.NotNull(result);
                    Assert.True(result.Count >= 2);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            Assert.Empty(exceptions);
        }

        // =====================================================================
        // 追加実用パターン: 複合的な文
        // =====================================================================

        [SkippableFact]
        public void 複合パターン_日英混在長文_セグメント整合性()
        {
            SkipIfNoDictionary();

            var text = "今日のmeetingでprojectのstatusをreviewして、次のsprintのplanningをしましょう";
            var segments = _engine!.ToSegments(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Segments: {segments.Count}");
            foreach (var seg in segments)
                _output.WriteLine($"  {seg}");

            // 各セグメントのSourceTextを結合すると元テキストに近い内容になること
            var allSourceText = string.Concat(segments.Select(s => s.SourceText));
            Assert.Equal(text, allSourceText);

            // 全セグメントが音素を持つこと
            foreach (var seg in segments)
            {
                Assert.NotNull(seg.Phonemes);
                Assert.NotEmpty(seg.Phonemes);
            }
        }

        [SkippableFact]
        public void 複合パターン_技術記事風_変換される()
        {
            SkipIfNoDictionary();

            var text = "ReactとVueはfrontendのframeworkです";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void 複合パターン_SNS投稿風_変換される()
        {
            SkipIfNoDictionary();

            var text = "今日のlunchはpasta、おいしかった";
            var result = _engine!.ToPhonemes(text);
            _output.WriteLine($"Input: {text}");
            _output.WriteLine($"Phonemes: {result}");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // セグメント結合の正確性
        // =====================================================================

        [SkippableFact]
        public void セグメント結合_SourceText結合が元テキストと一致()
        {
            SkipIfNoDictionary();

            var texts = new[]
            {
                "日本語English日本語",
                "Helloこんにちは",
                "AIとMLの技術",
                "100個のitems",
                "aを入力",
            };

            foreach (var text in texts)
            {
                var segments = _engine!.ToSegments(text);
                var reconstructed = string.Concat(segments.Select(s => s.SourceText));
                Assert.Equal(text, reconstructed);
                _output.WriteLine($"OK: '{text}' -> {segments.Count} segments");
            }
        }

        // =====================================================================
        // バッチAPIの日英混在テスト
        // =====================================================================

        [SkippableFact]
        public void バッチAPI_日英混在テキスト複数_全て変換される()
        {
            SkipIfNoDictionary();

            var texts = new[]
            {
                "このAPIはRESTfulなdesignです",
                "AppleのiPhoneは日本で人気です",
                "pythonのlistをsortする",
                "meetingのscheduleをconfirmしてください",
                "100個のitemsを処理する",
            };

            var results = _engine!.ToPhonemesBatch(texts);
            Assert.Equal(texts.Length, results.Count);

            for (int i = 0; i < results.Count; i++)
            {
                Assert.NotNull(results[i]);
                Assert.NotEmpty(results[i]);
                _output.WriteLine($"[{i}] Input: {texts[i]}");
                _output.WriteLine($"[{i}] Phonemes: {results[i]}");
            }
        }

        [SkippableFact]
        public void バッチAPI_ToSegmentsBatch_日英混在_全て変換される()
        {
            SkipIfNoDictionary();

            var texts = new[]
            {
                "日本語English日本語",
                "Helloこんにちはworld",
                "deadlineまでにreportを提出する",
            };

            var results = _engine!.ToSegmentsBatch(texts);
            Assert.Equal(texts.Length, results.Count);

            for (int i = 0; i < results.Count; i++)
            {
                Assert.NotNull(results[i]);
                Assert.NotEmpty(results[i]);
                _output.WriteLine($"[{i}] Input: {texts[i]} -> {results[i].Count} segments");
            }
        }

        // =====================================================================
        // 冪等性: 同じ入力で同じ結果が得られること
        // =====================================================================

        [SkippableFact]
        public void 冪等性_実用パターンで繰り返し変換_同じ結果()
        {
            SkipIfNoDictionary();

            var text = "meetingのscheduleをconfirmしてください";
            var result1 = _engine!.ToPhonemes(text);
            var result2 = _engine.ToPhonemes(text);
            var result3 = _engine.ToPhonemes(text);

            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);

            var segments1 = _engine.ToSegments(text);
            var segments2 = _engine.ToSegments(text);

            Assert.Equal(segments1.Count, segments2.Count);
            for (int i = 0; i < segments1.Count; i++)
            {
                Assert.Equal(segments1[i].Language, segments2[i].Language);
                Assert.Equal(segments1[i].SourceText, segments2[i].SourceText);
                Assert.Equal(segments1[i].Phonemes, segments2[i].Phonemes);
            }
        }
    }
}
