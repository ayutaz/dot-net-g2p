using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetG2P.Chinese
{
    /// <summary>ピンイン→IPA（国際音声記号）変換。</summary>
    internal static class PinyinToIpa
    {
        // 声母→IPAマッピング
        private static readonly Dictionary<Initial, string> s_initialIpa = new Dictionary<Initial, string>
        {
            [Initial.B] = "p",
            [Initial.P] = "p\u02B0",    // pʰ
            [Initial.M] = "m",
            [Initial.F] = "f",
            [Initial.D] = "t",
            [Initial.T] = "t\u02B0",    // tʰ
            [Initial.N] = "n",
            [Initial.L] = "l",
            [Initial.G] = "k",
            [Initial.K] = "k\u02B0",    // kʰ
            [Initial.H] = "x",
            [Initial.J] = "t\u0255",    // tɕ
            [Initial.Q] = "t\u0255\u02B0", // tɕʰ
            [Initial.X] = "\u0255",      // ɕ
            [Initial.Zh] = "\u0288\u0282",    // ʈʂ
            [Initial.Ch] = "\u0288\u0282\u02B0", // ʈʂʰ
            [Initial.Sh] = "\u0282",     // ʂ
            [Initial.R] = "\u027B",      // ɻ
            [Initial.Z] = "ts",
            [Initial.C] = "ts\u02B0",    // tsʰ
            [Initial.S] = "s",
            [Initial.Y] = "j",
            [Initial.W] = "w",
        };

        // 韻母→IPAマッピング
        private static readonly Dictionary<Final, string> s_finalIpa = new Dictionary<Final, string>
        {
            [Final.A] = "a",
            [Final.O] = "o",
            [Final.E] = "\u0264",       // ɤ
            [Final.Ai] = "a\u026A",     // aɪ
            [Final.Ei] = "e\u026A",     // eɪ
            [Final.Ao] = "a\u028A",     // aʊ
            [Final.Ou] = "o\u028A",     // oʊ
            [Final.An] = "an",
            [Final.En] = "\u0259n",     // ən
            [Final.Ang] = "a\u014B",    // aŋ
            [Final.Eng] = "\u0259\u014B", // əŋ
            [Final.Ong] = "\u028A\u014B", // ʊŋ
            [Final.I] = "i",
            [Final.Ia] = "ia",
            [Final.Ie] = "i\u025B",     // iɛ
            [Final.Iao] = "ia\u028A",   // iaʊ
            [Final.Iu] = "io\u028A",    // ioʊ
            [Final.Ian] = "i\u025Bn",   // iɛn
            [Final.In] = "in",
            [Final.Iang] = "ia\u014B",  // iaŋ
            [Final.Ing] = "i\u014B",    // iŋ
            [Final.Iong] = "i\u028A\u014B",   // iʊŋ
            [Final.U] = "u",
            [Final.Ua] = "ua",
            [Final.Uo] = "uo",
            [Final.Uai] = "ua\u026A",   // uaɪ
            [Final.Ui] = "ue\u026A",    // ueɪ
            [Final.Uan] = "uan",
            [Final.Un] = "u\u0259n",    // uən
            [Final.Uang] = "ua\u014B",  // uaŋ
            [Final.Ueng] = "u\u0259\u014B", // uəŋ
            [Final.V] = "y",
            [Final.Ve] = "y\u025B",     // yɛ
            [Final.Van] = "yan",
            [Final.Vn] = "yn",
            [Final.Er] = "\u0259\u027B", // əɻ
        };

        // 声調→IPA tone lettersマッピング
        private static readonly string[] s_toneLetters = new string[]
        {
            "",                                       // Neutral (0) - なし
            "\u02E5\u02E5",                           // First (1)  - ˥˥
            "\u02E7\u02E5",                           // Second (2) - ˧˥
            "\u02E8\u02E9\u02E6",                     // Third (3)  - ˨˩˦
            "\u02E5\u02E9",                           // Fourth (4) - ˥˩
        };

        // zh/ch/sh/r + i のそり舌母音 ɻ̩
        private static readonly string s_retroflexApical = "\u027B\u0329"; // ɻ̩

        // z/c/s + i の歯茎母音 ɹ̩
        private static readonly string s_alveolarApical = "\u0279\u0329"; // ɹ̩

        /// <summary>
        /// 声調記号付きピンインをIPA表記に変換する（声調マーカー付き）。
        /// </summary>
        /// <param name="pinyin">声調記号付きまたは声調数字付きのピンイン文字列。</param>
        /// <returns>IPA表記文字列。</returns>
        public static string Convert(string pinyin)
        {
            return Convert(pinyin, true);
        }

        /// <summary>
        /// 声調記号付きピンインをIPA表記に変換する。
        /// </summary>
        /// <param name="pinyin">声調記号付きまたは声調数字付きのピンイン文字列。</param>
        /// <param name="includeTones">声調マーカーを含めるかどうか。</param>
        /// <returns>IPA表記文字列。</returns>
        public static string Convert(string pinyin, bool includeTones)
        {
            if (string.IsNullOrEmpty(pinyin))
                return string.Empty;

            // 数字声調形式（"ma1"等）を声調記号付きに変換してからパース
            string normalized = ToneConverter.ToToneMarked(pinyin);
            if (!PinyinParser.TryParse(normalized, out var syllable))
                return string.Empty;

            return ConvertSyllable(syllable, includeTones);
        }

        /// <summary>
        /// PinyinSyllableをIPA表記に変換する。
        /// </summary>
        internal static string ConvertSyllable(PinyinSyllable syllable, bool includeTones)
        {
            var sb = new StringBuilder(16);

            // 声母のIPA
            if (syllable.Initial != Initial.None)
            {
                // y/w はゼロ声母的扱いだが、韻母が単独の場合は半母音を付与
                // PinyinParserが y→Initial.Y, w→Initial.W としてパースするので
                // 韻母のIPA変換に任せる部分もあるが、基本的に声母IPAを出力
                if (syllable.Initial == Initial.Y || syllable.Initial == Initial.W)
                {
                    // y + i系韻母 → 半母音は不要（韻母自体が i で始まる）
                    // y + ü系韻母 → 半母音は不要（韻母自体が y [IPA] で始まる）
                    // w + u系韻母 → 半母音は不要（韻母自体が u で始まる）
                    // ただし y + a/e/ao 等 → j を付与
                    // w + o/a 等 → w を付与
                    if (!ShouldOmitSemivowel(syllable.Initial, syllable.Final))
                    {
                        sb.Append(s_initialIpa[syllable.Initial]);
                    }
                }
                else
                {
                    sb.Append(s_initialIpa[syllable.Initial]);
                }
            }

            // 韻母のIPA
            if (syllable.Final != Final.None)
            {
                // zhi/chi/shi/ri の i はそり舌母音 ɻ̩
                if (syllable.Final == Final.I && IsRetroflex(syllable.Initial))
                {
                    sb.Append(s_retroflexApical);
                }
                // zi/ci/si の i は歯茎母音 ɹ̩
                else if (syllable.Final == Final.I && IsAlveolar(syllable.Initial))
                {
                    sb.Append(s_alveolarApical);
                }
                else
                {
                    sb.Append(s_finalIpa[syllable.Final]);
                }
            }

            // 声調マーカー
            if (includeTones && syllable.Tone != Tone.Neutral)
            {
                sb.Append(s_toneLetters[(int)syllable.Tone]);
            }

            return sb.ToString();
        }

        /// <summary>zh/ch/sh/r のそり舌声母かどうか。</summary>
        private static bool IsRetroflex(Initial initial)
        {
            return initial == Initial.Zh
                || initial == Initial.Ch
                || initial == Initial.Sh
                || initial == Initial.R;
        }

        /// <summary>z/c/s の歯茎声母かどうか。</summary>
        private static bool IsAlveolar(Initial initial)
        {
            return initial == Initial.Z
                || initial == Initial.C
                || initial == Initial.S;
        }

        /// <summary>y/wの半母音を省略すべきかどうかを判定する。</summary>
        private static bool ShouldOmitSemivowel(Initial initial, Final final_)
        {
            if (initial == Initial.Y)
            {
                // y + i系韻母: 韻母がiで始まるのでjは不要
                // y + ü系韻母: 韻母がy[IPA]で始まるのでjは不要
                switch (final_)
                {
                    case Final.I:
                    case Final.In:
                    case Final.Ing:
                    case Final.V:
                    case Final.Ve:
                    case Final.Van:
                    case Final.Vn:
                        return true;
                    default:
                        return false;
                }
            }

            if (initial == Initial.W)
            {
                // w + u系韻母: 韻母がuで始まるのでwは不要
                switch (final_)
                {
                    case Final.U:
                    case Final.Un:
                        return true;
                    default:
                        return false;
                }
            }

            return false;
        }
    }
}
