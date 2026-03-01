using System;
using System.Collections.Generic;
using DotNetG2P.Models;

namespace DotNetG2P.NJD
{
    /// <summary>
    /// NJD処理: 無声音化処理。
    /// 母音 i/u の無声化条件6ルールを適用する。
    /// jpreprocess の open_jtalk/unvoiced_vowel.rs に準拠。
    ///
    /// ルール:
    ///   0. フィラーは無声化しない
    ///   1. 助動詞の「です」「ます」の「す」が無声化
    ///   2. 動詞・助動詞・助詞の「し」は無声化しやすい
    ///   3. 続けて無声化しない（2モーラ連続無声化の回避）
    ///   4. アクセント核で無声化しない
    ///   5. 無声子音に囲まれた i/u が無声化
    ///      例外: s->s, s->sh, f->f, f->h, f->hy, h->f, h->h, h->hy
    /// </summary>
    public static class SetUnvoicedVowel
    {
        /// <summary>
        /// モーラの状態を保持する内部構造体。
        /// 全ノードのモーラを平坦化して走査するために使用する。
        /// </summary>
        private class MoraState
        {
            /// <summary>モーラが属するノードのインデックス</summary>
            public int NodeIndex;

            /// <summary>ノード内でのモーラの位置インデックス</summary>
            public int MoraListIndex;

            /// <summary>品詞情報</summary>
            public POS Pos;

            /// <summary>
            /// 有声フラグ。
            /// null = 未確定、true = 有声（無声化しない）、false = 無声化する
            /// </summary>
            public bool? IsVoicedFlag;

            /// <summary>アクセント句内でのモーラインデックス（0始まり）</summary>
            public int MoraIndexInPhrase;

            /// <summary>アクセント核位置</summary>
            public int AccentType;

            /// <summary>モーラの子音</summary>
            public Consonant? Consonant;

            /// <summary>モーラの母音</summary>
            public Vowel? Vowel;

            /// <summary>モーラの種類</summary>
            public MoraKind Kind;
        }

        /// <summary>
        /// NjdNodeリスト全体に対して無声音化処理を適用する。
        /// </summary>
        /// <param name="nodes">処理対象のNjdNodeリスト</param>
        public static void Process(List<NjdNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return;

            // 全ノードのモーラを平坦化してMoraStateリストを構築
            var states = BuildMoraStates(nodes);
            if (states.Count == 0)
                return;

            // 前方参照を伴うウィンドウ走査で無声化判定を行う
            for (int i = 0; i < states.Count; i++)
            {
                var curr = states[i];
                var next = (i + 1 < states.Count) ? states[i + 1] : null;
                var nextNext = (i + 2 < states.Count) ? states[i + 2] : null;

                // ルール1: 「ます」「です」の「す」の先読み
                ApplyRule1_MasuDesu(curr, next, nextNext);

                // ルール2: 「し」の先読み
                ApplyRule2_Shi(curr, next, nextNext);

                // 未確定のモーラに対してルール0, 3, 4, 5を適用
                if (curr.IsVoicedFlag == null)
                {
                    // ルール0: フィラーは無声化しない
                    if (curr.Pos.IsFiller)
                    {
                        curr.IsVoicedFlag = true;
                    }
                    // ルール3: 次のモーラが既に無声化確定なら、連続無声化を避ける
                    else if (next != null && next.IsVoicedFlag == false)
                    {
                        curr.IsVoicedFlag = true;
                    }
                    // ルール4: アクセント核位置のモーラは無声化しない
                    else if (curr.AccentType == curr.MoraIndexInPhrase + 1)
                    {
                        curr.IsVoicedFlag = true;
                    }
                    else
                    {
                        // ルール5: 無声子音に囲まれた i/u の無声化
                        curr.IsVoicedFlag = ApplyUnvoiceRule(curr, next);
                    }
                }

                // 無声化が確定したモーラの次のモーラは連続無声化を避けるため有声を確定
                if (curr.IsVoicedFlag == false && next != null)
                {
                    if (next.IsVoicedFlag == null)
                        next.IsVoicedFlag = true;
                }
            }

            // 判定結果をノードのMoraリストに反映
            ApplyResults(nodes, states);
        }

        /// <summary>
        /// 全ノードのモーラを平坦化してMoraStateリストを構築する。
        /// </summary>
        private static List<MoraState> BuildMoraStates(List<NjdNode> nodes)
        {
            var states = new List<MoraState>();
            int moraIndexInPhrase = 0;
            int accentType = 0;

            for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                var node = nodes[nodeIndex];
                if (node.Pronunciation == null)
                    continue;

                // ChainFlagがfalseまたは未設定の場合、新しいアクセント句なのでリセット
                if (node.ChainFlag != true)
                {
                    moraIndexInPhrase = 0;
                    accentType = node.AccentType;
                }

                var moras = node.Pronunciation.Moras;
                for (int moraIdx = 0; moraIdx < moras.Count; moraIdx++)
                {
                    var mora = moras[moraIdx];
                    var state = new MoraState
                    {
                        NodeIndex = nodeIndex,
                        MoraListIndex = moraIdx,
                        Pos = node.Details?.PartOfSpeech ?? new POS(POSType.Unknown),
                        MoraIndexInPhrase = moraIndexInPhrase,
                        AccentType = accentType,
                        Consonant = mora.Consonant,
                        Vowel = mora.Vowel,
                        Kind = mora.Kind,
                    };

                    // 既に無声化されているモーラ（'マーカーで事前に無声化済み）はfalse確定
                    if (mora.Vowel.HasValue && mora.Vowel.Value.IsUnvoiced())
                    {
                        state.IsVoicedFlag = false;
                    }

                    states.Add(state);
                    moraIndexInPhrase++;
                }
            }

            return states;
        }

        /// <summary>
        /// ルール1: 「ます」「です」の「す」の無声化。
        /// 動詞・助動詞の語末で「マス」or「デス」パターンの「ス」が無声化する。
        /// ただし次の語頭が長音(ー)や疑問符(？)の場合は有声のまま。
        /// </summary>
        private static void ApplyRule1_MasuDesu(MoraState curr, MoraState next, MoraState nextNext)
        {
            if (next == null || nextNext == null)
                return;

            // 現在と次が同じノードで、次の次は別ノード（= 語末のスを判定）
            bool indexOk = curr.NodeIndex == next.NodeIndex
                        && next.NodeIndex != nextNext.NodeIndex;
            if (!indexOk)
                return;

            // 品詞チェック: 動詞、助動詞
            bool posOk = next.Pos.IsDoushi || next.Pos.IsJodoushi;
            if (!posOk)
                return;

            // モーラパターン: (マ|デ) + ス
            bool isMaSu = (curr.Kind == MoraKind.Ma || curr.Kind == MoraKind.De)
                       && next.Kind == MoraKind.Su;
            if (!isMaSu)
                return;

            // 次の次が長音かQuestionなら有声のまま
            bool nextNextIsLongOrQuestion = nextNext.Kind == MoraKind.Question
                                         || nextNext.Kind == MoraKind.Long;
            next.IsVoicedFlag = nextNextIsLongOrQuestion;
        }

        /// <summary>
        /// ルール2: 動詞・助動詞・助詞の語頭「シ」の無声化。
        /// 前ノードの末尾の次に位置する「シ」で、1モーラ語の場合に無声化判定を行う。
        /// </summary>
        private static void ApplyRule2_Shi(MoraState curr, MoraState next, MoraState nextNext)
        {
            if (next == null)
                return;

            // 次のモーラが未確定であること
            if (next.IsVoicedFlag != null)
                return;

            // 品詞チェック: 動詞、助動詞、助詞
            bool posOk = next.Pos.IsDoushi || next.Pos.IsJodoushi || next.Pos.IsJoshi;
            if (!posOk)
                return;

            // モーラチェック: 「シ」であり、前のノードと別ノード、かつ次の次とも別ノード（1モーラ語）
            bool moraOk = next.Kind == MoraKind.Shi
                       && curr.NodeIndex != next.NodeIndex
                       && (nextNext == null || nextNext.NodeIndex != next.NodeIndex);
            if (!moraOk)
                return;

            // ルール4: アクセント核位置なら有声のまま
            if (next.AccentType == next.MoraIndexInPhrase + 1)
            {
                next.IsVoicedFlag = true;
            }
            else
            {
                // ルール5: 無声子音に囲まれているかチェック
                next.IsVoicedFlag = ApplyUnvoiceRule(next, nextNext);
            }

            // 無声化が確定したら、前後を有声に固定（連続無声化回避）
            if (next.IsVoicedFlag == false)
            {
                if (curr.IsVoicedFlag == null)
                    curr.IsVoicedFlag = true;
                if (nextNext != null && nextNext.IsVoicedFlag == null)
                    nextNext.IsVoicedFlag = true;
            }
        }

        /// <summary>
        /// ルール5: 無声子音に囲まれた i/u の無声化判定。
        /// 無声子音: k, ky, s, sh, t, ty, ch, ts, h, f, hy, p, py
        /// 例外ペア（無声化しない）: s->s, s->sh, f->f, f->h, f->hy, h->f, h->h, h->hy
        /// </summary>
        /// <returns>true=有声のまま、false=無声化、null=対象外</returns>
        private static bool? ApplyUnvoiceRule(MoraState curr, MoraState next)
        {
            if (next == null)
            {
                // 語末: 有声のまま
                return true;
            }

            // 母音が i/u（有声・無声問わず）でなければ対象外
            if (!IsTargetVowel(curr.Vowel))
                return null;

            var currConsonant = curr.Consonant;
            var nextConsonant = next.Consonant;

            // 現在のモーラと次のモーラの両方が無声子音であるかチェック
            if (!currConsonant.HasValue || !IsUnvoicedConsonant(currConsonant.Value))
                return null;
            if (!nextConsonant.HasValue || !IsUnvoicedConsonant(nextConsonant.Value))
                return null;

            // 例外ペアのチェック（これらの組み合わせでは有声のまま）
            if (IsExceptionPair(currConsonant.Value, nextConsonant.Value))
                return true;

            // 無声子音に囲まれた i/u → 無声化
            return false;
        }

        /// <summary>
        /// 母音が無声化対象 (i/u) かどうかを判定する。
        /// </summary>
        private static bool IsTargetVowel(Vowel? vowel)
        {
            if (!vowel.HasValue)
                return false;

            return vowel.Value == Vowel.I || vowel.Value == Vowel.U
                || vowel.Value == Vowel.I_Unvoiced || vowel.Value == Vowel.U_Unvoiced;
        }

        /// <summary>
        /// 無声子音かどうかを判定する。
        /// 無声子音: K, Ky, S, Sh, T, Ty, Ch, Ts, H, F, Hy, P, Py
        /// </summary>
        private static bool IsUnvoicedConsonant(Consonant c)
        {
            switch (c)
            {
                case Consonant.K:
                case Consonant.Ky:
                case Consonant.S:
                case Consonant.Sh:
                case Consonant.T:
                case Consonant.Ty:
                case Consonant.Ch:
                case Consonant.Ts:
                case Consonant.H:
                case Consonant.F:
                case Consonant.Hy:
                case Consonant.P:
                case Consonant.Py:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 例外ペア（この組み合わせでは無声化しない）を判定する。
        /// s->s, s->sh, f->f, f->h, f->hy, h->f, h->h, h->hy
        /// </summary>
        private static bool IsExceptionPair(Consonant curr, Consonant next)
        {
            switch (curr)
            {
                case Consonant.S:
                    return next == Consonant.S || next == Consonant.Sh;
                case Consonant.F:
                    return next == Consonant.F || next == Consonant.H || next == Consonant.Hy;
                case Consonant.H:
                    return next == Consonant.F || next == Consonant.H || next == Consonant.Hy;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 判定結果をノードのMoraリストに反映する。
        /// 無声化フラグがfalseのモーラは、母音を無声版に置き換えた新しいMoraで差し替える。
        /// </summary>
        private static void ApplyResults(List<NjdNode> nodes, List<MoraState> states)
        {
            foreach (var state in states)
            {
                // 無声化する場合のみ処理
                if (state.IsVoicedFlag != false)
                    continue;

                // 母音がなければスキップ
                if (!state.Vowel.HasValue)
                    continue;

                var node = nodes[state.NodeIndex];
                if (node.Pronunciation == null)
                    continue;

                var moras = node.Pronunciation.Moras;
                if (state.MoraListIndex >= moras.Count)
                    continue;

                var oldMora = moras[state.MoraListIndex];

                // 既に無声化済みならスキップ
                if (oldMora.Vowel.HasValue && oldMora.Vowel.Value.IsUnvoiced())
                    continue;

                // 母音を無声版に変換した新しいMoraを作成して差し替え
                var unvoicedVowel = oldMora.Vowel.Value.ToUnvoiced();
                var newMora = new Mora(oldMora.Consonant, unvoicedVowel, oldMora.Kind);
                moras[state.MoraListIndex] = newMora;
            }
        }
    }
}
