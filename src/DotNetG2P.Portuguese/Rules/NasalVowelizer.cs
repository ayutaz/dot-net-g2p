using System.Runtime.CompilerServices;

namespace DotNetG2P.Portuguese.Rules
{
    /// <summary>
    /// ポルトガル語の鼻母音化判定ロジック。
    /// チルダ付き母音、語末鼻二重母音、母音+n/m+子音/語末パターンから
    /// 鼻母音化の可否を決定し、対応する音素列を返す。
    /// </summary>
    internal static class NasalVowelizer
    {
        /// <summary>
        /// 鼻母音化を試みる。成功した場合、出力音素と消費文字数を返す。
        /// </summary>
        /// <param name="word">対象の単語（小文字化済み）</param>
        /// <param name="index">現在の文字インデックス</param>
        /// <param name="isWordFinal">現在位置が語末近傍かどうか（残りの文字が鼻母音パターンで消費しきれる場合 true）</param>
        /// <param name="isStressed">現在の音節にストレスがあるかどうか</param>
        /// <param name="phonemes">出力音素配列（鼻母音 or 鼻二重母音）</param>
        /// <param name="charsConsumed">消費した文字数</param>
        /// <returns>鼻母音化した場合は true</returns>
        internal static bool TryNasalize(
            string word, int index, bool isWordFinal, bool isStressed,
            out PortugueseIpaPhoneme[] phonemes, out int charsConsumed)
        {
            phonemes = null!;
            charsConsumed = 0;

            if (index >= word.Length)
                return false;

            var c = char.ToLowerInvariant(word[index]);

            // --- 1. チルダ付き母音（常に鼻母音化、最優先） ---
            if (c == '\u00E3') // ã
            {
                return TryNasalizeTilde(word, index, PortugueseIpaPhoneme.ANasal, out phonemes, out charsConsumed);
            }

            if (c == '\u00F5') // õ
            {
                return TryNasalizeTilde(word, index, PortugueseIpaPhoneme.ONasal, out phonemes, out charsConsumed);
            }

            // --- 2, 3. 母音 + n/m パターン ---
            if (!IsNasalizableVowel(c))
                return false;

            // 母音の次が n/m であることを確認
            if (index + 1 >= word.Length)
                return false;

            var next = char.ToLowerInvariant(word[index + 1]);
            if (!IsNasalConsonant(next))
                return false;

            // 語末判定: index+2 が語末 or index+2 以降が語末パターンに収まるか
            var afterNasal = index + 2;
            var isAtEnd = afterNasal >= word.Length;

            // --- 2. 語末鼻二重母音 ---
            if (isAtEnd || IsWordFinalNasalDiphthong(word, index, afterNasal))
            {
                return TryWordFinalNasalDiphthong(word, index, c, next, isStressed, out phonemes, out charsConsumed);
            }

            // --- 3. 母音 + n/m + 子音 → 鼻母音化 ---
            if (afterNasal < word.Length)
            {
                var afterNasalChar = char.ToLowerInvariant(word[afterNasal]);

                // 後続が母音 → 非鼻母音化（n/m は onset として次の音節に属する）
                if (PortugueseOrthography.IsVowel(afterNasalChar))
                    return false;

                // 後続が同じ鼻子音（nn, mm） → 非鼻母音化
                if (afterNasalChar == next)
                    return false;

                // 後続が子音 → 鼻母音化
                phonemes = new[] { GetNasalVowel(c) };
                charsConsumed = 2;
                return true;
            }

            return false;
        }

        /// <summary>
        /// チルダ付き母音の鼻母音化処理。
        /// 鼻二重母音パターン（ão, ãe/ãi, õe）もチェックする。
        /// </summary>
        private static bool TryNasalizeTilde(
            string word, int index, PortugueseIpaPhoneme baseNasal,
            out PortugueseIpaPhoneme[] phonemes, out int charsConsumed)
        {
            // 後続文字で鼻二重母音パターンをチェック
            if (index + 1 < word.Length)
            {
                var next = char.ToLowerInvariant(word[index + 1]);

                if (baseNasal == PortugueseIpaPhoneme.ANasal)
                {
                    // ão → [ANasal, WNasal]
                    if (next == 'o')
                    {
                        // ões → [ANasal, WNasal] ではなく ão の処理のみ
                        phonemes = new[] { PortugueseIpaPhoneme.ANasal, PortugueseIpaPhoneme.WNasal };
                        charsConsumed = 2;
                        return true;
                    }

                    // ãe / ãi → [ANasal, JNasal]
                    if (next == 'e' || next == 'i')
                    {
                        // ães → [ANasal, JNasal, S]
                        if (next == 'e' && index + 2 < word.Length && char.ToLowerInvariant(word[index + 2]) == 's'
                            && index + 3 >= word.Length)
                        {
                            phonemes = new[] { PortugueseIpaPhoneme.ANasal, PortugueseIpaPhoneme.JNasal, PortugueseIpaPhoneme.S };
                            charsConsumed = 3;
                            return true;
                        }

                        phonemes = new[] { PortugueseIpaPhoneme.ANasal, PortugueseIpaPhoneme.JNasal };
                        charsConsumed = 2;
                        return true;
                    }
                }
                else if (baseNasal == PortugueseIpaPhoneme.ONasal)
                {
                    // õe → [ONasal, JNasal]
                    if (next == 'e')
                    {
                        // ões → [ONasal, JNasal, S]
                        if (index + 2 < word.Length && char.ToLowerInvariant(word[index + 2]) == 's'
                            && index + 3 >= word.Length)
                        {
                            phonemes = new[] { PortugueseIpaPhoneme.ONasal, PortugueseIpaPhoneme.JNasal, PortugueseIpaPhoneme.S };
                            charsConsumed = 3;
                            return true;
                        }

                        phonemes = new[] { PortugueseIpaPhoneme.ONasal, PortugueseIpaPhoneme.JNasal };
                        charsConsumed = 2;
                        return true;
                    }
                }
            }

            // 単独チルダ母音 → 単純鼻母音
            phonemes = new[] { baseNasal };
            charsConsumed = 1;
            return true;
        }

        /// <summary>
        /// 語末鼻二重母音パターンのマッチ判定（語末位置か、語末+s のみか）。
        /// </summary>
        private static bool IsWordFinalNasalDiphthong(string word, int index, int afterNasal)
        {
            // index+2 が語末 → 語末 am/em/om 等
            if (afterNasal >= word.Length)
                return true;

            // index+2 が 's' で index+3 が語末 → ens パターン
            if (char.ToLowerInvariant(word[afterNasal]) == 's' && afterNasal + 1 >= word.Length)
                return true;

            return false;
        }

        /// <summary>
        /// 語末の鼻二重母音処理。
        /// am(語末)→[ANasal,WNasal], em(語末)→[ENasal,JNasal], ens→[ENasal,JNasal,S],
        /// om(語末,強勢)→[ONasal,WNasal]
        /// </summary>
        private static bool TryWordFinalNasalDiphthong(
            string word, int index, char vowel, char nasal,
            bool isStressed,
            out PortugueseIpaPhoneme[] phonemes, out int charsConsumed)
        {
            phonemes = null!;
            charsConsumed = 0;

            var afterNasal = index + 2;
            var isAtEnd = afterNasal >= word.Length;
            var hasTrailingS = !isAtEnd
                && afterNasal < word.Length
                && char.ToLowerInvariant(word[afterNasal]) == 's'
                && afterNasal + 1 >= word.Length;

            var baseVowel = PortugueseOrthography.StripAccent(vowel);

            switch (baseVowel)
            {
                case 'a':
                    if (isAtEnd)
                    {
                        // am(語末) → [ANasal, WNasal]
                        phonemes = new[] { PortugueseIpaPhoneme.ANasal, PortugueseIpaPhoneme.WNasal };
                        charsConsumed = 2;
                        return true;
                    }
                    break;

                case 'e':
                    if (isAtEnd)
                    {
                        // em(語末) → [ENasal, JNasal]
                        phonemes = new[] { PortugueseIpaPhoneme.ENasal, PortugueseIpaPhoneme.JNasal };
                        charsConsumed = 2;
                        return true;
                    }
                    if (hasTrailingS)
                    {
                        // ens(語末) → [ENasal, JNasal, S]
                        phonemes = new[] { PortugueseIpaPhoneme.ENasal, PortugueseIpaPhoneme.JNasal, PortugueseIpaPhoneme.S };
                        charsConsumed = 3;
                        return true;
                    }
                    break;

                case 'o':
                    if (isAtEnd && isStressed)
                    {
                        // om(語末,強勢) → [ONasal, WNasal]
                        phonemes = new[] { PortugueseIpaPhoneme.ONasal, PortugueseIpaPhoneme.WNasal };
                        charsConsumed = 2;
                        return true;
                    }
                    if (isAtEnd && !isStressed)
                    {
                        // om(語末,非強勢) → 単純鼻母音 [ONasal]
                        phonemes = new[] { PortugueseIpaPhoneme.ONasal };
                        charsConsumed = 2;
                        return true;
                    }
                    break;

                case 'i':
                    if (isAtEnd)
                    {
                        // im(語末) → [INasal]
                        phonemes = new[] { PortugueseIpaPhoneme.INasal };
                        charsConsumed = 2;
                        return true;
                    }
                    break;

                case 'u':
                    if (isAtEnd)
                    {
                        // um(語末) → [UNasal]
                        phonemes = new[] { PortugueseIpaPhoneme.UNasal };
                        charsConsumed = 2;
                        return true;
                    }
                    break;
            }

            // 語末パターンにマッチしなかった場合、単純鼻母音として処理
            if (isAtEnd)
            {
                phonemes = new[] { GetNasalVowel(vowel) };
                charsConsumed = 2;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 指定された文字が鼻子音（n または m）であるかどうかを判定する。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsNasalConsonant(char c)
        {
            c = char.ToLowerInvariant(c);
            return c == 'n' || c == 'm';
        }

        /// <summary>
        /// 指定された母音文字が鼻母音化可能な母音であるかどうかを判定する。
        /// a, e, i, o, u（アクセント付き含む、ただし ã/õ はチルダとして別途処理）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNasalizableVowel(char c)
        {
            c = char.ToLowerInvariant(c);
            switch (c)
            {
                case 'a': case 'e': case 'i': case 'o': case 'u':
                case '\u00E1': // á
                case '\u00E0': // à
                case '\u00E2': // â
                case '\u00E9': // é
                case '\u00EA': // ê
                case '\u00ED': // í
                case '\u00F3': // ó
                case '\u00F4': // ô
                case '\u00FA': // ú
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 母音文字から対応する鼻母音音素を返す。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static PortugueseIpaPhoneme GetNasalVowel(char vowel)
        {
            var baseVowel = PortugueseOrthography.StripAccent(char.ToLowerInvariant(vowel));
            switch (baseVowel)
            {
                case 'a': return PortugueseIpaPhoneme.ANasal;
                case 'e': return PortugueseIpaPhoneme.ENasal;
                case 'i': return PortugueseIpaPhoneme.INasal;
                case 'o': return PortugueseIpaPhoneme.ONasal;
                case 'u': return PortugueseIpaPhoneme.UNasal;
                default: return PortugueseIpaPhoneme.ANasal;
            }
        }
    }
}
