using System;
using System.Text;

namespace DotNetG2P.Chinese
{
    /// <summary>ピンイン声調の変換ユーティリティ。</summary>
    internal static class ToneConverter
    {
        // 声調記号付き母音 → (基本母音, 声調番号) のマッピング
        // a系
        private const string TonedA = "āáǎà";
        // e系
        private const string TonedE = "ēéěè";
        // i系
        private const string TonedI = "īíǐì";
        // o系
        private const string TonedO = "ōóǒò";
        // u系
        private const string TonedU = "ūúǔù";
        // ü系
        private const string TonedV = "ǖǘǚǜ";

        // 数字→記号変換用の声調記号テーブル [母音インデックス, 声調1-4]
        // 母音順: a=0, e=1, i=2, o=3, u=4, ü=5
        private static readonly char[,] ToneMarks = new char[,]
        {
            { 'ā', 'á', 'ǎ', 'à' }, // a
            { 'ē', 'é', 'ě', 'è' }, // e
            { 'ī', 'í', 'ǐ', 'ì' }, // i
            { 'ō', 'ó', 'ǒ', 'ò' }, // o
            { 'ū', 'ú', 'ǔ', 'ù' }, // u
            { 'ǖ', 'ǘ', 'ǚ', 'ǜ' }, // ü
        };

        /// <summary>声調記号付きピンインから声調番号を抽出する（例: "zhōng" → Tone.First）。</summary>
        /// <param name="pinyin">声調記号付きピンイン文字列。</param>
        /// <returns>検出された声調。記号がなければ <see cref="Tone.Neutral"/>。</returns>
        public static Tone ExtractTone(string pinyin)
        {
            if (string.IsNullOrEmpty(pinyin))
                return Tone.Neutral;

            for (int i = 0; i < pinyin.Length; i++)
            {
                var tone = GetToneFromChar(pinyin[i]);
                if (tone != Tone.Neutral)
                    return tone;
            }

            return Tone.Neutral;
        }

        /// <summary>声調記号を除去する（例: "zhōng" → "zhong"）。</summary>
        /// <param name="pinyin">声調記号付きピンイン文字列。</param>
        /// <returns>声調記号を基本母音に置換した文字列。</returns>
        public static string RemoveTone(string pinyin)
        {
            if (string.IsNullOrEmpty(pinyin))
                return pinyin ?? string.Empty;

            // まず声調記号を含む文字のインデックスを探す
            int toneIndex = -1;
            for (int i = 0; i < pinyin.Length; i++)
            {
                if (GetBaseVowel(pinyin[i]) != '\0')
                {
                    toneIndex = i;
                    break;
                }
            }

            // 声調記号がなければ元の文字列をそのまま返す（アロケーション不要）
            if (toneIndex < 0)
                return pinyin;

            // StringBuilderで声調記号を置換
            var sb = new StringBuilder(pinyin.Length);
            // toneIndex より前はそのままコピー
            if (toneIndex > 0)
                sb.Append(pinyin, 0, toneIndex);

            for (int i = toneIndex; i < pinyin.Length; i++)
            {
                char c = pinyin[i];
                char baseChar = GetBaseVowel(c);
                sb.Append(baseChar != '\0' ? baseChar : c);
            }
            return sb.ToString();
        }

        /// <summary>声調記号付き→数字末尾（例: "zhōng" → "zhong1"）。</summary>
        /// <param name="pinyin">声調記号付きピンイン文字列。</param>
        /// <returns>声調記号を除去し末尾に声調番号を付加した文字列。軽声の場合は番号なし。</returns>
        public static string ToToneNumber(string pinyin)
        {
            if (string.IsNullOrEmpty(pinyin))
                return pinyin ?? string.Empty;

            var tone = ExtractTone(pinyin);
            var bare = RemoveTone(pinyin);

            if (tone == Tone.Neutral)
                return bare;

            return bare + ((int)tone).ToString();
        }

        /// <summary>声調数字末尾→声調記号付き（例: "zhong1" → "zhōng"）。</summary>
        /// <param name="pinyin">声調数字末尾のピンイン文字列。</param>
        /// <returns>声調記号付きピンイン文字列。数字がなければそのまま返す。</returns>
        public static string ToToneMarked(string pinyin)
        {
            if (string.IsNullOrEmpty(pinyin))
                return pinyin ?? string.Empty;

            // 末尾が1-4の数字か判定
            char lastChar = pinyin[pinyin.Length - 1];
            if (lastChar < '1' || lastChar > '4')
                return pinyin;

            int toneNumber = lastChar - '0';
            int bareLen = pinyin.Length - 1;

            if (bareLen == 0)
                return pinyin;

            // 声調記号の配置位置を決定（bareはpinyinの先頭からbareLen文字分）
            int placeIndex = FindTonePlacement(pinyin, bareLen);
            if (placeIndex < 0)
                return pinyin;

            var sb = new StringBuilder(bareLen);
            // placeIndex前をまとめてコピー
            if (placeIndex > 0)
                sb.Append(pinyin, 0, placeIndex);

            // 声調記号を配置
            int vowelIndex = GetVowelIndex(pinyin[placeIndex]);
            sb.Append(vowelIndex >= 0 ? ToneMarks[vowelIndex, toneNumber - 1] : pinyin[placeIndex]);

            // placeIndex後をまとめてコピー
            if (placeIndex + 1 < bareLen)
                sb.Append(pinyin, placeIndex + 1, bareLen - placeIndex - 1);

            return sb.ToString();
        }

        /// <summary>声調記号付き文字から声調番号を取得する。</summary>
        private static Tone GetToneFromChar(char c)
        {
            switch (c)
            {
                // a系: ā á ǎ à
                case '\u0101': return Tone.First;
                case '\u00E1': return Tone.Second;
                case '\u01CE': return Tone.Third;
                case '\u00E0': return Tone.Fourth;
                // e系: ē é ě è
                case '\u0113': return Tone.First;
                case '\u00E9': return Tone.Second;
                case '\u011B': return Tone.Third;
                case '\u00E8': return Tone.Fourth;
                // i系: ī í ǐ ì
                case '\u012B': return Tone.First;
                case '\u00ED': return Tone.Second;
                case '\u01D0': return Tone.Third;
                case '\u00EC': return Tone.Fourth;
                // o系: ō ó ǒ ò
                case '\u014D': return Tone.First;
                case '\u00F3': return Tone.Second;
                case '\u01D2': return Tone.Third;
                case '\u00F2': return Tone.Fourth;
                // u系: ū ú ǔ ù
                case '\u016B': return Tone.First;
                case '\u00FA': return Tone.Second;
                case '\u01D4': return Tone.Third;
                case '\u00F9': return Tone.Fourth;
                // ü系: ǖ ǘ ǚ ǜ
                case '\u01D6': return Tone.First;
                case '\u01D8': return Tone.Second;
                case '\u01DA': return Tone.Third;
                case '\u01DC': return Tone.Fourth;
                default: return Tone.Neutral;
            }
        }

        /// <summary>声調記号付き文字を基本母音に変換する。声調記号なしなら '\0' を返す。</summary>
        private static char GetBaseVowel(char c)
        {
            switch (c)
            {
                // a系: ā á ǎ à
                case '\u0101': case '\u00E1': case '\u01CE': case '\u00E0': return 'a';
                // e系: ē é ě è
                case '\u0113': case '\u00E9': case '\u011B': case '\u00E8': return 'e';
                // i系: ī í ǐ ì
                case '\u012B': case '\u00ED': case '\u01D0': case '\u00EC': return 'i';
                // o系: ō ó ǒ ò
                case '\u014D': case '\u00F3': case '\u01D2': case '\u00F2': return 'o';
                // u系: ū ú ǔ ù
                case '\u016B': case '\u00FA': case '\u01D4': case '\u00F9': return 'u';
                // ü系: ǖ ǘ ǚ ǜ
                case '\u01D6': case '\u01D8': case '\u01DA': case '\u01DC': return '\u00fc';
                default: return '\0';
            }
        }

        /// <summary>母音文字のインデックスを返す（a=0, e=1, i=2, o=3, u=4, ü=5）。母音でなければ -1。</summary>
        private static int GetVowelIndex(char c)
        {
            switch (c)
            {
                case 'a': case 'A': return 0;
                case 'e': case 'E': return 1;
                case 'i': case 'I': return 2;
                case 'o': case 'O': return 3;
                case 'u': case 'U': return 4;
                case '\u00fc': case '\u00dc': // ü, Ü
                case '\u01d6': case '\u01d8': case '\u01da': case '\u01dc': // ǖǘǚǜ (既に声調付き)
                    return 5;
                default: return -1;
            }
        }

        /// <summary>
        /// 声調記号を配置する母音の位置を決定する。
        /// ルール:
        /// 1. a または e を含む場合はそこに付ける
        /// 2. ou の場合は o に付ける
        /// 3. それ以外は最後の母音に付ける
        /// </summary>
        /// <param name="pinyin">元の文字列（末尾の数字を除く先頭bareLen文字を対象）</param>
        /// <param name="bareLen">対象文字数</param>
        private static int FindTonePlacement(string pinyin, int bareLen)
        {
            // ルール1: a または e があればそこに配置
            for (int i = 0; i < bareLen; i++)
            {
                char lower = char.ToLowerInvariant(pinyin[i]);
                if (lower == 'a' || lower == 'e')
                    return i;
            }

            // ルール2: ou の場合は o に配置
            for (int i = 0; i < bareLen - 1; i++)
            {
                char lower = char.ToLowerInvariant(pinyin[i]);
                char nextLower = char.ToLowerInvariant(pinyin[i + 1]);
                if (lower == 'o' && nextLower == 'u')
                    return i;
            }

            // ルール3: 最後の母音に配置
            int lastVowel = -1;
            for (int i = 0; i < bareLen; i++)
            {
                if (GetVowelIndex(pinyin[i]) >= 0)
                    lastVowel = i;
            }
            return lastVowel;
        }
    }
}
