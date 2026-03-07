using System.Collections.Generic;
using DotNetG2P.Models;

namespace DotNetG2P.NJD
{
    /// <summary>
    /// NJD処理: アクセント句結合判定。
    /// 隣接するNjdNode間の品詞パターンに基づく18ルールで結合/非結合を決定する。
    /// jpreprocess の open_jtalk/accent_phrase.rs に準拠。
    /// </summary>
    public static class SetAccentPhrase
    {
        /// <summary>
        /// NjdNodeリストに対してアクセント句の結合判定を行い、ChainFlagを設定する。
        /// </summary>
        public static void Process(List<NjdNode> nodes)
        {
            if (nodes == null || nodes.Count < 2)
                return;

            for (int i = 1; i < nodes.Count; i++)
            {
                var node = nodes[i];
                // ChainFlagが既に辞書から設定されている場合(非null)はスキップ。
                // jpreprocess では chain_flag が None のときのみルールを適用する。
                if (node.ChainFlag.HasValue)
                    continue;

                var prev = nodes[i - 1];
                node.ChainFlag = DetermineChainFlag(prev, node);
            }
        }

        /// <summary>
        /// 2つの隣接ノード間の結合フラグを18ルールに基づいて判定する。
        /// ルール番号はOpenJTalkの定義に準拠（番号が大きいほど優先度が高い）。
        /// </summary>
        private static bool DetermineChainFlag(NjdNode prev, NjdNode curr)
        {
            var prevPos = prev.Details.PartOfSpeech;
            var currPos = curr.Details.PartOfSpeech;

            // --- Rule 18: 接尾辞は前にくっつける ---
            // 形容詞-接尾、動詞-接尾、名詞-接尾 は前のノードに結合
            if (IsSetsubi(currPos))
                return true;

            // --- Rule 17: 名詞の後の「名」は別のアクセント句に ---
            if (prevPos.IsMeishi && IsMeishiKoyuuMei(currPos))
                return false;

            // --- Rule 16: 「姓」の後の名詞は別のアクセント句に ---
            if (IsMeishiKoyuuSei(prevPos) && currPos.IsMeishi)
                return false;

            // --- Rule 15: 接頭詞は単独のアクセント句に ---
            if (currPos.Type == POSType.Settoushi)
                return false;

            // --- Rule 14: 記号は単独のアクセント句に ---
            if (prevPos.IsKigou || currPos.IsKigou)
                return false;

            // --- Rule 12: 動詞-非自立は、動詞-連用形 or 名詞-サ変接続 に接続する場合に前にくっつける ---
            // ※ Rule 13（名詞+動詞→非結合）の例外として先にチェックする必要がある
            if (IsDoushiHijiritsu(currPos))
            {
                if (prevPos.IsDoushi && prev.IsRenyou)
                    return true;
                if (IsMeishiSahenSetsuzoku(prevPos))
                    return true;
            }

            // --- Rule 13: 名詞の後に動詞/形容詞/形容動詞語幹がきたら別のアクセント句に ---
            if (prevPos.IsMeishi && currPos.IsDoushi)
                return false;
            if (prevPos.IsMeishi && currPos.IsKeiyoushi)
                return false;
            if (prevPos.IsMeishi && IsMeishiKeiyoudoushiGokan(currPos))
                return false;

            // --- Rule 11: 形容詞-非自立は特定パターンで前にくっつける ---
            // 動詞-連用形 + 形容詞-非自立
            if (prevPos.IsDoushi && IsKeiyoushiHijiritsu(currPos) && prev.IsRenyou)
                return true;
            // 形容詞-連用形 + 形容詞-非自立
            if (prevPos.IsKeiyoushi && IsKeiyoushiHijiritsu(currPos) && prev.IsRenyou)
                return true;
            // 助詞-接続助詞「て」「で」 + 形容詞-非自立
            if (IsJoshiSetsuzokuJoshi(prevPos) && IsKeiyoushiHijiritsu(currPos)
                && (prev.Surface == "て" || prev.Surface == "で"))
                return true;

            // --- Rule 10: 接尾の後の名詞は別のアクセント句に ---
            if (IsSetsubi(prevPos) && currPos.IsMeishi)
                return false;

            // --- Rule 08/09: 助詞・助動詞のルール ---
            // Rule 08: 付属語同士はくっつける
            if (IsFuzokugo(prevPos) && IsFuzokugo(currPos))
                return true;
            // Rule 09: 付属語の後の自立語は別のアクセント句に
            if (IsFuzokugo(prevPos))
                return false;
            // Rule 08: 自立語の後の付属語は前にくっつける
            if (IsFuzokugo(currPos))
                return true;

            // --- Rule 07: 名詞-副詞可能は単独のアクセント句に ---
            if (IsMeishiFukushiKanou(prevPos) || IsMeishiFukushiKanou(currPos))
                return false;

            // --- Rule 06: 副詞・接続詞・連体詞は単独のアクセント句に ---
            if (IsIndependentAdverbial(prevPos) || IsIndependentAdverbial(currPos))
                return false;

            // --- Rule 05: 動詞の後に形容詞or名詞がきたら別のアクセント句に ---
            if (prevPos.IsDoushi && (currPos.IsKeiyoushi || currPos.IsMeishi))
                return false;

            // --- Rule 04: 名詞-形容動詞語幹の後に名詞がきたら別のアクセント句に ---
            if (IsMeishiKeiyoudoushiGokan(prevPos) && currPos.IsMeishi)
                return false;

            // --- Rule 03: 形容詞の後に名詞がきたら別のアクセント句に ---
            if (prevPos.IsKeiyoushi && currPos.IsMeishi)
                return false;

            // --- Rule 02: 名詞の連続はくっつける ---
            if (prevPos.IsMeishi && currPos.IsMeishi)
                return true;

            // --- Rule 01: デフォルトはくっつける ---
            return true;
        }

        // ===== 品詞判定ヘルパー =====

        /// <summary>接尾辞（形容詞-接尾、動詞-接尾、名詞-接尾）かどうか</summary>
        private static bool IsSetsubi(POS pos)
        {
            // 形容詞-接尾
            if (pos.IsKeiyoushi && pos.SubCategory1 == "接尾")
                return true;
            // 動詞-接尾
            if (pos.IsDoushi && pos.SubCategory1 == "接尾")
                return true;
            // 名詞-接尾
            if (pos.IsMeishiSetsubi)
                return true;
            return false;
        }

        /// <summary>名詞-固有名詞-人名-名 かどうか</summary>
        private static bool IsMeishiKoyuuMei(POS pos)
        {
            return pos.IsMeishiKoyuu && pos.SubCategory2 == "人名" && pos.SubCategory3 == "名";
        }

        /// <summary>名詞-固有名詞-人名-姓 かどうか</summary>
        private static bool IsMeishiKoyuuSei(POS pos)
        {
            return pos.IsMeishiKoyuu && pos.SubCategory2 == "人名" && pos.SubCategory3 == "姓";
        }

        /// <summary>名詞-形容動詞語幹 かどうか</summary>
        private static bool IsMeishiKeiyoudoushiGokan(POS pos)
        {
            return pos.IsMeishi && pos.SubCategory1 == "形容動詞語幹";
        }

        /// <summary>動詞-非自立 かどうか</summary>
        private static bool IsDoushiHijiritsu(POS pos)
        {
            return pos.IsDoushi && pos.SubCategory1 == "非自立";
        }

        /// <summary>形容詞-非自立 かどうか</summary>
        private static bool IsKeiyoushiHijiritsu(POS pos)
        {
            return pos.IsKeiyoushi && pos.SubCategory1 == "非自立";
        }

        /// <summary>助詞-接続助詞 かどうか</summary>
        private static bool IsJoshiSetsuzokuJoshi(POS pos)
        {
            return pos.IsJoshi && pos.SubCategory1 == "接続助詞";
        }

        /// <summary>付属語（助詞 or 助動詞）かどうか</summary>
        private static bool IsFuzokugo(POS pos)
        {
            return pos.IsJoshi || pos.IsJodoushi;
        }

        /// <summary>名詞-副詞可能 かどうか</summary>
        private static bool IsMeishiFukushiKanou(POS pos)
        {
            return pos.IsMeishi && pos.SubCategory1 == "副詞可能";
        }

        /// <summary>副詞・接続詞・連体詞 かどうか</summary>
        private static bool IsIndependentAdverbial(POS pos)
        {
            return pos.Type == POSType.Fukushi
                || pos.Type == POSType.Setsuzokushi
                || pos.Type == POSType.Rentaishi;
        }

        /// <summary>
        /// 名詞-サ変接続 かどうか。Rule 12 の追加条件。
        /// jpreprocess では動詞-非自立が「動詞-連用形 or 名詞-サ変接続」に接続する場合に結合する。
        /// 名詞-サ変接続の場合は活用形チェック不要（名詞は連用形を持たないため）。
        /// </summary>
        private static bool IsMeishiSahenSetsuzoku(POS pos)
        {
            return pos.IsMeishi && pos.SubCategory1 == "サ変接続";
        }
    }
}
