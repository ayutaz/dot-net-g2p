using System;
using System.Collections.Generic;
using System.Linq;
using DotNetG2P.Internal;

namespace DotNetG2P.JPCommon
{
    /// <summary>
    /// HTSフルコンテキストラベル生成ロジック。
    /// JPUtteranceからフルコンテキストラベル列を生成する。
    /// jpreprocess の feature/mod.rs に準拠。
    /// </summary>
    public static class FullContextLabel
    {
        private const string XX = "xx";

        /// <summary>
        /// JPUtteranceからフルコンテキストラベル列を生成する。
        /// </summary>
        public static List<string> Generate(JPUtterance utterance)
        {
            if (utterance == null)
                throw new ArgumentNullException(nameof(utterance));

            // 1. 全音素をフラットリストに展開
            var phonemeEntries = FlattenPhonemes(utterance);

            // 2. sil/pau挿入
            var withPauses = InsertSilAndPau(phonemeEntries, utterance);

            // 3. 各音素に対してラベル文字列を生成
            var labels = new List<string>(withPauses.Count);
            for (int i = 0; i < withPauses.Count; i++)
            {
                var entry = withPauses[i];
                string label = BuildLabel(entry, i, withPauses, utterance);
                labels.Add(label);
            }

            return labels;
        }

        // ====== 内部構造 ======

        /// <summary>
        /// フラットな音素エントリ。所属階層への参照を持つ。
        /// </summary>
        private sealed class PhonemeEntry
        {
            public string Phoneme;
            public bool IsPause; // sil or pau
            public JPMora Mora;
            public JPWord Word;
            public JPAccentPhrase AccentPhrase;
            public JPBreathGroup BreathGroup;
            public int MoraIndexInAP; // アクセント句内のモーラ位置（0始まり）
        }

        // ====== 1. 音素フラット展開 ======

        private static List<PhonemeEntry> FlattenPhonemes(JPUtterance utterance)
        {
            var result = new List<PhonemeEntry>(utterance.MoraCount * 2);

            foreach (var bg in utterance.BreathGroups)
            {
                foreach (var ap in bg.AccentPhrases)
                {
                    int moraIdx = 0;
                    foreach (var word in ap.Words)
                    {
                        foreach (var mora in word.Moras)
                        {
                            foreach (var phoneme in mora.Phonemes)
                            {
                                result.Add(new PhonemeEntry
                                {
                                    Phoneme = phoneme.Phoneme,
                                    IsPause = false,
                                    Mora = mora,
                                    Word = word,
                                    AccentPhrase = ap,
                                    BreathGroup = bg,
                                    MoraIndexInAP = moraIdx,
                                });
                            }
                            moraIdx++;
                        }
                    }
                }
            }

            return result;
        }

        // ====== 2. sil/pau挿入 ======

        private static List<PhonemeEntry> InsertSilAndPau(List<PhonemeEntry> phonemes, JPUtterance utterance)
        {
            var result = new List<PhonemeEntry>(phonemes.Count + utterance.BreathGroupCount + 1);

            // 先頭sil
            result.Add(new PhonemeEntry { Phoneme = "sil", IsPause = true });

            JPBreathGroup lastBG = null;
            foreach (var entry in phonemes)
            {
                // 呼気グループが変わったらpau挿入
                if (lastBG != null && entry.BreathGroup != lastBG)
                {
                    result.Add(new PhonemeEntry { Phoneme = "pau", IsPause = true });
                }
                result.Add(entry);
                lastBG = entry.BreathGroup;
            }

            // 末尾sil
            result.Add(new PhonemeEntry { Phoneme = "sil", IsPause = true });

            return result;
        }

        // ====== 3. ラベル文字列生成 ======

        private static string BuildLabel(PhonemeEntry current, int index, List<PhonemeEntry> all, JPUtterance utterance)
        {
            var sb = new ValueStringBuilder(256);

            // --- 音素コンテキスト (p2^p1-c+n1=n2) ---
            string p2 = GetPhonemeAt(all, index - 2);
            string p1 = GetPhonemeAt(all, index - 1);
            string c = current.Phoneme;
            string n1 = GetPhonemeAt(all, index + 1);
            string n2 = GetPhonemeAt(all, index + 2);

            sb.Append(p2); sb.Append('^'); sb.Append(p1); sb.Append('-'); sb.Append(c); sb.Append('+'); sb.Append(n1); sb.Append('='); sb.Append(n2);

            // --- A: モーラ位置情報 ---
            sb.Append("/A:");
            if (current.IsPause || current.Mora == null)
            {
                sb.Append("xx+xx+xx");
            }
            else
            {
                var ap = current.AccentPhrase;
                int moraCount = ap.MoraCount;
                int accent = NormalizeAccentForA(ap.AccentType, moraCount);
                int moraPos = current.MoraIndexInAP; // 0始まり
                int a1 = moraPos - accent + 1; // signed
                int a2 = moraPos + 1; // 1始まり、前方から
                int a3 = moraCount - moraPos; // 後方から
                sb.Append(ClampSigned(a1, 49)); sb.Append('+'); sb.Append(ClampUnsigned(a2, 49)); sb.Append('+'); sb.Append(ClampUnsigned(a3, 49));
            }

            // --- B: 前単語 ---
            sb.Append("/B:");
            if (current.IsPause)
            {
                sb.Append("xx-xx_xx");
            }
            else
            {
                var prevWord = FindPrevWord(current, index, all);
                AppendWordField(ref sb, prevWord, '-', '_');
            }

            // --- C: 現在単語 ---
            sb.Append("/C:");
            if (current.IsPause || current.Word == null)
            {
                sb.Append("xx_xx+xx");
            }
            else
            {
                AppendWordField(ref sb, current.Word, '_', '+');
            }

            // --- D: 次単語 ---
            sb.Append("/D:");
            if (current.IsPause)
            {
                sb.Append("xx+xx_xx");
            }
            else
            {
                var nextWord = FindNextWord(current, index, all);
                AppendWordField(ref sb, nextWord, '+', '_');
            }

            // --- E: 前アクセント句 ---
            sb.Append("/E:");
            var prevAP = FindPrevAccentPhrase(current, index, all);
            bool ePause = HasPauseBetweenPrevAP(current, prevAP, index, all);
            AppendAccentPhraseE(ref sb, prevAP, ePause);

            // --- F: 現在アクセント句 ---
            sb.Append("/F:");
            if (current.IsPause || current.AccentPhrase == null)
            {
                sb.Append("xx_xx#xx_xx@xx_xx|xx_xx");
            }
            else
            {
                AppendCurrentAccentPhraseF(ref sb, current.AccentPhrase, utterance);
            }

            // --- G: 次アクセント句 ---
            sb.Append("/G:");
            var nextAP = FindNextAccentPhrase(current, index, all);
            bool gPause = HasPauseBetweenNextAP(current, nextAP, index, all);
            AppendAccentPhraseG(ref sb, nextAP, gPause);

            // --- H: 前呼気グループ ---
            sb.Append("/H:");
            var prevBG = FindPrevBreathGroup(current, index, all);
            AppendBreathGroupHJ(ref sb, prevBG);

            // --- I: 現在呼気グループ ---
            sb.Append("/I:");
            if (current.IsPause || current.BreathGroup == null)
            {
                sb.Append("xx-xx@xx+xx&xx-xx|xx+xx");
            }
            else
            {
                AppendCurrentBreathGroupI(ref sb, current.BreathGroup, utterance);
            }

            // --- J: 次呼気グループ ---
            sb.Append("/J:");
            var nextBG = FindNextBreathGroup(current, index, all);
            AppendBreathGroupHJ(ref sb, nextBG);

            // --- K: 発話全体 ---
            sb.Append("/K:");
            sb.Append(ClampUnsigned(utterance.BreathGroupCount, 19));
            sb.Append('+');
            sb.Append(ClampUnsigned(utterance.AccentPhraseCount, 49));
            sb.Append('-');
            sb.Append(ClampUnsigned(utterance.MoraCount, 199));

            return sb.ToStringAndDispose();
        }

        // ====== 音素取得 ======

        private static string GetPhonemeAt(List<PhonemeEntry> all, int index)
        {
            if (index < 0 || index >= all.Count)
                return XX;
            return all[index].Phoneme;
        }

        // ====== 前後要素探索 ======

        /// <summary>
        /// 前単語を探す。BG境界（pause/sil）を越えた場合はnull（xx）を返す。
        /// </summary>
        private static JPWord FindPrevWord(PhonemeEntry current, int index, List<PhonemeEntry> all)
        {
            if (current.IsPause)
            {
                // pause音素の場合: xx
                return null;
            }

            // 通常音素: 現在単語より前の単語（BG境界を越えない）
            var currentWord = current.Word;
            for (int i = index - 1; i >= 0; i--)
            {
                if (all[i].IsPause) return null; // BG境界に達した → xx
                if (all[i].Word != null && all[i].Word != currentWord)
                    return all[i].Word;
            }
            return null;
        }

        /// <summary>
        /// 次単語を探す。BG境界（pause/sil）を越えた場合はnull（xx）を返す。
        /// </summary>
        private static JPWord FindNextWord(PhonemeEntry current, int index, List<PhonemeEntry> all)
        {
            if (current.IsPause)
            {
                // pause音素の場合: xx
                return null;
            }

            var currentWord = current.Word;
            for (int i = index + 1; i < all.Count; i++)
            {
                if (all[i].IsPause) return null; // BG境界に達した → xx
                if (all[i].Word != null && all[i].Word != currentWord)
                    return all[i].Word;
            }
            return null;
        }

        /// <summary>前アクセント句を探す。</summary>
        private static JPAccentPhrase FindPrevAccentPhrase(PhonemeEntry current, int index, List<PhonemeEntry> all)
        {
            if (current.IsPause)
            {
                // pause: 直前の非pause音素のAP
                for (int i = index - 1; i >= 0; i--)
                {
                    if (!all[i].IsPause && all[i].AccentPhrase != null)
                        return all[i].AccentPhrase;
                }
                return null;
            }

            var currentAP = current.AccentPhrase;
            for (int i = index - 1; i >= 0; i--)
            {
                if (all[i].IsPause) continue;
                if (all[i].AccentPhrase != null && all[i].AccentPhrase != currentAP)
                    return all[i].AccentPhrase;
            }
            return null;
        }

        /// <summary>次アクセント句を探す。</summary>
        private static JPAccentPhrase FindNextAccentPhrase(PhonemeEntry current, int index, List<PhonemeEntry> all)
        {
            if (current.IsPause)
            {
                for (int i = index + 1; i < all.Count; i++)
                {
                    if (!all[i].IsPause && all[i].AccentPhrase != null)
                        return all[i].AccentPhrase;
                }
                return null;
            }

            var currentAP = current.AccentPhrase;
            for (int i = index + 1; i < all.Count; i++)
            {
                if (all[i].IsPause) continue;
                if (all[i].AccentPhrase != null && all[i].AccentPhrase != currentAP)
                    return all[i].AccentPhrase;
            }
            return null;
        }

        /// <summary>前呼気グループを探す。</summary>
        private static JPBreathGroup FindPrevBreathGroup(PhonemeEntry current, int index, List<PhonemeEntry> all)
        {
            if (current.IsPause)
            {
                for (int i = index - 1; i >= 0; i--)
                {
                    if (!all[i].IsPause && all[i].BreathGroup != null)
                        return all[i].BreathGroup;
                }
                return null;
            }

            var currentBG = current.BreathGroup;
            for (int i = index - 1; i >= 0; i--)
            {
                if (all[i].IsPause) continue;
                if (all[i].BreathGroup != null && all[i].BreathGroup != currentBG)
                    return all[i].BreathGroup;
            }
            return null;
        }

        /// <summary>次呼気グループを探す。</summary>
        private static JPBreathGroup FindNextBreathGroup(PhonemeEntry current, int index, List<PhonemeEntry> all)
        {
            if (current.IsPause)
            {
                for (int i = index + 1; i < all.Count; i++)
                {
                    if (!all[i].IsPause && all[i].BreathGroup != null)
                        return all[i].BreathGroup;
                }
                return null;
            }

            var currentBG = current.BreathGroup;
            for (int i = index + 1; i < all.Count; i++)
            {
                if (all[i].IsPause) continue;
                if (all[i].BreathGroup != null && all[i].BreathGroup != currentBG)
                    return all[i].BreathGroup;
            }
            return null;
        }

        // ====== ポーズ境界判定 ======

        /// <summary>
        /// 現在のAPと前のAPの間にpauがあるか判定する。
        /// 前APと現在APが異なるBGに属する場合、true。
        /// </summary>
        private static bool HasPauseBetweenPrevAP(PhonemeEntry current, JPAccentPhrase prevAP, int index, List<PhonemeEntry> all)
        {
            if (prevAP == null) return false;
            var currentAP = current.IsPause ? null : current.AccentPhrase;
            if (currentAP == null)
            {
                // pause音素の場合: 直前のAPと直後のAPの間にpau（=自分自身）がある
                return true;
            }
            // 前APと現在APが異なるBGに属するか
            return prevAP.ParentBreathGroup != currentAP.ParentBreathGroup;
        }

        /// <summary>
        /// 現在のAPと次のAPの間にpauがあるか判定する。
        /// 次APと現在APが異なるBGに属する場合、true。
        /// </summary>
        private static bool HasPauseBetweenNextAP(PhonemeEntry current, JPAccentPhrase nextAP, int index, List<PhonemeEntry> all)
        {
            if (nextAP == null) return false;
            var currentAP = current.IsPause ? null : current.AccentPhrase;
            if (currentAP == null)
            {
                // pause音素の場合: 自分自身がpauなので間にポーズがある
                return true;
            }
            // 現在APと次APが異なるBGに属するか
            return currentAP.ParentBreathGroup != nextAP.ParentBreathGroup;
        }

        // ====== フォーマットヘルパー ======

        /// <summary>B/C/D: 単語のPOS/CType/CFormフィールドを出力する。区切り文字はB/C/Dで異なる。</summary>
        private static void AppendWordField(ref ValueStringBuilder sb, JPWord word, char sep1, char sep2)
        {
            if (word == null)
            {
                sb.Append(XX); sb.Append(sep1); sb.Append(XX); sb.Append(sep2); sb.Append(XX);
                return;
            }
            string posStr = word.PosId.HasValue ? word.PosId.Value.ToString("D2") : XX;
            string ctypeStr = word.CTypeId.HasValue ? word.CTypeId.Value.ToString() : XX;
            string cformStr = word.CFormId.HasValue ? word.CFormId.Value.ToString() : XX;
            sb.Append(posStr); sb.Append(sep1); sb.Append(ctypeStr); sb.Append(sep2); sb.Append(cformStr);
        }

        /// <summary>E: 前アクセント句情報を出力する。形式: {e1}_{e2}!{e3}_{e4}-{e5}</summary>
        private static void AppendAccentPhraseE(ref ValueStringBuilder sb, JPAccentPhrase ap, bool hasPauseBefore)
        {
            if (ap == null)
            {
                sb.Append("xx_xx!xx_xx-xx");
                return;
            }

            int moraCount = ap.MoraCount;
            int accent = ap.AccentType; // E: そのまま出力（0=平板のまま）
            int isInterr = ap.IsInterrogative ? 1 : 0;

            sb.Append(ClampUnsigned(moraCount, 49));
            sb.Append('_');
            sb.Append(ClampAccent(accent, 49));
            sb.Append('!');
            sb.Append(isInterr);
            sb.Append('_');
            sb.Append(hasPauseBefore ? 1 : 0); // e4: 前APとの間にポーズがあるか
            sb.Append('-');
            sb.Append(XX); // e5: 未使用
        }

        /// <summary>G: 次アクセント句情報を出力する。形式: {g1}_{g2}%{g3}_{g4}_{g5}</summary>
        private static void AppendAccentPhraseG(ref ValueStringBuilder sb, JPAccentPhrase ap, bool hasPauseAfter)
        {
            if (ap == null)
            {
                sb.Append("xx_xx%xx_xx_xx");
                return;
            }

            int moraCount = ap.MoraCount;
            int accent = ap.AccentType; // G: そのまま出力（0=平板のまま）
            int isInterr = ap.IsInterrogative ? 1 : 0;

            sb.Append(ClampUnsigned(moraCount, 49));
            sb.Append('_');
            sb.Append(ClampAccent(accent, 49));
            sb.Append('%');
            sb.Append(isInterr);
            sb.Append('_');
            sb.Append(hasPauseAfter ? 1 : 0); // g4: 次APとの間にポーズがあるか
            sb.Append('_');
            sb.Append(XX); // g5: 未使用
        }

        /// <summary>F: 現在アクセント句の詳細情報を出力する。</summary>
        private static void AppendCurrentAccentPhraseF(ref ValueStringBuilder sb, JPAccentPhrase ap, JPUtterance utterance)
        {
            int moraCount = ap.MoraCount;
            int accent = ap.AccentType; // F: そのまま出力（0=平板のまま）
            int isInterr = ap.IsInterrogative ? 1 : 0;

            sb.Append(ClampUnsigned(moraCount, 49));
            sb.Append('_');
            sb.Append(ClampAccent(accent, 49));
            sb.Append('#');
            sb.Append(isInterr);
            sb.Append('_');
            sb.Append(XX); // f4: 未使用
            sb.Append('@');

            // f5: 呼気グループ内AP位置（前方、1始まり）
            var bg = ap.ParentBreathGroup;
            int apIdxInBG = ap.IndexInBreathGroup;
            int f5 = apIdxInBG + 1;
            int f6 = bg.AccentPhraseCount - apIdxInBG;
            sb.Append(ClampUnsigned(f5, 49));
            sb.Append('_');
            sb.Append(ClampUnsigned(f6, 49));
            sb.Append('|');

            // f7: 呼気グループ内モーラ位置（前方、1始まり）
            int morasBefore = 0;
            for (int i = 0; i < apIdxInBG; i++)
                morasBefore += bg.AccentPhrases[i].MoraCount;
            int f7 = morasBefore + 1;
            int f8 = bg.MoraCount - morasBefore;
            sb.Append(ClampUnsigned(f7, 49));
            sb.Append('_');
            sb.Append(ClampUnsigned(f8, 49));
        }

        /// <summary>H/J: 呼気グループ情報を出力する。</summary>
        private static void AppendBreathGroupHJ(ref ValueStringBuilder sb, JPBreathGroup bg)
        {
            if (bg == null)
            {
                sb.Append("xx_xx");
                return;
            }
            sb.Append(ClampUnsigned(bg.AccentPhraseCount, 49));
            sb.Append('_');
            sb.Append(ClampUnsigned(bg.MoraCount, 99));
        }

        /// <summary>I: 現在呼気グループの詳細情報を出力する。</summary>
        private static void AppendCurrentBreathGroupI(ref ValueStringBuilder sb, JPBreathGroup bg, JPUtterance utterance)
        {
            // i1: アクセント句数
            sb.Append(ClampUnsigned(bg.AccentPhraseCount, 49));
            sb.Append('-');
            // i2: モーラ数
            sb.Append(ClampUnsigned(bg.MoraCount, 99));
            sb.Append('@');

            int bgIdx = bg.IndexInUtterance;
            int bgCount = utterance.BreathGroupCount;

            // i3: 発話内BG位置（前方、1始まり）
            sb.Append(ClampUnsigned(bgIdx + 1, 19));
            sb.Append('+');
            // i4: 発話内BG位置（後方）
            sb.Append(ClampUnsigned(bgCount - bgIdx, 19));
            sb.Append('&');

            // i5: 発話内AP位置（前方、1始まり）
            int apsBefore = 0;
            for (int i = 0; i < bgIdx; i++)
                apsBefore += utterance.BreathGroups[i].AccentPhraseCount;
            sb.Append(ClampUnsigned(apsBefore + 1, 49));
            sb.Append('-');
            // i6: 発話内AP位置（後方）
            int apsAfter = 0;
            for (int i = bgIdx + 1; i < bgCount; i++)
                apsAfter += utterance.BreathGroups[i].AccentPhraseCount;
            sb.Append(ClampUnsigned(apsAfter + bg.AccentPhraseCount, 49));
            sb.Append('|');

            // i7: 発話内モーラ位置（前方）
            int morasBefore = 0;
            for (int i = 0; i < bgIdx; i++)
                morasBefore += utterance.BreathGroups[i].MoraCount;
            sb.Append(ClampUnsigned(morasBefore + 1, 199));
            sb.Append('+');
            // i8: 発話内モーラ位置（後方）
            int morasAfter = 0;
            for (int i = bgIdx + 1; i < bgCount; i++)
                morasAfter += utterance.BreathGroups[i].MoraCount;
            sb.Append(ClampUnsigned(morasAfter + bg.MoraCount, 199));
        }

        // ====== アクセント正規化 ======

        /// <summary>
        /// Aフィールド用: アクセント位置を正規化する。0（平板型）の場合はmora_countに変換する。
        /// jpreprocess準拠。Aフィールドのa1計算でのみ使用。
        /// </summary>
        private static int NormalizeAccentForA(int accent, int moraCount)
        {
            return accent == 0 ? moraCount : accent;
        }

        // ====== クランプ ======

        /// <summary>unsigned値をクランプする（1-max）。</summary>
        private static int ClampUnsigned(int value, int max)
        {
            if (value < 1) return 1;
            if (value > max) return max;
            return value;
        }

        /// <summary>アクセント値をクランプする（0-max）。0を有効値として許容する。</summary>
        private static int ClampAccent(int value, int max)
        {
            if (value < 0) return 0;
            if (value > max) return max;
            return value;
        }

        /// <summary>signed値をクランプする（-max..max）。</summary>
        private static int ClampSigned(int value, int max)
        {
            if (value < -max) return -max;
            if (value > max) return max;
            return value;
        }
    }
}
