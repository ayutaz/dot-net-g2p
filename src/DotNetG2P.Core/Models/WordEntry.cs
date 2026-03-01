using System;

namespace DotNetG2P.Models
{
    /// <summary>
    /// 辞書エントリ。表層形、詳細情報、アクセント情報を保持する。
    /// </summary>
    public sealed class WordEntry
    {
        /// <summary>表層形（原文中の文字列）</summary>
        public string Surface { get; }

        /// <summary>単語詳細情報</summary>
        public WordDetails Details { get; }

        /// <summary>アクセント情報文字列（"核位置/モーラ数"）</summary>
        public string AccentInfo { get; }

        /// <summary>アクセント結合タイプ（C1-C5等）</summary>
        public string ChainRule { get; }

        public WordEntry(string surface, WordDetails details, string accentInfo, string chainRule)
        {
            Surface = surface ?? "";
            Details = details ?? throw new ArgumentNullException(nameof(details));
            AccentInfo = accentInfo ?? "*";
            ChainRule = chainRule ?? "*";
        }

        /// <summary>
        /// ITokenからWordEntryを構築する。
        /// </summary>
        public static WordEntry FromToken(DotNetG2P.IToken token)
        {
            var details = WordDetails.FromToken(token);
            return new WordEntry(token.Surface, details, token.AccentInfo, token.ChainRule);
        }
    }
}
