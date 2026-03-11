using System;
using System.Collections.Generic;

namespace DotNetG2P.Portuguese.Rules
{
    /// <summary>
    /// ポルトガル語のルールベース書記素→音素変換。
    /// ダイグラフ+鼻母音化、文脈依存子音、母音変換（ストレス依存）、半母音化、黙字処理の
    /// 5フェーズを統合的に処理する。
    /// 母音弱化（Phase 5 in 02_g2p_rules.md）はP2の AllophoneProcessor で処理する。
    /// </summary>
    internal static class GraphemeToPhonemeRules
    {
        /// <summary>
        /// 単語をポルトガル語のG2Pルールに基づいて音素列に変換する。
        /// </summary>
        /// <param name="word">変換対象の単語</param>
        /// <param name="dialect">ポルトガル語方言</param>
        /// <param name="enableExceptionDictionary">例外辞書を使用するか</param>
        /// <returns>音素列・音節分割情報を含む発音情報</returns>
        public static PortuguesePronunciation ConvertWord(string word, PortugueseDialect dialect, bool enableExceptionDictionary = true)
        {
            if (string.IsNullOrEmpty(word))
                return new PortuguesePronunciation(Array.Empty<PortuguesePhoneme>(), Array.Empty<int>(), -1);

            // 小文字化 + NFC正規化
            var lower = word.ToLowerInvariant();
            lower = lower.Normalize(System.Text.NormalizationForm.FormC);

            // 例外辞書チェック
            if (enableExceptionDictionary && Data.PortugueseExceptionDictionary.TryLookup(lower, dialect, out var exception))
                return exception;

            // 音節分割 + ストレス決定
            var syllables = PortugueseSyllabifier.Syllabify(lower);
            var stressedSyllables = StressAssigner.MarkStress(lower, syllables);

            // ストレス音節インデックスを事前計算
            var stressedIndex = -1;
            for (var i = 0; i < stressedSyllables.Count; i++)
            {
                if (stressedSyllables[i].IsStressed)
                    stressedIndex = i;
            }

            // 統合走査で全フェーズを処理（音節オフセットも同時追跡）
            var phonemes = new List<PortuguesePhoneme>(lower.Length + 4);
            var syllableOffsets = new int[stressedSyllables.Count];
            ConvertGraphemes(lower, stressedSyllables, dialect, phonemes, syllableOffsets);

            return new PortuguesePronunciation(phonemes.ToArray(), syllableOffsets, stressedIndex);
        }

        /// <summary>
        /// 単語の書記素列から音素リストを構築する統合走査。
        /// Phase1: ダイグラフ+鼻母音化、Phase2: 文脈依存子音、
        /// Phase3: 母音変換、Phase4: 半母音化、Phase5: 黙字処理
        /// </summary>
        internal static void ConvertGraphemes(
            string word,
            IReadOnlyList<PortugueseSyllable> syllables,
            PortugueseDialect dialect,
            List<PortuguesePhoneme> phonemes,
            int[]? syllableOffsets = null)
        {
            var i = 0;
            var len = word.Length;

            // 音節オフセット追跡用: 次に記録すべき音節のインデックス
            var nextSyllableIdx = 0;

            // 最初の音節オフセットを記録
            if (syllableOffsets != null && syllables.Count > 0)
            {
                syllableOffsets[0] = 0;
                nextSyllableIdx = 1;
            }

            while (i < len)
            {
                var c = word[i];

                // 音節境界チェック: 現在の文字位置が次の音節の開始位置に達したらオフセットを記録
                if (syllableOffsets != null && nextSyllableIdx < syllables.Count)
                {
                    if (i >= syllables[nextSyllableIdx].StartIndex)
                    {
                        syllableOffsets[nextSyllableIdx] = phonemes.Count;
                        nextSyllableIdx++;
                    }
                }

                // Phase 5: 語頭 h → 黙字
                if (c == 'h' && i == 0)
                {
                    i++;
                    continue;
                }

                // Phase 1: チルダ付き母音（語末単独を含む）
                if (c == '\u00E3' || c == '\u00F5') // ã, õ
                {
                    if (TryTildeVowel(word, i, len, syllables, phonemes, out var tildeConsumed))
                    {
                        i += tildeConsumed;
                        continue;
                    }
                }

                // Phase 1: ダイグラフ + 鼻母音化（最長一致、i+1 < len が必要）
                if (i + 1 < len)
                {
                    if (TryDigraphOrNasal(word, i, len, syllables, dialect, phonemes, out var consumed))
                    {
                        i += consumed;
                        continue;
                    }
                }

                // Phase 2: 文脈依存子音
                if (PortugueseOrthography.IsConsonant(c))
                {
                    if (TryConsonant(word, i, len, dialect, phonemes, out var consumed))
                    {
                        i += consumed;
                        continue;
                    }
                }

                // Phase 3 + Phase 4: 母音変換 + 半母音化
                if (PortugueseOrthography.IsVowel(c))
                {
                    var consumed = ConvertVowel(word, i, len, syllables, phonemes);
                    i += consumed;
                    continue;
                }

                // 未知文字 → スキップ
                i++;
            }
        }

        #region Phase 1: ダイグラフ + 鼻母音化

        /// <summary>チルダ付き母音（ã, õ）を処理する。語末単独を含む。</summary>
        private static bool TryTildeVowel(
            string word, int i, int len,
            IReadOnlyList<PortugueseSyllable> syllables,
            List<PortuguesePhoneme> phonemes,
            out int consumed)
        {
            consumed = 0;
            var c0 = word[i];

            // ã → 常に鼻母音
            if (c0 == '\u00E3') // ã
            {
                var stressed = IsInStressedSyllable(word, i, syllables);
                // ão (鼻二重母音)
                if (i + 1 < len && word[i + 1] == 'o')
                {
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.ANasal, stressed));
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.WNasal));
                    consumed = 2;
                    return true;
                }
                // ãe / ães (鼻二重母音)
                if (i + 1 < len && word[i + 1] == 'e')
                {
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.ANasal, stressed));
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.JNasal));
                    consumed = 2;
                    return true;
                }
                // ãi (鼻二重母音)
                if (i + 1 < len && word[i + 1] == 'i')
                {
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.ANasal, stressed));
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.JNasal));
                    consumed = 2;
                    return true;
                }
                // ã 単独（語末含む）
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.ANasal, stressed));
                consumed = 1;
                return true;
            }

            // õe / õ → 鼻母音
            if (c0 == '\u00F5') // õ
            {
                var stressed = IsInStressedSyllable(word, i, syllables);
                // õe /ões (鼻二重母音)
                if (i + 1 < len && word[i + 1] == 'e')
                {
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.ONasal, stressed));
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.JNasal));
                    consumed = 2;
                    return true;
                }
                // õ 単独（語末含む）
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.ONasal, stressed));
                consumed = 1;
                return true;
            }

            return false;
        }

        /// <summary>ダイグラフまたは鼻母音化パターンを判定・変換する。i+1 &lt; len が保証されていること。</summary>
        private static bool TryDigraphOrNasal(
            string word, int i, int len,
            IReadOnlyList<PortugueseSyllable> syllables,
            PortugueseDialect dialect,
            List<PortuguesePhoneme> phonemes,
            out int consumed)
        {
            consumed = 0;
            var c0 = word[i];
            var c1 = word[i + 1];

            // --- 子音ダイグラフ ---

            // ch → /ʃ/
            if (c0 == 'c' && c1 == 'h')
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.Sh));
                consumed = 2;
                return true;
            }

            // lh → /ʎ/
            if (c0 == 'l' && c1 == 'h')
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.Lh));
                consumed = 2;
                return true;
            }

            // nh → /ɲ/
            if (c0 == 'n' && c1 == 'h')
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.Ny));
                consumed = 2;
                return true;
            }

            // rr → /ʁ/
            if (c0 == 'r' && c1 == 'r')
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.Rr));
                consumed = 2;
                return true;
            }

            // ss → /s/
            if (c0 == 's' && c1 == 's')
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.S));
                consumed = 2;
                return true;
            }

            // xc + 前舌母音 → /s/
            if (c0 == 'x' && c1 == 'c' && i + 2 < len && PortugueseOrthography.IsFrontVowel(word[i + 2]))
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.S));
                consumed = 2;
                return true;
            }

            // sc + 前舌母音 → /s/
            if (c0 == 's' && c1 == 'c' && i + 2 < len && PortugueseOrthography.IsFrontVowel(word[i + 2]))
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.S));
                consumed = 2;
                return true;
            }

            // qu + 前舌母音 → /k/ (u黙字)
            if (c0 == 'q' && c1 == 'u')
            {
                if (i + 2 < len && PortugueseOrthography.IsFrontVowel(word[i + 2]))
                {
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.K));
                    consumed = 2;
                    return true;
                }
                // qu 語末 → /k/ のみ（S8修正: 後続文字なしの場合 /w/ を出力しない）
                if (i + 2 >= len)
                {
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.K));
                    consumed = 2;
                    return true;
                }
                // qu + 非前舌母音 → /kw/
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.K));
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.W));
                consumed = 2;
                return true;
            }

            // gü → /gw/ (旧正書法互換: güe, güi)
            if (c0 == 'g' && c1 == '\u00FC')
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.G));
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.W));
                consumed = 2;
                return true;
            }

            // gu + 前舌母音 → /g/ (u黙字)
            if (c0 == 'g' && c1 == 'u')
            {
                if (i + 2 < len && PortugueseOrthography.IsFrontVowel(word[i + 2]))
                {
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.G));
                    consumed = 2;
                    return true;
                }
                // gu + 非前舌母音: g は通常処理に回す (gua → /gwa/)
                // ここでは処理しない。g として Phase 2 で処理し、u は Phase 3 で母音として処理
                return false;
            }

            // --- 鼻母音化 ---
            // 注: チルダ付き母音(ã, õ)はTryTildeVowelで既に処理済み

            // 母音 + n/m → 鼻母音化判定
            // ただし n+h の場合は nh ダイグラフとして扱うため鼻母音化しない
            if (PortugueseOrthography.IsVowel(c0) && (c1 == 'n' || c1 == 'm')
                && !(c1 == 'n' && i + 2 < len && word[i + 2] == 'h'))
            {
                var isWordFinal = i + 2 >= word.Length || (i + 3 >= word.Length && char.ToLowerInvariant(word[i + 2]) == 's');
                var isStressed = IsInStressedSyllable(word, i, syllables);
                if (NasalVowelizer.TryNasalize(word, i, isWordFinal, isStressed, out var nasalPhonemes, out var nasalConsumed))
                {
                    foreach (var np in nasalPhonemes)
                        phonemes.Add(new PortuguesePhoneme(np, np == nasalPhonemes[0] && isStressed));
                    consumed = nasalConsumed;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Phase 2: 文脈依存子音

        /// <summary>子音の文脈依存変換を行う。</summary>
        private static bool TryConsonant(
            string word, int i, int len,
            PortugueseDialect dialect,
            List<PortuguesePhoneme> phonemes,
            out int consumed)
        {
            consumed = 1;
            var c = word[i];

            switch (c)
            {
                case 'b':
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.B));
                    return true;

                case 'c':
                    return HandleC(word, i, len, phonemes, ref consumed);

                case '\u00E7': // ç → /s/
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.S));
                    return true;

                case 'd':
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.D));
                    return true;

                case 'f':
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.F));
                    return true;

                case 'g':
                    return HandleG(word, i, len, phonemes, ref consumed);

                case 'h':
                    // 語頭hは既にConvertGraphemesでスキップ済み
                    // 語中hはダイグラフ（ch,lh,nh）で処理済み
                    // それ以外は黙字
                    return true;

                case 'j':
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.Zh));
                    return true;

                case 'k':
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.K));
                    return true;

                case 'l':
                    // P1: 基本 /l/（coda l の方言異音化はP2のAllophoneProcessorで処理）
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.L));
                    return true;

                case 'm':
                    // onset の m（鼻母音化は Phase1 で処理済み）
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.M));
                    return true;

                case 'n':
                    // onset の n（鼻母音化は Phase1 で処理済み）
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.N));
                    return true;

                case 'p':
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.P));
                    return true;

                case 'q':
                    // q 単独（qu はダイグラフで処理済み）
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.K));
                    return true;

                case 'r':
                    return HandleR(word, i, len, phonemes, ref consumed);

                case 's':
                    return HandleS(word, i, len, phonemes, ref consumed);

                case 't':
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.T));
                    return true;

                case 'v':
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.V));
                    return true;

                case 'w':
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.W));
                    return true;

                case 'x':
                    return HandleX(word, i, len, phonemes, ref consumed);

                case 'z':
                    // P1: 基本 /z/（coda z の方言処理はP2のAllophoneProcessorで処理）
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.Z));
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>'c' の処理。前舌母音の前では /s/、それ以外は /k/。</summary>
        private static bool HandleC(string word, int i, int len, List<PortuguesePhoneme> phonemes, ref int consumed)
        {
            // cc + 前舌母音 → /s/（旧正書法: 最初のcは黙字）
            if (i + 1 < len && word[i + 1] == 'c' && i + 2 < len && PortugueseOrthography.IsFrontVowel(word[i + 2]))
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.S));
                consumed = 2;
                return true;
            }

            // c + 前舌母音 → /s/
            if (i + 1 < len && PortugueseOrthography.IsFrontVowel(word[i + 1]))
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.S));
                return true;
            }

            // c + 他 → /k/
            phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.K));
            return true;
        }

        /// <summary>'g' の処理。前舌母音の前では /ʒ/、それ以外は /g/。</summary>
        private static bool HandleG(string word, int i, int len, List<PortuguesePhoneme> phonemes, ref int consumed)
        {
            // g + 前舌母音 → /ʒ/
            if (i + 1 < len && PortugueseOrthography.IsFrontVowel(word[i + 1]))
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.Zh));
                return true;
            }

            // g + 他 → /g/
            phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.G));
            return true;
        }

        /// <summary>'r' の処理。位置依存で /ɾ/ か /ʁ/ かを決定する。</summary>
        private static bool HandleR(string word, int i, int len, List<PortuguesePhoneme> phonemes, ref int consumed)
        {
            // 語頭 r → /ʁ/ (強いR)
            if (i == 0)
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.Rr));
                return true;
            }

            // n/l/s の直後 → /ʁ/ (強いR)
            var prev = word[i - 1];
            if (prev == 'n' || prev == 'l' || prev == 's')
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.Rr));
                return true;
            }

            // 母音間（単独r）→ /ɾ/ (弱いR, はじき音)
            if (i > 0 && i + 1 < len
                && PortugueseOrthography.IsVowel(prev)
                && PortugueseOrthography.IsVowel(word[i + 1]))
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.R));
                return true;
            }

            // それ以外（子音クラスタ、coda等）→ /ɾ/ (弱いR)
            phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.R));
            return true;
        }

        /// <summary>'s' の処理。位置依存で /s/ か /z/ かを決定する。</summary>
        private static bool HandleS(string word, int i, int len, List<PortuguesePhoneme> phonemes, ref int consumed)
        {
            // 語頭 s → /s/
            if (i == 0)
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.S));
                return true;
            }

            // 母音間の s → /z/
            if (i > 0 && i + 1 < len
                && PortugueseOrthography.IsVowel(word[i - 1])
                && PortugueseOrthography.IsVowel(word[i + 1]))
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.Z));
                return true;
            }

            // それ以外 → /s/
            phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.S));
            return true;
        }

        /// <summary>'x' の処理。4通りの発音を文脈で決定する。</summary>
        private static bool HandleX(string word, int i, int len, List<PortuguesePhoneme> phonemes, ref int consumed)
        {
            // 語頭 x → /ʃ/
            if (i == 0)
            {
                phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.Sh));
                return true;
            }

            // ex + 母音 → /z/ (ex- 接頭辞パターン)
            if (i > 0 && word[i - 1] == 'e' && i + 1 < len && PortugueseOrthography.IsVowel(word[i + 1]))
            {
                // 直前が語頭の 'e' であるかチェック (位置1)
                if (i == 1)
                {
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.Z));
                    return true;
                }
                // 母音-ex+母音 の場合もex-パターン（例: inexato → in-e-xa-to）
                // 簡易判定: 直前の 'e' のさらに前が子音なら ex- パターン
                if (i >= 2 && PortugueseOrthography.IsConsonant(word[i - 2]))
                {
                    phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.Z));
                    return true;
                }
            }

            // デフォルト → /ʃ/ (最も一般的)
            // /ks/, /s/ のケースは例外辞書で対処（P2）
            phonemes.Add(new PortuguesePhoneme(PortugueseIpaPhoneme.Sh));
            return true;
        }

        #endregion

        #region Phase 3 + Phase 4: 母音変換 + 半母音化

        /// <summary>母音を変換する。ストレス位置に基づいて開/閉を決定し、半母音化も処理する。</summary>
        private static int ConvertVowel(
            string word, int i, int len,
            IReadOnlyList<PortugueseSyllable> syllables,
            List<PortuguesePhoneme> phonemes)
        {
            var c = word[i];
            var stressed = IsInStressedSyllable(word, i, syllables);

            // Phase 4: 半母音化判定（上昇二重母音: 弱母音 + 後続母音）
            if ((c == 'i' || c == 'u') && i + 1 < len && PortugueseOrthography.IsVowel(word[i + 1]))
            {
                // 弱母音 + 強母音 → 半母音化（上昇二重母音）
                var next = word[i + 1];
                if (PortugueseOrthography.CanFormDiphthong(c, next))
                {
                    var glide = c == 'i' ? PortugueseIpaPhoneme.J : PortugueseIpaPhoneme.W;
                    phonemes.Add(new PortuguesePhoneme(glide));
                    return 1;
                }
            }

            // Phase 4: 下降二重母音判定（母音 + 弱母音）
            if (i + 1 < len && (word[i + 1] == 'i' || word[i + 1] == 'u'))
            {
                var next = word[i + 1];
                if (PortugueseOrthography.CanFormDiphthong(c, next))
                {
                    // 先に現在の母音を出力
                    var vowelPhoneme = MapVowel(c, stressed);
                    phonemes.Add(new PortuguesePhoneme(vowelPhoneme, stressed));

                    // 後続弱母音を半母音として出力
                    var glide = next == 'i' ? PortugueseIpaPhoneme.J : PortugueseIpaPhoneme.W;
                    phonemes.Add(new PortuguesePhoneme(glide));
                    return 2;
                }
            }

            // 単独母音
            var phoneme = MapVowel(c, stressed);
            phonemes.Add(new PortuguesePhoneme(phoneme, stressed));
            return 1;
        }

        /// <summary>母音字を音素にマッピングする。</summary>
        internal static PortugueseIpaPhoneme MapVowel(char c, bool stressed)
        {
            switch (c)
            {
                // アクセント記号付き母音（確定的マッピング）
                case '\u00E1': // á → /a/ (開a)
                    return PortugueseIpaPhoneme.A;
                case '\u00E2': // â → /ɐ/ (閉a)
                    return PortugueseIpaPhoneme.Schwa;
                case '\u00E0': // à → /a/
                    return PortugueseIpaPhoneme.A;
                case '\u00E9': // é → /ɛ/ (開e)
                    return PortugueseIpaPhoneme.Eh;
                case '\u00EA': // ê → /e/ (閉e)
                    return PortugueseIpaPhoneme.E;
                case '\u00ED': // í → /i/
                    return PortugueseIpaPhoneme.I;
                case '\u00F3': // ó → /ɔ/ (開o)
                    return PortugueseIpaPhoneme.Oh;
                case '\u00F4': // ô → /o/ (閉o)
                    return PortugueseIpaPhoneme.O;
                case '\u00FA': // ú → /u/
                    return PortugueseIpaPhoneme.U;
                case '\u00FC': // ü → /u/ (旧正書法互換)
                    return PortugueseIpaPhoneme.U;

                // 無標母音（ストレス依存）
                case 'a':
                    return PortugueseIpaPhoneme.A;
                case 'e':
                    // 無標ストレスe → /e/ (閉e, デフォルト)
                    // 無標非ストレスe → /e/ (P2でAllophoneProcessorにより弱化)
                    return PortugueseIpaPhoneme.E;
                case 'i':
                    return PortugueseIpaPhoneme.I;
                case 'o':
                    // 無標ストレスo → /o/ (閉o, デフォルト)
                    // 無標非ストレスo → /o/ (P2でAllophoneProcessorにより弱化)
                    return PortugueseIpaPhoneme.O;
                case 'u':
                    return PortugueseIpaPhoneme.U;

                default:
                    return PortugueseIpaPhoneme.A;
            }
        }

        #endregion

        #region ヘルパー

        /// <summary>指定されたインデックスがストレス音節内にあるかどうかを判定する。</summary>
        private static bool IsInStressedSyllable(string word, int charIndex, IReadOnlyList<PortugueseSyllable> syllables)
        {
            for (var i = 0; i < syllables.Count; i++)
            {
                var syl = syllables[i];
                if (charIndex >= syl.StartIndex && charIndex < syl.StartIndex + syl.Length)
                    return syl.IsStressed;
            }
            return false;
        }

        #endregion
    }
}
