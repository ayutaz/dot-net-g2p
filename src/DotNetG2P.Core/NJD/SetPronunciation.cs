using System;
using System.Collections.Generic;
using DotNetG2P.Models;

namespace DotNetG2P.NJD
{
    /// <summary>
    /// NJD処理の第1段階: 発音設定。
    /// 各NjdNodeに対してカタカナ発音からMoraリストを構築する。
    /// jpreprocess の open_jtalk/pronunciation.rs に相当（最小版）。
    /// </summary>
    public static class SetPronunciation
    {
        /// <summary>
        /// NjdNodeリストの各ノードに発音情報を設定する。
        /// トークンの発音フィールド（カタカナ）からMoraリストを構築し、
        /// NjdNodeのPronunciationに設定する。
        /// </summary>
        public static void Process(List<NjdNode> nodes, IReadOnlyList<IToken> tokens)
        {
            for (int i = 0; i < nodes.Count && i < tokens.Count; i++)
            {
                var node = nodes[i];
                var token = tokens[i];

                // 発音フィールドからカタカナを取得
                var katakana = token.Pronunciation;

                // 発音が"*"や空の場合は読みフィールドを試す
                if (string.IsNullOrEmpty(katakana) || katakana == "*")
                {
                    katakana = token.Reading;
                }

                // それでも取得できない場合は表層形をそのまま使用
                if (string.IsNullOrEmpty(katakana) || katakana == "*")
                {
                    katakana = token.Surface;
                }

                // カタカナからPronunciationを構築
                try
                {
                    node.Pronunciation = Pronunciation.FromKatakana(katakana, node.AccentType);
                }
                catch (ArgumentException)
                {
                    // 解析できない文字列の場合は空のPronunciationのまま
                }
            }
        }
    }
}
