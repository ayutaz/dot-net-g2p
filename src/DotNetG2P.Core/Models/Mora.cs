using System;

namespace DotNetG2P.Models
{
    /// <summary>
    /// モーラ（日本語の音節単位）を表す構造体。
    /// 子音（オプション）+ 母音で1モーラを構成する。
    /// 撥音(ン)や促音(ッ)は子音のみで母音なし。
    /// </summary>
    public readonly struct Mora : IEquatable<Mora>
    {
        /// <summary>子音（母音のみのモーラはnull）</summary>
        public Consonant? Consonant { get; }

        /// <summary>母音（撥音・促音はnull）</summary>
        public Vowel? Vowel { get; }

        /// <summary>モーラの種類</summary>
        public MoraKind Kind { get; }

        public Mora(Consonant? consonant, Vowel? vowel, MoraKind kind)
        {
            Consonant = consonant;
            Vowel = vowel;
            Kind = kind;
        }

        /// <summary>有声モーラかどうか</summary>
        public bool IsVoiced => Vowel == null || !Vowel.Value.IsUnvoiced();

        /// <summary>撥音(ン)かどうか</summary>
        public bool IsNN => Consonant == Models.Consonant.Nn && Vowel == null;

        /// <summary>促音(ッ)かどうか</summary>
        public bool IsCl => Consonant == Models.Consonant.Cl && Vowel == null;

        /// <summary>長音(ー)かどうか</summary>
        public bool IsLong => Consonant == Models.Consonant.Long && Vowel == null;

        /// <summary>
        /// 音素文字列を返す。例: "k a", "sh i", "N", "cl"
        /// </summary>
        public string ToPhonemeString()
        {
            if (Consonant == null && Vowel == null)
                return "";

            if (Consonant == null)
                return Vowel!.Value.ToSymbol();

            if (Vowel == null)
                return Consonant.Value.ToSymbol();

            return $"{Consonant.Value.ToSymbol()} {Vowel.Value.ToSymbol()}";
        }

        public bool Equals(Mora other)
            => Consonant == other.Consonant && Vowel == other.Vowel && Kind == other.Kind;

        public override bool Equals(object obj) => obj is Mora other && Equals(other);
        public override int GetHashCode() => (Consonant, Vowel, Kind).GetHashCode();
        public static bool operator ==(Mora left, Mora right) => left.Equals(right);
        public static bool operator !=(Mora left, Mora right) => !left.Equals(right);

        public override string ToString() => $"{Kind.ToKatakana()} [{ToPhonemeString()}]";
    }
}
