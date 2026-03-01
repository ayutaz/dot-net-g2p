using System;
using System.IO;
using DotNetG2P;
using DotNetG2P.Models;
using DotNetG2P.NMeCab;
using Xunit;

namespace DotNetG2P.Tests
{
    /// <summary>
    /// G2PEngine API（ToProsody, ToAccentPhrases, ToFullContextLabels）のテスト。
    /// </summary>
    public class G2PEngineApiTests : IDisposable
    {
        private const string DictionaryPath = "C:/Users/yuta/Desktop/Private/piper-plus/src/wasm/openjtalk-web/assets/dict/";

        private static readonly bool DictionaryExists =
            Directory.Exists(DictionaryPath) && File.Exists(Path.Combine(DictionaryPath, "sys.dic"));

        private readonly NMeCabTokenizer? _tokenizer;
        private readonly G2PEngine? _engine;

        public G2PEngineApiTests()
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

        // ===== ToProsody =====

        [Fact]
        public void ToProsody_EmptyString_ReturnsEmpty()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            Assert.Equal("", _engine!.ToProsody(""));
        }

        [Fact]
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

        [Fact]
        public void ToAccentPhrases_EmptyString_ReturnsEmptyList()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToAccentPhrases("");
            Assert.Empty(result);
        }

        [Fact]
        public void ToAccentPhrases_Null_ReturnsEmptyList()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToAccentPhrases(null!);
            Assert.Empty(result);
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

        [Fact]
        public void ToFullContextLabels_EmptyString_ReturnsEmptyList()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToFullContextLabels("");
            Assert.Empty(result);
        }

        [Fact]
        public void ToFullContextLabels_Null_ReturnsEmptyList()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            var result = _engine!.ToFullContextLabels(null!);
            Assert.Empty(result);
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
            using var tokenizer = new NMeCabTokenizer(DictionaryPath);
            var engine = new G2PEngine(tokenizer);
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToProsody("テスト"));
        }

        [SkippableFact]
        public void ToAccentPhrases_AfterDispose_ThrowsObjectDisposedException()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            using var tokenizer = new NMeCabTokenizer(DictionaryPath);
            var engine = new G2PEngine(tokenizer);
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToAccentPhrases("テスト"));
        }

        [SkippableFact]
        public void ToFullContextLabels_AfterDispose_ThrowsObjectDisposedException()
        {
            Skip.IfNot(DictionaryExists, "辞書が見つかりません");
            using var tokenizer = new NMeCabTokenizer(DictionaryPath);
            var engine = new G2PEngine(tokenizer);
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToFullContextLabels("テスト"));
        }
    }
}
