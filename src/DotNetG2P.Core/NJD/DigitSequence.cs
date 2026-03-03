using System;
using System.Collections.Generic;
using DotNetG2P.Models;

namespace DotNetG2P.NJD
{
    /// <summary>
    /// 数字列処理。
    /// NjdNodeリストから連続する数字トークンを検出し、
    /// 数値読み（例: 123→ひゃくにじゅうさん）か
    /// 順序読み（例: 123→いちにーさん）かを判定してグループ化する。
    /// jpreprocess の digit_sequence モジュールに相当。
    /// </summary>
    public static class DigitSequenceProcessor
    {
        // ====== 数字正規化テーブル（NUMERAL_LIST1相当） ======
        // 全角数字・漢数字・ひらがな読み等を漢数字一文字に統一
        private static readonly Dictionary<string, string> DigitNormalize = new Dictionary<string, string>
        {
            { "○", "〇" },
            { "１", "一" }, { "２", "二" }, { "３", "三" }, { "４", "四" },
            { "５", "五" }, { "６", "六" }, { "７", "七" }, { "８", "八" }, { "９", "九" },
            { "一", "一" }, { "二", "二" }, { "三", "三" }, { "四", "四" },
            { "五", "五" }, { "六", "六" }, { "七", "七" }, { "八", "八" }, { "九", "九" },
            { "いち", "一" }, { "に", "二" }, { "さん", "三" }, { "よん", "四" },
            { "ご", "五" }, { "ろく", "六" }, { "なな", "七" }, { "はち", "八" }, { "きゅう", "九" },
            { "〇", "〇" }, { "０", "０" },
            { "壱", "一" }, { "弐", "二" }, { "貳", "二" }, { "ニ", "二" },
            { "参", "三" }, { "し", "四" }, { "しち", "七" }, { "く", "九" },
        };

        // ====== 位取り（十・百・千）テーブル（NUMERAL_LIST2相当） ======
        // CSV形式: "表層,品詞,品詞細分類1,*,*,*,*,原形,読み,発音,アクセント情報,チェインルール"
        private static readonly string[] NumeralList2 =
        {
            "", // 一の位（位取り不要）
            "十,名詞,数,*,*,*,*,十,ジュウ,ジュー,1/2,*",
            "百,名詞,数,*,*,*,*,百,ヒャク,ヒャク,2/2,*",
            "千,名詞,数,*,*,*,*,千,セン,セン,1/2,*",
        };

        // ====== 大数テーブル（NUMERAL_LIST3相当） ======
        private static readonly string[] NumeralList3 =
        {
            "", // 万未満（不要）
            "万,名詞,数,*,*,*,*,万,マン,マン,1/2,*",
            "億,名詞,数,*,*,*,*,億,オク,オク,1/2,*",
            "兆,名詞,数,*,*,*,*,兆,チョウ,チョー,1/2,C3",
            "京,名詞,数,*,*,*,*,京,ケイ,ケー,1/2,*",
            "垓,名詞,数,*,*,*,*,垓,ガイ,ガイ,1/2,*",
            "𥝱,名詞,数,*,*,*,*,𥝱,ジョ,ジョ,1/1,*",
            "穣,名詞,数,*,*,*,*,穣,ジョウ,ジョー,1/2,*",
            "溝,名詞,数,*,*,*,*,溝,コウ,コウ,1/2,*",
            "澗,名詞,数,*,*,*,*,澗,カン,カン,1/2,*",
            "正,名詞,数,*,*,*,*,正,セイ,セー,1/2,*",
            "載,名詞,数,*,*,*,*,載,サイ,サイ,1/2,*",
            "極,名詞,数,*,*,*,*,極,ゴク,ゴク,1/2,*",
            "恒河沙,名詞,数,*,*,*,*,恒河沙,ゴウガシャ,ゴウガシャ,1/4,*",
            "阿僧祇,名詞,数,*,*,*,*,阿僧祇,アソウギ,アソーギ,2/4,*",
            "那由他,名詞,数,*,*,*,*,那由他,ナユタ,ナユタ,1/3,*",
            "不可思議,名詞,数,*,*,*,*,不可思議,フカシギ,フカシギ,2/4,*",
            "無量大数,名詞,数,*,*,*,*,無量大数,ムリョウタイスウ,ムリョータイスー,6/7,*",
        };

        /// <summary>
        /// NjdNodeリストに対して数字列処理を行う。
        /// 連続する数字トークンを検出し、数値読みまたは順序読みに変換する。
        /// </summary>
        public static void Process(List<NjdNode> nodes)
        {
            // 数字の正規化
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.Surface != "*" && node.Details.PartOfSpeech.IsMeishiSuu)
                {
                    if (DigitNormalize.TryGetValue(node.Surface, out var replace))
                    {
                        node.Surface = replace;
                    }
                }
            }

            // 数字列の検出
            var sequences = DigitSequenceBuilder.FromNodes(nodes);

            // 各数字列を変換（オフセットを追跡）
            int offset = 0;
            for (int i = 0; i < sequences.Count; i++)
            {
                offset += sequences[i].Convert(nodes, offset);
            }

            // 無音ノードの除去（Pronunciationがnullの場合も安全に処理）
            nodes.RemoveAll(n => n.Surface == "" && n.MoraCount == 0 && (n.Pronunciation?.Moras?.Count ?? 0) == 0);
        }

        /// <summary>
        /// CSV形式の文字列からNjdNodeを構築する。
        /// "表層,品詞,品詞細分類1,*,*,*,*,原形,読み,発音,アクセント情報,チェインルール"
        /// </summary>
        internal static NjdNode CreateNodeFromCsv(string csv)
        {
            var parts = csv.Split(',');
            if (parts.Length < 10)
                throw new ArgumentException($"CSVフィールド数が不足しています: {csv}");

            var surface = parts[0];
            var pos = POS.FromFeatures(parts[1], parts[2], parts[3], parts[4]);
            var conjugationType = parts[5];
            var conjugationForm = parts[6];
            var originalForm = parts[7];
            var reading = parts[8];
            var pronunciation = parts[9];

            // アクセント情報
            var accentInfo = parts.Length > 10 ? parts[10] : "*";
            var chainRule = parts.Length > 11 ? parts[11] : "*";

            int accentPosition = 0;
            if (accentInfo != "*" && !string.IsNullOrEmpty(accentInfo))
            {
                int slashIndex = accentInfo.IndexOf('/');
                if (slashIndex > 0)
                {
                    int.TryParse(accentInfo.Substring(0, slashIndex), out accentPosition);
                }
            }

            Pronunciation? pron = null;
            if (!string.IsNullOrEmpty(pronunciation) && pronunciation != "*")
            {
                try
                {
                    pron = Pronunciation.FromKatakana(pronunciation, accentPosition);
                }
                catch (ArgumentException)
                {
                    // 解析できない場合はnull
                }
            }

            var details = new WordDetails(pos, conjugationType, conjugationForm, originalForm, reading, pron);
            var node = new NjdNode(surface, details)
            {
                AccentType = accentPosition,
                ChainRule = chainRule,
            };

            // WordDetailsから発音情報をコピー（NjdNode.FromTokensと同じパターン）
            if (details.Pronunciation != null && details.Pronunciation.MoraCount > 0)
            {
                node.Pronunciation = details.Pronunciation;
            }

            return node;
        }

        // ====== 数字列構造体 ======

        /// <summary>
        /// 検出された数字列を表す。
        /// ノードリスト内のstartからendまでの範囲と、各桁の数値を保持する。
        /// </summary>
        internal sealed class DigitSequenceInfo
        {
            /// <summary>ノードリスト中の開始インデックス</summary>
            public int Start { get; set; }

            /// <summary>ノードリスト中の終了インデックス</summary>
            public int End { get; set; }

            /// <summary>各桁の数値（0-9）のリスト</summary>
            public List<byte> Digits { get; }

            /// <summary>数値読みかどうか（null=未判定、true=数値読み、false=順序読み）</summary>
            public bool? IsNumericalReading { get; set; }

            public DigitSequenceInfo(int start, int end, List<byte> digits, bool? isNumericalReading)
            {
                Start = start;
                End = end;
                Digits = digits;
                IsNumericalReading = isNumericalReading;
            }

            /// <summary>
            /// 数値読みかどうかを推定する。
            /// </summary>
            public void EstimateNumericalReading(List<NjdNode> nodes)
            {
                if (IsNumericalReading == null)
                {
                    IsNumericalReading = DigitSequenceScore.Score(nodes, Start, End) >= 0;
                }
            }

            /// <summary>
            /// 数字列をノードリスト上で変換する。オフセットの変化量を返す。
            /// </summary>
            public int Convert(List<NjdNode> nodes, int offset)
            {
                Start += offset;
                End += offset;
                if (IsNumericalReading == true)
                {
                    return ConvertForNumericalReading(nodes);
                }
                else
                {
                    ConvertForNonNumericalReading(nodes);
                    return 0;
                }
            }

            /// <summary>
            /// 順序読み（数字を順に読み上げる）の変換。
            /// 例: 123 → イチ・ニー・サン
            /// </summary>
            private void ConvertForNonNumericalReading(List<NjdNode> nodes)
            {
                for (int i = 0; i < Digits.Count && Start + i < nodes.Count; i++)
                {
                    var node = nodes[Start + i];
                    var digit = Digits[i];

                    // 特定の数字の読み替え
                    switch (digit)
                    {
                        case 0:
                            // 0 → "ゼロ"（レー）
                            node.Pronunciation = Pronunciation.FromKatakana("ゼロ", 1);
                            break;
                        case 2:
                            // 2 → "ニー"（長音化）
                            node.Pronunciation = Pronunciation.FromKatakana("ニー", 1);
                            break;
                        case 5:
                            // 5 → "ゴー"（長音化）
                            node.Pronunciation = Pronunciation.FromKatakana("ゴー", 1);
                            break;
                    }

                    // チェインルールをクリア
                    node.ChainRule = "*";

                    // 2桁ずつのグルーピング
                    if (i % 2 == 0)
                    {
                        node.ChainFlag = false;
                        if (i != Digits.Count - 1)
                        {
                            // 最後の桁でなければアクセント位置を3に設定
                            node.Pronunciation = new Pronunciation(
                                node.Pronunciation.Moras,
                                3
                            );
                        }
                    }
                    else
                    {
                        node.ChainFlag = true;
                    }
                }
            }

            /// <summary>
            /// 数値読み（数として読み上げる）の変換。
            /// 例: 123 → ヒャクニジュウサン
            /// </summary>
            private int ConvertForNumericalReading(List<NjdNode> nodes)
            {
                // まずコンマを除去
                int commaOffset = 0;
                for (int idx = End; idx >= Start; idx--)
                {
                    if (idx < nodes.Count && nodes[idx].Surface == "，")
                    {
                        nodes.RemoveAt(idx);
                        commaOffset++;
                    }
                }

                // 桁数が大きすぎる場合はそのまま返す
                if (Digits.Count > NumeralList3.Length * 4)
                {
                    return -commaOffset;
                }

                // ブロック内に0以外の桁があるかどうか
                bool haveDigitInBlock = false;
                int insertOffset = 0;

                // 各桁を変換
                for (int i = 0; i < Digits.Count; i++)
                {
                    int nodesIndex = Start + i + insertOffset;
                    int revIndex = Digits.Count - i - 1;
                    byte digit = Digits[i];

                    // コンマ削除等でインデックスがずれた場合のガード
                    if (nodesIndex >= nodes.Count)
                        break;

                    if (digit == 0)
                    {
                        // 0の桁はリセット（無音化）
                        var node = nodes[nodesIndex];
                        node.Surface = "";
                        node.Pronunciation = new Pronunciation();
                    }
                    else
                    {
                        haveDigitInBlock = true;
                    }

                    if (revIndex % 4 == 0)
                    {
                        // 4桁区切りの境界（万・億・兆等）
                        if (haveDigitInBlock && revIndex > 0)
                        {
                            int listIndex = revIndex / 4;
                            if (listIndex < NumeralList3.Length && !string.IsNullOrEmpty(NumeralList3[listIndex]))
                            {
                                var largeNode = CreateNodeFromCsv(NumeralList3[listIndex]);
                                nodes.Insert(nodesIndex + 1, largeNode);
                                insertOffset++;
                            }
                        }
                        haveDigitInBlock = false;
                    }
                    else
                    {
                        // 十・百・千の位取り
                        int posInBlock = revIndex % 4;
                        if (posInBlock < NumeralList2.Length && !string.IsNullOrEmpty(NumeralList2[posInBlock]))
                        {
                            if (digit == 0)
                            {
                                // 0は何もしない（既にリセット済み）
                            }
                            else if (digit == 1)
                            {
                                // 1は位取りノードに置換（「一十」→「十」）
                                nodes[nodesIndex] = CreateNodeFromCsv(NumeralList2[posInBlock]);
                            }
                            else
                            {
                                // 2-9は位取りノードを挿入
                                var placeNode = CreateNodeFromCsv(NumeralList2[posInBlock]);
                                nodes.Insert(nodesIndex + 1, placeNode);
                                insertOffset++;
                            }
                        }
                    }
                }

                return insertOffset - commaOffset;
            }
        }
    }

    // ====== 数字列ビルダー ======

    /// <summary>
    /// NjdNodeリストから連続する数字トークンを検出し、DigitSequenceInfoリストを構築する。
    /// jpreprocess の digit_sequence/builder.rs に相当。
    /// </summary>
    internal static class DigitSequenceBuilder
    {
        /// <summary>
        /// 解析時の数字トークンの種類。
        /// </summary>
        private enum DigitToken
        {
            /// <summary>数字（0-9）</summary>
            Digit,
            /// <summary>コンマ区切り</summary>
            Comma,
        }

        /// <summary>
        /// 数字トークンの値を保持する構造体。
        /// </summary>
        private readonly struct ParsedDigit
        {
            public DigitToken Type { get; }
            public byte Value { get; }

            private ParsedDigit(DigitToken type, byte value = 0)
            {
                Type = type;
                Value = value;
            }

            public static ParsedDigit FromDigit(byte value) => new ParsedDigit(DigitToken.Digit, value);
            public static ParsedDigit FromComma() => new ParsedDigit(DigitToken.Comma);

            /// <summary>
            /// 表層形文字列から数字トークンを解析する。
            /// </summary>
            public static ParsedDigit? FromString(string surface)
            {
                switch (surface)
                {
                    case "一": return FromDigit(1);
                    case "二": return FromDigit(2);
                    case "三": return FromDigit(3);
                    case "四": return FromDigit(4);
                    case "五": return FromDigit(5);
                    case "六": return FromDigit(6);
                    case "七": return FromDigit(7);
                    case "八": return FromDigit(8);
                    case "九": return FromDigit(9);
                    case "〇":
                    case "０": return FromDigit(0);
                    case "，": return FromComma();
                    default: return null;
                }
            }
        }

        /// <summary>
        /// NjdNodeリストから数字列を検出・構築する。
        /// </summary>
        public static List<DigitSequenceProcessor.DigitSequenceInfo> FromNodes(List<NjdNode> nodes)
        {
            var result = new List<DigitSequenceProcessor.DigitSequenceInfo>(4);
            var digits = new List<ParsedDigit>(8);
            int start = 0;
            bool isInSeq = false;

            for (int i = 0; i < nodes.Count; i++)
            {
                // シーケンス中でない場合、前回の蓄積があれば確定
                if (!isInSeq && digits.Count > 0)
                {
                    TrimTrailingNonDigits(digits);
                    result.AddRange(FromParsedDigits(start, digits, nodes));
                    digits.Clear();
                }

                var parsed = ParsedDigit.FromString(nodes[i].Surface);
                if (parsed == null)
                {
                    isInSeq = false;
                    continue;
                }

                if (!isInSeq)
                {
                    if (parsed.Value.Type == DigitToken.Digit)
                    {
                        start = i;
                        isInSeq = true;
                    }
                    else
                    {
                        continue;
                    }
                }

                digits.Add(parsed.Value);
            }

            // 末尾の蓄積を処理
            if (digits.Count > 0)
            {
                TrimTrailingNonDigits(digits);
                result.AddRange(FromParsedDigits(start, digits, nodes));
            }

            // 各数字列の数値読み判定
            for (int i = 0; i < result.Count; i++)
            {
                result[i].EstimateNumericalReading(nodes);
            }

            return result;
        }

        /// <summary>
        /// 末尾の非数字トークン（コンマ）を除去する。
        /// </summary>
        private static void TrimTrailingNonDigits(List<ParsedDigit> digits)
        {
            while (digits.Count > 0 && digits[digits.Count - 1].Type != DigitToken.Digit)
            {
                digits.RemoveAt(digits.Count - 1);
            }
        }

        /// <summary>
        /// パースされた数字列からDigitSequenceInfoリストを生成する。
        /// コンマ区切りの正しさを検証し、正しい場合は1つの数値読みシーケンスとして、
        /// そうでない場合はコンマで分割して個別のシーケンスとして返す。
        /// </summary>
        private static List<DigitSequenceProcessor.DigitSequenceInfo> FromParsedDigits(
            int start, List<ParsedDigit> digits, List<NjdNode> nodes)
        {
            var result = new List<DigitSequenceProcessor.DigitSequenceInfo>(2);
            bool isZeroStart = CheckZeroStart(digits);

            if (!isZeroStart && CheckCommaSequence(digits))
            {
                // コンマ区切りが正しい → 数値読み
                var seq = CreateSequence(start, digits, true);
                if (seq != null)
                {
                    result.Add(seq);
                }
            }
            else
            {
                // コンマで分割して個別処理
                int chunkStart = start;
                var chunk = new List<ParsedDigit>(digits.Count);

                for (int i = 0; i < digits.Count; i++)
                {
                    if (digits[i].Type == DigitToken.Comma)
                    {
                        if (chunk.Count > 0)
                        {
                            var seq = CreateSequence(chunkStart, chunk, null);
                            if (seq != null)
                            {
                                result.Add(seq);
                            }
                        }
                        chunkStart = start + i + 1;
                        chunk.Clear();
                    }
                    else
                    {
                        chunk.Add(digits[i]);
                    }
                }

                if (chunk.Count > 0)
                {
                    var seq = CreateSequence(chunkStart, chunk, null);
                    if (seq != null)
                    {
                        result.Add(seq);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// DigitSequenceInfoを生成する。桁数が1以下の場合はnullを返す。
        /// </summary>
        private static DigitSequenceProcessor.DigitSequenceInfo? CreateSequence(
            int start, List<ParsedDigit> digits, bool? isNumericalReading)
        {
            if (digits.Count <= 1)
            {
                return null;
            }

            var digitValues = new List<byte>(digits.Count);
            foreach (var d in digits)
            {
                if (d.Type == DigitToken.Digit)
                {
                    digitValues.Add(d.Value);
                }
            }

            bool? reading = CheckZeroStart(digits) ? (bool?)false : isNumericalReading;

            return new DigitSequenceProcessor.DigitSequenceInfo(
                start,
                start + digits.Count - 1,
                digitValues,
                reading
            );
        }

        /// <summary>
        /// 先頭が0で始まるかどうかを検査する（電話番号等の判定に使用）。
        /// </summary>
        private static bool CheckZeroStart(List<ParsedDigit> digits)
        {
            return digits.Count > 0
                && digits[0].Type == DigitToken.Digit
                && digits[0].Value == 0;
        }

        /// <summary>
        /// コンマ区切りが正しいかどうかを検査する。
        /// 正しいコンマ区切り: 右端から3桁ごとにコンマが入る（例: 1,234,567）
        /// </summary>
        private static bool CheckCommaSequence(List<ParsedDigit> digits)
        {
            int commaCount = 0;
            for (int revIdx = 0; revIdx < digits.Count; revIdx++)
            {
                int i = digits.Count - 1 - revIdx;
                bool isCommaPlace = revIdx % 4 == 3;

                if (digits[i].Type == DigitToken.Digit && isCommaPlace)
                {
                    return false;
                }
                if (digits[i].Type == DigitToken.Comma && !isCommaPlace)
                {
                    return false;
                }
                if (digits[i].Type == DigitToken.Comma)
                {
                    commaCount++;
                }
            }
            return commaCount > 0;
        }
    }

    // ====== 数字列スコアリング ======

    /// <summary>
    /// 数字列が数値読みかどうかを判定するスコアリング。
    /// 前後の品詞やトークンからスコアを計算し、0以上なら数値読みと判定する。
    /// jpreprocess の digit_sequence/score.rs に相当。
    /// </summary>
    internal static class DigitSequenceScore
    {
        // ハイフン・ダッシュ類
        private const string Haihun1 = "\u2015"; // ―（HORIZONTAL BAR）
        private const string Haihun2 = "\u2212"; // −（MINUS SIGN）
        private const string Haihun3 = "\u2010"; // ‐（HYPHEN）
        private const string Haihun4 = "\u2014"; // —（EM DASH）
        private const string Haihun5 = "\uFF0D"; // −（FULLWIDTH HYPHEN-MINUS）
        private const string Kakko1 = "\uFF08"; // （（FULLWIDTH LEFT PARENTHESIS）
        private const string Kakko2 = "\uFF09"; // ）（FULLWIDTH RIGHT PARENTHESIS）
        private const string Bangou = "番号";

        /// <summary>
        /// 数字列のスコアを計算する。0以上なら数値読み、負なら順序読み。
        /// </summary>
        public static int Score(List<NjdNode> nodes, int start, int end)
        {
            return ScoreStart(nodes, start) + ScoreEnd(nodes, end);
        }

        /// <summary>
        /// 数字列の先頭側のスコアを計算する。
        /// </summary>
        private static int ScoreStart(List<NjdNode> nodes, int start)
        {
            int score = 0;
            if (start <= 0)
                return score;

            var p1 = nodes[start - 1];
            var p1Pos = p1.Details.PartOfSpeech;
            var p1String = p1.Surface;

            // 接頭詞-数接続 → 数値読み寄り
            if (p1Pos.Type == POSType.Settoushi && p1Pos.SubCategory1 == "数接続")
                score += 2;
            // 名詞-副詞可能 → 数値読み寄り
            else if (p1Pos.Type == POSType.Meishi && p1Pos.SubCategory1 == "副詞可能")
                score += 1;
            // 名詞-接尾-助数詞 → 数値読み寄り
            else if (p1Pos.Type == POSType.Meishi && p1Pos.SubCategory1 == "接尾" && p1Pos.SubCategory2 == "助数詞")
                score += 1;

            // 2つ前のノードの情報
            bool p2IsKazu = false;
            bool p2IsBangou = false;
            if (start > 1)
            {
                var p2 = nodes[start - 2];
                p2IsKazu = p2.Details.PartOfSpeech.IsMeishiSuu;
                p2IsBangou = p2.Surface == Bangou;
            }

            // ピリオド判定
            if (IsPeriod(p1String))
            {
                if (p2IsKazu)
                    score -= 5; // 小数点の可能性 → 順序読み寄り
            }
            else
            {
                // ハイフン類 → 電話番号等 → 順序読み寄り
                if (IsHaihun(p1String))
                    score -= 2;
                else if (p1String == Kakko1 && p2IsKazu)
                    score -= 2;
                else if (p1String == Kakko2)
                    score -= 2;
                else if (p1String == Bangou)
                    score -= 2;
            }

            if (p2IsBangou)
                score -= 2;

            return score;
        }

        /// <summary>
        /// 数字列の末尾側のスコアを計算する。
        /// </summary>
        private static int ScoreEnd(List<NjdNode> nodes, int end)
        {
            int score = 0;
            if (end + 1 >= nodes.Count)
                return score;

            var n1 = nodes[end + 1];
            var n1Pos = n1.Details.PartOfSpeech;
            var n1String = n1.Surface;

            // 名詞-副詞可能 → 数値読み寄り（例: 「5回」）
            if (n1Pos.Type == POSType.Meishi && n1Pos.SubCategory1 == "副詞可能")
                score += 2;
            // 名詞-接尾-助数詞 → 数値読み寄り（例: 「5個」）
            else if (n1Pos.Type == POSType.Meishi && n1Pos.SubCategory1 == "接尾" && n1Pos.SubCategory2 == "助数詞")
                score += 2;

            // ハイフン類 → 順序読み寄り
            if (IsHaihun(n1String))
                score -= 2;
            else if (n1String == Kakko1)
                score -= 2;
            else if (n1String == Kakko2)
            {
                // 直後が数字なら順序読み寄り
                if (end + 2 < nodes.Count && nodes[end + 2].Details.PartOfSpeech.IsMeishiSuu)
                    score -= 2;
            }
            else if (n1String == Bangou)
                score -= 2;
            else if (IsPeriod(n1String))
                score += 4; // 小数点の次も数字があるなら数値読み寄り

            return score;
        }

        /// <summary>
        /// ピリオド（全角、中黒）かどうかを判定する。
        /// </summary>
        private static bool IsPeriod(string s)
        {
            return s == "\uFF0E" || s == "\u30FB"; // ．, ・
        }

        /// <summary>
        /// ハイフン類かどうかを判定する。
        /// </summary>
        private static bool IsHaihun(string s)
        {
            return s == Haihun1 || s == Haihun2 || s == Haihun3 || s == Haihun4 || s == Haihun5;
        }
    }
}
