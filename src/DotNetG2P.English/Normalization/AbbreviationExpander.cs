using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DotNetG2P.English.Normalization
{
    /// <summary>
    /// 既知の英語略語を正式名称に展開する。
    /// ピリオド付き/なし・大文字小文字不問で略語を認識する。
    /// </summary>
    internal static class AbbreviationExpander
    {
        // キーは小文字・ピリオドなしで正規化済み
        private static readonly Dictionary<string, string> s_abbreviations = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // 敬称
            ["dr"] = "Doctor",
            ["mr"] = "Mister",
            ["mrs"] = "Misses",
            ["ms"] = "Miz",
            ["prof"] = "Professor",
            ["rev"] = "Reverend",

            // 場所
            ["st"] = "Street",
            ["ave"] = "Avenue",
            ["blvd"] = "Boulevard",
            ["rd"] = "Road",
            ["hwy"] = "Highway",
            ["ln"] = "Lane",

            // 一般
            ["etc"] = "etcetera",
            ["vs"] = "versus",
            ["inc"] = "Incorporated",
            ["corp"] = "Corporation",
            ["ltd"] = "Limited",
            ["jr"] = "Junior",
            ["sr"] = "Senior",
            ["dept"] = "Department",
            ["univ"] = "University",
            ["gov"] = "Governor",
            ["gen"] = "General",
            ["sgt"] = "Sergeant",
            ["capt"] = "Captain",
            ["col"] = "Colonel",
            ["lt"] = "Lieutenant",
            ["fig"] = "Figure",
            ["vol"] = "Volume",
            ["no"] = "Number",
            ["approx"] = "Approximately",

            // 月
            ["jan"] = "January",
            ["feb"] = "February",
            ["mar"] = "March",
            ["apr"] = "April",
            ["jun"] = "June",
            ["jul"] = "July",
            ["aug"] = "August",
            ["sep"] = "September",
            ["oct"] = "October",
            ["nov"] = "November",
            ["dec"] = "December",
        };

        /// <summary>
        /// トークンが既知の略語であれば展開形を返す。未知の場合はnullを返す。
        /// ピリオド付き（"Dr."）/なし（"Dr"）のどちらにも対応する。
        /// 大文字小文字は不問。
        /// ただし "no" は大文字始まり（"No"/"No."）の場合のみ "Number" に展開する。
        /// </summary>
        /// <param name="token">入力トークン</param>
        /// <returns>展開形。未知の略語の場合はnull。</returns>
        // 辞書内最長キーは6文字（"approx"）
        private const int MaxKeyLength = 6;

        public static string? TryExpand(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            // 実効長を計算（末尾ピリオドをスキップ）
            int len = token.Length;
            while (len > 0 && token[len - 1] == '.')
                len--;

            if (len == 0 || len > MaxKeyLength)
                return null;

            // 短い略語のみ検索（アロケーション回避のためSpanベース小文字化）
            var key = ToLowerInvariantSubstring(token, len);

            if (!s_abbreviations.TryGetValue(key, out var expanded))
                return null;

            // "no" → "Number" は大文字始まり（"No", "No."）の場合のみ展開
            if (key == "no" && token[0] != 'N')
                return null;

            return expanded;
        }

        /// <summary>
        /// token[0..length) を小文字化した文字列を返す。
        /// stackallocで短い文字列のヒープアロケーションを回避。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ToLowerInvariantSubstring(string token, int length)
        {
            // MaxKeyLength以下なのでstackallocで安全
            Span<char> buf = stackalloc char[length];
            for (int i = 0; i < length; i++)
            {
                char c = token[i];
                // ASCII大文字→小文字
                if ((uint)(c - 'A') <= ('Z' - 'A'))
                    buf[i] = (char)(c | 0x20);
                else
                    buf[i] = c;
            }
            return new string(buf);
        }
    }
}
