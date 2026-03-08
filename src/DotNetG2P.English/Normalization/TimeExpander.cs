// Copyright (c) 2026 DotNetG2P Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace DotNetG2P.English.Normalization
{
    /// <summary>
    /// 時刻パターン（H:MM / HH:MM）を英語読みに展開する。
    /// 例: "3:14" → "three fourteen", "3:00" → "three o'clock"
    /// </summary>
    internal static class TimeExpander
    {
        /// <summary>
        /// トークンが時刻パターンであれば英語読みに展開する。非時刻ならnullを返す。
        /// </summary>
        /// <param name="token">入力トークン（例: "3:14", "12:00"）</param>
        /// <returns>英語読み文字列。時刻パターンでなければnull。</returns>
        public static string TryExpand(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            // ":"で分割し、ちょうど2パートであること
            int colonIndex = token.IndexOf(':');
            if (colonIndex < 0)
            {
                return null;
            }

            // ":"が複数ある場合は不正
            if (token.IndexOf(':', colonIndex + 1) >= 0)
            {
                return null;
            }

            string hourPart = token.Substring(0, colonIndex);
            string minutePart = token.Substring(colonIndex + 1);

            // 時間部分: 1-2桁の数字
            if (hourPart.Length < 1 || hourPart.Length > 2)
            {
                return null;
            }

            // 分部分: ちょうど2桁の数字
            if (minutePart.Length != 2)
            {
                return null;
            }

            // 数字のみで構成されていることを検証
            if (!IsDigits(hourPart) || !IsDigits(minutePart))
            {
                return null;
            }

            int hour = int.Parse(hourPart);
            int minute = int.Parse(minutePart);

            // 範囲チェック: 時間0-23、分0-59
            if (hour < 0 || hour > 23 || minute < 0 || minute > 59)
            {
                return null;
            }

            // 24時間制（13-23時）の処理: "時間 hundred" または "時間 分の読み"
            if (hour >= 13)
            {
                string hourWord = NumberToWords.Cardinal(hour);
                if (minute == 0)
                {
                    return string.Concat(hourWord, " hundred");
                }
                if (minute >= 1 && minute <= 9)
                {
                    return string.Concat(hourWord, " oh ", NumberToWords.Cardinal(minute));
                }
                return string.Concat(hourWord, " ", NumberToWords.Cardinal(minute));
            }

            // 0時 → 12として読む（midnight = twelve o'clock）
            int displayHour = hour == 0 ? 12 : hour;
            string displayHourWord = NumberToWords.Cardinal(displayHour);

            // 分が0 → "時間 o'clock"
            if (minute == 0)
            {
                return string.Concat(displayHourWord, " o'clock");
            }

            // 分が1-9 → "時間 oh 分の読み"
            if (minute >= 1 && minute <= 9)
            {
                return string.Concat(displayHourWord, " oh ", NumberToWords.Cardinal(minute));
            }

            // 分が10-59 → "時間 分の読み"
            return string.Concat(displayHourWord, " ", NumberToWords.Cardinal(minute));
        }

        /// <summary>
        /// 文字列がすべてASCII数字で構成されているか検証する。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDigits(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] < '0' || s[i] > '9')
                {
                    return false;
                }
            }
            return true;
        }
    }
}
