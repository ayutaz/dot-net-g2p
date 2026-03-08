using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetG2P.Chinese
{
    /// <summary>ピンイン→注音符号（ボポモフォ/Zhuyin）変換ユーティリティ。</summary>
    internal static class PinyinToZhuyin
    {
        // 声調マーカー（注音符号用）
        // 1声: 省略、2声: ˊ (U+02CA)、3声: ˇ (U+02C7)、4声: ˋ (U+02CB)、軽声: ˙ (U+02D9) 先頭
        private const char Tone2Mark = '\u02CA'; // ˊ
        private const char Tone3Mark = '\u02C7'; // ˇ
        private const char Tone4Mark = '\u02CB'; // ˋ
        private const char NeutralMark = '\u02D9'; // ˙

        // 声母→注音マッピング
        private static readonly Dictionary<string, string> s_initialMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["b"] = "\u3105",   // ㄅ
            ["p"] = "\u3106",   // ㄆ
            ["m"] = "\u3107",   // ㄇ
            ["f"] = "\u3108",   // ㄈ
            ["d"] = "\u3109",   // ㄉ
            ["t"] = "\u310A",   // ㄊ
            ["n"] = "\u310B",   // ㄋ
            ["l"] = "\u310C",   // ㄌ
            ["g"] = "\u310D",   // ㄍ
            ["k"] = "\u310E",   // ㄎ
            ["h"] = "\u310F",   // ㄏ
            ["j"] = "\u3110",   // ㄐ
            ["q"] = "\u3111",   // ㄑ
            ["x"] = "\u3112",   // ㄒ
            ["zh"] = "\u3113",  // ㄓ
            ["ch"] = "\u3114",  // ㄔ
            ["sh"] = "\u3115",  // ㄕ
            ["r"] = "\u3116",   // ㄖ
            ["z"] = "\u3117",   // ㄗ
            ["c"] = "\u3118",   // ㄘ
            ["s"] = "\u3119",   // ㄙ
        };

        // 韻母→注音マッピング
        private static readonly Dictionary<string, string> s_finalMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // 単韻母
            ["a"] = "\u311A",       // ㄚ
            ["o"] = "\u311B",       // ㄛ
            ["e"] = "\u311C",       // ㄜ
            ["i"] = "\u3127",       // ㄧ
            ["u"] = "\u3128",       // ㄨ
            ["\u00fc"] = "\u3129",  // ü → ㄩ
            ["er"] = "\u3126",      // ㄦ

            // 複韻母
            ["ai"] = "\u311E",      // ㄞ
            ["ei"] = "\u311F",      // ㄟ
            ["ao"] = "\u3120",      // ㄠ
            ["ou"] = "\u3121",      // ㄡ

            // 鼻韻母
            ["an"] = "\u3122",      // ㄢ
            ["en"] = "\u3123",      // ㄣ
            ["ang"] = "\u3124",     // ㄤ
            ["eng"] = "\u3125",     // ㄥ

            // i系韻母
            ["ia"] = "\u3127\u311A",    // ㄧㄚ
            ["ie"] = "\u3127\u311D",    // ㄧㄝ
            ["iao"] = "\u3127\u3120",   // ㄧㄠ
            ["iu"] = "\u3127\u3121",    // ㄧㄡ (iou → ㄧㄡ)
            ["ian"] = "\u3127\u3122",   // ㄧㄢ
            ["in"] = "\u3127\u3123",    // ㄧㄣ
            ["iang"] = "\u3127\u3124",  // ㄧㄤ
            ["ing"] = "\u3127\u3125",   // ㄧㄥ

            // u系韻母
            ["ua"] = "\u3128\u311A",    // ㄨㄚ
            ["uo"] = "\u3128\u311B",    // ㄨㄛ
            ["uai"] = "\u3128\u311E",   // ㄨㄞ
            ["ui"] = "\u3128\u311F",    // ㄨㄟ (uei → ㄨㄟ)
            ["uan"] = "\u3128\u3122",   // ㄨㄢ
            ["un"] = "\u3128\u3123",    // ㄨㄣ (uen → ㄨㄣ)
            ["uang"] = "\u3128\u3124",  // ㄨㄤ
            ["ong"] = "\u3128\u3125",   // ㄨㄥ
            ["ueng"] = "\u3128\u3125", // ㄨㄥ (weng用)

            // ü系韻母
            ["\u00fce"] = "\u3129\u311D",    // üe → ㄩㄝ
            ["\u00fcan"] = "\u3129\u3122",   // üan → ㄩㄢ
            ["\u00fcn"] = "\u3129\u3123",    // ün → ㄩㄣ
            ["iong"] = "\u3129\u3125",       // iong → ㄩㄥ
        };

        // zh/ch/sh/r/z/c/s: これらの声母の後のiは空韻母
        private static readonly HashSet<string> s_retroflexAndDental = new HashSet<string>(StringComparer.Ordinal)
        {
            "zh", "ch", "sh", "r", "z", "c", "s"
        };

        /// <summary>声調記号付きピンインを注音符号に変換する（声調付き）。</summary>
        /// <param name="pinyin">声調記号付きピンイン文字列（例: "zhōng"）。</param>
        /// <returns>注音符号文字列（例: "ㄓㄨㄥ"）。</returns>
        public static string Convert(string pinyin)
        {
            return Convert(pinyin, true);
        }

        /// <summary>声調記号付きピンインを注音符号に変換する。</summary>
        /// <param name="pinyin">声調記号付きピンイン文字列（例: "zhōng"）。</param>
        /// <param name="includeTones">trueなら声調マーカーを付加する。</param>
        /// <returns>注音符号文字列。</returns>
        public static string Convert(string pinyin, bool includeTones)
        {
            if (string.IsNullOrEmpty(pinyin))
                return string.Empty;

            // 1. 声調を抽出し、声調記号を除去
            var tone = ToneConverter.ExtractTone(pinyin);
            var bare = ToneConverter.RemoveTone(pinyin).ToLowerInvariant();

            if (bare.Length == 0)
                return string.Empty;

            // 2. 声母を特定（2文字声母を先に判定）
            string initialStr = null;
            string initialZhuyin = null;
            int initialLen = 0;

            // 2文字声母 zh, ch, sh を先にチェック
            if (bare.Length >= 2)
            {
                string two = bare.Substring(0, 2);
                if (s_initialMap.TryGetValue(two, out string zhuyin2))
                {
                    initialStr = two;
                    initialZhuyin = zhuyin2;
                    initialLen = 2;
                }
            }

            // 1文字声母をチェック（2文字声母が見つからなかった場合のみ）
            if (initialStr == null && bare.Length >= 1)
            {
                string one = bare.Substring(0, 1);
                if (s_initialMap.TryGetValue(one, out string zhuyin1))
                {
                    initialStr = one;
                    initialZhuyin = zhuyin1;
                    initialLen = 1;
                }
            }

            // 3. 残りの文字列から韻母を特定
            string remainder = bare.Substring(initialLen);

            // ゼロ声母の処理: y/w で始まる音節
            if (initialStr == null)
            {
                // yi → ㄧ, wu → ㄨ, yu → ㄩ 等
                return ConvertZeroInitial(bare, tone, includeTones);
            }

            // zh/ch/sh/r/z/c/s + i の場合: 空韻母（注音では声母のみ）
            if (s_retroflexAndDental.Contains(initialStr) && remainder == "i")
            {
                return AppendTone(initialZhuyin, tone, includeTones);
            }

            // j/q/x 後の u は ü として扱う
            if (IsJqx(initialStr) && remainder.Length > 0 && remainder[0] == 'u')
            {
                remainder = "\u00fc" + remainder.Substring(1);
            }

            // 韻母を変換
            string finalZhuyin = LookupFinal(remainder);
            if (finalZhuyin == null)
                return string.Empty;

            return AppendTone(initialZhuyin + finalZhuyin, tone, includeTones);
        }

        /// <summary>ゼロ声母の音節を処理する。</summary>
        private static string ConvertZeroInitial(string bare, Tone tone, bool includeTones)
        {
            // y 系
            if (bare.StartsWith("y", StringComparison.Ordinal))
            {
                string afterY = bare.Substring(1);

                if (afterY == "i" || afterY.Length == 0)
                {
                    // yi → ㄧ
                    return AppendTone("\u3127", tone, includeTones);
                }

                if (afterY == "u")
                {
                    // yu → ㄩ
                    return AppendTone("\u3129", tone, includeTones);
                }

                if (afterY.StartsWith("u", StringComparison.Ordinal))
                {
                    // yuan → ㄩㄢ, yue → ㄩㄝ, yun → ㄩㄣ
                    string vRemainder = "\u00fc" + afterY.Substring(1);
                    string vFinal = LookupFinal(vRemainder);
                    if (vFinal != null)
                        return AppendTone(vFinal, tone, includeTones);
                }

                // ya → ㄧㄚ, ye → ㄧㄝ, yao → ㄧㄠ, etc.
                // afterY を i+残り の韻母として検索
                string iFinal = LookupFinal("i" + afterY);
                if (iFinal != null)
                    return AppendTone(iFinal, tone, includeTones);

                // you → ㄧㄡ: iou の省略形 → "iu" で検索
                string shortFinal = GetShortFinalForY(afterY);
                if (shortFinal != null)
                {
                    iFinal = LookupFinal(shortFinal);
                    if (iFinal != null)
                        return AppendTone(iFinal, tone, includeTones);
                }

                // yin → ㄧㄣ にもマッチ: "in" を検索
                iFinal = LookupFinal(afterY);
                if (iFinal != null)
                    return AppendTone(iFinal, tone, includeTones);
            }

            // w 系
            if (bare.StartsWith("w", StringComparison.Ordinal))
            {
                string afterW = bare.Substring(1);

                if (afterW == "u" || afterW.Length == 0)
                {
                    // wu → ㄨ
                    return AppendTone("\u3128", tone, includeTones);
                }

                // wa → ㄨㄚ, wo → ㄨㄛ, wai → ㄨㄞ, etc.
                string uFinal = LookupFinal("u" + afterW);
                if (uFinal != null)
                    return AppendTone(uFinal, tone, includeTones);

                // wei → ㄨㄟ (uei省略形 → "ui"), wen → ㄨㄣ (uen省略形 → "un")
                string shortWFinal = GetShortFinalForW(afterW);
                if (shortWFinal != null)
                {
                    uFinal = LookupFinal(shortWFinal);
                    if (uFinal != null)
                        return AppendTone(uFinal, tone, includeTones);
                }
            }

            // 声母なし・y/wなし: a, o, e, ai, an, ang, er 等
            string directFinal = LookupFinal(bare);
            if (directFinal != null)
                return AppendTone(directFinal, tone, includeTones);

            return string.Empty;
        }

        /// <summary>韻母文字列を注音符号に変換する（最長一致）。</summary>
        private static string LookupFinal(string remainder)
        {
            if (string.IsNullOrEmpty(remainder))
                return null;

            if (s_finalMap.TryGetValue(remainder, out string zhuyin))
                return zhuyin;

            return null;
        }

        /// <summary>注音符号に声調マーカーを付加する。</summary>
        private static string AppendTone(string zhuyin, Tone tone, bool includeTones)
        {
            if (!includeTones || tone == Tone.First)
                return zhuyin;

            switch (tone)
            {
                case Tone.Second:
                    return zhuyin + Tone2Mark;
                case Tone.Third:
                    return zhuyin + Tone3Mark;
                case Tone.Fourth:
                    return zhuyin + Tone4Mark;
                case Tone.Neutral:
                    return NeutralMark + zhuyin;
                default:
                    return zhuyin;
            }
        }

        /// <summary>j/q/x の声母かどうか判定。</summary>
        private static bool IsJqx(string initial)
        {
            return initial == "j" || initial == "q" || initial == "x";
        }

        /// <summary>y系ゼロ声母の省略形韻母を返す（you→iu等）。</summary>
        private static string GetShortFinalForY(string afterY)
        {
            // you (iou省略形) → iu
            if (afterY == "ou")
                return "iu";
            return null;
        }

        /// <summary>w系ゼロ声母の省略形韻母を返す（wei→ui, wen→un等）。</summary>
        private static string GetShortFinalForW(string afterW)
        {
            // wei (uei省略形) → ui
            if (afterW == "ei")
                return "ui";
            // wen (uen省略形) → un
            if (afterW == "en")
                return "un";
            // weng → ueng (ただし通常は weng のまま)
            if (afterW == "eng")
                return "ueng";
            return null;
        }
    }
}
