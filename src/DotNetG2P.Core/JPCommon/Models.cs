using System.Collections.Generic;

namespace DotNetG2P.JPCommon
{
    /// <summary>
    /// JPCommon音素。フルコンテキストラベル生成の最小単位。
    /// 前後の音素への双方向参照と、所属モーラへの参照を持つ。
    /// </summary>
    public sealed class JPPhoneme
    {
        /// <summary>音素名 ("k", "o", "N", "cl", "-", "pau", "sil" 等)</summary>
        public string Phoneme { get; set; }

        /// <summary>前の音素（先頭の場合はnull）</summary>
        public JPPhoneme? Prev { get; set; }

        /// <summary>次の音素（末尾の場合はnull）</summary>
        public JPPhoneme? Next { get; set; }

        /// <summary>所属モーラ（"sil"/"pau"等のポーズ音素はnull）</summary>
        public JPMora? ParentMora { get; set; }

        /// <summary>モーラ内のインデックス（0始まり）</summary>
        public int IndexInMora { get; set; }

        public JPPhoneme(string phoneme)
        {
            Phoneme = phoneme;
        }

        public override string ToString() => Phoneme;
    }

    /// <summary>
    /// JPCommonモーラ。音素のリストを保持し、所属単語への参照を持つ。
    /// 通常1-2個の音素（子音+母音、または母音のみ）で構成される。
    /// </summary>
    public sealed class JPMora
    {
        /// <summary>音素リスト（通常1-2個: 子音+母音、または母音のみ）</summary>
        public List<JPPhoneme> Phonemes { get; }

        /// <summary>所属単語</summary>
        public JPWord? ParentWord { get; set; }

        /// <summary>所属アクセント句（ParentWord.ParentAccentPhrase の便利アクセサ）</summary>
        public JPAccentPhrase? ParentAccentPhrase => ParentWord?.ParentAccentPhrase;

        /// <summary>アクセント句内のインデックス（0始まり、全Word横断）</summary>
        public int IndexInAccentPhrase { get; set; }

        public JPMora()
        {
            Phonemes = new List<JPPhoneme>(2);
        }

        public override string ToString()
        {
            if (Phonemes.Count == 0) return string.Empty;
            if (Phonemes.Count == 1) return Phonemes[0].Phoneme;
            var sb = new System.Text.StringBuilder();
            sb.Append(Phonemes[0].Phoneme);
            for (int i = 1; i < Phonemes.Count; i++)
            {
                sb.Append(' ');
                sb.Append(Phonemes[i].Phoneme);
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// JPCommon単語。モーラのリストと品詞情報を保持する。
    /// jpreprocess の jpcommon::label::Word に準拠。
    /// </summary>
    public sealed class JPWord
    {
        /// <summary>モーラリスト</summary>
        public List<JPMora> Moras { get; }

        /// <summary>品詞ID（フルコンテキストラベルのp1フィールド、null=未定義 → "xx"）</summary>
        public int? PosId { get; set; }

        /// <summary>活用型ID（フルコンテキストラベルのp2フィールド、null=未定義 → "xx"）</summary>
        public int? CTypeId { get; set; }

        /// <summary>活用形ID（フルコンテキストラベルのp3フィールド、null=未定義 → "xx"）</summary>
        public int? CFormId { get; set; }

        /// <summary>所属アクセント句</summary>
        public JPAccentPhrase? ParentAccentPhrase { get; set; }

        /// <summary>アクセント句内のインデックス（0始まり）</summary>
        public int IndexInAccentPhrase { get; set; }

        /// <summary>モーラ数</summary>
        public int MoraCount => Moras.Count;

        public JPWord()
        {
            Moras = new List<JPMora>(4);
        }

        public override string ToString()
        {
            return $"Word[{MoraCount}モーラ, pos={PosId?.ToString() ?? "xx"}]";
        }
    }

    /// <summary>
    /// JPCommonアクセント句。単語のリストとアクセント型を保持する。
    /// jpreprocess の jpcommon::label::AccentPhrase に準拠。
    /// 所属呼気グループへの参照を持つ。
    /// </summary>
    public sealed class JPAccentPhrase
    {
        /// <summary>単語リスト</summary>
        public List<JPWord> Words { get; }

        /// <summary>アクセント核位置（0=平板）</summary>
        public int AccentType { get; set; }

        /// <summary>所属呼気グループ</summary>
        public JPBreathGroup? ParentBreathGroup { get; set; }

        /// <summary>呼気グループ内のインデックス（0始まり）</summary>
        public int IndexInBreathGroup { get; set; }

        /// <summary>総モーラ数（全Word横断）</summary>
        public int MoraCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Words.Count; i++)
                    count += Words[i].MoraCount;
                return count;
            }
        }

        /// <summary>単語数</summary>
        public int WordCount => Words.Count;

        /// <summary>疑問文フラグ</summary>
        public bool IsInterrogative { get; set; }

        public JPAccentPhrase()
        {
            Words = new List<JPWord>(2);
        }

        /// <summary>
        /// 全Word横断でモーラのフラットリストを取得する。
        /// </summary>
        public List<JPMora> AllMoras()
        {
            var result = new List<JPMora>(MoraCount);
            for (int i = 0; i < Words.Count; i++)
            {
                var moras = Words[i].Moras;
                for (int j = 0; j < moras.Count; j++)
                    result.Add(moras[j]);
            }
            return result;
        }

        public override string ToString()
        {
            return $"AP[{WordCount}語, {MoraCount}モーラ, accent={AccentType}]";
        }
    }

    /// <summary>
    /// JPCommon呼気グループ。ポーズで区切られたアクセント句の連続。
    /// 所属発話への参照を持つ。
    /// </summary>
    public sealed class JPBreathGroup
    {
        /// <summary>アクセント句リスト</summary>
        public List<JPAccentPhrase> AccentPhrases { get; }

        /// <summary>所属発話</summary>
        public JPUtterance? ParentUtterance { get; set; }

        /// <summary>発話内のインデックス（0始まり）</summary>
        public int IndexInUtterance { get; set; }

        /// <summary>アクセント句数</summary>
        public int AccentPhraseCount => AccentPhrases.Count;

        /// <summary>総モーラ数</summary>
        public int MoraCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < AccentPhrases.Count; i++)
                    count += AccentPhrases[i].MoraCount;
                return count;
            }
        }

        public JPBreathGroup()
        {
            AccentPhrases = new List<JPAccentPhrase>(4);
        }

        public override string ToString()
        {
            return $"BG[{AccentPhraseCount}句, {MoraCount}モーラ]";
        }
    }

    /// <summary>
    /// JPCommon発話。最上位の階層単位で、呼気グループのリストを保持する。
    /// </summary>
    public sealed class JPUtterance
    {
        /// <summary>呼気グループリスト</summary>
        public List<JPBreathGroup> BreathGroups { get; }

        /// <summary>呼気グループ数</summary>
        public int BreathGroupCount => BreathGroups.Count;

        /// <summary>総アクセント句数</summary>
        public int AccentPhraseCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < BreathGroups.Count; i++)
                    count += BreathGroups[i].AccentPhraseCount;
                return count;
            }
        }

        /// <summary>総モーラ数</summary>
        public int MoraCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < BreathGroups.Count; i++)
                    count += BreathGroups[i].MoraCount;
                return count;
            }
        }

        public JPUtterance()
        {
            BreathGroups = new List<JPBreathGroup>(2);
        }

        public override string ToString()
        {
            return $"Utt[{BreathGroupCount}BG, {AccentPhraseCount}AP, {MoraCount}モーラ]";
        }
    }
}
