using System;
using System.Collections.Generic;

namespace DotNetG2P.English
{
    /// <summary>
    /// ARPAbet文字列トークン（例: "HH", "AH0"）を <see cref="EnglishPhoneme"/> に変換するパーサー。
    /// </summary>
    public static class ArpabetParser
    {
        private static readonly Dictionary<string, ArpabetPhoneme> StringToPhoneme;
        private static readonly string[] PhonemeNames;

        static ArpabetParser()
        {
            // 全39音素の名前テーブル
            PhonemeNames = new string[]
            {
                "AA", "AE", "AH", "AO", "AW", "AY", "EH", "ER", "EY", "IH", "IY", "OW", "OY", "UH", "UW",
                "B", "CH", "D", "DH", "F", "G", "HH", "JH", "K", "L", "M", "N", "NG", "P", "R", "S", "SH", "T", "TH", "V", "W", "Y", "Z", "ZH",
            };

            StringToPhoneme = new Dictionary<string, ArpabetPhoneme>(PhonemeNames.Length, StringComparer.Ordinal);
            for (var i = 0; i < PhonemeNames.Length; i++)
            {
                StringToPhoneme[PhonemeNames[i]] = (ArpabetPhoneme)i;
            }
        }

        /// <summary>
        /// ARPAbet音素名文字列を返す（例: ArpabetPhoneme.AH → "AH"）。
        /// </summary>
        /// <param name="phoneme">ARPAbet音素</param>
        /// <returns>音素名文字列</returns>
        public static string PhonemeToString(ArpabetPhoneme phoneme)
        {
            var idx = (int)phoneme;
            if (idx < 0 || idx >= PhonemeNames.Length)
                throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, "未知のARPAbet音素です。");
            return PhonemeNames[idx];
        }

        /// <summary>
        /// CMU辞書のトークン文字列（例: "AH0", "K"）を <see cref="EnglishPhoneme"/> に変換する。
        /// </summary>
        /// <param name="token">ARPAbetトークン文字列</param>
        /// <returns>変換された音素</returns>
        /// <exception cref="ArgumentException">未知のトークンの場合</exception>
        public static EnglishPhoneme Parse(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("トークンが空です。", nameof(token));

            // 末尾がストレス数字（0, 1, 2）かチェック
            var lastChar = token[token.Length - 1];
            string phonemePart;
            var stress = Stress.None;

            if (lastChar >= '0' && lastChar <= '2')
            {
                phonemePart = token.Substring(0, token.Length - 1);
                switch (lastChar)
                {
                    case '0': stress = Stress.NoStress; break;
                    case '1': stress = Stress.Primary; break;
                    case '2': stress = Stress.Secondary; break;
                }
            }
            else
            {
                phonemePart = token;
            }

            if (!StringToPhoneme.TryGetValue(phonemePart, out var phoneme))
                throw new ArgumentException($"未知のARPAbetトークンです: '{token}'", nameof(token));

            // 子音にストレスが付いている不正入力はStress.Noneに強制
            if (stress != Stress.None && phoneme >= ArpabetPhoneme.B)
                stress = Stress.None;

            return new EnglishPhoneme(phoneme, stress);
        }

        /// <summary>
        /// CMU辞書のトークン文字列の変換を試みる。
        /// </summary>
        /// <param name="token">ARPAbetトークン文字列</param>
        /// <param name="result">変換結果</param>
        /// <returns>変換に成功した場合 true</returns>
        public static bool TryParse(string token, out EnglishPhoneme result)
        {
            result = default;

            if (string.IsNullOrEmpty(token))
                return false;

            var lastChar = token[token.Length - 1];
            string phonemePart;
            var stress = Stress.None;

            if (lastChar >= '0' && lastChar <= '2')
            {
                phonemePart = token.Substring(0, token.Length - 1);
                switch (lastChar)
                {
                    case '0': stress = Stress.NoStress; break;
                    case '1': stress = Stress.Primary; break;
                    case '2': stress = Stress.Secondary; break;
                }
            }
            else
            {
                phonemePart = token;
            }

            if (!StringToPhoneme.TryGetValue(phonemePart, out var phoneme))
                return false;

            // 子音にストレスが付いている不正入力はStress.Noneに強制
            if (stress != Stress.None && phoneme >= ArpabetPhoneme.B)
                stress = Stress.None;

            result = new EnglishPhoneme(phoneme, stress);
            return true;
        }
    }
}
