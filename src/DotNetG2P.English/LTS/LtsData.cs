// 自動生成ファイル - 手動で編集しないでください
// Flite (https://github.com/festvox/flite) の cmu_lts_model/cmu_lts_rules から抽出
// Flite License: Carnegie Mellon University Free Software License (BSD-like)
// 詳細はプロジェクトルートのNOTICEファイルを参照

using System;
using System.IO;
using System.Reflection;

namespace DotNetG2P.English.LTS
{
    /// <summary>
    /// Flite LTS CARTツリーデータ。CMU英語辞書のLetter-to-Sound規則。
    /// </summary>
    internal static class LtsData
    {
        /// <summary>コンテキスト窓サイズ（前後の文字数）。</summary>
        internal const int ContextWindowSize = 4;

        /// <summary>追加特徴数。</summary>
        internal const int ContextExtraFeats = 1;

        /// <summary>ツリー終端マーカー。</summary>
        internal const byte EndOfRule = 255;

        /// <summary>ノードサイズ（バイト）。</summary>
        internal const int NodeSize = 6;

        /// <summary>
        /// 音素テーブル。インデックス→音素文字列のマッピング。
        /// ツリーのリーフノードのval値がこのテーブルのインデックスに対応する。
        /// </summary>
        internal static readonly string[] PhoneTable = new string[]
        {
            "epsilon", // 0
            "eh1", // 1
            "aa1", // 2
            "ey1", // 3
            "aw1", // 4
            "ax0", // 5
            "ao1", // 6
            "ay0", // 7
            "aa0", // 8
            "ey0", // 9
            "ae1", // 10
            "ih1", // 11
            "aw0", // 12
            "ow0", // 13
            "ao0", // 14
            "ow1", // 15
            "eh0", // 16
            "ih0", // 17
            "w-ey1", // 18
            "w-ax0", // 19
            "y-ax0", // 20
            "ae0", // 21
            "ay1", // 22
            "ah0", // 23
            "ah1", // 24
            "b", // 25
            "ch", // 26
            "k", // 27
            "s", // 28
            "t-s", // 29
            "sh", // 30
            "d", // 31
            "t", // 32
            "jh", // 33
            "iy1", // 34
            "iy0", // 35
            "uw1", // 36
            "y-uw1", // 37
            "oy1", // 38
            "y-uw0", // 39
            "uw0", // 40
            "oy0", // 41
            "f", // 42
            "g", // 43
            "zh", // 44
            "hh", // 45
            "y", // 46
            "l", // 47
            "ax0-l", // 48
            "m", // 49
            "ax0-m", // 50
            "m-ae1", // 51
            "m-ax0", // 52
            "ng", // 53
            "n", // 54
            "n-y", // 55
            "uh1", // 56
            "uh0", // 57
            "w", // 58
            "w-ah1", // 59
            "er1", // 60
            "p", // 61
            "r", // 62
            "er0", // 63
            "z", // 64
            "th", // 65
            "dh", // 66
            "y-er0", // 67
            "y-uh1", // 68
            "y-er1", // 69
            "v", // 70
            "k-s", // 71
            "g-zh", // 72
            "k-sh", // 73
            "g-z" // 74
        };

        /// <summary>
        /// 各文字(a-z)のツリー開始ノードインデックス。
        /// index 0=a, 1=b, ..., 25=z。
        /// </summary>
        internal static readonly ushort[] LetterIndex = new ushort[]
        {
            0, // a
            5371, // b
            5414, // c
            6048, // d
            6256, // e
            10649, // f
            10666, // g
            11293, // h
            11522, // i
            15403, // j
            15514, // k
            15533, // l
            15900, // m
            15942, // n
            16176, // o
            19793, // p
            19830, // q
            19831, // r
            21775, // s
            22667, // t
            22957, // u
            24709, // v
            24719, // w
            24877, // x
            24923, // y
            25377 // z
        };

        /// <summary>
        /// CARTツリーモデルバイナリデータを埋め込みリソースから読み込む。
        /// 各ノードは6バイト: feat(1), val(1), qtrue(2, LE), qfalse(2, LE)。
        /// </summary>
        internal static byte[] LoadModelData()
        {
            var assembly = typeof(LtsData).Assembly;
            using (var stream = assembly.GetManifestResourceStream("DotNetG2P.English.LTS.cmu_lts_model.bin"))
            {
                if (stream == null)
                    throw new InvalidOperationException("埋め込みリソース cmu_lts_model.bin が見つかりません。");
                var data = new byte[stream.Length];
                var totalRead = 0;
                while (totalRead < data.Length)
                {
                    var bytesRead = stream.Read(data, totalRead, data.Length - totalRead);
                    if (bytesRead == 0)
                        throw new InvalidOperationException("埋め込みリソースの読み込みが途中で終了しました。");
                    totalRead += bytesRead;
                }
                return data;
            }
        }

        /// <summary>バイト配列からLTSモデルデータを読み込む（Unity StreamingAssets / WebGL対応）。</summary>
        /// <param name="data">LTSモデルバイナリデータ。</param>
        /// <returns>読み込まれたバイト配列。</returns>
        internal static byte[] LoadModelData(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return data;
        }

        /// <summary>ストリームからLTSモデルデータを読み込む。</summary>
        /// <param name="stream">LTSモデルバイナリデータを含むストリーム。</param>
        /// <returns>読み込まれたバイト配列。</returns>
        internal static byte[] LoadModelData(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
