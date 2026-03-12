using System;

namespace DotNetG2P.Korean.Rules
{
    internal static class GraphemeToPhonemeRules
    {
        public static KoreanSyllable[] Convert(KoreanSyllable[] syllables)
        {
            if (syllables == null)
                throw new ArgumentNullException(nameof(syllables));
            if (syllables.Length == 0)
                return Array.Empty<KoreanSyllable>();

            var result = new KoreanSyllable[syllables.Length];
            Array.Copy(syllables, result, syllables.Length);

            AssimilationProcessor.ApplyHDeletionBeforeNasals(result);
            AssimilationProcessor.ApplyNInsertion(result);
            AssimilationProcessor.ApplyResyllabification(result);
            AssimilationProcessor.ApplyLiquidization(result);
            AssimilationProcessor.ApplyNasalization(result);
            AssimilationProcessor.ApplyTensification(result);
            AssimilationProcessor.ApplyFinalNeutralization(result);

            return result;
        }
    }
}
