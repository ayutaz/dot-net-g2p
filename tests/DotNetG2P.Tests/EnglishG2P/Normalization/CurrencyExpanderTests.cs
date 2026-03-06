// Copyright (c) 2026 DotNetG2P Contributors
// SPDX-License-Identifier: Apache-2.0

using DotNetG2P.English.Normalization;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Normalization
{
    /// <summary>
    /// CurrencyExpander の通貨展開テスト。
    /// </summary>
    public class CurrencyExpanderTests
    {
        // ===== ドル ($) =====

        [Fact]
        public void TryExpand_Dollar_IntegerOnly()
        {
            Assert.Equal("five dollars", CurrencyExpander.TryExpand("$5"));
        }

        [Fact]
        public void TryExpand_Dollar_Singular()
        {
            Assert.Equal("one dollar", CurrencyExpander.TryExpand("$1"));
        }

        [Fact]
        public void TryExpand_Dollar_CentsOnly()
        {
            // 整数部0、小数部99 → セント部分のみ
            Assert.Equal("ninety nine cents", CurrencyExpander.TryExpand("$0.99"));
        }

        [Fact]
        public void TryExpand_Dollar_WithCents()
        {
            Assert.Equal("one dollar fifty cents", CurrencyExpander.TryExpand("$1.50"));
        }

        [Fact]
        public void TryExpand_Dollar_OneCent()
        {
            // $0.01 → 小数部を2桁正規化: "01" → 1 → 単数形 "cent"
            Assert.Equal("one cent", CurrencyExpander.TryExpand("$0.01"));
        }

        [Fact]
        public void TryExpand_Dollar_WithComma()
        {
            Assert.Equal("one thousand dollars", CurrencyExpander.TryExpand("$1,000"));
        }

        // ===== ポンド (£) =====

        [Fact]
        public void TryExpand_Pound()
        {
            Assert.Equal("five pounds", CurrencyExpander.TryExpand("£5"));
        }

        // ===== ユーロ (€) =====

        [Fact]
        public void TryExpand_Euro()
        {
            Assert.Equal("ten euros", CurrencyExpander.TryExpand("€10"));
        }

        // ===== 円 (¥) =====

        [Fact]
        public void TryExpand_Yen()
        {
            // 円は単複同形、小数部なし
            Assert.Equal("five hundred yen", CurrencyExpander.TryExpand("¥500"));
        }

        // ===== 非通貨・エッジケース =====

        [Fact]
        public void TryExpand_NonCurrency_ReturnsNull()
        {
            Assert.Null(CurrencyExpander.TryExpand("hello"));
        }

        [Fact]
        public void TryExpand_EmptyString_ReturnsNull()
        {
            Assert.Null(CurrencyExpander.TryExpand(""));
        }

        // ===== 追加エッジケース =====

        [Fact]
        public void TryExpand_YenWithDecimal_ReturnsNull()
        {
            // 円は小数部なし → ExpandYenで "100.50" はlong.TryParseに失敗するためnull
            Assert.Null(CurrencyExpander.TryExpand("¥100.50"));
        }

        [Fact]
        public void TryExpand_Dollar_ZeroDollarsZeroCents()
        {
            // $0.00 → 整数部0、小数部0 → 整数部が0で小数部も0なので "zero dollars" になる
            var result = CurrencyExpander.TryExpand("$0.00");
            Assert.NotNull(result);
            Assert.Equal("zero dollars", result);
        }

        [Fact]
        public void TryExpand_Dollar_OneDollarFiftyCents_SingleDecimalDigit()
        {
            // $1.5 → 小数部1桁 "5" → 50に正規化 → "one dollar fifty cents"
            Assert.Equal("one dollar fifty cents", CurrencyExpander.TryExpand("$1.5"));
        }
    }
}
