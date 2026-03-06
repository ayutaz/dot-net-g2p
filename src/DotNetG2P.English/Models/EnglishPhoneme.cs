using System;
using System.Runtime.CompilerServices;

namespace DotNetG2P.English
{
    /// <summary>
    /// ストレス情報付きARPAbet音素。
    /// 子音の場合 <see cref="Stress"/> は <see cref="DotNetG2P.English.Stress.None"/>。
    /// </summary>
    public readonly struct EnglishPhoneme : IEquatable<EnglishPhoneme>
    {
        private const int PhonemeCount = 39;
        private const int StressCount = 4;

        /// <summary>全パターン（39音素 x 4ストレス = 156）の事前キャッシュ</summary>
        private static readonly string[] s_toStringCache = BuildToStringCache();

        /// <summary>ARPAbet音素</summary>
        public ArpabetPhoneme Phoneme { get; }

        /// <summary>ストレス（強勢）</summary>
        public Stress Stress { get; }

        /// <summary>
        /// EnglishPhonemeを初期化する。
        /// </summary>
        /// <param name="phoneme">ARPAbet音素</param>
        /// <param name="stress">ストレス</param>
        public EnglishPhoneme(ArpabetPhoneme phoneme, Stress stress)
        {
            Phoneme = phoneme;
            Stress = stress;
        }

        /// <summary>
        /// この音素が母音かどうかを返す。
        /// </summary>
        public bool IsVowel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Phoneme <= ArpabetPhoneme.UW;
        }

        /// <summary>
        /// ARPAbet表記文字列を返す（例: "AH0", "K"）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            var p = (int)Phoneme;
            var s = (int)Stress;
            if ((uint)p < PhonemeCount && (uint)s < StressCount)
                return s_toStringCache[p * StressCount + s];

            // 範囲外フォールバック（通常到達しない）
            var name = ArpabetParser.PhonemeToString(Phoneme);
            if (Stress == Stress.None)
                return name;
            return name + ((int)Stress - 1).ToString();
        }

        private static string[] BuildToStringCache()
        {
            var cache = new string[PhonemeCount * StressCount];
            for (int p = 0; p < PhonemeCount; p++)
            {
                var name = ArpabetParser.PhonemeToString((ArpabetPhoneme)p);
                // Stress.None (0) → 音素名のみ
                cache[p * StressCount + 0] = name;
                // Stress.NoStress (1) → name + "0"
                cache[p * StressCount + 1] = name + "0";
                // Stress.Primary (2) → name + "1"
                cache[p * StressCount + 2] = name + "1";
                // Stress.Secondary (3) → name + "2"
                cache[p * StressCount + 3] = name + "2";
            }
            return cache;
        }

        /// <inheritdoc />
        public bool Equals(EnglishPhoneme other) => Phoneme == other.Phoneme && Stress == other.Stress;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is EnglishPhoneme other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => ((int)Phoneme << 8) | (int)Stress;

        /// <summary>等価演算子</summary>
        public static bool operator ==(EnglishPhoneme left, EnglishPhoneme right) => left.Equals(right);

        /// <summary>非等価演算子</summary>
        public static bool operator !=(EnglishPhoneme left, EnglishPhoneme right) => !left.Equals(right);
    }
}
