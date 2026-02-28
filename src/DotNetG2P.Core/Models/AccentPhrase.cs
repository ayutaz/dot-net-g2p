using System.Collections.Generic;

namespace DotNetG2P.Models
{
    /// <summary>
    /// アクセント句（VOICEVOX互換）。
    /// 1つ以上のモーラとアクセント位置で構成される。
    /// </summary>
    public sealed class AccentPhrase
    {
        /// <summary>モーラのリスト</summary>
        public List<Mora> Moras { get; set; }

        /// <summary>アクセント核位置</summary>
        public int Accent { get; set; }

        /// <summary>ポーズモーラ（句末のポーズ）</summary>
        public Mora? PauseMora { get; set; }

        /// <summary>疑問文かどうか</summary>
        public bool IsInterrogative { get; set; }

        public AccentPhrase()
        {
            Moras = new List<Mora>();
            Accent = 0;
            PauseMora = null;
            IsInterrogative = false;
        }

        public AccentPhrase(List<Mora> moras, int accent)
        {
            Moras = moras;
            Accent = accent;
            PauseMora = null;
            IsInterrogative = false;
        }
    }
}
