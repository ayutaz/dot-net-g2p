using System;
using System.Collections.Generic;

namespace DotNetG2P.Models
{
    /// <summary>
    /// 韻律特徴量（A1/A2/A3）を音素単位で保持する。
    /// uPiper等の音声合成エンジンが直接利用できる形式。
    /// </summary>
    public sealed class ProsodyFeatures
    {
        /// <summary>音素文字列配列（"sil","k","o",...）</summary>
        public IReadOnlyList<string> Phonemes { get; }

        /// <summary>アクセント核からの相対位置（モーラ単位）</summary>
        public IReadOnlyList<int> A1 { get; }

        /// <summary>アクセント句先頭からの位置（1始まり）</summary>
        public IReadOnlyList<int> A2 { get; }

        /// <summary>アクセント句末尾からの位置</summary>
        public IReadOnlyList<int> A3 { get; }

        /// <summary>音素数</summary>
        public int Count => Phonemes.Count;

        internal ProsodyFeatures(string[] phonemes, int[] a1, int[] a2, int[] a3)
        {
            Phonemes = phonemes ?? Array.Empty<string>();
            A1 = a1 ?? Array.Empty<int>();
            A2 = a2 ?? Array.Empty<int>();
            A3 = a3 ?? Array.Empty<int>();
        }
    }
}
