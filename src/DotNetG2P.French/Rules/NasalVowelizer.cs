namespace DotNetG2P.French.Rules
{
    /// <summary>
    /// フランス語の鼻母音化判定ロジック。
    /// 母音 + n/m の後続文字パターンから鼻母音化の可否を決定する。
    /// </summary>
    internal static class NasalVowelizer
    {
        /// <summary>
        /// 母音+鼻子音(n/m)の組み合わせが鼻母音化するかどうかを判定する。
        /// </summary>
        /// <param name="word">対象の単語（小文字化済み）</param>
        /// <param name="vowelIndex">母音のインデックス</param>
        /// <param name="nasalConsonant">鼻子音 ('n' または 'm')</param>
        /// <param name="dialect">方言</param>
        /// <param name="nasalPhoneme">鼻母音化された場合の音素</param>
        /// <param name="charsConsumed">消費された文字数（母音+鼻子音を含む）</param>
        /// <returns>鼻母音化する場合は true</returns>
        public static bool TryNasalize(
            string word,
            int vowelIndex,
            char nasalConsonant,
            FrenchDialect dialect,
            out FrenchIpaPhoneme nasalPhoneme,
            out int charsConsumed)
        {
            nasalPhoneme = default;
            charsConsumed = 0;

            var nasalIndex = vowelIndex + 1;

            // 鼻子音の次の文字を確認
            var afterNasal = nasalIndex + 1;

            // 後続文字が母音 → 非鼻母音化（母音間のn/mは鼻母音化しない）
            if (afterNasal < word.Length && FrenchOrthography.IsVowelChar(word[afterNasal]))
                return false;

            // 後続文字が同じ鼻子音（nn, mm）→ 非鼻母音化
            if (afterNasal < word.Length && char.ToLowerInvariant(word[afterNasal]) == nasalConsonant)
                return false;

            // 後続が h + 母音のパターン → 非鼻母音化（hは透過的で実質母音間）
            if (afterNasal < word.Length && char.ToLowerInvariant(word[afterNasal]) == 'h'
                && afterNasal + 1 < word.Length && FrenchOrthography.IsVowelChar(word[afterNasal + 1]))
                return false;

            // ここに到達 → 鼻母音化する（後続は子音または語末）
            var vowelChar = char.ToLowerInvariant(word[vowelIndex]);
            nasalPhoneme = MapNasalVowel(vowelChar, nasalConsonant, dialect);
            charsConsumed = 2; // 母音 + 鼻子音

            return true;
        }

        /// <summary>
        /// 母音文字と鼻子音の組み合わせから対応する鼻母音音素を返す。
        /// </summary>
        private static FrenchIpaPhoneme MapNasalVowel(char vowel, char nasal, FrenchDialect dialect)
        {
            // 基底母音にマッピングしてから鼻母音を決定
            switch (vowel)
            {
                // a/à/â + n/m → /ɑ̃/ (ANasal)
                case 'a':
                case '\u00E0': // à
                case '\u00E2': // â
                    return FrenchIpaPhoneme.ANasal;

                // e/è/é/ê + n/m → /ɑ̃/ (ANasal) — 'en' は歴史的に /ɑ̃/
                case 'e':
                case '\u00E8': // è
                case '\u00E9': // é
                case '\u00EA': // ê
                    return FrenchIpaPhoneme.ANasal;

                // o/ô + n/m → /ɔ̃/ (ONasal)
                case 'o':
                case '\u00F4': // ô
                    return FrenchIpaPhoneme.ONasal;

                // i/î + n/m → /ɛ̃/ (ENasal)
                case 'i':
                case '\u00EE': // î
                    return FrenchIpaPhoneme.ENasal;

                // u/û + n/m → /ɛ̃/ (Metropolitan) または /œ̃/ (Conservative)
                case 'u':
                case '\u00FB': // û
                    return dialect == FrenchDialect.Conservative
                        ? FrenchIpaPhoneme.OeNasal
                        : FrenchIpaPhoneme.ENasal;

                // y + n/m → /ɛ̃/ (ENasal)
                case 'y':
                    return FrenchIpaPhoneme.ENasal;

                default:
                    return FrenchIpaPhoneme.ANasal;
            }
        }
    }
}
