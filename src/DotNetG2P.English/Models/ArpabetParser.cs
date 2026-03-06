using System;

namespace DotNetG2P.English
{
    /// <summary>
    /// ARPAbet文字列トークン（例: "HH", "AH0"）を <see cref="EnglishPhoneme"/> に変換するパーサー。
    /// </summary>
    public static class ArpabetParser
    {
        /// <summary>音素名テーブル（enumインデックス→文字列）</summary>
        private static readonly string[] PhonemeNames = new string[]
        {
            "AA", "AE", "AH", "AO", "AW", "AY", "EH", "ER", "EY", "IH", "IY", "OW", "OY", "UH", "UW",
            "B", "CH", "D", "DH", "F", "G", "HH", "JH", "K", "L", "M", "N", "NG", "P", "R", "S", "SH", "T", "TH", "V", "W", "Y", "Z", "ZH",
        };

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

            if (!TryParseCore(token, out var phoneme, out var stress))
                throw new ArgumentException($"未知のARPAbetトークンです: '{token}'", nameof(token));

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

            if (!TryParseCore(token, out var phoneme, out var stress))
                return false;

            result = new EnglishPhoneme(phoneme, stress);
            return true;
        }

        /// <summary>
        /// Substring不要のコアパーサー。
        /// トークン文字列から直接char値を読み取り、switch式で音素を特定する。
        /// </summary>
        private static bool TryParseCore(string token, out ArpabetPhoneme phoneme, out Stress stress)
        {
            phoneme = default;
            stress = Stress.None;

            var len = token.Length;

            // ストレス数字の判定（末尾が0/1/2か）
            var lastChar = token[len - 1];
            var hasStress = lastChar >= '0' && lastChar <= '2';
            // 音素名部分の長さ
            var nameLen = hasStress ? len - 1 : len;

            if (nameLen < 1 || nameLen > 2)
                return false;

            // 1文字音素名
            if (nameLen == 1)
            {
                if (!TryParseSingle(token[0], out phoneme))
                    return false;
            }
            // 2文字音素名
            else
            {
                if (!TryParseDouble(token[0], token[1], out phoneme))
                    return false;
            }

            // ストレスの設定
            if (hasStress)
            {
                switch (lastChar)
                {
                    case '0': stress = Stress.NoStress; break;
                    case '1': stress = Stress.Primary; break;
                    case '2': stress = Stress.Secondary; break;
                }
                // 子音にストレスが付いている不正入力はStress.Noneに強制
                if (phoneme >= ArpabetPhoneme.B)
                    stress = Stress.None;
            }

            return true;
        }

        /// <summary>1文字音素名のパース（B, D, F, G, K, L, M, N, P, R, S, T, V, W, Y, Z）</summary>
        private static bool TryParseSingle(char c, out ArpabetPhoneme phoneme)
        {
            switch (c)
            {
                case 'B': phoneme = ArpabetPhoneme.B; return true;
                case 'D': phoneme = ArpabetPhoneme.D; return true;
                case 'F': phoneme = ArpabetPhoneme.F; return true;
                case 'G': phoneme = ArpabetPhoneme.G; return true;
                case 'K': phoneme = ArpabetPhoneme.K; return true;
                case 'L': phoneme = ArpabetPhoneme.L; return true;
                case 'M': phoneme = ArpabetPhoneme.M; return true;
                case 'N': phoneme = ArpabetPhoneme.N; return true;
                case 'P': phoneme = ArpabetPhoneme.P; return true;
                case 'R': phoneme = ArpabetPhoneme.R; return true;
                case 'S': phoneme = ArpabetPhoneme.S; return true;
                case 'T': phoneme = ArpabetPhoneme.T; return true;
                case 'V': phoneme = ArpabetPhoneme.V; return true;
                case 'W': phoneme = ArpabetPhoneme.W; return true;
                case 'Y': phoneme = ArpabetPhoneme.Y; return true;
                case 'Z': phoneme = ArpabetPhoneme.Z; return true;
                default:
                    phoneme = default;
                    return false;
            }
        }

        /// <summary>2文字音素名のパース（AA, AE, AH, ... ZH）</summary>
        private static bool TryParseDouble(char c1, char c2, out ArpabetPhoneme phoneme)
        {
            // 第1文字で分岐し、第2文字で確定
            switch (c1)
            {
                case 'A':
                    switch (c2)
                    {
                        case 'A': phoneme = ArpabetPhoneme.AA; return true;
                        case 'E': phoneme = ArpabetPhoneme.AE; return true;
                        case 'H': phoneme = ArpabetPhoneme.AH; return true;
                        case 'O': phoneme = ArpabetPhoneme.AO; return true;
                        case 'W': phoneme = ArpabetPhoneme.AW; return true;
                        case 'Y': phoneme = ArpabetPhoneme.AY; return true;
                    }
                    break;
                case 'C':
                    if (c2 == 'H') { phoneme = ArpabetPhoneme.CH; return true; }
                    break;
                case 'D':
                    if (c2 == 'H') { phoneme = ArpabetPhoneme.DH; return true; }
                    break;
                case 'E':
                    switch (c2)
                    {
                        case 'H': phoneme = ArpabetPhoneme.EH; return true;
                        case 'R': phoneme = ArpabetPhoneme.ER; return true;
                        case 'Y': phoneme = ArpabetPhoneme.EY; return true;
                    }
                    break;
                case 'H':
                    if (c2 == 'H') { phoneme = ArpabetPhoneme.HH; return true; }
                    break;
                case 'I':
                    switch (c2)
                    {
                        case 'H': phoneme = ArpabetPhoneme.IH; return true;
                        case 'Y': phoneme = ArpabetPhoneme.IY; return true;
                    }
                    break;
                case 'J':
                    if (c2 == 'H') { phoneme = ArpabetPhoneme.JH; return true; }
                    break;
                case 'N':
                    if (c2 == 'G') { phoneme = ArpabetPhoneme.NG; return true; }
                    break;
                case 'O':
                    switch (c2)
                    {
                        case 'W': phoneme = ArpabetPhoneme.OW; return true;
                        case 'Y': phoneme = ArpabetPhoneme.OY; return true;
                    }
                    break;
                case 'S':
                    if (c2 == 'H') { phoneme = ArpabetPhoneme.SH; return true; }
                    break;
                case 'T':
                    if (c2 == 'H') { phoneme = ArpabetPhoneme.TH; return true; }
                    break;
                case 'U':
                    switch (c2)
                    {
                        case 'H': phoneme = ArpabetPhoneme.UH; return true;
                        case 'W': phoneme = ArpabetPhoneme.UW; return true;
                    }
                    break;
                case 'Z':
                    if (c2 == 'H') { phoneme = ArpabetPhoneme.ZH; return true; }
                    break;
            }

            phoneme = default;
            return false;
        }
    }
}
