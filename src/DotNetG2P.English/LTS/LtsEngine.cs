using System;
using System.Collections.Generic;

namespace DotNetG2P.English.LTS
{
    /// <summary>
    /// Flite LTS（Letter-to-Sound）CARTツリーエンジン。
    /// 英単語のスペルから音素列を予測する。CMU辞書に未登録の単語に対するフォールバックとして使用。
    /// </summary>
    /// <remarks>
    /// <para>
    /// このクラスはスレッドセーフです。ツリーデータは遅延初期化後、読み取り専用で使用されます。
    /// Flite (https://github.com/festvox/flite) の cst_lts.c / lts_apply を参考に実装。
    /// </para>
    /// <para>
    /// ストレス制約: Flite LTSモデルはPrimary stress (1) と No stress (0) のみ出力します。
    /// Secondary stress (2) は生成されません。CMU辞書のSecondary stressを含む発音が必要な場合は
    /// 辞書ルックアップを使用してください。
    /// </para>
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
        /// アポストロフィを含む単語は分割して各部分をLTS処理し結合する。
        /// ハイフンなど英字以外の文字を含む単語はnullを返す。
        /// </summary>
        /// <param name="word">入力単語（英字のみ、小文字推奨）</param>
        /// <returns>
        /// 予測された音素配列。予測不可の場合はnull。
        /// ストレスはPrimary (1) と NoStress (0) のみ。Secondary (2) は生成されない。
        /// </returns>
        internal static EnglishPhoneme[]? Predict(string word)
        {
            if (string.IsNullOrEmpty(word))
                return null;

            var lowerWord = word.ToLowerInvariant();

            // アポストロフィを含む場合は分割して各部分を処理
            if (lowerWord.IndexOf('\'') >= 0 || lowerWord.IndexOf('\u2019') >= 0)
            {
                return PredictWithApostrophe(lowerWord);
            }

            // 英字以外を含む場合はスキップ
            for (var i = 0; i < lowerWord.Length; i++)
            {
                var c = lowerWord[i];
                if (c < 'a' || c > 'z')
                    return null;
            }

            var modelData = ModelData;
            var result = new List<EnglishPhoneme>();

            // パディング付き文字配列を構築: "000#word#000"（string.Concatによるアロケーション回避）
            var paddedLen = Padding.Length + lowerWord.Length + 4; // "000#" + word + "#000"
            var padded = new char[paddedLen];
            padded[0] = '0'; padded[1] = '0'; padded[2] = '0'; padded[3] = '#';
            lowerWord.CopyTo(0, padded, Padding.Length, lowerWord.Length);
            var suffixStart = Padding.Length + lowerWord.Length;
            padded[suffixStart] = '#'; padded[suffixStart + 1] = '0';
            padded[suffixStart + 2] = '0'; padded[suffixStart + 3] = '0';

            // 各文字を順方向に処理（先頭→末尾）
            // パディング後の文字列中、単語部分のインデックスは [4, 4+len-1]
            var wordStart = Padding.Length; // 4
            var wordEnd = wordStart + lowerWord.Length - 1;

            // コンテキスト窓バッファをループ外に配置し再利用
            var fvalBuff = new byte[ContextSize];

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

                // コンテキスト窓を構築（バッファ再利用）
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
                        contextPos < paddedLen ? (byte)padded[contextPos] : (byte)'0';
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
        /// アポストロフィを含む単語を分割してLTS処理し結合する。
        /// </summary>
        private static EnglishPhoneme[]? PredictWithApostrophe(string lowerWord)
        {
            // アポストロフィ（ASCII ' と U+2019）で分割
            var parts = lowerWord.Split(new[] { '\'', '\u2019' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return null;

            var result = new List<EnglishPhoneme>();
            foreach (var part in parts)
            {
                // 各部分を個別にPredict（再帰呼び出しだが、アポストロフィは除去済みなので通常パスに入る）
                var partResult = Predict(part);
                if (partResult != null)
                    result.AddRange(partResult);
            }

            return result.Count > 0 ? result.ToArray() : null;
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
