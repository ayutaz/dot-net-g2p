using System.Collections.Generic;
using DotNetG2P.Models;

namespace DotNetG2P.JPCommon
{
    /// <summary>
    /// NjdNodeリストからJPUtterance階層構造を構築する。
    /// jpreprocess の Utterance::from(&amp;[NJDNode]) に準拠。
    /// </summary>
    public static class JPCommonBuilder
    {
        /// <summary>
        /// NjdNodeリストからJPUtterance階層を構築する。
        /// </summary>
        /// <param name="nodes">NJD処理済みのノードリスト</param>
        /// <returns>構築されたJPUtterance</returns>
        public static JPUtterance Build(IReadOnlyList<NjdNode> nodes)
        {
            var utterance = new JPUtterance();
            if (nodes == null || nodes.Count == 0)
                return utterance;

            var breathGroups = new List<JPBreathGroup>(4);
            // 現在の呼気グループに溜めるアクセント句リスト
            var accentPhrases = new List<JPAccentPhrase>(4);

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var pron = node.Pronunciation;

                // Questionノード: 直前のアクセント句にIsInterrogativeを設定
                if (pron != null && pron.IsQuestion)
                {
                    if (accentPhrases.Count > 0)
                    {
                        accentPhrases[accentPhrases.Count - 1].IsInterrogative = true;
                    }
                }

                // Touten/Questionノード: 呼気グループ境界
                if (pron != null && (pron.IsTouten || pron.IsQuestion))
                {
                    if (accentPhrases.Count > 0)
                    {
                        breathGroups.Add(CreateBreathGroup(accentPhrases));
                    }
                    accentPhrases = new List<JPAccentPhrase>(4);
                    continue;
                }

                // ChainFlag == true: 前のアクセント句に結合
                if (node.ChainFlag == true)
                {
                    if (accentPhrases.Count > 0)
                    {
                        var lastAP = accentPhrases[accentPhrases.Count - 1];
                        AddNodeToAccentPhrase(lastAP, node);
                    }
                    else
                    {
                        // 前のアクセント句がない場合は新規作成
                        accentPhrases.Add(CreateAccentPhrase(node));
                    }
                }
                else
                {
                    // 新しいアクセント句を開始
                    accentPhrases.Add(CreateAccentPhrase(node));
                }
            }

            // 残りのアクセント句を最後の呼気グループとして追加
            if (accentPhrases.Count > 0)
            {
                breathGroups.Add(CreateBreathGroup(accentPhrases));
            }

            // 発話にBreathGroupを追加しインデックス・親参照を設定
            for (int bgIdx = 0; bgIdx < breathGroups.Count; bgIdx++)
            {
                var bg = breathGroups[bgIdx];
                bg.IndexInUtterance = bgIdx;
                bg.ParentUtterance = utterance;
                utterance.BreathGroups.Add(bg);
            }

            // 音素の前後リンクを構築
            LinkPhonemes(utterance);

            return utterance;
        }

        /// <summary>
        /// NjdNodeから新しいJPAccentPhraseを作成する。
        /// </summary>
        private static JPAccentPhrase CreateAccentPhrase(NjdNode node)
        {
            var ap = new JPAccentPhrase();
            ap.AccentType = node.AccentType;
            var word = CreateWord(node);
            word.ParentAccentPhrase = ap;
            word.IndexInAccentPhrase = 0;
            ap.Words.Add(word);

            // モーラインデックスを設定
            SetMoraIndices(ap);

            return ap;
        }

        /// <summary>
        /// 既存のJPAccentPhraseにNjdNodeを追加する。
        /// </summary>
        private static void AddNodeToAccentPhrase(JPAccentPhrase ap, NjdNode node)
        {
            var word = CreateWord(node);
            word.ParentAccentPhrase = ap;
            word.IndexInAccentPhrase = ap.Words.Count;
            ap.Words.Add(word);

            // モーラインデックスを再計算
            SetMoraIndices(ap);
        }

        /// <summary>
        /// NjdNodeからJPWordを作成する。
        /// </summary>
        private static JPWord CreateWord(NjdNode node)
        {
            var word = new JPWord();

            // POS/CType/CForm IDを設定（WordAttrの変換テーブルに委譲）
            word.PosId = WordAttr.PosToId(node.PartOfSpeech);
            word.CTypeId = WordAttr.CTypeToId(node.ConjugationType);
            word.CFormId = WordAttr.CFormToId(node.ConjugationForm);

            // Pronunciationの各Moraからモーラを構築
            if (node.Pronunciation != null)
            {
                foreach (var mora in node.Pronunciation.Moras)
                {
                    var jpMora = CreateJPMora(mora);
                    jpMora.ParentWord = word;
                    word.Moras.Add(jpMora);
                }
            }

            return word;
        }

        /// <summary>
        /// Models.MoraからJPMoraを作成する。
        /// </summary>
        private static JPMora CreateJPMora(Mora mora)
        {
            var jpMora = new JPMora();

            // 特殊モーラの処理
            if (mora.Kind == MoraKind.Xtsu)
            {
                // 促音: "cl" のみ
                jpMora.Phonemes.Add(new JPPhoneme("cl") { ParentMora = jpMora, IndexInMora = 0 });
                return jpMora;
            }

            if (mora.Kind == MoraKind.N)
            {
                // 撥音: "N" のみ
                jpMora.Phonemes.Add(new JPPhoneme("N") { ParentMora = jpMora, IndexInMora = 0 });
                return jpMora;
            }

            if (mora.Kind == MoraKind.Long)
            {
                // 長音: 特殊処理（前のモーラの母音を繰り返す）
                // ここでは仮に "-" を設定し、後でリンク構築時に解決する
                jpMora.Phonemes.Add(new JPPhoneme("-") { ParentMora = jpMora, IndexInMora = 0 });
                return jpMora;
            }

            if (mora.Kind == MoraKind.Touten || mora.Kind == MoraKind.Question)
            {
                // ポーズ/疑問符: 音素なし
                return jpMora;
            }

            // 通常モーラ: 子音 + 母音
            int idx = 0;
            if (mora.Consonant.HasValue)
            {
                string consonantStr = mora.Consonant.Value.ToSymbol();
                jpMora.Phonemes.Add(new JPPhoneme(consonantStr) { ParentMora = jpMora, IndexInMora = idx });
                idx++;
            }

            if (mora.Vowel.HasValue)
            {
                string vowelStr = mora.Vowel.Value.ToSymbol();
                jpMora.Phonemes.Add(new JPPhoneme(vowelStr) { ParentMora = jpMora, IndexInMora = idx });
            }

            return jpMora;
        }

        /// <summary>
        /// JPBreathGroupを作成し、アクセント句リストからインデックスと親参照を設定する。
        /// </summary>
        private static JPBreathGroup CreateBreathGroup(List<JPAccentPhrase> accentPhrases)
        {
            var bg = new JPBreathGroup();
            for (int apIdx = 0; apIdx < accentPhrases.Count; apIdx++)
            {
                var ap = accentPhrases[apIdx];
                ap.IndexInBreathGroup = apIdx;
                ap.ParentBreathGroup = bg;
                bg.AccentPhrases.Add(ap);
            }
            return bg;
        }

        /// <summary>
        /// アクセント句内の全モーラにインデックスを設定する。
        /// </summary>
        private static void SetMoraIndices(JPAccentPhrase ap)
        {
            int moraIdx = 0;
            foreach (var word in ap.Words)
            {
                foreach (var mora in word.Moras)
                {
                    mora.IndexInAccentPhrase = moraIdx;
                    moraIdx++;
                }
            }
        }

        /// <summary>
        /// 発話内の全音素に対して前後リンク (Prev/Next) を構築する。
        /// </summary>
        private static void LinkPhonemes(JPUtterance utterance)
        {
            // 全音素をフラットリストに展開
            var allPhonemes = new List<JPPhoneme>(utterance.MoraCount * 2);
            foreach (var bg in utterance.BreathGroups)
            {
                foreach (var ap in bg.AccentPhrases)
                {
                    foreach (var word in ap.Words)
                    {
                        foreach (var mora in word.Moras)
                        {
                            foreach (var phoneme in mora.Phonemes)
                            {
                                allPhonemes.Add(phoneme);
                            }
                        }
                    }
                }
            }

            // 前後リンクを設定
            for (int i = 0; i < allPhonemes.Count; i++)
            {
                if (i > 0)
                    allPhonemes[i].Prev = allPhonemes[i - 1];
                if (i < allPhonemes.Count - 1)
                    allPhonemes[i].Next = allPhonemes[i + 1];
            }
        }

    }
}
