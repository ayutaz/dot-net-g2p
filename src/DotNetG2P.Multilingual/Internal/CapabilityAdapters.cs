using System;
using System.Collections.Generic;
using DotNetG2P;
using DotNetG2P.Chinese;
using DotNetG2P.English;
using DotNetG2P.French;
using DotNetG2P.Korean;
using DotNetG2P.Portuguese;
using DotNetG2P.Spanish;

namespace DotNetG2P.Multilingual.Internal
{
    internal interface ITextBatchProcessor<out TResult>
    {
        TResult Convert(string text);

        IReadOnlyList<TResult> ConvertBatch(IReadOnlyList<string> texts);
    }

    internal interface IIpaTextBatchProcessor : ITextBatchProcessor<string>
    {
        string ConvertToIpa(string text);

        IReadOnlyList<string> ConvertToIpaBatch(IReadOnlyList<string> texts);
    }

    internal sealed class LanguageCapabilityRouter
    {
        private readonly IReadOnlyDictionary<Language, ITextBatchProcessor<string>> _primaryProcessors;

        private LanguageCapabilityRouter(IReadOnlyDictionary<Language, ITextBatchProcessor<string>> primaryProcessors)
        {
            _primaryProcessors = primaryProcessors;
        }

        public static LanguageCapabilityRouter Create(
            G2PEngine japaneseEngine,
            object japaneseLock,
            EnglishG2PEngine englishEngine,
            ChineseG2PEngine chineseEngine,
            KoreanG2PEngine koreanEngine,
            SpanishG2PEngine spanishEngine,
            FrenchG2PEngine frenchEngine,
            PortugueseG2PEngine portugueseEngine)
        {
            if (japaneseEngine == null) throw new ArgumentNullException(nameof(japaneseEngine));
            if (japaneseLock == null) throw new ArgumentNullException(nameof(japaneseLock));
            if (englishEngine == null) throw new ArgumentNullException(nameof(englishEngine));
            if (chineseEngine == null) throw new ArgumentNullException(nameof(chineseEngine));
            if (koreanEngine == null) throw new ArgumentNullException(nameof(koreanEngine));
            if (spanishEngine == null) throw new ArgumentNullException(nameof(spanishEngine));
            if (frenchEngine == null) throw new ArgumentNullException(nameof(frenchEngine));
            if (portugueseEngine == null) throw new ArgumentNullException(nameof(portugueseEngine));

            var primaryProcessors = new Dictionary<Language, ITextBatchProcessor<string>>
            {
                [Language.Japanese] = new DelegateTextBatchProcessor(
                    text =>
                    {
                        lock (japaneseLock)
                        {
                            return japaneseEngine.ToPhonemes(text);
                        }
                    },
                    texts =>
                    {
                        lock (japaneseLock)
                        {
                            return japaneseEngine.ToPhonemesBatch(texts);
                        }
                    }),
                [Language.English] = new DelegateIpaTextBatchProcessor(
                    englishEngine.ToPhonemes,
                    englishEngine.ToPhonemesBatch,
                    englishEngine.ToIPA,
                    englishEngine.ToIPABatch),
                [Language.Chinese] = new DelegateIpaTextBatchProcessor(
                    chineseEngine.ToPinyin,
                    texts => chineseEngine.ToPinyinBatch(ToArray(texts)),
                    chineseEngine.ToIPA,
                    texts => chineseEngine.ToIPABatch(ToArray(texts))),
                [Language.Korean] = new DelegateTextBatchProcessor(
                    koreanEngine.ToPhonemes,
                    koreanEngine.ToPhonemesBatch),
                [Language.Spanish] = new DelegateIpaTextBatchProcessor(
                    spanishEngine.ToPhonemes,
                    spanishEngine.ToPhonemesBatch,
                    spanishEngine.ToIPA,
                    spanishEngine.ToIPABatch),
                [Language.French] = new DelegateIpaTextBatchProcessor(
                    frenchEngine.ToPhonemes,
                    frenchEngine.ToPhonemesBatch,
                    frenchEngine.ToIPA,
                    frenchEngine.ToIPABatch),
                [Language.Portuguese] = new DelegateIpaTextBatchProcessor(
                    portugueseEngine.ToPhonemes,
                    portugueseEngine.ToPhonemesBatch,
                    portugueseEngine.ToIPA,
                    portugueseEngine.ToIPABatch),
            };

            return new LanguageCapabilityRouter(primaryProcessors);
        }

        /// <summary>
        /// 非日本語エンジンを遅延初期化で登録するファクトリメソッド。
        /// Lazy&lt;T&gt;.Value へのアクセスはデリゲート経由で行われるため、
        /// 実際にその言語が要求されるまでエンジンは生成されない。
        /// </summary>
        public static LanguageCapabilityRouter CreateLazy(
            G2PEngine japaneseEngine,
            object japaneseLock,
            Lazy<EnglishG2PEngine> lazyEnglishEngine,
            Lazy<ChineseG2PEngine> lazyChineseEngine,
            Lazy<KoreanG2PEngine> lazyKoreanEngine,
            Lazy<SpanishG2PEngine> lazySpanishEngine,
            Lazy<FrenchG2PEngine> lazyFrenchEngine,
            Lazy<PortugueseG2PEngine> lazyPortugueseEngine)
        {
            if (japaneseEngine == null) throw new ArgumentNullException(nameof(japaneseEngine));
            if (japaneseLock == null) throw new ArgumentNullException(nameof(japaneseLock));
            if (lazyEnglishEngine == null) throw new ArgumentNullException(nameof(lazyEnglishEngine));
            if (lazyChineseEngine == null) throw new ArgumentNullException(nameof(lazyChineseEngine));
            if (lazyKoreanEngine == null) throw new ArgumentNullException(nameof(lazyKoreanEngine));
            if (lazySpanishEngine == null) throw new ArgumentNullException(nameof(lazySpanishEngine));
            if (lazyFrenchEngine == null) throw new ArgumentNullException(nameof(lazyFrenchEngine));
            if (lazyPortugueseEngine == null) throw new ArgumentNullException(nameof(lazyPortugueseEngine));

            var primaryProcessors = new Dictionary<Language, ITextBatchProcessor<string>>
            {
                [Language.Japanese] = new DelegateTextBatchProcessor(
                    text =>
                    {
                        lock (japaneseLock)
                        {
                            return japaneseEngine.ToPhonemes(text);
                        }
                    },
                    texts =>
                    {
                        lock (japaneseLock)
                        {
                            return japaneseEngine.ToPhonemesBatch(texts);
                        }
                    }),
                [Language.English] = new DelegateIpaTextBatchProcessor(
                    text => lazyEnglishEngine.Value.ToPhonemes(text),
                    texts => lazyEnglishEngine.Value.ToPhonemesBatch(texts),
                    text => lazyEnglishEngine.Value.ToIPA(text),
                    texts => lazyEnglishEngine.Value.ToIPABatch(texts)),
                [Language.Chinese] = new DelegateIpaTextBatchProcessor(
                    text => lazyChineseEngine.Value.ToPinyin(text),
                    texts => lazyChineseEngine.Value.ToPinyinBatch(ToArray(texts)),
                    text => lazyChineseEngine.Value.ToIPA(text),
                    texts => lazyChineseEngine.Value.ToIPABatch(ToArray(texts))),
                [Language.Korean] = new DelegateTextBatchProcessor(
                    text => lazyKoreanEngine.Value.ToPhonemes(text),
                    texts => lazyKoreanEngine.Value.ToPhonemesBatch(texts)),
                [Language.Spanish] = new DelegateIpaTextBatchProcessor(
                    text => lazySpanishEngine.Value.ToPhonemes(text),
                    texts => lazySpanishEngine.Value.ToPhonemesBatch(texts),
                    text => lazySpanishEngine.Value.ToIPA(text),
                    texts => lazySpanishEngine.Value.ToIPABatch(texts)),
                [Language.French] = new DelegateIpaTextBatchProcessor(
                    text => lazyFrenchEngine.Value.ToPhonemes(text),
                    texts => lazyFrenchEngine.Value.ToPhonemesBatch(texts),
                    text => lazyFrenchEngine.Value.ToIPA(text),
                    texts => lazyFrenchEngine.Value.ToIPABatch(texts)),
                [Language.Portuguese] = new DelegateIpaTextBatchProcessor(
                    text => lazyPortugueseEngine.Value.ToPhonemes(text),
                    texts => lazyPortugueseEngine.Value.ToPhonemesBatch(texts),
                    text => lazyPortugueseEngine.Value.ToIPA(text),
                    texts => lazyPortugueseEngine.Value.ToIPABatch(texts)),
            };

            return new LanguageCapabilityRouter(primaryProcessors);
        }

        public ITextBatchProcessor<string> GetRequired(Language language)
        {
            if (_primaryProcessors.TryGetValue(language, out var processor))
                return processor;

            throw new KeyNotFoundException($"No text processor is registered for language '{language}'.");
        }

        public bool TryGetIpa(Language language, out IIpaTextBatchProcessor? processor)
        {
            if (_primaryProcessors.TryGetValue(language, out var primaryProcessor)
                && primaryProcessor is IIpaTextBatchProcessor ipaProcessor)
            {
                processor = ipaProcessor;
                return true;
            }

            processor = null;
            return false;
        }

        private static string[] ToArray(IReadOnlyList<string> texts)
        {
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            if (texts is string[] array)
                return array;

            var copy = new string[texts.Count];
            for (var i = 0; i < texts.Count; i++)
                copy[i] = texts[i];

            return copy;
        }
    }

    internal sealed class DelegateTextBatchProcessor : ITextBatchProcessor<string>
    {
        private readonly Func<string, string> _convert;
        private readonly Func<IReadOnlyList<string>, IReadOnlyList<string>> _convertBatch;

        public DelegateTextBatchProcessor(
            Func<string, string> convert,
            Func<IReadOnlyList<string>, IReadOnlyList<string>> convertBatch)
        {
            _convert = convert ?? throw new ArgumentNullException(nameof(convert));
            _convertBatch = convertBatch ?? throw new ArgumentNullException(nameof(convertBatch));
        }

        public string Convert(string text) => _convert(text);

        public IReadOnlyList<string> ConvertBatch(IReadOnlyList<string> texts) => _convertBatch(texts);
    }

    internal sealed class DelegateIpaTextBatchProcessor : IIpaTextBatchProcessor
    {
        private readonly DelegateTextBatchProcessor _primaryProcessor;
        private readonly Func<string, string> _convertToIpa;
        private readonly Func<IReadOnlyList<string>, IReadOnlyList<string>> _convertToIpaBatch;

        public DelegateIpaTextBatchProcessor(
            Func<string, string> convert,
            Func<IReadOnlyList<string>, IReadOnlyList<string>> convertBatch,
            Func<string, string> convertToIpa,
            Func<IReadOnlyList<string>, IReadOnlyList<string>> convertToIpaBatch)
        {
            _primaryProcessor = new DelegateTextBatchProcessor(convert, convertBatch);
            _convertToIpa = convertToIpa ?? throw new ArgumentNullException(nameof(convertToIpa));
            _convertToIpaBatch = convertToIpaBatch ?? throw new ArgumentNullException(nameof(convertToIpaBatch));
        }

        public string Convert(string text) => _primaryProcessor.Convert(text);

        public IReadOnlyList<string> ConvertBatch(IReadOnlyList<string> texts) => _primaryProcessor.ConvertBatch(texts);

        public string ConvertToIpa(string text) => _convertToIpa(text);

        public IReadOnlyList<string> ConvertToIpaBatch(IReadOnlyList<string> texts) => _convertToIpaBatch(texts);
    }
}
