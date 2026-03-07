using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotNetG2P.Multilingual;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// MultilingualG2PEngine の Dispose/IDisposable パターンのテスト。
    /// Dispose後のObjectDisposedException発生、二重Dispose安全性、
    /// using文パターン等を検証する。
    /// </summary>
    public class MultilingualDisposeTests
    {
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

        private static void SkipIfNoDictionary(string? dictPath)
        {
            Skip.If(dictPath == null, "naist-jdic辞書が見つかりません（環境変数 NAIST_JDIC_PATH を設定してください）");
        }

        // =====================================================================
        // 1. Dispose後_ToPhonemes_ObjectDisposedException
        // =====================================================================

        [SkippableFact]
        public void Dispose後_ToPhonemes_ObjectDisposedException()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            var engine = new MultilingualG2PEngine(dictPath!);
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemes("hello世界"));
        }

        // =====================================================================
        // 2. Dispose後_ToSegments_ObjectDisposedException
        // =====================================================================

        [SkippableFact]
        public void Dispose後_ToSegments_ObjectDisposedException()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            var engine = new MultilingualG2PEngine(dictPath!);
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToSegments("hello世界"));
        }

        // =====================================================================
        // 3. Dispose後_ToPhonemesBatch_ObjectDisposedException
        // =====================================================================

        [SkippableFact]
        public void Dispose後_ToPhonemesBatch_ObjectDisposedException()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            var engine = new MultilingualG2PEngine(dictPath!);
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(
                () => engine.ToPhonemesBatch(new[] { "hello", "世界" }));
        }

        // =====================================================================
        // 4. Dispose後_ToSegmentsBatch_ObjectDisposedException
        // =====================================================================

        [SkippableFact]
        public void Dispose後_ToSegmentsBatch_ObjectDisposedException()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            var engine = new MultilingualG2PEngine(dictPath!);
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(
                () => engine.ToSegmentsBatch(new[] { "hello", "世界" }));
        }

        // =====================================================================
        // 5. 二重Dispose_例外なし
        // =====================================================================

        [SkippableFact]
        public void 二重Dispose_例外なし()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            var engine = new MultilingualG2PEngine(dictPath!);

            // 二重Disposeで例外が発生しないこと（Interlocked保護）
            engine.Dispose();
            var exception = Record.Exception(() => engine.Dispose());

            Assert.Null(exception);
        }

        // =====================================================================
        // 6. 三重Dispose_例外なし
        // =====================================================================

        [SkippableFact]
        public void 三重Dispose_例外なし()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            var engine = new MultilingualG2PEngine(dictPath!);

            engine.Dispose();
            engine.Dispose();
            var exception = Record.Exception(() => engine.Dispose());

            Assert.Null(exception);
        }

        // =====================================================================
        // 7. Dispose前_正常動作確認
        // =====================================================================

        [SkippableFact]
        public void Dispose前_正常動作確認()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            using var engine = new MultilingualG2PEngine(dictPath!);

            // Dispose前は正常に動作すること
            var phonemes = engine.ToPhonemes("こんにちはhello");
            Assert.NotNull(phonemes);
            Assert.NotEmpty(phonemes);

            var segments = engine.ToSegments("こんにちはhello");
            Assert.NotNull(segments);
            Assert.NotEmpty(segments);
        }

        // =====================================================================
        // 8. using文パターン_正常動作
        // =====================================================================

        [SkippableFact]
        public void using文パターン_正常動作()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            string result;
            using (var engine = new MultilingualG2PEngine(dictPath!))
            {
                result = engine.ToPhonemes("テストtest");
            }

            // usingブロック終了後も結果は有効
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 9. Dispose後に再度Dispose_安全（Interlocked）
        // =====================================================================

        [SkippableFact]
        public void Dispose後に再度Dispose_Interlocked保護で安全()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            var engine = new MultilingualG2PEngine(dictPath!);

            // 正常動作を確認してからDispose
            engine.ToPhonemes("テスト");
            engine.Dispose();

            // 再度Disposeしても安全
            var exception = Record.Exception(() => engine.Dispose());
            Assert.Null(exception);

            // Dispose後はObjectDisposedExceptionになること
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemes("テスト"));
        }

        // =====================================================================
        // 10. 複数インスタンス_独立Dispose
        // =====================================================================

        [SkippableFact]
        public void 複数インスタンス_独立Dispose()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            var engine1 = new MultilingualG2PEngine(dictPath!);
            var engine2 = new MultilingualG2PEngine(dictPath!);

            // engine1をDisposeしてもengine2は動作すること
            engine1.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine1.ToPhonemes("テスト"));

            var result = engine2.ToPhonemes("テスト");
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            engine2.Dispose();
        }

        // =====================================================================
        // 11. Dispose後_別インスタンスは影響なし
        // =====================================================================

        [SkippableFact]
        public void Dispose後_別インスタンスは影響なし()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            var engine1 = new MultilingualG2PEngine(dictPath!);
            var engine2 = new MultilingualG2PEngine(dictPath!);

            // 両方で変換実行
            var result1Before = engine1.ToPhonemes("hello世界");
            var result2Before = engine2.ToPhonemes("hello世界");

            // engine1をDispose
            engine1.Dispose();

            // engine2は影響なく動作すること
            var result2After = engine2.ToPhonemes("hello世界");
            Assert.Equal(result2Before, result2After);

            engine2.Dispose();
        }

        // =====================================================================
        // 12. 大量の変換後Dispose_正常
        // =====================================================================

        [SkippableFact]
        public void 大量の変換後Dispose_正常()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            var engine = new MultilingualG2PEngine(dictPath!);

            // 100回変換を実行
            for (int i = 0; i < 100; i++)
            {
                var result = engine.ToPhonemes($"テスト{i}回目test{i}");
                Assert.NotNull(result);
            }

            // 大量変換後もDisposeが正常に完了すること
            var exception = Record.Exception(() => engine.Dispose());
            Assert.Null(exception);
        }

        // =====================================================================
        // 13. GC後も安全
        // =====================================================================

        [SkippableFact]
        public void GC後も安全()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            var engine = new MultilingualG2PEngine(dictPath!);
            var result = engine.ToPhonemes("テストtest");
            Assert.NotNull(result);

            // GCを強制実行
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // GC後もDisposeが安全に完了すること
            var exception = Record.Exception(() => engine.Dispose());
            Assert.Null(exception);
        }

        // =====================================================================
        // 14. コンストラクタ直後Dispose_正常
        // =====================================================================

        [SkippableFact]
        public void コンストラクタ直後Dispose_正常()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            // 一度も使用せずにDispose
            var engine = new MultilingualG2PEngine(dictPath!);
            var exception = Record.Exception(() => engine.Dispose());

            Assert.Null(exception);
        }

        // =====================================================================
        // 15. Dispose呼出順序_内部エンジンも解放される
        // =====================================================================

        [SkippableFact]
        public void Dispose呼出順序_内部エンジンも解放される()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            var engine = new MultilingualG2PEngine(dictPath!);

            // 正常動作を確認
            var phonemes = engine.ToPhonemes("テストtest");
            Assert.NotNull(phonemes);
            Assert.NotEmpty(phonemes);

            var segments = engine.ToSegments("テストtest");
            Assert.NotNull(segments);

            // Dispose実行
            engine.Dispose();

            // すべてのAPIがObjectDisposedExceptionをスローすること
            // →内部の日本語・英語エンジンも解放されている
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemes("テスト"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToSegments("テスト"));
            Assert.Throws<ObjectDisposedException>(
                () => engine.ToPhonemesBatch(new[] { "テスト" }));
            Assert.Throws<ObjectDisposedException>(
                () => engine.ToSegmentsBatch(new[] { "テスト" }));
        }

        // =====================================================================
        // 16. 並行Dispose_例外なし
        // =====================================================================

        [SkippableFact]
        public void 並行Dispose_例外なし()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            var engine = new MultilingualG2PEngine(dictPath!);

            // 複数スレッドから同時にDispose()を呼び出しても例外が発生しないこと
            var tasks = new Task[8];
            var barrier = new Barrier(8);

            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    var ex = Record.Exception(() => engine.Dispose());
                    Assert.Null(ex);
                });
            }

            Task.WaitAll(tasks);
        }

        // =====================================================================
        // 17. 並行アクセス中のDispose_安全
        // =====================================================================

        [SkippableFact]
        public void 並行アクセス中のDispose_安全()
        {
            var dictPath = FindDictPath();
            SkipIfNoDictionary(dictPath);

            var engine = new MultilingualG2PEngine(dictPath!);

            // ウォームアップ
            engine.ToPhonemes("テストtest");

            // 複数スレッドで変換を実行しながらDisposeしても安全であること
            // ObjectDisposedExceptionは正常な振る舞い
            var tasks = new Task[4];
            for (int i = 0; i < tasks.Length - 1; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < 20; j++)
                    {
                        try
                        {
                            engine.ToPhonemes("テストtest");
                        }
                        catch (ObjectDisposedException)
                        {
                            // Dispose後のObjectDisposedExceptionは正常
                            break;
                        }
                    }
                });
            }
            // 最後のタスクでDispose実行
            tasks[tasks.Length - 1] = Task.Run(() =>
            {
                Thread.Sleep(5);
                engine.Dispose();
            });

            // 全タスクが例外なく完了すること
            var ex = Record.Exception(() => Task.WaitAll(tasks));
            Assert.Null(ex);
        }
    }
}
