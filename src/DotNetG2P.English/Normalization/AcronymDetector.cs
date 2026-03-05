// Copyright (c) 2026 DotNetG2P Contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace DotNetG2P.English.Normalization
{
    /// <summary>
    /// 全大文字トークン（頭字語）の読み方を判定する。
    /// スペルアウト（1文字ずつ読む: API → A P I）か、
    /// 1語読み（acronym: NASA → ナサ）かを判定し、スペルアウト文字列を生成する。
    /// </summary>
    internal static class AcronymDetector
    {
        // スペルアウト対象の既知トークン（1文字ずつ読む）
        private static readonly HashSet<string> s_spellOutSet = new HashSet<string>(StringComparer.Ordinal)
        {
            // コンピュータ・IT
            "USB", "CPU", "GPU", "API", "URL", "HTML", "CSS", "PDF", "SQL", "XML",
            "JSON", "HTTP", "HTTPS", "CLI", "GUI", "SDK", "RAM", "ROM", "SSD", "HDD",
            "AWS", "GCP", "IDE", "OOP", "TCP", "UDP", "FTP", "SSH", "DNS", "VPN",
            "SVG", "PNG", "GIF",

            // IoT・AI関連
            "IoT", "AI", "ML", "UI", "UX", "QA",

            // ビジネス・組織
            "CEO", "CFO", "CTO", "COO", "MVP", "VIP", "HR", "PR", "IT",

            // 生活・一般
            "DIY", "FAQ", "ATM", "PIN", "SIM", "GPS",
            "OK", "US", "UK", "EU", "UN", "AM", "PM",

            // メディア・放送
            "BMW", "IBM", "HBO", "BBC", "CNN", "NBC", "ABC", "ESPN",

            // スポーツ
            "NFL", "NBA", "MLB", "NHL", "UFC", "FIFA",

            // 組織
            "FBI", "CIA",
        };

        // 1語読み（acronym）の既知トークン（単語として発音する）
        private static readonly HashSet<string> s_acronymSet = new HashSet<string>(StringComparer.Ordinal)
        {
            "NASA", "NATO", "FEMA", "SWAT", "SCUBA", "LASER", "RADAR", "SONAR",
            "AIDS", "OPEC", "UNICEF", "UNESCO", "ASAP", "AWOL", "CAPTCHA", "JPEG",
        };

        /// <summary>
        /// トークンが2文字以上かつ全てA-Zであるかを判定する。
        /// 1文字、小文字混在、数字含みの場合はfalseを返す。
        /// </summary>
        /// <param name="token">判定対象のトークン</param>
        /// <returns>全大文字かどうか</returns>
        public static bool IsAllUpperCase(string token)
        {
            if (token == null || token.Length < 2)
                return false;

            for (int i = 0; i < token.Length; i++)
            {
                char c = token[i];
                if (c < 'A' || c > 'Z')
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 全大文字トークンをスペルアウト（1文字ずつ読む）すべきかを判定する。
        /// IsAllUpperCaseがfalseの場合は常にfalseを返す。
        /// </summary>
        /// <param name="token">判定対象のトークン</param>
        /// <returns>スペルアウトすべきならtrue</returns>
        public static bool ShouldSpellOut(string token)
        {
            if (!IsAllUpperCase(token))
                return false;

            // 既知のスペルアウト辞書に一致
            if (s_spellOutSet.Contains(token))
                return true;

            // 既知の1語読み辞書に一致
            if (s_acronymSet.Contains(token))
                return false;

            // ヒューリスティック判定
            // 2文字は常にスペルアウト
            if (token.Length == 2)
                return true;

            // 母音を含まなければスペルアウト（例: CTRL, FPS）
            bool hasVowel = false;
            for (int i = 0; i < token.Length; i++)
            {
                char c = token[i];
                if (c == 'A' || c == 'E' || c == 'I' || c == 'O' || c == 'U')
                {
                    hasVowel = true;
                    break;
                }
            }

            if (!hasVowel)
                return true;

            // 子音のみの連続が3文字以上あればスペルアウト
            int consonantRun = 0;
            for (int i = 0; i < token.Length; i++)
            {
                char c = token[i];
                if (c == 'A' || c == 'E' || c == 'I' || c == 'O' || c == 'U')
                {
                    consonantRun = 0;
                }
                else
                {
                    consonantRun++;
                    if (consonantRun >= 3)
                        return true;
                }
            }

            // 母音を含み、子音連続が3未満 → 1語読み（例: YOLO, BOGO）
            return false;
        }

        /// <summary>
        /// トークンの各文字をスペースで区切ったスペルアウト文字列を返す。
        /// 例: "API" → "A P I", "FBI" → "F B I"
        /// </summary>
        /// <param name="token">スペルアウト対象のトークン</param>
        /// <returns>スペース区切りの文字列</returns>
        public static string SpellOut(string token)
        {
            if (string.IsNullOrEmpty(token))
                return string.Empty;

            if (token.Length == 1)
                return token;

            // 文字数 + (文字数-1)個のスペース
            var chars = new char[token.Length * 2 - 1];
            chars[0] = token[0];
            for (int i = 1; i < token.Length; i++)
            {
                chars[i * 2 - 1] = ' ';
                chars[i * 2] = token[i];
            }

            return new string(chars);
        }
    }
}
