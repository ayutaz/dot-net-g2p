using System;
using System.Collections.Generic;

namespace DotNetG2P.French.Rules
{
    /// <summary>
    /// フランス語のルールベース書記素→音素変換。
    /// マルチグラフ認識、文脈依存子音、鼻母音化、半母音化、位置の法則、黙字処理の
    /// 6フェーズを統合的に処理する。
    /// </summary>
    internal static class GraphemeToPhonemeRules
    {
        /// <summary>
        /// 単語をフランス語のG2Pルールに基づいて音素列に変換する。
        /// </summary>
        /// <param name="word">変換対象の単語（小文字化推奨）</param>
        /// <param name="dialect">フランス語方言</param>
        /// <param name="enableExceptionDictionary">例外辞書を使用するか</param>
        /// <returns>音素列・音節分割情報を含む発音情報</returns>
        public static FrenchPronunciation ConvertWord(string word, FrenchDialect dialect, bool enableExceptionDictionary = true)
        {
            if (string.IsNullOrEmpty(word))
                return new FrenchPronunciation(Array.Empty<FrenchPhoneme>(), Array.Empty<int>(), -1);

            if (enableExceptionDictionary && Data.FrenchExceptionDictionary.TryLookup(word, dialect, out var exception))
                return exception;

            // 1. 書記素→音素変換（Phase1〜3, 5, 6を統合）
            var phonemes = ConvertGraphemes(word, dialect);

            // 2. 半母音化適用（Phase4）
            ApplySemivowelization(phonemes);

            // 3. 方言補正
            ApplyDialectMerger(phonemes, dialect);

            // 4. 音節分割 + FrenchPhoneme配列構築（音節核マーク付き）
            var phonemeArray = phonemes.ToArray();
            var (syllableOffsets, phonemesWithNucleus) = FrenchSyllabifier.Syllabify(phonemeArray);

            // フランス語はストレスを音韻的に区別しないため、常に -1
            return new FrenchPronunciation(phonemesWithNucleus, syllableOffsets, -1);
        }

        /// <summary>
        /// 単語の書記素列から音素リストを構築する。
        /// Phase1（マルチグラフ）、Phase2（文脈依存子音）、Phase3（鼻母音）、
        /// Phase5（位置の法則）、Phase6（黙字処理）を統合的に処理する。
        /// </summary>
        internal static List<FrenchIpaPhoneme> ConvertGraphemes(string word, FrenchDialect dialect)
        {
            var lower = word.ToLowerInvariant();
            var phonemes = new List<FrenchIpaPhoneme>(lower.Length + 4);
            var i = 0;
            var len = lower.Length;

            while (i < len)
            {
                var c = lower[i];

                // ---- Phase1: マルチグラフ認識（最長一致） ----

                // 4文字マルチグラフ
                if (i + 3 < len)
                {
                    if (TryFourCharMultigraph(lower, i, len, phonemes, out var consumed4))
                    {
                        i += consumed4;
                        continue;
                    }
                }

                // 接尾辞パターン（語末チェック）
                if (TrySuffixPattern(lower, i, len, phonemes, dialect, out var consumedSuffix))
                {
                    i += consumedSuffix;
                    continue;
                }

                // 3文字マルチグラフ
                if (i + 2 < len)
                {
                    if (TryThreeCharMultigraph(lower, i, len, phonemes, dialect, out var consumed3))
                    {
                        i += consumed3;
                        continue;
                    }
                }

                // 2文字マルチグラフ
                if (i + 1 < len)
                {
                    if (TryTwoCharMultigraph(lower, i, len, phonemes, dialect, out var consumed2))
                    {
                        i += consumed2;
                        continue;
                    }
                }

                // ---- Phase2: 文脈依存子音 / Phase5: 位置の法則 / Phase6: 黙字処理 ----
                if (TrySingleChar(lower, i, len, phonemes, dialect, out var consumed1))
                {
                    i += consumed1;
                    continue;
                }

                // 未知文字 → スキップ
                i++;
            }

            return phonemes;
        }

        #region Phase1: マルチグラフ認識

        /// <summary>4文字マルチグラフを判定する。</summary>
        private static bool TryFourCharMultigraph(string word, int i, int len, List<FrenchIpaPhoneme> phonemes, out int consumed)
        {
            consumed = 0;

            // "eaux" → /o/
            if (word[i] == 'e' && word[i + 1] == 'a' && word[i + 2] == 'u' && word[i + 3] == 'x')
            {
                phonemes.Add(FrenchIpaPhoneme.O);
                consumed = 4;
                return true;
            }

            return false;
        }

        /// <summary>接尾辞パターンを判定する。</summary>
        private static bool TrySuffixPattern(string word, int i, int len, List<FrenchIpaPhoneme> phonemes, FrenchDialect dialect, out int consumed)
        {
            consumed = 0;
            var remaining = len - i;

            // "-tion" 語末 → /sjɔ̃/
            if (remaining >= 4 && i + 4 == len
                && word[i] == 't' && word[i + 1] == 'i' && word[i + 2] == 'o' && word[i + 3] == 'n')
            {
                phonemes.Add(FrenchIpaPhoneme.S);
                phonemes.Add(FrenchIpaPhoneme.J);
                phonemes.Add(FrenchIpaPhoneme.ONasal);
                consumed = 4;
                return true;
            }

            // "-ssion" 語末 → /sjɔ̃/
            if (remaining >= 5 && i + 5 == len
                && word[i] == 's' && word[i + 1] == 's' && word[i + 2] == 'i' && word[i + 3] == 'o' && word[i + 4] == 'n')
            {
                phonemes.Add(FrenchIpaPhoneme.S);
                phonemes.Add(FrenchIpaPhoneme.J);
                phonemes.Add(FrenchIpaPhoneme.ONasal);
                consumed = 5;
                return true;
            }

            // "-sion" 語末 → /zjɔ̃/
            if (remaining >= 4 && i + 4 == len
                && word[i] == 's' && word[i + 1] == 'i' && word[i + 2] == 'o' && word[i + 3] == 'n')
            {
                phonemes.Add(FrenchIpaPhoneme.Z);
                phonemes.Add(FrenchIpaPhoneme.J);
                phonemes.Add(FrenchIpaPhoneme.ONasal);
                consumed = 4;
                return true;
            }

            // "-ouille" 語末 → /uj/
            if (remaining >= 6 && i + 6 == len
                && word[i] == 'o' && word[i + 1] == 'u' && word[i + 2] == 'i'
                && word[i + 3] == 'l' && word[i + 4] == 'l' && word[i + 5] == 'e')
            {
                phonemes.Add(FrenchIpaPhoneme.U);
                phonemes.Add(FrenchIpaPhoneme.J);
                consumed = 6;
                return true;
            }

            // "-euille" 語末 → /œj/
            if (remaining >= 6 && i + 6 == len
                && word[i] == 'e' && word[i + 1] == 'u' && word[i + 2] == 'i'
                && word[i + 3] == 'l' && word[i + 4] == 'l' && word[i + 5] == 'e')
            {
                phonemes.Add(FrenchIpaPhoneme.Oeh);
                phonemes.Add(FrenchIpaPhoneme.J);
                consumed = 6;
                return true;
            }

            // "-aille" 語末 → /aj/
            if (remaining >= 5 && i + 5 == len
                && word[i] == 'a' && word[i + 1] == 'i' && word[i + 2] == 'l'
                && word[i + 3] == 'l' && word[i + 4] == 'e')
            {
                phonemes.Add(FrenchIpaPhoneme.A);
                phonemes.Add(FrenchIpaPhoneme.J);
                consumed = 5;
                return true;
            }

            // "-eille" 語末 → /ɛj/
            if (remaining >= 5 && i + 5 == len
                && word[i] == 'e' && word[i + 1] == 'i' && word[i + 2] == 'l'
                && word[i + 3] == 'l' && word[i + 4] == 'e')
            {
                phonemes.Add(FrenchIpaPhoneme.Eh);
                phonemes.Add(FrenchIpaPhoneme.J);
                consumed = 5;
                return true;
            }

            // "-ille" 語末 → /ij/
            if (remaining >= 4 && i + 4 == len
                && word[i] == 'i' && word[i + 1] == 'l' && word[i + 2] == 'l' && word[i + 3] == 'e')
            {
                phonemes.Add(FrenchIpaPhoneme.I);
                phonemes.Add(FrenchIpaPhoneme.J);
                consumed = 4;
                return true;
            }

            return false;
        }

        /// <summary>3文字マルチグラフを判定する。</summary>
        private static bool TryThreeCharMultigraph(string word, int i, int len, List<FrenchIpaPhoneme> phonemes, FrenchDialect dialect, out int consumed)
        {
            consumed = 0;
            var c0 = word[i];
            var c1 = word[i + 1];
            var c2 = word[i + 2];

            // "eau" → /o/
            if (c0 == 'e' && c1 == 'a' && c2 == 'u')
            {
                phonemes.Add(FrenchIpaPhoneme.O);
                consumed = 3;
                return true;
            }

            // "ain" → /ɛ̃/ （後続が子音または語末の場合）
            if (c0 == 'a' && c1 == 'i' && c2 == 'n' && !FrenchOrthography.HasTrema(word[i + 1]))
            {
                if (IsNasalContext(word, i + 2))
                {
                    phonemes.Add(FrenchIpaPhoneme.ENasal);
                    consumed = 3;
                    return true;
                }
            }

            // "ein" → /ɛ̃/
            if (c0 == 'e' && c1 == 'i' && c2 == 'n')
            {
                if (IsNasalContext(word, i + 2))
                {
                    phonemes.Add(FrenchIpaPhoneme.ENasal);
                    consumed = 3;
                    return true;
                }
            }

            // "oin" → /wɛ̃/
            if (c0 == 'o' && c1 == 'i' && c2 == 'n')
            {
                if (IsNasalContext(word, i + 2))
                {
                    phonemes.Add(FrenchIpaPhoneme.W);
                    phonemes.Add(FrenchIpaPhoneme.ENasal);
                    consumed = 3;
                    return true;
                }
            }

            // "sch" → /ʃ/
            if (c0 == 's' && c1 == 'c' && c2 == 'h')
            {
                phonemes.Add(FrenchIpaPhoneme.Sh);
                consumed = 3;
                return true;
            }

            // "ill" 語中（母音後） → /ij/
            if (c0 == 'i' && c1 == 'l' && c2 == 'l' && i > 0 && FrenchOrthography.IsVowelChar(word[i - 1]))
            {
                // 語中で後続に母音がある場合
                if (i + 3 < len && FrenchOrthography.IsVowelChar(word[i + 3]))
                {
                    phonemes.Add(FrenchIpaPhoneme.I);
                    phonemes.Add(FrenchIpaPhoneme.J);
                    consumed = 3;
                    return true;
                }
                // 語末の場合も
                if (i + 3 == len)
                {
                    phonemes.Add(FrenchIpaPhoneme.I);
                    phonemes.Add(FrenchIpaPhoneme.J);
                    consumed = 3;
                    return true;
                }
            }

            return false;
        }

        /// <summary>2文字マルチグラフ（ダイグラフ）を判定する。</summary>
        private static bool TryTwoCharMultigraph(string word, int i, int len, List<FrenchIpaPhoneme> phonemes, FrenchDialect dialect, out int consumed)
        {
            consumed = 0;
            var c0 = word[i];
            var c1 = word[i + 1];

            // トレマ付き文字はダイグラフ認識を抑制
            if (FrenchOrthography.HasTrema(c1))
                return false;

            // "ou" → /u/
            if (c0 == 'o' && c1 == 'u')
            {
                // "ou" + n/m → 鼻母音チェックは不要（ou は /u/ であり鼻母音化しない）
                phonemes.Add(FrenchIpaPhoneme.U);
                consumed = 2;
                return true;
            }

            // "oi" → /wa/
            if (c0 == 'o' && c1 == 'i')
            {
                phonemes.Add(FrenchIpaPhoneme.W);
                phonemes.Add(FrenchIpaPhoneme.A);
                consumed = 2;
                return true;
            }

            // "ai" → /ɛ/
            if (c0 == 'a' && c1 == 'i')
            {
                phonemes.Add(FrenchIpaPhoneme.Eh);
                consumed = 2;
                return true;
            }

            // "ei" → /ɛ/
            if (c0 == 'e' && c1 == 'i')
            {
                phonemes.Add(FrenchIpaPhoneme.Eh);
                consumed = 2;
                return true;
            }

            // "au" → /o/
            if (c0 == 'a' && c1 == 'u')
            {
                phonemes.Add(FrenchIpaPhoneme.O);
                consumed = 2;
                return true;
            }

            // "eu" / "œu" 処理
            if ((c0 == 'e' && c1 == 'u') || (c0 == '\u0153' /* œ */ && c1 == 'u'))
            {
                // 閉音節 → /œ/ (Oeh), 開音節 → /ø/ (Oe)
                if (IsInClosedSyllableContext(word, i + 2, len))
                    phonemes.Add(FrenchIpaPhoneme.Oeh);
                else
                    phonemes.Add(FrenchIpaPhoneme.Oe);
                consumed = 2;
                return true;
            }

            // "ch" → /ʃ/
            if (c0 == 'c' && c1 == 'h')
            {
                phonemes.Add(FrenchIpaPhoneme.Sh);
                consumed = 2;
                return true;
            }

            // "ph" → /f/
            if (c0 == 'p' && c1 == 'h')
            {
                phonemes.Add(FrenchIpaPhoneme.F);
                consumed = 2;
                return true;
            }

            // "gn" → /ɲ/
            if (c0 == 'g' && c1 == 'n')
            {
                phonemes.Add(FrenchIpaPhoneme.Ny);
                consumed = 2;
                return true;
            }

            // "th" → /t/
            if (c0 == 't' && c1 == 'h')
            {
                phonemes.Add(FrenchIpaPhoneme.T);
                consumed = 2;
                return true;
            }

            // "qu" → /k/
            if (c0 == 'q' && c1 == 'u')
            {
                phonemes.Add(FrenchIpaPhoneme.K);
                consumed = 2;
                return true;
            }

            // "gu" + {e,i,y} → /g/ （uは黙字）
            if (c0 == 'g' && c1 == 'u' && i + 2 < len && IsFrontVowelForSoftening(word[i + 2]))
            {
                phonemes.Add(FrenchIpaPhoneme.G);
                consumed = 2;
                return true;
            }

            // "sc" + {e,i,y} → /s/
            if (c0 == 's' && c1 == 'c' && i + 2 < len && IsFrontVowelForSoftening(word[i + 2]))
            {
                phonemes.Add(FrenchIpaPhoneme.S);
                consumed = 2;
                return true;
            }

            // 鼻母音ダイグラフ: 母音 + n/m
            if (FrenchOrthography.IsVowelChar(c0) && (c1 == 'n' || c1 == 'm'))
            {
                if (NasalVowelizer.TryNasalize(word, i, c1, dialect, out var nasalPhoneme, out var nasalConsumed))
                {
                    phonemes.Add(nasalPhoneme);
                    consumed = nasalConsumed;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Phase2: 文脈依存子音 / Phase5: 位置の法則 / Phase6: 黙字処理

        /// <summary>単一文字の変換処理を行う。</summary>
        private static bool TrySingleChar(string word, int i, int len, List<FrenchIpaPhoneme> phonemes, FrenchDialect dialect, out int consumed)
        {
            consumed = 1;
            var c = word[i];

            switch (c)
            {
                // ---- 母音 ----

                case 'a':
                case '\u00E0': // à
                    phonemes.Add(FrenchIpaPhoneme.A);
                    return true;

                case '\u00E2': // â → /ɑ/ (Ah: 後舌a)
                    phonemes.Add(FrenchIpaPhoneme.Ah);
                    return true;

                case '\u00E9': // é → /e/
                    phonemes.Add(FrenchIpaPhoneme.E);
                    return true;

                case '\u00E8': // è → /ɛ/
                case '\u00EA': // ê → /ɛ/
                case '\u00EB': // ë → /ɛ/
                    phonemes.Add(FrenchIpaPhoneme.Eh);
                    return true;

                case 'e':
                    return HandlePlainE(word, i, len, phonemes, ref consumed);

                case 'i':
                case '\u00EE': // î
                    phonemes.Add(FrenchIpaPhoneme.I);
                    return true;

                case '\u00EF': // ï （トレマ付き、常に独立した /i/）
                    phonemes.Add(FrenchIpaPhoneme.I);
                    return true;

                case 'o':
                    // 閉音節→/ɔ/ (Oh)、開音節→/o/ (O)
                    if (IsInClosedSyllableContext(word, i + 1, len))
                        phonemes.Add(FrenchIpaPhoneme.Oh);
                    else
                        phonemes.Add(FrenchIpaPhoneme.O);
                    return true;

                case '\u00F4': // ô → /o/
                    phonemes.Add(FrenchIpaPhoneme.O);
                    return true;

                case 'u':
                case '\u00F9': // ù
                case '\u00FB': // û
                    phonemes.Add(FrenchIpaPhoneme.Y);
                    return true;

                case '\u00FC': // ü（トレマ付き、独立した /y/）
                    phonemes.Add(FrenchIpaPhoneme.Y);
                    return true;

                case 'y':
                    return HandleY(word, i, len, phonemes);

                case '\u00E6': // æ → /e/
                    phonemes.Add(FrenchIpaPhoneme.E);
                    return true;

                case '\u0153': // œ → /œ/
                    phonemes.Add(FrenchIpaPhoneme.Oeh);
                    return true;

                // ---- 子音 ----

                case 'b':
                    phonemes.Add(FrenchIpaPhoneme.B);
                    // 重複子音: bb → /b/
                    if (i + 1 < len && word[i + 1] == 'b')
                        consumed = 2;
                    return true;

                case 'c':
                    return HandleC(word, i, len, phonemes, ref consumed);

                case '\u00E7': // ç → /s/
                    phonemes.Add(FrenchIpaPhoneme.S);
                    return true;

                case 'd':
                    // 語末 -d → 黙字（CaReFuL規則）
                    if (i + 1 == len)
                        return true; // 黙字
                    // 語末 -ds → 黙字
                    if (i + 2 == len && word[i + 1] == 's')
                    {
                        consumed = 2;
                        return true;
                    }
                    phonemes.Add(FrenchIpaPhoneme.D);
                    // 重複子音: dd → /d/
                    if (i + 1 < len && word[i + 1] == 'd')
                        consumed = 2;
                    return true;

                case 'f':
                    phonemes.Add(FrenchIpaPhoneme.F);
                    // 重複子音: ff → /f/
                    if (i + 1 < len && word[i + 1] == 'f')
                        consumed = 2;
                    return true;

                case 'g':
                    return HandleG(word, i, len, phonemes, ref consumed);

                case 'h':
                    // h は常に黙字
                    return true;

                case 'j':
                    phonemes.Add(FrenchIpaPhoneme.Zh);
                    return true;

                case 'k':
                    phonemes.Add(FrenchIpaPhoneme.K);
                    return true;

                case 'l':
                    phonemes.Add(FrenchIpaPhoneme.L);
                    // 重複子音: ll → /l/（-ille パターンは先にマルチグラフで処理済み）
                    if (i + 1 < len && word[i + 1] == 'l')
                        consumed = 2;
                    return true;

                case 'm':
                    phonemes.Add(FrenchIpaPhoneme.M);
                    // 重複子音: mm → /m/
                    if (i + 1 < len && word[i + 1] == 'm')
                        consumed = 2;
                    return true;

                case 'n':
                    phonemes.Add(FrenchIpaPhoneme.N);
                    // 重複子音: nn → /n/
                    if (i + 1 < len && word[i + 1] == 'n')
                        consumed = 2;
                    return true;

                case 'p':
                    // 語末 -p → 黙字（CaReFuL規則: pは発音しない）
                    if (i + 1 == len)
                        return true;
                    // 語末 -ps → 黙字
                    if (i + 2 == len && word[i + 1] == 's')
                    {
                        consumed = 2;
                        return true;
                    }
                    phonemes.Add(FrenchIpaPhoneme.P);
                    // 重複子音: pp → /p/
                    if (i + 1 < len && word[i + 1] == 'p')
                        consumed = 2;
                    return true;

                case 'q':
                    // 'q' 単独（qu はダイグラフで処理済み）
                    phonemes.Add(FrenchIpaPhoneme.K);
                    return true;

                case 'r':
                    phonemes.Add(FrenchIpaPhoneme.R);
                    // 重複子音: rr → /r/
                    if (i + 1 < len && word[i + 1] == 'r')
                        consumed = 2;
                    return true;

                case 's':
                    return HandleS(word, i, len, phonemes, ref consumed);

                case 't':
                    return HandleT(word, i, len, phonemes, ref consumed);

                case 'v':
                    phonemes.Add(FrenchIpaPhoneme.V);
                    return true;

                case 'w':
                    // デフォルト /v/。外来語の /w/ は例外辞書でF2対応
                    phonemes.Add(FrenchIpaPhoneme.V);
                    return true;

                case 'x':
                    return HandleX(word, i, len, phonemes);

                case 'z':
                    // 語末 -z → 黙字
                    if (i + 1 == len)
                        return true;
                    phonemes.Add(FrenchIpaPhoneme.Z);
                    return true;

                default:
                    return false;
            }
        }

        #endregion

        #region 個別文字ハンドラ

        /// <summary>無装飾 'e' の処理。位置の法則に基づく。</summary>
        private static bool HandlePlainE(string word, int i, int len, List<FrenchIpaPhoneme> phonemes, ref int consumed)
        {
            // 語末 -er → /e/（rは黙字）
            if (i + 2 == len && word[i + 1] == 'r')
            {
                phonemes.Add(FrenchIpaPhoneme.E);
                consumed = 2;
                return true;
            }

            // 語末 -et → /ɛ/（tは黙字）
            if (i + 2 == len && word[i + 1] == 't')
            {
                phonemes.Add(FrenchIpaPhoneme.Eh);
                consumed = 2;
                return true;
            }

            // 語末 -ed → /e/（dは黙字）
            if (i + 2 == len && word[i + 1] == 'd')
            {
                phonemes.Add(FrenchIpaPhoneme.E);
                consumed = 2;
                return true;
            }

            // 語末 -ez → /e/（zは黙字）
            if (i + 2 == len && word[i + 1] == 'z')
            {
                phonemes.Add(FrenchIpaPhoneme.E);
                consumed = 2;
                return true;
            }

            // 語末 -es → 黙字（語末の e + s は両方黙字）
            if (i + 2 == len && word[i + 1] == 's')
            {
                consumed = 2;
                return true;
            }

            // 語末 -ent → /ɑ̃/ (鼻母音) デフォルト
            if (i + 3 == len && word[i + 1] == 'n' && word[i + 2] == 't')
            {
                phonemes.Add(FrenchIpaPhoneme.ANasal);
                consumed = 3;
                return true;
            }

            // 語末 -e → 黙字（ただし単音節語は /ə/）
            if (i + 1 == len)
            {
                // 単音節語チェック: 先行する音素がない or 先行に母音音素がない
                if (phonemes.Count == 0 || !ContainsVowelPhoneme(phonemes))
                    phonemes.Add(FrenchIpaPhoneme.Schwa);
                // それ以外は黙字
                return true;
            }

            // 子音+e+子音 → /ə/ (schwa)
            if (i > 0 && FrenchOrthography.IsConsonantChar(word[i - 1])
                && i + 1 < len && FrenchOrthography.IsConsonantChar(word[i + 1]))
            {
                phonemes.Add(FrenchIpaPhoneme.Schwa);
                return true;
            }

            // 子音+e+母音 → 黙字
            if (i > 0 && FrenchOrthography.IsConsonantChar(word[i - 1])
                && i + 1 < len && FrenchOrthography.IsVowelChar(word[i + 1]))
            {
                return true;
            }

            // デフォルト: /ə/
            phonemes.Add(FrenchIpaPhoneme.Schwa);
            return true;
        }

        /// <summary>'c' の処理。前舌母音の前では /s/、それ以外は /k/。</summary>
        private static bool HandleC(string word, int i, int len, List<FrenchIpaPhoneme> phonemes, ref int consumed)
        {
            // 語末 -c → CaReFuL規則: /k/
            if (i + 1 == len)
            {
                phonemes.Add(FrenchIpaPhoneme.K);
                return true;
            }

            // 重複子音: cc + 前舌母音 → /ks/
            if (i + 2 < len && word[i + 1] == 'c' && IsFrontVowelForSoftening(word[i + 2]))
            {
                phonemes.Add(FrenchIpaPhoneme.K);
                phonemes.Add(FrenchIpaPhoneme.S);
                return true;
            }

            // 重複子音: cc + 非前舌母音 → /k/ (consumed=2)
            if (i + 1 < len && word[i + 1] == 'c' && (i + 2 >= len || !IsFrontVowelForSoftening(word[i + 2])))
            {
                phonemes.Add(FrenchIpaPhoneme.K);
                consumed = 2;
                return true;
            }

            // c + 前舌母音 → /s/
            if (i + 1 < len && IsFrontVowelForSoftening(word[i + 1]))
            {
                phonemes.Add(FrenchIpaPhoneme.S);
                return true;
            }

            // c + 他 → /k/
            phonemes.Add(FrenchIpaPhoneme.K);
            return true;
        }

        /// <summary>'g' の処理。前舌母音の前では /ʒ/、それ以外は /g/。</summary>
        private static bool HandleG(string word, int i, int len, List<FrenchIpaPhoneme> phonemes, ref int consumed)
        {
            // 語末 -g → 黙字
            if (i + 1 == len)
                return true;

            // 重複子音: gg → /g/ (consumed=2)
            if (i + 1 < len && word[i + 1] == 'g')
            {
                phonemes.Add(FrenchIpaPhoneme.G);
                consumed = 2;
                return true;
            }

            // g + 前舌母音 → /ʒ/
            if (i + 1 < len && IsFrontVowelForSoftening(word[i + 1]))
            {
                phonemes.Add(FrenchIpaPhoneme.Zh);
                return true;
            }

            // g + 他 → /g/
            phonemes.Add(FrenchIpaPhoneme.G);
            return true;
        }

        /// <summary>'s' の処理。母音間の s は /z/、それ以外は /s/。</summary>
        private static bool HandleS(string word, int i, int len, List<FrenchIpaPhoneme> phonemes, ref int consumed)
        {
            // 語末 -s → 黙字
            if (i + 1 == len)
                return true;

            // 重複子音: ss → /s/
            if (i + 1 < len && word[i + 1] == 's')
            {
                phonemes.Add(FrenchIpaPhoneme.S);
                consumed = 2;
                return true;
            }

            // 母音間の s → /z/
            if (i > 0 && FrenchOrthography.IsVowelChar(word[i - 1])
                && i + 1 < len && FrenchOrthography.IsVowelChar(word[i + 1]))
            {
                phonemes.Add(FrenchIpaPhoneme.Z);
                return true;
            }

            phonemes.Add(FrenchIpaPhoneme.S);
            return true;
        }

        /// <summary>'t' の処理。語末は黙字。</summary>
        private static bool HandleT(string word, int i, int len, List<FrenchIpaPhoneme> phonemes, ref int consumed)
        {
            // 語末 -t → 黙字
            if (i + 1 == len)
                return true;

            // 語末 -ts → 黙字
            if (i + 2 == len && word[i + 1] == 's')
            {
                consumed = 2;
                return true;
            }

            // 重複子音: tt → /t/
            if (i + 1 < len && word[i + 1] == 't')
            {
                phonemes.Add(FrenchIpaPhoneme.T);
                consumed = 2;
                return true;
            }

            phonemes.Add(FrenchIpaPhoneme.T);
            return true;
        }

        /// <summary>'x' の処理。</summary>
        private static bool HandleX(string word, int i, int len, List<FrenchIpaPhoneme> phonemes)
        {
            // 語末 -x → 黙字
            if (i + 1 == len)
                return true;

            // 語頭 "ex-" + 母音 → /ɛgz/
            if (i == 1 && word[0] == 'e' && i + 1 < len && FrenchOrthography.IsVowelChar(word[i + 1]))
            {
                // 直前の 'e' は既にPhonemes に追加済みなので、ここでは g+z を追加
                // ただし直前の e がSchwa/E/Ehで追加済みの場合、Ehに置き換え
                if (phonemes.Count > 0)
                {
                    var lastIdx = phonemes.Count - 1;
                    var lastPhoneme = phonemes[lastIdx];
                    if (lastPhoneme == FrenchIpaPhoneme.Schwa || lastPhoneme == FrenchIpaPhoneme.E)
                        phonemes[lastIdx] = FrenchIpaPhoneme.Eh;
                }
                phonemes.Add(FrenchIpaPhoneme.G);
                phonemes.Add(FrenchIpaPhoneme.Z);
                return true;
            }

            // x + 子音 → /ks/
            if (i + 1 < len && FrenchOrthography.IsConsonantChar(word[i + 1]))
            {
                phonemes.Add(FrenchIpaPhoneme.K);
                phonemes.Add(FrenchIpaPhoneme.S);
                return true;
            }

            // デフォルト: /ks/
            phonemes.Add(FrenchIpaPhoneme.K);
            phonemes.Add(FrenchIpaPhoneme.S);
            return true;
        }

        /// <summary>'y' の処理。</summary>
        private static bool HandleY(string word, int i, int len, List<FrenchIpaPhoneme> phonemes)
        {
            // 語頭 y + 母音 → /j/
            if (i == 0 && i + 1 < len && FrenchOrthography.IsVowelChar(word[i + 1]))
            {
                phonemes.Add(FrenchIpaPhoneme.J);
                return true;
            }

            // 母音 + y + 母音 → 前母音は既出 + /i/ + /j/（y が母音分裂を起こす）
            if (i > 0 && FrenchOrthography.IsVowelChar(word[i - 1])
                && i + 1 < len && FrenchOrthography.IsVowelChar(word[i + 1]))
            {
                phonemes.Add(FrenchIpaPhoneme.I);
                phonemes.Add(FrenchIpaPhoneme.J);
                return true;
            }

            // y + 子音 or 語末 → /i/
            phonemes.Add(FrenchIpaPhoneme.I);
            return true;
        }

        #endregion

        #region Phase4: 半母音化

        /// <summary>
        /// 半母音化を適用する。母音+母音の連続で半母音に変化するパターンを処理する。
        /// </summary>
        internal static void ApplySemivowelization(List<FrenchIpaPhoneme> phonemes)
        {
            for (var i = 0; i < phonemes.Count - 1; i++)
            {
                var current = phonemes[i];
                var next = phonemes[i + 1];

                // I + 母音 → J + 母音（鼻母音を含む全母音が対象）
                if (current == FrenchIpaPhoneme.I && IsVowelPhoneme(next))
                {
                    // 語頭で後ろに母音がない場合はI維持（ただしここでは音素列上の判定）
                    // 語頭の I は ConvertGraphemes で J に変換済みなので、残ったI+母音は半母音化
                    phonemes[i] = FrenchIpaPhoneme.J;
                    continue;
                }

                // Y(=/y/) + 母音 → Uj + 母音（鼻母音を含む全母音が対象）
                if (current == FrenchIpaPhoneme.Y && IsVowelPhoneme(next))
                {
                    phonemes[i] = FrenchIpaPhoneme.Uj;
                    continue;
                }

                // U(=/u/) + 母音 → W + 母音（鼻母音を含む全母音が対象）
                if (current == FrenchIpaPhoneme.U && IsVowelPhoneme(next))
                {
                    phonemes[i] = FrenchIpaPhoneme.W;
                    continue;
                }
            }
        }

        #endregion

        #region 方言補正

        /// <summary>
        /// 方言に基づく音素マージを適用する。
        /// Metropolitan: Ah→A, OeNasal→ENasal
        /// </summary>
        internal static void ApplyDialectMerger(List<FrenchIpaPhoneme> phonemes, FrenchDialect dialect)
        {
            if (dialect != FrenchDialect.Metropolitan)
                return;

            for (var i = 0; i < phonemes.Count; i++)
            {
                switch (phonemes[i])
                {
                    case FrenchIpaPhoneme.Ah:
                        phonemes[i] = FrenchIpaPhoneme.A;
                        break;
                    case FrenchIpaPhoneme.OeNasal:
                        phonemes[i] = FrenchIpaPhoneme.ENasal;
                        break;
                }
            }
        }

        #endregion

        #region ヘルパー

        /// <summary>鼻母音化の文脈（後続が子音または語末）かどうかを判定する。</summary>
        private static bool IsNasalContext(string word, int nasalIndex)
        {
            // nasalIndex は鼻子音('n')の位置
            var afterNasal = nasalIndex + 1;

            // 語末 → 鼻母音化
            if (afterNasal >= word.Length)
                return true;

            var next = word[afterNasal];

            // 後続が母音 → 非鼻母音化
            if (FrenchOrthography.IsVowelChar(next))
                return false;

            // 後続が同じ子音（nn）→ 非鼻母音化
            if (next == word[nasalIndex])
                return false;

            // 後続が子音 → 鼻母音化
            return true;
        }

        /// <summary>c/g の軟音化をトリガーする前舌母音かどうか。</summary>
        private static bool IsFrontVowelForSoftening(char c)
        {
            c = char.ToLowerInvariant(c);
            switch (c)
            {
                case 'e': case 'i': case 'y':
                case '\u00E8': // è
                case '\u00E9': // é
                case '\u00EA': // ê
                case '\u00EB': // ë
                case '\u00EE': // î
                case '\u00EF': // ï
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>閉音節の文脈かどうかを簡易判定する。</summary>
        private static bool IsInClosedSyllableContext(string word, int afterVowel, int len)
        {
            // 語末 → 開音節
            if (afterVowel >= len)
                return false;

            // 後続に子音が1つだけで語末 → 開音節 (CV構造)
            if (afterVowel + 1 == len && FrenchOrthography.IsConsonantChar(word[afterVowel]))
            {
                // 語末子音が発音される場合は閉音節
                var finalC = word[afterVowel];
                return IsCaReFuLConsonant(finalC);
            }

            // 後続に子音クラスターがある → 閉音節
            if (afterVowel + 1 < len
                && FrenchOrthography.IsConsonantChar(word[afterVowel])
                && FrenchOrthography.IsConsonantChar(word[afterVowel + 1]))
                return true;

            // 後続に子音+母音 → 開音節
            if (afterVowel + 1 < len
                && FrenchOrthography.IsConsonantChar(word[afterVowel])
                && FrenchOrthography.IsVowelChar(word[afterVowel + 1]))
                return false;

            return false;
        }

        /// <summary>CaReFuL規則で語末に発音される子音かどうか。</summary>
        private static bool IsCaReFuLConsonant(char c)
        {
            c = char.ToLowerInvariant(c);
            return c == 'c' || c == 'r' || c == 'f' || c == 'l';
        }

        /// <summary>口母音（非鼻母音の母音）音素かどうか。</summary>
        private static bool IsOralVowel(FrenchIpaPhoneme phoneme)
        {
            switch (phoneme)
            {
                case FrenchIpaPhoneme.A:
                case FrenchIpaPhoneme.Ah:
                case FrenchIpaPhoneme.E:
                case FrenchIpaPhoneme.Eh:
                case FrenchIpaPhoneme.I:
                case FrenchIpaPhoneme.O:
                case FrenchIpaPhoneme.Oh:
                case FrenchIpaPhoneme.U:
                case FrenchIpaPhoneme.Y:
                case FrenchIpaPhoneme.Oe:
                case FrenchIpaPhoneme.Oeh:
                case FrenchIpaPhoneme.Schwa:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>母音音素（口母音＋鼻母音）かどうか。</summary>
        private static bool IsVowelPhoneme(FrenchIpaPhoneme phoneme)
        {
            switch (phoneme)
            {
                case FrenchIpaPhoneme.A:
                case FrenchIpaPhoneme.Ah:
                case FrenchIpaPhoneme.E:
                case FrenchIpaPhoneme.Eh:
                case FrenchIpaPhoneme.I:
                case FrenchIpaPhoneme.O:
                case FrenchIpaPhoneme.Oh:
                case FrenchIpaPhoneme.U:
                case FrenchIpaPhoneme.Y:
                case FrenchIpaPhoneme.Oe:
                case FrenchIpaPhoneme.Oeh:
                case FrenchIpaPhoneme.Schwa:
                case FrenchIpaPhoneme.ANasal:
                case FrenchIpaPhoneme.ONasal:
                case FrenchIpaPhoneme.ENasal:
                case FrenchIpaPhoneme.OeNasal:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>音素リストに母音音素が含まれているか。</summary>
        private static bool ContainsVowelPhoneme(List<FrenchIpaPhoneme> phonemes)
        {
            for (var i = 0; i < phonemes.Count; i++)
            {
                if (IsVowelPhoneme(phonemes[i]))
                    return true;
            }
            return false;
        }

        #endregion
    }
}
