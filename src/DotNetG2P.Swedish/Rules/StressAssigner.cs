using System.Collections.Generic;

namespace DotNetG2P.Swedish.Rules
{
    /// <summary>
    /// スウェーデン語の強勢位置決定。
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

        private static int GetStressedSyllableIndex(
            string word, IReadOnlyList<SwedishSyllable> syllables)
        {
            if (syllables.Count == 1)
                return 0;

            var lower = word.ToLowerInvariant();

            // 外来語接尾辞チェック（最終音節にストレス）
            if (lower.EndsWith("tion") || lower.EndsWith("sion")
                || lower.EndsWith("ell") || lower.EndsWith("ent")
                || lower.EndsWith("ang") || lower.EndsWith("ik"))
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
