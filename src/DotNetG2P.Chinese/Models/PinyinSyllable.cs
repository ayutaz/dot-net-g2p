using System;

namespace DotNetG2P.Chinese
{
    /// <summary>
    /// ピンイン音節を表す構造体。
    /// 声母（Initial）+ 韻母（Final）+ 声調（Tone）で1音節を構成する。
    /// </summary>
    public readonly struct PinyinSyllable : IEquatable<PinyinSyllable>
    {
        /// <summary>声母</summary>
        public Initial Initial { get; }

        /// <summary>韻母</summary>
        public Final Final { get; }

        /// <summary>声調</summary>
        public Tone Tone { get; }

        /// <summary>
        /// ピンイン音節を生成する。
        /// </summary>
        /// <param name="initial">声母</param>
        /// <param name="final_">韻母</param>
        /// <param name="tone">声調</param>
        public PinyinSyllable(Initial initial, Final final_, Tone tone)
        {
            Initial = initial;
            Final = final_;
            Tone = tone;
        }

        /// <summary>声母があるかどうか</summary>
        public bool HasInitial => Initial != Initial.None;

        /// <summary>軽声かどうか</summary>
        public bool IsNeutralTone => Tone == Tone.Neutral;

        /// <summary>
        /// ピンイン数字表記を返す（例: "zhong1", "guo2", "a1"）。
        /// </summary>
        public override string ToString()
        {
            var ini = InitialToString(Initial);
            var fin = FinalToString(Initial, Final);
            var tone = Tone == Tone.Neutral ? "" : ((int)Tone).ToString();
            return ini + fin + tone;
        }

        /// <summary>
        /// 声母をピンイン文字列に変換する。
        /// </summary>
        internal static string InitialToString(Initial initial)
        {
            switch (initial)
            {
                case Initial.None: return "";
                case Initial.B: return "b";
                case Initial.P: return "p";
                case Initial.M: return "m";
                case Initial.F: return "f";
                case Initial.D: return "d";
                case Initial.T: return "t";
                case Initial.N: return "n";
                case Initial.L: return "l";
                case Initial.G: return "g";
                case Initial.K: return "k";
                case Initial.H: return "h";
                case Initial.J: return "j";
                case Initial.Q: return "q";
                case Initial.X: return "x";
                case Initial.Zh: return "zh";
                case Initial.Ch: return "ch";
                case Initial.Sh: return "sh";
                case Initial.R: return "r";
                case Initial.Z: return "z";
                case Initial.C: return "c";
                case Initial.S: return "s";
                case Initial.Y: return "y";
                case Initial.W: return "w";
                default: return "";
            }
        }

        /// <summary>
        /// 韻母をピンイン文字列に変換する。
        /// j/q/x後のü系韻母はu表記に変換する。
        /// </summary>
        internal static string FinalToString(Initial initial, Final final_)
        {
            bool isPalatal = initial == Initial.J || initial == Initial.Q || initial == Initial.X;

            switch (final_)
            {
                case Final.None: return "";
                case Final.A: return "a";
                case Final.O: return "o";
                case Final.E: return "e";
                case Final.Ai: return "ai";
                case Final.Ei: return "ei";
                case Final.Ao: return "ao";
                case Final.Ou: return "ou";
                case Final.An: return "an";
                case Final.En: return "en";
                case Final.Ang: return "ang";
                case Final.Eng: return "eng";
                case Final.Ong: return "ong";
                case Final.I: return "i";
                case Final.Ia: return "ia";
                case Final.Ie: return "ie";
                case Final.Iao: return "iao";
                case Final.Iu: return "iu";
                case Final.Ian: return "ian";
                case Final.In: return "in";
                case Final.Iang: return "iang";
                case Final.Ing: return "ing";
                case Final.Iong: return "iong";
                case Final.U: return "u";
                case Final.Ua: return "ua";
                case Final.Uo: return "uo";
                case Final.Uai: return "uai";
                case Final.Ui: return "ui";
                case Final.Uan: return "uan";
                case Final.Un: return "un";
                case Final.Uang: return "uang";
                case Final.Ueng: return "ueng";
                case Final.V: return isPalatal ? "u" : "\u00fc";
                case Final.Ve: return isPalatal ? "ue" : "\u00fce";
                case Final.Van: return isPalatal ? "uan" : "\u00fcan";
                case Final.Vn: return isPalatal ? "un" : "\u00fcn";
                case Final.Er: return "er";
                default: return "";
            }
        }

        public bool Equals(PinyinSyllable other)
            => Initial == other.Initial && Final == other.Final && Tone == other.Tone;

        public override bool Equals(object obj) => obj is PinyinSyllable other && Equals(other);

        public override int GetHashCode()
            => ((int)Initial << 16) | ((int)Final << 8) | (int)Tone;

        public static bool operator ==(PinyinSyllable left, PinyinSyllable right) => left.Equals(right);
        public static bool operator !=(PinyinSyllable left, PinyinSyllable right) => !left.Equals(right);
    }
}
