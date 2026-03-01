using System.Collections.Generic;

namespace DotNetG2P.Models
{
    /// <summary>
    /// NJD（Natural Language Processing for Japanese text to speech Documents）ノード。
    /// OpenJTalkのNJD処理パイプラインで使用される中間表現。
    /// jpreprocess の NJDNode 構造体に準拠。
    /// </summary>
    public sealed class NjdNode
    {
        /// <summary>表層形</summary>
        public string Surface { get; set; }

        /// <summary>単語詳細情報</summary>
        public WordDetails Details { get; set; }

        /// <summary>発音情報</summary>
        public Pronunciation Pronunciation { get; set; }

        /// <summary>アクセント型番号（0=平板型、1以上=アクセント核位置）</summary>
        public int AccentType { get; set; }

        /// <summary>
        /// 前ノードとの結合フラグ（3値）。
        /// null=未設定、true=結合する、false=結合しない。
        /// jpreprocess の chain_flag: Option&lt;bool&gt; に準拠。
        /// </summary>
        public bool? ChainFlag { get; set; }

        /// <summary>アクセント結合ルール文字列（"C1", "F2@-1", "形容詞%F2/動詞%F5" 等）</summary>
        public string ChainRule { get; set; }

        /// <summary>読み（カタカナ）。NJD処理でのノード結合時に連結される。</summary>
        public string Reading { get; set; }

        /// <summary>モーラ数</summary>
        public int MoraCount => Pronunciation?.MoraCount ?? 0;

        // ====== 便利アクセサ: WordDetails からの委譲 ======

        /// <summary>品詞情報（Detailsがnullの場合は名詞を返す）</summary>
        public POS PartOfSpeech => Details?.PartOfSpeech ?? new POS(POSType.Meishi);

        /// <summary>活用型</summary>
        public string ConjugationType => Details?.ConjugationType ?? "*";

        /// <summary>活用形</summary>
        public string ConjugationForm => Details?.ConjugationForm ?? "*";

        /// <summary>原形</summary>
        public string OriginalForm => Details?.OriginalForm ?? "*";

        /// <summary>
        /// 連用形かどうかを判定する。
        /// jpreprocess の is_renyou() に準拠。
        /// </summary>
        public bool IsRenyou
        {
            get
            {
                var form = ConjugationForm;
                return form == "連用形"
                    || form == "連用タ接続"
                    || form == "連用テ接続"
                    || form == "連用デ接続"
                    || form == "連用ニ接続"
                    || form == "連用ゴザイ接続";
            }
        }

        /// <param name="surface">表層形（nullの場合は空文字列に正規化）</param>
        /// <param name="details">単語詳細情報（nullを許容: 記号等で詳細情報がない場合）</param>
        public NjdNode(string surface, WordDetails details)
        {
            Surface = surface ?? "";
            Details = details;
            Pronunciation = new Pronunciation();
            AccentType = 0;
            ChainFlag = null;
            ChainRule = "*";
            Reading = details?.Reading ?? "*";
        }

        /// <summary>
        /// 他のノードの内容を自ノードに統合する（アクセント句結合時に使用）。
        /// jpreprocess の transfer_from に準拠。
        /// 表層形・読み・モーラリストを連結し、統合元ノードは Reset される。
        /// </summary>
        /// <param name="other">統合元ノード（統合後にResetされる）</param>
        public void MergeFrom(NjdNode other)
        {
            if (other == null) return;

            // 表層形の連結
            Surface += other.Surface;

            // 読みの連結
            if (Reading == "*" || Reading == null)
            {
                Reading = other.Reading;
            }
            else if (other.Reading != null && other.Reading != "*")
            {
                Reading += other.Reading;
            }

            // モーラリストの連結（Pronunciation の transfer_from に相当）
            if (other.Pronunciation != null && other.Pronunciation.MoraCount > 0)
            {
                if (Pronunciation == null)
                {
                    Pronunciation = new Pronunciation();
                }
                Pronunciation.Moras.AddRange(other.Pronunciation.Moras);
            }

            // 統合元ノードをリセット
            other.Reset();
        }

        /// <summary>
        /// ノードを初期状態にリセットする。
        /// jpreprocess の reset に準拠。
        /// MergeFrom で統合された後の空化に使用。
        /// </summary>
        public void Reset()
        {
            Surface = "";
            Details = new WordDetails(
                new POS(POSType.Meishi),
                "*", "*", "*", "*", null
            );
            Pronunciation = new Pronunciation();
            AccentType = 0;
            ChainFlag = null;
            ChainRule = "*";
            Reading = "*";
        }

        /// <summary>
        /// ノードが空（Resetされた状態）かどうかを判定する。
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(Surface) && MoraCount == 0;

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

                // WordDetailsから発音情報を防御的コピー（元のWordDetailsのPronunciationが変更されないようにする）
                if (entry.Details?.Pronunciation != null && entry.Details.Pronunciation.MoraCount > 0)
                {
                    var orig = entry.Details.Pronunciation;
                    node.Pronunciation = new Pronunciation(
                        new List<Mora>(orig.Moras), orig.AccentPosition);
                }

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

        /// <summary>
        /// NjdNodeリストから空ノード（Reset済み）を除去する。
        /// MergeFrom 後のクリーンアップに使用。
        /// </summary>
        public static void RemoveEmpty(List<NjdNode> nodes)
        {
            nodes.RemoveAll(n => n.IsEmpty);
        }
    }
}
