#nullable enable

using System;
using DotNetG2P.Chinese;
using DotNetG2P.English;
using DotNetG2P.French;
using DotNetG2P.Spanish;

namespace DotNetG2P.Multilingual
{
    /// <summary>多言語G2Pエンジンのオプション（イミュータブル）。</summary>
    public sealed class MultilingualG2POptions
    {
        /// <summary>日本語G2Pオプション（null時はデフォルト）。</summary>
        public G2POptions? JapaneseOptions { get; }

        /// <summary>英語G2Pオプション（null時はデフォルト）。</summary>
        public EnglishG2POptions? EnglishOptions { get; }

        /// <summary>中国語G2Pオプション（null時はデフォルト）。</summary>
        public ChineseG2POptions? ChineseOptions { get; }

        /// <summary>スペイン語G2Pオプション（null時はデフォルト）。</summary>
        public SpanishG2POptions? SpanishOptions { get; }

        /// <summary>フランス語G2Pオプション（null時はデフォルト）。</summary>
        public FrenchG2POptions? FrenchOptions { get; }

        /// <summary>CJK漢字のデフォルト言語（周囲にかな文字がない場合に使用、デフォルト: Japanese）。</summary>
        public Language DefaultCjkLanguage { get; }

        /// <summary>ラテン文字列のデフォルト言語（デフォルト: English）。</summary>
        public Language DefaultLatinLanguage { get; }

        /// <summary>セグメント間の区切り文字（デフォルト: スペース）。</summary>
        public string SegmentSeparator { get; }

        /// <summary>デフォルトオプション。</summary>
        public static readonly MultilingualG2POptions Default = new MultilingualG2POptions();

        /// <summary>
        /// MultilingualG2POptionsを初期化する。
        /// </summary>
        /// <param name="japaneseOptions">日本語G2Pオプション（null時はデフォルト）</param>
        /// <param name="englishOptions">英語G2Pオプション（null時はデフォルト）</param>
        /// <param name="chineseOptions">中国語G2Pオプション（null時はデフォルト）</param>
        /// <param name="defaultCjkLanguage">CJK漢字のデフォルト言語（デフォルト: Japanese）</param>
        /// <param name="segmentSeparator">セグメント間の区切り文字（デフォルト: スペース）</param>
        /// <param name="spanishOptions">スペイン語G2Pオプション（null時はデフォルト）</param>
        /// <param name="defaultLatinLanguage">ラテン文字列のデフォルト言語（デフォルト: English）</param>
        /// <param name="frenchOptions">フランス語G2Pオプション（null時はデフォルト）</param>
        public MultilingualG2POptions(
            G2POptions? japaneseOptions = null,
            EnglishG2POptions? englishOptions = null,
            ChineseG2POptions? chineseOptions = null,
            Language defaultCjkLanguage = Language.Japanese,
            string segmentSeparator = " ",
            SpanishG2POptions? spanishOptions = null,
            Language defaultLatinLanguage = Language.English,
            FrenchG2POptions? frenchOptions = null)
        {
            if (defaultLatinLanguage != Language.English && defaultLatinLanguage != Language.Spanish && defaultLatinLanguage != Language.French)
                throw new ArgumentOutOfRangeException(nameof(defaultLatinLanguage), "DefaultLatinLanguage must be English, Spanish, or French.");

            JapaneseOptions = japaneseOptions;
            EnglishOptions = englishOptions;
            ChineseOptions = chineseOptions;
            SpanishOptions = spanishOptions;
            FrenchOptions = frenchOptions;
            DefaultCjkLanguage = defaultCjkLanguage;
            DefaultLatinLanguage = defaultLatinLanguage;
            SegmentSeparator = segmentSeparator;
        }
    }
}
