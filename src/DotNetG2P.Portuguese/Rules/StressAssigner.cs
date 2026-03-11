using System;
using System.Collections.Generic;

namespace DotNetG2P.Portuguese.Rules
{
    /// <summary>
    /// ポルトガル語の強勢位置決定。
    /// 3フェーズアルゴリズム:
    /// Phase 1: 鋭/曲折アクセント (á,é,í,ó,ú,â,ê,ô) → 最優先
    /// Phase 2: チルダ (ã,õ) → Phase 1 未検出時のみ
    /// Phase 3: デフォルトストレス → 語末パターンに基づく判定
    /// </summary>
    internal static class StressAssigner
    {
        /// <summary>
        /// 強勢音節のインデックスを返す。音節が空の場合は -1 を返す。
        /// </summary>
        public static int GetStressedSyllableIndex(string word, IReadOnlyList<PortugueseSyllable> syllables)
        {
            if (string.IsNullOrEmpty(word) || syllables.Count == 0)
                return -1;

            // Phase 1: 鋭/曲折アクセント検索（最優先）
            for (var i = 0; i < syllables.Count; i++)
            {
                var syllable = syllables[i];
                for (var j = 0; j < syllable.Length; j++)
                {
                    var c = word[syllable.StartIndex + j];
                    if (PortugueseOrthography.HasAcuteAccent(c) || PortugueseOrthography.HasCircumflexAccent(c))
                        return i;
                }
            }

            // Phase 2: チルダ検索（Phase 1 で見つからなかった場合のみ）
            for (var i = 0; i < syllables.Count; i++)
            {
                var syllable = syllables[i];
                for (var j = 0; j < syllable.Length; j++)
                {
                    var c = word[syllable.StartIndex + j];
                    if (PortugueseOrthography.HasTilde(c))
                        return i;
                }
            }

            // Phase 3: デフォルトストレス（アクセント記号なし）
            if (syllables.Count == 1)
                return 0;

            // 統一的ルール: 語末から -s, -ns, -m を除去した後、a/e/o で終わるなら paroxytone
            var lower = word.ToLowerInvariant();

            // Paroxytone がデフォルトの語末パターン:
            // -a(s), -e(s), -o(s), -am, -em, -ens
            if (EndsWithParoxytonePattern(lower))
                return syllables.Count - 2;

            // それ以外は Oxytone（最終音節）
            return syllables.Count - 1;
        }

        /// <summary>
        /// GetStressedSyllableIndex の結果に基づき、IsStressed を設定した新しい音節リストを返す。
        /// </summary>
        public static IReadOnlyList<PortugueseSyllable> MarkStress(
            string word, IReadOnlyList<PortugueseSyllable> syllables)
        {
            if (syllables.Count == 0)
                return syllables;

            var stressed = GetStressedSyllableIndex(word, syllables);
            var result = new PortugueseSyllable[syllables.Count];
            for (var i = 0; i < syllables.Count; i++)
            {
                var syllable = syllables[i];
                result[i] = new PortugueseSyllable(syllable.StartIndex, syllable.Length, syllable.Text, i == stressed);
            }

            return result;
        }

        /// <summary>
        /// 語末が paroxytone デフォルトパターンに該当するかどうかを判定する。
        /// -a, -e, -o, -as, -es, -os, -am, -em, -ens
        /// </summary>
        private static bool EndsWithParoxytonePattern(string lower)
        {
            if (lower.Length == 0)
                return false;

            var last = lower[lower.Length - 1];

            // -a, -e, -o
            if (last == 'a' || last == 'e' || last == 'o')
                return true;

            if (lower.Length >= 2)
            {
                var secondLast = lower[lower.Length - 2];

                // -as, -es, -os
                if (last == 's' && (secondLast == 'a' || secondLast == 'e' || secondLast == 'o'))
                    return true;

                // -am, -em
                if (last == 'm' && (secondLast == 'a' || secondLast == 'e'))
                    return true;
            }

            // -ens
            if (lower.Length >= 3 && lower.EndsWith("ens", StringComparison.Ordinal))
                return true;

            return false;
        }
    }
}
