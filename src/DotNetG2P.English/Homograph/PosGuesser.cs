using System;
using System.Collections.Generic;

namespace DotNetG2P.English.Homograph
{
    /// <summary>
    /// 軽量品詞推定器。接尾辞ルールと文脈ルールにより品詞を推定する。
    /// 同綴異音語解決に必要最低限の精度を目標とする。
    /// </summary>
    internal static class PosGuesser
    {
        // -ing で終わるが名詞として扱う例外語
        private static readonly HashSet<string> IngNounExceptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "thing", "king", "ring", "spring", "string", "sing", "bring", "swing",
            "sting", "cling", "wing", "fling", "ceiling", "evening", "morning",
            "nothing", "something", "anything", "everything", "pudding", "building",
            "wedding", "clothing", "feeling", "meeting", "warning", "beginning",
            "meaning", "blessing", "offering", "setting", "painting", "writing",
            "reading", "finding", "ending", "opening", "crossing", "bearing",
            "being", "seeing", "living", "fishing", "mining", "banking", "parking",
            "shopping", "hunting", "boxing", "sewing", "hearing", "nursing",
            "lighting", "cutting", "surfing", "spelling"
        };

        // 文脈ルール: 前の単語 → 推定POS
        // 冠詞・所有格・指示詞・数量詞・形容詞的修飾 → Noun
        private static readonly HashSet<string> NounContextWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // 冠詞
            "a", "an", "the",
            // 所有格
            "my", "your", "his", "her", "its", "our", "their",
            // 指示詞
            "this", "that", "these", "those",
            // 数量詞
            "some", "any", "no", "every", "each", "many", "few", "several", "all", "both",
            // 形容詞的修飾
            "new", "old", "big", "small", "good", "bad", "great", "long", "short",
            "high", "low", "large", "little", "young"
        };

        // "to", 助動詞, "please", "not", 代名詞主格 → Verb
        private static readonly HashSet<string> VerbContextWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "to",
            "will", "would", "can", "could", "shall", "should", "may", "might", "must",
            "do", "does", "did",
            "please", "not",
            "I", "you", "he", "she", "it", "we", "they"
        };

        // 程度副詞 → Adjective
        private static readonly HashSet<string> AdjectiveContextWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "very", "quite", "rather", "so", "too", "most", "more", "less",
            "really", "extremely", "incredibly"
        };

        /// <summary>
        /// 単語列中の指定位置の単語のPOSを推定する。
        /// </summary>
        /// <param name="words">単語列</param>
        /// <param name="index">推定対象のインデックス</param>
        /// <returns>推定された品詞タグ</returns>
        public static PosTag Guess(string[] words, int index)
        {
            if (words == null || index < 0 || index >= words.Length)
                return PosTag.Unknown;

            // 1. 文脈ルール（優先）
            var contextPos = GuessByContext(words, index);
            if (contextPos != PosTag.Unknown)
                return contextPos;

            // 2. 接尾辞ルール
            return GuessBySuffix(words[index]);
        }

        /// <summary>
        /// 前の単語から文脈ベースでPOSを推定する。
        /// </summary>
        private static PosTag GuessByContext(string[] words, int index)
        {
            if (index == 0)
                return PosTag.Unknown;

            var prev = words[index - 1];

            if (NounContextWords.Contains(prev))
                return PosTag.Noun;

            if (VerbContextWords.Contains(prev))
                return PosTag.Verb;

            if (AdjectiveContextWords.Contains(prev))
                return PosTag.Adjective;

            return PosTag.Unknown;
        }

        /// <summary>
        /// 接尾辞ルールでPOSを推定する。
        /// </summary>
        private static PosTag GuessBySuffix(string word)
        {
            if (string.IsNullOrEmpty(word))
                return PosTag.Unknown;

            // -ing: Verb (例外は Noun)
            if (word.EndsWith("ing", StringComparison.OrdinalIgnoreCase))
            {
                return IngNounExceptions.Contains(word) ? PosTag.Noun : PosTag.Verb;
            }

            // -tion, -sion: Noun
            if (word.EndsWith("tion", StringComparison.OrdinalIgnoreCase) ||
                word.EndsWith("sion", StringComparison.OrdinalIgnoreCase))
                return PosTag.Noun;

            // -ment: Noun
            if (word.EndsWith("ment", StringComparison.OrdinalIgnoreCase))
                return PosTag.Noun;

            // -ness: Noun
            if (word.EndsWith("ness", StringComparison.OrdinalIgnoreCase))
                return PosTag.Noun;

            // -ity: Noun
            if (word.EndsWith("ity", StringComparison.OrdinalIgnoreCase))
                return PosTag.Noun;

            // -ance, -ence: Noun
            if (word.EndsWith("ance", StringComparison.OrdinalIgnoreCase) ||
                word.EndsWith("ence", StringComparison.OrdinalIgnoreCase))
                return PosTag.Noun;

            // -ism: Noun
            if (word.EndsWith("ism", StringComparison.OrdinalIgnoreCase))
                return PosTag.Noun;

            // -ist: Noun
            if (word.EndsWith("ist", StringComparison.OrdinalIgnoreCase))
                return PosTag.Noun;

            // -er: Noun (3文字以下は除外)
            if (word.Length > 3 && word.EndsWith("er", StringComparison.OrdinalIgnoreCase))
                return PosTag.Noun;

            // -or: Noun (3文字以下は除外)
            if (word.Length > 3 && word.EndsWith("or", StringComparison.OrdinalIgnoreCase))
                return PosTag.Noun;

            // -ed: Verb (2文字の "ed" 自体は除外)
            if (word.Length > 2 && word.EndsWith("ed", StringComparison.OrdinalIgnoreCase))
                return PosTag.Verb;

            // -ly: Adverb
            if (word.EndsWith("ly", StringComparison.OrdinalIgnoreCase))
                return PosTag.Adverb;

            // -ful: Adjective
            if (word.EndsWith("ful", StringComparison.OrdinalIgnoreCase))
                return PosTag.Adjective;

            // -ous: Adjective
            if (word.EndsWith("ous", StringComparison.OrdinalIgnoreCase))
                return PosTag.Adjective;

            // -ive: Adjective ("live" は除外)
            if (word.EndsWith("ive", StringComparison.OrdinalIgnoreCase) &&
                !word.Equals("live", StringComparison.OrdinalIgnoreCase))
                return PosTag.Adjective;

            // -able, -ible: Adjective
            if (word.EndsWith("able", StringComparison.OrdinalIgnoreCase) ||
                word.EndsWith("ible", StringComparison.OrdinalIgnoreCase))
                return PosTag.Adjective;

            // -ical: Adjective (先に -ical をチェックし -al より優先)
            if (word.EndsWith("ical", StringComparison.OrdinalIgnoreCase))
                return PosTag.Adjective;

            // -ial: Adjective
            if (word.EndsWith("ial", StringComparison.OrdinalIgnoreCase))
                return PosTag.Adjective;

            // -al: Adjective (3文字以下は除外)
            if (word.Length > 3 && word.EndsWith("al", StringComparison.OrdinalIgnoreCase))
                return PosTag.Adjective;

            // -est: Adjective (3文字以下は除外)
            if (word.Length > 3 && word.EndsWith("est", StringComparison.OrdinalIgnoreCase))
                return PosTag.Adjective;

            // -less: Adjective
            if (word.EndsWith("less", StringComparison.OrdinalIgnoreCase))
                return PosTag.Adjective;

            return PosTag.Unknown;
        }
    }
}
