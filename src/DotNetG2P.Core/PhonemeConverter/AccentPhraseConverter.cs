using System.Collections.Generic;
using DotNetG2P.Models;

namespace DotNetG2P.PhonemeConverter
{
    /// <summary>
    /// NjdNodeリストからVOICEVOX互換のAccentPhraseリストを生成するコンバータ。
    /// RunPipeline後のNjdNodeリストを受け取り、ChainFlagに基づいてノードをグループ化し、
    /// 各グループを1つのAccentPhraseに変換する。
    /// </summary>
    public static class AccentPhraseConverter
    {
        /// <summary>
        /// NjdNodeリストからAccentPhraseリストを生成する。
        /// </summary>
        /// <param name="nodes">RunPipeline後のNjdNodeリスト</param>
        /// <returns>VOICEVOX互換のAccentPhraseリスト</returns>
        public static List<AccentPhrase> Convert(IReadOnlyList<NjdNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return new List<AccentPhrase>();
            var result = new List<AccentPhrase>(nodes.Count / 3 + 1);

            // ChainFlagに基づいてノードをアクセント句ごとにグループ化する。
            // ChainFlag=true のノードは前のノードと同じアクセント句に属する。
            // ChainFlag=false/null のノードは新しいアクセント句の開始。
            var groups = GroupByAccentPhrase(nodes);

            foreach (var group in groups)
            {
                var phrase = BuildAccentPhrase(group);
                if (phrase != null)
                    result.Add(phrase);
            }

            // ポーズモーラの処理: 句点/読点ノードのみのアクセント句は、
            // 直前のAccentPhraseのPauseMoraとして設定する
            MergePauseMoras(result);

            return result;
        }

        /// <summary>
        /// ChainFlagに基づいてノードをアクセント句グループに分割する。
        /// </summary>
        private static List<List<NjdNode>> GroupByAccentPhrase(IReadOnlyList<NjdNode> nodes)
        {
            var groups = new List<List<NjdNode>>(nodes.Count / 3 + 1);
            List<NjdNode> currentGroup = null;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                // 空ノード（Reset済み）はスキップ
                if (node.IsEmpty)
                    continue;

                // アクセント句の開始判定:
                // - 最初のノード
                // - ChainFlag が false または null（結合しない/未設定）
                if (i == 0 || node.ChainFlag != true)
                {
                    currentGroup = new List<NjdNode>();
                    groups.Add(currentGroup);
                }

                currentGroup.Add(node);
            }

            return groups;
        }

        /// <summary>
        /// ノードグループから1つのAccentPhraseを構築する。
        /// </summary>
        private static AccentPhrase BuildAccentPhrase(List<NjdNode> group)
        {
            if (group == null || group.Count == 0)
                return null;

            var moras = new List<Mora>(8);
            bool isInterrogative = false;
            bool isPauseOnly = true;

            foreach (var node in group)
            {
                if (node.Pronunciation == null)
                    continue;

                // 疑問符ノードの検出
                if (node.Pronunciation.IsQuestion)
                {
                    isInterrogative = true;
                    continue;
                }

                foreach (var mora in node.Pronunciation.Moras)
                {
                    // Toutenモーラはスキップ（PauseMoraとして後処理）
                    if (mora.Kind == MoraKind.Touten)
                        continue;

                    // Questionモーラはフラグ設定のみ
                    if (mora.Kind == MoraKind.Question)
                    {
                        isInterrogative = true;
                        continue;
                    }

                    moras.Add(mora);
                    isPauseOnly = false;
                }
            }

            // 句点/読点のみのアクセント句はToutenのみのフラグ付きで返す
            if (isPauseOnly && !isInterrogative)
            {
                // Toutenのみのアクセント句: PauseMora処理用のマーカーとして返す
                var pausePhrase = new AccentPhrase();
                pausePhrase.Accent = 0;
                pausePhrase.PauseMora = new Mora(null, null, MoraKind.Touten);
                return pausePhrase;
            }

            if (moras.Count == 0 && !isInterrogative)
                return null;

            // アクセント句先頭ノードのAccentTypeを使用
            int accent = group[0].AccentType;

            var phrase = new AccentPhrase(moras, accent);
            phrase.IsInterrogative = isInterrogative;

            return phrase;
        }

        /// <summary>
        /// Toutenのみのアクセント句を直前のAccentPhraseのPauseMoraに統合する。
        /// </summary>
        private static void MergePauseMoras(List<AccentPhrase> phrases)
        {
            for (int i = phrases.Count - 1; i >= 0; i--)
            {
                var phrase = phrases[i];

                // Toutenのみのアクセント句を検出: Morasが空でPauseMoraが設定されている
                if (phrase.Moras.Count == 0 && phrase.PauseMora != null)
                {
                    if (i > 0)
                    {
                        // 直前のアクセント句にPauseMoraを設定
                        phrases[i - 1].PauseMora = phrase.PauseMora;
                    }
                    phrases.RemoveAt(i);
                }
            }
        }
    }
}
