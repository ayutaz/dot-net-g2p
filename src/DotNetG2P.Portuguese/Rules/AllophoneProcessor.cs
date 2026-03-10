using System.Runtime.CompilerServices;

namespace DotNetG2P.Portuguese.Rules
{
    /// <summary>
    /// ポルトガル語の異音規則を適用する。
    /// 適用順序:
    ///   1. 母音弱化 (VowelReduction) — 最初に適用（後続規則に影響）
    ///   2. t/d破擦音化 (TDPalatalization) — BP専用、母音弱化後の /i/ も対象
    ///   3. 閉鎖音弱化 (Lenition) — EP向け
    ///   4. 鼻音同化 (NasalAssimilation)
    ///   5. 歯擦音有声性同化 (SibilantVoicingAssimilation)
    ///   6. 歯擦音後部歯茎化 (SibilantPalatalization) — EP/Rio向け
    ///   7. l異音 (LAllophony) — BP: l→w, EP: l→ɫ
    /// </summary>
    internal static class AllophoneProcessor
    {
        public static PortuguesePronunciation Apply(
            PortuguesePronunciation pronunciation,
            PortugueseAllophoneFeatures features,
            PortugueseDialect dialect)
        {
            if (pronunciation.PhonemesInternal.Length == 0 || features == PortugueseAllophoneFeatures.None)
                return pronunciation;

            var source = pronunciation.PhonemesInternal;
            var result = new PortuguesePhoneme[source.Length];
            for (var i = 0; i < source.Length; i++)
                result[i] = source[i];

            // 1. 母音弱化（最初に適用 — 後続の TDPalatalization 等に影響）
            if (HasFeature(features, PortugueseAllophoneFeatures.VowelReduction))
                ApplyVowelReduction(result, pronunciation.SyllableOffsetsInternal, pronunciation.StressedSyllableIndex, dialect);

            // 2. t/d破擦音化（BP専用、母音弱化後の /i/ も対象）
            if (HasFeature(features, PortugueseAllophoneFeatures.TDPalatalization))
                ApplyTDPalatalization(result);

            // 3. 閉鎖音弱化（EP向け）
            if (HasFeature(features, PortugueseAllophoneFeatures.Lenition))
                ApplyLenition(result);

            // 4. 鼻音同化
            if (HasFeature(features, PortugueseAllophoneFeatures.NasalAssimilation))
                ApplyNasalAssimilation(result);

            // 5. 歯擦音有声性同化
            if (HasFeature(features, PortugueseAllophoneFeatures.SibilantVoicingAssimilation))
                ApplySibilantVoicingAssimilation(result);

            // 6. 歯擦音後部歯茎化（EP/Rio向け）
            if (HasFeature(features, PortugueseAllophoneFeatures.SibilantPalatalization))
                ApplySibilantPalatalization(result);

            // 7. l異音（BP: 半母音化, EP: 軟口蓋化）
            if (HasFeature(features, PortugueseAllophoneFeatures.LAllophony))
                ApplyLAllophony(result, dialect);

            return new PortuguesePronunciation(result, pronunciation.SyllableOffsetsInternal, pronunciation.StressedSyllableIndex);
        }

        // ===== 規則 1: 母音弱化 =====

        /// <summary>
        /// 母音弱化: ストレスのない音節の母音を弱化する。
        /// <list type="bullet">
        ///   <item>非ストレスの E → Schwa (BP) / HighCentral (EP)</item>
        ///   <item>非ストレスの O → U (両方言)</item>
        ///   <item>非ストレスの A → Schwa (EPのみ、語末位置)</item>
        /// </list>
        /// SyllableOffsetsInternal で音節境界を判定し、StressedSyllableIndex で強勢音節を特定する。
        /// </summary>
        private static void ApplyVowelReduction(
            PortuguesePhoneme[] phonemes,
            int[] syllableOffsets,
            int stressedSyllableIndex,
            PortugueseDialect dialect)
        {
            var lastVowelIndex = FindLastVowelIndex(phonemes);

            for (var i = 0; i < phonemes.Length; i++)
            {
                // ストレス音節内の音素はスキップ
                if (IsInStressedSyllable(i, syllableOffsets, stressedSyllableIndex, phonemes.Length))
                    continue;

                var p = phonemes[i].Phoneme;
                var isWordFinal = (i == lastVowelIndex);

                switch (p)
                {
                    case PortugueseIpaPhoneme.E:
                        // BP: 非ストレス e→/ɐ/, EP: 非ストレス e→/ɨ/
                        phonemes[i] = dialect == PortugueseDialect.Brazilian
                            ? new PortuguesePhoneme(PortugueseIpaPhoneme.Schwa, false)
                            : new PortuguesePhoneme(PortugueseIpaPhoneme.HighCentral, false);
                        break;

                    case PortugueseIpaPhoneme.O:
                        // 両方言: 非ストレス o→/u/
                        phonemes[i] = new PortuguesePhoneme(PortugueseIpaPhoneme.U, false);
                        break;

                    case PortugueseIpaPhoneme.A:
                        // EP のみ、語末位置の 非ストレス a→/ɐ/
                        if (dialect == PortugueseDialect.European && isWordFinal)
                            phonemes[i] = new PortuguesePhoneme(PortugueseIpaPhoneme.Schwa, false);
                        break;
                }
            }
        }

        // ===== 規則 2: t/d破擦音化 =====

        /// <summary>
        /// t/d破擦音化（BP専用）: T + I/INasal → Ch, D + I/INasal → Jh。
        /// 母音弱化で E→Schwa (この実装ではSchwa) になった後でも、
        /// E→I に弱化された場合は対象となる（上流の弱化結果に依存）。
        /// </summary>
        private static void ApplyTDPalatalization(PortuguesePhoneme[] phonemes)
        {
            for (var i = 0; i < phonemes.Length - 1; i++)
            {
                var next = phonemes[i + 1].Phoneme;
                if (next != PortugueseIpaPhoneme.I && next != PortugueseIpaPhoneme.INasal)
                    continue;

                var current = phonemes[i].Phoneme;
                if (current == PortugueseIpaPhoneme.T)
                    phonemes[i] = new PortuguesePhoneme(PortugueseIpaPhoneme.Ch, false);
                else if (current == PortugueseIpaPhoneme.D)
                    phonemes[i] = new PortuguesePhoneme(PortugueseIpaPhoneme.Jh, false);
            }
        }

        // ===== 規則 3: 閉鎖音弱化 =====

        /// <summary>
        /// 閉鎖音弱化（EP向け）: 母音間の B→Beta, D→Dh, G→Gh。
        /// 「母音間」= 前が母音/半母音/鼻わたり音で、後が母音/半母音。
        /// </summary>
        private static void ApplyLenition(PortuguesePhoneme[] phonemes)
        {
            for (var i = 1; i < phonemes.Length; i++)
            {
                var p = phonemes[i].Phoneme;
                if (p != PortugueseIpaPhoneme.B && p != PortugueseIpaPhoneme.D && p != PortugueseIpaPhoneme.G)
                    continue;

                // 前が母音/半母音/鼻わたり音
                if (!IsVowelOrGlide(phonemes[i - 1].Phoneme))
                    continue;

                // 後が母音/半母音（配列末尾なら弱化しない）
                if (i + 1 >= phonemes.Length || !IsVowelOrSemivowel(phonemes[i + 1].Phoneme))
                    continue;

                switch (p)
                {
                    case PortugueseIpaPhoneme.B:
                        phonemes[i] = new PortuguesePhoneme(PortugueseIpaPhoneme.Beta, phonemes[i].IsStressed);
                        break;
                    case PortugueseIpaPhoneme.D:
                        phonemes[i] = new PortuguesePhoneme(PortugueseIpaPhoneme.Dh, phonemes[i].IsStressed);
                        break;
                    case PortugueseIpaPhoneme.G:
                        phonemes[i] = new PortuguesePhoneme(PortugueseIpaPhoneme.Gh, phonemes[i].IsStressed);
                        break;
                }
            }
        }

        // ===== 規則 4: 鼻音同化 =====

        /// <summary>
        /// 鼻音調音位置同化: N + 後続子音 → 調音位置を合わせる。
        /// <list type="bullet">
        ///   <item>N + labial (P/B/M/F/V) → M</item>
        ///   <item>N + labiodental (F/V) → NLabiodental</item>
        ///   <item>N + velar (K/G/Ng) → Ng</item>
        ///   <item>N + dental (T/D) → NDental</item>
        /// </list>
        /// 注: 唇歯 (F/V) は labial より優先して NLabiodental になる。
        /// </summary>
        private static void ApplyNasalAssimilation(PortuguesePhoneme[] phonemes)
        {
            for (var i = 0; i < phonemes.Length - 1; i++)
            {
                if (phonemes[i].Phoneme != PortugueseIpaPhoneme.N)
                    continue;

                var next = phonemes[i + 1].Phoneme;
                var assimilated = AssimilateNasal(next);
                if (assimilated != phonemes[i].Phoneme)
                    phonemes[i] = new PortuguesePhoneme(assimilated, phonemes[i].IsStressed);
            }
        }

        // ===== 規則 5: 歯擦音有声性同化 =====

        /// <summary>
        /// 歯擦音有声性同化: S + 有声子音 → Z, Z + 無声子音 → S。
        /// </summary>
        private static void ApplySibilantVoicingAssimilation(PortuguesePhoneme[] phonemes)
        {
            for (var i = 0; i < phonemes.Length - 1; i++)
            {
                var p = phonemes[i].Phoneme;
                if (p != PortugueseIpaPhoneme.S && p != PortugueseIpaPhoneme.Z)
                    continue;

                var next = phonemes[i + 1].Phoneme;
                if (!IsConsonant(next))
                    continue;

                if (p == PortugueseIpaPhoneme.S && IsVoicedConsonant(next))
                    phonemes[i] = new PortuguesePhoneme(PortugueseIpaPhoneme.Z, phonemes[i].IsStressed);
                else if (p == PortugueseIpaPhoneme.Z && !IsVoicedConsonant(next))
                    phonemes[i] = new PortuguesePhoneme(PortugueseIpaPhoneme.S, phonemes[i].IsStressed);
            }
        }

        // ===== 規則 6: 歯擦音後部歯茎化 =====

        /// <summary>
        /// 歯擦音後部歯茎化（EP/Rio）: 音節末の S → Sh, Z → Zh。
        /// 音節末 = 次の音節オフセットの直前、または配列末尾。
        /// </summary>
        private static void ApplySibilantPalatalization(PortuguesePhoneme[] phonemes)
        {
            for (var i = 0; i < phonemes.Length; i++)
            {
                var p = phonemes[i].Phoneme;
                if (p != PortugueseIpaPhoneme.S && p != PortugueseIpaPhoneme.Z)
                    continue;

                if (!IsCodaPosition(i, phonemes))
                    continue;

                if (p == PortugueseIpaPhoneme.S)
                    phonemes[i] = new PortuguesePhoneme(PortugueseIpaPhoneme.Sh, phonemes[i].IsStressed);
                else // Z
                    phonemes[i] = new PortuguesePhoneme(PortugueseIpaPhoneme.Zh, phonemes[i].IsStressed);
            }
        }

        // ===== 規則 7: l異音 =====

        /// <summary>
        /// コーダ l 異音: 音節末の L を方言に応じて変換する。
        /// BP: L → W（半母音化）, EP: L → DarkL（軟口蓋化）。
        /// </summary>
        private static void ApplyLAllophony(PortuguesePhoneme[] phonemes, PortugueseDialect dialect)
        {
            for (var i = 0; i < phonemes.Length; i++)
            {
                if (phonemes[i].Phoneme != PortugueseIpaPhoneme.L)
                    continue;

                if (!IsCodaPosition(i, phonemes))
                    continue;

                if (dialect == PortugueseDialect.Brazilian)
                    phonemes[i] = new PortuguesePhoneme(PortugueseIpaPhoneme.W, phonemes[i].IsStressed);
                else
                    phonemes[i] = new PortuguesePhoneme(PortugueseIpaPhoneme.DarkL, phonemes[i].IsStressed);
            }
        }

        // ===== ヘルパーメソッド =====

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasFeature(PortugueseAllophoneFeatures value, PortugueseAllophoneFeatures feature)
        {
            return (value & feature) == feature;
        }

        /// <summary>
        /// 指定インデックスの音素がストレス音節内にあるかを判定する。
        /// SyllableOffsetsInternal と StressedSyllableIndex を使用。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsInStressedSyllable(int phonemeIndex, int[] syllableOffsets, int stressedSyllableIndex, int totalPhonemes)
        {
            if (stressedSyllableIndex < 0 || syllableOffsets.Length == 0)
                return false;

            if (stressedSyllableIndex >= syllableOffsets.Length)
                return false;

            var syllStart = syllableOffsets[stressedSyllableIndex];
            var syllEnd = (stressedSyllableIndex + 1 < syllableOffsets.Length)
                ? syllableOffsets[stressedSyllableIndex + 1]
                : totalPhonemes;

            return phonemeIndex >= syllStart && phonemeIndex < syllEnd;
        }

        /// <summary>
        /// コーダ位置（音節末）かどうかを判定する。
        /// 語末、または後続が子音の場合にコーダとみなす。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsCodaPosition(int index, PortuguesePhoneme[] phonemes)
        {
            // 語末
            if (index == phonemes.Length - 1)
                return true;

            // 後続が子音
            return IsConsonant(phonemes[index + 1].Phoneme);
        }

        /// <summary>
        /// 音素配列中の最後の母音インデックスを返す。語末母音判定に使用。
        /// </summary>
        private static int FindLastVowelIndex(PortuguesePhoneme[] phonemes)
        {
            for (var i = phonemes.Length - 1; i >= 0; i--)
            {
                if (IsVowel(phonemes[i].Phoneme))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 母音/半母音/鼻わたり音 かどうかを判定する（Lenition の「前が…」条件）。
        /// A-HighCentral, ANasal-UNasal, J, W, WNasal, JNasal。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsVowelOrGlide(PortugueseIpaPhoneme p)
        {
            // 口母音 (0-8) + 鼻母音 (9-13) + 半母音 (14-15)
            return p <= PortugueseIpaPhoneme.W
                || p == PortugueseIpaPhoneme.WNasal
                || p == PortugueseIpaPhoneme.JNasal;
        }

        /// <summary>
        /// 母音/半母音 かどうかを判定する（Lenition の「後が…」条件）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsVowelOrSemivowel(PortugueseIpaPhoneme p)
        {
            // 口母音 (0-8) + 鼻母音 (9-13) + 半母音 (14-15)
            return p <= PortugueseIpaPhoneme.W;
        }

        /// <summary>
        /// 口母音または鼻母音かどうかを判定する。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsVowel(PortugueseIpaPhoneme p)
        {
            return p <= PortugueseIpaPhoneme.UNasal;
        }

        /// <summary>
        /// 子音かどうかを判定する（母音でも半母音でもない）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsConsonant(PortugueseIpaPhoneme p)
        {
            // 半母音 (J=14, W=15) と鼻わたり音 (WNasal=47, JNasal=48) は子音でない
            if (p <= PortugueseIpaPhoneme.UNasal)
                return false; // 母音
            if (p == PortugueseIpaPhoneme.J || p == PortugueseIpaPhoneme.W)
                return false; // 半母音
            if (p == PortugueseIpaPhoneme.WNasal || p == PortugueseIpaPhoneme.JNasal)
                return false; // 鼻わたり音
            return true;
        }

        /// <summary>
        /// 鼻音かどうかを判定する。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNasal(PortugueseIpaPhoneme p)
        {
            switch (p)
            {
                case PortugueseIpaPhoneme.M:
                case PortugueseIpaPhoneme.N:
                case PortugueseIpaPhoneme.Ny:
                case PortugueseIpaPhoneme.Ng:
                case PortugueseIpaPhoneme.NLabiodental:
                case PortugueseIpaPhoneme.NDental:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 有声子音かどうかを判定する。
        /// </summary>
        private static bool IsVoicedConsonant(PortugueseIpaPhoneme p)
        {
            switch (p)
            {
                case PortugueseIpaPhoneme.B:
                case PortugueseIpaPhoneme.D:
                case PortugueseIpaPhoneme.G:
                case PortugueseIpaPhoneme.V:
                case PortugueseIpaPhoneme.Z:
                case PortugueseIpaPhoneme.Zh:
                case PortugueseIpaPhoneme.M:
                case PortugueseIpaPhoneme.N:
                case PortugueseIpaPhoneme.Ny:
                case PortugueseIpaPhoneme.Ng:
                case PortugueseIpaPhoneme.NLabiodental:
                case PortugueseIpaPhoneme.NDental:
                case PortugueseIpaPhoneme.L:
                case PortugueseIpaPhoneme.Lh:
                case PortugueseIpaPhoneme.R:
                case PortugueseIpaPhoneme.Rr:
                case PortugueseIpaPhoneme.Beta:
                case PortugueseIpaPhoneme.Dh:
                case PortugueseIpaPhoneme.Gh:
                case PortugueseIpaPhoneme.Jh:
                case PortugueseIpaPhoneme.DarkL:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 鼻音の調音位置同化先を返す。
        /// </summary>
        private static PortugueseIpaPhoneme AssimilateNasal(PortugueseIpaPhoneme next)
        {
            switch (next)
            {
                // 唇歯: f/v → ɱ（labial より優先）
                case PortugueseIpaPhoneme.F:
                case PortugueseIpaPhoneme.V:
                    return PortugueseIpaPhoneme.NLabiodental;

                // 両唇: p/b/m → m
                case PortugueseIpaPhoneme.P:
                case PortugueseIpaPhoneme.B:
                case PortugueseIpaPhoneme.Beta:
                case PortugueseIpaPhoneme.M:
                    return PortugueseIpaPhoneme.M;

                // 軟口蓋: k/g/ŋ → ŋ
                case PortugueseIpaPhoneme.K:
                case PortugueseIpaPhoneme.G:
                case PortugueseIpaPhoneme.Gh:
                case PortugueseIpaPhoneme.Ng:
                    return PortugueseIpaPhoneme.Ng;

                // 歯茎/後部歯茎: t/d/ʃ/ʒ/tʃ/dʒ → n̪
                case PortugueseIpaPhoneme.T:
                case PortugueseIpaPhoneme.D:
                case PortugueseIpaPhoneme.Dh:
                    return PortugueseIpaPhoneme.NDental;

                // 後部歯茎: ʃ/ʒ/tʃ/dʒ → n̠(NDental)
                case PortugueseIpaPhoneme.Sh:
                case PortugueseIpaPhoneme.Zh:
                case PortugueseIpaPhoneme.Ch:
                case PortugueseIpaPhoneme.Jh:
                    return PortugueseIpaPhoneme.NDental;

                // それ以外は変化なし（デフォルト n）
                default:
                    return PortugueseIpaPhoneme.N;
            }
        }
    }
}
