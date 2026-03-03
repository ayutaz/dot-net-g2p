using System;
using System.Collections.Generic;

namespace DotNetG2P.Models
{
    /// <summary>
    /// 日本語の子音を表す列挙型。
    /// jpreprocess の Consonant enum に準拠。
    /// </summary>
    public enum Consonant : byte
    {
        /// <summary>有声摩擦音 (ヴ行)</summary>
        V,
        /// <summary>半母音 (ワ行)</summary>
        W,
        /// <summary>流音 (ラ行)</summary>
        R,
        /// <summary>流音拗音 (リャ行)</summary>
        Ry,
        /// <summary>半母音 (ヤ行)</summary>
        Y,
        /// <summary>鼻音 (マ行)</summary>
        M,
        /// <summary>鼻音拗音 (ミャ行)</summary>
        My,
        /// <summary>無声両唇破裂音 (パ行)</summary>
        P,
        /// <summary>有声両唇破裂音 (バ行)</summary>
        B,
        /// <summary>無声声門摩擦音 (ハ行)</summary>
        H,
        /// <summary>無声唇歯摩擦音 (フ)</summary>
        F,
        /// <summary>無声両唇破裂拗音 (ピャ行)</summary>
        Py,
        /// <summary>有声両唇破裂拗音 (ビャ行)</summary>
        By,
        /// <summary>無声声門摩擦拗音 (ヒャ行)</summary>
        Hy,
        /// <summary>歯茎鼻音 (ナ行)</summary>
        N,
        /// <summary>歯茎鼻音拗音 (ニャ行)</summary>
        Ny,
        /// <summary>有声歯茎破裂音 (ダ行)</summary>
        D,
        /// <summary>無声歯茎破裂音 (タ行)</summary>
        T,
        /// <summary>有声歯茎破裂拗音 (デャ行)</summary>
        Dy,
        /// <summary>無声歯茎破裂拗音 (テャ行)</summary>
        Ty,
        /// <summary>無声歯茎破擦音 (ツ)</summary>
        Ts,
        /// <summary>無声歯茎硬口蓋破擦音 (チャ行)</summary>
        Ch,
        /// <summary>有声歯茎摩擦音 (ザ行)</summary>
        Z,
        /// <summary>無声歯茎摩擦音 (サ行)</summary>
        S,
        /// <summary>有声歯茎硬口蓋破擦音 (ジャ行)</summary>
        J,
        /// <summary>無声歯茎硬口蓋摩擦音 (シャ行)</summary>
        Sh,
        /// <summary>有声軟口蓋破裂音 (ガ行)</summary>
        G,
        /// <summary>無声軟口蓋破裂音 (カ行)</summary>
        K,
        /// <summary>有声軟口蓋破裂拗音 (ギャ行)</summary>
        Gy,
        /// <summary>無声軟口蓋破裂拗音 (キャ行)</summary>
        Ky,
        /// <summary>有声軟口蓋唇音化 (グヮ)</summary>
        Gw,
        /// <summary>無声軟口蓋唇音化 (クヮ)</summary>
        Kw,
        /// <summary>撥音 (ン)</summary>
        Nn,
        /// <summary>促音 (ッ)</summary>
        Cl,
        /// <summary>長音 (ー)</summary>
        Long,
    }

    /// <summary>
    /// 日本語の母音を表す列挙型。
    /// jpreprocess の Vowel enum に準拠。
    /// 無声母音はアクセント・韻律処理で区別される。
    /// </summary>
    public enum Vowel : byte
    {
        /// <summary>有声母音 ア</summary>
        A,
        /// <summary>有声母音 イ</summary>
        I,
        /// <summary>有声母音 ウ</summary>
        U,
        /// <summary>有声母音 エ</summary>
        E,
        /// <summary>有声母音 オ</summary>
        O,
        /// <summary>無声母音 ア</summary>
        A_Unvoiced,
        /// <summary>無声母音 イ</summary>
        I_Unvoiced,
        /// <summary>無声母音 ウ</summary>
        U_Unvoiced,
        /// <summary>無声母音 エ</summary>
        E_Unvoiced,
        /// <summary>無声母音 オ</summary>
        O_Unvoiced,
    }

    /// <summary>
    /// Consonant / Vowel enum に対する音素記号変換ユーティリティ。
    /// </summary>
    public static class PhonemeExtensions
    {
        // ====== Consonant → 文字列 ======

        private static readonly Dictionary<Consonant, string> ConsonantToSymbol = new Dictionary<Consonant, string>
        {
            { Consonant.V,    "v" },
            { Consonant.W,    "w" },
            { Consonant.R,    "r" },
            { Consonant.Ry,   "ry" },
            { Consonant.Y,    "y" },
            { Consonant.M,    "m" },
            { Consonant.My,   "my" },
            { Consonant.P,    "p" },
            { Consonant.B,    "b" },
            { Consonant.H,    "h" },
            { Consonant.F,    "f" },
            { Consonant.Py,   "py" },
            { Consonant.By,   "by" },
            { Consonant.Hy,   "hy" },
            { Consonant.N,    "n" },
            { Consonant.Ny,   "ny" },
            { Consonant.D,    "d" },
            { Consonant.T,    "t" },
            { Consonant.Dy,   "dy" },
            { Consonant.Ty,   "ty" },
            { Consonant.Ts,   "ts" },
            { Consonant.Ch,   "ch" },
            { Consonant.Z,    "z" },
            { Consonant.S,    "s" },
            { Consonant.J,    "j" },
            { Consonant.Sh,   "sh" },
            { Consonant.G,    "g" },
            { Consonant.K,    "k" },
            { Consonant.Gy,   "gy" },
            { Consonant.Ky,   "ky" },
            { Consonant.Gw,   "gw" },
            { Consonant.Kw,   "kw" },
            { Consonant.Nn,   "N" },
            { Consonant.Cl,   "cl" },
            { Consonant.Long, "-" },
        };

        // ====== 文字列 → Consonant ======

        private static readonly Dictionary<string, Consonant> SymbolToConsonant = new Dictionary<string, Consonant>(StringComparer.Ordinal);

        // ====== Vowel → 文字列 ======

        private static readonly Dictionary<Vowel, string> VowelToSymbol = new Dictionary<Vowel, string>
        {
            { Vowel.A, "a" },
            { Vowel.I, "i" },
            { Vowel.U, "u" },
            { Vowel.E, "e" },
            { Vowel.O, "o" },
            { Vowel.A_Unvoiced, "A" },
            { Vowel.I_Unvoiced, "I" },
            { Vowel.U_Unvoiced, "U" },
            { Vowel.E_Unvoiced, "E" },
            { Vowel.O_Unvoiced, "O" },
        };

        // ====== 文字列 → Vowel ======

        private static readonly Dictionary<string, Vowel> SymbolToVowel = new Dictionary<string, Vowel>(StringComparer.Ordinal);

        /// <summary>
        /// 静的コンストラクタ。逆引き辞書を構築する。
        /// </summary>
        static PhonemeExtensions()
        {
            foreach (var kv in ConsonantToSymbol)
                SymbolToConsonant[kv.Value] = kv.Key;

            foreach (var kv in VowelToSymbol)
                SymbolToVowel[kv.Value] = kv.Key;
        }

        // ====== 公開メソッド: Consonant ======

        /// <summary>
        /// 子音 enum を音素記号文字列に変換する。
        /// 例: Consonant.Sh → "sh", Consonant.Nn → "N"
        /// </summary>
        public static string ToSymbol(this Consonant consonant)
        {
            return ConsonantToSymbol[consonant];
        }

        /// <summary>
        /// 音素記号文字列から Consonant enum へ変換する。
        /// </summary>
        /// <param name="symbol">音素記号（例: "sh", "N", "cl"）</param>
        /// <returns>対応する Consonant 値</returns>
        /// <exception cref="ArgumentException">未知の音素記号の場合</exception>
        public static Consonant ParseConsonant(string symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            if (SymbolToConsonant.TryGetValue(symbol, out var result))
                return result;

            throw new ArgumentException($"未知の子音記号です: '{symbol}'", nameof(symbol));
        }

        /// <summary>
        /// 音素記号文字列から Consonant enum への変換を試みる。
        /// </summary>
        /// <param name="symbol">音素記号</param>
        /// <param name="consonant">変換結果</param>
        /// <returns>変換に成功した場合 true</returns>
        public static bool TryParseConsonant(string symbol, out Consonant consonant)
        {
            if (symbol != null && SymbolToConsonant.TryGetValue(symbol, out consonant))
                return true;

            consonant = default;
            return false;
        }

        // ====== 公開メソッド: Vowel ======

        /// <summary>
        /// 母音 enum を音素記号文字列に変換する。
        /// 有声母音は小文字、無声母音は大文字で返す。
        /// 例: Vowel.A → "a", Vowel.A_Unvoiced → "A"
        /// </summary>
        public static string ToSymbol(this Vowel vowel)
        {
            return VowelToSymbol[vowel];
        }

        /// <summary>
        /// 音素記号文字列から Vowel enum へ変換する。
        /// </summary>
        /// <param name="symbol">音素記号（例: "a", "A"）</param>
        /// <returns>対応する Vowel 値</returns>
        /// <exception cref="ArgumentException">未知の音素記号の場合</exception>
        public static Vowel ParseVowel(string symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            if (SymbolToVowel.TryGetValue(symbol, out var result))
                return result;

            throw new ArgumentException($"未知の母音記号です: '{symbol}'", nameof(symbol));
        }

        /// <summary>
        /// 音素記号文字列から Vowel enum への変換を試みる。
        /// </summary>
        /// <param name="symbol">音素記号</param>
        /// <param name="vowel">変換結果</param>
        /// <returns>変換に成功した場合 true</returns>
        public static bool TryParseVowel(string symbol, out Vowel vowel)
        {
            if (symbol != null && SymbolToVowel.TryGetValue(symbol, out vowel))
                return true;

            vowel = default;
            return false;
        }

        /// <summary>
        /// 有声母音を無声母音に変換する。
        /// 既に無声母音の場合はそのまま返す。
        /// </summary>
        public static Vowel ToUnvoiced(this Vowel vowel)
        {
            switch (vowel)
            {
                case Vowel.A: return Vowel.A_Unvoiced;
                case Vowel.I: return Vowel.I_Unvoiced;
                case Vowel.U: return Vowel.U_Unvoiced;
                case Vowel.E: return Vowel.E_Unvoiced;
                case Vowel.O: return Vowel.O_Unvoiced;
                default: return vowel;
            }
        }

        /// <summary>
        /// 無声母音を有声母音に変換する。
        /// 既に有声母音の場合はそのまま返す。
        /// </summary>
        public static Vowel ToVoiced(this Vowel vowel)
        {
            switch (vowel)
            {
                case Vowel.A_Unvoiced: return Vowel.A;
                case Vowel.I_Unvoiced: return Vowel.I;
                case Vowel.U_Unvoiced: return Vowel.U;
                case Vowel.E_Unvoiced: return Vowel.E;
                case Vowel.O_Unvoiced: return Vowel.O;
                default: return vowel;
            }
        }

        /// <summary>
        /// 母音が無声かどうかを返す。
        /// </summary>
        public static bool IsUnvoiced(this Vowel vowel)
        {
            return vowel >= Vowel.A_Unvoiced;
        }
    }
}
