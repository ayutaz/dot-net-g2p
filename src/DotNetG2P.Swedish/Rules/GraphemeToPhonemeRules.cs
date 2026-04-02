using System;
using System.Collections.Generic;

namespace DotNetG2P.Swedish.Rules
{
    /// <summary>
    /// スウェーデン語のルールベース書記素→音素変換。
    /// 音節分割→ストレス→音節ごとG2P変換→後処理を一括実行し、
    /// 完成品の SwedishPronunciation を返す（スペイン語パターン）。
    /// </summary>
    internal static class GraphemeToPhonemeRules
    {
        /// <summary>
        /// 単語をG2P変換する。入力は小文字化済みを前提とする。
        /// </summary>
        internal static SwedishPronunciation ConvertWord(string word, SwedishDialect dialect)
        {
            if (string.IsNullOrEmpty(word))
                return new SwedishPronunciation(Array.Empty<SwedishPhoneme>(), Array.Empty<int>(), -1);

            var lower = word.ToLowerInvariant();

            // 1. 音節分割 + ストレス付与（先に決定）
            var syllables = StressAssigner.MarkStress(lower, SwedishSyllabifier.Syllabify(lower));

            // 2. 音節ごとにG2P変換（オフセットを自然追跡）
            var phonemes = new List<SwedishPhoneme>(lower.Length + 4);
            var syllableOffsets = new int[syllables.Count];
            var stressedIndex = -1;

            for (var si = 0; si < syllables.Count; si++)
            {
                syllableOffsets[si] = phonemes.Count;
                if (syllables[si].IsStressed)
                    stressedIndex = si;

                var syl = syllables[si];
                AppendSyllable(lower, syl.StartIndex, syl.StartIndex + syl.Length,
                    syl.IsStressed, phonemes, dialect);
            }

            // 3. 後処理（dialect依存）
            if (dialect == SwedishDialect.Central)
            {
                ApplyRetroflexion(phonemes, syllableOffsets);
            }

            ApplyFinalGWeakening(lower, phonemes);

            return new SwedishPronunciation(phonemes.ToArray(), syllableOffsets, stressedIndex);
        }

        /// <summary>音節範囲のG2P変換。</summary>
        private static void AppendSyllable(string word, int start, int end,
            bool isStressed, List<SwedishPhoneme> output, SwedishDialect dialect)
        {
            var i = start;
            while (i < end)
            {
                var consumed = TryMultigraph(word, i, end, output);
                if (consumed > 0)
                {
                    i += consumed;
                    continue;
                }

                consumed = TrySingleChar(word, i, end, isStressed, output);
                i += consumed;
            }
        }

        /// <summary>
        /// Phase 1: トリグラフ/ダイグラフ認識（最長一致）。
        /// 3文字パターン → 2文字パターンの順に試行する。
        /// </summary>
        private static int TryMultigraph(string word, int i, int end, List<SwedishPhoneme> output)
        {
            var remaining = end - i;
            var c0 = word[i];
            var c1 = remaining >= 2 ? word[i + 1] : '\0';
            var c2 = remaining >= 3 ? word[i + 2] : '\0';

            // --- 3文字パターン ---
            if (remaining >= 3)
            {
                if (c0 == 's' && c1 == 't' && c2 == 'j')
                {
                    output.Add(P(SwedishIpaPhoneme.Sj));
                    return 3;
                }
                if (c0 == 's' && c1 == 'k' && c2 == 'j')
                {
                    output.Add(P(SwedishIpaPhoneme.Sj));
                    return 3;
                }
                if (c0 == 's' && c1 == 'c' && c2 == 'h')
                {
                    output.Add(P(SwedishIpaPhoneme.Sj));
                    return 3;
                }
            }

            // --- 2文字パターン ---
            if (remaining >= 2)
            {
                switch (c0)
                {
                    case 's':
                        if (c1 == 'j')
                        {
                            output.Add(P(SwedishIpaPhoneme.Sj));
                            return 2;
                        }
                        if (c1 == 'h')
                        {
                            output.Add(P(SwedishIpaPhoneme.Sj));
                            return 2;
                        }
                        // sk + 軟母音 → ɧ
                        if (c1 == 'k' && i + 2 < word.Length && SwedishOrthography.IsSoftVowel(word[i + 2]))
                        {
                            output.Add(P(SwedishIpaPhoneme.Sj));
                            return 2;
                        }
                        break;
                    case 't':
                        if (c1 == 'j')
                        {
                            output.Add(P(SwedishIpaPhoneme.Tj));
                            return 2;
                        }
                        break;
                    case 'k':
                        if (c1 == 'j')
                        {
                            output.Add(P(SwedishIpaPhoneme.Tj));
                            return 2;
                        }
                        break;
                    case 'd':
                        if (c1 == 'j')
                        {
                            output.Add(P(SwedishIpaPhoneme.J));
                            return 2;
                        }
                        break;
                    case 'g':
                        if (c1 == 'j')
                        {
                            output.Add(P(SwedishIpaPhoneme.J));
                            return 2;
                        }
                        break;
                    case 'h':
                        if (c1 == 'j')
                        {
                            output.Add(P(SwedishIpaPhoneme.J));
                            return 2;
                        }
                        break;
                    case 'l':
                        if (c1 == 'j')
                        {
                            output.Add(P(SwedishIpaPhoneme.J));
                            return 2;
                        }
                        break;
                    case 'n':
                        if (c1 == 'g')
                        {
                            output.Add(P(SwedishIpaPhoneme.Ng));
                            return 2;
                        }
                        if (c1 == 'k')
                        {
                            output.Add(P(SwedishIpaPhoneme.Ng));
                            output.Add(P(SwedishIpaPhoneme.K));
                            return 2;
                        }
                        break;
                    case 'c':
                        if (c1 == 'k')
                        {
                            output.Add(P(SwedishIpaPhoneme.K));
                            return 2;
                        }
                        break;
                }
            }

            return 0;
        }

        /// <summary>
        /// Phase 2 および Phase 3: 単一文字の子音軟化 + 母音変換。
        /// </summary>
        private static int TrySingleChar(string word, int i, int end,
            bool isStressed, List<SwedishPhoneme> output)
        {
            var c = word[i];

            // --- Phase 2: 子音軟化 ---

            // k + 軟母音 → tj音 /ɕ/
            if (c == 'k' && i + 1 < word.Length && SwedishOrthography.IsSoftVowel(word[i + 1]))
            {
                output.Add(P(SwedishIpaPhoneme.Tj));
                return 1;
            }

            // g軟化: 語頭のみ（語中母音間では軟化しない）
            if (c == 'g' && i == 0 && i + 1 < word.Length && SwedishOrthography.IsSoftVowel(word[i + 1]))
            {
                output.Add(P(SwedishIpaPhoneme.J));
                return 1;
            }

            // c + 軟母音 → /s/
            if (c == 'c' && i + 1 < word.Length && SwedishOrthography.IsSoftVowel(word[i + 1]))
            {
                output.Add(P(SwedishIpaPhoneme.S));
                return 1;
            }

            // x → /ks/ (2音素)
            if (c == 'x')
            {
                output.Add(P(SwedishIpaPhoneme.K));
                output.Add(P(SwedishIpaPhoneme.S));
                return 1;
            }

            // --- Phase 3: 母音変換 ---
            if (SwedishOrthography.IsVowelChar(c))
            {
                var isLong = isStressed && IsLongVowelContext(word, i);
                output.Add(P(MapVowel(c, isLong)));
                return 1;
            }

            // --- 子音マッピング ---
            var phoneme = MapConsonant(c);
            if (phoneme.HasValue)
            {
                output.Add(P(phoneme.Value));
                return 1;
            }

            // 未知文字はスキップ
            return 1;
        }

        /// <summary>
        /// 相補的数量法則に基づく長母音の文脈判定。
        /// ストレス音節内での長母音判定に使用（非ストレスは呼び出し元で短母音に確定済み）。
        /// </summary>
        private static bool IsLongVowelContext(string word, int vowelIndex)
        {
            // 語末母音（後続文字なし）→ 長い
            if (vowelIndex >= word.Length - 1)
                return true;

            var next = vowelIndex + 1;

            // 母音の後に母音（hiatus）→ 長い
            if (next < word.Length && SwedishOrthography.IsVowelChar(word[next]))
                return true;

            // x は /ks/ 相当の2子音 → 短い
            if (next < word.Length && word[next] == 'x')
                return false;

            // 子音が後続する場合
            if (next < word.Length && SwedishOrthography.IsConsonantChar(word[next]))
            {
                // 後続に2子音以上連続 → 短い
                if (next + 1 < word.Length && SwedishOrthography.IsConsonantChar(word[next + 1]))
                    return false;

                // 子音1つのみ → 長い
                return true;
            }

            return true;
        }

        /// <summary>
        /// 書記素（母音文字）をIPA音素にマッピングする。
        /// </summary>
        private static SwedishIpaPhoneme MapVowel(char c, bool isLong)
        {
            switch (c)
            {
                case 'a': return isLong ? SwedishIpaPhoneme.LongA : SwedishIpaPhoneme.ShortA;
                case 'e': return isLong ? SwedishIpaPhoneme.LongE : SwedishIpaPhoneme.ShortE;
                case 'i': return isLong ? SwedishIpaPhoneme.LongI : SwedishIpaPhoneme.ShortI;
                case 'o': return isLong ? SwedishIpaPhoneme.LongU : SwedishIpaPhoneme.ShortU;          // o → uː/ʊ
                case 'u': return isLong ? SwedishIpaPhoneme.LongUCentral : SwedishIpaPhoneme.ShortUCentral;
                case 'y': return isLong ? SwedishIpaPhoneme.LongY : SwedishIpaPhoneme.ShortY;
                case '\u00e5': return isLong ? SwedishIpaPhoneme.LongO : SwedishIpaPhoneme.ShortO;     // å → oː/ɔ
                case '\u00e4': return isLong ? SwedishIpaPhoneme.LongEh : SwedishIpaPhoneme.ShortE;    // ä → ɛː/ɛ
                case '\u00f6': return isLong ? SwedishIpaPhoneme.LongOe : SwedishIpaPhoneme.ShortOe;   // ö → øː/œ
                default: return SwedishIpaPhoneme.Schwa;
            }
        }

        /// <summary>
        /// 書記素（子音文字）をIPA音素にマッピングする。
        /// 軟化はPhase 2で処理済みのため、ここではデフォルトの硬子音値を返す。
        /// </summary>
        private static SwedishIpaPhoneme? MapConsonant(char c)
        {
            switch (c)
            {
                case 'b': return SwedishIpaPhoneme.B;
                case 'c': return SwedishIpaPhoneme.K;  // 軟化はPhase 2で処理済み
                case 'd': return SwedishIpaPhoneme.D;
                case 'f': return SwedishIpaPhoneme.F;
                case 'g': return SwedishIpaPhoneme.G;  // 軟化はPhase 2で処理済み
                case 'h': return SwedishIpaPhoneme.H;
                case 'j': return SwedishIpaPhoneme.J;
                case 'k': return SwedishIpaPhoneme.K;  // 軟化はPhase 2で処理済み
                case 'l': return SwedishIpaPhoneme.L;
                case 'm': return SwedishIpaPhoneme.M;
                case 'n': return SwedishIpaPhoneme.N;
                case 'p': return SwedishIpaPhoneme.P;
                case 'q': return SwedishIpaPhoneme.K;
                case 'r': return SwedishIpaPhoneme.R;
                case 's': return SwedishIpaPhoneme.S;
                case 't': return SwedishIpaPhoneme.T;
                case 'v':
                case 'w': return SwedishIpaPhoneme.V;
                case 'z': return SwedishIpaPhoneme.S;
                default: return null;
            }
        }

        /// <summary>
        /// Phase 4: そり舌化（Central方言のみ）。
        /// 音素列上で r + 歯茎子音（t, d, n, l, s）のペアをそり舌音に変換する。
        /// 後方から走査して r を削除し、歯茎子音をそり舌音に置き換える。
        /// オフセット連動補正あり。
        /// </summary>
        private static void ApplyRetroflexion(List<SwedishPhoneme> phonemes, int[] syllableOffsets)
        {
            for (var i = phonemes.Count - 2; i >= 0; i--)
            {
                if (phonemes[i].Phoneme != SwedishIpaPhoneme.R)
                    continue;

                SwedishIpaPhoneme? retro = null;
                switch (phonemes[i + 1].Phoneme)
                {
                    case SwedishIpaPhoneme.T: retro = SwedishIpaPhoneme.RetroT; break;
                    case SwedishIpaPhoneme.D: retro = SwedishIpaPhoneme.RetroD; break;
                    case SwedishIpaPhoneme.N: retro = SwedishIpaPhoneme.RetroN; break;
                    case SwedishIpaPhoneme.L: retro = SwedishIpaPhoneme.RetroL; break;
                    case SwedishIpaPhoneme.S: retro = SwedishIpaPhoneme.RetroS; break;
                }

                if (retro.HasValue)
                {
                    phonemes[i + 1] = new SwedishPhoneme(retro.Value, phonemes[i + 1].IsStressed);
                    phonemes.RemoveAt(i);

                    // オフセット補正: i以降の全音節オフセットを1減算
                    for (var s = 0; s < syllableOffsets.Length; s++)
                    {
                        if (syllableOffsets[s] > i)
                            syllableOffsets[s]--;
                    }
                }
            }
        }

        /// <summary>
        /// Phase 5: 語末 -ig/-lig/-igt の g 処理。
        /// -igt: g を完全削除（t はそのまま発音）。
        /// -ig/-lig: g を /j/ に弱化。
        /// </summary>
        private static void ApplyFinalGWeakening(string word, List<SwedishPhoneme> phonemes)
        {
            if (phonemes.Count < 2) return;

            // -igt: g を完全削除（t はそのまま発音）
            if (word.EndsWith("igt") && phonemes.Count >= 3)
            {
                // 末尾から2番目がGなら削除
                var gIdx = phonemes.Count - 2;
                if (phonemes[gIdx].Phoneme == SwedishIpaPhoneme.G)
                {
                    phonemes.RemoveAt(gIdx);
                    return;
                }
            }

            // -ig/-lig: g を /j/ に弱化（削除ではない）
            if (word.Length >= 3 && (word.EndsWith("ig") || word.EndsWith("lig")))
            {
                var lastIdx = phonemes.Count - 1;
                if (phonemes[lastIdx].Phoneme == SwedishIpaPhoneme.G)
                {
                    phonemes[lastIdx] = new SwedishPhoneme(SwedishIpaPhoneme.J, phonemes[lastIdx].IsStressed);
                }
            }
        }

        /// <summary>SwedishPhoneme のファクトリヘルパー。</summary>
        private static SwedishPhoneme P(SwedishIpaPhoneme phoneme)
        {
            return new SwedishPhoneme(phoneme);
        }
    }
}
