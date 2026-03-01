using System;

namespace DotNetG2P.Models
{
    /// <summary>
    /// 品詞大分類。
    /// jpreprocess の品詞体系に準拠。
    /// </summary>
    public enum POSType
    {
        /// <summary>名詞</summary>
        Meishi,
        /// <summary>動詞</summary>
        Doushi,
        /// <summary>形容詞</summary>
        Keiyoushi,
        /// <summary>副詞</summary>
        Fukushi,
        /// <summary>連体詞</summary>
        Rentaishi,
        /// <summary>接続詞</summary>
        Setsuzokushi,
        /// <summary>感動詞</summary>
        Kandoushi,
        /// <summary>助詞</summary>
        Joshi,
        /// <summary>助動詞</summary>
        Jodoushi,
        /// <summary>接頭詞</summary>
        Settoushi,
        /// <summary>記号</summary>
        Kigou,
        /// <summary>フィラー</summary>
        Filler,
        /// <summary>その他</summary>
        Sonota,
        /// <summary>未知語</summary>
        Unknown,
    }

    /// <summary>
    /// 品詞情報（大分類 + 細分類1-3）。
    /// naist-jdic 辞書フォーマットの品詞フィールドに対応。
    /// </summary>
    public sealed class POS : IEquatable<POS>
    {
        /// <summary>品詞大分類</summary>
        public POSType Type { get; }

        /// <summary>品詞細分類1</summary>
        public string SubCategory1 { get; }

        /// <summary>品詞細分類2</summary>
        public string SubCategory2 { get; }

        /// <summary>品詞細分類3</summary>
        public string SubCategory3 { get; }

        /// <summary>
        /// POS インスタンスを構築する。
        /// </summary>
        /// <param name="type">品詞大分類</param>
        /// <param name="sub1">品詞細分類1（省略時は "*"）</param>
        /// <param name="sub2">品詞細分類2（省略時は "*"）</param>
        /// <param name="sub3">品詞細分類3（省略時は "*"）</param>
        public POS(POSType type, string sub1 = "*", string sub2 = "*", string sub3 = "*")
        {
            Type = type;
            SubCategory1 = sub1 ?? "*";
            SubCategory2 = sub2 ?? "*";
            SubCategory3 = sub3 ?? "*";
        }

        /// <summary>
        /// naist-jdic 辞書の品詞フィールド（4フィールド）から POS を構築する。
        /// </summary>
        /// <param name="pos">品詞大分類（日本語文字列）</param>
        /// <param name="sub1">品詞細分類1</param>
        /// <param name="sub2">品詞細分類2</param>
        /// <param name="sub3">品詞細分類3</param>
        /// <returns>対応する POS インスタンス</returns>
        public static POS FromFeatures(string pos, string sub1, string sub2, string sub3)
        {
            if (pos == null)
                throw new ArgumentNullException(nameof(pos));

            POSType type = pos switch
            {
                "名詞" => POSType.Meishi,
                "動詞" => POSType.Doushi,
                "形容詞" => POSType.Keiyoushi,
                "副詞" => POSType.Fukushi,
                "連体詞" => POSType.Rentaishi,
                "接続詞" => POSType.Setsuzokushi,
                "感動詞" => POSType.Kandoushi,
                "助詞" => POSType.Joshi,
                "助動詞" => POSType.Jodoushi,
                "接頭詞" => POSType.Settoushi,
                "記号" => POSType.Kigou,
                "フィラー" => POSType.Filler,
                "その他" => POSType.Sonota,
                _ => POSType.Unknown
            };

            return new POS(type, sub1, sub2, sub3);
        }

        // ====== ヘルパープロパティ: 品詞大分類判定 ======

        /// <summary>名詞かどうか</summary>
        public bool IsMeishi => Type == POSType.Meishi;

        /// <summary>動詞かどうか</summary>
        public bool IsDoushi => Type == POSType.Doushi;

        /// <summary>形容詞かどうか</summary>
        public bool IsKeiyoushi => Type == POSType.Keiyoushi;

        /// <summary>助詞かどうか</summary>
        public bool IsJoshi => Type == POSType.Joshi;

        /// <summary>助動詞かどうか</summary>
        public bool IsJodoushi => Type == POSType.Jodoushi;

        /// <summary>記号かどうか</summary>
        public bool IsKigou => Type == POSType.Kigou;

        /// <summary>フィラーかどうか</summary>
        public bool IsFiller => Type == POSType.Filler;

        /// <summary>内容語（名詞・動詞・形容詞）かどうか</summary>
        public bool IsContentWord => IsMeishi || IsDoushi || IsKeiyoushi;

        // ====== ヘルパープロパティ: 名詞サブカテゴリ判定（OpenJTalk NJD処理用） ======

        /// <summary>数詞（名詞-数）かどうか</summary>
        public bool IsMeishiSuu => Type == POSType.Meishi && SubCategory1 == "数";

        /// <summary>固有名詞（名詞-固有名詞）かどうか</summary>
        public bool IsMeishiKoyuu => Type == POSType.Meishi && SubCategory1 == "固有名詞";

        /// <summary>名詞接尾辞（名詞-接尾）かどうか</summary>
        public bool IsMeishiSetsubi => Type == POSType.Meishi && SubCategory1 == "接尾";

        /// <summary>
        /// 指定した POS と等しいかどうかを判定する。
        /// </summary>
        public bool Equals(POS other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Type == other.Type
                && SubCategory1 == other.SubCategory1
                && SubCategory2 == other.SubCategory2
                && SubCategory3 == other.SubCategory3;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as POS);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Type.GetHashCode();
                hash = hash * 31 + (SubCategory1?.GetHashCode() ?? 0);
                hash = hash * 31 + (SubCategory2?.GetHashCode() ?? 0);
                hash = hash * 31 + (SubCategory3?.GetHashCode() ?? 0);
                return hash;
            }
        }

        /// <summary>
        /// 品詞情報を文字列化する。
        /// 形式: "大分類,細分類1,細分類2,細分類3"
        /// </summary>
        public override string ToString()
        {
            string posName = Type switch
            {
                POSType.Meishi => "名詞",
                POSType.Doushi => "動詞",
                POSType.Keiyoushi => "形容詞",
                POSType.Fukushi => "副詞",
                POSType.Rentaishi => "連体詞",
                POSType.Setsuzokushi => "接続詞",
                POSType.Kandoushi => "感動詞",
                POSType.Joshi => "助詞",
                POSType.Jodoushi => "助動詞",
                POSType.Settoushi => "接頭詞",
                POSType.Kigou => "記号",
                POSType.Filler => "フィラー",
                POSType.Sonota => "その他",
                POSType.Unknown => "未知語",
                _ => "不明"
            };

            return $"{posName},{SubCategory1},{SubCategory2},{SubCategory3}";
        }
    }
}
