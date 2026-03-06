#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetG2P.Multilingual
{
    /// <summary>
    /// 日英混在テキストを言語タグ付きセグメントに分割する。
    /// </summary>
    public static class TextSegmenter
    {
        /// <summary>テキストを言語セグメントに分割する。</summary>
        public static IReadOnlyList<TextSegment> Segment(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<TextSegment>();

            // 1パス目: 各文字のScriptKindを分類
            Span<ScriptKind> kinds = text.Length <= 256
                ? stackalloc ScriptKind[text.Length]
                : new ScriptKind[text.Length];
            for (int i = 0; i < text.Length; i++)
                kinds[i] = LanguageDetector.Classify(text[i]);

            // 2パス目: 各文字に確定した言語を割り当てる
            var languages = new Language?[text.Length];

            // まず、言語確定文字(Japanese/English)を直接割り当て
            // アポストロフィとハイフンは英語文字間では英語に含める
            for (int i = 0; i < text.Length; i++)
            {
                var kind = kinds[i];
                if (kind == ScriptKind.Japanese || kind == ScriptKind.English || kind == ScriptKind.Latin)
                {
                    languages[i] = LanguageDetector.ToLanguage(kind);
                }
            }

            // アポストロフィ・ハイフン処理: 前後が英語なら英語に含める
            for (int i = 0; i < text.Length; i++)
            {
                if (kinds[i] == ScriptKind.Punctuation && (text[i] == '\'' || text[i] == '-'))
                {
                    var prevLang = FindPrevLanguage(languages, i);
                    var nextLang = FindNextLanguage(languages, i);
                    if (prevLang == Language.English && nextLang == Language.English)
                    {
                        languages[i] = Language.English;
                    }
                }
            }

            // 数字処理: 隣接する言語セグメントに吸収
            for (int i = 0; i < text.Length; i++)
            {
                if (kinds[i] == ScriptKind.Digit)
                {
                    // 前方を探す
                    var prevLang = FindPrevLanguage(languages, i);
                    // 後方を探す
                    var nextLang = FindNextLanguage(languages, i);

                    if (prevLang != null)
                        languages[i] = prevLang;
                    else if (nextLang != null)
                        languages[i] = nextLang;
                    else
                        languages[i] = Language.Japanese; // デフォルト
                }
            }

            // 空白処理
            for (int i = 0; i < text.Length; i++)
            {
                if (kinds[i] == ScriptKind.Whitespace)
                {
                    // 前後の確定済み言語を探す
                    var prevLang = FindPrevLanguage(languages, i);
                    var nextLang = FindNextLanguage(languages, i);

                    if (prevLang != null && nextLang != null && prevLang == nextLang)
                    {
                        // 同一言語間の空白はその言語に含める
                        languages[i] = prevLang;
                    }
                    else if (prevLang != null)
                    {
                        // 言語境界の空白は前のセグメントに付属
                        languages[i] = prevLang;
                    }
                    else if (nextLang != null)
                    {
                        languages[i] = nextLang;
                    }
                    // else: 前後どちらも言語なし → 未確定のまま（後で処理）
                }
            }

            // 句読点・記号・Other処理: 直前のセグメント言語に付属、先頭なら直後に付属
            for (int i = 0; i < text.Length; i++)
            {
                if (languages[i] != null) continue;

                var prevLang = FindPrevLanguage(languages, i);
                if (prevLang != null)
                {
                    languages[i] = prevLang;
                }
                else
                {
                    var nextLang = FindNextLanguage(languages, i);
                    if (nextLang != null)
                        languages[i] = nextLang;
                    else
                        languages[i] = Language.Japanese; // 全て記号等のみの場合のデフォルト
                }
            }

            // 空白のみの入力チェック（全文字がWhitespaceで前後に言語なし）
            bool allWhitespace = true;
            for (int i = 0; i < text.Length; i++)
            {
                if (kinds[i] != ScriptKind.Whitespace)
                {
                    allWhitespace = false;
                    break;
                }
            }
            if (allWhitespace)
                return new List<TextSegment>();

            // セグメント構築: 同一言語の連続文字をグループ化
            var result = new List<TextSegment>();
            var sb = new StringBuilder();
            Language currentLang = languages[0]!.Value;
            sb.Append(text[0]);

            for (int i = 1; i < text.Length; i++)
            {
                Language lang = languages[i]!.Value;
                if (lang == currentLang)
                {
                    sb.Append(text[i]);
                }
                else
                {
                    result.Add(new TextSegment(sb.ToString(), currentLang));
                    sb.Clear();
                    sb.Append(text[i]);
                    currentLang = lang;
                }
            }

            if (sb.Length > 0)
                result.Add(new TextSegment(sb.ToString(), currentLang));

            return result;
        }

        /// <summary>指定位置より前で最初に見つかる確定済み言語を返す。</summary>
        private static Language? FindPrevLanguage(Language?[] languages, int index)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                if (languages[i] != null)
                    return languages[i];
            }
            return null;
        }

        /// <summary>指定位置より後で最初に見つかる確定済み言語を返す。</summary>
        private static Language? FindNextLanguage(Language?[] languages, int index)
        {
            for (int i = index + 1; i < languages.Length; i++)
            {
                if (languages[i] != null)
                    return languages[i];
            }
            return null;
        }
    }
}
