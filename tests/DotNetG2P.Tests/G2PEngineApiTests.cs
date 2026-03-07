using System;
using System.IO;
using DotNetG2P;
using DotNetG2P.Models;
using DotNetG2P.MeCab;
using Xunit;

namespace DotNetG2P.Tests
{
    /// <summary>
    /// G2PEngine API（ToProsody, ToAccentPhrases, ToFullContextLabels）のテスト。
    /// </summary>
    public class G2PEngineApiTests : IDisposable
    {
        private static string? DicPath => Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
        private static bool DictionaryExists => !string.IsNullOrEmpty(DicPath) && Directory.Exists(DicPath);

        private readonly MeCabTokenizer? _tokenizer;
        private readonly G2PEngine? _engine;

        public G2PEngineApiTests()
        {
            if (DictionaryExists)
            {
                _tokenizer = new MeCabTokenizer(DicPath!);
                _engine = new G2PEngine(_tokenizer);
            }
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }

        // ===== ToProsody =====

        [SkippableFact]
        public void ToProsody_EmptyString_ReturnsEmpty()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            Assert.Equal("", _engine!.ToProsody(""));
        }

        [SkippableFact]
        public void ToProsody_Null_ReturnsEmpty()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            Assert.Equal("", _engine!.ToProsody(null!));
        }

        [SkippableFact]
        public void ToProsody_BasicText_ContainsProsodyMarkers()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToProsody("こんにちは");
            Assert.StartsWith("^", result);
            Assert.EndsWith("$", result);
            Assert.Contains("k o", result);
        }

        // ===== ToAccentPhrases =====

        [SkippableFact]
        public void ToAccentPhrases_EmptyString_ReturnsEmptyList()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToAccentPhrases("");
            Assert.Empty(result);
        }

        [SkippableFact]
        public void ToAccentPhrases_Null_ReturnsEmpty()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            Assert.Empty(_engine!.ToAccentPhrases(null!));
        }

        [SkippableFact]
        public void ToAccentPhrases_BasicText_ReturnsNonEmptyList()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToAccentPhrases("こんにちは");
            Assert.NotEmpty(result);
            Assert.True(result[0].Moras.Count > 0);
        }

        // ===== ToFullContextLabels =====

        [SkippableFact]
        public void ToFullContextLabels_EmptyString_ReturnsEmptyList()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToFullContextLabels("");
            Assert.Empty(result);
        }

        [SkippableFact]
        public void ToFullContextLabels_Null_ReturnsEmpty()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            Assert.Empty(_engine!.ToFullContextLabels(null!));
        }

        [SkippableFact]
        public void ToFullContextLabels_BasicText_ReturnsLabels()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToFullContextLabels("こんにちは");
            Assert.NotEmpty(result);
            // 先頭はsil、末尾もsilで始まる
            Assert.Contains("sil", result[0]);
            Assert.Contains("sil", result[result.Count - 1]);
        }

        // ===== ToProsodyFeatures =====

        [SkippableFact]
        public void ToProsodyFeatures_EmptyString_ReturnsEmptyFeatures()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToProsodyFeatures("");
            Assert.Equal(0, result.Count);
        }

        [SkippableFact]
        public void ToProsodyFeatures_Null_ReturnsEmptyFeatures()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToProsodyFeatures(null!);
            Assert.Equal(0, result.Count);
        }

        [SkippableFact]
        public void ToProsodyFeatures_BasicText_ReturnsFeatures()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToProsodyFeatures("こんにちは");
            Assert.True(result.Count > 0);
            // 先頭・末尾はsil
            Assert.Equal("sil", result.Phonemes[0]);
            Assert.Equal("sil", result.Phonemes[result.Count - 1]);
            // 配列長が一致
            Assert.Equal(result.Phonemes.Count, result.A1.Count);
            Assert.Equal(result.Phonemes.Count, result.A2.Count);
            Assert.Equal(result.Phonemes.Count, result.A3.Count);
        }

        [SkippableFact]
        public void ToProsodyFeatures_ConsistentWithFullContextLabels()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var labels = _engine!.ToFullContextLabels("東京は晴れです");
            var features = _engine!.ToProsodyFeatures("東京は晴れです");
            Assert.Equal(labels.Count, features.Count);
        }

        // ===== ToProsodyFeaturesBatch =====

        [SkippableFact]
        public void ToProsodyFeaturesBatch_ReturnsCorrectCount()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var texts = new[] { "こんにちは", "東京は晴れです", "" };
            var results = _engine!.ToProsodyFeaturesBatch(texts);
            Assert.Equal(3, results.Count);
            Assert.True(results[0].Count > 0);
            Assert.True(results[1].Count > 0);
            Assert.Equal(0, results[2].Count);
        }

        // ===== バッチAPI =====

        [SkippableFact]
        public void ToPhonemesBatch_NullArgument_ThrowsArgumentNullException()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            Assert.Throws<ArgumentNullException>(() => _engine!.ToPhonemesBatch(null!));
        }

        [SkippableFact]
        public void ToKanaBatch_NullArgument_ThrowsArgumentNullException()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            Assert.Throws<ArgumentNullException>(() => _engine!.ToKanaBatch(null!));
        }

        [SkippableFact]
        public void ToProsodyBatch_NullArgument_ThrowsArgumentNullException()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            Assert.Throws<ArgumentNullException>(() => _engine!.ToProsodyBatch(null!));
        }

        [SkippableFact]
        public void ToFullContextLabelsBatch_NullArgument_ThrowsArgumentNullException()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            Assert.Throws<ArgumentNullException>(() => _engine!.ToFullContextLabelsBatch(null!));
        }

        [SkippableFact]
        public void ToProsodyFeaturesBatch_NullArgument_ThrowsArgumentNullException()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            Assert.Throws<ArgumentNullException>(() => _engine!.ToProsodyFeaturesBatch(null!));
        }

        [SkippableFact]
        public void ToPhonemesBatch_EmptyList_ReturnsEmptyList()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToPhonemesBatch(Array.Empty<string>());
            Assert.Empty(result);
        }

        [SkippableFact]
        public void ToKanaBatch_EmptyList_ReturnsEmptyList()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToKanaBatch(Array.Empty<string>());
            Assert.Empty(result);
        }

        [SkippableFact]
        public void ToProsodyBatch_EmptyList_ReturnsEmptyList()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToProsodyBatch(Array.Empty<string>());
            Assert.Empty(result);
        }

        [SkippableFact]
        public void ToFullContextLabelsBatch_EmptyList_ReturnsEmptyList()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToFullContextLabelsBatch(Array.Empty<string>());
            Assert.Empty(result);
        }

        [SkippableFact]
        public void ToPhonemesBatch_MixedInput_HandlesAllElements()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var texts = new string[] { "こんにちは", "", null! };
            var result = _engine!.ToPhonemesBatch(texts);

            Assert.Equal(3, result.Count);
            Assert.NotEmpty(result[0]); // 通常文字列は音素が出る
            Assert.Equal("", result[1]); // 空文字列は空
            Assert.Equal("", result[2]); // nullは空
        }

        [SkippableFact]
        public void ToKanaBatch_MixedInput_HandlesAllElements()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var texts = new string[] { "東京", "", null! };
            var result = _engine!.ToKanaBatch(texts);

            Assert.Equal(3, result.Count);
            Assert.NotEmpty(result[0]);
            Assert.Equal("", result[1]);
            Assert.Equal("", result[2]);
        }

        [SkippableFact]
        public void ToPhonemesBatch_MultipleTexts_ReturnsCorrectCount()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var texts = new string[] { "東京", "大阪", "名古屋" };
            var result = _engine!.ToPhonemesBatch(texts);

            Assert.Equal(3, result.Count);
            foreach (var r in result)
            {
                Assert.NotEmpty(r);
                // 音素文字列がスペース区切りであることを検証
                Assert.Contains(" ", r);
            }
        }

        [SkippableFact]
        public void ToProsodyFeatures_WhitespaceOnly_ReturnsEmptyFeatures()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToProsodyFeatures(" ");
            Assert.Equal(0, result.Count);
        }

        // ===== Disposed後のメソッド呼び出し =====

        [SkippableFact]
        public void ToProsody_AfterDispose_ThrowsObjectDisposedException()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            using var tokenizer = new MeCabTokenizer(DicPath!);
            var engine = new G2PEngine(tokenizer);
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToProsody("テスト"));
        }

        [SkippableFact]
        public void ToAccentPhrases_AfterDispose_ThrowsObjectDisposedException()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            using var tokenizer = new MeCabTokenizer(DicPath!);
            var engine = new G2PEngine(tokenizer);
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToAccentPhrases("テスト"));
        }

        [SkippableFact]
        public void ToFullContextLabels_AfterDispose_ThrowsObjectDisposedException()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            using var tokenizer = new MeCabTokenizer(DicPath!);
            var engine = new G2PEngine(tokenizer);
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToFullContextLabels("テスト"));
        }

        [SkippableFact]
        public void ToProsodyFeatures_AfterDispose_ThrowsObjectDisposedException()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            using var tokenizer = new MeCabTokenizer(DicPath!);
            var engine = new G2PEngine(tokenizer);
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToProsodyFeatures("テスト"));
        }
    }
}
