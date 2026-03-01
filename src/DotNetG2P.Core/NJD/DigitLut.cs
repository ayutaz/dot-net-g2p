using System.Collections.Generic;
using DotNetG2P.Models;

namespace DotNetG2P.NJD
{
    /// <summary>
    /// 数字読み変換（SetDigit）処理で使用されるLUT（Look-Up Table）群。
    /// jpreprocess の open_jtalk/digit/lut/ に相当。
    /// </summary>
    public static class DigitLut
    {
        // =====================================================================
        // DigitType: 連濁・半濁の種別
        // =====================================================================

        /// <summary>
        /// 助数詞の音便変化の種別。
        /// 連濁（Voiced: 「ひゃく」→「びゃく」）か半濁（SemiVoiced: 「ひゃく」→「ぴゃく」）を示す。
        /// </summary>
        public enum DigitType
        {
            /// <summary>連濁（濁音化）</summary>
            Voiced,
            /// <summary>半濁音化</summary>
            SemiVoiced,
        }

        // =====================================================================
        // 基数・位取り判定用セット（numeral.rs の NUMERAL_LIST4, LIST5）
        // =====================================================================

        /// <summary>
        /// 数字漢字セット（一〜九 + 何/幾/数）。
        /// numeral.rs の NUMERAL_LIST4 に対応。
        /// </summary>
        public static readonly HashSet<string> NumeralDigits = new HashSet<string>
        {
            "一", "二", "三", "四", "五", "六", "七", "八", "九", "何", "幾", "数",
        };

        /// <summary>
        /// 位取り漢字セット（十〜無量大数）。
        /// numeral.rs の NUMERAL_LIST5 に対応。
        /// </summary>
        public static readonly HashSet<string> NumeralPlaces = new HashSet<string>
        {
            "十", "百", "千", "万", "億", "兆", "京", "垓",
            "𥝱",
            "穣", "溝", "澗", "正", "載", "極",
            "恒河沙", "阿僧祇", "那由他", "不可思議", "無量大数",
        };

        // =====================================================================
        // 位取り音便変化テーブル（numeral.rs の NUMERAL_LIST6-11）
        // =====================================================================

        /// <summary>
        /// 位取りの音便変化（連濁/半濁）テーブル。
        /// key1: 位取り漢字, key2: 数字漢字 → DigitType。
        /// NUMERAL_LIST6 + NUMERAL_LIST7 に対応。
        /// 「百」「千」の前の「三」→連濁、「六」「八」→半濁、「何」→連濁。
        /// </summary>
        public static readonly ConvEntry<DigitType>[] NumerativeConvTable = new[]
        {
            new ConvEntry<DigitType>(
                new HashSet<string> { "百", "千" },
                new Dictionary<string, DigitType>
                {
                    { "三", DigitType.Voiced },
                    { "六", DigitType.SemiVoiced },
                    { "八", DigitType.SemiVoiced },
                    { "何", DigitType.Voiced },
                }
            ),
        };

        /// <summary>
        /// 位取りの発音変化テーブル。
        /// key1: 位取り漢字, key2: 数字漢字 → Pronunciation。
        /// NUMERAL_LIST8-11 に対応。
        /// </summary>
        public static readonly ConvEntry<Pronunciation>[] DigitConvTable = new[]
        {
            // NUMERAL_LIST8 + LIST9: 「百」の前の「六」→ロッ、「八」→ハッ
            new ConvEntry<Pronunciation>(
                new HashSet<string> { "百" },
                new Dictionary<string, Pronunciation>
                {
                    { "六", Pronunciation.FromKatakana("ロッ", 0) },
                    { "八", Pronunciation.FromKatakana("ハッ", 0) },
                }
            ),
            // NUMERAL_LIST10 + LIST11: 「千」「兆」の前の「一」→イッ、「八」→ハッ、「十」→ジュッ
            new ConvEntry<Pronunciation>(
                new HashSet<string> { "千", "兆" },
                new Dictionary<string, Pronunciation>
                {
                    { "一", Pronunciation.FromKatakana("イッ", 0) },
                    { "八", Pronunciation.FromKatakana("ハッ", 0) },
                    { "十", Pronunciation.FromKatakana("ジュッ", 1) },
                }
            ),
        };

        // =====================================================================
        // 助数詞クラス1: 数字の発音変化（class1.rs）
        // =====================================================================

        /// <summary>
        /// 助数詞クラス1 変換テーブル。
        /// key1: 助数詞, key2: 数字漢字 → Pronunciation。
        /// class1.rs の CONVERSION_TABLE に対応。
        /// </summary>
        public static readonly ConvEntry<Pronunciation>[] Class1ConvTable = new[]
        {
            // CLASS1B: 年/円 系 → 四→ヨ
            new ConvEntry<Pronunciation>(
                new HashSet<string>
                {
                    "年", "円",
                    "年間", "年生", "年代", "年度", "年版", "年余", "年来", "えん",
                },
                new Dictionary<string, Pronunciation>
                {
                    { "四", Pronunciation.FromKatakana("ヨ", 0) },
                }
            ),
            // CLASS1C1: 人 系 → 四→ヨ, 七→シチ
            new ConvEntry<Pronunciation>(
                new HashSet<string>
                {
                    "人",
                    "人月", "人前", "人組",
                },
                new Dictionary<string, Pronunciation>
                {
                    { "四", Pronunciation.FromKatakana("ヨ", 0) },
                    { "七", Pronunciation.FromKatakana("シチ", 1) },
                }
            ),
            // CLASS1C2: 時/時間 系 → 四→ヨ, 七→シチ, 九→ク
            new ConvEntry<Pronunciation>(
                new HashSet<string>
                {
                    "時", "時間",
                    "時限", "時半",
                },
                new Dictionary<string, Pronunciation>
                {
                    { "四", Pronunciation.FromKatakana("ヨ", 0) },
                    { "七", Pronunciation.FromKatakana("シチ", 1) },
                    { "九", Pronunciation.FromKatakana("ク", 0) },
                }
            ),
            // CLASS1D: 日/日間 系 → 七→シチ, 九→ク
            new ConvEntry<Pronunciation>(
                new HashSet<string>
                {
                    "日",
                    "日間",
                },
                new Dictionary<string, Pronunciation>
                {
                    { "七", Pronunciation.FromKatakana("シチ", 1) },
                    { "九", Pronunciation.FromKatakana("ク", 0) },
                }
            ),
            // CLASS1E: 月 系 → 四→シ, 七→シチ, 九→ク
            new ConvEntry<Pronunciation>(
                new HashSet<string> { "月" },
                new Dictionary<string, Pronunciation>
                {
                    { "四", Pronunciation.FromKatakana("シ", 0) },
                    { "七", Pronunciation.FromKatakana("シチ", 1) },
                    { "九", Pronunciation.FromKatakana("ク", 0) },
                }
            ),
            // CLASS1F: 意図的に空セット（jpreprocess では「工」「つ」がコメントアウト /* modified */）
            // キーセットが空のためマッチすることはないが、原典の構造を保持するため残している
            new ConvEntry<Pronunciation>(
                new HashSet<string>(),
                new Dictionary<string, Pronunciation>
                {
                    { "六", Pronunciation.FromKatakana("ロッ", 1) },
                    { "八", Pronunciation.FromKatakana("ハッ", 1) },
                    { "十", Pronunciation.FromKatakana("ジュッ", 1) },
                    { "百", Pronunciation.FromKatakana("ヒャッ", 1) },
                }
            ),
            // CLASS1G: 個/階/分/発/本 他多数 → 一→イッ, 六→ロッ, 八→ハッ, 十→ジュッ, 百→ヒャッ
            new ConvEntry<Pronunciation>(
                new HashSet<string>
                {
                    "個", "階", "分", "発", "本", "鉢", "口", "切れ", "箱",
                    "か月", "か国", "か所", "か条", "か村", "か年", "カ月", "カ国", "カ寺", "カ所", "カ条", "カ村",
                    "カ店", "カ年", "ケ月", "ケ国", "ケ所", "ケ条", "ケ村", "ケ年", "ヵ月", "ヵ国", "ヵ所",
                    "ヵ条", "ヵ村", "ヵ年", "ヶ月", "ヶ国", "ヶ所", "ヶ条", "ヶ村", "ヶ年", "個月", "個口",
                    "個国", "個条", "個年", "箇月", "箇国", "箇所", "箇条", "箇年", "かけ", "くだり", "けた",
                    "価", "課", "画", "回", "回忌", "回生", "回戦", "回線", "回分", "海里", "カイリ", "浬", "角",
                    "株", "冠", "巻", "缶", "貫", "貫目", "間", "基", "期", "期生", "機", "気圧", "季", "騎",
                    "客", "脚", "球", "級", "橋", "局", "曲", "極", "重ね", "斤", "金", "句", "区", "躯", "計",
                    "桁", "ケタ", "校", "港", "項", "組", "件", "軒", "言", "戸", "湖", "光年", "石",
                    "ぴき", "ぺん", "波", "派", "敗", "杯", "拍", "泊", "版", "犯", "班", "匹", "疋", "筆", "俵",
                    "票", "品", "分間", "分目", "片", "篇", "編", "辺", "遍", "歩", "報", "方",
                    "法", "本立て", "頭身",
                },
                new Dictionary<string, Pronunciation>
                {
                    { "一", Pronunciation.FromKatakana("イッ", 1) },
                    { "六", Pronunciation.FromKatakana("ロッ", 1) },
                    { "八", Pronunciation.FromKatakana("ハッ", 1) },
                    { "十", Pronunciation.FromKatakana("ジュッ", 1) },
                    { "百", Pronunciation.FromKatakana("ヒャッ", 1) },
                }
            ),
            // CLASS1H: 才/頭/着/足/尺/坪/通り/センチ 他多数 → 一→イッ, 八→ハッ, 十→ジュッ
            new ConvEntry<Pronunciation>(
                new HashSet<string>
                {
                    "．", "・", "才", "頭", "着", "足", "尺", "坪", "通り", "センチ", "シーシー",
                    "ＣＣ", "ｃｃ", "ｃｍ", "サイクル", "サンチーム", "シーズン", "シート", "シリング",
                    "シンガポールドル", "スイスフラン", "スウェーデンクローネ", "スクレ", "セット", "セント",
                    "ソル", "ゾーン", "糎", "竿", "差", "差し", "歳", "歳児", "作", "冊", "刷", "皿", "棹",
                    "艘", "子", "視", "式", "失", "室", "射", "社", "勺", "種", "首", "周", "周忌", "周年", "州",
                    "週", "週間", "集", "宿", "所", "勝", "升", "床", "章", "色", "食", "親等", "進",
                    "進数", "品", "すじ", "そう", "そろい", "筋", "数", "寸", "世", "隻", "席", "石", "節", "戦",
                    "線", "選", "銭", "層", "相", "揃", "たび", "つかみ", "つがい", "つぶ", "つまみ", "つ折",
                    "つ折り", "とおり", "とき", "ところ", "とせ", "玉", "月", "手", "束", "続き", "体", "対",
                    "卓", "樽", "反", "丁", "丁目", "鳥", "通", "掴み", "艇", "滴", "店", "転", "点", "斗", "棟",
                    "盗", "灯", "等", "等席", "等地", "等分", "答", "得", "噸", "粒", "種類", "歳馬", "世紀",
                    "車種",
                },
                new Dictionary<string, Pronunciation>
                {
                    { "一", Pronunciation.FromKatakana("イッ", 1) },
                    { "八", Pronunciation.FromKatakana("ハッ", 1) },
                    { "十", Pronunciation.FromKatakana("ジュッ", 1) },
                }
            ),
            // CLASS1I: キロ/カロリー 系 → 六→ロッ, 十→ジュッ, 百→ヒャッ
            new ConvEntry<Pronunciation>(
                new HashSet<string>
                {
                    "キロ", "カロリー",
                    "ｃａｌ", "ｋｂ", "ｋｇ", "ｋｌ", "ｋｍ", "ｋｔ", "ｋｗ", "ｋグラム", "ｋバイト", "ｋヘルツ",
                    "ｋメートル", "ｋリットル", "ｋワット", "カナダドル", "カラット", "ガロン", "キュリー",
                    "キロカロリー", "キログラム", "キロトン", "キロバイト", "キロヘルツ", "キロメートル",
                    "キロリットル", "キロワット", "キロワット時", "クラス", "クローナ", "クローネ", "グァラニ",
                    "ケース", "コース", "粁",
                },
                new Dictionary<string, Pronunciation>
                {
                    { "六", Pronunciation.FromKatakana("ロッ", 1) },
                    { "十", Pronunciation.FromKatakana("ジュッ", 1) },
                    { "百", Pronunciation.FromKatakana("ヒャッ", 1) },
                }
            ),
            // CLASS1J: トン 系 → 一→イッ, 十→ジュッ
            new ConvEntry<Pronunciation>(
                new HashSet<string>
                {
                    "トン",
                    "ｔ", "タル", "テラ", "トライ",
                },
                new Dictionary<string, Pronunciation>
                {
                    { "一", Pronunciation.FromKatakana("イッ", 1) },
                    { "十", Pronunciation.FromKatakana("ジュッ", 1) },
                }
            ),
            // CLASS1K: 房/柱/％/ポンド 系 → 十→ジュッ
            new ConvEntry<Pronunciation>(
                new HashSet<string>
                {
                    "房", "柱", "％", "ポンド",
                    "ｐａ", "ｐｐｍ", "パーセント", "パーミル", "パスカル", "パック", "パット", "ピーピーエム",
                    "ピコ", "ページ", "頁", "ペア", "ペセタ", "ペソ", "ペニー", "ペニヒ", "ペンス", "ポイント",
                    "振り", "針", "袋", "張り", "平米", "平方キロ", "平方キロメートル", "平方センチメートル",
                    "平方メートル", "品目",
                },
                new Dictionary<string, Pronunciation>
                {
                    { "十", Pronunciation.FromKatakana("ジュッ", 1) },
                }
            ),
        };

        // =====================================================================
        // 助数詞クラス2: 助数詞側の音便変化（class2.rs）
        // =====================================================================

        /// <summary>
        /// 助数詞クラス2 変換テーブル。
        /// key1: 助数詞, key2: 数字漢字 → DigitType（助数詞側が連濁/半濁する）。
        /// class2.rs の CONVERSION_TABLE に対応。
        /// </summary>
        public static readonly ConvEntry<DigitType>[] Class2ConvTable = new[]
        {
            // CLASS2B: 分/版/敗/発/拍/鉢 他 → 大半が半濁
            new ConvEntry<DigitType>(
                new HashSet<string>
                {
                    "分", "版", "敗", "発", "拍", "鉢",
                    "波", "派", "泊", "犯", "班", "品", "分間", "分目", "片", "篇", "編", "辺", "遍", "歩", "報",
                    "方",
                },
                new Dictionary<string, DigitType>
                {
                    { "一", DigitType.SemiVoiced },
                    { "三", DigitType.SemiVoiced },
                    { "四", DigitType.SemiVoiced },
                    { "六", DigitType.SemiVoiced },
                    { "八", DigitType.SemiVoiced },
                    { "十", DigitType.SemiVoiced },
                    { "百", DigitType.SemiVoiced },
                    { "千", DigitType.SemiVoiced },
                    { "万", DigitType.SemiVoiced },
                    { "何", DigitType.SemiVoiced },
                }
            ),
            // CLASS2C: 本/匹/疋/票/俵/箱 他 → 混合
            new ConvEntry<DigitType>(
                new HashSet<string>
                {
                    "本", "匹", "疋", "票", "俵", "箱",
                    "本立て", "杯", "針", "柱",
                },
                new Dictionary<string, DigitType>
                {
                    { "一", DigitType.SemiVoiced },
                    { "三", DigitType.Voiced },
                    { "六", DigitType.SemiVoiced },
                    { "八", DigitType.SemiVoiced },
                    { "十", DigitType.SemiVoiced },
                    { "百", DigitType.SemiVoiced },
                    { "千", DigitType.Voiced },
                    { "万", DigitType.Voiced },
                    { "何", DigitType.Voiced },
                }
            ),
            // CLASS2D: 意図的に空セット（jpreprocess では「工」「つ」がコメントアウト /* modified */）
            // キーセットが空のためマッチすることはないが、原典の構造を保持するため残している
            new ConvEntry<DigitType>(
                new HashSet<string>(),
                new Dictionary<string, DigitType>
                {
                    { "三", DigitType.Voiced },
                    { "六", DigitType.SemiVoiced },
                    { "八", DigitType.SemiVoiced },
                    { "十", DigitType.SemiVoiced },
                    { "百", DigitType.SemiVoiced },
                    { "千", DigitType.Voiced },
                    { "万", DigitType.Voiced },
                    { "何", DigitType.Voiced },
                }
            ),
            // CLASS2E: 軒/石/足/尺 他 → 三/千/万が連濁
            new ConvEntry<DigitType>(
                new HashSet<string>
                {
                    "軒", "石", "足", "尺",
                    "かけ", "重ね", "件", "勺",
                },
                new Dictionary<string, DigitType>
                {
                    { "三", DigitType.Voiced },
                    { "千", DigitType.Voiced },
                    { "万", DigitType.Voiced },
                }
            ),
            // CLASS2F: 階 → 三が連濁
            new ConvEntry<DigitType>(
                new HashSet<string> { "階" },
                new Dictionary<string, DigitType>
                {
                    { "三", DigitType.Voiced },
                }
            ),
        };

        // =====================================================================
        // 助数詞クラス3: 和語読み変化（class3.rs）
        // =====================================================================

        /// <summary>
        /// 助数詞クラス3 変換テーブル。
        /// key1: 助数詞 + 読みのペア, key2: 数字漢字 → Pronunciation。
        /// class3.rs の CONVERSION_TABLE に対応。
        /// 助数詞の漢字と読み（カタカナ）の組み合わせで検索する。
        /// </summary>
        public static readonly Class3Entry[] Class3ConvTable = new[]
        {
            new Class3Entry(
                new Dictionary<string, string[]>
                {
                    { "棟", new[] { "ムネ" } },
                    { "かけ", new[] { "カケ" } },
                    { "くだり", new[] { "クダリ" } },
                    { "けた", new[] { "ケタ" } },
                    { "すじ", new[] { "スジ" } },
                    { "そろい", new[] { "ソロイ" } },
                    { "たび", new[] { "タビ" } },
                    { "つかみ", new[] { "ツカミ" } },
                    { "つがい", new[] { "ツガイ" } },
                    { "つまみ", new[] { "ツマミ" } },
                    { "とおり", new[] { "トオリ" } },
                    { "ところ", new[] { "トコロ" } },
                    { "とせ", new[] { "トセ" } },
                    { "まわり", new[] { "マワリ" } },
                    { "シーズン", new[] { "シーズン" } },
                    { "セット", new[] { "セット" } },
                    { "握り", new[] { "ニギリ" } },
                    { "回り", new[] { "マワリ" } },
                    { "株", new[] { "カブ" } },
                    { "竿", new[] { "サオ" } },
                    { "筋", new[] { "スジ" } },
                    { "桁", new[] { "ケタ" } },
                    { "ケタ", new[] { "ケタ" } },
                    { "月", new[] { "ツキ" } },
                    { "言", new[] { "コト" } },
                    { "口", new[] { "クチ" } },
                    { "差し", new[] { "サシ" } },
                    { "皿", new[] { "サラ" } },
                    { "山", new[] { "ヤマ" } },
                    { "勺", new[] { "シャク" } },
                    { "尺", new[] { "シャク" } },
                    { "重ね", new[] { "カサネ", "ガサネ" } },
                    { "振り", new[] { "フリ" } },
                    { "針", new[] { "ハリ" } },
                    { "切れ", new[] { "キレ" } },
                    { "束", new[] { "タバ" } },
                    { "続き", new[] { "ツヅキ" } },
                    { "揃", new[] { "ソロイ" } },
                    { "袋", new[] { "フクロ" } },
                    { "柱", new[] { "ハシラ" } },
                    { "張り", new[] { "ハリ" } },
                    { "通り", new[] { "トオリ" } },
                    { "掴み", new[] { "ツカミ" } },
                    { "坪", new[] { "ツボ" } },
                    { "箱", new[] { "ハコ" } },
                    { "鉢", new[] { "ハチ" } },
                    { "晩", new[] { "バン" } },
                    { "品", new[] { "シナ" } },
                    { "瓶", new[] { "ビン" } },
                    { "分け", new[] { "ワケ" } },
                    { "幕", new[] { "マク" } },
                    { "夜", new[] { "ヤ", "ヨ" } },
                    { "粒", new[] { "ツブ" } },
                    { "枠", new[] { "ワク" } },
                    { "棹", new[] { "サオ" } },
                    { "つ折", new[] { "ツオリ" } },
                    { "つ折り", new[] { "ツオリ" } },
                    { "つぶ", new[] { "ツブ" } },
                    { "とき", new[] { "トキ" } },
                },
                new Dictionary<string, Pronunciation>
                {
                    { "一", Pronunciation.FromKatakana("ヒト", 0) },
                    { "二", Pronunciation.FromKatakana("フタ", 0) },
                }
            ),
        };

        // =====================================================================
        // 特殊読みテーブル（others.rs: 人/日/日間）
        // =====================================================================

        /// <summary>
        /// 特殊読み変換テーブル。
        /// key1: 助数詞, key2: 数字漢字 → 完全なノード情報文字列。
        /// others.rs の CONVERSION_TABLE に対応。
        /// 「一人」→ヒトリ、「二人」→フタリ、「二日」→フツカ 等の特殊読み。
        /// </summary>
        public static readonly ConvEntry<string>[] SpecialConvTable = new[]
        {
            // 人: 一人→ヒトリ、二人→フタリ
            new ConvEntry<string>(
                new HashSet<string> { "人" },
                new Dictionary<string, string>
                {
                    { "一", "一人,名詞,副詞可能,*,*,*,*,一人,ヒトリ,ヒトリ,2/3,*" },
                    { "二", "二人,名詞,副詞可能,*,*,*,*,二人,フタリ,フタリ,3/3,*" },
                }
            ),
            // 日: 一日〜十日の特殊読み
            new ConvEntry<string>(
                new HashSet<string> { "日" },
                new Dictionary<string, string>
                {
                    { "一", "一日,名詞,副詞可能,*,*,*,*,一日,イチニチ,イチニチ,4/4,*" },
                    { "二", "二日,名詞,副詞可能,*,*,*,*,二日,フツカ,フツカ,0/3,*" },
                    { "三", "三日,名詞,副詞可能,*,*,*,*,三日,ミッカ,ミッカ,0/3,*" },
                    { "四", "四日,名詞,副詞可能,*,*,*,*,四日,ヨッカ,ヨッカ,0/3,*" },
                    { "五", "五日,名詞,副詞可能,*,*,*,*,五日,イツカ,イツカ,0/3,*" },
                    { "六", "六日,名詞,副詞可能,*,*,*,*,六日,ムイカ,ムイカ,0/3,*" },
                    { "七", "七日,名詞,副詞可能,*,*,*,*,七日,ナノカ,ナノカ,0/3,*" },
                    { "八", "八日,名詞,副詞可能,*,*,*,*,八日,ヨウカ,ヨーカ,0/3,*" },
                    { "九", "九日,名詞,副詞可能,*,*,*,*,九日,ココノカ,ココノカ,0/4,*" },
                    { "十", "十日,名詞,副詞可能,*,*,*,*,十日,トウカ,トーカ,0/3,*" },
                }
            ),
            // 日間: 一日間〜十日間の特殊読み
            new ConvEntry<string>(
                new HashSet<string> { "日間" },
                new Dictionary<string, string>
                {
                    { "一", "一日間,名詞,副詞可能,*,*,*,*,一日間,イチニチカン,イチニチカン,4/6,*" },
                    { "二", "二日間,名詞,副詞可能,*,*,*,*,二日,フツカカン,フツカカン,3/5,*" },
                    { "三", "三日間,名詞,副詞可能,*,*,*,*,三日,ミッカカン,ミッカカン,3/5,*" },
                    { "四", "四日間,名詞,副詞可能,*,*,*,*,四日,ヨッカカン,ヨッカカン,3/5,*" },
                    { "五", "五日間,名詞,副詞可能,*,*,*,*,五日,イツカカン,イツカカン,3/5,*" },
                    { "六", "六日間,名詞,副詞可能,*,*,*,*,六日,ムイカカン,ムイカカン,3/5,*" },
                    { "七", "七日間,名詞,副詞可能,*,*,*,*,七日,ナノカカン,ナノカカン,3/5,*" },
                    { "八", "八日間,名詞,副詞可能,*,*,*,*,八日,ヨウカカン,ヨーカカン,3/5,*" },
                    { "九", "九日間,名詞,副詞可能,*,*,*,*,九日,ココノカカン,ココノカカン,4/6,*" },
                    { "十", "十日間,名詞,副詞可能,*,*,*,*,十日,トウカカン,トーカカン,3/5,*" },
                }
            ),
        };

        // =====================================================================
        // 検索ヘルパーメソッド
        // =====================================================================

        /// <summary>
        /// ConvEntry配列から、key1がセットに含まれ、key2が辞書に含まれる場合に値を返す。
        /// jpreprocess の find_pron_conv_set に対応。
        /// </summary>
        public static T? FindConvSet<T>(ConvEntry<T>[] table, string key1, string key2) where T : class
        {
            for (int i = 0; i < table.Length; i++)
            {
                if (table[i].Keys.Contains(key1) && table[i].Values.TryGetValue(key2, out var value))
                {
                    return value;
                }
            }
            return null;
        }

        /// <summary>
        /// ConvEntry配列から値型を検索する。見つかった場合 true を返す。
        /// </summary>
        public static bool TryFindConvSet<T>(ConvEntry<T>[] table, string key1, string key2, out T value) where T : struct
        {
            for (int i = 0; i < table.Length; i++)
            {
                if (table[i].Keys.Contains(key1) && table[i].Values.TryGetValue(key2, out value))
                {
                    return true;
                }
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Class3Entry配列から、助数詞の表層形と読みの組み合わせで検索する。
        /// jpreprocess の find_pron_conv_map に対応。
        /// </summary>
        public static Pronunciation? FindConvMap(Class3Entry[] table, string numerativeSurface, string numerativeReading, string digitKey)
        {
            for (int i = 0; i < table.Length; i++)
            {
                if (table[i].Keys.TryGetValue(numerativeSurface, out var readings))
                {
                    bool found = false;
                    for (int j = 0; j < readings.Length; j++)
                    {
                        if (readings[j] == numerativeReading)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found && table[i].Values.TryGetValue(digitKey, out var pron))
                    {
                        return pron;
                    }
                }
            }
            return null;
        }

        // =====================================================================
        // 変換テーブルエントリ型
        // =====================================================================

        /// <summary>
        /// 変換テーブルの1エントリ。キーのセットと値の辞書のペア。
        /// Rust の (Keys, Map) タプルに対応。
        /// </summary>
        public sealed class ConvEntry<T>
        {
            public HashSet<string> Keys { get; }
            public Dictionary<string, T> Values { get; }

            public ConvEntry(HashSet<string> keys, Dictionary<string, T> values)
            {
                Keys = keys;
                Values = values;
            }
        }

        /// <summary>
        /// 助数詞クラス3のエントリ。助数詞→読みリストのマップと、数字→発音の辞書。
        /// Rust の (Class3Keys, DigitLUT) タプルに対応。
        /// </summary>
        public sealed class Class3Entry
        {
            public Dictionary<string, string[]> Keys { get; }
            public Dictionary<string, Pronunciation> Values { get; }

            public Class3Entry(Dictionary<string, string[]> keys, Dictionary<string, Pronunciation> values)
            {
                Keys = keys;
                Values = values;
            }
        }
    }
}
