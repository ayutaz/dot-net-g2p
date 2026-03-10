using System;

namespace DotNetG2P.Chinese
{
    internal static class EmbeddedChineseDictionaryCache
    {
        private static readonly Lazy<PinyinCharDictionary> s_charDictionary =
            new Lazy<PinyinCharDictionary>(PinyinCharDictionary.LoadEmbedded);

        private static readonly Lazy<PinyinPhraseDictionary> s_phraseDictionary =
            new Lazy<PinyinPhraseDictionary>(PinyinPhraseDictionary.LoadEmbedded);

        public static PinyinCharDictionary CharDictionary => s_charDictionary.Value;

        public static PinyinPhraseDictionary PhraseDictionary => s_phraseDictionary.Value;

        public static PinyinCharDictionary? TryGetCharDictionary()
        {
            try
            {
                return s_charDictionary.Value;
            }
            catch
            {
                return null;
            }
        }

        public static PinyinPhraseDictionary? TryGetPhraseDictionary()
        {
            try
            {
                return s_phraseDictionary.Value;
            }
            catch
            {
                return null;
            }
        }
    }
}
