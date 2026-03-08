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

            var sb = new StringBuilder(pinyin.Length);
            for (int i = 0; i < pinyin.Length; i++)
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
            string bare = pinyin.Substring(0, pinyin.Length - 1);

            if (bare.Length == 0)
                return pinyin;

            // 声調記号の配置位置を決定
            int placeIndex = FindTonePlacement(bare);
            if (placeIndex < 0)
                return pinyin;

            var sb = new StringBuilder(bare.Length);
            for (int i = 0; i < bare.Length; i++)
            {
                if (i == placeIndex)
                {
                    int vowelIndex = GetVowelIndex(bare[i]);
                    if (vowelIndex >= 0)
                    {
                        sb.Append(ToneMarks[vowelIndex, toneNumber - 1]);
                    }
                    else
                    {
                        sb.Append(bare[i]);
                    }
                }
                else
                {
                    sb.Append(bare[i]);
                }
            }
            return sb.ToString();
        }

        /// <summary>声調記号付き文字から声調番号を取得する。</summary>
        private static Tone GetToneFromChar(char c)
        {
            int idx;

            idx = TonedA.IndexOf(c);
            if (idx >= 0) return (Tone)(idx + 1);

            idx = TonedE.IndexOf(c);
            if (idx >= 0) return (Tone)(idx + 1);

            idx = TonedI.IndexOf(c);
            if (idx >= 0) return (Tone)(idx + 1);

            idx = TonedO.IndexOf(c);
            if (idx >= 0) return (Tone)(idx + 1);

            idx = TonedU.IndexOf(c);
            if (idx >= 0) return (Tone)(idx + 1);

            idx = TonedV.IndexOf(c);
            if (idx >= 0) return (Tone)(idx + 1);

            return Tone.Neutral;
        }

        /// <summary>声調記号付き文字を基本母音に変換する。声調記号なしなら '\0' を返す。</summary>
        private static char GetBaseVowel(char c)
        {
            if (TonedA.IndexOf(c) >= 0) return 'a';
            if (TonedE.IndexOf(c) >= 0) return 'e';
            if (TonedI.IndexOf(c) >= 0) return 'i';
            if (TonedO.IndexOf(c) >= 0) return 'o';
            if (TonedU.IndexOf(c) >= 0) return 'u';
            if (TonedV.IndexOf(c) >= 0) return '\u00fc'; // ü
            return '\0';
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
        private static int FindTonePlacement(string bare)
        {
            // ルール1: a または e があればそこに配置
            for (int i = 0; i < bare.Length; i++)
            {
                char lower = char.ToLowerInvariant(bare[i]);
                if (lower == 'a' || lower == 'e')
                    return i;
            }

            // ルール2: ou の場合は o に配置
            for (int i = 0; i < bare.Length - 1; i++)
            {
                char lower = char.ToLowerInvariant(bare[i]);
                char nextLower = char.ToLowerInvariant(bare[i + 1]);
                if (lower == 'o' && nextLower == 'u')
                    return i;
            }

            // ルール3: 最後の母音に配置
            int lastVowel = -1;
            for (int i = 0; i < bare.Length; i++)
            {
                if (GetVowelIndex(bare[i]) >= 0)
                    lastVowel = i;
            }
            return lastVowel;
        }
    }
}
