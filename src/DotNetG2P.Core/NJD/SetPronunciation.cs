using System;
using System.Collections.Generic;
using DotNetG2P.Models;

namespace DotNetG2P.NJD
{
    /// <summary>
    /// NJD処理の第1段階: 発音設定。
    /// 各NjdNodeに対してカタカナ発音からMoraリストを構築する。
    /// jpreprocess の open_jtalk/pronunciation.rs の完全移植版。
    ///
    /// 処理の流れ:
    /// 1. 発音がまだ設定されていないノードに対し、表層形からモーラ解析を行う
    ///    - 記号やカナ変換不能文字はToutenセグメントとして分割
    ///    - 複数セグメントがある場合はノードを分割
    /// 2. 発音が空のノードを除去
    /// 3. 連続するカナフィラーノードを先頭ノードに統合
    /// 4. 再度発音が空のノードを除去
    /// 5. 助動詞「う」→長音変換、「です/ます」+「？」の発音修正
    /// </summary>
    public static class SetPronunciation
    {
        /// <summary>
        /// NjdNodeリストに対して発音設定処理を実行する。
        /// </summary>
        public static void Process(List<NjdNode> nodes)
        {
            // === 第1段階: 発音がないノードの処理 ===
            ProcessUnpronounced(nodes);

            // === 第2段階: 発音が空のノードを除去 ===
            RemoveSilentNodes(nodes);

            // === 第3段階: 連続するカナフィラーシーケンスを結合 ===
            ChainKanaFillerSequence(nodes);

            // === 第4段階: 再度発音が空のノードを除去 ===
            RemoveSilentNodes(nodes);

            // === 第5段階: 助動詞「う」→長音変換、「です/ます」+「？」修正 ===
            ProcessJodoushiAndQuestion(nodes);
        }

        // ========================================================================
        // 第1段階: 発音がないノードの処理
        // ========================================================================

        /// <summary>
        /// 発音が設定されていないノードに対し、表層形からモーラ解析を行う。
        /// jpreprocess pronunciation.rs の最初のブロックに相当。
        /// 複数セグメントに分割される場合はノードを分割して挿入する。
        /// </summary>
        private static void ProcessUnpronounced(List<NjdNode> nodes)
        {
            // 元のリストを退避し、新しいリストを構築
            var original = new List<NjdNode>(nodes);
            nodes.Clear();

            foreach (var node in original)
            {
                // 既に発音が設定されている場合はそのまま通す
                if (node.Pronunciation != null && node.Pronunciation.MoraCount > 0)
                {
                    nodes.Add(node);
                    continue;
                }

                // 表層形からモーラセグメントを取得
                var segments = Pronunciation.ParseMoraSegments(node.Surface);
                if (segments.Count == 0)
                {
                    // セグメントが取れない場合はスキップ（後でremove_silent_nodeで除去）
                    continue;
                }

                foreach (var (text, moras) in segments)
                {
                    // セグメントごとに新ノードを作成
                    var segNode = CloneNodeWithSurface(node, text);
                    var pron = new Pronunciation(moras, 0);

                    var moraSize = pron.MoraCount;
                    if (moraSize == 0)
                    {
                        // モーラ数0（記号のみ） → 品詞を記号に変換
                        if (pron.IsTouten)
                        {
                            ConvertToKigou(segNode);
                        }
                    }
                    else
                    {
                        // モーラがある → 品詞をフィラーに設定
                        segNode.Details = new WordDetails(
                            new POS(POSType.Filler),
                            segNode.ConjugationType,
                            segNode.ConjugationForm,
                            segNode.OriginalForm,
                            segNode.Reading,
                            null
                        );
                    }

                    if (pron.IsEmpty)
                    {
                        // 空の発音 → ノードをリセット（後で除去される）
                        segNode.Reset();
                    }
                    else
                    {
                        // 読みをカタカナ文字列から設定
                        segNode.Reading = pron.ToKatakana();
                        segNode.Pronunciation = pron;
                        nodes.Add(segNode);
                    }
                }
            }
        }

        // ========================================================================
        // 第3段階: 連続するカナフィラーシーケンスを結合
        // ========================================================================

        /// <summary>
        /// 連続するフィラー品詞のカナ変換可能ノードを先頭ノードに統合する。
        /// jpreprocess pronunciation.rs の "chain kana sequence" ブロックに相当。
        /// 例: [バリー(Filler)] [ペーン(Filler)] → [バリーペーン(Filler)]
        /// </summary>
        private static void ChainKanaFillerSequence(List<NjdNode> nodes)
        {
            int? headIndex = null;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.PartOfSpeech.IsFiller)
                {
                    if (Pronunciation.IsMoraConvertable(node.Surface))
                    {
                        if (headIndex.HasValue)
                        {
                            // 先頭ノードに統合
                            var head = nodes[headIndex.Value];
                            head.Surface += node.Surface;

                            if (head.Reading != null && head.Reading != "*" &&
                                node.Reading != null && node.Reading != "*")
                            {
                                head.Reading += node.Reading;
                            }

                            head.Pronunciation.TransferFrom(node.Pronunciation);

                            // 統合元ノードをリセット（後で除去される）
                            node.Reset();
                        }
                        else
                        {
                            headIndex = i;
                        }
                    }
                    else
                    {
                        headIndex = null;
                    }
                }
                else
                {
                    headIndex = null;
                }
            }
        }

        // ========================================================================
        // 第5段階: 助動詞「う」→長音変換、「です/ます」+「？」修正
        // ========================================================================

        /// <summary>
        /// 隣接ノード間のルール処理を行う。
        /// jpreprocess pronunciation.rs の最後のブロックに相当。
        ///
        /// 1. 動詞/助動詞の後の助動詞「ウ」を長音（ー）に変換
        /// 2. 助動詞「です」「ます」の直後に「？」がある場合の発音修正
        /// </summary>
        private static void ProcessJodoushiAndQuestion(List<NjdNode> nodes)
        {
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                var node = nodes[i];
                var next = nodes[i + 1];

                // ルール1: 動詞/助動詞 + 助動詞「ウ」→ 長音
                if (next.Pronunciation != null &&
                    next.Pronunciation.MoraMatches(MoraKind.U) &&
                    next.PartOfSpeech.IsJodoushi &&
                    (node.PartOfSpeech.IsDoushi || node.PartOfSpeech.IsJodoushi) &&
                    node.Pronunciation != null &&
                    node.Pronunciation.MoraCount > 0)
                {
                    // 「ウ」を長音（ー）に変換
                    next.Pronunciation = new Pronunciation(
                        new List<Mora> { Pronunciation.CreateMora(MoraKind.Long) },
                        0
                    );
                }

                // ルール2: 助動詞「です/ます」+ 「？」→ 発音修正
                if (node.PartOfSpeech.IsJodoushi && next.Surface == "\uFF1F") // ？
                {
                    if (node.Surface == "\u3067\u3059") // です
                    {
                        node.Pronunciation = new Pronunciation(
                            new List<Mora>
                            {
                                Pronunciation.CreateMora(MoraKind.De),
                                Pronunciation.CreateMora(MoraKind.Su)
                            },
                            1
                        );
                    }
                    else if (node.Surface == "\u307E\u3059") // ます
                    {
                        node.Pronunciation = new Pronunciation(
                            new List<Mora>
                            {
                                Pronunciation.CreateMora(MoraKind.Ma),
                                Pronunciation.CreateMora(MoraKind.Su)
                            },
                            1
                        );
                    }
                }
            }
        }

        // ========================================================================
        // ヘルパーメソッド
        // ========================================================================

        /// <summary>
        /// 発音が空のノードをリストから除去する。
        /// jpreprocess の remove_silent_node に相当。
        /// </summary>
        private static void RemoveSilentNodes(List<NjdNode> nodes)
        {
            nodes.RemoveAll(n => n.Pronunciation == null || n.Pronunciation.IsEmpty);
        }

        /// <summary>
        /// ノードの表層形を変更したクローンを作成する。
        /// セグメント分割時に使用。
        /// </summary>
        private static NjdNode CloneNodeWithSurface(NjdNode source, string newSurface)
        {
            return new NjdNode(newSurface, source.Details)
            {
                AccentType = source.AccentType,
                ChainFlag = source.ChainFlag,
                ChainRule = source.ChainRule,
                Reading = source.Reading
            };
        }

        /// <summary>
        /// ノードの品詞を記号に変換する。
        /// jpreprocess の convert_to_kigou に相当。
        /// </summary>
        private static void ConvertToKigou(NjdNode node)
        {
            var currentPos = node.PartOfSpeech;
            POS newPos;

            if (currentPos.IsKigou)
            {
                // 既に記号の場合はそのまま
                newPos = currentPos;
            }
            else if (currentPos.IsMeishi && currentPos.SubCategory1 == "\u6570") // "数"
            {
                newPos = new POS(POSType.Kigou, "\u6570"); // 記号-数
            }
            else if ((currentPos.Type == POSType.Fukushi && currentPos.SubCategory1 == "\u4E00\u822C") || // 副詞-一般
                     (currentPos.IsMeishi && currentPos.SubCategory1 == "\u4E00\u822C")) // 名詞-一般
            {
                newPos = new POS(POSType.Kigou, "\u4E00\u822C"); // 記号-一般
            }
            else
            {
                newPos = new POS(POSType.Kigou);
            }

            node.Details = new WordDetails(
                newPos,
                node.ConjugationType,
                node.ConjugationForm,
                node.OriginalForm,
                node.Reading,
                node.Details?.Pronunciation
            );
        }
    }
}
