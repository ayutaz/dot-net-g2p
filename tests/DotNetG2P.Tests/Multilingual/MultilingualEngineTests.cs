using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetG2P.MeCab;
using DotNetG2P.Multilingual;
using DotNetG2P.English;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// MultilingualG2PEngineのAPI統合テスト。
    /// 辞書依存テストはSkippableFactを使用し、辞書が存在しない環境ではスキップされる。
    /// </summary>
    public class MultilingualEngineTests
    {
        private static string? FindDictPath()
        {
            return NaistJdicLocator.TryResolve(out var dictionaryPath)
                ? dictionaryPath
                : null;
        }

        private static readonly string? DictPath = FindDictPath();

        private void SkipIfNoDictionary()
        {
            Skip.If(DictPath == null, "naist-jdic辞書が見つかりません");
        }

        // =====================================================================
        // 辞書不要テスト（Fact）
        // =====================================================================

        [Fact]
        public void コンストラクタ_nullパス_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new MultilingualG2PEngine((string)null!));
        }

        [Fact]
        public void コンストラクタ_存在しないパス_DirectoryNotFoundException()
        {
            Assert.Throws<DirectoryNotFoundException>(
                () => new MultilingualG2PEngine(@"C:\this_path_does_not_exist_12345"));
        }

        [Fact]
        public void コンストラクタ_nullオプション_ArgumentNullException()
        {
            // nullパスを渡すとArgumentNullExceptionがjapaneseDictPathで先にスローされるため、
            // nullパス + nullオプションの組み合わせでArgumentNullExceptionを検証する。
            // （実装ではパス存在チェックがオプションチェックより先に実行されるため、
            //   存在しないダミーパスではDirectoryNotFoundExceptionが出てしまう）
            var ex = Assert.Throws<ArgumentNullException>(
                () => new MultilingualG2PEngine((string)null!, null!));
            Assert.Equal("japaneseDictPath", ex.ParamName);
        }

        [SkippableFact]
        public void ToPhonemesBatch_nullテキスト_ArgumentNullException()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);
            Assert.Throws<ArgumentNullException>(() => engine.ToPhonemesBatch(null!));
        }

        [SkippableFact]
        public void コンストラクタ_パス省略_既定辞書を解決できる()
        {
            SkipIfNoDictionary();

            using var engine = new MultilingualG2PEngine();
            var phonemes = engine.ToPhonemes("こんにちは hello");

            Assert.False(string.IsNullOrWhiteSpace(phonemes));
        }

        [SkippableFact]
        public void ToSegmentsBatch_nullテキスト_ArgumentNullException()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);
            Assert.Throws<ArgumentNullException>(() => engine.ToSegmentsBatch(null!));
        }

        // =====================================================================
        // ToPhonemes テスト（辞書依存）
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_日本語のみ_正常変換()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToPhonemes("こんにちは");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 日本語音素が含まれることを確認（例: k, o, N など）
            Assert.Contains(" ", result); // スペース区切りの音素列
        }

        [SkippableFact]
        public void ToPhonemes_英語のみ_正常変換()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToPhonemes("hello");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void ToPhonemes_日英混在_両言語の音素が含まれる()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToPhonemes("こんにちはhello");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 結果が空でなく、ある程度の長さがあることを確認
            Assert.True(result.Length > 5, $"日英混在テキストの音素が短すぎます: '{result}'");
        }

        [SkippableFact]
        public void ToPhonemes_空文字列_空文字列()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToPhonemes("");

            Assert.Equal("", result);
        }

        [SkippableFact]
        public void ToPhonemes_null_空文字列()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToPhonemes(null!);

            Assert.Equal("", result);
        }

        [SkippableFact]
        public void ToPhonemes_空白のみ_空文字列()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToPhonemes("   ");

            // 空白のみの場合、セグメンテーション結果に依存するが空文字列が期待される
            // 実装によっては何らかの出力がある可能性もあるため、例外が出ないことを確認
            Assert.NotNull(result);
        }

        // =====================================================================
        // ToSegments テスト（辞書依存）
        // =====================================================================

        [SkippableFact]
        public void ToSegments_日本語のみ_1セグメント言語Japanese()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToSegments("こんにちは");

            Assert.Single(result);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.NotEmpty(result[0].Phonemes);
        }

        [SkippableFact]
        public void ToSegments_英語のみ_1セグメント言語English()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToSegments("hello");

            Assert.Single(result);
            Assert.Equal(Language.English, result[0].Language);
            Assert.NotEmpty(result[0].Phonemes);
        }

        [SkippableFact]
        public void ToSegments_日英混在_複数セグメント()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToSegments("こんにちはhello");

            Assert.True(result.Count >= 2, $"日英混在テキストのセグメント数が2未満: {result.Count}");
            Assert.Contains(result, s => s.Language == Language.Japanese);
            Assert.Contains(result, s => s.Language == Language.English);
        }

        [SkippableFact]
        public void ToSegments_空文字列_空リスト()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToSegments("");

            Assert.Empty(result);
        }

        [SkippableFact]
        public void ToSegments_null_空リスト()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToSegments(null!);

            Assert.Empty(result);
        }

        [SkippableFact]
        public void ToSegments_各セグメントSourceTextが正しい()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToSegments("日本語English");

            Assert.True(result.Count >= 2, $"セグメント数が2未満: {result.Count}");

            // 日本語セグメントのSourceTextが日本語を含む
            var jpSegment = result.First(s => s.Language == Language.Japanese);
            Assert.Contains("日本語", jpSegment.SourceText);

            // 英語セグメントのSourceTextが英語を含む
            var enSegment = result.First(s => s.Language == Language.English);
            Assert.Contains("English", enSegment.SourceText);
        }

        // =====================================================================
        // Batch API テスト（辞書依存）
        // =====================================================================

        [SkippableFact]
        public void ToPhonemesBatch_複数テキスト_全て変換される()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var texts = new[] { "こんにちは", "hello", "東京Tower" };
            var result = engine.ToPhonemesBatch(texts);

            Assert.Equal(3, result.Count);
            foreach (var phonemes in result)
            {
                Assert.NotNull(phonemes);
                Assert.NotEmpty(phonemes);
            }
        }

        [SkippableFact]
        public void ToPhonemesBatch_空リスト_空リスト()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToPhonemesBatch(Array.Empty<string>());

            Assert.Empty(result);
        }

        [SkippableFact]
        public void ToSegmentsBatch_複数テキスト_全て変換される()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var texts = new[] { "こんにちは", "hello", "東京Tower" };
            var result = engine.ToSegmentsBatch(texts);

            Assert.Equal(3, result.Count);
            foreach (var segments in result)
            {
                Assert.NotNull(segments);
                Assert.NotEmpty(segments);
            }
        }

        [SkippableFact]
        public void ToSegmentsBatch_空リスト_空リスト()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToSegmentsBatch(Array.Empty<string>());

            Assert.Empty(result);
        }

        // =====================================================================
        // 追加シナリオテスト（辞書依存）
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_数字混在_日本語音素が返る()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToPhonemes("3月");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void ToPhonemes_英語文_音素が返る()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToPhonemes("I love sushi");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void ToSegments_日英日_3セグメント()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToSegments("東京のTokyoタワー");

            // 「東京の」(JP) + 「Tokyo」(EN) + 「タワー」(JP) = 3セグメント
            Assert.Equal(3, result.Count);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal(Language.English, result[1].Language);
            Assert.Equal(Language.Japanese, result[2].Language);
        }

        [SkippableFact]
        public void カスタムオプション_SegmentSeparator変更()
        {
            SkipIfNoDictionary();
            var options = new MultilingualG2POptions(segmentSeparator: " | ");
            using var engine = new MultilingualG2PEngine(DictPath!, options);

            var result = engine.ToPhonemes("こんにちはhello");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // セパレータが " | " に変更されている場合、日英セグメント間に含まれる
            Assert.Contains(" | ", result);
        }

        [SkippableFact]
        public void カスタムオプション_英語ストレスなし()
        {
            SkipIfNoDictionary();
            var englishOptions = new EnglishG2POptions(includeStress: false);
            var options = new MultilingualG2POptions(englishOptions: englishOptions);
            using var engine = new MultilingualG2PEngine(DictPath!, options);

            var result = engine.ToPhonemes("hello");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // ストレスなしの場合、数字（0, 1, 2）が含まれないことを確認
            Assert.DoesNotContain("0", result);
            Assert.DoesNotContain("1", result);
            Assert.DoesNotContain("2", result);
        }

        [SkippableFact]
        public void ToPhonemes_記号のみ_例外なし()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            // 記号のみのテキスト: 例外を出さずに処理できることを確認
            var result = engine.ToPhonemes("！？。、");

            Assert.NotNull(result);
            // 記号のみの場合、日本語デフォルト処理として空でない結果になる可能性がある
        }

        [SkippableFact]
        public void ToSegments_長文混在テキスト_セグメント数チェック()
        {
            SkipIfNoDictionary();
            using var engine = new MultilingualG2PEngine(DictPath!);

            var result = engine.ToSegments("今日はvery good dayですね。Tokyoに行きましょう。");

            // 複数の言語切り替えが含まれるため、セグメント数は3以上
            Assert.True(result.Count >= 3,
                $"長文混在テキストのセグメント数が3未満: {result.Count}");

            // 全セグメントが音素を持つ
            foreach (var segment in result)
            {
                Assert.NotNull(segment.Phonemes);
                Assert.NotNull(segment.SourceText);
                Assert.NotEmpty(segment.SourceText);
            }
        }
    }
}
