namespace DotNetG2P.Chinese
{
    /// <summary>
    /// 中国語ピンインの声調変調（Tone Sandhi）を処理する。
    /// 三声連読変調、"一"変調、"不"変調の3ルールを適用する。
    /// </summary>
    internal static class ToneSandhiProcessor
    {
        private const char Yi = '\u4E00';  // 一
        private const char Bu = '\u4E0D';  // 不
        private const char Di = '\u7B2C';  // 第

        /// <summary>
        /// 声調変調を適用する。
        /// ピンイン配列を直接変更（in-place mutation）する。
        /// </summary>
        /// <param name="pinyins">声調記号付きピンイン配列（変更される）</param>
        /// <param name="originalChars">元の漢字文字配列（一/不判定用）</param>
        public static void Apply(string[] pinyins, char[] originalChars)
        {
            if (pinyins == null || originalChars == null)
                return;
            if (pinyins.Length == 0 || pinyins.Length != originalChars.Length)
                return;

            // 処理順序: "一"変調 → "不"変調 → 三声連読
            ApplyYiSandhi(pinyins, originalChars);
            ApplyBuSandhi(pinyins, originalChars);
            ApplyThirdToneSandhi(pinyins, originalChars);
        }

        /// <summary>"一" (yī) の声調変調を適用する。</summary>
        private static void ApplyYiSandhi(string[] pinyins, char[] originalChars)
        {
            for (int i = 0; i < pinyins.Length; i++)
            {
                if (originalChars[i] != Yi)
                    continue;

                // 序数例外: 直前が "第" の場合は変調しない
                if (i > 0 && originalChars[i - 1] == Di)
                    continue;

                // 次の漢字スロットを探す
                int next = FindNextHanziSlot(originalChars, i + 1);
                if (next < 0)
                    continue; // 文末は変調しない

                var nextTone = ToneConverter.ExtractTone(pinyins[next]);

                if (nextTone == Tone.Fourth)
                {
                    // 4声の前: yī → yí（2声）
                    pinyins[i] = ChangeTone(pinyins[i], Tone.Second);
                }
                else if (nextTone == Tone.First || nextTone == Tone.Second || nextTone == Tone.Third)
                {
                    // 1声/2声/3声の前: yī → yì（4声）
                    pinyins[i] = ChangeTone(pinyins[i], Tone.Fourth);
                }
            }
        }

        /// <summary>"不" (bù) の声調変調を適用する。</summary>
        private static void ApplyBuSandhi(string[] pinyins, char[] originalChars)
        {
            for (int i = 0; i < pinyins.Length; i++)
            {
                if (originalChars[i] != Bu)
                    continue;

                // 次の漢字スロットを探す
                int next = FindNextHanziSlot(originalChars, i + 1);
                if (next < 0)
                    continue;

                var nextTone = ToneConverter.ExtractTone(pinyins[next]);

                if (nextTone == Tone.Fourth)
                {
                    // 4声の前: bù → bú（2声）
                    pinyins[i] = ChangeTone(pinyins[i], Tone.Second);
                }
            }
        }

        /// <summary>三声連読の声調変調を適用する。</summary>
        private static void ApplyThirdToneSandhi(string[] pinyins, char[] originalChars)
        {
            // 連続する3声のグループを見つけて、最後以外を2声に変更
            int i = 0;
            while (i < pinyins.Length)
            {
                // 非漢字スロットはスキップ
                if (!IsHanziSlot(originalChars[i]))
                {
                    i++;
                    continue;
                }

                var tone = ToneConverter.ExtractTone(pinyins[i]);
                if (tone != Tone.Third)
                {
                    i++;
                    continue;
                }

                // 3声の連続範囲を特定
                int start = i;
                int j = i + 1;
                while (j < pinyins.Length)
                {
                    if (!IsHanziSlot(originalChars[j]))
                    {
                        j++;
                        continue;
                    }

                    if (ToneConverter.ExtractTone(pinyins[j]) != Tone.Third)
                        break;

                    j++;
                }

                // 漢字スロットのうち3声のものを集める
                int lastThirdIndex = -1;
                for (int k = start; k < j; k++)
                {
                    if (IsHanziSlot(originalChars[k]) && ToneConverter.ExtractTone(pinyins[k]) == Tone.Third)
                        lastThirdIndex = k;
                }

                // 最後の3声以外を2声に変更
                if (lastThirdIndex > start)
                {
                    for (int k = start; k < lastThirdIndex; k++)
                    {
                        if (IsHanziSlot(originalChars[k]) && ToneConverter.ExtractTone(pinyins[k]) == Tone.Third)
                        {
                            pinyins[k] = ChangeTone(pinyins[k], Tone.Second);
                        }
                    }
                }

                i = j;
            }
        }

        /// <summary>ピンインの声調を変更する。</summary>
        private static string ChangeTone(string pinyin, Tone newTone)
        {
            var bare = ToneConverter.RemoveTone(pinyin);
            var numbered = bare + ((int)newTone).ToString();
            return ToneConverter.ToToneMarked(numbered);
        }

        /// <summary>指定位置以降で最初の漢字スロットのインデックスを返す。見つからなければ -1。</summary>
        private static int FindNextHanziSlot(char[] originalChars, int startIndex)
        {
            for (int i = startIndex; i < originalChars.Length; i++)
            {
                if (IsHanziSlot(originalChars[i]))
                    return i;
            }
            return -1;
        }

        /// <summary>漢字スロットかどうかを判定する。</summary>
        private static bool IsHanziSlot(char c)
        {
            // '\0' や非CJK文字はスキップ
            if (c == '\0')
                return false;

            // CJK統合漢字の範囲チェック (U+4E00-U+9FFF)
            // CJK統合漢字拡張A (U+3400-U+4DBF)
            // CJK互換漢字 (U+F900-U+FAFF)
            return (c >= '\u4E00' && c <= '\u9FFF')
                || (c >= '\u3400' && c <= '\u4DBF')
                || (c >= '\uF900' && c <= '\uFAFF');
        }
    }
}
