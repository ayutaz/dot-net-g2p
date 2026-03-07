// 自動生成ファイル - 手動で編集しないでください
// Flite LTS音素テーブルとARPAbet enumのマッピング

using System;
using System.Collections.Generic;

namespace DotNetG2P.English.LTS
{
    /// <summary>
    /// Flite LTS出力音素をARPAbet EnglishPhonemeに変換するマッピング。
    /// </summary>
    internal static class LtsPhoneMapping
    {
        /// <summary>
        /// LTS音素テーブルインデックスからEnglishPhoneme配列へのマッピング。
        /// epsilon（インデックス0）はnull、二重音素は2要素配列。
        /// </summary>
        internal static readonly EnglishPhoneme[]?[] PhoneToArpabet = BuildMapping();

        private static EnglishPhoneme[]?[] BuildMapping()
        {
            var map = new EnglishPhoneme[]?[75];

            map[0] = null; // epsilon
            map[1] = new[] { new EnglishPhoneme(ArpabetPhoneme.EH, Stress.Primary) }; // eh1
            map[2] = new[] { new EnglishPhoneme(ArpabetPhoneme.AA, Stress.Primary) }; // aa1
            map[3] = new[] { new EnglishPhoneme(ArpabetPhoneme.EY, Stress.Primary) }; // ey1
            map[4] = new[] { new EnglishPhoneme(ArpabetPhoneme.AW, Stress.Primary) }; // aw1
            map[5] = new[] { new EnglishPhoneme(ArpabetPhoneme.AH, Stress.NoStress) }; // ax0
            map[6] = new[] { new EnglishPhoneme(ArpabetPhoneme.AO, Stress.Primary) }; // ao1
            map[7] = new[] { new EnglishPhoneme(ArpabetPhoneme.AY, Stress.NoStress) }; // ay0
            map[8] = new[] { new EnglishPhoneme(ArpabetPhoneme.AA, Stress.NoStress) }; // aa0
            map[9] = new[] { new EnglishPhoneme(ArpabetPhoneme.EY, Stress.NoStress) }; // ey0
            map[10] = new[] { new EnglishPhoneme(ArpabetPhoneme.AE, Stress.Primary) }; // ae1
            map[11] = new[] { new EnglishPhoneme(ArpabetPhoneme.IH, Stress.Primary) }; // ih1
            map[12] = new[] { new EnglishPhoneme(ArpabetPhoneme.AW, Stress.NoStress) }; // aw0
            map[13] = new[] { new EnglishPhoneme(ArpabetPhoneme.OW, Stress.NoStress) }; // ow0
            map[14] = new[] { new EnglishPhoneme(ArpabetPhoneme.AO, Stress.NoStress) }; // ao0
            map[15] = new[] { new EnglishPhoneme(ArpabetPhoneme.OW, Stress.Primary) }; // ow1
            map[16] = new[] { new EnglishPhoneme(ArpabetPhoneme.EH, Stress.NoStress) }; // eh0
            map[17] = new[] { new EnglishPhoneme(ArpabetPhoneme.IH, Stress.NoStress) }; // ih0
            map[18] = new[] { new EnglishPhoneme(ArpabetPhoneme.W, Stress.None), new EnglishPhoneme(ArpabetPhoneme.EY, Stress.Primary) }; // w-ey1
            map[19] = new[] { new EnglishPhoneme(ArpabetPhoneme.W, Stress.None), new EnglishPhoneme(ArpabetPhoneme.AH, Stress.NoStress) }; // w-ax0
            map[20] = new[] { new EnglishPhoneme(ArpabetPhoneme.Y, Stress.None), new EnglishPhoneme(ArpabetPhoneme.AH, Stress.NoStress) }; // y-ax0
            map[21] = new[] { new EnglishPhoneme(ArpabetPhoneme.AE, Stress.NoStress) }; // ae0
            map[22] = new[] { new EnglishPhoneme(ArpabetPhoneme.AY, Stress.Primary) }; // ay1
            map[23] = new[] { new EnglishPhoneme(ArpabetPhoneme.AH, Stress.NoStress) }; // ah0
            map[24] = new[] { new EnglishPhoneme(ArpabetPhoneme.AH, Stress.Primary) }; // ah1
            map[25] = new[] { new EnglishPhoneme(ArpabetPhoneme.B, Stress.None) }; // b
            map[26] = new[] { new EnglishPhoneme(ArpabetPhoneme.CH, Stress.None) }; // ch
            map[27] = new[] { new EnglishPhoneme(ArpabetPhoneme.K, Stress.None) }; // k
            map[28] = new[] { new EnglishPhoneme(ArpabetPhoneme.S, Stress.None) }; // s
            map[29] = new[] { new EnglishPhoneme(ArpabetPhoneme.T, Stress.None), new EnglishPhoneme(ArpabetPhoneme.S, Stress.None) }; // t-s
            map[30] = new[] { new EnglishPhoneme(ArpabetPhoneme.SH, Stress.None) }; // sh
            map[31] = new[] { new EnglishPhoneme(ArpabetPhoneme.D, Stress.None) }; // d
            map[32] = new[] { new EnglishPhoneme(ArpabetPhoneme.T, Stress.None) }; // t
            map[33] = new[] { new EnglishPhoneme(ArpabetPhoneme.JH, Stress.None) }; // jh
            map[34] = new[] { new EnglishPhoneme(ArpabetPhoneme.IY, Stress.Primary) }; // iy1
            map[35] = new[] { new EnglishPhoneme(ArpabetPhoneme.IY, Stress.NoStress) }; // iy0
            map[36] = new[] { new EnglishPhoneme(ArpabetPhoneme.UW, Stress.Primary) }; // uw1
            map[37] = new[] { new EnglishPhoneme(ArpabetPhoneme.Y, Stress.None), new EnglishPhoneme(ArpabetPhoneme.UW, Stress.Primary) }; // y-uw1
            map[38] = new[] { new EnglishPhoneme(ArpabetPhoneme.OY, Stress.Primary) }; // oy1
            map[39] = new[] { new EnglishPhoneme(ArpabetPhoneme.Y, Stress.None), new EnglishPhoneme(ArpabetPhoneme.UW, Stress.NoStress) }; // y-uw0
            map[40] = new[] { new EnglishPhoneme(ArpabetPhoneme.UW, Stress.NoStress) }; // uw0
            map[41] = new[] { new EnglishPhoneme(ArpabetPhoneme.OY, Stress.NoStress) }; // oy0
            map[42] = new[] { new EnglishPhoneme(ArpabetPhoneme.F, Stress.None) }; // f
            map[43] = new[] { new EnglishPhoneme(ArpabetPhoneme.G, Stress.None) }; // g
            map[44] = new[] { new EnglishPhoneme(ArpabetPhoneme.ZH, Stress.None) }; // zh
            map[45] = new[] { new EnglishPhoneme(ArpabetPhoneme.HH, Stress.None) }; // hh
            map[46] = new[] { new EnglishPhoneme(ArpabetPhoneme.Y, Stress.None) }; // y
            map[47] = new[] { new EnglishPhoneme(ArpabetPhoneme.L, Stress.None) }; // l
            map[48] = new[] { new EnglishPhoneme(ArpabetPhoneme.AH, Stress.NoStress), new EnglishPhoneme(ArpabetPhoneme.L, Stress.None) }; // ax0-l
            map[49] = new[] { new EnglishPhoneme(ArpabetPhoneme.M, Stress.None) }; // m
            map[50] = new[] { new EnglishPhoneme(ArpabetPhoneme.AH, Stress.NoStress), new EnglishPhoneme(ArpabetPhoneme.M, Stress.None) }; // ax0-m
            map[51] = new[] { new EnglishPhoneme(ArpabetPhoneme.M, Stress.None), new EnglishPhoneme(ArpabetPhoneme.AE, Stress.Primary) }; // m-ae1
            map[52] = new[] { new EnglishPhoneme(ArpabetPhoneme.M, Stress.None), new EnglishPhoneme(ArpabetPhoneme.AH, Stress.NoStress) }; // m-ax0
            map[53] = new[] { new EnglishPhoneme(ArpabetPhoneme.NG, Stress.None) }; // ng
            map[54] = new[] { new EnglishPhoneme(ArpabetPhoneme.N, Stress.None) }; // n
            map[55] = new[] { new EnglishPhoneme(ArpabetPhoneme.N, Stress.None), new EnglishPhoneme(ArpabetPhoneme.Y, Stress.None) }; // n-y
            map[56] = new[] { new EnglishPhoneme(ArpabetPhoneme.UH, Stress.Primary) }; // uh1
            map[57] = new[] { new EnglishPhoneme(ArpabetPhoneme.UH, Stress.NoStress) }; // uh0
            map[58] = new[] { new EnglishPhoneme(ArpabetPhoneme.W, Stress.None) }; // w
            map[59] = new[] { new EnglishPhoneme(ArpabetPhoneme.W, Stress.None), new EnglishPhoneme(ArpabetPhoneme.AH, Stress.Primary) }; // w-ah1
            map[60] = new[] { new EnglishPhoneme(ArpabetPhoneme.ER, Stress.Primary) }; // er1
            map[61] = new[] { new EnglishPhoneme(ArpabetPhoneme.P, Stress.None) }; // p
            map[62] = new[] { new EnglishPhoneme(ArpabetPhoneme.R, Stress.None) }; // r
            map[63] = new[] { new EnglishPhoneme(ArpabetPhoneme.ER, Stress.NoStress) }; // er0
            map[64] = new[] { new EnglishPhoneme(ArpabetPhoneme.Z, Stress.None) }; // z
            map[65] = new[] { new EnglishPhoneme(ArpabetPhoneme.TH, Stress.None) }; // th
            map[66] = new[] { new EnglishPhoneme(ArpabetPhoneme.DH, Stress.None) }; // dh
            map[67] = new[] { new EnglishPhoneme(ArpabetPhoneme.Y, Stress.None), new EnglishPhoneme(ArpabetPhoneme.ER, Stress.NoStress) }; // y-er0
            map[68] = new[] { new EnglishPhoneme(ArpabetPhoneme.Y, Stress.None), new EnglishPhoneme(ArpabetPhoneme.UH, Stress.Primary) }; // y-uh1
            map[69] = new[] { new EnglishPhoneme(ArpabetPhoneme.Y, Stress.None), new EnglishPhoneme(ArpabetPhoneme.ER, Stress.Primary) }; // y-er1
            map[70] = new[] { new EnglishPhoneme(ArpabetPhoneme.V, Stress.None) }; // v
            map[71] = new[] { new EnglishPhoneme(ArpabetPhoneme.K, Stress.None), new EnglishPhoneme(ArpabetPhoneme.S, Stress.None) }; // k-s
            map[72] = new[] { new EnglishPhoneme(ArpabetPhoneme.G, Stress.None), new EnglishPhoneme(ArpabetPhoneme.ZH, Stress.None) }; // g-zh
            map[73] = new[] { new EnglishPhoneme(ArpabetPhoneme.K, Stress.None), new EnglishPhoneme(ArpabetPhoneme.SH, Stress.None) }; // k-sh
            map[74] = new[] { new EnglishPhoneme(ArpabetPhoneme.G, Stress.None), new EnglishPhoneme(ArpabetPhoneme.Z, Stress.None) }; // g-z

            return map;
        }
    }
}
