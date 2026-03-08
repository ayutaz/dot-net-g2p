namespace DotNetG2P.Spanish.Rules
{
    internal static class SpanishOrthography
    {
        public static bool IsPronouncedVowel(string word, int index)
        {
            var c = char.ToLowerInvariant(word[index]);
            if (c == 'y')
                return word.Length == 1 || index == word.Length - 1;

            if (c == 'ü' && index > 0 && index < word.Length - 1)
            {
                var prev = char.ToLowerInvariant(word[index - 1]);
                var next = char.ToLowerInvariant(word[index + 1]);
                if (prev == 'g' && (next == 'e' || next == 'é' || next == 'i' || next == 'í'))
                    return false;
            }

            if (!IsVowelChar(c))
                return false;

            return !IsSilentU(word, index);
        }

        public static bool IsStrongVowel(char c)
        {
            c = char.ToLowerInvariant(c);
            return c == 'a' || c == 'á' || c == 'e' || c == 'é' || c == 'o' || c == 'ó' || c == 'í' || c == 'ú';
        }

        public static bool IsWeakUnaccentedVowel(char c)
        {
            c = char.ToLowerInvariant(c);
            return c == 'i' || c == 'u' || c == 'ü' || c == 'y';
        }

        public static bool IsVowelChar(char c)
        {
            c = char.ToLowerInvariant(c);
            return c == 'a' || c == 'á'
                || c == 'e' || c == 'é'
                || c == 'i' || c == 'í'
                || c == 'o' || c == 'ó'
                || c == 'u' || c == 'ú'
                || c == 'ü';
        }

        public static bool HasWrittenAccent(char c)
        {
            c = char.ToLowerInvariant(c);
            return c == 'á' || c == 'é' || c == 'í' || c == 'ó' || c == 'ú';
        }

        public static bool IsPronouncedVowelChar(char c)
        {
            c = char.ToLowerInvariant(c);
            return IsVowelChar(c) || c == 'y';
        }

        public static bool CanFormDiphthong(char left, char right)
        {
            left = char.ToLowerInvariant(left);
            right = char.ToLowerInvariant(right);

            if (!IsPronouncedVowelChar(left) || !IsPronouncedVowelChar(right))
                return false;

            if ((left == 'í' || left == 'ú') || (right == 'í' || right == 'ú'))
                return false;

            return !(IsStrongVowel(left) && IsStrongVowel(right));
        }

        public static bool CanFormTriphthong(char first, char second, char third)
        {
            return IsWeakUnaccentedVowel(first)
                && IsStrongVowel(second)
                && IsWeakUnaccentedVowel(third);
        }

        private static bool IsSilentU(string word, int index)
        {
            var c = char.ToLowerInvariant(word[index]);
            if (c != 'u' || index == 0 || index == word.Length - 1)
                return false;

            var prev = char.ToLowerInvariant(word[index - 1]);
            var next = char.ToLowerInvariant(word[index + 1]);
            var softNext = next == 'e' || next == 'é' || next == 'i' || next == 'í';

            return softNext && (prev == 'q' || prev == 'g');
        }
    }
}
