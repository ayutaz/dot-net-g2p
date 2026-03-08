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

                // サロゲートペア対応: ハイサロゲートの場合は2つのcharを1文字として扱う
                if (char.IsHighSurrogate(c))
                {
                    // 前の文字をフラッシュ
                    if (prev.HasValue)
                    {
                        sb.Append(prev.Value);
                        prev = null;
                    }
                    // サロゲートペアをそのまま出力
                    sb.Append(c);
                    if (i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                    {
                        sb.Append(text[i + 1]);
                        i++; // ローサロゲートをスキップ
                    }
                    continue;
                }

                // まず半角→全角マッピング適用
                char mapped;
                if (c >= HalfKatakanaStart && c <= HalfKatakanaEnd)
                {
                    // 半角カタカナ・記号: 配列インデックス参照
                    mapped = s_halfKatakanaMap[c - HalfKatakanaStart];
                }
                else
                {
                    char special = MapSpecial(c);
                    if (special != '\0')
                    {
                        mapped = special;
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
                    else
                    {
                        // 結合不可: 前の文字と半濁点をそのまま出力
                        if (prev.HasValue)
                        {
                            sb.Append(prev.Value);
                        }
                        sb.Append(mapped);
                    }
                    prev = null;
                }
                else if (isVoiced)
                {
                    // 濁点: 前の文字と結合可能か試みる
                    if (prev.HasValue && s_voicedMap.TryGetValue(prev.Value, out char combined))
                    {
                        sb.Append(combined);
                    }
                    else
                    {
                        // 結合不可: 前の文字と濁点をそのまま出力
                        if (prev.HasValue)
                        {
                            sb.Append(prev.Value);
                        }
                        sb.Append(mapped);
                    }
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

        // ----- 半角カタカナ・記号→全角変換テーブル (U+FF61〜U+FF9D) -----
        // 配列インデックス参照による高速ルックアップ
        private const char HalfKatakanaStart = '\uFF61';
        private const char HalfKatakanaEnd = '\uFF9D';
        private static readonly char[] s_halfKatakanaMap = new char[HalfKatakanaEnd - HalfKatakanaStart + 1]
        {
            '\u3002',   // FF61: ｡ → 。
            '\u300C',   // FF62: ｢ → 「
            '\u300D',   // FF63: ｣ → 」
            '\u3001',   // FF64: ､ → 、
            '\u30FB',   // FF65: ･ → ・
            '\u30F2',   // FF66: ｦ → ヲ
            '\u30A1',   // FF67: ｧ → ァ
            '\u30A3',   // FF68: ｨ → ィ
            '\u30A5',   // FF69: ｩ → ゥ
            '\u30A7',   // FF6A: ｪ → ェ
            '\u30A9',   // FF6B: ｫ → ォ
            '\u30E3',   // FF6C: ｬ → ャ
            '\u30E5',   // FF6D: ｭ → ュ
            '\u30E7',   // FF6E: ｮ → ョ
            '\u30C3',   // FF6F: ｯ → ッ
            '\u30FC',   // FF70: ｰ → ー
            '\u30A2',   // FF71: ｱ → ア
            '\u30A4',   // FF72: ｲ → イ
            '\u30A6',   // FF73: ｳ → ウ
            '\u30A8',   // FF74: ｴ → エ
            '\u30AA',   // FF75: ｵ → オ
            '\u30AB',   // FF76: ｶ → カ
            '\u30AD',   // FF77: ｷ → キ
            '\u30AF',   // FF78: ｸ → ク
            '\u30B1',   // FF79: ｹ → ケ
            '\u30B3',   // FF7A: ｺ → コ
            '\u30B5',   // FF7B: ｻ → サ
            '\u30B7',   // FF7C: ｼ → シ
            '\u30B9',   // FF7D: ｽ → ス
            '\u30BB',   // FF7E: ｾ → セ
            '\u30BD',   // FF7F: ｿ → ソ
            '\u30BF',   // FF80: ﾀ → タ
            '\u30C1',   // FF81: ﾁ → チ
            '\u30C4',   // FF82: ﾂ → ツ
            '\u30C6',   // FF83: ﾃ → テ
            '\u30C8',   // FF84: ﾄ → ト
            '\u30CA',   // FF85: ﾅ → ナ
            '\u30CB',   // FF86: ﾆ → ニ
            '\u30CC',   // FF87: ﾇ → ヌ
            '\u30CD',   // FF88: ﾈ → ネ
            '\u30CE',   // FF89: ﾉ → ノ
            '\u30CF',   // FF8A: ﾊ → ハ
            '\u30D2',   // FF8B: ﾋ → ヒ
            '\u30D5',   // FF8C: ﾌ → フ
            '\u30D8',   // FF8D: ﾍ → ヘ
            '\u30DB',   // FF8E: ﾎ → ホ
            '\u30DE',   // FF8F: ﾏ → マ
            '\u30DF',   // FF90: ﾐ → ミ
            '\u30E0',   // FF91: ﾑ → ム
            '\u30E1',   // FF92: ﾒ → メ
            '\u30E2',   // FF93: ﾓ → モ
            '\u30E4',   // FF94: ﾔ → ヤ
            '\u30E6',   // FF95: ﾕ → ユ
            '\u30E8',   // FF96: ﾖ → ヨ
            '\u30E9',   // FF97: ﾗ → ラ
            '\u30EA',   // FF98: ﾘ → リ
            '\u30EB',   // FF99: ﾙ → ル
            '\u30EC',   // FF9A: ﾚ → レ
            '\u30ED',   // FF9B: ﾛ → ロ
            '\u30EF',   // FF9C: ﾜ → ワ
            '\u30F3',   // FF9D: ﾝ → ン
        };

        // ----- 特殊マッピング（少数の散在する文字） -----
        private static char MapSpecial(char c)
        {
            switch (c)
            {
                case ' ':       return '\u3000';   // 半角スペース → 全角スペース
                case '\u00A5':  return '\uFFE5';   // ¥ → ￥
                case '\\':     return '\uFFE5';   // バックスラッシュ → ￥
                case '-':      return '\u2212';   // ハイフン → −
                case '~':      return '\u301C';   // チルダ → 〜
                case '`':      return '\u2018';   // バッククォート → '
                case '"':      return '\u201D';   // ダブルクォート → \u201D
                case '\'':     return '\u2019';   // シングルクォート → \u2019
                default:       return '\0';       // マッチなし
            }
        }

        // ----- 濁点結合テーブル（カタカナ・ひらがな） -----
        private static readonly Dictionary<char, char> s_voicedMap = new Dictionary<char, char>(52)
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
        private static readonly Dictionary<char, char> s_semivoicedMap = new Dictionary<char, char>(10)
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
