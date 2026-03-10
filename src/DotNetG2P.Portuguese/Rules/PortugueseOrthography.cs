using System.Runtime.CompilerServices;

namespace DotNetG2P.Portuguese.Rules
{
    /// <summary>
    /// ポルトガル語の正書法ユーティリティ。母音/子音判定、アクセント記号処理、
    /// ダイグラフ判定、二重母音/離音判定等を提供する。
    /// </summary>
    internal static class PortugueseOrthography
    {
        /// <summary>
        /// 指定された文字がポルトガル語の母音字（アクセント付きを含む）であるかどうかを判定する。
        /// a,e,i,o,u + ã,õ,â,ê,ô,á,é,í,ó,ú,à,ü
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVowel(char c)
        {
            c = char.ToLowerInvariant(c);
            switch (c)
            {
                case 'a': case 'e': case 'i': case 'o': case 'u':
                case '\u00E1': // á
                case '\u00E0': // à
                case '\u00E2': // â
                case '\u00E3': // ã
                case '\u00E9': // é
                case '\u00EA': // ê
                case '\u00ED': // í
                case '\u00F3': // ó
                case '\u00F4': // ô
                case '\u00F5': // õ
                case '\u00FA': // ú
                case '\u00FC': // ü
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 指定された文字が強母音（a, e, o およびそのアクセント変種）であるかどうかを判定する。
        /// 強母音同士が隣接すると離音(hiatus)を形成する。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsStrongVowel(char c)
        {
            c = char.ToLowerInvariant(c);
            switch (c)
            {
                case 'a': case 'e': case 'o':
                case '\u00E1': // á
                case '\u00E0': // à
                case '\u00E2': // â
                case '\u00E9': // é
                case '\u00EA': // ê
                case '\u00F3': // ó
                case '\u00F4': // ô
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 指定された文字が弱母音（アクセントなしの i, u）であるかどうかを判定する。
        /// アクセント付きの í, ú は hiatus を形成するため false を返す。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWeakVowel(char c)
        {
            c = char.ToLowerInvariant(c);
            return c == 'i' || c == 'u';
        }

        /// <summary>
        /// 指定された文字が鋭アクセント（acento agudo）付きであるかどうかを判定する。
        /// á, é, í, ó, ú
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAcuteAccent(char c)
        {
            c = char.ToLowerInvariant(c);
            return c == '\u00E1' || c == '\u00E9' || c == '\u00ED'
                || c == '\u00F3' || c == '\u00FA';
        }

        /// <summary>
        /// 指定された文字が曲折アクセント（acento circunflexo）付きであるかどうかを判定する。
        /// â, ê, ô
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasCircumflexAccent(char c)
        {
            c = char.ToLowerInvariant(c);
            return c == '\u00E2' || c == '\u00EA' || c == '\u00F4';
        }

        /// <summary>
        /// 指定された文字がチルダ（til）付きであるかどうかを判定する。
        /// ã, õ
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasTilde(char c)
        {
            c = char.ToLowerInvariant(c);
            return c == '\u00E3' || c == '\u00F5';
        }

        /// <summary>
        /// 指定された文字がグレイヴアクセント（acento grave / crase）付きであるかどうかを判定する。
        /// à
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasGraveAccent(char c)
        {
            c = char.ToLowerInvariant(c);
            return c == '\u00E0';
        }

        /// <summary>
        /// 指定された文字がいずれかのアクセント記号付きであるかどうかを判定する。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAnyAccent(char c)
        {
            return HasAcuteAccent(c) || HasCircumflexAccent(c) || HasTilde(c) || HasGraveAccent(c);
        }

        /// <summary>
        /// アクセント記号を除去し、基底文字を返す。
        /// á,â,ã,à→a, é,ê→e, í→i, ó,ô,õ→o, ú,ü→u, それ以外→そのまま
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char StripAccent(char c)
        {
            var lower = char.ToLowerInvariant(c);
            char result;
            switch (lower)
            {
                case '\u00E1': // á
                case '\u00E0': // à
                case '\u00E2': // â
                case '\u00E3': // ã
                    result = 'a';
                    break;
                case '\u00E9': // é
                case '\u00EA': // ê
                    result = 'e';
                    break;
                case '\u00ED': // í
                    result = 'i';
                    break;
                case '\u00F3': // ó
                case '\u00F4': // ô
                case '\u00F5': // õ
                    result = 'o';
                    break;
                case '\u00FA': // ú
                case '\u00FC': // ü
                    result = 'u';
                    break;
                default:
                    return c;
            }

            return char.IsUpper(c) ? char.ToUpperInvariant(result) : result;
        }

        /// <summary>
        /// 指定された文字がポルトガル語の子音字であるかどうかを判定する。
        /// アルファベットかつ非母音の場合 true。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConsonant(char c)
        {
            c = char.ToLowerInvariant(c);
            switch (c)
            {
                case 'b': case 'c': case 'd': case 'f': case 'g':
                case 'h': case 'j': case 'k': case 'l': case 'm':
                case 'n': case 'p': case 'q': case 'r': case 's':
                case 't': case 'v': case 'w': case 'x': case 'z':
                case '\u00E7': // ç
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 指定された文字が前舌母音字（c/g の軟音化判定用）であるかどうかを判定する。
        /// e, i, é, ê, í
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFrontVowel(char c)
        {
            c = char.ToLowerInvariant(c);
            return c == 'e' || c == 'i'
                || c == '\u00E9' // é
                || c == '\u00EA' // ê
                || c == '\u00ED'; // í
        }

        /// <summary>
        /// 文字列中のすべてのアクセント記号を除去し、基底文字に変換した文字列を返す。
        /// ストレス判定後にアクセントなしの語形が必要な場合に使用する。
        /// </summary>
        public static string RemoveAccentMarks(string word)
        {
            if (string.IsNullOrEmpty(word))
                return word;

            // 高速パス: アクセント文字が含まれていなければそのまま返す
            var hasAccent = false;
            for (var i = 0; i < word.Length; i++)
            {
                if (StripAccent(word[i]) != word[i])
                {
                    hasAccent = true;
                    break;
                }
            }

            if (!hasAccent)
                return word;

            var chars = new char[word.Length];
            for (var i = 0; i < word.Length; i++)
                chars[i] = StripAccent(word[i]);

            return new string(chars);
        }

        /// <summary>
        /// 指定された位置がポルトガル語のダイグラフ（2文字組）であるかどうかを判定する。
        /// ch, lh, nh, rr, ss, qu, gu
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDigraph(string word, int index)
        {
            if (index + 1 >= word.Length)
                return false;

            var c1 = char.ToLowerInvariant(word[index]);
            var c2 = char.ToLowerInvariant(word[index + 1]);

            switch (c1)
            {
                case 'c': return c2 == 'h';
                case 'l': return c2 == 'h';
                case 'n': return c2 == 'h';
                case 'r': return c2 == 'r';
                case 's': return c2 == 's';
                case 'q': return c2 == 'u';
                case 'g': return c2 == 'u';
                default: return false;
            }
        }

        /// <summary>
        /// 2つの母音が二重母音（ditongo）を形成できるかどうかを判定する。
        /// 弱+強、強+弱、弱+弱 → true。強+強 → false。
        /// ただしアクセント付き í/ú は hiatus を形成するため false。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanFormDiphthong(char v1, char v2)
        {
            v1 = char.ToLowerInvariant(v1);
            v2 = char.ToLowerInvariant(v2);

            if (!IsVowel(v1) || !IsVowel(v2))
                return false;

            // アクセント付き í/ú は hiatus を形成する
            if (v1 == '\u00ED' || v1 == '\u00FA' || v2 == '\u00ED' || v2 == '\u00FA')
                return false;

            // 強+強 は二重母音にならない
            return !(IsStrongVowel(v1) && IsStrongVowel(v2));
        }

        /// <summary>
        /// 3つの母音が三重母音（tritongo）を形成できるかどうかを判定する。
        /// 弱+強+弱 の組み合わせのみ三重母音を形成する。
        /// 例: Uruguai (u-a-i), Paraguai (a-i), quais (a-i)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanFormTriphthong(char v1, char v2, char v3)
        {
            return IsWeakVowel(char.ToLowerInvariant(v1))
                && IsStrongVowel(char.ToLowerInvariant(v2))
                && IsWeakVowel(char.ToLowerInvariant(v3));
        }

        /// <summary>
        /// 2つの母音が離音（hiato）を形成するかどうかを判定する。
        /// 強+強 → true、アクセント付き í/ú + 他 → true、同一母音 → true。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsHiatus(char v1, char v2)
        {
            v1 = char.ToLowerInvariant(v1);
            v2 = char.ToLowerInvariant(v2);

            if (!IsVowel(v1) || !IsVowel(v2))
                return false;

            // アクセント付き í/ú は常に hiatus
            if (v1 == '\u00ED' || v1 == '\u00FA' || v2 == '\u00ED' || v2 == '\u00FA')
                return true;

            // 同一母音（基底文字で比較）
            if (StripAccent(v1) == StripAccent(v2))
                return true;

            // 強+強 は hiatus
            return IsStrongVowel(v1) && IsStrongVowel(v2);
        }

        /// <summary>
        /// 2つの母音が離音（hiato）を形成するかどうかを判定する。
        /// v2HasAccent が true の場合、v2 を強制的にアクセント付き弱母音として扱い hiatus を形成する。
        /// ストレス割り当て後に、綴りにはアクセント記号がない弱母音がストレスを持つケースで使用する。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsHiatus(char v1, char v2, bool v2HasAccent)
        {
            if (v2HasAccent)
            {
                var v2Lower = char.ToLowerInvariant(v2);
                // v2 がアクセント付き弱母音扱い → hiatus
                if (v2Lower == 'i' || v2Lower == 'u')
                    return IsVowel(v1);
            }

            return IsHiatus(v1, v2);
        }

        /// <summary>
        /// 指定された位置の u が黙字であるかどうかを判定する。
        /// qu/gu + 前舌母音 のときの u は発音しない。
        /// ただし ü（旧正書法）は発音するため false。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSilentU(string word, int index)
        {
            var c = char.ToLowerInvariant(word[index]);

            // ü は旧正書法で発音する u を示す → 黙字ではない
            if (c == '\u00FC') // ü
                return false;

            if (c != 'u' || index == 0 || index + 1 >= word.Length)
                return false;

            var prev = char.ToLowerInvariant(word[index - 1]);
            if (prev != 'q' && prev != 'g')
                return false;

            return IsFrontVowel(word[index + 1]);
        }
    }
}
