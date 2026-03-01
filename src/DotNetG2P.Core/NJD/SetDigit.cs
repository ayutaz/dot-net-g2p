using System;
using System.Collections.Generic;
using DotNetG2P.Models;

namespace DotNetG2P.NJD
{
    /// <summary>
    /// 数字の発音補正処理。
    /// 助数詞との組み合わせによる読み変化、日付特殊読み等を処理する。
    /// jpreprocess の open_jtalk/digit/mod.rs の njd_set_digit に準拠。
    ///
    /// 処理フェーズ:
    /// 1. 小数点処理（ピリオドを「テン」に変換、前後の数字の読みを調整）
    /// 2. 助数詞との読み変化（class1: 数字側、class2: 助数詞側の音便変化）
    /// 3. 位取りの読み変化（numeral: 百・千等の前の数字の音便変化）
    /// 4. 和語助数詞・特殊読み（class3: ヒト・フタ読み、others: 日付・人数特殊読み）
    /// 5. 複合日付パターン（十四日、二十日、二十四日等の特殊処理）
    /// </summary>
    public static class SetDigit
    {
        // 表層形定数
        private const string Zero1 = "\u3007"; // 〇
        private const string Zero2 = "\uFF10"; // ０
        private const string One = "一";
        private const string Two = "二";
        private const string Four = "四";
        private const string Five = "五";
        private const string Six = "六";
        private const string TenKanji = "十";
        private const string Gatsu = "月";
        private const string Nichi = "日";
        private const string Nichikan = "日間";

        // テン（小数点）ノードのCSV
        private const string TenFeature = "．,名詞,接尾,助数詞,*,*,*,．,テン,テン,0/2,*,-1";

        // ツイタチ（一日）ノードのCSV
        private const string TsuitachiFeature = "一日,名詞,副詞可能,*,*,*,*,一日,ツイタチ,ツイタチ,4/4,*";

        // 複合日付パターンのCSV
        private const string JuyokkaFeature = "十四日,名詞,副詞可能,*,*,*,*,十四日,ジュウヨッカ,ジューヨッカ,1/5,*";
        private const string JuyokkakanFeature = "十四日間,名詞,副詞可能,*,*,*,*,十四日間,ジュウヨッカカン,ジューヨッカカン,5/7,*";
        private const string NijuFeature = "二十,名詞,副詞可能,*,*,*,*,二十,ニジュウ,ニジュー,1/3,*";
        private const string YokkaFeature = "四日,名詞,副詞可能,*,*,*,*,四日,ヨッカ,ヨッカ,0/3,*,0";
        private const string YokkakanFeature = "四日間,名詞,副詞可能,*,*,*,*,四日間,ヨッカカン,ヨッカカン,3/5,*,0";
        private const string HatsukaFeature = "二十日,名詞,副詞可能,*,*,*,*,二十日,ハツカ,ハツカ,0/3,*";
        private const string HatsukakanFeature = "二十日間,名詞,副詞可能,*,*,*,*,二十日間,ハツカカン,ハツカカン,3/5,*";

        /// <summary>
        /// ピリオド（全角、中黒）かどうかを判定する。
        /// </summary>
        private static bool IsPeriod(string s)
        {
            return s == "\uFF0E" || s == "\u30FB"; // ．, ・
        }

        /// <summary>
        /// 品詞が数（名詞-数）かどうかを判定する。
        /// jpreprocess の is_kazu() に相当。
        /// </summary>
        private static bool IsKazu(POS pos)
        {
            return pos != null && pos.IsMeishiSuu;
        }

        /// <summary>
        /// 品詞が名詞-副詞可能かどうかを判定する。
        /// </summary>
        private static bool IsFukushiKanou(POS pos)
        {
            return pos != null && pos.Type == POSType.Meishi && pos.SubCategory1 == "副詞可能";
        }

        /// <summary>
        /// 品詞が名詞-接尾-助数詞かどうかを判定する。
        /// </summary>
        private static bool IsJosuushi(POS pos)
        {
            return pos != null && pos.Type == POSType.Meishi && pos.SubCategory1 == "接尾" && pos.SubCategory2 == "助数詞";
        }

        /// <summary>
        /// 品詞が記号かどうかを判定する。
        /// </summary>
        private static bool IsKigou(POS pos)
        {
            return pos != null && pos.IsKigou;
        }

        /// <summary>
        /// ノードの読み（カタカナ）を取得する。
        /// </summary>
        private static string GetReading(NjdNode node)
        {
            return node.Details?.Reading ?? "*";
        }

        /// <summary>
        /// ノードを無音化（リセット）する。
        /// jpreprocess の node.reset() に相当。
        /// </summary>
        private static void ResetNode(NjdNode node)
        {
            node.Surface = "";
            node.Pronunciation = new Pronunciation();
        }

        /// <summary>
        /// CSV文字列からNjdNodeを構築する。
        /// DigitSequenceProcessor.CreateNodeFromCsv を利用。
        /// </summary>
        private static NjdNode CreateNodeFromCsv(string csv)
        {
            return DigitSequenceProcessor.CreateNodeFromCsv(csv);
        }

        /// <summary>
        /// ノードの最初のモーラを濁音化する。
        /// jpreprocess の mora.convert_to_voiced_sound() に相当。
        /// </summary>
        private static void ConvertFirstMoraToVoiced(NjdNode node)
        {
            if (node.Pronunciation == null || node.Pronunciation.Moras.Count == 0)
                return;

            var firstMora = node.Pronunciation.Moras[0];
            var voicedKind = ToVoicedMoraKind(firstMora.Kind);
            if (voicedKind != firstMora.Kind)
            {
                node.Pronunciation.Moras[0] = Pronunciation.CreateMora(voicedKind);
            }
        }

        /// <summary>
        /// ノードの最初のモーラを半濁音化する。
        /// jpreprocess の mora.convert_to_semivoiced_sound() に相当。
        /// </summary>
        private static void ConvertFirstMoraToSemiVoiced(NjdNode node)
        {
            if (node.Pronunciation == null || node.Pronunciation.Moras.Count == 0)
                return;

            var firstMora = node.Pronunciation.Moras[0];
            var semiVoicedKind = ToSemiVoicedMoraKind(firstMora.Kind);
            if (semiVoicedKind != firstMora.Kind)
            {
                node.Pronunciation.Moras[0] = Pronunciation.CreateMora(semiVoicedKind);
            }
        }

        /// <summary>
        /// MoraKindを濁音化する。
        /// ハ行→バ行、カ行→ガ行、サ行→ザ行、タ行→ダ行 等。
        /// </summary>
        private static MoraKind ToVoicedMoraKind(MoraKind kind)
        {
            switch (kind)
            {
                // ハ行 → バ行
                case MoraKind.Ha: return MoraKind.Ba;
                case MoraKind.Hi: return MoraKind.Bi;
                case MoraKind.Fu: return MoraKind.Bu;
                case MoraKind.He: return MoraKind.Be;
                case MoraKind.Ho: return MoraKind.Bo;
                case MoraKind.Hya: return MoraKind.Bya;
                case MoraKind.Hyu: return MoraKind.Byu;
                case MoraKind.Hyo: return MoraKind.Byo;
                case MoraKind.Hye: return MoraKind.Bye;
                // カ行 → ガ行
                case MoraKind.Ka: return MoraKind.Ga;
                case MoraKind.Ki: return MoraKind.Gi;
                case MoraKind.Ku: return MoraKind.Gu;
                case MoraKind.Ke: return MoraKind.Ge;
                case MoraKind.Ko: return MoraKind.Go;
                case MoraKind.Kya: return MoraKind.Gya;
                case MoraKind.Kyu: return MoraKind.Gyu;
                case MoraKind.Kyo: return MoraKind.Gyo;
                case MoraKind.Kye: return MoraKind.Gye;
                // サ行 → ザ行
                case MoraKind.Sa: return MoraKind.Za;
                case MoraKind.Shi: return MoraKind.Ji;
                case MoraKind.Su: return MoraKind.Zu;
                case MoraKind.Se: return MoraKind.Ze;
                case MoraKind.So: return MoraKind.Zo;
                case MoraKind.Sha: return MoraKind.Ja;
                case MoraKind.Shu: return MoraKind.Ju;
                case MoraKind.Sho: return MoraKind.Jo;
                case MoraKind.She: return MoraKind.Je;
                // タ行 → ダ行
                case MoraKind.Ta: return MoraKind.Da;
                case MoraKind.Chi: return MoraKind.Di;
                case MoraKind.Tsu: return MoraKind.Du;
                case MoraKind.Te: return MoraKind.De;
                case MoraKind.To: return MoraKind.Do;
                default: return kind;
            }
        }

        /// <summary>
        /// MoraKindを半濁音化する。
        /// ハ行 → パ行。
        /// </summary>
        private static MoraKind ToSemiVoicedMoraKind(MoraKind kind)
        {
            switch (kind)
            {
                case MoraKind.Ha: return MoraKind.Pa;
                case MoraKind.Hi: return MoraKind.Pi;
                case MoraKind.Fu: return MoraKind.Pu;
                case MoraKind.He: return MoraKind.Pe;
                case MoraKind.Ho: return MoraKind.Po;
                case MoraKind.Hya: return MoraKind.Pya;
                case MoraKind.Hyu: return MoraKind.Pyu;
                case MoraKind.Hyo: return MoraKind.Pyo;
                case MoraKind.Hye: return MoraKind.Pye;
                default: return kind;
            }
        }

        /// <summary>
        /// NjdNodeリストに対して数字の発音補正処理を行う。
        /// jpreprocess の njd_set_digit() に準拠した5フェーズ処理。
        /// </summary>
        public static void Process(List<NjdNode> nodes)
        {
            // フェーズ1: 小数点処理
            ProcessDecimalPoint(nodes);

            // フェーズ2: 助数詞との読み変化（class1 + class2）
            ProcessNumerativeConversion(nodes);

            // フェーズ3: 位取りの読み変化（numeral）
            ProcessNumeralConversion(nodes);

            // フェーズ4: 和語助数詞・特殊読み（class3 + others）
            ProcessSpecialReadings(nodes);

            // フェーズ5: 複合日付パターン（十四日、二十日等）
            ProcessCompoundDatePatterns(nodes);

            // 無音ノードの除去
            RemoveSilentNodes(nodes);
        }

        /// <summary>
        /// フェーズ1: 小数点処理。
        /// 数字の間のピリオド（「．」「・」）を「テン」に変換し、前後の数字の読みを調整する。
        /// jpreprocess の njd_set_digit 第1ブロックに対応。
        /// </summary>
        private static void ProcessDecimalPoint(List<NjdNode> nodes)
        {
            // スキップ状態管理（小数点以下の名詞をスキップ）
            // Disabled: 通常
            // IfMeishi: 次が名詞ならスキップ開始
            // Skipping: 名詞をスキップ中
            int skipState = 0; // 0=Disabled, 1=IfMeishi, 2=Skipping

            for (int i = 1; i < nodes.Count - 1; i++)
            {
                var prev = nodes[i - 1];
                var node = nodes[i];
                var next = nodes[i + 1];

                var nodePOS = node.Details?.PartOfSpeech;

                // スキップ状態処理
                if (skipState == 1)
                {
                    // IfMeishi: 次の状態に移行
                    skipState = 2;
                    continue;
                }
                else if (skipState == 2)
                {
                    if (nodePOS != null && nodePOS.IsMeishi)
                    {
                        // 名詞をスキップ中
                        continue;
                    }
                    else
                    {
                        // 名詞以外でスキップ終了
                        skipState = 0;
                        continue;
                    }
                }

                // ピリオド判定
                if (!string.IsNullOrEmpty(node.Surface)
                    && !string.IsNullOrEmpty(prev.Surface)
                    && IsPeriod(node.Surface)
                    && IsKazu(prev.Details?.PartOfSpeech)
                    && IsKazu(next.Details?.PartOfSpeech))
                {
                    // ピリオドを「テン」ノードに置換
                    var tenNode = CreateNodeFromCsv(TenFeature);
                    nodes[i] = tenNode;
                    tenNode.ChainFlag = true;

                    // 前の数字の読みを調整
                    switch (prev.Surface)
                    {
                        case Zero1:
                        case Zero2:
                            // 0 → "レー"
                            prev.Pronunciation = Pronunciation.FromKatakana("レー", 1);
                            break;
                        case Two:
                            // 二 → "ニー"
                            prev.Pronunciation = Pronunciation.FromKatakana("ニー", 1);
                            break;
                        case Five:
                            // 五 → "ゴー"
                            prev.Pronunciation = Pronunciation.FromKatakana("ゴー", 1);
                            break;
                        case Six:
                            // 六 → "ロク"
                            prev.Pronunciation = Pronunciation.FromKatakana("ロク", 1);
                            break;
                    }

                    skipState = 1; // 次が名詞ならスキップ開始
                }
            }
        }

        /// <summary>
        /// フェーズ2: 助数詞との読み変化。
        /// 数字 + 助数詞（副詞可能 or 接尾-助数詞）の並びで、数字と助数詞の発音を変更する。
        /// jpreprocess の njd_set_digit 第2ブロック（class1 + class2）に対応。
        /// </summary>
        private static void ProcessNumerativeConversion(List<NjdNode> nodes)
        {
            for (int i = 1; i < nodes.Count; i++)
            {
                var prev = nodes[i - 1];
                var node = nodes[i];

                if (!IsKazu(prev.Details?.PartOfSpeech))
                    continue;

                var nodePOS = node.Details?.PartOfSpeech;
                if (nodePOS == null)
                    continue;
                if (!IsFukushiKanou(nodePOS) && !IsJosuushi(nodePOS))
                    continue;

                // class1: 数字側の発音変化
                var class1Pron = DigitLut.FindConvSet(
                    DigitLut.Class1ConvTable,
                    node.Surface,
                    prev.Surface);
                if (class1Pron != null)
                {
                    prev.Pronunciation = class1Pron;
                }

                // class2: 助数詞側の音便変化（濁音化/半濁音化）
                if (DigitLut.TryFindConvSet(
                    DigitLut.Class2ConvTable,
                    node.Surface,
                    prev.Surface,
                    out DigitLut.DigitType class2Type))
                {
                    switch (class2Type)
                    {
                        case DigitLut.DigitType.Voiced:
                            ConvertFirstMoraToVoiced(node);
                            break;
                        case DigitLut.DigitType.SemiVoiced:
                            ConvertFirstMoraToSemiVoiced(node);
                            break;
                    }
                }

                prev.ChainFlag = false;
                node.ChainFlag = true;
            }
        }

        /// <summary>
        /// フェーズ3: 位取りの読み変化。
        /// 数字 + 位取り（百、千等）の並びで、チェインフラグと発音を調整する。
        /// jpreprocess の njd_set_digit 第3ブロック（numeral）に対応。
        /// </summary>
        private static void ProcessNumeralConversion(List<NjdNode> nodes)
        {
            for (int i = 1; i < nodes.Count; i++)
            {
                var prev = nodes[i - 1];
                var node = nodes[i];

                if (!IsKazu(prev.Details?.PartOfSpeech))
                    continue;

                // 数字+位取りのチェインフラグ設定
                if (IsKazu(node.Details?.PartOfSpeech) && !string.IsNullOrEmpty(node.Surface))
                {
                    if (DigitLut.NumeralDigits.Contains(prev.Surface)
                        && DigitLut.NumeralPlaces.Contains(node.Surface))
                    {
                        // 基数 + 位取り → 位取りをチェイン
                        prev.ChainFlag = false;
                        node.ChainFlag = true;
                    }
                    else if (DigitLut.NumeralPlaces.Contains(prev.Surface)
                        && DigitLut.NumeralDigits.Contains(node.Surface))
                    {
                        // 位取り + 基数 → 基数のチェインを解除
                        node.ChainFlag = false;
                    }
                }

                // numeral: 数字側の発音変化（百の前の六→ロッ等）
                var numDigitPron = DigitLut.FindConvSet(
                    DigitLut.DigitConvTable,
                    node.Surface,
                    prev.Surface);
                if (numDigitPron != null)
                {
                    prev.Pronunciation = numDigitPron;
                }

                // numeral: 位取り側の音便変化（三百→サンビャク等）
                if (DigitLut.TryFindConvSet(
                    DigitLut.NumerativeConvTable,
                    node.Surface,
                    prev.Surface,
                    out DigitLut.DigitType numType))
                {
                    switch (numType)
                    {
                        case DigitLut.DigitType.Voiced:
                            ConvertFirstMoraToVoiced(node);
                            break;
                        case DigitLut.DigitType.SemiVoiced:
                            ConvertFirstMoraToSemiVoiced(node);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// フェーズ4: 和語助数詞・特殊読み。
        /// class3（ヒト・フタ読み）と others（日付・人数特殊読み）を処理する。
        /// jpreprocess の njd_set_digit 第4ブロックに対応。
        /// </summary>
        private static void ProcessSpecialReadings(List<NjdNode> nodes)
        {
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                var node = nodes[i];
                var next = nodes[i + 1];

                if (string.IsNullOrEmpty(next.Surface))
                    continue;

                if (!IsKazu(node.Details?.PartOfSpeech))
                    continue;

                // 前のノードがある場合、それが数字なら処理をスキップ
                // （数字列の先頭のみ処理対象）
                if (i > 0)
                {
                    var prevPOS = nodes[i - 1].Details?.PartOfSpeech;
                    if (prevPOS != null && !IsKigou(prevPOS) && IsKazu(prevPOS))
                        continue;
                }

                var nextPOS = next.Details?.PartOfSpeech;
                if (nextPOS == null)
                    continue;
                if (!IsFukushiKanou(nextPOS) && !IsJosuushi(nextPOS))
                    continue;

                // class3: 和語助数詞読み変換（一→ヒト、二→フタ）
                var nextReading = GetReading(next);
                var class3Pron = DigitLut.FindConvMap(
                    DigitLut.Class3ConvTable,
                    next.Surface,
                    nextReading != "*" ? nextReading : "",
                    node.Surface);
                if (class3Pron != null)
                {
                    // 読みと発音を更新
                    node.Details = new WordDetails(
                        node.Details.PartOfSpeech,
                        node.Details.ConjugationType,
                        node.Details.ConjugationForm,
                        node.Details.OriginalForm,
                        class3Pron.ToKatakana(),
                        class3Pron
                    );
                    node.Pronunciation = class3Pron;
                }

                // others: 人数・日付の特殊読み
                var specialCsv = DigitLut.FindConvSet(
                    DigitLut.SpecialConvTable,
                    next.Surface,
                    node.Surface);
                if (specialCsv != null)
                {
                    // 「月」の後の「一」+「日」→ 通常の「イチニチ」ではなく「ツイタチ」に変換
                    if (i > 0
                        && nodes[i - 1].Surface.Contains(Gatsu)
                        && node.Surface == One
                        && next.Surface == Nichi)
                    {
                        var tsuitachiNode = CreateNodeFromCsv(TsuitachiFeature);
                        nodes[i] = tsuitachiNode;
                    }
                    else
                    {
                        var specialNode = CreateNodeFromCsv(specialCsv);
                        nodes[i] = specialNode;
                    }

                    ResetNode(next);
                }
            }
        }

        /// <summary>
        /// フェーズ5: 複合日付パターン。
        /// 「十四日」「二十日」「二十四日」等の複合パターンを処理する。
        /// jpreprocess の njd_set_digit 第5ブロックに対応。
        /// </summary>
        private static void ProcessCompoundDatePatterns(List<NjdNode> nodes)
        {
            if (nodes.Count < 3)
                return;

            for (int i = 0; i < nodes.Count - 2; i++)
            {
                var node = nodes[i];
                var nx1 = nodes[i + 1];
                var nx2 = nodes[i + 2];

                // 前のノードが数字でないことを確認（先頭 or 記号 or 非数字）
                if (i > 0)
                {
                    var prevPOS = nodes[i - 1].Details?.PartOfSpeech;
                    if (prevPOS != null && IsKazu(prevPOS))
                        continue;
                }

                var nodeS = node.Surface;
                var nx1S = nx1.Surface;
                var nx2S = nx2.Surface;
                string nx3S = (i + 3 < nodes.Count) ? nodes[i + 3].Surface : null;

                // パターンマッチング
                string newNodeFeature = null;
                string newNx1Feature = null;
                int unsetPattern = 0; // 0=None, 1=Nx1Nx2, 2=Nx2Nx3

                if (nodeS == TenKanji && nx1S == Four && nx2S == Nichi)
                {
                    // 十 + 四 + 日 → 十四日（ジューヨッカ）
                    newNodeFeature = JuyokkaFeature;
                    unsetPattern = 1;
                }
                else if (nodeS == TenKanji && nx1S == Four && nx2S == Nichikan)
                {
                    // 十 + 四 + 日間 → 十四日間
                    newNodeFeature = JuyokkakanFeature;
                    unsetPattern = 1;
                }
                else if (nodeS == Two && nx1S == TenKanji && nx2S == Nichi)
                {
                    // 二 + 十 + 日 → 二十日（ハツカ）
                    newNodeFeature = HatsukaFeature;
                    unsetPattern = 1;
                }
                else if (nodeS == Two && nx1S == TenKanji && nx2S == Nichikan)
                {
                    // 二 + 十 + 日間 → 二十日間（ハツカカン）
                    newNodeFeature = HatsukakanFeature;
                    unsetPattern = 1;
                }
                else if (nodeS == Two && nx1S == TenKanji && nx2S == Four && nx3S == Nichi)
                {
                    // 二 + 十 + 四 + 日 → 二十（ニジュー）+ 四日（ヨッカ）
                    newNodeFeature = NijuFeature;
                    newNx1Feature = YokkaFeature;
                    unsetPattern = 2;
                }
                else if (nodeS == Two && nx1S == TenKanji && nx2S == Four && nx3S == Nichikan)
                {
                    // 二 + 十 + 四 + 日間 → 二十（ニジュー）+ 四日間（ヨッカカン）
                    newNodeFeature = NijuFeature;
                    newNx1Feature = YokkakanFeature;
                    unsetPattern = 2;
                }

                if (newNodeFeature != null)
                {
                    nodes[i] = CreateNodeFromCsv(newNodeFeature);
                }
                if (newNx1Feature != null)
                {
                    nodes[i + 1] = CreateNodeFromCsv(newNx1Feature);
                }

                switch (unsetPattern)
                {
                    case 1:
                        // Nx1とNx2をリセット
                        ResetNode(nodes[i + 1]);
                        ResetNode(nodes[i + 2]);
                        break;
                    case 2:
                        // Nx2とNx3をリセット
                        ResetNode(nodes[i + 2]);
                        if (i + 3 < nodes.Count)
                        {
                            ResetNode(nodes[i + 3]);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 無音ノード（Surface が空で発音モーラ数が0）を除去する。
        /// jpreprocess の njd.remove_silent_node() に相当。
        /// </summary>
        private static void RemoveSilentNodes(List<NjdNode> nodes)
        {
            nodes.RemoveAll(n =>
                string.IsNullOrEmpty(n.Surface)
                && (n.Pronunciation == null || n.Pronunciation.Moras.Count == 0));
        }
    }
}
