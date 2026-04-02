#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using DotNetG2P.Chinese;

namespace DotNetG2P.Multilingual
{
    /// <summary>
    /// 多言語混在テキストを言語タグ付きセグメントに分割する。
    /// </summary>
    public static class TextSegmenter
    {
        // byte エンコーディング定数（Language? の代替）
        private const byte LangNone = 0;
        private const byte LangJapanese = 1;  // Language.Japanese
        private const byte LangEnglish = 2;   // Language.English
        private const byte LangChinese = 3;   // Language.Chinese
        private const byte LangSpanish = 4;   // Language.Spanish
        private const byte LangFrench = 5;    // Language.French
        private const byte LangPortuguese = 6; // Language.Portuguese
        private const byte LangKorean = 7;    // Language.Korean
        private const byte LangSwedish = 8;   // Language.Swedish

        private static readonly string[] s_frenchWordSignals =
        {
            "alors", "au", "aussi", "autre", "aux", "avec", "bien", "bonjour",
            "bonsoir", "ce", "cette", "comme", "dans", "depuis", "des",
            "donc", "du", "encore", "entre", "et", "faire", "ici", "jamais",
            "je", "le", "les", "leur", "mais", "merci", "monde", "ne",
            "notre", "nous", "parce", "peut", "plus", "pour", "quand",
            "sans", "seulement", "sous", "tout", "toujours", "une",
            "votre", "vous"
        };

        private static readonly string[] s_frenchSuffixSignals =
        {
            "tion", "sion", "ment", "eux", "euse", "euses", "ence", "ance",
            "ique", "iques", "iste", "istes", "aire", "aires",
            "oire", "oires", "able", "ables", "ible", "ibles",
            "eur", "eure", "eures"
        };

        private static readonly string[] s_spanishWordSignals =
        {
            "adios", "amigo", "amigos", "amiga", "amigas", "buenas", "buenos",
            "dias", "gracias", "hasta", "hola", "luego", "mundo", "para",
            "porque", "quiero", "vamos", "gratis", "seguro", "senor", "senora",
            "senorita", "casa", "comida", "familia", "trabajo", "tiempo", "wifi"
        };

        private static readonly string[] s_spanishSuffixSignals =
        {
            "cion", "ciones", "mente", "ando", "iendo", "ados", "adas",
            "ado", "ada", "idos", "idas", "ido", "ida", "ista", "istas",
            "ismo", "ismos", "anza", "anzas", "oso", "osa", "osos", "osas"
        };

        private static readonly string[] s_portugueseWordSignals =
        {
            "obrigado", "obrigada", "muito", "muita", "também", "sempre",
            "agora", "aqui", "hoje", "depois",
            "onde", "quem", "quanto", "qual", "esse", "essa", "isso",
            "isto", "vocês", "nosso", "nossa",
            "senhor", "senhora", "bom", "boa", "tchau", "tudo"
        };

        private static readonly string[] s_portugueseSuffixSignals =
        {
            "ção", "ções", "agem", "agens", "eiro", "eira", "eiros", "eiras",
            "ável", "ível",
            "endo", "indo"
        };

        private static readonly string[] s_englishWordSignals =
        {
            "api", "example", "free", "good", "hello", "known", "night",
            "openai", "test", "today", "tomorrow", "well", "world"
        };

        private static readonly string[] s_swedishWordSignals =
        {
            "att", "dag", "den", "ett", "har", "hej", "hur", "inte",
            "kan", "och", "ska", "tack", "vill"
        };

        private static readonly string[] s_swedishSuffixSignals =
        {
            "ande", "else", "ighet", "lig", "ning", "skap", "tion"
        };

        private static readonly char[] s_chineseStrongMarkers =
        {
            '这', '们', '说', '话', '吗', '边', '门', '电', '车', '书',
            '欢', '谢', '气', '医', '网'
        };

        private static readonly char[] s_chineseWeakMarkers =
        {
            '个', '为', '开', '关', '东', '乐', '习', '飞', '广', '后', '发',
            '经', '听'
        };

        private static readonly char[] s_japaneseMarkers =
        {
            '駅', '円', '気', '込', '働', '畑', '栃', '辻', '峠', '栄', '覚', '団',
            '広', '転', '読', '売', '辺'
        };

        private static readonly string[] s_japaneseWordSignals =
        {
            "東京", "東京都", "大阪", "大阪府", "京都", "北海道", "名古屋", "日本語",
            "株式会社", "新宿", "渋谷", "山手線", "電車", "地下鉄", "改札", "ホーム"
        };

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

            if (defaultCjkLanguage != Language.Japanese && defaultCjkLanguage != Language.Chinese)
                throw new ArgumentOutOfRangeException(nameof(defaultCjkLanguage), "DefaultCjkLanguage must be Japanese or Chinese.");

            if (defaultLatinLanguage != Language.English && defaultLatinLanguage != Language.Spanish && defaultLatinLanguage != Language.French && defaultLatinLanguage != Language.Portuguese && defaultLatinLanguage != Language.Swedish)
                throw new ArgumentOutOfRangeException(nameof(defaultLatinLanguage), "DefaultLatinLanguage must be English, Spanish, French, Portuguese, or Swedish.");

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
                                    : LangJapanese;
                byte defaultLatinByte = defaultLatinLanguage == Language.Spanish ? LangSpanish
                                     : defaultLatinLanguage == Language.French ? LangFrench
                                     : defaultLatinLanguage == Language.Portuguese ? LangPortuguese
                                     : defaultLatinLanguage == Language.Swedish ? LangSwedish
                                     : LangEnglish;

                // まず、日本語・韓国語の確定文字を直接割り当てる。
                for (int i = 0; i < len; i++)
                {
                    var kind = kinds[i];
                    if (kind == ScriptKind.Japanese)
                    {
                        languages[i] = LangJapanese;
                    }
                    else if (kind == ScriptKind.Korean)
                    {
                        languages[i] = LangKorean;
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

                    // 残りのCJKIdeograph（前後にかながない）を連続run単位で解決する
                    for (int i = 0; i < len;)
                    {
                        if (kinds[i] != ScriptKind.CJKIdeograph || languages[i] != LangNone)
                        {
                            i++;
                            continue;
                        }

                        int start = i;
                        while (i < len && kinds[i] == ScriptKind.CJKIdeograph && languages[i] == LangNone)
                            i++;

                        byte resolved = ResolveCjkIdeographLanguage(text, start, i - start, defaultCjkByte);
                        for (int j = start; j < i; j++)
                            languages[j] = resolved;
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
                            else
                                languages[i] = ResolveStandaloneDigitLanguage(text[i], defaultCjkByte, defaultLatinByte);
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
                                languages[i] = ResolveStandaloneNeutralLanguage(kinds[i], text[i], defaultCjkByte, defaultLatinByte);
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
                case LangFrench: return Language.French;
                case LangPortuguese: return Language.Portuguese;
                case LangKorean: return Language.Korean;
                case LangSwedish: return Language.Swedish;
                default: return Language.English;
            }
        }

        private static bool IsLatinScript(ScriptKind kind)
        {
            return kind == ScriptKind.English || kind == ScriptKind.Latin;
        }

        private static bool IsLatinLanguage(byte language)
        {
            return language == LangEnglish || language == LangSpanish || language == LangFrench || language == LangPortuguese || language == LangSwedish;
        }

        private static byte ResolveLatinLanguage(string text, int start, int length, byte defaultLatinByte, bool hasLatinExtended)
        {
            ReadOnlySpan<char> token = text.AsSpan(start, length);

            // ポルトガル語特有文字の検出（ã, õ はスペイン語にもフランス語にもない）
            // フランス語より先に判定: ç は仏葡共通だが ã/õ はポルトガル語固有
            if (ContainsExplicitPortugueseCharacter(token))
                return LangPortuguese;

            // ç + ポルトガル語固有パターン（-ço, -ça 等）はフランス語判定より先にチェック
            if (ContainsPortugueseCedillaPattern(token))
                return LangPortuguese;

            // スウェーデン語特有文字 å (U+00E5) の検出
            // å はスウェーデン語/ノルウェー語/デンマーク語の明確マーカー
            // 現在ノルウェー語・デンマーク語は非サポートのためスウェーデン語に分類
            if (ContainsExplicitSwedishCharacter(token))
                return LangSwedish;

            // フランス語特有文字の検出（スペイン語より先に判定）
            if (ContainsExplicitFrenchCharacter(token))
                return LangFrench;

            if (ContainsExplicitSpanishCharacter(token) || ContainsSpanishDiaeresisPattern(token))
                return LangSpanish;

            // é のみ（á/í/ó/ú/ñ なし）→ フランス語（英語圏での仏語借用語がスペイン語より多い）
            if (ContainsAcuteEOnly(token))
                return LangFrench;

            if (!hasLatinExtended && LooksLikeFrenchAsciiToken(token))
                return LangFrench;

            if (!hasLatinExtended && LooksLikeSpanishAsciiToken(token))
                return LangSpanish;

            if (!hasLatinExtended && LooksLikePortugueseAsciiToken(token))
                return LangPortuguese;

            if (!hasLatinExtended && LooksLikeSwedishAsciiToken(token))
                return LangSwedish;

            return defaultLatinByte;
        }

        private static bool ContainsExplicitSpanishCharacter(ReadOnlySpan<char> token)
        {
            // é/É はフランス語でも高頻度のため、スペイン語専用とはみなさない
            for (int i = 0; i < token.Length; i++)
            {
                switch (token[i])
                {
                    case '\u00C1': // Á
                    case '\u00CD': // Í
                    case '\u00D1': // Ñ
                    case '\u00D3': // Ó
                    case '\u00DA': // Ú
                    case '\u00E1': // á
                    case '\u00ED': // í
                    case '\u00F1': // ñ
                    case '\u00F3': // ó
                    case '\u00FA': // ú
                        return true;
                }
            }

            return false;
        }

        /// <summary>é/É のみを含む（á/í/ó/ú/ñ は含まない）ラテン拡張トークンか判定する。</summary>
        private static bool ContainsAcuteEOnly(ReadOnlySpan<char> token)
        {
            bool hasAcuteE = false;
            for (int i = 0; i < token.Length; i++)
            {
                if (token[i] == '\u00E9' || token[i] == '\u00C9') // é, É
                    hasAcuteE = true;
            }
            return hasAcuteE;
        }

        private static bool ContainsSpanishDiaeresisPattern(ReadOnlySpan<char> token)
        {
            for (int i = 1; i + 1 < token.Length; i++)
            {
                if (token[i] != '\u00DC' && token[i] != '\u00FC')
                    continue;

                char prev = token[i - 1];
                char next = token[i + 1];
                if ((prev == 'g' || prev == 'G') &&
                    (next == 'e' || next == 'E' || next == 'i' || next == 'I'))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeSpanishAsciiToken(ReadOnlySpan<char> token)
        {
            if (token.Length < 2 || IsLikelyAcronym(token))
                return false;

            string lower = new string(token).ToLowerInvariant();
            if (Array.IndexOf(s_englishWordSignals, lower) >= 0)
                return false;

            int score = 0;

            if (Array.IndexOf(s_spanishWordSignals, lower) >= 0)
                score += 4;

            for (int i = 0; i < s_spanishSuffixSignals.Length; i++)
            {
                if (lower.EndsWith(s_spanishSuffixSignals[i], StringComparison.Ordinal))
                {
                    score += 2;
                    break;
                }
            }

            if (lower.Contains("ll", StringComparison.Ordinal) || lower.Contains("rr", StringComparison.Ordinal))
                score += 1;

            if (lower.Contains("que", StringComparison.Ordinal) ||
                lower.Contains("qui", StringComparison.Ordinal) ||
                lower.Contains("gue", StringComparison.Ordinal) ||
                lower.Contains("gui", StringComparison.Ordinal))
            {
                score += 1;
            }

            return score >= 3;
        }

        private static bool ContainsExplicitFrenchCharacter(ReadOnlySpan<char> token)
        {
            for (int i = 0; i < token.Length; i++)
            {
                switch (token[i])
                {
                    // è, ê, ë（スペイン語にはない）
                    case '\u00C8': // È
                    case '\u00CA': // Ê
                    case '\u00CB': // Ë
                    case '\u00E8': // è
                    case '\u00EA': // ê
                    case '\u00EB': // ë
                    // ô, î, ï, û, ù（スペイン語にはない）
                    case '\u00CE': // Î
                    case '\u00CF': // Ï
                    case '\u00D4': // Ô
                    case '\u00D9': // Ù
                    case '\u00DB': // Û
                    case '\u00EE': // î
                    case '\u00EF': // ï
                    case '\u00F4': // ô
                    case '\u00F9': // ù
                    case '\u00FB': // û
                    // ç（セディーユ — スペイン語では使わない）
                    case '\u00C7': // Ç
                    case '\u00E7': // ç
                    // œ, æ（リガチャ）
                    case '\u0152': // Œ
                    case '\u0153': // œ
                    case '\u00C6': // Æ
                    case '\u00E6': // æ
                    // ÿ
                    case '\u00FF': // ÿ
                    case '\u0178': // Ÿ
                        return true;
                }
            }

            return false;
        }

        private static bool LooksLikeFrenchAsciiToken(ReadOnlySpan<char> token)
        {
            if (token.Length < 2 || IsLikelyAcronym(token))
                return false;

            string lower = new string(token).ToLowerInvariant();
            if (Array.IndexOf(s_englishWordSignals, lower) >= 0)
                return false;

            int score = 0;

            if (Array.IndexOf(s_frenchWordSignals, lower) >= 0)
                score += 4;

            for (int i = 0; i < s_frenchSuffixSignals.Length; i++)
            {
                if (lower.EndsWith(s_frenchSuffixSignals[i], StringComparison.Ordinal))
                {
                    score += 2;
                    break;
                }
            }

            // フランス語的な綴りパターン
            if (lower.Contains("eau", StringComparison.Ordinal) ||
                lower.Contains("eux", StringComparison.Ordinal) ||
                lower.Contains("oux", StringComparison.Ordinal) ||
                lower.Contains("oi", StringComparison.Ordinal))
            {
                score += 1;
            }

            if (lower.Contains("qu", StringComparison.Ordinal) ||
                lower.Contains("ou", StringComparison.Ordinal))
            {
                score += 1;
            }

            return score >= 3;
        }

        private static bool ContainsExplicitPortugueseCharacter(ReadOnlySpan<char> token)
        {
            for (int i = 0; i < token.Length; i++)
            {
                switch (token[i])
                {
                    case '\u00C3': // Ã
                    case '\u00E3': // ã
                    case '\u00D5': // Õ
                    case '\u00F5': // õ
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// ç + ポルトガル語固有の接尾パターン（-ço, -ça, -ços, -ças）を検出する。
        /// ç はフランス語にもポルトガル語にもあるが、-ço/-ça パターンはポルトガル語固有。
        /// </summary>
        private static bool ContainsPortugueseCedillaPattern(ReadOnlySpan<char> token)
        {
            // ç/Ç を含まなければ対象外
            bool hasCedilla = false;
            for (int i = 0; i < token.Length; i++)
            {
                if (token[i] == '\u00E7' || token[i] == '\u00C7') // ç, Ç
                {
                    hasCedilla = true;
                    break;
                }
            }

            if (!hasCedilla)
                return false;

            // ポルトガル語固有パターン: -ço, -ça, -ços, -ças
            string lower = new string(token).ToLowerInvariant();
            return lower.EndsWith("\u00E7o", StringComparison.Ordinal)     // ço
                || lower.EndsWith("\u00E7a", StringComparison.Ordinal)     // ça
                || lower.EndsWith("\u00E7os", StringComparison.Ordinal)    // ços
                || lower.EndsWith("\u00E7as", StringComparison.Ordinal);   // ças
        }

        private static bool LooksLikePortugueseAsciiToken(ReadOnlySpan<char> token)
        {
            if (token.Length < 2 || IsLikelyAcronym(token))
                return false;

            string lower = new string(token).ToLowerInvariant();
            if (Array.IndexOf(s_englishWordSignals, lower) >= 0)
                return false;

            int score = 0;

            if (Array.IndexOf(s_portugueseWordSignals, lower) >= 0)
                score += 4;

            for (int i = 0; i < s_portugueseSuffixSignals.Length; i++)
            {
                if (lower.EndsWith(s_portugueseSuffixSignals[i], StringComparison.Ordinal))
                {
                    score += 2;
                    break;
                }
            }

            if (lower.Contains("lh", StringComparison.Ordinal) || lower.Contains("nh", StringComparison.Ordinal))
                score += 1;

            return score >= 3;
        }

        /// <summary>
        /// スウェーデン語特有文字 å (U+00E5) を含むか判定する。
        /// ä (U+00E4) と ö (U+00F6) はドイツ語等と共有するため除外。
        /// å はスウェーデン語/ノルウェー語/デンマーク語で使用されるが、
        /// 現在ノルウェー語・デンマーク語は非サポートのためスウェーデン語に分類する。
        /// </summary>
        private static bool ContainsExplicitSwedishCharacter(ReadOnlySpan<char> text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\u00E5') // å
                    return true;
            }
            return false;
        }

        private static bool LooksLikeSwedishAsciiToken(ReadOnlySpan<char> token)
        {
            if (token.Length < 2 || IsLikelyAcronym(token))
                return false;

            string lower = new string(token).ToLowerInvariant();
            if (Array.IndexOf(s_englishWordSignals, lower) >= 0)
                return false;

            int score = 0;

            if (Array.IndexOf(s_swedishWordSignals, lower) >= 0)
                score += 4;

            for (int i = 0; i < s_swedishSuffixSignals.Length; i++)
            {
                if (lower.EndsWith(s_swedishSuffixSignals[i], StringComparison.Ordinal))
                {
                    score += 2;
                    break;
                }
            }

            return score >= 3;
        }

        private static bool IsLikelyAcronym(ReadOnlySpan<char> token)
        {
            if (token.Length == 0 || token.Length > 6)
                return false;

            bool hasLetter = false;
            for (int i = 0; i < token.Length; i++)
            {
                char c = token[i];
                if (c >= 'A' && c <= 'Z')
                {
                    hasLetter = true;
                    continue;
                }

                if (c >= '0' && c <= '9')
                    continue;

                return false;
            }

            return hasLetter;
        }

        private static byte ResolveCjkIdeographLanguage(string text, int start, int length, byte defaultCjkByte)
        {
            ReadOnlySpan<char> token = text.AsSpan(start, length);

            if (ContainsAny(token, s_chineseStrongMarkers) || CountMarkers(token, s_chineseWeakMarkers) >= 2)
                return LangChinese;

            if (ContainsAny(token, s_japaneseMarkers))
                return LangJapanese;

            if (ContainsAnyWordSignal(token, s_japaneseWordSignals))
                return LangJapanese;

            int chineseLexicalScore = ComputeChineseLexicalScore(token);
            if (ShouldPreferChineseLexically(token, chineseLexicalScore, defaultCjkByte))
                return LangChinese;

            return defaultCjkByte;
        }

        private static bool ContainsAny(ReadOnlySpan<char> token, ReadOnlySpan<char> markers)
        {
            for (int i = 0; i < token.Length; i++)
            {
                if (markers.IndexOf(token[i]) >= 0)
                    return true;
            }

            return false;
        }

        private static int CountMarkers(ReadOnlySpan<char> token, ReadOnlySpan<char> markers)
        {
            int count = 0;
            for (int i = 0; i < token.Length; i++)
            {
                if (markers.IndexOf(token[i]) >= 0)
                    count++;
            }

            return count;
        }

        private static bool ContainsAnyWordSignal(ReadOnlySpan<char> token, string[] candidates)
        {
            string surface = new string(token);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (surface.Contains(candidates[i], StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool ShouldPreferChineseLexically(ReadOnlySpan<char> token, int chineseLexicalScore, byte defaultCjkByte)
        {
            if (chineseLexicalScore < 3)
                return false;

            if (defaultCjkByte == LangChinese)
                return true;

            // Shared short kanji compounds such as "世界" are common in both Japanese and Chinese.
            // When Japanese is the default, only override on longer lexical evidence.
            return token.Length >= 3 && chineseLexicalScore >= 4;
        }

        private static int ComputeChineseLexicalScore(ReadOnlySpan<char> token)
        {
            int score = 0;
            string surface = new string(token);

            int coveredChars = ComputeChinesePhraseCoverage(surface);
            if (coveredChars == surface.Length && surface.Length >= 2)
                score += 4;
            else if (coveredChars >= Math.Max(2, surface.Length - 1))
                score += 3;
            else if (coveredChars * 2 >= surface.Length && coveredChars > 0)
                score += 1;

            if (AllCharsHaveChineseReadings(token))
                score += 1;

            return score;
        }

        private static int ComputeChinesePhraseCoverage(string surface)
        {
            var phraseDictionary = EmbeddedChineseDictionaryCache.TryGetPhraseDictionary();
            if (phraseDictionary == null || surface.Length < 2)
                return 0;

            int covered = 0;
            for (int i = 0; i < surface.Length;)
            {
                int matchedLength = phraseDictionary.FindLongestMatch(surface, i, out _);
                if (matchedLength >= 2)
                {
                    covered += matchedLength;
                    i += matchedLength;
                    continue;
                }

                i++;
            }

            return covered;
        }

        private static bool AllCharsHaveChineseReadings(ReadOnlySpan<char> token)
        {
            var charDictionary = EmbeddedChineseDictionaryCache.TryGetCharDictionary();
            if (charDictionary == null || token.IsEmpty)
                return false;

            for (int i = 0; i < token.Length; i++)
            {
                if (!charDictionary.TryLookup(token[i], out _))
                    return false;
            }

            return true;
        }

        private static byte ResolveStandaloneDigitLanguage(char c, byte defaultCjkByte, byte defaultLatinByte)
        {
            if (c >= '\uFF10' && c <= '\uFF19')
                return defaultCjkByte;

            return defaultLatinByte;
        }

        private static byte ResolveStandaloneNeutralLanguage(ScriptKind kind, char c, byte defaultCjkByte, byte defaultLatinByte)
        {
            if (kind == ScriptKind.Punctuation)
            {
                if (c >= '\uFF01' && c <= '\uFF5E')
                    return defaultCjkByte;

                return defaultLatinByte;
            }

            return defaultCjkByte;
        }
    }
}
