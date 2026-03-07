#nullable enable

using DotNetG2P.Chinese;
using DotNetG2P.English;

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

        /// <summary>CJK漢字のデフォルト言語（周囲にかな文字がない場合に使用、デフォルト: Japanese）。</summary>
        public Language DefaultCjkLanguage { get; }

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
        public MultilingualG2POptions(
            G2POptions? japaneseOptions = null,
            EnglishG2POptions? englishOptions = null,
            ChineseG2POptions? chineseOptions = null,
            Language defaultCjkLanguage = Language.Japanese,
            string segmentSeparator = " ")
        {
            JapaneseOptions = japaneseOptions;
            EnglishOptions = englishOptions;
            ChineseOptions = chineseOptions;
            DefaultCjkLanguage = defaultCjkLanguage;
            SegmentSeparator = segmentSeparator;
        }
    }
}
