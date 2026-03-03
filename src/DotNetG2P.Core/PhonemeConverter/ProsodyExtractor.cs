using System.Collections.Generic;
using DotNetG2P.Internal;
using DotNetG2P.Models;

namespace DotNetG2P.PhonemeConverter
{
    /// <summary>
    /// NjdNodeリストからESPnet韻律記号付き文字列を生成する。
    /// ESPnet/VOICEVOX式の韻律記号:
    /// ^ : 発話開始、$ : 発話終了、[ : アクセント上昇、] : アクセント下降
    /// # : ポーズ（アクセント句間）、_ : モーラ間区切り、? : 疑問
    /// </summary>
    public static class ProsodyExtractor
    {
        /// <summary>
        /// NjdNodeリストからESPnet韻律記号付き文字列を生成する。
        /// </summary>
        /// <param name="nodes">NJD処理済みのノードリスト</param>
        /// <returns>韻律記号付き音素文字列</returns>
        public static string Extract(IReadOnlyList<NjdNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return "^ $";

            var sb = new ValueStringBuilder(nodes.Count * 8);
            sb.Append('^');

            // 発話全体でのモーラ出力済みフラグ（先頭モーラの前に _ を出さないため）
            bool hasPrevMora = false;
            // ] が直前に出力されたフラグ（] はモーラ間区切りを兼ねるため次の _ をスキップ）
            bool afterFall = false;

            for (int nodeIdx = 0; nodeIdx < nodes.Count; nodeIdx++)
            {
                var node = nodes[nodeIdx];
                if (node.IsEmpty)
                    continue;

                var pron = node.Pronunciation;
                if (pron == null || pron.IsEmpty)
                    continue;

                // Toutenノード → ポーズ挿入
                if (pron.IsTouten)
                {
                    if (hasPrevMora)
                        sb.Append(" #");
                    // ポーズの後はモーラ間区切りをリセット
                    hasPrevMora = false;
                    afterFall = false;
                    continue;
                }

                // Questionノード → 疑問記号
                if (pron.IsQuestion)
                {
                    sb.Append(" ?");
                    hasPrevMora = true;
                    afterFall = false;
                    continue;
                }

                // 通常ノード: モーラの音素を出力し、アクセント記号を挿入
                var moras = pron.Moras;
                int accentType = node.AccentType;
                int moraIndex = 0; // 1-based、このノード内でのモーラ番号

                for (int i = 0; i < moras.Count; i++)
                {
                    var mora = moras[i];

                    // Touten/Questionモーラはスキップ
                    if (mora.Kind == MoraKind.Touten || mora.Kind == MoraKind.Question)
                        continue;

                    var phoneme = mora.ToPhonemeString();
                    if (string.IsNullOrEmpty(phoneme))
                        continue;

                    moraIndex++; // 1-based

                    // モーラの前に挿入するセパレータを決定
                    // [ と ] はモーラ間区切り _ の代わりに機能する
                    bool needRise = NeedRise(accentType, moraIndex);

                    if (needRise)
                    {
                        // [ はモーラ間区切り _ の代わりに挿入
                        sb.Append(" [");
                    }
                    else if (hasPrevMora && !afterFall)
                    {
                        // 前のモーラがあり、] の直後でなければ _ で区切る
                        sb.Append(" _");
                    }

                    // モーラの音素を出力（"k o" 等はスペースを含む）
                    sb.Append(' ');
                    sb.Append(phoneme);
                    hasPrevMora = true;
                    afterFall = false;

                    // アクセント下降記号 ] の挿入
                    if (accentType > 0 && moraIndex == accentType)
                    {
                        sb.Append(" ]");
                        afterFall = true;
                    }
                }
            }

            sb.Append(" $");
            return sb.ToStringAndDispose();
        }

        /// <summary>
        /// 指定のモーラ位置でアクセント上昇記号 [ が必要かどうかを判定する。
        /// </summary>
        private static bool NeedRise(int accentType, int moraIndex)
        {
            // 頭高型(accent=1): 第1モーラの前に [
            if (accentType == 1 && moraIndex == 1)
                return true;

            // 平板型(accent=0) / 中高型/尾高型(accent>=2): 第2モーラの前に [
            if (accentType != 1 && moraIndex == 2)
                return true;

            return false;
        }
    }
}
