using System.Collections.Generic;
using System.Text;

namespace DotNetG2P.Chinese
{
    /// <summary>
    /// Misaki 互換のピンイン→IPA変換。
    /// Kokoro TTS の G2P フロントエンド <see href="https://github.com/hexgrad/misaki"/> と同一の音素表記を使用する。
    /// Phase 1-R verified via uv misaki 0.9.4 (実測 137 件、.claude/tmp/misaki-gold.txt 参照)。
    /// 主な特徴:
    /// <list type="bullet">
    /// <item>破擦音 j/q/z/c/zh/ch は合字 (ʨ/ʨʰ/ʦ/ʦʰ/ꭧ/ꭧʰ) を使用</item>
    /// <item>韻母は Prefix + tone + Suffix 方式 (An=("a","n") → "a" + 声調 + "n")</item>
    /// <item>声調は IPA tone letters ではなく矢印記号 (→ ↗ ↓ ↘) を使用</item>
    /// <item>二重母音は U+032F を付けず、template 側で事前除去済み (ai, au, ei, ou 等)</item>
    /// <item>retroflex/alveolar apical は直接 ɨ (U+0268) として出力</item>
    /// <item>Y/W 複合韻母は (Initial, Final) ペアから独立テーブルで lookup</item>
    /// </list>
    /// 仕様参照: .claude/tmp/misaki-spec.md
    /// <para>
    /// 注意: Misaki Python 実装 (legacy) は声調変調（三声連読等）を適用しません。
    /// 本クラスは音節単位の変換のみを担当し、声調変調は <see cref="ChineseG2PEngine"/> のパイプラインで制御されます。
    /// </para>
    /// </summary>
    internal static class PinyinToMisaki
    {
        // ────────────────────────────────────────────────────────────
        // 声母マッピング (21 エントリ、Misaki spec 準拠)
        // Y/W は compound final 層で処理するのでエントリなし
        // ────────────────────────────────────────────────────────────
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
            [Initial.J] = "\u02A8",         // ʨ (合字、confirmed by uv run)
            [Initial.Q] = "\u02A8\u02B0",   // ʨʰ
            [Initial.X] = "\u0255",         // ɕ
            [Initial.Zh] = "\uAB67",        // ꭧ (Misaki 合字、NOT ʈʂ、confirmed by uv run)
            [Initial.Ch] = "\uAB67\u02B0",  // ꭧʰ
            [Initial.Sh] = "\u0282",        // ʂ
            [Initial.R] = "\u027B",         // ɻ
            [Initial.Z] = "\u02A6",         // ʦ (合字)
            [Initial.C] = "\u02A6\u02B0",   // ʦʰ
            [Initial.S] = "s",
            // Y, W は意図的にエントリなし (compound final 層で処理)
        };

        // ────────────────────────────────────────────────────────────
        // 韻母テンプレート (Prefix + Tone + Suffix 方式、36 エントリ)
        // 声調は Prefix と Suffix の間に挿入される
        // U+032F (非音節化符号) は事前に strip 済み
        // ────────────────────────────────────────────────────────────
        private static readonly Dictionary<Final, (string Prefix, string Suffix)> s_finalMisaki = new Dictionary<Final, (string, string)>
        {
            [Final.A]    = ("a", ""),
            [Final.O]    = ("wo", ""),                     // bpmf + o → "pwo" etc.
            [Final.E]    = ("\u0264", ""),                 // ɤ
            [Final.Ai]   = ("ai", ""),                     // U+032F strip 済み
            [Final.Ei]   = ("ei", ""),
            [Final.Ao]   = ("au", ""),
            [Final.Ou]   = ("ou", ""),
            [Final.An]   = ("a", "n"),                     // 声調が中間
            [Final.En]   = ("\u0259", "n"),                // ən
            [Final.Ang]  = ("a", "\u014B"),                // aŋ
            [Final.Eng]  = ("\u0259", "\u014B"),           // əŋ
            [Final.Ong]  = ("\u028A", "\u014B"),           // ʊŋ (U+028A、旧 u̯ は誤り)
            [Final.I]    = ("i", ""),
            [Final.Ia]   = ("ja", ""),
            [Final.Ie]   = ("je", ""),                     // je (標準IPA の iɛ とは違う、NOT jɛ)
            [Final.Iao]  = ("jau", ""),                    // strip 済み
            [Final.Iu]   = ("jou", ""),                    // Misaki "iou"、strip 済み
            [Final.Ian]  = ("j\u025B", "n"),               // jɛn、声調が中間
            [Final.In]   = ("i", "n"),
            [Final.Iang] = ("ja", "\u014B"),               // jaŋ
            [Final.Ing]  = ("i", "\u014B"),                // iŋ
            [Final.Iong] = ("j\u028A", "\u014B"),          // jʊŋ
            [Final.U]    = ("u", ""),
            [Final.Ua]   = ("wa", ""),
            [Final.Uo]   = ("wo", ""),
            [Final.Uai]  = ("wai", ""),                    // strip 済み
            [Final.Ui]   = ("wei", ""),                    // Misaki "uei"、strip 済み
            [Final.Uan]  = ("wa", "n"),
            [Final.Un]   = ("w\u0259", "n"),               // wən (Misaki "uen")
            [Final.Uang] = ("wa", "\u014B"),               // waŋ
            [Final.Ueng] = ("w\u0259", "\u014B"),          // wəŋ
            [Final.V]    = ("y", ""),
            [Final.Ve]   = ("\u0265e", ""),                // ɥe (ɥ=U+0265、NOT y)
            [Final.Van]  = ("\u0265\u025B", "n"),          // ɥɛn (NOT yan)
            [Final.Vn]   = ("y", "n"),                     // yn
            [Final.Er]   = ("\u025A", ""),                 // ɚ (U+025A、NOT əɻ)
        };

        // ────────────────────────────────────────────────────────────
        // 声調矢印 (5 エントリ、Misaki spec 準拠)
        // ────────────────────────────────────────────────────────────
        private static readonly string[] s_toneArrows = new string[]
        {
            "",             // Neutral (0) - 軽声は声調なし
            "\u2192",       // First  (1) → 陰平
            "\u2197",       // Second (2) ↗ 陽平
            "\u2193",       // Third  (3) ↓ 上声
            "\u2198",       // Fourth (4) ↘ 去声
        };

        // ────────────────────────────────────────────────────────────
        // Y/W 複合韻母マッピング (23 エントリ、Misaki spec の Y/W 表から)
        // DotNetG2P の PinyinParser は "wang" を Initial.W + Final.Ang に parse する
        // (Misaki の "uang" とは違う構造) ので、このペアから独立テーブルで lookup する。
        // Value: (Prefix, Suffix, OmitInitial)
        // OmitInitial=true の場合、W/Y は省略される (yi→i, wu→u 等)
        // ────────────────────────────────────────────────────────────
        private static readonly Dictionary<(Initial, Final), (string Prefix, string Suffix, bool OmitInitial)> s_yWCompoundMisaki = new Dictionary<(Initial, Final), (string, string, bool)>
        {
            // Y + X (14 エントリ)
            [(Initial.Y, Final.A)]    = ("ja", "", false),                       // ya → ia
            [(Initial.Y, Final.An)]   = ("j\u025B", "n", false),                 // yan → ian → jɛn
            [(Initial.Y, Final.Ang)]  = ("ja", "\u014B", false),                 // yang → iang → jaŋ
            [(Initial.Y, Final.Ao)]   = ("jau", "", false),                      // yao → iao → jau
            [(Initial.Y, Final.E)]    = ("je", "", false),                       // ye → ie → je
            [(Initial.Y, Final.I)]    = ("i", "", true),                         // yi → i (j 省略)
            [(Initial.Y, Final.In)]   = ("i", "n", true),                        // yin → in (j 省略)
            [(Initial.Y, Final.Ing)]  = ("i", "\u014B", true),                   // ying → iŋ (j 省略)
            [(Initial.Y, Final.Ong)]  = ("j\u028A", "\u014B", false),            // yong → iong → jʊŋ
            [(Initial.Y, Final.Ou)]   = ("jou", "", false),                      // you → iu (iou) → jou
            [(Initial.Y, Final.V)]    = ("y", "", true),                         // yu → ü → y (ɥ 省略)
            [(Initial.Y, Final.Ve)]   = ("\u0265e", "", false),                  // yue → üe → ɥe
            [(Initial.Y, Final.Van)]  = ("\u0265\u025B", "n", false),            // yuan → üan → ɥɛn
            [(Initial.Y, Final.Vn)]   = ("y", "n", true),                        // yun → ün → yn (ɥ 省略)

            // W + X (9 エントリ)
            [(Initial.W, Final.A)]    = ("wa", "", false),                       // wa → ua
            [(Initial.W, Final.Ai)]   = ("wai", "", false),                      // wai
            [(Initial.W, Final.An)]   = ("wa", "n", false),                      // wan → uan
            [(Initial.W, Final.Ang)]  = ("wa", "\u014B", false),                 // wang → uang → waŋ
            [(Initial.W, Final.Ei)]   = ("wei", "", false),                      // wei → ui (uei)
            [(Initial.W, Final.En)]   = ("w\u0259", "n", false),                 // wen → un (uen) → wən
            [(Initial.W, Final.Eng)]  = ("w\u0259", "\u014B", false),            // weng → wəŋ
            [(Initial.W, Final.O)]    = ("wo", "", false),                       // wo → uo
            [(Initial.W, Final.U)]    = ("u", "", true),                         // wu → u (w 省略)
        };

        // ────────────────────────────────────────────────────────────
        // そり舌/歯茎母音の直接マッピング (retroflex/alveolar apical → ɨ)
        // retroflex (ɻ̩) / alveolar (ɹ̩) の代わりに Misaki は直接 ɨ を使用する
        // ────────────────────────────────────────────────────────────
        private const string s_apicalMisaki = "\u0268"; // ɨ

        // ────────────────────────────────────────────────────────────
        // アクセサ (internal、テスト用)
        // ────────────────────────────────────────────────────────────

        /// <summary>声母 Initial に対応する Misaki 互換 IPA 文字列を返す（テスト・検証用）。</summary>
        internal static string GetInitialMisaki(Initial initial)
        {
            return s_initialMisaki.TryGetValue(initial, out var value) ? value : string.Empty;
        }

        /// <summary>韻母 Final に対応する Misaki 互換 (Prefix, Suffix) タプルを返す（テスト・検証用）。</summary>
        internal static (string Prefix, string Suffix) GetFinalMisaki(Final final_)
        {
            return s_finalMisaki.TryGetValue(final_, out var value) ? value : (string.Empty, string.Empty);
        }

        /// <summary>声調 Tone に対応する Misaki 互換矢印記号を返す（テスト・検証用）。</summary>
        internal static string GetToneArrow(Tone tone)
        {
            int index = (int)tone;
            return (index >= 0 && index < s_toneArrows.Length) ? s_toneArrows[index] : string.Empty;
        }

        /// <summary>Y/W 複合韻母マッピングのエントリを取得する（テスト・検証用）。</summary>
        internal static bool TryGetYWCompound(Initial initial, Final final_, out (string Prefix, string Suffix, bool OmitInitial) result)
        {
            return s_yWCompoundMisaki.TryGetValue((initial, final_), out result);
        }

        /// <summary>retroflex/alveolar apical に使用する ɨ を返す（テスト・検証用）。</summary>
        internal static string GetApicalMisaki() => s_apicalMisaki;

        // ────────────────────────────────────────────────────────────
        // 変換メソッド
        // ────────────────────────────────────────────────────────────

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
        /// パイプライン:
        /// <list type="number">
        /// <item>声調矢印決定</item>
        /// <item>特別ケース判定 (単独 ō, Er, retroflex/alveolar apical)</item>
        /// <item>Y/W compound final lookup or standard path</item>
        /// <item>Initial + Prefix + ToneArrow + Suffix で構築</item>
        /// </list>
        /// </summary>
        internal static string ConvertSyllable(PinyinSyllable syllable, bool includeTones)
        {
            // ステップ 1: 声調矢印
            string toneArrow = (includeTones && syllable.Tone != Tone.Neutral)
                ? s_toneArrows[(int)syllable.Tone]
                : string.Empty;

            // ステップ 2: 特別ケース判定

            // a. Initial.None + Final.O → 単独感嘆詞 "ɔ" (NOT wo)
            if (syllable.Initial == Initial.None && syllable.Final == Final.O)
            {
                return "\u0254" + toneArrow; // ɔ
            }

            // b. Final.Er (声母ありでも単独でも) → ɚ (U+025A)
            if (syllable.Final == Final.Er)
            {
                var erSb = new StringBuilder(8);
                if (syllable.Initial != Initial.None && s_initialMisaki.TryGetValue(syllable.Initial, out var erInitial))
                {
                    erSb.Append(erInitial);
                }
                erSb.Append("\u025A"); // ɚ
                erSb.Append(toneArrow);
                return erSb.ToString();
            }

            // c/d. Zh/Ch/Sh/R/Z/C/S + Final.I → 声母 + ɨ (retroflex/alveolar apical)
            if (syllable.Final == Final.I && (IsRetroflex(syllable.Initial) || IsAlveolar(syllable.Initial)))
            {
                return s_initialMisaki[syllable.Initial] + s_apicalMisaki + toneArrow;
            }

            // ステップ 3: Y/W compound final lookup
            string prefix;
            string suffix;
            bool omitInitial = false;

            if (s_yWCompoundMisaki.TryGetValue((syllable.Initial, syllable.Final), out var compound))
            {
                prefix = compound.Prefix;
                suffix = compound.Suffix;
                omitInitial = compound.OmitInitial;
            }
            else if (syllable.Initial == Initial.Y || syllable.Initial == Initial.W)
            {
                // Y/W が compound テーブルにない組み合わせ ("yei" 等) は無効
                return string.Empty;
            }
            else
            {
                // Standard path: 韻母テンプレートから (Prefix, Suffix) を取得
                if (s_finalMisaki.TryGetValue(syllable.Final, out var finalTuple))
                {
                    prefix = finalTuple.Prefix;
                    suffix = finalTuple.Suffix;
                }
                else
                {
                    prefix = string.Empty;
                    suffix = string.Empty;
                }
            }

            // ステップ 4-5: 構築
            var result = new StringBuilder(16);
            if (!omitInitial
                && syllable.Initial != Initial.None
                && s_initialMisaki.TryGetValue(syllable.Initial, out var initialIpa))
            {
                result.Append(initialIpa);
            }
            result.Append(prefix);
            result.Append(toneArrow);
            result.Append(suffix);
            return result.ToString();
        }

        // ────────────────────────────────────────────────────────────
        // ヘルパー
        // ────────────────────────────────────────────────────────────

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
    }
}
