using System;

namespace DotNetG2P.English
{
    /// <summary>
    /// ストレス情報付きARPAbet音素。
    /// 子音の場合 <see cref="Stress"/> は <see cref="DotNetG2P.English.Stress.None"/>。
    /// </summary>
    public readonly struct EnglishPhoneme : IEquatable<EnglishPhoneme>
    {
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
        public bool IsVowel => Phoneme <= ArpabetPhoneme.UW;

        /// <summary>
        /// ARPAbet表記文字列を返す（例: "AH0", "K"）。
        /// </summary>
        public override string ToString()
        {
            var name = ArpabetParser.PhonemeToString(Phoneme);
            if (Stress == Stress.None)
                return name;

            // ストレス値: NoStress=1→"0", Primary=2→"1", Secondary=3→"2"
            return name + ((int)Stress - 1).ToString();
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
