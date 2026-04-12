using System.Collections.Generic;
using System.Text;

namespace DotNetG2P.Chinese
{
    /// <summary>
    /// Misaki 互換のピンイン→IPA変換。
    /// Kokoro TTS の G2P フロントエンド <see href="https://github.com/hexgrad/misaki"/> と同一の音素表記を使用する。
    /// 主な差異:
    /// <list type="bullet">
    /// <item>破擦音 j/q/z/c は 1 文字合字 (ʨ/ʨʰ/ʦ/ʦʰ) を使用</item>
    /// <item>二重母音の滑り音に非音節化符号 (U+032F) を付与 (ai̯, au̯, ei̯, ou̯ …)</item>
    /// <item>声調は IPA tone letters ではなく矢印記号 (→ ↗ ↓ ↘) を使用</item>
    /// </list>
    /// </summary>
    internal static class PinyinToMisaki
    {
        // 声母→IPAマッピング（Misaki準拠）
        private static readonly Dictionary<Initial, string> s_initialMisaki = new Dictionary<Initial, string>
        {
            [Initial.B] = "p",
            [Initial.P] = "p\u02B0",        // pʰ
            [Initial.M] = "m",
            [Initial.F] = "f",
            [Initial.D] = "t",
            [Initial.T] = "t\u02B0",        // tʰ
            [Initial.N] = "n",
            [Initial.L] = "l",
            [Initial.G] = "k",
            [Initial.K] = "k\u02B0",        // kʰ
            [Initial.H] = "x",
            [Initial.J] = "\u02A8",         // ʨ (Misaki: 合字、標準IPAは tɕ)
            [Initial.Q] = "\u02A8\u02B0",   // ʨʰ
            [Initial.X] = "\u0255",         // ɕ
            [Initial.Zh] = "\u0288\u0282",       // ʈʂ
            [Initial.Ch] = "\u0288\u0282\u02B0", // ʈʂʰ
            [Initial.Sh] = "\u0282",        // ʂ
            [Initial.R] = "\u027B",         // ɻ
            [Initial.Z] = "\u02A6",         // ʦ (Misaki: 合字、標準IPAは ts)
            [Initial.C] = "\u02A6\u02B0",   // ʦʰ
            [Initial.S] = "s",
            [Initial.Y] = "j",
            [Initial.W] = "w",
        };

        // 韻母→IPAマッピング（Misaki準拠）
        // 二重母音の滑り音には非音節化符号 U+032F (COMBINING INVERTED BREVE BELOW) を付与する。
        private static readonly Dictionary<Final, string> s_finalMisaki = new Dictionary<Final, string>
        {
            [Final.A] = "a",
            [Final.O] = "o",
            [Final.E] = "\u0264",               // ɤ
            [Final.Ai] = "ai\u032F",            // ai̯  (標準IPA: aɪ)
            [Final.Ei] = "ei\u032F",            // ei̯  (標準IPA: eɪ)
            [Final.Ao] = "au\u032F",            // au̯  (標準IPA: aʊ)
            [Final.Ou] = "ou\u032F",            // ou̯  (標準IPA: oʊ)
            [Final.An] = "an",
            [Final.En] = "\u0259n",             // ən
            [Final.Ang] = "a\u014B",            // aŋ
            [Final.Eng] = "\u0259\u014B",       // əŋ
            [Final.Ong] = "u\u032F\u014B",      // u̯ŋ (標準IPA: ʊŋ)
            [Final.I] = "i",
            [Final.Ia] = "ia",
            [Final.Ie] = "i\u025B",             // iɛ
            [Final.Iao] = "iau\u032F",          // iau̯
            [Final.Iu] = "iou\u032F",           // iou̯
            [Final.Ian] = "i\u025Bn",           // iɛn
            [Final.In] = "in",
            [Final.Iang] = "ia\u014B",          // iaŋ
            [Final.Ing] = "i\u014B",            // iŋ
            [Final.Iong] = "iu\u032F\u014B",    // iu̯ŋ (標準IPA: iʊŋ)
            [Final.U] = "u",
            [Final.Ua] = "ua",
            [Final.Uo] = "uo",
            [Final.Uai] = "uai\u032F",          // uai̯
            [Final.Ui] = "uei\u032F",           // uei̯
            [Final.Uan] = "uan",
            [Final.Un] = "u\u0259n",            // uən
            [Final.Uang] = "ua\u014B",          // uaŋ
            [Final.Ueng] = "u\u0259\u014B",     // uəŋ
            [Final.V] = "y",
            [Final.Ve] = "y\u025B",             // yɛ
            [Final.Van] = "yan",
            [Final.Vn] = "yn",
            [Final.Er] = "\u0259\u027B",        // əɻ
        };

        // 声調→矢印記号マッピング（Misaki準拠）
        private static readonly string[] s_toneArrows = new string[]
        {
            "",             // Neutral (0) - 軽声は声調なし
            "\u2192",       // First  (1) → 陰平
            "\u2197",       // Second (2) ↗ 陽平
            "\u2193",       // Third  (3) ↓ 上声
            "\u2198",       // Fourth (4) ↘ 去声
        };

        // zh/ch/sh/r + i のそり舌母音 ɻ̩ （PinyinToIpa と同一）
        private static readonly string s_retroflexApical = "\u027B\u0329"; // ɻ̩

        // z/c/s + i の歯茎母音 ɹ̩ （PinyinToIpa と同一）
        private static readonly string s_alveolarApical = "\u0279\u0329"; // ɹ̩

        /// <summary>声母 Initial に対応する Misaki 互換 IPA 文字列を返す（テスト・検証用）。</summary>
        internal static string GetInitialMisaki(Initial initial)
        {
            return s_initialMisaki.TryGetValue(initial, out var value) ? value : string.Empty;
        }

        /// <summary>韻母 Final に対応する Misaki 互換 IPA 文字列を返す（テスト・検証用）。</summary>
        internal static string GetFinalMisaki(Final final_)
        {
            return s_finalMisaki.TryGetValue(final_, out var value) ? value : string.Empty;
        }

        /// <summary>声調 Tone に対応する Misaki 互換矢印記号を返す（テスト・検証用）。</summary>
        internal static string GetToneArrow(Tone tone)
        {
            int index = (int)tone;
            return (index >= 0 && index < s_toneArrows.Length) ? s_toneArrows[index] : string.Empty;
        }

        /// <summary>zh/ch/sh/r + i で使用するそり舌母音を返す（テスト・検証用）。</summary>
        internal static string GetRetroflexApical() => s_retroflexApical;

        /// <summary>z/c/s + i で使用する歯茎母音を返す（テスト・検証用）。</summary>
        internal static string GetAlveolarApical() => s_alveolarApical;

        /// <summary>
        /// 声調記号付きピンインを Misaki 互換 IPA 表記に変換する（声調矢印付き）。
        /// </summary>
        /// <param name="pinyin">声調記号付きまたは声調数字付きのピンイン文字列。</param>
        /// <returns>Misaki 互換 IPA 表記文字列。パース失敗時は空文字列。</returns>
        public static string Convert(string pinyin)
        {
            return Convert(pinyin, true);
        }

        /// <summary>
        /// 声調記号付きピンインを Misaki 互換 IPA 表記に変換する。
        /// </summary>
        /// <param name="pinyin">声調記号付きまたは声調数字付きのピンイン文字列。</param>
        /// <param name="includeTones">声調矢印を含めるかどうか。</param>
        /// <returns>Misaki 互換 IPA 表記文字列。パース失敗時は空文字列。</returns>
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
        /// PinyinSyllable を Misaki 互換 IPA 表記に変換する。
        /// </summary>
        internal static string ConvertSyllable(PinyinSyllable syllable, bool includeTones)
        {
            var sb = new StringBuilder(16);

            // 声母の Misaki IPA
            if (syllable.Initial != Initial.None)
            {
                if (syllable.Initial == Initial.Y || syllable.Initial == Initial.W)
                {
                    // y/w は韻母側が対応する母音で始まる場合、半母音を省略する
                    if (!ShouldOmitSemivowel(syllable.Initial, syllable.Final))
                    {
                        sb.Append(s_initialMisaki[syllable.Initial]);
                    }
                }
                else
                {
                    sb.Append(s_initialMisaki[syllable.Initial]);
                }
            }

            // 韻母の Misaki IPA
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
                    sb.Append(s_finalMisaki[syllable.Final]);
                }
            }

            // 声調矢印
            if (includeTones && syllable.Tone != Tone.Neutral)
            {
                sb.Append(s_toneArrows[(int)syllable.Tone]);
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

        /// <summary>y/w の半母音を省略すべきかどうかを判定する。</summary>
        private static bool ShouldOmitSemivowel(Initial initial, Final final_)
        {
            if (initial == Initial.Y)
            {
                // y + i系韻母: 韻母が i で始まるので j は不要
                // y + ü系韻母: 韻母が y[IPA] で始まるので j は不要
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
                // w + u系韻母: 韻母が u で始まるので w は不要
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
