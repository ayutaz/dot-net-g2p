using System;
using System.Collections.Generic;
using System.Text;
using DotNetG2P.Models;

namespace DotNetG2P.PhonemeConverter
{
    /// <summary>
    /// カタカナ⇔音素のマッピング。
    /// jpreprocess の mora_dict.rs / phoneme.rs および
    /// VOICEVOX の mora_mapping.py に準拠した全モーラの変換を提供する。
    /// </summary>
    public static class MoraMapping
    {
        /// <summary>カタカナ→(子音, 母音, MoraKind) の変換テーブル（キー長降順ソート済み）</summary>
        private static readonly (string Katakana, Consonant? Consonant, Vowel? Vowel, MoraKind Kind)[] _mapping;

        /// <summary>先頭文字→候補リスト（キー長降順ソート済み）の索引テーブル</summary>
        private static readonly Dictionary<char, List<(string Katakana, Consonant? Consonant, Vowel? Vowel, MoraKind Kind)>> _firstCharIndex;

        /// <summary>MoraKind → (子音, 母音) の音素マッピングテーブル</summary>
        private static readonly Dictionary<MoraKind, (Consonant?, Vowel?)> _moraToPhoneme;

        static MoraMapping()
        {
            // jpreprocess の phoneme.rs / mora_dict.rs に完全準拠したマッピングテーブル
            var list = new List<(string, Consonant?, Vowel?, MoraKind)>
            {
                // === 2文字モーラ（拗音・外来音等、最長一致のため先に定義） ===

                // カ行拗音
                ("キャ", Consonant.Ky, Vowel.A, MoraKind.Kya),
                ("キュ", Consonant.Ky, Vowel.U, MoraKind.Kyu),
                ("キョ", Consonant.Ky, Vowel.O, MoraKind.Kyo),
                ("キェ", Consonant.Ky, Vowel.E, MoraKind.Kye),
                ("クヮ", Consonant.Kw, Vowel.A, MoraKind.Kwa),

                // ガ行拗音
                ("ギャ", Consonant.Gy, Vowel.A, MoraKind.Gya),
                ("ギュ", Consonant.Gy, Vowel.U, MoraKind.Gyu),
                ("ギョ", Consonant.Gy, Vowel.O, MoraKind.Gyo),
                ("ギェ", Consonant.Gy, Vowel.E, MoraKind.Gye),
                ("グヮ", Consonant.Gw, Vowel.A, MoraKind.Gwa),

                // サ行拗音
                ("シャ", Consonant.Sh, Vowel.A, MoraKind.Sha),
                ("シュ", Consonant.Sh, Vowel.U, MoraKind.Shu),
                ("ショ", Consonant.Sh, Vowel.O, MoraKind.Sho),
                ("シェ", Consonant.Sh, Vowel.E, MoraKind.She),
                ("スィ", Consonant.S, Vowel.I, MoraKind.Swi),

                // ザ行拗音
                ("ジャ", Consonant.J, Vowel.A, MoraKind.Ja),
                ("ジュ", Consonant.J, Vowel.U, MoraKind.Ju),
                ("ジョ", Consonant.J, Vowel.O, MoraKind.Jo),
                ("ジェ", Consonant.J, Vowel.E, MoraKind.Je),
                ("ズィ", Consonant.Z, Vowel.I, MoraKind.Zwi),

                // タ行拗音・外来音
                ("チャ", Consonant.Ch, Vowel.A, MoraKind.Cha),
                ("チュ", Consonant.Ch, Vowel.U, MoraKind.Chu),
                ("チョ", Consonant.Ch, Vowel.O, MoraKind.Cho),
                ("チェ", Consonant.Ch, Vowel.E, MoraKind.Che),
                ("ツァ", Consonant.Ts, Vowel.A, MoraKind.Tsa),
                ("ツィ", Consonant.Ts, Vowel.I, MoraKind.Tsi),
                ("ツェ", Consonant.Ts, Vowel.E, MoraKind.Tse),
                ("ツォ", Consonant.Ts, Vowel.O, MoraKind.Tso),
                ("ティ", Consonant.T, Vowel.I, MoraKind.Thi),
                ("テャ", Consonant.Ty, Vowel.A, MoraKind.Tha),
                ("テュ", Consonant.Ty, Vowel.U, MoraKind.Thu),
                ("テョ", Consonant.Ty, Vowel.O, MoraKind.Tho),
                ("トゥ", Consonant.T, Vowel.U, MoraKind.Twu),

                // ダ行外来音
                ("ディ", Consonant.D, Vowel.I, MoraKind.Dhi),
                ("デャ", Consonant.Dy, Vowel.A, MoraKind.Dha),
                ("デュ", Consonant.Dy, Vowel.U, MoraKind.Dhu),
                ("デョ", Consonant.Dy, Vowel.O, MoraKind.Dho),
                ("ドゥ", Consonant.D, Vowel.U, MoraKind.Dwu),

                // ナ行拗音
                ("ニャ", Consonant.Ny, Vowel.A, MoraKind.Nya),
                ("ニュ", Consonant.Ny, Vowel.U, MoraKind.Nyu),
                ("ニョ", Consonant.Ny, Vowel.O, MoraKind.Nyo),
                ("ニェ", Consonant.Ny, Vowel.E, MoraKind.Nye),

                // ハ行拗音・外来音
                ("ヒャ", Consonant.Hy, Vowel.A, MoraKind.Hya),
                ("ヒュ", Consonant.Hy, Vowel.U, MoraKind.Hyu),
                ("ヒョ", Consonant.Hy, Vowel.O, MoraKind.Hyo),
                ("ヒェ", Consonant.Hy, Vowel.E, MoraKind.Hye),
                ("ファ", Consonant.F, Vowel.A, MoraKind.Fa),
                ("フィ", Consonant.F, Vowel.I, MoraKind.Fi),
                ("フェ", Consonant.F, Vowel.E, MoraKind.Fe),
                ("フォ", Consonant.F, Vowel.O, MoraKind.Fo),

                // バ行拗音
                ("ビャ", Consonant.By, Vowel.A, MoraKind.Bya),
                ("ビュ", Consonant.By, Vowel.U, MoraKind.Byu),
                ("ビョ", Consonant.By, Vowel.O, MoraKind.Byo),
                ("ビェ", Consonant.By, Vowel.E, MoraKind.Bye),

                // パ行拗音
                ("ピャ", Consonant.Py, Vowel.A, MoraKind.Pya),
                ("ピュ", Consonant.Py, Vowel.U, MoraKind.Pyu),
                ("ピョ", Consonant.Py, Vowel.O, MoraKind.Pyo),
                ("ピェ", Consonant.Py, Vowel.E, MoraKind.Pye),

                // マ行拗音
                ("ミャ", Consonant.My, Vowel.A, MoraKind.Mya),
                ("ミュ", Consonant.My, Vowel.U, MoraKind.Myu),
                ("ミョ", Consonant.My, Vowel.O, MoraKind.Myo),
                ("ミェ", Consonant.My, Vowel.E, MoraKind.Mye),

                // ラ行拗音
                ("リャ", Consonant.Ry, Vowel.A, MoraKind.Rya),
                ("リュ", Consonant.Ry, Vowel.U, MoraKind.Ryu),
                ("リョ", Consonant.Ry, Vowel.O, MoraKind.Ryo),
                ("リェ", Consonant.Ry, Vowel.E, MoraKind.Rye),

                // ワ行外来音
                ("ウィ", Consonant.W, Vowel.I, MoraKind.Whi),
                ("ウェ", Consonant.W, Vowel.E, MoraKind.Whe),
                ("ウォ", Consonant.W, Vowel.O, MoraKind.Who),

                // ヴ行外来音（拗音）
                // jpreprocess準拠: ヴャ/ヴュ/ヴョ は by 子音にマッピングされる
                ("ヴァ", Consonant.V, Vowel.A, MoraKind.Va),
                ("ヴィ", Consonant.V, Vowel.I, MoraKind.Vi),
                ("ヴェ", Consonant.V, Vowel.E, MoraKind.Ve),
                ("ヴォ", Consonant.V, Vowel.O, MoraKind.Vo),
                ("ヴャ", Consonant.By, Vowel.A, MoraKind.Vya),
                ("ヴュ", Consonant.By, Vowel.U, MoraKind.Vyu),
                ("ヴョ", Consonant.By, Vowel.O, MoraKind.Vyo),

                // イェ（ヤ行外来音）
                ("イェ", Consonant.Y, Vowel.E, MoraKind.Ye),

                // === 1文字モーラ（基本音） ===

                // ア行
                ("ア", null, Vowel.A, MoraKind.A),
                ("ァ", null, Vowel.A, MoraKind.Xa),
                ("イ", null, Vowel.I, MoraKind.I),
                ("ィ", null, Vowel.I, MoraKind.Xi),
                ("ウ", null, Vowel.U, MoraKind.U),
                ("ゥ", null, Vowel.U, MoraKind.Xu),
                ("エ", null, Vowel.E, MoraKind.E),
                ("ェ", null, Vowel.E, MoraKind.Xe),
                ("オ", null, Vowel.O, MoraKind.O),
                ("ォ", null, Vowel.O, MoraKind.Xo),

                // カ行
                ("カ", Consonant.K, Vowel.A, MoraKind.Ka),
                ("キ", Consonant.K, Vowel.I, MoraKind.Ki),
                ("ク", Consonant.K, Vowel.U, MoraKind.Ku),
                ("ケ", Consonant.K, Vowel.E, MoraKind.Ke),
                ("コ", Consonant.K, Vowel.O, MoraKind.Ko),

                // ガ行
                ("ガ", Consonant.G, Vowel.A, MoraKind.Ga),
                ("ギ", Consonant.G, Vowel.I, MoraKind.Gi),
                ("グ", Consonant.G, Vowel.U, MoraKind.Gu),
                ("ゲ", Consonant.G, Vowel.E, MoraKind.Ge),
                ("ゴ", Consonant.G, Vowel.O, MoraKind.Go),

                // サ行
                ("サ", Consonant.S, Vowel.A, MoraKind.Sa),
                ("シ", Consonant.Sh, Vowel.I, MoraKind.Shi),
                ("ス", Consonant.S, Vowel.U, MoraKind.Su),
                ("セ", Consonant.S, Vowel.E, MoraKind.Se),
                ("ソ", Consonant.S, Vowel.O, MoraKind.So),

                // ザ行
                ("ザ", Consonant.Z, Vowel.A, MoraKind.Za),
                ("ジ", Consonant.J, Vowel.I, MoraKind.Ji),
                ("ズ", Consonant.Z, Vowel.U, MoraKind.Zu),
                ("ゼ", Consonant.Z, Vowel.E, MoraKind.Ze),
                ("ゾ", Consonant.Z, Vowel.O, MoraKind.Zo),

                // タ行
                ("タ", Consonant.T, Vowel.A, MoraKind.Ta),
                ("チ", Consonant.Ch, Vowel.I, MoraKind.Chi),
                ("ツ", Consonant.Ts, Vowel.U, MoraKind.Tsu),
                ("テ", Consonant.T, Vowel.E, MoraKind.Te),
                ("ト", Consonant.T, Vowel.O, MoraKind.To),

                // ダ行
                // jpreprocess準拠: ヂ→j i, ヅ→z u
                ("ダ", Consonant.D, Vowel.A, MoraKind.Da),
                ("ヂ", Consonant.J, Vowel.I, MoraKind.Di),
                ("ヅ", Consonant.Z, Vowel.U, MoraKind.Du),
                ("デ", Consonant.D, Vowel.E, MoraKind.De),
                ("ド", Consonant.D, Vowel.O, MoraKind.Do),

                // ナ行
                ("ナ", Consonant.N, Vowel.A, MoraKind.Na),
                ("ニ", Consonant.N, Vowel.I, MoraKind.Ni),
                ("ヌ", Consonant.N, Vowel.U, MoraKind.Nu),
                ("ネ", Consonant.N, Vowel.E, MoraKind.Ne),
                ("ノ", Consonant.N, Vowel.O, MoraKind.No),

                // ハ行
                ("ハ", Consonant.H, Vowel.A, MoraKind.Ha),
                ("ヒ", Consonant.H, Vowel.I, MoraKind.Hi),
                ("フ", Consonant.F, Vowel.U, MoraKind.Fu),
                ("ヘ", Consonant.H, Vowel.E, MoraKind.He),
                ("ホ", Consonant.H, Vowel.O, MoraKind.Ho),

                // バ行
                ("バ", Consonant.B, Vowel.A, MoraKind.Ba),
                ("ビ", Consonant.B, Vowel.I, MoraKind.Bi),
                ("ブ", Consonant.B, Vowel.U, MoraKind.Bu),
                ("ベ", Consonant.B, Vowel.E, MoraKind.Be),
                ("ボ", Consonant.B, Vowel.O, MoraKind.Bo),

                // パ行
                ("パ", Consonant.P, Vowel.A, MoraKind.Pa),
                ("ピ", Consonant.P, Vowel.I, MoraKind.Pi),
                ("プ", Consonant.P, Vowel.U, MoraKind.Pu),
                ("ペ", Consonant.P, Vowel.E, MoraKind.Pe),
                ("ポ", Consonant.P, Vowel.O, MoraKind.Po),

                // マ行
                ("マ", Consonant.M, Vowel.A, MoraKind.Ma),
                ("ミ", Consonant.M, Vowel.I, MoraKind.Mi),
                ("ム", Consonant.M, Vowel.U, MoraKind.Mu),
                ("メ", Consonant.M, Vowel.E, MoraKind.Me),
                ("モ", Consonant.M, Vowel.O, MoraKind.Mo),

                // ヤ行
                ("ヤ", Consonant.Y, Vowel.A, MoraKind.Ya),
                ("ャ", Consonant.Y, Vowel.A, MoraKind.Xya),
                ("ユ", Consonant.Y, Vowel.U, MoraKind.Yu),
                ("ュ", Consonant.Y, Vowel.U, MoraKind.Xyu),
                ("ヨ", Consonant.Y, Vowel.O, MoraKind.Yo),
                ("ョ", Consonant.Y, Vowel.O, MoraKind.Xyo),

                // ラ行
                ("ラ", Consonant.R, Vowel.A, MoraKind.Ra),
                ("リ", Consonant.R, Vowel.I, MoraKind.Ri),
                ("ル", Consonant.R, Vowel.U, MoraKind.Ru),
                ("レ", Consonant.R, Vowel.E, MoraKind.Re),
                ("ロ", Consonant.R, Vowel.O, MoraKind.Ro),

                // ワ行
                // jpreprocess準拠: ヰ→null+i, ヱ→null+e, ヲ→null+o
                ("ワ", Consonant.W, Vowel.A, MoraKind.Wa),
                ("ヮ", Consonant.W, Vowel.A, MoraKind.Xwa),
                ("ヰ", null, Vowel.I, MoraKind.Wi),
                ("ヱ", null, Vowel.E, MoraKind.We),
                ("ヲ", null, Vowel.O, MoraKind.Wo),

                // ヴ（単独）
                ("ヴ", Consonant.V, Vowel.U, MoraKind.Vu),

                // 特殊モーラ
                ("ン", Consonant.Nn, null, MoraKind.N),
                ("ッ", Consonant.Cl, null, MoraKind.Xtsu),
                ("ー", Consonant.Long, null, MoraKind.Long),
                ("ヶ", Consonant.K, Vowel.E, MoraKind.Xke),
                ("、", null, null, MoraKind.Touten),
                ("？", null, null, MoraKind.Question),
            };

            // カタカナ文字列長の降順でソート（最長一致用）
            list.Sort((a, b) => b.Item1.Length.CompareTo(a.Item1.Length));
            _mapping = list.ToArray();

            // 先頭文字→候補リストの索引テーブルを構築（キー長降順を維持）
            _firstCharIndex = new Dictionary<char, List<(string, Consonant?, Vowel?, MoraKind)>>();
            foreach (var entry in _mapping)
            {
                char firstChar = entry.Katakana[0];
                if (!_firstCharIndex.TryGetValue(firstChar, out var candidates))
                {
                    candidates = new List<(string, Consonant?, Vowel?, MoraKind)>();
                    _firstCharIndex[firstChar] = candidates;
                }
                candidates.Add((entry.Katakana, entry.Consonant, entry.Vowel, entry.Kind));
            }

            // MoraKind → (Consonant?, Vowel?) テーブルの構築
            _moraToPhoneme = new Dictionary<MoraKind, (Consonant?, Vowel?)>();
            foreach (var entry in list)
            {
                // 同一MoraKindが複数のカタカナ表記を持つことはないが、
                // 最初の登録を優先する
                if (!_moraToPhoneme.ContainsKey(entry.Item4))
                {
                    _moraToPhoneme[entry.Item4] = (entry.Item2, entry.Item3);
                }
            }
        }

        /// <summary>
        /// MoraKindに対応する子音・母音のペアを返す。
        /// </summary>
        /// <param name="kind">モーラの種類</param>
        /// <returns>(子音, 母音) のタプル。どちらも null の場合がある。</returns>
        /// <exception cref="ArgumentException">未知のMoraKindの場合</exception>
        public static (Consonant? consonant, Vowel? vowel) GetPhonemes(MoraKind kind)
        {
            if (_moraToPhoneme.TryGetValue(kind, out var result))
                return result;

            throw new ArgumentException($"未知のMoraKindです: {kind}", nameof(kind));
        }

        /// <summary>
        /// MoraKindからMora構造体を生成する。
        /// </summary>
        /// <param name="kind">モーラの種類</param>
        /// <returns>対応するMora構造体</returns>
        public static Mora CreateMora(MoraKind kind)
        {
            var (consonant, vowel) = GetPhonemes(kind);
            return new Mora(consonant, vowel, kind);
        }

        /// <summary>
        /// カタカナ文字列をモーラのリストに変換する。
        /// 最長一致アルゴリズムで複数文字のカタカナ（例: 「キャ」= 1モーラ）を優先的にマッチする。
        /// </summary>
        /// <param name="katakana">カタカナ文字列</param>
        /// <returns>Moraのリスト</returns>
        /// <exception cref="ArgumentNullException">katakanaがnullの場合</exception>
        /// <exception cref="ArgumentException">マッチしない文字が含まれる場合</exception>
        public static List<Mora> KatakanaToMoras(string katakana)
        {
            if (katakana == null)
                throw new ArgumentNullException(nameof(katakana));

            if (katakana.Length == 0)
                return new List<Mora>();

            var moras = new List<Mora>();
            int i = 0;

            while (i < katakana.Length)
            {
                char firstChar = katakana[i];

                if (!_firstCharIndex.TryGetValue(firstChar, out var candidates))
                {
                    throw new ArgumentException(
                        $"カタカナ文字列に未知の文字が含まれています（位置 {i}: '{katakana[i]}'）",
                        nameof(katakana));
                }

                bool matched = false;
                int remaining = katakana.Length - i;

                foreach (var entry in candidates)
                {
                    if (entry.Katakana.Length > remaining)
                        continue;

                    if (entry.Katakana.Length == 1 ||
                        string.Compare(katakana, i, entry.Katakana, 0, entry.Katakana.Length, StringComparison.Ordinal) == 0)
                    {
                        moras.Add(new Mora(entry.Consonant, entry.Vowel, entry.Kind));
                        i += entry.Katakana.Length;
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    throw new ArgumentException(
                        $"カタカナ文字列に未知の文字が含まれています（位置 {i}: '{katakana[i]}'）",
                        nameof(katakana));
                }
            }

            return moras;
        }

        /// <summary>
        /// モーラのリストをスペース区切りの音素文字列に変換する。
        /// 例: [K+A, Nn+null, N+I, Ch+I, W+A] → "k a N n i ch i w a"
        /// </summary>
        /// <param name="moras">Moraのリスト</param>
        /// <returns>スペース区切りの音素文字列</returns>
        /// <exception cref="ArgumentNullException">morasがnullの場合</exception>
        public static string MorasToPhonemeString(IReadOnlyList<Mora> moras)
        {
            if (moras == null)
                throw new ArgumentNullException(nameof(moras));

            if (moras.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();

            for (int i = 0; i < moras.Count; i++)
            {
                var phoneme = moras[i].ToPhonemeString();

                // 空文字列のモーラ（句読点等）はスキップ
                if (phoneme.Length == 0)
                    continue;

                if (sb.Length > 0)
                    sb.Append(' ');

                sb.Append(phoneme);
            }

            return sb.ToString();
        }

        /// <summary>
        /// カタカナ文字列を直接音素文字列に変換するヘルパーメソッド。
        /// KatakanaToMoras + MorasToPhonemeString の一括実行。
        /// </summary>
        /// <param name="katakana">カタカナ文字列</param>
        /// <returns>スペース区切りの音素文字列</returns>
        public static string KatakanaToPhonemeString(string katakana)
        {
            var moras = KatakanaToMoras(katakana);
            return MorasToPhonemeString(moras);
        }
    }
}
