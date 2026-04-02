using System;
using System.Collections.Generic;

namespace DotNetG2P.Swedish.Rules
{
    /// <summary>
    /// スウェーデン語のルールベース書記素→音素変換。
    /// 5フェーズ（トリグラフ/ダイグラフ認識、子音軟化、母音変換、そり舌化、黙字処理）を
    /// 統合走査で処理する。
    /// </summary>
    internal static class GraphemeToPhonemeRules
    {
        /// <summary>
        /// 単語をスウェーデン語のG2Pルールに基づいて音素列に変換する。
        /// </summary>
        /// <param name="word">変換対象の単語</param>
        /// <returns>音素列・音節分割情報を含む発音情報</returns>
        internal static SwedishPronunciation ConvertWord(string word)
        {
            if (string.IsNullOrEmpty(word))
                return new SwedishPronunciation(Array.Empty<SwedishPhoneme>(), Array.Empty<int>(), -1);

            var lower = word.ToLowerInvariant();
            var phonemes = new List<SwedishPhoneme>(lower.Length + 2);

            // Phase 1-3: トリグラフ/ダイグラフ認識 + 子音軟化 + 母音変換（統合走査）
            var i = 0;
            while (i < lower.Length)
            {
                var consumed = TryMultigraph(lower, i, phonemes);
                if (consumed > 0)
                {
                    i += consumed;
                    continue;
                }

                consumed = TrySingleChar(lower, i, phonemes);
                i += consumed;
            }

            // Phase 4: そり舌化（音素列上で処理）
            ApplyRetroflexion(phonemes);

            // Phase 5: 語末 -ig/-lig の g 黙字
            ApplyFinalGSilence(lower, phonemes);

            return new SwedishPronunciation(
                phonemes.ToArray(),
                new[] { 0 }, // 音節オフセットはSyllabifierが後で設定
                -1);         // ストレスはStressAssignerが後で設定
        }

        /// <summary>
        /// Phase 1: トリグラフ/ダイグラフ認識（最長一致）。
        /// 3文字パターン → 2文字パターンの順に試行する。
        /// </summary>
        private static int TryMultigraph(string word, int i, List<SwedishPhoneme> output)
        {
            var remaining = word.Length - i;

            // --- 3文字パターン ---
            if (remaining >= 3)
            {
                var tri = word.Substring(i, 3);
                switch (tri)
                {
                    case "stj":
                        output.Add(P(SwedishIpaPhoneme.Sj));
                        return 3;
                    case "skj":
                        output.Add(P(SwedishIpaPhoneme.Sj));
                        return 3;
                    case "sch":
                        output.Add(P(SwedishIpaPhoneme.Sj));
                        return 3;
                }
            }

            // --- 2文字パターン ---
            if (remaining >= 2)
            {
                var di = word.Substring(i, 2);
                switch (di)
                {
                    case "sj":
                        output.Add(P(SwedishIpaPhoneme.Sj));
                        return 2;
                    case "tj":
                        output.Add(P(SwedishIpaPhoneme.Tj));
                        return 2;
                    case "kj":
                        output.Add(P(SwedishIpaPhoneme.Tj));
                        return 2;
                    case "dj":
                        output.Add(P(SwedishIpaPhoneme.J));
                        return 2;
                    case "gj":
                        output.Add(P(SwedishIpaPhoneme.J));
                        return 2;
                    case "hj":
                        output.Add(P(SwedishIpaPhoneme.J));
                        return 2;
                    case "lj":
                        output.Add(P(SwedishIpaPhoneme.J));
                        return 2;
                    case "ng":
                        output.Add(P(SwedishIpaPhoneme.Ng));
                        return 2;
                    case "ck":
                        // 重子音 k の正書法表記
                        output.Add(P(SwedishIpaPhoneme.K));
                        return 2;
                }

                // nk → ŋk（軟口蓋鼻音 + k）
                if (di == "nk")
                {
                    output.Add(P(SwedishIpaPhoneme.Ng));
                    output.Add(P(SwedishIpaPhoneme.K));
                    return 2;
                }

                // sk + 軟母音 → sj音
                if (di == "sk" && i + 2 < word.Length && SwedishOrthography.IsSoftVowel(word[i + 2]))
                {
                    output.Add(P(SwedishIpaPhoneme.Sj));
                    return 2;
                }
            }

            return 0;
        }

        /// <summary>
        /// Phase 2 および Phase 3: 単一文字の子音軟化 + 母音変換。
        /// k/g + 軟母音の軟化と、相補的数量法則に基づく母音の長短判定を行う。
        /// </summary>
        private static int TrySingleChar(string word, int i, List<SwedishPhoneme> output)
        {
            var c = word[i];

            // --- Phase 2: 子音軟化 ---

            // k + 軟母音 → tj音 /ɕ/
            if (c == 'k' && i + 1 < word.Length && SwedishOrthography.IsSoftVowel(word[i + 1]))
            {
                output.Add(P(SwedishIpaPhoneme.Tj));
                return 1;
            }

            // g + 軟母音 → /j/（語頭、または前に母音がない位置のみ軟化。語中は条件付き）
            if (c == 'g' && i + 1 < word.Length && SwedishOrthography.IsSoftVowel(word[i + 1]))
            {
                if (i == 0 || !SwedishOrthography.IsVowelChar(word[i - 1]))
                {
                    output.Add(P(SwedishIpaPhoneme.J));
                    return 1;
                }
            }

            // --- Phase 3: 母音変換 ---
            if (SwedishOrthography.IsVowelChar(c))
            {
                var isLong = IsLongVowelContext(word, i);
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
        /// 開音節（V+単子音またはV+語末）→長母音、閉音節（V+CC）→短母音。
        /// </summary>
        private static bool IsLongVowelContext(string word, int vowelIndex)
        {
            // 語末母音 → 長い
            if (vowelIndex == word.Length - 1)
                return true;

            // 母音の後に子音がない（母音連続 = hiatus）→ 長い
            if (vowelIndex + 1 < word.Length && !SwedishOrthography.IsConsonantChar(word[vowelIndex + 1]))
                return true;

            // 母音の後に子音が1つ以上
            if (vowelIndex + 1 < word.Length && SwedishOrthography.IsConsonantChar(word[vowelIndex + 1]))
            {
                // 後続に2子音以上（二重子音・子音クラスタ）→ 短い
                if (vowelIndex + 2 < word.Length && SwedishOrthography.IsConsonantChar(word[vowelIndex + 2]))
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
                case 'o': return isLong ? SwedishIpaPhoneme.LongU : SwedishIpaPhoneme.ShortO;       // o の長母音は /uː/
                case 'u': return isLong ? SwedishIpaPhoneme.LongUCentral : SwedishIpaPhoneme.ShortUCentral;
                case 'y': return isLong ? SwedishIpaPhoneme.LongY : SwedishIpaPhoneme.ShortY;
                case '\u00e5': return isLong ? SwedishIpaPhoneme.LongO : SwedishIpaPhoneme.ShortO;  // å → /oː/ or /ɔ/
                case '\u00e4': return isLong ? SwedishIpaPhoneme.LongEh : SwedishIpaPhoneme.ShortE; // ä → /ɛː/ or /ɛ/
                case '\u00f6': return isLong ? SwedishIpaPhoneme.LongOe : SwedishIpaPhoneme.ShortOe; // ö → /øː/ or /œ/
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
                case 'c': return SwedishIpaPhoneme.K;  // デフォルト k。外来語の s 判定はSw2で対応
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
                case 'v': return SwedishIpaPhoneme.V;
                case 'w': return SwedishIpaPhoneme.V;
                case 'x': return SwedishIpaPhoneme.K;  // x → ks 簡略化: k のみ出力（Sw2で改善予定）
                case 'z': return SwedishIpaPhoneme.S;
                default: return null;
            }
        }

        /// <summary>
        /// Phase 4: そり舌化。
        /// 音素列上で r + 歯茎子音（t, d, n, l, s）のペアをそり舌音に変換する。
        /// 後方から走査して r を削除し、歯茎子音をそり舌音に置き換える。
        /// </summary>
        private static void ApplyRetroflexion(List<SwedishPhoneme> phonemes)
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
                    phonemes.RemoveAt(i); // r を削除
                }
            }
        }

        /// <summary>
        /// Phase 5: 語末 -ig/-lig の g 黙字。
        /// 語末が -ig または -lig で終わる場合、最後の g を音素列から削除する。
        /// </summary>
        private static void ApplyFinalGSilence(string word, List<SwedishPhoneme> phonemes)
        {
            if (phonemes.Count < 2) return;

            if ((word.EndsWith("ig") || word.EndsWith("lig"))
                && phonemes[phonemes.Count - 1].Phoneme == SwedishIpaPhoneme.G)
            {
                phonemes.RemoveAt(phonemes.Count - 1);
            }
        }

        /// <summary>SwedishPhoneme のファクトリヘルパー。</summary>
        private static SwedishPhoneme P(SwedishIpaPhoneme phoneme) =>
            new SwedishPhoneme(phoneme);
    }
}
