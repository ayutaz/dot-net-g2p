using System.Collections.Generic;

namespace DotNetG2P.Swedish.Rules
{
    /// <summary>
    /// スウェーデン語の強勢位置決定およびピッチアクセント予測。
    /// </summary>
    internal static class StressAssigner
    {
        internal static IReadOnlyList<SwedishSyllable> MarkStress(
            string word, IReadOnlyList<SwedishSyllable> syllables)
        {
            if (syllables.Count == 0)
                return syllables;

            var stressIndex = GetStressedSyllableIndex(word, syllables);
            var result = new SwedishSyllable[syllables.Count];
            for (var i = 0; i < syllables.Count; i++)
            {
                var s = syllables[i];
                result[i] = new SwedishSyllable(s.StartIndex, s.Length, s.Text, i == stressIndex);
            }
            return result;
        }

        /// <summary>
        /// ピッチアクセント（accent 1/2）を規則ベースで予測する。
        /// 優先順序: 例外辞書→単音節→接尾辞→デフォルト
        /// </summary>
        internal static byte AssignAccent(string word, IReadOnlyList<SwedishSyllable> syllables, byte dictionaryAccent)
        {
            // 1. 例外辞書のaccent情報を優先
            if (dictionaryAccent == 1 || dictionaryAccent == 2)
                return dictionaryAccent;

            // 2. 単音節語 → Accent 1（常に）
            if (syllables.Count <= 1)
                return 1;

            // 3. Accent 2 誘発接尾辞チェック（長い接尾辞を先にマッチ）
            if (HasAccent2Suffix(word))
                return 2;

            // 4. デフォルト → Accent 1
            return 1;
        }

        /// <summary>
        /// Accent 2 を誘発する接尾辞パターンのチェック。
        /// 長い接尾辞から先にマッチし、誤マッチを防ぐ。
        /// </summary>
        private static bool HasAccent2Suffix(string word)
        {
            // 長い接尾辞から先にチェック（-ande/-ende を -de より先に）
            if (word.EndsWith("ande") || word.EndsWith("ende")) return true; // 現在分詞
            if (word.EndsWith("are")) return true; // 行為者 (lärare, arbetare)
            if (word.EndsWith("het")) return true; // 派生名詞 (frihet, storhet)
            if (word.EndsWith("ar")) return true;  // 複数形 (hundar, bilar)
            if (word.EndsWith("or")) return true;  // 複数形 (flickor)
            if (word.EndsWith("te") || word.EndsWith("de")) return true; // 過去形

            // 語幹末尾 -e: 2音節のネイティブ語（外来語 cafe, garage は例外辞書で対応）
            if (word.Length >= 3 && word.Length <= 6 && word.EndsWith("e")
                && !word.EndsWith("tion") && !word.EndsWith("sion"))
                return true;

            return false;
        }

        private static int GetStressedSyllableIndex(
            string word, IReadOnlyList<SwedishSyllable> syllables)
        {
            if (syllables.Count == 1)
                return 0;

            var lower = word.ToLowerInvariant();

            // 外来語接尾辞チェック（最終音節にストレス）
            if (lower.EndsWith("tion") || lower.EndsWith("sion")
                || lower.EndsWith("ell") || lower.EndsWith("ent")
                || lower.EndsWith("ang") || lower.EndsWith("ik")
                || lower.EndsWith("ment") || lower.EndsWith("ans")
                || lower.EndsWith("ance") || lower.EndsWith("\u00f6r")  // -ör (direktör, ingenjör)
                || lower.EndsWith("ist"))                               // -ist (turist, pianist)
            {
                return syllables.Count - 1;
            }

            // 外来語接尾辞（最終音節の1つ前にストレス）
            if (lower.EndsWith("era"))
            {
                return syllables.Count > 2 ? syllables.Count - 2 : syllables.Count - 1;
            }

            // デフォルト: 第1音節（ゲルマン語規則）
            return 0;
        }
    }
}
