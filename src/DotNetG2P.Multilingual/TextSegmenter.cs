#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace DotNetG2P.Multilingual
{
    /// <summary>
    /// 日英混在テキストを言語タグ付きセグメントに分割する。
    /// </summary>
    public static class TextSegmenter
    {
        // byte エンコーディング定数（Language? の代替）
        private const byte LangNone = 0;
        private const byte LangJapanese = 1;  // Language.Japanese
        private const byte LangEnglish = 2;   // Language.English
        private const byte LangChinese = 3;   // Language.Chinese
        private const byte LangSpanish = 4;   // Language.Spanish

        /// <summary>テキストを言語セグメントに分割する（後方互換: CJK漢字はJapanese扱い）。</summary>
        public static IReadOnlyList<TextSegment> Segment(string text)
        {
            return Segment(text, Language.Japanese, Language.English);
        }

        /// <summary>テキストを言語セグメントに分割する。</summary>
        /// <param name="text">入力テキスト</param>
        /// <param name="defaultCjkLanguage">CJK漢字のデフォルト言語（周囲にかな文字がない場合に使用）</param>
        public static IReadOnlyList<TextSegment> Segment(string text, Language defaultCjkLanguage)
        {
            return Segment(text, defaultCjkLanguage, Language.English);
        }

        /// <summary>テキストを言語セグメントに分割する。</summary>
        /// <param name="text">入力テキスト</param>
        /// <param name="defaultCjkLanguage">CJK漢字のデフォルト言語（周囲にかな文字がない場合に使用）</param>
        /// <param name="defaultLatinLanguage">ラテン文字列のデフォルト言語</param>
        public static IReadOnlyList<TextSegment> Segment(string text, Language defaultCjkLanguage, Language defaultLatinLanguage)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<TextSegment>();

            if (defaultLatinLanguage != Language.English && defaultLatinLanguage != Language.Spanish)
                throw new ArgumentOutOfRangeException(nameof(defaultLatinLanguage), "DefaultLatinLanguage must be English or Spanish.");

            int len = text.Length;

            // 1パス目: 各文字のScriptKindを分類（サロゲートペア考慮）
            Span<ScriptKind> kinds = len <= 256
                ? stackalloc ScriptKind[len]
                : new ScriptKind[len];
            for (int i = 0; i < len;)
            {
                var kind = LanguageDetector.Classify(text, i, out int charCount);
                kinds[i] = kind;
                if (charCount == 2)
                {
                    // サロゲートペアの後半も同じScriptKindを割り当て
                    kinds[i + 1] = kind;
                }
                i += charCount;
            }

            // 2パス目: 各文字に確定した言語を割り当てる（byte配列で管理）
            byte[]? langRented = null;
            Span<byte> languages = len <= 256
                ? stackalloc byte[256]
                : (langRented = ArrayPool<byte>.Shared.Rent(len));
            languages = languages.Slice(0, len);
            languages.Clear(); // 0 = LangNone

            try
            {
                // defaultCjkLanguageに対応するbyte値
                byte defaultCjkByte = defaultCjkLanguage == Language.Chinese ? LangChinese
                                    : defaultCjkLanguage == Language.English ? LangEnglish
                                    : LangJapanese;
                byte defaultLatinByte = defaultLatinLanguage == Language.Spanish ? LangSpanish : LangEnglish;

                // まず、日本語確定文字を直接割り当てる。
                for (int i = 0; i < len; i++)
                {
                    var kind = kinds[i];
                    if (kind == ScriptKind.Japanese)
                    {
                        languages[i] = LangJapanese;
                    }
                }

                // 次に、連続するラテン文字列を語単位で English / Spanish に振り分ける。
                for (int i = 0; i < len;)
                {
                    if (!IsLatinScript(kinds[i]))
                    {
                        i++;
                        continue;
                    }

                    int start = i;
                    bool hasLatinExtended = false;
                    while (i < len && IsLatinScript(kinds[i]))
                    {
                        if (kinds[i] == ScriptKind.Latin)
                            hasLatinExtended = true;
                        i++;
                    }

                    byte latinLanguage = ResolveLatinLanguage(text, start, i - start, defaultLatinByte, hasLatinExtended);
                    for (int j = start; j < i; j++)
                        languages[j] = latinLanguage;
                }

                // CJKIdeograph文字の言語割り当て:
                // 前後にかな文字（Japanese確定）がある → Japanese
                // 周囲にかな文字がない → defaultCjkLanguage
                {
                    // 前方パス: 最寄りのJapanese確定文字（かな由来）を探す
                    byte lastKana = LangNone;
                    for (int i = 0; i < len; i++)
                    {
                        if (kinds[i] == ScriptKind.Japanese) lastKana = LangJapanese;
                        if (kinds[i] == ScriptKind.CJKIdeograph)
                        {
                            if (lastKana == LangJapanese)
                                languages[i] = LangJapanese;
                        }
                        // 英語等の確定文字でかなチェーンをリセット
                        if (IsLatinScript(kinds[i]))
                            lastKana = LangNone;
                    }

                    // 後方パス: 後方にかな文字がある場合もJapanese
                    lastKana = LangNone;
                    for (int i = len - 1; i >= 0; i--)
                    {
                        if (kinds[i] == ScriptKind.Japanese) lastKana = LangJapanese;
                        if (kinds[i] == ScriptKind.CJKIdeograph && languages[i] == LangNone)
                        {
                            if (lastKana == LangJapanese)
                                languages[i] = LangJapanese;
                        }
                        if (IsLatinScript(kinds[i]))
                            lastKana = LangNone;
                    }

                    // 残りのCJKIdeograph（前後にかながない）にデフォルト言語を割り当て
                    for (int i = 0; i < len; i++)
                    {
                        if (kinds[i] == ScriptKind.CJKIdeograph && languages[i] == LangNone)
                        {
                            languages[i] = defaultCjkByte;
                        }
                    }
                }

                // 前方・後方の最寄り言語を事前計算（O(n)、2パス）
                byte[]? prevRented = null;
                Span<byte> prevLangs = len <= 256
                    ? stackalloc byte[256]
                    : (prevRented = ArrayPool<byte>.Shared.Rent(len));
                prevLangs = prevLangs.Slice(0, len);

                byte[]? nextRented = null;
                Span<byte> nextLangs = len <= 256
                    ? stackalloc byte[256]
                    : (nextRented = ArrayPool<byte>.Shared.Rent(len));
                nextLangs = nextLangs.Slice(0, len);

                try
                {
                    // 前方パス: 各位置の「最も近い前方の確定言語」
                    byte lastLang = LangNone;
                    for (int i = 0; i < len; i++)
                    {
                        if (languages[i] != LangNone) lastLang = languages[i];
                        prevLangs[i] = lastLang;
                    }

                    // 後方パス: 各位置の「最も近い後方の確定言語」
                    lastLang = LangNone;
                    for (int i = len - 1; i >= 0; i--)
                    {
                        if (languages[i] != LangNone) lastLang = languages[i];
                        nextLangs[i] = lastLang;
                    }

                    // アポストロフィ・ハイフン処理: 前後が同一ラテン言語ならそのセグメントに含める
                    for (int i = 0; i < len; i++)
                    {
                        if (kinds[i] == ScriptKind.Punctuation && (text[i] == '\'' || text[i] == '-'))
                        {
                            byte prev = prevLangs[i];
                            byte next = nextLangs[i];
                            if (prev == next && IsLatinLanguage(prev))
                            {
                                languages[i] = prev;
                            }
                        }
                    }

                    // アポストロフィ/ハイフンの割り当て後、prevLangs/nextLangsを再計算
                    // （後続処理でアポストロフィが英語に含まれた結果を反映するため）
                    lastLang = LangNone;
                    for (int i = 0; i < len; i++)
                    {
                        if (languages[i] != LangNone) lastLang = languages[i];
                        prevLangs[i] = lastLang;
                    }
                    lastLang = LangNone;
                    for (int i = len - 1; i >= 0; i--)
                    {
                        if (languages[i] != LangNone) lastLang = languages[i];
                        nextLangs[i] = lastLang;
                    }

                    // 数字処理: 隣接する言語セグメントに吸収
                    for (int i = 0; i < len; i++)
                    {
                        if (kinds[i] == ScriptKind.Digit)
                        {
                            byte prev = prevLangs[i];
                            byte next = nextLangs[i];

                            if (prev != LangNone)
                                languages[i] = prev;
                            else if (next != LangNone)
                                languages[i] = next;
                            else if (defaultLatinByte == LangSpanish)
                                languages[i] = LangSpanish;
                            else
                                languages[i] = LangJapanese; // デフォルト
                        }
                    }

                    // 数字割り当て後、prevLangs/nextLangsを再計算
                    lastLang = LangNone;
                    for (int i = 0; i < len; i++)
                    {
                        if (languages[i] != LangNone) lastLang = languages[i];
                        prevLangs[i] = lastLang;
                    }
                    lastLang = LangNone;
                    for (int i = len - 1; i >= 0; i--)
                    {
                        if (languages[i] != LangNone) lastLang = languages[i];
                        nextLangs[i] = lastLang;
                    }

                    // 空白処理
                    for (int i = 0; i < len; i++)
                    {
                        if (kinds[i] == ScriptKind.Whitespace)
                        {
                            byte prev = prevLangs[i];
                            byte next = nextLangs[i];

                            if (prev != LangNone && next != LangNone && prev == next)
                            {
                                // 同一言語間の空白はその言語に含める
                                languages[i] = prev;
                            }
                            else if (prev != LangNone)
                            {
                                // 言語境界の空白は前のセグメントに付属
                                languages[i] = prev;
                            }
                            else if (next != LangNone)
                            {
                                languages[i] = next;
                            }
                            // else: 前後どちらも言語なし → 未確定のまま（後で処理）
                        }
                    }

                    // 空白割り当て後、prevLangs/nextLangsを再計算
                    lastLang = LangNone;
                    for (int i = 0; i < len; i++)
                    {
                        if (languages[i] != LangNone) lastLang = languages[i];
                        prevLangs[i] = lastLang;
                    }
                    lastLang = LangNone;
                    for (int i = len - 1; i >= 0; i--)
                    {
                        if (languages[i] != LangNone) lastLang = languages[i];
                        nextLangs[i] = lastLang;
                    }

                    // 句読点・記号・Other処理: 直前のセグメント言語に付属、先頭なら直後に付属
                    for (int i = 0; i < len; i++)
                    {
                        if (languages[i] != LangNone) continue;

                        byte prev = prevLangs[i];
                        if (prev != LangNone)
                        {
                            languages[i] = prev;
                        }
                        else
                        {
                            byte next = nextLangs[i];
                            if (next != LangNone)
                                languages[i] = next;
                            else
                                languages[i] = LangJapanese; // 全て記号等のみの場合のデフォルト
                        }
                    }
                }
                finally
                {
                    if (prevRented != null) ArrayPool<byte>.Shared.Return(prevRented);
                    if (nextRented != null) ArrayPool<byte>.Shared.Return(nextRented);
                }

                // 空白のみの入力チェック（全文字がWhitespaceで前後に言語なし）
                bool allWhitespace = true;
                for (int i = 0; i < len; i++)
                {
                    if (kinds[i] != ScriptKind.Whitespace)
                    {
                        allWhitespace = false;
                        break;
                    }
                }
                if (allWhitespace)
                    return Array.Empty<TextSegment>();

                // セグメント構築: 同一言語の連続文字をグループ化
                var result = new List<TextSegment>();
                var sb = new StringBuilder();
                byte currentLangByte = languages[0];
                sb.Append(text[0]);

                for (int i = 1; i < len; i++)
                {
                    byte langByte = languages[i];
                    if (langByte == currentLangByte)
                    {
                        sb.Append(text[i]);
                    }
                    else
                    {
                        result.Add(new TextSegment(sb.ToString(), FromLangByte(currentLangByte)));
                        sb.Clear();
                        sb.Append(text[i]);
                        currentLangByte = langByte;
                    }
                }

                if (sb.Length > 0)
                    result.Add(new TextSegment(sb.ToString(), FromLangByte(currentLangByte)));

                return result;
            }
            finally
            {
                if (langRented != null) ArrayPool<byte>.Shared.Return(langRented);
            }
        }

        /// <summary>byte → Language enum 変換。</summary>
        private static Language FromLangByte(byte b)
        {
            switch (b)
            {
                case LangJapanese: return Language.Japanese;
                case LangChinese: return Language.Chinese;
                case LangSpanish: return Language.Spanish;
                default: return Language.English;
            }
        }

        private static bool IsLatinScript(ScriptKind kind)
        {
            return kind == ScriptKind.English || kind == ScriptKind.Latin;
        }

        private static bool IsLatinLanguage(byte language)
        {
            return language == LangEnglish || language == LangSpanish;
        }

        private static byte ResolveLatinLanguage(string text, int start, int length, byte defaultLatinByte, bool hasLatinExtended)
        {
            if (defaultLatinByte == LangSpanish)
                return LangSpanish;

            if (!hasLatinExtended)
                return LangEnglish;

            for (int i = start; i < start + length; i++)
            {
                switch (text[i])
                {
                    case '\u00C1':
                    case '\u00C9':
                    case '\u00CD':
                    case '\u00D1':
                    case '\u00D3':
                    case '\u00DA':
                    case '\u00DC':
                    case '\u00E1':
                    case '\u00E9':
                    case '\u00ED':
                    case '\u00F1':
                    case '\u00F3':
                    case '\u00FA':
                    case '\u00FC':
                        return LangSpanish;
                }
            }

            return defaultLatinByte;
        }
    }
}
