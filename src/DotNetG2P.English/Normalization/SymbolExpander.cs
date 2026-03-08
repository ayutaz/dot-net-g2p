// Copyright (c) 2026 DotNetG2P Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DotNetG2P.English.Normalization
{
    /// <summary>
    /// 単一記号文字を英語名に展開する。
    /// 英字・数字が混在するトークンや複数文字のトークンは対象外。
    /// </summary>
    internal static class SymbolExpander
    {
        // 記号→英語名の変換テーブル
        // ! や ? は文末句読点として頻出するため対象外
        private static readonly Dictionary<char, string> s_symbolTable = new Dictionary<char, string>(14)
        {
            ['@'] = "at",
            ['#'] = "hash",
            ['&'] = "and",
            ['%'] = "percent",
            ['+'] = "plus",
            ['='] = "equals",
            ['*'] = "asterisk",
            ['/'] = "slash",
            ['\\'] = "backslash",
            ['|'] = "pipe",
            ['~'] = "tilde",
            ['^'] = "caret",
            ['<'] = "less than",
            ['>'] = "greater than",
            ['_'] = "underscore",
        };

        /// <summary>
        /// トークンが単一の記号文字の場合、対応する英語名を返す。
        /// それ以外の場合は null を返す。
        /// </summary>
        /// <param name="token">入力トークン。</param>
        /// <returns>記号の英語名。対象外の場合は null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? TryExpand(string token)
        {
            if (token == null || token.Length != 1)
                return null;

            return s_symbolTable.TryGetValue(token[0], out var expanded) ? expanded : null;
        }
    }
}
