using System;
using System.Collections.Generic;
using DotNetG2P.Multilingual;
using DotNetG2P.Multilingual.Internal;

namespace DotNetG2P.Tests.Multilingual
{
    [Collection(MultilingualSharedCollection.Name)]
    public sealed class CapabilityAdapterTests
    {
        private readonly MultilingualSharedFixture _fixture;

        public CapabilityAdapterTests(MultilingualSharedFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public void PrimaryAdapters_ConvertAndBatchMatchUnderlyingEngines()
        {
            Skip.If(!_fixture.HasDictionary, "naist-jdic辞書が見つかりません");

            var engine = _fixture.DefaultEngine!;

            AssertPrimary(
                engine.GetTextBatchProcessor(Language.Japanese),
                new[] { "こんにちは", "東京タワー" },
                _fixture.JapaneseEngine!.ToPhonemes,
                _fixture.JapaneseEngine.ToPhonemesBatch);

            AssertPrimary(
                engine.GetTextBatchProcessor(Language.English),
                new[] { "hello world", "benchmarking makes regressions visible" },
                _fixture.EnglishEngine.ToPhonemes,
                _fixture.EnglishEngine.ToPhonemesBatch);

            AssertPrimary(
                engine.GetTextBatchProcessor(Language.Chinese),
                new[] { "你好世界", "重要通知" },
                _fixture.ChineseEngine!.ToPinyin,
                texts => _fixture.ChineseEngine.ToPinyinBatch(ToArray(texts)));

            AssertPrimary(
                engine.GetTextBatchProcessor(Language.Korean),
                new[] { "안녕하세요", "한글 테스트" },
                _fixture.KoreanEngine.ToPhonemes,
                _fixture.KoreanEngine.ToPhonemesBatch);

            AssertPrimary(
                engine.GetTextBatchProcessor(Language.Spanish),
                new[] { "hola mundo", "la fonetica ayuda" },
                _fixture.SpanishEngine.ToPhonemes,
                _fixture.SpanishEngine.ToPhonemesBatch);

            AssertPrimary(
                engine.GetTextBatchProcessor(Language.French),
                new[] { "bonjour le monde", "la synthese vocale" },
                _fixture.FrenchEngine.ToPhonemes,
                _fixture.FrenchEngine.ToPhonemesBatch);

            AssertPrimary(
                engine.GetTextBatchProcessor(Language.Portuguese),
                new[] { "ola mundo", "sintese de fala" },
                _fixture.PortugueseEngine.ToPhonemes,
                _fixture.PortugueseEngine.ToPhonemesBatch);
        }

        [SkippableFact]
        public void IpaAdapters_ExposeOnlyLanguagesWithDedicatedIpaCapability()
        {
            Skip.If(!_fixture.HasDictionary, "naist-jdic辞書が見つかりません");

            var engine = _fixture.DefaultEngine!;

            Assert.True(engine.TryGetIpaTextBatchProcessor(Language.English, out var english));
            Assert.NotNull(english);
            AssertIpa(
                english!,
                new[] { "hello world", "phonemes and ipa" },
                _fixture.EnglishEngine.ToIPA,
                _fixture.EnglishEngine.ToIPABatch);

            Assert.True(engine.TryGetIpaTextBatchProcessor(Language.Chinese, out var chinese));
            Assert.NotNull(chinese);
            AssertIpa(
                chinese!,
                new[] { "你好世界", "中国語" },
                _fixture.ChineseEngine!.ToIPA,
                texts => _fixture.ChineseEngine.ToIPABatch(ToArray(texts)));

            Assert.True(engine.TryGetIpaTextBatchProcessor(Language.Spanish, out var spanish));
            Assert.NotNull(spanish);
            AssertIpa(
                spanish!,
                new[] { "hola mundo", "guitarra" },
                _fixture.SpanishEngine.ToIPA,
                _fixture.SpanishEngine.ToIPABatch);

            Assert.True(engine.TryGetIpaTextBatchProcessor(Language.French, out var french));
            Assert.NotNull(french);
            AssertIpa(
                french!,
                new[] { "bonjour", "synthese vocale" },
                _fixture.FrenchEngine.ToIPA,
                _fixture.FrenchEngine.ToIPABatch);

            Assert.True(engine.TryGetIpaTextBatchProcessor(Language.Portuguese, out var portuguese));
            Assert.NotNull(portuguese);
            AssertIpa(
                portuguese!,
                new[] { "ola mundo", "fonetica aplicada" },
                _fixture.PortugueseEngine.ToIPA,
                _fixture.PortugueseEngine.ToIPABatch);

            Assert.False(engine.TryGetIpaTextBatchProcessor(Language.Japanese, out _));
            Assert.False(engine.TryGetIpaTextBatchProcessor(Language.Korean, out _));
        }

        private static void AssertPrimary(
            ITextBatchProcessor<string> processor,
            string[] texts,
            Func<string, string> convert,
            Func<IReadOnlyList<string>, IReadOnlyList<string>> convertBatch)
        {
            for (var i = 0; i < texts.Length; i++)
                Assert.Equal(convert(texts[i]), processor.Convert(texts[i]));

            var expected = convertBatch(texts);
            var actual = processor.ConvertBatch(texts);

            Assert.Equal(expected.Count, actual.Count);
            for (var i = 0; i < expected.Count; i++)
                Assert.Equal(expected[i], actual[i]);
        }

        private static void AssertIpa(
            IIpaTextBatchProcessor processor,
            string[] texts,
            Func<string, string> convertToIpa,
            Func<IReadOnlyList<string>, IReadOnlyList<string>> convertToIpaBatch)
        {
            for (var i = 0; i < texts.Length; i++)
                Assert.Equal(convertToIpa(texts[i]), processor.ConvertToIpa(texts[i]));

            var expected = convertToIpaBatch(texts);
            var actual = processor.ConvertToIpaBatch(texts);

            Assert.Equal(expected.Count, actual.Count);
            for (var i = 0; i < expected.Count; i++)
                Assert.Equal(expected[i], actual[i]);
        }

        private static string[] ToArray(IReadOnlyList<string> texts)
        {
            if (texts is string[] array)
                return array;

            var copy = new string[texts.Count];
            for (var i = 0; i < texts.Count; i++)
                copy[i] = texts[i];

            return copy;
        }
    }
}
