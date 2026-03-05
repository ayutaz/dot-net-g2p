using System;
using System.Collections.Generic;

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
            ["ms"] = "Miss",
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
        /// </summary>
        /// <param name="token">入力トークン</param>
        /// <returns>展開形。未知の略語の場合はnull。</returns>
        public static string? TryExpand(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            // 末尾のピリオドを除去して小文字化
            var key = token.TrimEnd('.').ToLowerInvariant();

            if (key.Length == 0)
                return null;

            return s_abbreviations.TryGetValue(key, out var expanded) ? expanded : null;
        }
    }
}
