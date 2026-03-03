using System;
using System.Collections.Generic;

namespace DotNetG2P.Models
{
    /// <summary>
    /// 日本語のモーラ（音節単位）を表す列挙型。
    /// jpreprocess の MoraEnum に準拠した約220種のバリアントを定義する。
    /// </summary>
    public enum MoraKind : ushort
    {
        // ===== ア行 =====
        /// <summary>ア</summary>
        A,
        /// <summary>ァ（小書き）</summary>
        Xa,
        /// <summary>イ</summary>
        I,
        /// <summary>ィ（小書き）</summary>
        Xi,
        /// <summary>ウ</summary>
        U,
        /// <summary>ゥ（小書き）</summary>
        Xu,
        /// <summary>エ</summary>
        E,
        /// <summary>ェ（小書き）</summary>
        Xe,
        /// <summary>オ</summary>
        O,
        /// <summary>ォ（小書き）</summary>
        Xo,

        // ===== カ行 =====
        /// <summary>カ</summary>
        Ka,
        /// <summary>キ</summary>
        Ki,
        /// <summary>キャ</summary>
        Kya,
        /// <summary>キュ</summary>
        Kyu,
        /// <summary>キョ</summary>
        Kyo,
        /// <summary>キェ</summary>
        Kye,
        /// <summary>ク</summary>
        Ku,
        /// <summary>クヮ</summary>
        Kwa,
        /// <summary>ケ</summary>
        Ke,
        /// <summary>コ</summary>
        Ko,

        // ===== ガ行 =====
        /// <summary>ガ</summary>
        Ga,
        /// <summary>ギ</summary>
        Gi,
        /// <summary>ギャ</summary>
        Gya,
        /// <summary>ギュ</summary>
        Gyu,
        /// <summary>ギョ</summary>
        Gyo,
        /// <summary>ギェ</summary>
        Gye,
        /// <summary>グ</summary>
        Gu,
        /// <summary>グヮ</summary>
        Gwa,
        /// <summary>ゲ</summary>
        Ge,
        /// <summary>ゴ</summary>
        Go,

        // ===== サ行 =====
        /// <summary>サ</summary>
        Sa,
        /// <summary>シ</summary>
        Shi,
        /// <summary>シャ</summary>
        Sha,
        /// <summary>シュ</summary>
        Shu,
        /// <summary>ショ</summary>
        Sho,
        /// <summary>シェ</summary>
        She,
        /// <summary>ス</summary>
        Su,
        /// <summary>スィ</summary>
        Swi,
        /// <summary>セ</summary>
        Se,
        /// <summary>ソ</summary>
        So,

        // ===== ザ行 =====
        /// <summary>ザ</summary>
        Za,
        /// <summary>ジ</summary>
        Ji,
        /// <summary>ジャ</summary>
        Ja,
        /// <summary>ジュ</summary>
        Ju,
        /// <summary>ジョ</summary>
        Jo,
        /// <summary>ジェ</summary>
        Je,
        /// <summary>ズ</summary>
        Zu,
        /// <summary>ズィ</summary>
        Zwi,
        /// <summary>ゼ</summary>
        Ze,
        /// <summary>ゾ</summary>
        Zo,

        // ===== タ行 =====
        /// <summary>タ</summary>
        Ta,
        /// <summary>チ</summary>
        Chi,
        /// <summary>チャ</summary>
        Cha,
        /// <summary>チュ</summary>
        Chu,
        /// <summary>チョ</summary>
        Cho,
        /// <summary>チェ</summary>
        Che,
        /// <summary>ツ</summary>
        Tsu,
        /// <summary>ツァ</summary>
        Tsa,
        /// <summary>ツィ</summary>
        Tsi,
        /// <summary>ツェ</summary>
        Tse,
        /// <summary>ツォ</summary>
        Tso,
        /// <summary>ッ（促音）</summary>
        Xtsu,
        /// <summary>テ</summary>
        Te,
        /// <summary>ティ</summary>
        Thi,
        /// <summary>テャ</summary>
        Tha,
        /// <summary>テュ</summary>
        Thu,
        /// <summary>テョ</summary>
        Tho,
        /// <summary>ト</summary>
        To,
        /// <summary>トゥ</summary>
        Twu,

        // ===== ダ行 =====
        /// <summary>ダ</summary>
        Da,
        /// <summary>ヂ</summary>
        Di,
        /// <summary>ヅ</summary>
        Du,
        /// <summary>デ</summary>
        De,
        /// <summary>ディ</summary>
        Dhi,
        /// <summary>デャ</summary>
        Dha,
        /// <summary>デュ</summary>
        Dhu,
        /// <summary>デョ</summary>
        Dho,
        /// <summary>ド</summary>
        Do,
        /// <summary>ドゥ</summary>
        Dwu,

        // ===== ナ行 =====
        /// <summary>ナ</summary>
        Na,
        /// <summary>ニ</summary>
        Ni,
        /// <summary>ニャ</summary>
        Nya,
        /// <summary>ニュ</summary>
        Nyu,
        /// <summary>ニョ</summary>
        Nyo,
        /// <summary>ニェ</summary>
        Nye,
        /// <summary>ヌ</summary>
        Nu,
        /// <summary>ネ</summary>
        Ne,
        /// <summary>ノ</summary>
        No,

        // ===== ハ行 =====
        /// <summary>ハ</summary>
        Ha,
        /// <summary>ヒ</summary>
        Hi,
        /// <summary>ヒャ</summary>
        Hya,
        /// <summary>ヒュ</summary>
        Hyu,
        /// <summary>ヒョ</summary>
        Hyo,
        /// <summary>ヒェ</summary>
        Hye,
        /// <summary>フ</summary>
        Fu,
        /// <summary>ファ</summary>
        Fa,
        /// <summary>フィ</summary>
        Fi,
        /// <summary>フェ</summary>
        Fe,
        /// <summary>フォ</summary>
        Fo,
        /// <summary>ヘ</summary>
        He,
        /// <summary>ホ</summary>
        Ho,

        // ===== バ行 =====
        /// <summary>バ</summary>
        Ba,
        /// <summary>ビ</summary>
        Bi,
        /// <summary>ビャ</summary>
        Bya,
        /// <summary>ビュ</summary>
        Byu,
        /// <summary>ビョ</summary>
        Byo,
        /// <summary>ビェ</summary>
        Bye,
        /// <summary>ブ</summary>
        Bu,
        /// <summary>ベ</summary>
        Be,
        /// <summary>ボ</summary>
        Bo,

        // ===== パ行 =====
        /// <summary>パ</summary>
        Pa,
        /// <summary>ピ</summary>
        Pi,
        /// <summary>ピャ</summary>
        Pya,
        /// <summary>ピュ</summary>
        Pyu,
        /// <summary>ピョ</summary>
        Pyo,
        /// <summary>ピェ</summary>
        Pye,
        /// <summary>プ</summary>
        Pu,
        /// <summary>ペ</summary>
        Pe,
        /// <summary>ポ</summary>
        Po,

        // ===== マ行 =====
        /// <summary>マ</summary>
        Ma,
        /// <summary>ミ</summary>
        Mi,
        /// <summary>ミャ</summary>
        Mya,
        /// <summary>ミュ</summary>
        Myu,
        /// <summary>ミョ</summary>
        Myo,
        /// <summary>ミェ</summary>
        Mye,
        /// <summary>ム</summary>
        Mu,
        /// <summary>メ</summary>
        Me,
        /// <summary>モ</summary>
        Mo,

        // ===== ヤ行 =====
        /// <summary>ヤ</summary>
        Ya,
        /// <summary>ャ（小書き）</summary>
        Xya,
        /// <summary>ユ</summary>
        Yu,
        /// <summary>ュ（小書き）</summary>
        Xyu,
        /// <summary>イェ</summary>
        Ye,
        /// <summary>ヨ</summary>
        Yo,
        /// <summary>ョ（小書き）</summary>
        Xyo,

        // ===== ラ行 =====
        /// <summary>ラ</summary>
        Ra,
        /// <summary>リ</summary>
        Ri,
        /// <summary>リャ</summary>
        Rya,
        /// <summary>リュ</summary>
        Ryu,
        /// <summary>リョ</summary>
        Ryo,
        /// <summary>リェ</summary>
        Rye,
        /// <summary>ル</summary>
        Ru,
        /// <summary>レ</summary>
        Re,
        /// <summary>ロ</summary>
        Ro,

        // ===== ワ行 =====
        /// <summary>ワ</summary>
        Wa,
        /// <summary>ヮ（小書き）</summary>
        Xwa,
        /// <summary>ヰ</summary>
        Wi,
        /// <summary>ウィ</summary>
        Whi,
        /// <summary>ウェ</summary>
        Whe,
        /// <summary>ヱ</summary>
        We,
        /// <summary>ウォ</summary>
        Who,
        /// <summary>ヲ</summary>
        Wo,

        // ===== ン =====
        /// <summary>ン（撥音）</summary>
        N,

        // ===== ヴ行（外来語） =====
        /// <summary>ヴ</summary>
        Vu,
        /// <summary>ヴァ</summary>
        Va,
        /// <summary>ヴィ</summary>
        Vi,
        /// <summary>ヴェ</summary>
        Ve,
        /// <summary>ヴォ</summary>
        Vo,
        /// <summary>ヴャ</summary>
        Vya,
        /// <summary>ヴュ</summary>
        Vyu,
        /// <summary>ヴョ</summary>
        Vyo,

        // ===== 特殊 =====
        /// <summary>ヶ（小書き）</summary>
        Xke,
        /// <summary>長音（ー）</summary>
        Long,
        /// <summary>句点（、）</summary>
        Touten,
        /// <summary>疑問符（？）</summary>
        Question,
    }

    /// <summary>
    /// MoraKindの拡張メソッド群。
    /// カタカナ文字列との相互変換を提供する。
    /// </summary>
    public static class MoraKindExtensions
    {
        /// <summary>
        /// MoraKindからカタカナ文字列への変換テーブル。
        /// </summary>
        private static readonly Dictionary<MoraKind, string> _toKatakana = new Dictionary<MoraKind, string>
        {
            // ア行
            { MoraKind.A, "ア" },
            { MoraKind.Xa, "ァ" },
            { MoraKind.I, "イ" },
            { MoraKind.Xi, "ィ" },
            { MoraKind.U, "ウ" },
            { MoraKind.Xu, "ゥ" },
            { MoraKind.E, "エ" },
            { MoraKind.Xe, "ェ" },
            { MoraKind.O, "オ" },
            { MoraKind.Xo, "ォ" },
            // カ行
            { MoraKind.Ka, "カ" },
            { MoraKind.Ki, "キ" },
            { MoraKind.Kya, "キャ" },
            { MoraKind.Kyu, "キュ" },
            { MoraKind.Kyo, "キョ" },
            { MoraKind.Kye, "キェ" },
            { MoraKind.Ku, "ク" },
            { MoraKind.Kwa, "クヮ" },
            { MoraKind.Ke, "ケ" },
            { MoraKind.Ko, "コ" },
            // ガ行
            { MoraKind.Ga, "ガ" },
            { MoraKind.Gi, "ギ" },
            { MoraKind.Gya, "ギャ" },
            { MoraKind.Gyu, "ギュ" },
            { MoraKind.Gyo, "ギョ" },
            { MoraKind.Gye, "ギェ" },
            { MoraKind.Gu, "グ" },
            { MoraKind.Gwa, "グヮ" },
            { MoraKind.Ge, "ゲ" },
            { MoraKind.Go, "ゴ" },
            // サ行
            { MoraKind.Sa, "サ" },
            { MoraKind.Shi, "シ" },
            { MoraKind.Sha, "シャ" },
            { MoraKind.Shu, "シュ" },
            { MoraKind.Sho, "ショ" },
            { MoraKind.She, "シェ" },
            { MoraKind.Su, "ス" },
            { MoraKind.Swi, "スィ" },
            { MoraKind.Se, "セ" },
            { MoraKind.So, "ソ" },
            // ザ行
            { MoraKind.Za, "ザ" },
            { MoraKind.Ji, "ジ" },
            { MoraKind.Ja, "ジャ" },
            { MoraKind.Ju, "ジュ" },
            { MoraKind.Jo, "ジョ" },
            { MoraKind.Je, "ジェ" },
            { MoraKind.Zu, "ズ" },
            { MoraKind.Zwi, "ズィ" },
            { MoraKind.Ze, "ゼ" },
            { MoraKind.Zo, "ゾ" },
            // タ行
            { MoraKind.Ta, "タ" },
            { MoraKind.Chi, "チ" },
            { MoraKind.Cha, "チャ" },
            { MoraKind.Chu, "チュ" },
            { MoraKind.Cho, "チョ" },
            { MoraKind.Che, "チェ" },
            { MoraKind.Tsu, "ツ" },
            { MoraKind.Tsa, "ツァ" },
            { MoraKind.Tsi, "ツィ" },
            { MoraKind.Tse, "ツェ" },
            { MoraKind.Tso, "ツォ" },
            { MoraKind.Xtsu, "ッ" },
            { MoraKind.Te, "テ" },
            { MoraKind.Thi, "ティ" },
            { MoraKind.Tha, "テャ" },
            { MoraKind.Thu, "テュ" },
            { MoraKind.Tho, "テョ" },
            { MoraKind.To, "ト" },
            { MoraKind.Twu, "トゥ" },
            // ダ行
            { MoraKind.Da, "ダ" },
            { MoraKind.Di, "ヂ" },
            { MoraKind.Du, "ヅ" },
            { MoraKind.De, "デ" },
            { MoraKind.Dhi, "ディ" },
            { MoraKind.Dha, "デャ" },
            { MoraKind.Dhu, "デュ" },
            { MoraKind.Dho, "デョ" },
            { MoraKind.Do, "ド" },
            { MoraKind.Dwu, "ドゥ" },
            // ナ行
            { MoraKind.Na, "ナ" },
            { MoraKind.Ni, "ニ" },
            { MoraKind.Nya, "ニャ" },
            { MoraKind.Nyu, "ニュ" },
            { MoraKind.Nyo, "ニョ" },
            { MoraKind.Nye, "ニェ" },
            { MoraKind.Nu, "ヌ" },
            { MoraKind.Ne, "ネ" },
            { MoraKind.No, "ノ" },
            // ハ行
            { MoraKind.Ha, "ハ" },
            { MoraKind.Hi, "ヒ" },
            { MoraKind.Hya, "ヒャ" },
            { MoraKind.Hyu, "ヒュ" },
            { MoraKind.Hyo, "ヒョ" },
            { MoraKind.Hye, "ヒェ" },
            { MoraKind.Fu, "フ" },
            { MoraKind.Fa, "ファ" },
            { MoraKind.Fi, "フィ" },
            { MoraKind.Fe, "フェ" },
            { MoraKind.Fo, "フォ" },
            { MoraKind.He, "ヘ" },
            { MoraKind.Ho, "ホ" },
            // バ行
            { MoraKind.Ba, "バ" },
            { MoraKind.Bi, "ビ" },
            { MoraKind.Bya, "ビャ" },
            { MoraKind.Byu, "ビュ" },
            { MoraKind.Byo, "ビョ" },
            { MoraKind.Bye, "ビェ" },
            { MoraKind.Bu, "ブ" },
            { MoraKind.Be, "ベ" },
            { MoraKind.Bo, "ボ" },
            // パ行
            { MoraKind.Pa, "パ" },
            { MoraKind.Pi, "ピ" },
            { MoraKind.Pya, "ピャ" },
            { MoraKind.Pyu, "ピュ" },
            { MoraKind.Pyo, "ピョ" },
            { MoraKind.Pye, "ピェ" },
            { MoraKind.Pu, "プ" },
            { MoraKind.Pe, "ペ" },
            { MoraKind.Po, "ポ" },
            // マ行
            { MoraKind.Ma, "マ" },
            { MoraKind.Mi, "ミ" },
            { MoraKind.Mya, "ミャ" },
            { MoraKind.Myu, "ミュ" },
            { MoraKind.Myo, "ミョ" },
            { MoraKind.Mye, "ミェ" },
            { MoraKind.Mu, "ム" },
            { MoraKind.Me, "メ" },
            { MoraKind.Mo, "モ" },
            // ヤ行
            { MoraKind.Ya, "ヤ" },
            { MoraKind.Xya, "ャ" },
            { MoraKind.Yu, "ユ" },
            { MoraKind.Xyu, "ュ" },
            { MoraKind.Ye, "イェ" },
            { MoraKind.Yo, "ヨ" },
            { MoraKind.Xyo, "ョ" },
            // ラ行
            { MoraKind.Ra, "ラ" },
            { MoraKind.Ri, "リ" },
            { MoraKind.Rya, "リャ" },
            { MoraKind.Ryu, "リュ" },
            { MoraKind.Ryo, "リョ" },
            { MoraKind.Rye, "リェ" },
            { MoraKind.Ru, "ル" },
            { MoraKind.Re, "レ" },
            { MoraKind.Ro, "ロ" },
            // ワ行
            { MoraKind.Wa, "ワ" },
            { MoraKind.Xwa, "ヮ" },
            { MoraKind.Wi, "ヰ" },
            { MoraKind.Whi, "ウィ" },
            { MoraKind.Whe, "ウェ" },
            { MoraKind.We, "ヱ" },
            { MoraKind.Who, "ウォ" },
            { MoraKind.Wo, "ヲ" },
            // ン
            { MoraKind.N, "ン" },
            // ヴ行
            { MoraKind.Vu, "ヴ" },
            { MoraKind.Va, "ヴァ" },
            { MoraKind.Vi, "ヴィ" },
            { MoraKind.Ve, "ヴェ" },
            { MoraKind.Vo, "ヴォ" },
            { MoraKind.Vya, "ヴャ" },
            { MoraKind.Vyu, "ヴュ" },
            { MoraKind.Vyo, "ヴョ" },
            // 特殊
            { MoraKind.Xke, "ヶ" },
            { MoraKind.Long, "ー" },
            { MoraKind.Touten, "、" },
            { MoraKind.Question, "？" },
        };

        /// <summary>
        /// カタカナ文字列からMoraKindへの逆引きテーブル。
        /// 長いキーから先にマッチさせるため、呼び出し側でキー長の降順ソートが必要。
        /// </summary>
        private static readonly Dictionary<string, MoraKind> _fromKatakana;

        /// <summary>
        /// 静的コンストラクタで逆引きテーブルを構築する。
        /// </summary>
        static MoraKindExtensions()
        {
            _fromKatakana = new Dictionary<string, MoraKind>();
            foreach (var pair in _toKatakana)
            {
                _fromKatakana[pair.Value] = pair.Key;
            }
        }

        /// <summary>
        /// MoraKindに対応するカタカナ文字列を返す。
        /// </summary>
        /// <param name="kind">変換対象のMoraKind</param>
        /// <returns>カタカナ文字列</returns>
        /// <exception cref="ArgumentOutOfRangeException">未定義のMoraKindが渡された場合</exception>
        public static string ToKatakana(this MoraKind kind)
        {
            if (_toKatakana.TryGetValue(kind, out var katakana))
            {
                return katakana;
            }
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "未定義のMoraKindです");
        }

        /// <summary>
        /// カタカナ文字列からMoraKindを返す。
        /// </summary>
        /// <param name="katakana">カタカナ文字列</param>
        /// <returns>対応するMoraKind</returns>
        /// <exception cref="ArgumentException">対応するMoraKindが見つからない場合</exception>
        /// <exception cref="ArgumentNullException">nullが渡された場合</exception>
        public static MoraKind FromKatakana(string katakana)
        {
            if (katakana == null)
            {
                throw new ArgumentNullException(nameof(katakana));
            }
            if (_fromKatakana.TryGetValue(katakana, out var kind))
            {
                return kind;
            }
            throw new ArgumentException($"対応するMoraKindが見つかりません: {katakana}", nameof(katakana));
        }

        /// <summary>
        /// カタカナ文字列からMoraKindへの変換を試みる。
        /// </summary>
        /// <param name="katakana">カタカナ文字列</param>
        /// <param name="kind">変換結果</param>
        /// <returns>変換に成功した場合はtrue</returns>
        public static bool TryFromKatakana(string katakana, out MoraKind kind)
        {
            if (katakana != null && _fromKatakana.TryGetValue(katakana, out kind))
            {
                return true;
            }
            kind = default;
            return false;
        }
    }
}
