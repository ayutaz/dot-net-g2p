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

            // 入力は小文字化済みを前提とする（呼び出し元 GetWords で変換済み）
            // 1. 音節分割 + ストレス付与（先に決定）
            var syllables = StressAssigner.MarkStress(word, SwedishSyllabifier.Syllabify(word));

            // 2. 音節ごとにG2P変換（オフセットを自然追跡）
            var phonemes = new List<SwedishPhoneme>(word.Length + 4);
            var syllableOffsets = new int[syllables.Count];
            var stressedIndex = -1;

            for (var si = 0; si < syllables.Count; si++)
            {
                syllableOffsets[si] = phonemes.Count;
                if (syllables[si].IsStressed)
                    stressedIndex = si;

                var syl = syllables[si];
                AppendSyllable(word, syl.StartIndex, syl.StartIndex + syl.Length,
                    syl.IsStressed, phonemes, dialect);
            }

            // 3. 後処理
            // Phase 4: そり舌化（常に適用。FinlandSwedishの場合はAllophoneProcessorで戻す）
            ApplyRetroflexion(phonemes, syllableOffsets);

            ApplyFinalGWeakening(word, phonemes);

            var pron = new SwedishPronunciation(phonemes.ToArray(), syllableOffsets, stressedIndex);
            pron.Accent = StressAssigner.AssignAccent(word, syllables, 0);
            return pron;
        }

        /// <summary>音節範囲のG2P変換。</summary>
        private static void AppendSyllable(string word, int start, int end,
            bool isStressed, List<SwedishPhoneme> output, SwedishDialect dialect)
        {
            var i = start;
            while (i < end)
            {
                // Phase 0: 重子音（同一子音連続）→ 1音素に縮約。
                // ck は TryMultigraph でダイグラフ処理済み、ng はダイグラフ /ŋ/ のため除外。
                if (i + 1 < end && word[i] == word[i + 1]
                    && SwedishOrthography.IsConsonantChar(word[i]))
                {
                    // 最初の子音を通常処理し、2文字目をスキップ
                    var consumed = TrySingleChar(word, i, end, isStressed, output);
                    i += consumed + 1; // +1 で重複分をスキップ
                    continue;
                }

                var multigraphConsumed = TryMultigraph(word, i, end, start == 0 && i == start, i == start, output);
                if (multigraphConsumed > 0)
                {
                    i += multigraphConsumed;
                    continue;
                }

                var singleConsumed = TrySingleChar(word, i, end, isStressed, output);
                i += singleConsumed;
            }
        }

        /// <summary>
        /// Phase 1: トリグラフ/ダイグラフ認識（最長一致）。
        /// 3文字パターン → 2文字パターンの順に試行する。
        /// </summary>
        /// <param name="word">対象単語（小文字化済み）。</param>
        /// <param name="i">現在位置。</param>
        /// <param name="end">音節末尾インデックス。</param>
        /// <param name="isWordInitial">語頭位置か（gn/ps/pn 黙字判定に使用）。</param>
        /// <param name="isSyllableStart">音節先頭か（将来拡張用）。</param>
        /// <param name="output">出力リスト。</param>
        private static int TryMultigraph(string word, int i, int end,
            bool isWordInitial, bool isSyllableStart, List<SwedishPhoneme> output)
        {
            var remaining = end - i;
            var c0 = word[i];
            var c1 = remaining >= 2 ? word[i + 1] : '\0';
            var c2 = remaining >= 3 ? word[i + 2] : '\0';

            // --- 語頭の黙字ダイグラフ (gn, ps, pn) ---
            if (isWordInitial && remaining >= 2)
            {
                // gn → /n/ (gnista, gnälla)
                if (c0 == 'g' && c1 == 'n')
                {
                    output.Add(P(SwedishIpaPhoneme.N));
                    return 2;
                }
                // ps → /s/ (psykolog, psalm)
                if (c0 == 'p' && c1 == 's')
                {
                    output.Add(P(SwedishIpaPhoneme.S));
                    return 2;
                }
                // pn → /n/ (pneumoni)
                if (c0 == 'p' && c1 == 'n')
                {
                    output.Add(P(SwedishIpaPhoneme.N));
                    return 2;
                }
            }

            // --- -tion/-sion 接尾辞: ti/si + on → /ɧ/ + on ---
            // ti/si の位置から on が続き語末（または接尾辞 -ell/-är 等の前）で sj音化
            if (remaining >= 2 && (c0 == 't' || c0 == 's') && c1 == 'i')
            {
                // i の次に 'on' が続くか確認（音節境界をまたぐ可能性があるので word 全体で確認）
                var onPos = i + 2;
                if (onPos + 1 < word.Length && word[onPos] == 'o' && word[onPos + 1] == 'n')
                {
                    // 語末の -tion/-sion、または -tionell/-tionär 等
                    var afterOn = onPos + 2;
                    if (afterOn == word.Length
                        || (afterOn < word.Length && word[afterOn] == 'e')   // -tionell
                        || (afterOn < word.Length && word[afterOn] == '\u00e4') // -tionär (ä)
                        || (afterOn < word.Length && word[afterOn] == 's'))  // -tions
                    {
                        output.Add(P(SwedishIpaPhoneme.Sj));
                        return 2; // ti/si を消費、on は後続処理
                    }
                }
            }

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

            // g軟化: 音節先頭の g + 軟母音 → /j/
            // ng ダイグラフは TryMultigraph で先に処理されるため誤検出しない
            if (c == 'g' && i + 1 < word.Length && SwedishOrthography.IsSoftVowel(word[i + 1]))
            {
                // 直前が子音でない場合（語頭 or 母音後）に軟化
                // 直前に n がある場合は ng ダイグラフが先に処理されるのでここに到達しない
                if (i == 0 || !SwedishOrthography.IsConsonantChar(word[i - 1]))
                {
                    output.Add(P(SwedishIpaPhoneme.J));
                    return 1;
                }
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
                output.Add(P(MapVowel(c, isLong, word, i)));
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
        /// o は後続子音文脈に依存して /uː/~/ʊ/ または /oː/~/ɔ/ を返す。
        /// </summary>
        private static SwedishIpaPhoneme MapVowel(char c, bool isLong, string word, int pos)
        {
            switch (c)
            {
                case 'a': return isLong ? SwedishIpaPhoneme.LongA : SwedishIpaPhoneme.ShortA;
                case 'e': return isLong ? SwedishIpaPhoneme.LongE : SwedishIpaPhoneme.ShortE;
                case 'i': return isLong ? SwedishIpaPhoneme.LongI : SwedishIpaPhoneme.ShortI;
                case 'o': return IsOBeforeR(word, pos)
                    ? (isLong ? SwedishIpaPhoneme.LongO : SwedishIpaPhoneme.ShortO)    // o+r文脈 → oː/ɔ
                    : (isLong ? SwedishIpaPhoneme.LongU : SwedishIpaPhoneme.ShortU);   // デフォルト → uː/ʊ
                case 'u': return isLong ? SwedishIpaPhoneme.LongUCentral : SwedishIpaPhoneme.ShortUCentral;
                case 'y': return isLong ? SwedishIpaPhoneme.LongY : SwedishIpaPhoneme.ShortY;
                case '\u00e5': return isLong ? SwedishIpaPhoneme.LongO : SwedishIpaPhoneme.ShortO;     // å → oː/ɔ
                case '\u00e4': return isLong ? SwedishIpaPhoneme.LongEh : SwedishIpaPhoneme.ShortE;    // ä → ɛː/ɛ
                case '\u00f6': return isLong ? SwedishIpaPhoneme.LongOe : SwedishIpaPhoneme.ShortOe;   // ö → øː/œ
                default: return SwedishIpaPhoneme.Schwa;
            }
        }

        /// <summary>
        /// o が r 系文脈の前にあるか判定する。
        /// or, ord, ort, orn, ors, orl 等のパターンで o → /oː/ or /ɔ/ になる。
        /// </summary>
        private static bool IsOBeforeR(string word, int pos)
        {
            var next = pos + 1;
            if (next >= word.Length)
                return false;

            // 直後が r なら r 文脈
            if (word[next] == 'r')
                return true;

            // rr（重子音縮約前の位置では rr として現れる場合もある）
            // ※ 重子音は Phase 0 で処理されるため、ここでは r 単体で十分

            return false;
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
