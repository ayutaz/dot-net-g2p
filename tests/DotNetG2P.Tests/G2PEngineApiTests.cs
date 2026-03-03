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
        public void ToProsody_Null_ThrowsArgumentNullException()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            Assert.Throws<ArgumentNullException>(() => _engine!.ToProsody(null!));
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
        public void ToAccentPhrases_Null_ThrowsArgumentNullException()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            Assert.Throws<ArgumentNullException>(() => _engine!.ToAccentPhrases(null!));
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
        public void ToFullContextLabels_Null_ThrowsArgumentNullException()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            Assert.Throws<ArgumentNullException>(() => _engine!.ToFullContextLabels(null!));
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
    }
}
