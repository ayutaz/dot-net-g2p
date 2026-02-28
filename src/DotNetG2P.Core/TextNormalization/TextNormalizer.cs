using System.Collections.Generic;
using System.Text;

namespace DotNetG2P.TextNormalization
{
    /// <summary>
    /// naist-jdic辞書向けテキスト正規化処理。
    /// jpreprocessのnormalize_text.rsに準拠した実装。
    ///
    /// 処理内容:
    /// 1. 半角ASCII（0x21〜0x7E）を全角に変換（naist-jdic辞書が全角前提のため）
    /// 2. 半角カタカナ・半角記号を全角に変換
    /// 3. 特殊記号の正規化（バックスラッシュ→￥、ハイフン→マイナス、チルダ→波ダッシュ等）
    /// 4. 濁点・半濁点の結合（「カ゛」→「ガ」）
    /// </summary>
    public static class TextNormalizer
    {
        /// <summary>
        /// naist-jdic辞書向けにテキストを正規化する。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>正規化済みテキスト</returns>
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text ?? string.Empty;

            var sb = new StringBuilder(text.Length);
            // 前の文字を保持（濁点・半濁点結合のため）
            char? prev = null;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // まず半角→全角マッピング適用
                char mapped;
                if (s_halfwidthMap.TryGetValue(c, out char hw))
                {
                    mapped = hw;
                }
                else if (c > '\u0020' && c < '\u007F')
                {
                    // 半角ASCII（0x21〜0x7E）を全角に変換（+0xFEE0）
                    mapped = (char)(c + 0xFEE0);
                }
                else
                {
                    mapped = c;
                }

                // 濁点・半濁点の判定
                bool isSemivoiced = IsSemivoicedMark(mapped);
                bool isVoiced = IsVoicedMark(mapped);

                if (isSemivoiced)
                {
                    // 半濁点: 前の文字と結合可能か試みる
                    if (prev.HasValue && s_semivoicedMap.TryGetValue(prev.Value, out char combined))
                    {
                        sb.Append(combined);
                    }
                    else if (prev.HasValue)
                    {
                        // 結合不可: 前の文字だけ出力（濁点は捨てる）
                        sb.Append(prev.Value);
                    }
                    // else: 先頭の濁点マークは捨てる
                    prev = null;
                }
                else if (isVoiced)
                {
                    // 濁点: 前の文字と結合可能か試みる
                    if (prev.HasValue && s_voicedMap.TryGetValue(prev.Value, out char combined))
                    {
                        sb.Append(combined);
                    }
                    else if (prev.HasValue)
                    {
                        // 結合不可: 前の文字だけ出力（濁点は捨てる）
                        sb.Append(prev.Value);
                    }
                    // else: 先頭の濁点マークは捨てる
                    prev = null;
                }
                else
                {
                    // 通常文字: 前の文字をフラッシュして現在の文字を保持
                    if (prev.HasValue)
                    {
                        sb.Append(prev.Value);
                    }
                    prev = mapped;
                }
            }

            // 最後の文字をフラッシュ
            if (prev.HasValue)
            {
                sb.Append(prev.Value);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 濁点マーク判定
        /// </summary>
        private static bool IsVoicedMark(char c)
        {
            return c == '\u3099'   // U+3099 Combining Katakana-Hiragana Voiced Sound Mark
                || c == '\u309B'   // U+309B Katakana-Hiragana Voiced Sound Mark
                || c == '\uFF9E';  // U+FF9E Halfwidth Katakana Voiced Sound Mark
        }

        /// <summary>
        /// 半濁点マーク判定
        /// </summary>
        private static bool IsSemivoicedMark(char c)
        {
            return c == '\u309A'   // U+309A Combining Katakana-Hiragana Semi-Voiced Sound Mark
                || c == '\u309C'   // U+309C Katakana-Hiragana Semi-Voiced Sound Mark
                || c == '\uFF9F';  // U+FF9F Halfwidth Katakana Semi-Voiced Sound Mark
        }

        // ----- 半角→全角変換テーブル -----
        // 特殊なマッピングが必要な文字のみ定義。
        // それ以外の半角ASCII(0x21〜0x7E)は+0xFEE0で一括変換。
        private static readonly Dictionary<char, char> s_halfwidthMap = new Dictionary<char, char>
        {
            // --- 記号の特殊マッピング ---
            { ' ', '\u3000' },        // 半角スペース → 全角スペース
            { '\u00A5', '\uFFE5' },   // ¥ (U+00A5) → ￥ (U+FFE5)
            { '\\', '\uFFE5' },       // バックスラッシュ → ￥ (U+FFE5)
            { '-', '\u2212' },        // ハイフン → − (U+2212 MINUS SIGN)
            { '~', '\u301C' },        // チルダ → 〜 (U+301C WAVE DASH)
            { '`', '\u2018' },        // バッククォート → ' (U+2018 LEFT SINGLE QUOTATION MARK)
            { '"', '\u201D' },        // ダブルクォート → " (U+201D RIGHT DOUBLE QUOTATION MARK)
            { '\'', '\u2019' },       // シングルクォート → ' (U+2019 RIGHT SINGLE QUOTATION MARK)

            // --- 半角カタカナ記号 → 全角 ---
            { '\uFF61', '\u3002' },   // ｡ → 。
            { '\uFF62', '\u300C' },   // ｢ → 「
            { '\uFF63', '\u300D' },   // ｣ → 」
            { '\uFF64', '\u3001' },   // ､ → 、
            { '\uFF65', '\u30FB' },   // ･ → ・

            // --- 半角カタカナ → 全角カタカナ ---
            { '\uFF66', '\u30F2' },   // ｦ → ヲ
            { '\uFF67', '\u30A1' },   // ｧ → ァ
            { '\uFF68', '\u30A3' },   // ｨ → ィ
            { '\uFF69', '\u30A5' },   // ｩ → ゥ
            { '\uFF6A', '\u30A7' },   // ｪ → ェ
            { '\uFF6B', '\u30A9' },   // ｫ → ォ
            { '\uFF6C', '\u30E3' },   // ｬ → ャ
            { '\uFF6D', '\u30E5' },   // ｭ → ュ
            { '\uFF6E', '\u30E7' },   // ｮ → ョ
            { '\uFF6F', '\u30C3' },   // ｯ → ッ
            { '\uFF70', '\u30FC' },   // ｰ → ー
            { '\uFF71', '\u30A2' },   // ｱ → ア
            { '\uFF72', '\u30A4' },   // ｲ → イ
            { '\uFF73', '\u30A6' },   // ｳ → ウ
            { '\uFF74', '\u30A8' },   // ｴ → エ
            { '\uFF75', '\u30AA' },   // ｵ → オ
            { '\uFF76', '\u30AB' },   // ｶ → カ
            { '\uFF77', '\u30AD' },   // ｷ → キ
            { '\uFF78', '\u30AF' },   // ｸ → ク
            { '\uFF79', '\u30B1' },   // ｹ → ケ
            { '\uFF7A', '\u30B3' },   // ｺ → コ
            { '\uFF7B', '\u30B5' },   // ｻ → サ
            { '\uFF7C', '\u30B7' },   // ｼ → シ
            { '\uFF7D', '\u30B9' },   // ｽ → ス
            { '\uFF7E', '\u30BB' },   // ｾ → セ
            { '\uFF7F', '\u30BD' },   // ｿ → ソ
            { '\uFF80', '\u30BF' },   // ﾀ → タ
            { '\uFF81', '\u30C1' },   // ﾁ → チ
            { '\uFF82', '\u30C4' },   // ﾂ → ツ
            { '\uFF83', '\u30C6' },   // ﾃ → テ
            { '\uFF84', '\u30C8' },   // ﾄ → ト
            { '\uFF85', '\u30CA' },   // ﾅ → ナ
            { '\uFF86', '\u30CB' },   // ﾆ → ニ
            { '\uFF87', '\u30CC' },   // ﾇ → ヌ
            { '\uFF88', '\u30CD' },   // ﾈ → ネ
            { '\uFF89', '\u30CE' },   // ﾉ → ノ
            { '\uFF8A', '\u30CF' },   // ﾊ → ハ
            { '\uFF8B', '\u30D2' },   // ﾋ → ヒ
            { '\uFF8C', '\u30D5' },   // ﾌ → フ
            { '\uFF8D', '\u30D8' },   // ﾍ → ヘ
            { '\uFF8E', '\u30DB' },   // ﾎ → ホ
            { '\uFF8F', '\u30DE' },   // ﾏ → マ
            { '\uFF90', '\u30DF' },   // ﾐ → ミ
            { '\uFF91', '\u30E0' },   // ﾑ → ム
            { '\uFF92', '\u30E1' },   // ﾒ → メ
            { '\uFF93', '\u30E2' },   // ﾓ → モ
            { '\uFF94', '\u30E4' },   // ﾔ → ヤ
            { '\uFF95', '\u30E6' },   // ﾕ → ユ
            { '\uFF96', '\u30E8' },   // ﾖ → ヨ
            { '\uFF97', '\u30E9' },   // ﾗ → ラ
            { '\uFF98', '\u30EA' },   // ﾘ → リ
            { '\uFF99', '\u30EB' },   // ﾙ → ル
            { '\uFF9A', '\u30EC' },   // ﾚ → レ
            { '\uFF9B', '\u30ED' },   // ﾛ → ロ
            { '\uFF9C', '\u30EF' },   // ﾜ → ワ
            { '\uFF9D', '\u30F3' },   // ﾝ → ン
        };

        // ----- 濁点結合テーブル（カタカナ・ひらがな） -----
        private static readonly Dictionary<char, char> s_voicedMap = new Dictionary<char, char>
        {
            // カタカナ
            { '\u30AB', '\u30AC' },   // カ → ガ
            { '\u30AD', '\u30AE' },   // キ → ギ
            { '\u30AF', '\u30B0' },   // ク → グ
            { '\u30B1', '\u30B2' },   // ケ → ゲ
            { '\u30B3', '\u30B4' },   // コ → ゴ
            { '\u30B5', '\u30B6' },   // サ → ザ
            { '\u30B7', '\u30B8' },   // シ → ジ
            { '\u30B9', '\u30BA' },   // ス → ズ
            { '\u30BB', '\u30BC' },   // セ → ゼ
            { '\u30BD', '\u30BE' },   // ソ → ゾ
            { '\u30BF', '\u30C0' },   // タ → ダ
            { '\u30C1', '\u30C2' },   // チ → ヂ
            { '\u30C4', '\u30C5' },   // ツ → ヅ
            { '\u30C6', '\u30C7' },   // テ → デ
            { '\u30C8', '\u30C9' },   // ト → ド
            { '\u30CF', '\u30D0' },   // ハ → バ
            { '\u30D2', '\u30D3' },   // ヒ → ビ
            { '\u30D5', '\u30D6' },   // フ → ブ
            { '\u30D8', '\u30D9' },   // ヘ → ベ
            { '\u30DB', '\u30DC' },   // ホ → ボ
            { '\u30A6', '\u30F4' },   // ウ → ヴ
            { '\u30EF', '\u30F7' },   // ワ → ヷ
            { '\u30F0', '\u30F8' },   // ヰ → ヸ
            { '\u30F1', '\u30F9' },   // ヱ → ヹ
            { '\u30F2', '\u30FA' },   // ヲ → ヺ
            { '\u30FD', '\u30FE' },   // ヽ → ヾ
            // ひらがな
            { '\u304B', '\u304C' },   // か → が
            { '\u304D', '\u304E' },   // き → ぎ
            { '\u304F', '\u3050' },   // く → ぐ
            { '\u3051', '\u3052' },   // け → げ
            { '\u3053', '\u3054' },   // こ → ご
            { '\u3055', '\u3056' },   // さ → ざ
            { '\u3057', '\u3058' },   // し → じ
            { '\u3059', '\u305A' },   // す → ず
            { '\u305B', '\u305C' },   // せ → ぜ
            { '\u305D', '\u305E' },   // そ → ぞ
            { '\u305F', '\u3060' },   // た → だ
            { '\u3061', '\u3062' },   // ち → ぢ
            { '\u3064', '\u3065' },   // つ → づ
            { '\u3066', '\u3067' },   // て → で
            { '\u3068', '\u3069' },   // と → ど
            { '\u306F', '\u3070' },   // は → ば
            { '\u3072', '\u3073' },   // ひ → び
            { '\u3075', '\u3076' },   // ふ → ぶ
            { '\u3078', '\u3079' },   // へ → べ
            { '\u307B', '\u307C' },   // ほ → ぼ
            { '\u3046', '\u3094' },   // う → ゔ
        };

        // ----- 半濁点結合テーブル（カタカナ・ひらがな） -----
        private static readonly Dictionary<char, char> s_semivoicedMap = new Dictionary<char, char>
        {
            // カタカナ
            { '\u30CF', '\u30D1' },   // ハ → パ
            { '\u30D2', '\u30D4' },   // ヒ → ピ
            { '\u30D5', '\u30D7' },   // フ → プ
            { '\u30D8', '\u30DA' },   // ヘ → ペ
            { '\u30DB', '\u30DD' },   // ホ → ポ
            // ひらがな
            { '\u306F', '\u3071' },   // は → ぱ
            { '\u3072', '\u3074' },   // ひ → ぴ
            { '\u3075', '\u3077' },   // ふ → ぷ
            { '\u3078', '\u307A' },   // へ → ぺ
            { '\u307B', '\u307D' },   // ほ → ぽ
        };
    }
}
