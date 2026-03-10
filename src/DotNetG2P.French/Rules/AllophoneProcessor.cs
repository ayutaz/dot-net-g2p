namespace DotNetG2P.French.Rules
{
    /// <summary>
    /// フランス語の異音規則を適用する。
    /// </summary>
    internal static class AllophoneProcessor
    {
        public static FrenchPronunciation Apply(FrenchPronunciation pronunciation, FrenchAllophoneFeatures features)
        {
            if (pronunciation.PhonemesInternal.Length == 0 || features == FrenchAllophoneFeatures.None)
                return pronunciation;

            var source = pronunciation.PhonemesInternal;
            var result = new FrenchPhoneme[source.Length];
            for (var i = 0; i < source.Length; i++)
                result[i] = source[i];

            if (HasFeature(features, FrenchAllophoneFeatures.RDevoicing))
                ApplyRDevoicing(result);

            if (HasFeature(features, FrenchAllophoneFeatures.ObstruentVoicingAssimilation))
                ApplyObstruentVoicingAssimilation(result);

            return new FrenchPronunciation(result, pronunciation.SyllableOffsetsInternal, pronunciation.StressedSyllableIndex);
        }

        /// <summary>
        /// /ʁ/ が無声阻害音に隣接している場合、/χ/ に無声化する。
        /// 語末位置の R は無声化しない。
        /// </summary>
        private static void ApplyRDevoicing(FrenchPhoneme[] phonemes)
        {
            for (var i = 0; i < phonemes.Length; i++)
            {
                if (phonemes[i].Phoneme != FrenchIpaPhoneme.R)
                    continue;

                // 語末位置のRは無声化しない
                if (i == phonemes.Length - 1)
                    continue;

                var afterVoiceless = i > 0 && IsVoicelessObstruent(phonemes[i - 1].Phoneme);
                var beforeVoiceless = i + 1 < phonemes.Length && IsVoicelessObstruent(phonemes[i + 1].Phoneme);

                if (afterVoiceless || beforeVoiceless)
                    phonemes[i] = new FrenchPhoneme(FrenchIpaPhoneme.Rh, phonemes[i].IsSyllableNucleus);
            }
        }

        /// <summary>
        /// 阻害音クラスタ内で逆行同化を適用する。
        /// 後ろの阻害音の有声性に前の阻害音を統一する。
        /// </summary>
        private static void ApplyObstruentVoicingAssimilation(FrenchPhoneme[] phonemes)
        {
            for (var i = phonemes.Length - 2; i >= 0; i--)
            {
                if (!IsObstruent(phonemes[i].Phoneme) || !IsObstruent(phonemes[i + 1].Phoneme))
                    continue;

                var currentVoiced = IsVoicedObstruent(phonemes[i].Phoneme);
                var nextVoiced = IsVoicedObstruent(phonemes[i + 1].Phoneme);

                if (currentVoiced == nextVoiced)
                    continue;

                var transformed = nextVoiced
                    ? Voice(phonemes[i].Phoneme)
                    : Devoice(phonemes[i].Phoneme);

                phonemes[i] = new FrenchPhoneme(transformed, phonemes[i].IsSyllableNucleus);
            }
        }

        private static bool HasFeature(FrenchAllophoneFeatures value, FrenchAllophoneFeatures feature)
        {
            return (value & feature) == feature;
        }

        private static bool IsVoicelessObstruent(FrenchIpaPhoneme p)
        {
            switch (p)
            {
                case FrenchIpaPhoneme.P:
                case FrenchIpaPhoneme.T:
                case FrenchIpaPhoneme.K:
                case FrenchIpaPhoneme.F:
                case FrenchIpaPhoneme.S:
                case FrenchIpaPhoneme.Sh:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsVoicedObstruent(FrenchIpaPhoneme p)
        {
            switch (p)
            {
                case FrenchIpaPhoneme.B:
                case FrenchIpaPhoneme.D:
                case FrenchIpaPhoneme.G:
                case FrenchIpaPhoneme.V:
                case FrenchIpaPhoneme.Z:
                case FrenchIpaPhoneme.Zh:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsObstruent(FrenchIpaPhoneme p)
        {
            return IsVoicelessObstruent(p) || IsVoicedObstruent(p);
        }

        private static FrenchIpaPhoneme Devoice(FrenchIpaPhoneme p)
        {
            switch (p)
            {
                case FrenchIpaPhoneme.B: return FrenchIpaPhoneme.P;
                case FrenchIpaPhoneme.D: return FrenchIpaPhoneme.T;
                case FrenchIpaPhoneme.G: return FrenchIpaPhoneme.K;
                case FrenchIpaPhoneme.V: return FrenchIpaPhoneme.F;
                case FrenchIpaPhoneme.Z: return FrenchIpaPhoneme.S;
                case FrenchIpaPhoneme.Zh: return FrenchIpaPhoneme.Sh;
                default: return p;
            }
        }

        private static FrenchIpaPhoneme Voice(FrenchIpaPhoneme p)
        {
            switch (p)
            {
                case FrenchIpaPhoneme.P: return FrenchIpaPhoneme.B;
                case FrenchIpaPhoneme.T: return FrenchIpaPhoneme.D;
                case FrenchIpaPhoneme.K: return FrenchIpaPhoneme.G;
                case FrenchIpaPhoneme.F: return FrenchIpaPhoneme.V;
                case FrenchIpaPhoneme.S: return FrenchIpaPhoneme.Z;
                case FrenchIpaPhoneme.Sh: return FrenchIpaPhoneme.Zh;
                default: return p;
            }
        }
    }
}
