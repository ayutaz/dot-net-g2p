using System;
using System.Collections.Generic;

namespace DotNetG2P.Chinese
{
    /// <summary>ピンイン文字列をPinyinSyllableに分解するパーサ。</summary>
    internal static class PinyinParser
    {
        // 声母文字列→Initial enumのマッピング（最長一致用に長い順で検索）
        private static readonly (string Text, Initial Initial)[] s_initials = new (string, Initial)[]
        {
            // 2文字声母（最長一致のため先に判定）
            ("zh", Initial.Zh),
            ("ch", Initial.Ch),
            ("sh", Initial.Sh),
            // 1文字声母
            ("b", Initial.B),
            ("p", Initial.P),
            ("m", Initial.M),
            ("f", Initial.F),
            ("d", Initial.D),
            ("t", Initial.T),
            ("n", Initial.N),
            ("l", Initial.L),
            ("g", Initial.G),
            ("k", Initial.K),
            ("h", Initial.H),
            ("j", Initial.J),
            ("q", Initial.Q),
            ("x", Initial.X),
            ("r", Initial.R),
            ("z", Initial.Z),
            ("c", Initial.C),
            ("s", Initial.S),
            ("y", Initial.Y),
            ("w", Initial.W),
        };

        // 韻母文字列→Final enumのマッピング（最長一致用に長い順で検索）
        private static readonly Dictionary<string, Final> s_finals = BuildFinalMap();

        // 韻母の最長一致検索順（4文字→3文字→2文字→1文字）
        private static readonly string[] s_finalsByLength = BuildFinalsByLength();

        /// <summary>声調記号付きピンイン文字列をPinyinSyllableに変換（例: "zhōng" → PinyinSyllable(Zh, Ong, First)）。</summary>
        /// <param name="pinyin">声調記号付きまたは声調数字付きのピンイン文字列。</param>
        /// <returns>パース結果のPinyinSyllable。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pinyin"/>がnullの場合。</exception>
        /// <exception cref="FormatException">ピンイン文字列のパースに失敗した場合。</exception>
        public static PinyinSyllable Parse(string pinyin)
        {
            if (pinyin == null)
                throw new ArgumentNullException(nameof(pinyin));

            if (TryParse(pinyin, out var result))
                return result;

            throw new FormatException($"ピンイン文字列のパースに失敗しました: \"{pinyin}\"");
        }

        /// <summary>パース試行（失敗時false）。</summary>
        /// <param name="pinyin">声調記号付きまたは声調数字付きのピンイン文字列。</param>
        /// <param name="result">パース結果のPinyinSyllable。失敗時はdefault。</param>
        /// <returns>パース成功時true、失敗時false。</returns>
        public static bool TryParse(string pinyin, out PinyinSyllable result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(pinyin))
                return false;

            // 1. ToneConverterで声調を抽出し、声調記号を除去
            var tone = ToneConverter.ExtractTone(pinyin);
            var bare = ToneConverter.RemoveTone(pinyin).ToLowerInvariant();

            if (bare.Length == 0)
                return false;

            // 2. 声母を判別（最長一致）
            var initial = Initial.None;
            int initialLen = 0;

            for (int i = 0; i < s_initials.Length; i++)
            {
                var (text, init) = s_initials[i];
                if (bare.Length >= text.Length
                    && string.CompareOrdinal(bare, 0, text, 0, text.Length) == 0)
                {
                    initial = init;
                    initialLen = text.Length;
                    break;
                }
            }

            // 3. 残りを韻母として判別
            string remainder;

            // j/q/x/y 後の "u" は "ü" として扱う
            if (IsJqxy(initial) && initialLen < bare.Length && bare[initialLen] == 'u')
            {
                remainder = "v" + bare.Substring(initialLen + 1);
            }
            else
            {
                remainder = bare.Substring(initialLen);
            }

            // "v" 入力 → "ü" 系韻母として扱う（ü を v に正規化）
            // ü (U+00FC) も v に正規化
            remainder = NormalizeUmlaut(remainder);

            var final_ = Final.None;

            if (remainder.Length > 0)
            {
                if (!TryMatchFinal(remainder, out final_))
                    return false;
            }

            // 声母も韻母もないケースは無効
            if (initial == Initial.None && final_ == Final.None)
                return false;

            result = new PinyinSyllable(initial, final_, tone);
            return true;
        }

        /// <summary>j/q/x/y の声母かどうかを判定。</summary>
        private static bool IsJqxy(Initial initial)
        {
            return initial == Initial.J
                || initial == Initial.Q
                || initial == Initial.X
                || initial == Initial.Y;
        }

        /// <summary>ü (U+00FC) を v に正規化する。</summary>
        private static string NormalizeUmlaut(string s)
        {
            // ü → v, ǖ/ǘ/ǚ/ǜ は ToneConverter.RemoveTone() で既に ü に変換されているはず
            if (s.IndexOf('\u00FC') >= 0)
                return s.Replace('\u00FC', 'v');
            return s;
        }

        /// <summary>韻母文字列を最長一致で検索する。</summary>
        private static bool TryMatchFinal(string remainder, out Final final_)
        {
            // 最長一致で韻母を検索
            for (int i = 0; i < s_finalsByLength.Length; i++)
            {
                string candidate = s_finalsByLength[i];
                if (candidate.Length <= remainder.Length
                    && string.CompareOrdinal(remainder, 0, candidate, 0, candidate.Length) == 0
                    && candidate.Length == remainder.Length) // 完全一致のみ
                {
                    final_ = s_finals[candidate];
                    return true;
                }
            }

            final_ = Final.None;
            return false;
        }

        /// <summary>韻母文字列→Final enumのマッピングを構築。</summary>
        private static Dictionary<string, Final> BuildFinalMap()
        {
            return new Dictionary<string, Final>(36, StringComparer.Ordinal)
            {
                // ── 開口呼 (a/o/e系) ──
                ["a"] = Final.A,
                ["o"] = Final.O,
                ["e"] = Final.E,
                ["ai"] = Final.Ai,
                ["ei"] = Final.Ei,
                ["ao"] = Final.Ao,
                ["ou"] = Final.Ou,
                ["an"] = Final.An,
                ["en"] = Final.En,
                ["ang"] = Final.Ang,
                ["eng"] = Final.Eng,
                ["ong"] = Final.Ong,

                // ── 齊齒呼 (i系) ──
                ["i"] = Final.I,
                ["ia"] = Final.Ia,
                ["ie"] = Final.Ie,
                ["iao"] = Final.Iao,
                ["iu"] = Final.Iu,
                ["ian"] = Final.Ian,
                ["in"] = Final.In,
                ["iang"] = Final.Iang,
                ["ing"] = Final.Ing,
                ["iong"] = Final.Iong,

                // ── 合口呼 (u系) ──
                ["u"] = Final.U,
                ["ua"] = Final.Ua,
                ["uo"] = Final.Uo,
                ["uai"] = Final.Uai,
                ["ui"] = Final.Ui,
                ["uan"] = Final.Uan,
                ["un"] = Final.Un,
                ["uang"] = Final.Uang,
                ["ueng"] = Final.Ueng,

                // ── 撮口呼 (ü系、v表記) ──
                ["v"] = Final.V,
                ["ve"] = Final.Ve,
                ["van"] = Final.Van,
                ["vn"] = Final.Vn,

                // ── 特殊韻母 ──
                ["er"] = Final.Er,
            };
        }

        /// <summary>韻母文字列を長い順に並べた配列を構築（最長一致用）。</summary>
        private static string[] BuildFinalsByLength()
        {
            var keys = new List<string>(36)
            {
                // 4文字韻母
                "iang", "iong", "uang", "ueng",
                // 3文字韻母
                "iao", "ian", "ing", "ong",
                "uai", "uan", "ang", "eng",
                "van",
                // 2文字韻母
                "ai", "ei", "ao", "ou",
                "an", "en",
                "ia", "ie", "iu", "in",
                "ua", "uo", "ui", "un",
                "ve", "vn",
                "er",
                // 1文字韻母
                "a", "o", "e", "i", "u", "v",
            };
            return keys.ToArray();
        }
    }
}
