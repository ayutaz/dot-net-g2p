using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetG2P.Chinese
{
    /// <summary>
    /// piper-plus 互換のピンイン→IPA変換。
    /// piper-plus の chinese.py の _INITIAL_TO_IPA / _FINAL_TO_IPA テーブルと完全一致する。
    /// 声調マーカーは含めない（piper-plus は tone1-tone5 トークンで別管理するため）。
    /// </summary>
    internal static class PinyinToPiperIpa
    {
        // 声母→IPAマッピング（piper-plus準拠）
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
            [Initial.Zh] = "t\u0282",    // tʂ
            [Initial.Ch] = "t\u0282\u02B0", // tʂʰ
            [Initial.Sh] = "\u0282",     // ʂ
            [Initial.R] = "\u027B",      // ɻ
            [Initial.Z] = "ts",
            [Initial.C] = "ts\u02B0",    // tsʰ
            [Initial.S] = "s",
            [Initial.Y] = "j",
            [Initial.W] = "w",
        };

        // 韻母→IPAマッピング（piper-plus準拠）
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
            [Final.Ong] = "u\u014B",    // uŋ
            [Final.I] = "i",
            [Final.Ia] = "ia",
            [Final.Ie] = "i\u025B",     // iɛ
            [Final.Iao] = "ia\u028A",   // iaʊ
            [Final.Iu] = "iou",         // iou
            [Final.Ian] = "i\u025Bn",   // iɛn
            [Final.In] = "in",
            [Final.Iang] = "ia\u014B",  // iaŋ
            [Final.Ing] = "i\u014B",    // iŋ
            [Final.Iong] = "iu\u014B",  // iuŋ
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
            [Final.Van] = "y\u025Bn",   // yɛn
            [Final.Vn] = "yn",
            [Final.Er] = "\u025A",      // ɚ
        };

        // zh/ch/sh/r + i のそり舌母音 ɻ̩
        private static readonly string s_retroflexApical = "\u027B\u0329"; // ɻ̩

        // z/c/s + i の歯茎母音 ɨ
        private static readonly string s_alveolarApical = "\u0268"; // ɨ

        /// <summary>
        /// ピンイン文字列をpiper-plus互換IPAに変換する。
        /// 声調マーカーは含めない。
        /// </summary>
        /// <param name="pinyin">声調記号付きまたは声調数字付きのピンイン文字列。</param>
        /// <returns>piper-plus互換IPA表記文字列。</returns>
        public static string Convert(string pinyin)
        {
            if (string.IsNullOrEmpty(pinyin))
                return string.Empty;

            // 数字声調形式（"ma1"等）を声調記号付きに変換してからパース
            string normalized = ToneConverter.ToToneMarked(pinyin);
            if (!PinyinParser.TryParse(normalized, out var syllable))
                return string.Empty;

            return ConvertSyllable(syllable);
        }

        /// <summary>
        /// PinyinSyllableをpiper-plus互換IPAに変換する。
        /// </summary>
        internal static string ConvertSyllable(PinyinSyllable syllable)
        {
            var sb = new StringBuilder(16);

            // 声母のIPA
            if (syllable.Initial != Initial.None)
            {
                if (syllable.Initial == Initial.Y || syllable.Initial == Initial.W)
                {
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
                // zi/ci/si の i は歯茎母音 ɨ
                else if (syllable.Final == Final.I && IsAlveolar(syllable.Initial))
                {
                    sb.Append(s_alveolarApical);
                }
                else
                {
                    sb.Append(s_finalIpa[syllable.Final]);
                }
            }

            // piper-plus は声調マーカーを含めない（tone1-tone5 トークンで別管理）

            return sb.ToString();
        }

        /// <summary>
        /// PinyinSyllableからIPA音素配列を返す（声母と韻母を分離）。
        /// piper-plus のProsody処理や音素単位処理で使用する。
        /// </summary>
        internal static string[] ConvertToPhonemes(PinyinSyllable syllable)
        {
            string initialIpa = null;
            string finalIpa = null;

            // 声母のIPA
            if (syllable.Initial != Initial.None)
            {
                if (syllable.Initial == Initial.Y || syllable.Initial == Initial.W)
                {
                    if (!ShouldOmitSemivowel(syllable.Initial, syllable.Final))
                    {
                        initialIpa = s_initialIpa[syllable.Initial];
                    }
                }
                else
                {
                    initialIpa = s_initialIpa[syllable.Initial];
                }
            }

            // 韻母のIPA
            if (syllable.Final != Final.None)
            {
                // zhi/chi/shi/ri の i はそり舌母音 ɻ̩
                if (syllable.Final == Final.I && IsRetroflex(syllable.Initial))
                {
                    finalIpa = s_retroflexApical;
                }
                // zi/ci/si の i は歯茎母音 ɨ
                else if (syllable.Final == Final.I && IsAlveolar(syllable.Initial))
                {
                    finalIpa = s_alveolarApical;
                }
                else
                {
                    finalIpa = s_finalIpa[syllable.Final];
                }
            }

            // 声母と韻母を分離した配列を返す
            if (initialIpa != null && finalIpa != null)
                return new[] { initialIpa, finalIpa };
            if (initialIpa != null)
                return new[] { initialIpa };
            if (finalIpa != null)
                return new[] { finalIpa };
            return Array.Empty<string>();
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
