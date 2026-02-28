using System.Collections.Generic;

namespace DotNetG2P.Models
{
    /// <summary>
    /// NJD（Natural Language Processing for Japanese text to speech Documents）ノード。
    /// OpenJTalkのNJD処理パイプラインで使用される中間表現。
    /// </summary>
    public sealed class NjdNode
    {
        /// <summary>表層形</summary>
        public string Surface { get; set; }

        /// <summary>単語詳細情報</summary>
        public WordDetails Details { get; set; }

        /// <summary>発音情報</summary>
        public Pronunciation Pronunciation { get; set; }

        /// <summary>アクセント型番号</summary>
        public int AccentType { get; set; }

        /// <summary>前ノードとの結合フラグ</summary>
        public bool ChainFlag { get; set; }

        /// <summary>アクセント結合ルール文字列</summary>
        public string ChainRule { get; set; }

        /// <summary>モーラ数</summary>
        public int MoraCount => Pronunciation?.MoraCount ?? 0;

        public NjdNode(string surface, WordDetails details)
        {
            Surface = surface ?? "";
            Details = details;
            Pronunciation = new Pronunciation();
            AccentType = 0;
            ChainFlag = false;
            ChainRule = "*";
        }

        /// <summary>
        /// トークン列からNjdNodeリストを構築する。
        /// </summary>
        public static List<NjdNode> FromTokens(IReadOnlyList<DotNetG2P.IToken> tokens)
        {
            var nodes = new List<NjdNode>();
            foreach (var token in tokens)
            {
                var entry = WordEntry.FromToken(token);
                var node = new NjdNode(entry.Surface, entry.Details)
                {
                    ChainRule = entry.ChainRule
                };

                // アクセント情報のパース（"核位置/モーラ数" → AccentType）
                if (entry.AccentInfo != null && entry.AccentInfo != "*")
                {
                    var parts = entry.AccentInfo.Split('/');
                    if (parts.Length >= 1 && int.TryParse(parts[0], out var accentType))
                    {
                        node.AccentType = accentType;
                    }
                }

                nodes.Add(node);
            }
            return nodes;
        }
    }
}
