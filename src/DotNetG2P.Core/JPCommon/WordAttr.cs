using System;
using System.Collections.Generic;
using DotNetG2P.Models;

namespace DotNetG2P.JPCommon
{
    /// <summary>
    /// POS(品詞)、CType(活用型)、CForm(活用形)のフルコンテキストラベル用ID変換テーブル。
    /// jpreprocess の word_attr.rs に準拠。
    /// </summary>
    public static class WordAttr
    {
        // ====== POS → ID ======

        private static readonly Dictionary<string, int> PosTable = new Dictionary<string, int>(70, StringComparer.Ordinal)
        {
            // jpreprocess jpcommon/label/word_attr.rs 準拠
            { "その他,間投,*,*", 1 },
            { "フィラー,*,*,*", 2 },
            { "感動詞,*,*,*", 3 },
            { "記号,アルファベット,*,*", 4 },
            { "記号,一般,*,*", 5 },
            { "記号,括弧開,*,*", 6 },
            { "記号,括弧閉,*,*", 7 },
            { "記号,句点,*,*", 8 },
            { "記号,空白,*,*", 9 },
            { "記号,読点,*,*", 10 },
            { "形容詞,自立,*,*", 11 },
            { "形容詞,接尾,*,*", 12 },
            { "形容詞,非自立,*,*", 13 },
            { "助詞,格助詞,一般,*", 14 },
            { "助詞,格助詞,引用,*", 15 },
            { "助詞,格助詞,連語,*", 16 },
            { "助詞,係助詞,*,*", 17 },
            { "助詞,終助詞,*,*", 18 },
            { "助詞,接続助詞,*,*", 19 },
            { "助詞,特殊,*,*", 20 },
            { "助詞,副詞化,*,*", 21 },
            { "助詞,副助詞,*,*", 22 },
            { "助詞,副助詞／並立助詞／終助詞,*,*", 23 },
            { "助詞,並立助詞,*,*", 24 },
            { "助詞,連体化,*,*", 25 },
            { "助動詞,*,*,*", 26 },
            { "接続詞,*,*,*", 27 },
            { "接頭詞,形容詞接続,*,*", 28 },
            { "接頭詞,数接続,*,*", 29 },
            { "接頭詞,動詞接続,*,*", 30 },
            { "接頭詞,名詞接続,*,*", 31 },
            { "動詞,自立,*,*", 32 },
            { "動詞,接尾,*,*", 33 },
            { "動詞,非自立,*,*", 34 },
            { "副詞,一般,*,*", 35 },
            { "副詞,助詞類接続,*,*", 36 },
            { "名詞,サ変接続,*,*", 37 },
            { "名詞,ナイ形容詞語幹,*,*", 38 },
            { "名詞,一般,*,*", 39 },
            { "名詞,引用文字列,*,*", 40 },
            { "名詞,形容動詞語幹,*,*", 41 },
            { "名詞,固有名詞,一般,*", 42 },
            { "名詞,固有名詞,人名,一般", 43 },
            { "名詞,固有名詞,人名,姓", 44 },
            { "名詞,固有名詞,人名,名", 45 },
            { "名詞,固有名詞,地域,一般", 46 },
            { "名詞,固有名詞,地域,国", 47 },
            { "名詞,固有名詞,組織,*", 48 },
            { "名詞,数,*,*", 49 },
            { "名詞,接続詞的,*,*", 50 },
            { "名詞,接尾,サ変接続,*", 51 },
            { "名詞,接尾,一般,*", 52 },
            { "名詞,接尾,形容動詞語幹,*", 53 },
            { "名詞,接尾,助数詞,*", 54 },
            { "名詞,接尾,助動詞語幹,*", 55 },
            { "名詞,接尾,人名,*", 56 },
            { "名詞,接尾,地域,*", 57 },
            { "名詞,接尾,特殊,*", 58 },
            { "名詞,接尾,副詞可能,*", 59 },
            { "名詞,代名詞,一般,*", 60 },
            { "名詞,代名詞,縮約,*", 61 },
            { "名詞,動詞非自立的,*,*", 62 },
            { "名詞,特殊,助動詞語幹,*", 63 },
            { "名詞,非自立,一般,*", 64 },
            { "名詞,非自立,形容動詞語幹,*", 65 },
            { "名詞,非自立,助動詞語幹,*", 66 },
            { "名詞,非自立,副詞可能,*", 67 },
            { "名詞,副詞可能,*,*", 68 },
            { "連体詞,*,*,*", 69 },
        };

        // ====== CType → ID ======

        private static readonly Dictionary<string, int> CTypeTable = new Dictionary<string, int>(57, StringComparer.Ordinal)
        {
            { "カ変・クル", 1 },
            { "カ変・来ル", 2 },
            { "サ変・−スル", 3 },
            { "サ変・−ズル", 4 },
            { "サ変・スル", 5 },
            { "ラ変", 6 },
            { "一段", 7 },
            { "一段・クレル", 8 },
            { "一段・得ル", 9 },
            { "下二・カ行", 10 },
            { "下二・ガ行", 11 },
            { "下二・タ行", 12 },
            { "下二・ダ行", 13 },
            { "下二・ハ行", 14 },
            { "下二・マ行", 15 },
            { "下二・得", 16 },
            { "形容詞・アウオ段", 17 },
            { "形容詞・イ段", 18 },
            { "形容詞・イイ", 19 },
            { "五段・カ行イ音便", 20 },
            { "五段・カ行促音便", 21 },
            { "五段・カ行促音便ユク", 22 },
            { "五段・ガ行", 23 },
            { "五段・サ行", 24 },
            { "五段・タ行", 25 },
            { "五段・ナ行", 26 },
            { "五段・バ行", 27 },
            { "五段・マ行", 28 },
            { "五段・ラ行", 29 },
            { "五段・ラ行アル", 30 },
            { "五段・ラ行特殊", 31 },
            { "五段・ワ行ウ音便", 32 },
            { "五段・ワ行促音便", 33 },
            { "四段・サ行", 34 },
            { "四段・タ行", 35 },
            { "四段・ハ行", 36 },
            { "四段・バ行", 37 },
            { "上二・ダ行", 38 },
            { "上二・ハ行", 39 },
            { "特殊・ジャ", 40 },
            { "特殊・タ", 41 },
            { "特殊・タイ", 42 },
            { "特殊・ダ", 43 },
            { "特殊・デス", 44 },
            { "特殊・ナイ", 45 },
            { "特殊・ヌ", 46 },
            { "特殊・マス", 47 },
            { "特殊・ヤ", 48 },
            { "不変化型", 49 },
            { "文語・キ", 50 },
            { "文語・ケリ", 51 },
            { "文語・ゴトシ", 52 },
            { "文語・ナリ", 53 },
            { "文語・ベシ", 54 },
            { "文語・マジ", 55 },
            { "文語・リ", 56 },
            { "文語・ル", 57 },
        };

        // ====== CForm → ID ======

        private static readonly Dictionary<string, int> CFormTable = new Dictionary<string, int>(26, StringComparer.Ordinal)
        {
            { "ガル接続", 1 },
            { "仮定形", 2 },
            { "仮定縮約１", 3 },
            { "仮定縮約２", 4 },
            { "基本形", 5 },
            { "基本形-促音便", 6 },
            { "現代基本形", 7 },
            { "体言接続", 8 },
            { "体言接続特殊", 9 },
            { "体言接続特殊２", 10 },
            { "命令ｅ", 11 },
            { "命令ｉ", 12 },
            { "命令ｒｏ", 13 },
            { "命令ｙｏ", 14 },
            { "未然ウ接続", 15 },
            { "未然ヌ接続", 16 },
            { "未然レル接続", 17 },
            { "未然形", 18 },
            { "未然特殊", 19 },
            { "連用ゴザイ接続", 20 },
            { "連用タ接続", 21 },
            { "連用テ接続", 22 },
            { "連用デ接続", 23 },
            { "連用ニ接続", 24 },
            { "連用形", 25 },
            { "音便基本形", 26 },
        };

        /// <summary>
        /// POS オブジェクトからPOS IDに変換する。
        /// </summary>
        /// <param name="pos">品詞情報</param>
        /// <returns>POS ID（1始まり）、見つからない場合はnull</returns>
        public static int? PosToId(POS pos)
        {
            if (pos == null)
                return null;

            string posName = pos.Type switch
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
                _ => null
            };

            if (posName == null)
                return null;

            return GetPosId(posName, pos.SubCategory1, pos.SubCategory2, pos.SubCategory3);
        }

        /// <summary>
        /// 活用型文字列からCType IDに変換する（CTypeToIdのエイリアス）。
        /// </summary>
        public static int? CTypeToId(string conjugationType)
        {
            return GetCTypeId(conjugationType);
        }

        /// <summary>
        /// 活用形文字列からCForm IDに変換する（CFormToIdのエイリアス）。
        /// </summary>
        public static int? CFormToId(string conjugationForm)
        {
            return GetCFormId(conjugationForm);
        }

        /// <summary>
        /// 品詞文字列(4フィールド)からPOS IDに変換する。
        /// 対応するIDが見つからない場合はnullを返す。
        /// </summary>
        /// <param name="pos">品詞大分類</param>
        /// <param name="sub1">品詞細分類1</param>
        /// <param name="sub2">品詞細分類2</param>
        /// <param name="sub3">品詞細分類3</param>
        /// <returns>POS ID（1始まり）、見つからない場合はnull</returns>
        public static int? GetPosId(string pos, string sub1, string sub2, string sub3)
        {
            string key = $"{pos},{sub1},{sub2},{sub3}";
            if (PosTable.TryGetValue(key, out int id))
                return id;
            return null;
        }

        /// <summary>
        /// 活用型文字列からCType IDに変換する。
        /// "*" の場合はnullを返す。
        /// </summary>
        public static int? GetCTypeId(string ctype)
        {
            if (ctype == null || ctype == "*")
                return null;
            if (CTypeTable.TryGetValue(ctype, out int id))
                return id;
            return null;
        }

        /// <summary>
        /// 活用形文字列からCForm IDに変換する。
        /// "*" の場合はnullを返す。
        /// </summary>
        public static int? GetCFormId(string cform)
        {
            if (cform == null || cform == "*")
                return null;
            if (CFormTable.TryGetValue(cform, out int id))
                return id;
            return null;
        }

        /// <summary>
        /// POS IDをフルコンテキストラベルのフォーマット文字列に変換する。
        /// nullの場合は "xx" を返す。
        /// </summary>
        public static string FormatPosId(int? id)
        {
            return id.HasValue ? id.Value.ToString("D2") : "xx";
        }

        /// <summary>
        /// CType/CForm IDをフルコンテキストラベルのフォーマット文字列に変換する。
        /// nullの場合は "xx" を返す。
        /// </summary>
        public static string FormatId(int? id)
        {
            return id.HasValue ? id.Value.ToString() : "xx";
        }
    }
}
