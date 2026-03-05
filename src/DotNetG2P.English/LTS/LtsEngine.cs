using System;
using System.Collections.Generic;

namespace DotNetG2P.English.LTS
{
    /// <summary>
    /// Flite LTS（Letter-to-Sound）CARTツリーエンジン。
    /// 英単語のスペルから音素列を予測する。CMU辞書に未登録の単語に対するフォールバックとして使用。
    /// </summary>
    /// <remarks>
    /// このクラスはスレッドセーフです。ツリーデータは遅延初期化後、読み取り専用で使用されます。
    /// Flite (https://github.com/festvox/flite) の cst_lts.c / lts_apply を参考に実装。
    /// </remarks>
    internal static class LtsEngine
    {
        /// <summary>パディング文字列（単語の前後に付与）</summary>
        private const string Padding = "000#";

        /// <summary>コンテキスト窓の全要素数（左4+右4+追加特徴1）</summary>
        private const int ContextSize = LtsData.ContextWindowSize * 2 + LtsData.ContextExtraFeats;

        /// <summary>遅延初期化されたモデルデータ</summary>
        private static volatile byte[]? s_modelData;

        /// <summary>初期化ロック</summary>
        private static readonly object s_initLock = new object();

        /// <summary>
        /// モデルデータを取得する（遅延初期化、ダブルチェックロック）。
        /// </summary>
        private static byte[] ModelData
        {
            get
            {
                var data = s_modelData;
                if (data != null)
                    return data;

                lock (s_initLock)
                {
                    data = s_modelData;
                    if (data != null)
                        return data;

                    data = LtsData.LoadModelData();
                    s_modelData = data;
                    return data;
                }
            }
        }

        /// <summary>
        /// 英単語のスペルからARPAbet音素列を予測する。
        /// </summary>
        /// <param name="word">入力単語（英字のみ、小文字推奨）</param>
        /// <returns>予測された音素配列。予測不可の場合はnull。</returns>
        internal static EnglishPhoneme[]? Predict(string word)
        {
            if (string.IsNullOrEmpty(word))
                return null;

            var lowerWord = word.ToLowerInvariant();

            // 英字以外を含む場合はスキップ
            for (var i = 0; i < lowerWord.Length; i++)
            {
                var c = lowerWord[i];
                if (c < 'a' || c > 'z')
                    return null;
            }

            var modelData = ModelData;
            var result = new List<EnglishPhoneme>();

            // パディング付き文字列を構築: "000#word#000"
            var padded = string.Concat(Padding, lowerWord, "#000");

            // 各文字を順方向に処理（先頭→末尾）
            // パディング後の文字列中、単語部分のインデックスは [4, 4+len-1]
            var wordStart = Padding.Length; // 4
            var wordEnd = wordStart + lowerWord.Length - 1;

            for (var pos = wordStart; pos <= wordEnd; pos++)
            {
                var letter = padded[pos];

                // 英字以外はスキップ（パディング文字 '0' や '#' は処理しない）
                if (letter < 'a' || letter > 'z')
                    continue;

                var letterIdx = letter - 'a';
                if (letterIdx < 0 || letterIdx >= LtsData.LetterIndex.Length)
                    continue;

                var treeStart = LtsData.LetterIndex[letterIdx];

                // コンテキスト窓を構築
                var fvalBuff = new byte[ContextSize];

                // 左コンテキスト: pos-4, pos-3, pos-2, pos-1
                for (var j = 0; j < LtsData.ContextWindowSize; j++)
                {
                    var contextPos = pos - LtsData.ContextWindowSize + j;
                    fvalBuff[j] = contextPos >= 0 ? (byte)padded[contextPos] : (byte)'0';
                }

                // 右コンテキスト: pos+1, pos+2, pos+3, pos+4
                for (var j = 0; j < LtsData.ContextWindowSize; j++)
                {
                    var contextPos = pos + 1 + j;
                    fvalBuff[LtsData.ContextWindowSize + j] =
                        contextPos < padded.Length ? (byte)padded[contextPos] : (byte)'0';
                }

                // 追加特徴（POS: デフォルト "0"）
                fvalBuff[LtsData.ContextWindowSize * 2] = (byte)'0';

                // CARTツリートラバーサル
                var phoneIdx = TraverseTree(modelData, treeStart, fvalBuff);
                if (phoneIdx < 0 || phoneIdx >= LtsPhoneMapping.PhoneToArpabet.Length)
                    continue;

                var mapped = LtsPhoneMapping.PhoneToArpabet[phoneIdx];
                if (mapped == null) // epsilon
                    continue;

                result.AddRange(mapped);
            }

            if (result.Count == 0)
                return null;

            return result.ToArray();
        }

        /// <summary>
        /// CARTツリーをトラバースしてリーフノードの音素インデックスを返す。
        /// </summary>
        /// <param name="data">モデルバイナリデータ</param>
        /// <param name="startNode">ツリー開始ノードインデックス</param>
        /// <param name="fvalBuff">コンテキスト窓のバイト配列</param>
        /// <returns>PhoneTableインデックス。エラー時は-1。</returns>
        private static int TraverseTree(byte[] data, int startNode, byte[] fvalBuff)
        {
            var nodeIdx = startNode;

            // 無限ループ防止: ノード数の上限
            var maxNodes = data.Length / LtsData.NodeSize;
            var iterations = 0;

            while (iterations < maxNodes)
            {
                iterations++;

                var offset = nodeIdx * LtsData.NodeSize;
                if (offset + LtsData.NodeSize > data.Length)
                    return -1;

                var feat = data[offset];
                var val = data[offset + 1];
                var qtrue = (ushort)(data[offset + 2] | (data[offset + 3] << 8));
                var qfalse = (ushort)(data[offset + 4] | (data[offset + 5] << 8));

                // リーフノード: feat == EndOfRule (255)
                if (feat == LtsData.EndOfRule)
                    return val;

                // 分岐: コンテキスト窓のfeat位置の値とvalを比較
                if (feat < fvalBuff.Length && fvalBuff[feat] == val)
                {
                    nodeIdx = qtrue;
                }
                else
                {
                    nodeIdx = qfalse;
                }
            }

            // 最大反復回数を超えた（ツリーに問題がある）
            return -1;
        }
    }
}
