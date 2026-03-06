using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DotNetG2P.Models;

namespace DotNetG2P.NJD
{
    /// <summary>
    /// アクセント結合型の種類。
    /// jpreprocess の AccentType 列挙に準拠。
    /// </summary>
    internal enum AccentRuleType
    {
        /// <summary>ルールなし</summary>
        None,
        /// <summary>F1: 前部のアクセントを保持</summary>
        F1,
        /// <summary>F2: 前部が平板型の場合のみ加算</summary>
        F2,
        /// <summary>F3: 前部が起伏型の場合のみ加算</summary>
        F3,
        /// <summary>F4: 常に加算</summary>
        F4,
        /// <summary>F5: 平板型（0）にする</summary>
        F5,
        /// <summary>C1: 前部モーラ数 + 後部アクセント位置</summary>
        C1,
        /// <summary>C2: 前部モーラ数 + 1</summary>
        C2,
        /// <summary>C3: 前部モーラ数</summary>
        C3,
        /// <summary>C4: 平板型（0）にする</summary>
        C4,
        /// <summary>C5: 前部のアクセントを保持</summary>
        C5,
        /// <summary>P1: 前部が平板なら0、起伏なら前部モーラ数+後部アクセント</summary>
        P1,
        /// <summary>P2: P1と同じ</summary>
        P2,
        /// <summary>P6: 平板型（0）にする</summary>
        P6,
        /// <summary>P14: 前部が起伏型の場合のみ前部モーラ数+後部アクセント</summary>
        P14,
    }

    /// <summary>
    /// 単一のアクセント結合ルール。結合タイプと加算値を保持する。
    /// jpreprocess の ChainRule 構造体に準拠。
    /// </summary>
    internal sealed class AccentChainRule
    {
        /// <summary>アクセント結合タイプ</summary>
        public AccentRuleType AccentType { get; }

        /// <summary>加算値（F2, F3, F4で使用）</summary>
        public int AddType { get; }

        public AccentChainRule(AccentRuleType accentType, int addType)
        {
            AccentType = accentType;
            AddType = addType;
        }
    }

    /// <summary>
    /// 品詞別アクセント結合ルールの集合。
    /// ChainRule文字列（例: "動詞%F1/形容詞%F2@-1"）をパースして品詞別に格納する。
    /// jpreprocess の ChainRules 構造体に準拠。
    /// </summary>
    internal sealed class ChainRules
    {
        // キャッシュ: 同じChainRule文字列に対するChainRulesインスタンスを再利用する
        private static readonly ConcurrentDictionary<string, ChainRules> Cache = new ConcurrentDictionary<string, ChainRules>();

        /// <summary>デフォルトルール（品詞指定なし）</summary>
        public AccentChainRule? Default { get; private set; }

        /// <summary>動詞用ルール</summary>
        public AccentChainRule? Doushi { get; private set; }

        /// <summary>助詞用ルール</summary>
        public AccentChainRule? Joshi { get; private set; }

        /// <summary>形容詞用ルール</summary>
        public AccentChainRule? Keiyoushi { get; private set; }

        /// <summary>名詞用ルール</summary>
        public AccentChainRule? Meishi { get; private set; }

        /// <summary>
        /// ChainRule文字列からChainRulesを取得する。
        /// 同一文字列に対してはキャッシュからインスタンスを返す。
        /// </summary>
        /// <param name="ruleStr">ChainRule文字列（例: "C3", "動詞%F1/形容詞%F2@-1", "*"）</param>
        public static ChainRules GetOrCreate(string ruleStr)
        {
            if (ruleStr == null || ruleStr == "*")
                return Empty;

            return Cache.GetOrAdd(ruleStr, s => new ChainRules(s));
        }

        /// <summary>空のChainRules（ルールなし）</summary>
        private static readonly ChainRules Empty = new ChainRules();

        /// <summary>空のChainRulesを作成する（内部用）。</summary>
        private ChainRules() { }

        /// <summary>
        /// ChainRule文字列からChainRulesを構築する（内部用）。
        /// 外部からは <see cref="GetOrCreate"/> を使用すること。
        /// </summary>
        private ChainRules(string ruleStr)
        {
            var parts = ruleStr.Split('/');
            foreach (var part in parts)
            {
                PushRule(part);
            }
        }

        /// <summary>
        /// 前ノードの品詞に対応するルールを取得する。
        /// 品詞固有ルールが見つからない場合はデフォルトルールを返す。
        /// </summary>
        public AccentChainRule? GetRule(POS pos)
        {
            AccentChainRule? rule = null;

            switch (pos.Type)
            {
                case POSType.Doushi:
                    rule = Doushi;
                    break;
                case POSType.Joshi:
                    rule = Joshi;
                    break;
                case POSType.Keiyoushi:
                    rule = Keiyoushi;
                    break;
                case POSType.Meishi:
                    rule = Meishi;
                    break;
            }

            return rule ?? Default;
        }

        /// <summary>
        /// 個別ルール文字列をパースして格納する。
        /// パターン: (品詞%)?アクセントタイプ(@加算値)?
        /// 例: "C3", "動詞%F1", "形容詞%F2@-1"
        /// </summary>
        private void PushRule(string rule)
        {
            if (string.IsNullOrEmpty(rule))
                return;

            int pos = 0;

            // 1. 品詞接頭辞の検出（"動詞%", "名詞%", "形容詞%", "助詞%"）
            string? posStr = null;
            int percentIdx = rule.IndexOf('%');
            if (percentIdx > 0)
            {
                posStr = rule.Substring(0, percentIdx);
                pos = percentIdx + 1;
            }

            // 2. アクセントタイプのパース（C1-C5, F1-F5, P1, P2, P6, P14）
            var accentType = AccentRuleType.None;
            if (pos < rule.Length)
            {
                char first = rule[pos];
                if (first == 'C' || first == 'F' || first == 'P')
                {
                    // @の位置、またはルール末尾までをアクセントタイプ文字列とする
                    int atIdx = rule.IndexOf('@', pos);
                    int end = atIdx >= 0 ? atIdx : rule.Length;
                    string accentStr = rule.Substring(pos, end - pos);
                    accentType = ParseAccentType(accentStr);
                    pos = end;
                }
            }

            // アクセントタイプも品詞も検出できなかった場合は無効
            if (accentType == AccentRuleType.None && posStr == null)
                return;

            // 3. 加算値のパース（@の後の数値）
            int addType = 0;
            if (pos < rule.Length && rule[pos] == '@')
            {
                pos++;
                int.TryParse(rule.AsSpan(pos), out addType);
            }

            var chainRule = new AccentChainRule(accentType, addType);

            // 品詞による振り分け
            if (posStr != null)
            {
                switch (posStr)
                {
                    case "動詞":
                        Doushi = chainRule;
                        break;
                    case "助詞":
                        Joshi = chainRule;
                        break;
                    case "形容詞":
                        Keiyoushi = chainRule;
                        break;
                    case "名詞":
                        Meishi = chainRule;
                        break;
                }
            }
            else
            {
                Default = chainRule;
            }
        }

        /// <summary>
        /// アクセント結合タイプ文字列をenumに変換する。
        /// </summary>
        private static AccentRuleType ParseAccentType(string s)
        {
            switch (s)
            {
                case "F1": return AccentRuleType.F1;
                case "F2": return AccentRuleType.F2;
                case "F3": return AccentRuleType.F3;
                case "F4": return AccentRuleType.F4;
                case "F5": return AccentRuleType.F5;
                case "C1": return AccentRuleType.C1;
                case "C2": return AccentRuleType.C2;
                case "C3": return AccentRuleType.C3;
                case "C4": return AccentRuleType.C4;
                case "C5": return AccentRuleType.C5;
                case "P1": return AccentRuleType.P1;
                case "P2": return AccentRuleType.P2;
                case "P6": return AccentRuleType.P6;
                case "P14": return AccentRuleType.P14;
                default: return AccentRuleType.None;
            }
        }
    }

    /// <summary>
    /// NJD処理: アクセント結合型処理。
    /// アクセント句結合時のアクセント核位置を計算する。
    /// SetAccentPhrase の後に実行される。
    /// jpreprocess の open_jtalk/accent_type.rs に準拠。
    /// </summary>
    public static class SetAccentType
    {
        // 数詞定数（数字読み結合計算で使用）
        private const string ICHI = "一";
        private const string NI = "二";
        private const string SAN = "三";
        private const string YON = "四";
        private const string GO = "五";
        private const string ROKU = "六";
        private const string NANA = "七";
        private const string HACHI = "八";
        private const string KYUU = "九";
        private const string JYUU = "十";
        private const string HYAKU = "百";
        private const string SEN = "千";
        private const string MAN = "万";
        private const string OKU = "億";
        private const string CHOU = "兆";
        private const string NAN = "何";
        private const string IKU = "幾";

        /// <summary>
        /// NjdNodeリストのアクセント型を更新する。
        /// 各ノードのChainFlag（アクセント句結合フラグ）とChainRule（結合ルール）に基づき、
        /// 先頭ノードのアクセント核位置を再計算する。
        /// </summary>
        /// <param name="nodes">NjdNodeリスト</param>
        public static void Process(List<NjdNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return;

            int topNodeIndex = 0;
            int moraSize = 0;

            for (int i = 0; i < nodes.Count; i++)
            {
                var current = nodes[i];
                var prev = i > 0 ? nodes[i - 1] : null;
                var topNode = nodes[topNodeIndex];
                var next = (i + 1 < nodes.Count) ? nodes[i + 1] : null;

                int? topNodeNewAcc = null;
                int? prevNewAcc = null;
                int? currentNewAcc = null;

                if (i == 0 || current.ChainFlag != true)
                {
                    // アクセント句の先頭ノード
                    topNodeIndex = i;
                    moraSize = 0;

                    // 「十」の後に数詞が続く場合、「十」を平板型にする
                    if (current.Surface == JYUU && next != null && IsKazu(next))
                    {
                        currentNewAcc = 0;
                    }
                }
                else if (prev != null)
                {
                    // 結合中のノード: アクセント計算
                    int rawAcc = CalcTopNodeAcc(current, prev, topNode, moraSize);
                    // アクセント核位置を0〜アクセント句全体のモーラ数にクランプ
                    int totalMoraCount = moraSize + current.MoraCount;
                    topNodeNewAcc = Math.Max(0, Math.Min(totalMoraCount, rawAcc));

                    // 数詞同士の結合: 前ノードのアクセントを再計算
                    if (IsKazu(prev) && IsKazu(current))
                    {
                        prevNewAcc = CalcDigitAcc(prev, current, next);
                    }
                }

                moraSize += current.MoraCount;

                // 計算結果の適用
                if (topNodeNewAcc.HasValue)
                {
                    nodes[topNodeIndex].AccentType = topNodeNewAcc.Value;
                }
                if (prevNewAcc.HasValue)
                {
                    nodes[i - 1].AccentType = prevNewAcc.Value;
                }
                if (currentNewAcc.HasValue)
                {
                    nodes[i].AccentType = currentNewAcc.Value;
                }
            }
        }

        /// <summary>
        /// アクセント句先頭ノードのアクセント核位置を計算する。
        /// ChainRuleに基づいて、前ノードの品詞に応じた結合タイプを適用する。
        /// </summary>
        private static int CalcTopNodeAcc(NjdNode current, NjdNode prev, NjdNode topNode, int moraSize)
        {
            int nodeAcc = current.AccentType;
            int topNodeAcc = topNode.AccentType;

            // ChainRuleをパースし、前ノードの品詞に対応するルールを取得（キャッシュ利用）
            var rules = ChainRules.GetOrCreate(current.ChainRule);
            var rule = rules.GetRule(prev.Details.PartOfSpeech);

            if (rule == null)
                return topNodeAcc;

            // 加算値の計算: moraSize + addType
            int addResult = moraSize + rule.AddType;
            if (addResult < 0) addResult = 0;

            switch (rule.AccentType)
            {
                case AccentRuleType.F1:
                    // 前部のアクセントを保持
                    return topNodeAcc;

                case AccentRuleType.F2:
                    // 前部が平板型の場合のみ加算
                    if (topNodeAcc == 0)
                        return addResult;
                    return topNodeAcc;

                case AccentRuleType.F3:
                    // 前部が起伏型の場合のみ加算
                    if (topNodeAcc != 0)
                        return addResult;
                    return topNodeAcc;

                case AccentRuleType.F4:
                    // 常に加算
                    return addResult;

                case AccentRuleType.F5:
                    // 平板型（0）にする
                    return 0;

                case AccentRuleType.C1:
                    // 前部モーラ数 + 後部アクセント位置
                    return moraSize + nodeAcc;

                case AccentRuleType.C2:
                    // 前部モーラ数 + 1
                    return moraSize + 1;

                case AccentRuleType.C3:
                    // 前部モーラ数
                    return moraSize;

                case AccentRuleType.C4:
                    // 平板型（0）
                    return 0;

                case AccentRuleType.C5:
                    // 前部のアクセントを保持
                    return topNodeAcc;

                case AccentRuleType.P1:
                    // 前部が平板なら0、起伏なら前部モーラ数+後部アクセント
                    if (topNodeAcc == 0)
                        return 0;
                    return moraSize + nodeAcc;

                case AccentRuleType.P2:
                    // P1と同じ
                    if (topNodeAcc == 0)
                        return 0;
                    return moraSize + nodeAcc;

                case AccentRuleType.P6:
                    // 平板型（0）
                    return 0;

                case AccentRuleType.P14:
                    // 前部が起伏型の場合のみ
                    if (topNodeAcc != 0)
                        return moraSize + nodeAcc;
                    return topNodeAcc;

                default:
                    // ルールなし: 前部のアクセントを保持
                    return topNodeAcc;
            }
        }

        /// <summary>
        /// 数詞同士の結合時のアクセント核位置を計算する。
        /// 数字の桁（十、百、千、万、億、兆）に応じた特殊ルール。
        /// </summary>
        /// <returns>新しいアクセント位置。null の場合は変更なし。</returns>
        private static int? CalcDigitAcc(NjdNode prev, NjdNode current, NjdNode? next)
        {
            string prevStr = prev.Surface;
            string currentStr = current.Surface;
            string? nextStr = next?.Surface;

            // 十の位
            if (currentStr == JYUU)
            {
                // 五、六、八 + 十 + 一〜九 → 平板型
                if ((prevStr == GO || prevStr == ROKU || prevStr == HACHI) &&
                    nextStr != null &&
                    (nextStr == ICHI || nextStr == NI || nextStr == SAN || nextStr == YON ||
                     nextStr == GO || nextStr == ROKU || nextStr == NANA || nextStr == HACHI || nextStr == KYUU))
                {
                    return 0;
                }
                // それ以外の X + 十 → 1
                return 1;
            }

            // 百の位
            if (currentStr == HYAKU)
            {
                if (prevStr == NANA)
                    return 2;
                if (prevStr == SAN || prevStr == YON || prevStr == KYUU || prevStr == NAN)
                    return 1;
                return prev.MoraCount + current.MoraCount;
            }

            // 千の位
            if (currentStr == SEN)
            {
                return prev.MoraCount + 1;
            }

            // 万の位
            if (currentStr == MAN)
            {
                return prev.MoraCount + 1;
            }

            // 億の位
            if (currentStr == OKU)
            {
                if (prevStr == ICHI || prevStr == ROKU || prevStr == NANA || prevStr == HACHI || prevStr == IKU)
                    return 2;
                return 1;
            }

            // 兆の位
            if (currentStr == CHOU)
            {
                if (prevStr == ROKU || prevStr == NANA)
                    return 2;
                return 1;
            }

            return null;
        }

        /// <summary>
        /// ノードが数詞（名詞-数）かどうかを判定する。
        /// </summary>
        private static bool IsKazu(NjdNode node)
        {
            return node.Details != null && node.Details.PartOfSpeech != null && node.Details.PartOfSpeech.IsMeishiSuu;
        }
    }
}
