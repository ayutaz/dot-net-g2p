using System;
using System.Collections.Generic;

namespace DotNetG2P.Models
{
    /// <summary>
    /// 発音情報。モーラのリストとアクセント位置を保持する。
    /// </summary>
    public sealed class Pronunciation
    {
        /// <summary>モーラのリスト</summary>
        public List<Mora> Moras { get; }

        /// <summary>アクセント核位置（0=平板型、1以上=核の位置）</summary>
        public int AccentPosition { get; set; }

        public Pronunciation()
        {
            Moras = new List<Mora>();
            AccentPosition = 0;
        }

        public Pronunciation(List<Mora> moras, int accentPosition)
        {
            Moras = moras;
            AccentPosition = accentPosition;
        }

        /// <summary>
        /// 発音モーラ数を返す（Touten/Questionはカウントしない）。
        /// jpreprocess の mora_size() に準拠。
        /// </summary>
        public int MoraCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Moras.Count; i++)
                {
                    var kind = Moras[i].Kind;
                    if (kind != MoraKind.Touten && kind != MoraKind.Question)
                        count++;
                }
                return count;
            }
        }

        /// <summary>モーラリストが空かどうか</summary>
        public bool IsEmpty => Moras.Count == 0;

        /// <summary>句点（Touten）のみの発音かどうか</summary>
        public bool IsTouten => MoraMatches(MoraKind.Touten);

        /// <summary>疑問符（Question）のみの発音かどうか</summary>
        public bool IsQuestion => MoraMatches(MoraKind.Question);

        /// <summary>
        /// 単一モーラで、そのKindが指定のものと一致するかどうか。
        /// jpreprocess の mora_matches() に準拠。
        /// </summary>
        public bool MoraMatches(MoraKind kind)
        {
            return Moras.Count == 1 && Moras[0].Kind == kind;
        }

        /// <summary>
        /// 別のPronunciationからモーラを結合する（カナフィラー結合用）。
        /// jpreprocess の transfer_from() に準拠。
        /// </summary>
        public void TransferFrom(Pronunciation other)
        {
            Moras.AddRange(other.Moras);
        }

        /// <summary>
        /// 音素文字列を返す。各モーラの音素をスペース区切りで連結。
        /// 例: "k o N n i ch i w a"
        /// </summary>
        public string ToPhonemeString()
        {
            var parts = new List<string>();
            foreach (var mora in Moras)
            {
                var phoneme = mora.ToPhonemeString();
                if (!string.IsNullOrEmpty(phoneme))
                    parts.Add(phoneme);
            }
            return string.Join(" ", parts);
        }

        /// <summary>
        /// カタカナ文字列を返す。
        /// </summary>
        public string ToKatakana()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var mora in Moras)
            {
                sb.Append(mora.Kind.ToKatakana());
            }
            return sb.ToString();
        }

        /// <summary>
        /// カタカナ文字列とアクセント核位置から Pronunciation を構築する。
        /// jpreprocess の parse_mora_str に準拠した最長一致でモーラ分割を行う。
        /// シングルクォーテーション(') の直後のモーラは無声化される。
        /// </summary>
        /// <param name="katakana">カタカナ文字列（例: "コンニチワ"、"デ'ス"）</param>
        /// <param name="accentPosition">アクセント核位置（0=平板型）</param>
        /// <returns>構築された Pronunciation</returns>
        public static Pronunciation FromKatakana(string katakana, int accentPosition)
        {
            if (katakana == null)
                throw new ArgumentNullException(nameof(katakana));

            var moras = ParseKatakanaToMoras(katakana);
            return new Pronunciation(moras, accentPosition);
        }

        /// <summary>
        /// MoraKind から Mora インスタンスを生成するファクトリメソッド。
        /// </summary>
        /// <param name="kind">モーラの種類</param>
        /// <param name="unvoiced">無声化するかどうか（デフォルト: false）</param>
        /// <returns>生成された Mora</returns>
        public static Mora CreateMora(MoraKind kind, bool unvoiced = false)
        {
            if (!MoraPhonemeMap.TryGetValue(kind, out var phonemes))
            {
                throw new ArgumentException($"未定義のMoraKindです: {kind}", nameof(kind));
            }

            var (consonant, vowel) = phonemes;
            if (unvoiced && vowel.HasValue)
            {
                vowel = vowel.Value.ToUnvoiced();
            }

            return new Mora(consonant, vowel, kind);
        }

        /// <summary>
        /// 文字列がモーラ変換可能かどうか判定する。
        /// jpreprocess の is_mora_convertable() に準拠。
        /// カタカナ・ひらがな・全角アルファベット・特殊カタカナの全てがモーラ辞書に存在するかどうか。
        /// </summary>
        public static bool IsMoraConvertable(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            // 文字列全体が1つのセグメントとしてモーラ変換可能か判定
            // ParseMoraSegments で1セグメントかつモーラ数>0ならOK
            var segments = ParseMoraSegments(s);
            if (segments.Count == 1 && segments[0].moras.Count > 0)
            {
                // 全モーラがToutenでないことを確認
                foreach (var m in segments[0].moras)
                {
                    if (m.Kind == MoraKind.Touten)
                        return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 表層形文字列をモーラセグメントに分割する。
        /// jpreprocess の parse_mora_str() に準拠。
        /// カナ変換できない部分はToutenモーラのセグメントとして分割される。
        /// 例: "バリー・ペーン" → [("バリー", [Ba,Ri,Long]), ("・", [Touten]), ("ペーン", [Pe,Long,N])]
        /// </summary>
        /// <param name="s">入力文字列</param>
        /// <returns>セグメントのリスト。各セグメントは(部分文字列, モーラリスト)のタプル。</returns>
        public static List<(string text, List<Mora> moras)> ParseMoraSegments(string s)
        {
            if (s == "*")
                return new List<(string, List<Mora>)>();

            if (s == "\uFF1F") // ？ (全角疑問符)
            {
                return new List<(string, List<Mora>)>
                {
                    (s, new List<Mora> { new Mora(null, null, MoraKind.Question) })
                };
            }

            var result = new List<(string text, List<Mora> moras)>();
            int segmentStart = 0;
            var currentMoras = new List<Mora>();
            int currentPos = 0;

            while (currentPos < s.Length)
            {
                // 最長一致でモーラを探す
                bool matched = false;
                for (int i = 0; i < _sortedKatakanaKeys.Count; i++)
                {
                    var (key, kind) = _sortedKatakanaKeys[i];
                    if (currentPos + key.Length <= s.Length &&
                        string.Compare(s, currentPos, key, 0, key.Length, StringComparison.Ordinal) == 0)
                    {
                        // 無声化マーカー（'）チェック
                        bool unvoiced = false;
                        int advanceLen = key.Length;
                        if (currentPos + key.Length < s.Length && s[currentPos + key.Length] == '\'')
                        {
                            unvoiced = true;
                            advanceLen += 1; // ' の分を進める
                        }

                        var mora = CreateMora(kind, unvoiced);
                        currentMoras.Add(mora);
                        currentPos += advanceLen;
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    // カナ変換できない文字が見つかった
                    // 既存のモーラがあれば先にセグメントとして確定
                    if (currentMoras.Count > 0)
                    {
                        result.Add((s.Substring(segmentStart, currentPos - segmentStart), currentMoras));
                        currentMoras = new List<Mora>();
                        segmentStart = currentPos;
                    }

                    // 認識できない文字をスキップしてToutenセグメントを作成
                    // 連続する認識不能文字をまとめる
                    int unmatchedStart = currentPos;
                    currentPos++;
                    // 次の認識可能文字まで進める（1文字ずつ）
                    // jpreprocessの挙動: 認識不能部分はまとめてToutenセグメント

                    result.Add((s.Substring(unmatchedStart, currentPos - unmatchedStart),
                        new List<Mora> { new Mora(null, null, MoraKind.Touten) }));
                    segmentStart = currentPos;
                }
            }

            // 残りのモーラをセグメントとして追加
            if (currentMoras.Count > 0)
            {
                result.Add((s.Substring(segmentStart, currentPos - segmentStart), currentMoras));
            }

            return result;
        }

        public override string ToString()
        {
            return $"{ToKatakana()} [{AccentPosition}]";
        }

        // ===== 内部: カタカナ→モーラ変換 =====

        /// <summary>
        /// MoraKind → (子音?, 母音?) のマッピングテーブル。
        /// </summary>
        internal static readonly Dictionary<MoraKind, (Consonant? consonant, Vowel? vowel)> MoraPhonemeMap
            = new Dictionary<MoraKind, (Consonant?, Vowel?)>
        {
            // ア行
            { MoraKind.A,    (null, Vowel.A) },
            { MoraKind.Xa,   (null, Vowel.A) },
            { MoraKind.I,    (null, Vowel.I) },
            { MoraKind.Xi,   (null, Vowel.I) },
            { MoraKind.U,    (null, Vowel.U) },
            { MoraKind.Xu,   (null, Vowel.U) },
            { MoraKind.E,    (null, Vowel.E) },
            { MoraKind.Xe,   (null, Vowel.E) },
            { MoraKind.O,    (null, Vowel.O) },
            { MoraKind.Xo,   (null, Vowel.O) },
            // カ行
            { MoraKind.Ka,   (Consonant.K, Vowel.A) },
            { MoraKind.Ki,   (Consonant.K, Vowel.I) },
            { MoraKind.Kya,  (Consonant.Ky, Vowel.A) },
            { MoraKind.Kyu,  (Consonant.Ky, Vowel.U) },
            { MoraKind.Kyo,  (Consonant.Ky, Vowel.O) },
            { MoraKind.Kye,  (Consonant.Ky, Vowel.E) },
            { MoraKind.Ku,   (Consonant.K, Vowel.U) },
            { MoraKind.Kwa,  (Consonant.Kw, Vowel.A) },
            { MoraKind.Ke,   (Consonant.K, Vowel.E) },
            { MoraKind.Ko,   (Consonant.K, Vowel.O) },
            // ガ行
            { MoraKind.Ga,   (Consonant.G, Vowel.A) },
            { MoraKind.Gi,   (Consonant.G, Vowel.I) },
            { MoraKind.Gya,  (Consonant.Gy, Vowel.A) },
            { MoraKind.Gyu,  (Consonant.Gy, Vowel.U) },
            { MoraKind.Gyo,  (Consonant.Gy, Vowel.O) },
            { MoraKind.Gye,  (Consonant.Gy, Vowel.E) },
            { MoraKind.Gu,   (Consonant.G, Vowel.U) },
            { MoraKind.Gwa,  (Consonant.Gw, Vowel.A) },
            { MoraKind.Ge,   (Consonant.G, Vowel.E) },
            { MoraKind.Go,   (Consonant.G, Vowel.O) },
            // サ行
            { MoraKind.Sa,   (Consonant.S, Vowel.A) },
            { MoraKind.Shi,  (Consonant.Sh, Vowel.I) },
            { MoraKind.Sha,  (Consonant.Sh, Vowel.A) },
            { MoraKind.Shu,  (Consonant.Sh, Vowel.U) },
            { MoraKind.Sho,  (Consonant.Sh, Vowel.O) },
            { MoraKind.She,  (Consonant.Sh, Vowel.E) },
            { MoraKind.Su,   (Consonant.S, Vowel.U) },
            { MoraKind.Swi,  (Consonant.S, Vowel.I) },
            { MoraKind.Se,   (Consonant.S, Vowel.E) },
            { MoraKind.So,   (Consonant.S, Vowel.O) },
            // ザ行
            { MoraKind.Za,   (Consonant.Z, Vowel.A) },
            { MoraKind.Ji,   (Consonant.J, Vowel.I) },
            { MoraKind.Ja,   (Consonant.J, Vowel.A) },
            { MoraKind.Ju,   (Consonant.J, Vowel.U) },
            { MoraKind.Jo,   (Consonant.J, Vowel.O) },
            { MoraKind.Je,   (Consonant.J, Vowel.E) },
            { MoraKind.Zu,   (Consonant.Z, Vowel.U) },
            { MoraKind.Zwi,  (Consonant.Z, Vowel.I) },
            { MoraKind.Ze,   (Consonant.Z, Vowel.E) },
            { MoraKind.Zo,   (Consonant.Z, Vowel.O) },
            // タ行
            { MoraKind.Ta,   (Consonant.T, Vowel.A) },
            { MoraKind.Chi,  (Consonant.Ch, Vowel.I) },
            { MoraKind.Cha,  (Consonant.Ch, Vowel.A) },
            { MoraKind.Chu,  (Consonant.Ch, Vowel.U) },
            { MoraKind.Cho,  (Consonant.Ch, Vowel.O) },
            { MoraKind.Che,  (Consonant.Ch, Vowel.E) },
            { MoraKind.Tsu,  (Consonant.Ts, Vowel.U) },
            { MoraKind.Tsa,  (Consonant.Ts, Vowel.A) },
            { MoraKind.Tsi,  (Consonant.Ts, Vowel.I) },
            { MoraKind.Tse,  (Consonant.Ts, Vowel.E) },
            { MoraKind.Tso,  (Consonant.Ts, Vowel.O) },
            { MoraKind.Xtsu, (Consonant.Cl, null) },
            { MoraKind.Te,   (Consonant.T, Vowel.E) },
            { MoraKind.Thi,  (Consonant.Ty, Vowel.I) },
            { MoraKind.Tha,  (Consonant.Ty, Vowel.A) },
            { MoraKind.Thu,  (Consonant.Ty, Vowel.U) },
            { MoraKind.Tho,  (Consonant.Ty, Vowel.O) },
            { MoraKind.To,   (Consonant.T, Vowel.O) },
            { MoraKind.Twu,  (Consonant.T, Vowel.U) },
            // ダ行
            { MoraKind.Da,   (Consonant.D, Vowel.A) },
            { MoraKind.Di,   (Consonant.J, Vowel.I) },
            { MoraKind.Du,   (Consonant.Z, Vowel.U) },
            { MoraKind.De,   (Consonant.D, Vowel.E) },
            { MoraKind.Dhi,  (Consonant.Dy, Vowel.I) },
            { MoraKind.Dha,  (Consonant.Dy, Vowel.A) },
            { MoraKind.Dhu,  (Consonant.Dy, Vowel.U) },
            { MoraKind.Dho,  (Consonant.Dy, Vowel.O) },
            { MoraKind.Do,   (Consonant.D, Vowel.O) },
            { MoraKind.Dwu,  (Consonant.D, Vowel.U) },
            // ナ行
            { MoraKind.Na,   (Consonant.N, Vowel.A) },
            { MoraKind.Ni,   (Consonant.N, Vowel.I) },
            { MoraKind.Nya,  (Consonant.Ny, Vowel.A) },
            { MoraKind.Nyu,  (Consonant.Ny, Vowel.U) },
            { MoraKind.Nyo,  (Consonant.Ny, Vowel.O) },
            { MoraKind.Nye,  (Consonant.Ny, Vowel.E) },
            { MoraKind.Nu,   (Consonant.N, Vowel.U) },
            { MoraKind.Ne,   (Consonant.N, Vowel.E) },
            { MoraKind.No,   (Consonant.N, Vowel.O) },
            // ハ行
            { MoraKind.Ha,   (Consonant.H, Vowel.A) },
            { MoraKind.Hi,   (Consonant.H, Vowel.I) },
            { MoraKind.Hya,  (Consonant.Hy, Vowel.A) },
            { MoraKind.Hyu,  (Consonant.Hy, Vowel.U) },
            { MoraKind.Hyo,  (Consonant.Hy, Vowel.O) },
            { MoraKind.Hye,  (Consonant.Hy, Vowel.E) },
            { MoraKind.Fu,   (Consonant.F, Vowel.U) },
            { MoraKind.Fa,   (Consonant.F, Vowel.A) },
            { MoraKind.Fi,   (Consonant.F, Vowel.I) },
            { MoraKind.Fe,   (Consonant.F, Vowel.E) },
            { MoraKind.Fo,   (Consonant.F, Vowel.O) },
            { MoraKind.He,   (Consonant.H, Vowel.E) },
            { MoraKind.Ho,   (Consonant.H, Vowel.O) },
            // バ行
            { MoraKind.Ba,   (Consonant.B, Vowel.A) },
            { MoraKind.Bi,   (Consonant.B, Vowel.I) },
            { MoraKind.Bya,  (Consonant.By, Vowel.A) },
            { MoraKind.Byu,  (Consonant.By, Vowel.U) },
            { MoraKind.Byo,  (Consonant.By, Vowel.O) },
            { MoraKind.Bye,  (Consonant.By, Vowel.E) },
            { MoraKind.Bu,   (Consonant.B, Vowel.U) },
            { MoraKind.Be,   (Consonant.B, Vowel.E) },
            { MoraKind.Bo,   (Consonant.B, Vowel.O) },
            // パ行
            { MoraKind.Pa,   (Consonant.P, Vowel.A) },
            { MoraKind.Pi,   (Consonant.P, Vowel.I) },
            { MoraKind.Pya,  (Consonant.Py, Vowel.A) },
            { MoraKind.Pyu,  (Consonant.Py, Vowel.U) },
            { MoraKind.Pyo,  (Consonant.Py, Vowel.O) },
            { MoraKind.Pye,  (Consonant.Py, Vowel.E) },
            { MoraKind.Pu,   (Consonant.P, Vowel.U) },
            { MoraKind.Pe,   (Consonant.P, Vowel.E) },
            { MoraKind.Po,   (Consonant.P, Vowel.O) },
            // マ行
            { MoraKind.Ma,   (Consonant.M, Vowel.A) },
            { MoraKind.Mi,   (Consonant.M, Vowel.I) },
            { MoraKind.Mya,  (Consonant.My, Vowel.A) },
            { MoraKind.Myu,  (Consonant.My, Vowel.U) },
            { MoraKind.Myo,  (Consonant.My, Vowel.O) },
            { MoraKind.Mye,  (Consonant.My, Vowel.E) },
            { MoraKind.Mu,   (Consonant.M, Vowel.U) },
            { MoraKind.Me,   (Consonant.M, Vowel.E) },
            { MoraKind.Mo,   (Consonant.M, Vowel.O) },
            // ヤ行
            { MoraKind.Ya,   (Consonant.Y, Vowel.A) },
            { MoraKind.Xya,  (Consonant.Y, Vowel.A) },
            { MoraKind.Yu,   (Consonant.Y, Vowel.U) },
            { MoraKind.Xyu,  (Consonant.Y, Vowel.U) },
            { MoraKind.Ye,   (Consonant.Y, Vowel.E) },
            { MoraKind.Yo,   (Consonant.Y, Vowel.O) },
            { MoraKind.Xyo,  (Consonant.Y, Vowel.O) },
            // ラ行
            { MoraKind.Ra,   (Consonant.R, Vowel.A) },
            { MoraKind.Ri,   (Consonant.R, Vowel.I) },
            { MoraKind.Rya,  (Consonant.Ry, Vowel.A) },
            { MoraKind.Ryu,  (Consonant.Ry, Vowel.U) },
            { MoraKind.Ryo,  (Consonant.Ry, Vowel.O) },
            { MoraKind.Rye,  (Consonant.Ry, Vowel.E) },
            { MoraKind.Ru,   (Consonant.R, Vowel.U) },
            { MoraKind.Re,   (Consonant.R, Vowel.E) },
            { MoraKind.Ro,   (Consonant.R, Vowel.O) },
            // ワ行
            { MoraKind.Wa,   (Consonant.W, Vowel.A) },
            { MoraKind.Xwa,  (Consonant.W, Vowel.A) },
            { MoraKind.Wi,   (Consonant.W, Vowel.I) },
            { MoraKind.Whi,  (Consonant.W, Vowel.I) },
            { MoraKind.Whe,  (Consonant.W, Vowel.E) },
            { MoraKind.We,   (Consonant.W, Vowel.E) },
            { MoraKind.Who,  (Consonant.W, Vowel.O) },
            { MoraKind.Wo,   (Consonant.W, Vowel.O) },
            // ン
            { MoraKind.N,    (Consonant.Nn, null) },
            // ヴ行
            { MoraKind.Vu,   (Consonant.V, Vowel.U) },
            { MoraKind.Va,   (Consonant.V, Vowel.A) },
            { MoraKind.Vi,   (Consonant.V, Vowel.I) },
            { MoraKind.Ve,   (Consonant.V, Vowel.E) },
            { MoraKind.Vo,   (Consonant.V, Vowel.O) },
            { MoraKind.Vya,  (Consonant.V, Vowel.A) },
            { MoraKind.Vyu,  (Consonant.V, Vowel.U) },
            { MoraKind.Vyo,  (Consonant.V, Vowel.O) },
            // 特殊
            { MoraKind.Xke,  (Consonant.K, Vowel.E) },
            { MoraKind.Long, (Consonant.Long, null) },
            { MoraKind.Touten,   (null, null) },
            { MoraKind.Question, (null, null) },
        };

        /// <summary>
        /// カタカナ文字列のキー長降順リスト（最長一致に使用）。
        /// </summary>
        private static readonly List<(string katakana, MoraKind kind)> _sortedKatakanaKeys;

        /// <summary>
        /// 静的コンストラクタで最長一致用のソート済みリストを構築する。
        /// </summary>
        static Pronunciation()
        {
            var keys = new List<(string katakana, MoraKind kind)>();
            foreach (MoraKind kind in Enum.GetValues(typeof(MoraKind)))
            {
                string katakana = kind.ToKatakana();
                keys.Add((katakana, kind));
            }
            // 長いキーを優先してマッチさせる（最長一致）
            keys.Sort((a, b) => b.katakana.Length.CompareTo(a.katakana.Length));
            _sortedKatakanaKeys = keys;
        }

        /// <summary>
        /// カタカナ文字列をモーラ列に変換する（最長一致法）。
        /// jpreprocess の parse_mora_str に準拠。
        /// シングルクォーテーション(') の直後のモーラは無声化される。
        /// </summary>
        private static List<Mora> ParseKatakanaToMoras(string katakana)
        {
            var moras = new List<Mora>();
            int pos = 0;
            bool nextUnvoiced = false;

            while (pos < katakana.Length)
            {
                // シングルクォーテーションは無声化マーカー
                if (katakana[pos] == '\'')
                {
                    nextUnvoiced = true;
                    pos++;
                    continue;
                }

                bool matched = false;
                for (int i = 0; i < _sortedKatakanaKeys.Count; i++)
                {
                    var (key, kind) = _sortedKatakanaKeys[i];
                    if (pos + key.Length <= katakana.Length &&
                        string.Compare(katakana, pos, key, 0, key.Length, StringComparison.Ordinal) == 0)
                    {
                        var (consonant, vowel) = MoraPhonemeMap[kind];

                        // 無声化マーカーがある場合、母音を無声母音に変換
                        if (nextUnvoiced && vowel.HasValue)
                        {
                            vowel = vowel.Value.ToUnvoiced();
                        }
                        nextUnvoiced = false;

                        moras.Add(new Mora(consonant, vowel, kind));
                        pos += key.Length;
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    throw new ArgumentException(
                        $"解析できないカタカナ文字です: '{katakana[pos]}' (位置 {pos}, 文字列: \"{katakana}\")",
                        nameof(katakana));
                }
            }

            return moras;
        }
    }
}
