using DotNetG2P.Models;
using DotNetG2P.NJD;

namespace DotNetG2P.Tests.NJD
{
    public class SetUnvoicedVowelTests
    {
        /// <summary>
        /// NjdNodeを手動構築するヘルパー。
        /// カタカナ文字列とアクセント型からノードを生成する。
        /// </summary>
        private static NjdNode CreateNode(
            string surface,
            string katakana,
            POSType posType = POSType.Meishi,
            string sub1 = "*",
            int accentType = 0,
            bool? chainFlag = null,
            string conjugationForm = "*")
        {
            var pos = new POS(posType, sub1);
            var pron = Pronunciation.FromKatakana(katakana, 0);
            var details = new WordDetails(pos, "*", conjugationForm, surface, katakana, pron);
            var node = new NjdNode(surface, details)
            {
                AccentType = accentType,
                ChainFlag = chainFlag,
                Pronunciation = pron,
            };
            return node;
        }

        // ===== 基本ケース: s+u+k+i → s+U+k+i =====

        [Fact]
        public void Process_Suki_UnvoicesSu()
        {
            // "スキ" → s U k I (無声子音に囲まれたu/iが無声化)
            // ただし連続回避で2つ同時には無声化しない
            // "スキ"のみの場合: 2モーラ、語末は無声化しない
            // → スの後のキが語末なのでキは無声化しない
            // → スは次のキの子音kが無声子音なので s+u → s+U
            var node1 = CreateNode("好き", "スキ", POSType.Keiyoushi, accentType: 2);
            // 後続にもう1ノード必要（語末判定のため）
            var node2 = CreateNode("だ", "ダ", POSType.Jodoushi, accentType: 0, chainFlag: true);

            var nodes = new List<NjdNode> { node1, node2 };
            SetUnvoicedVowel.Process(nodes);

            // スの母音: uが無声化してU
            var moras = node1.Pronunciation.Moras;
            Assert.Equal(Vowel.U_Unvoiced, moras[0].Vowel); // ス → s U
            Assert.Equal(Vowel.I, moras[1].Vowel);           // キ → k i (連続回避で有声のまま)
        }

        [Fact]
        public void Process_Kusuri_UnvoicesCorrectMora()
        {
            // "クスリ" (k u, s u, r i) → 次の子音がsで無声なのでク(k u)が無声化
            // スも次がrで有声子音なので無声化しない
            var node1 = CreateNode("薬", "クスリ", accentType: 0);
            // 後続ノードなし（語末）

            var nodes = new List<NjdNode> { node1 };
            SetUnvoicedVowel.Process(nodes);

            var moras = node1.Pronunciation.Moras;
            Assert.Equal(Vowel.U_Unvoiced, moras[0].Vowel); // ク → k U (次がs=無声子音)
            Assert.Equal(Vowel.U, moras[1].Vowel);           // ス → s u (次がr=有声子音)
            Assert.Equal(Vowel.I, moras[2].Vowel);            // リ → r i (語末)
        }

        // ===== 連続回避テスト =====

        [Fact]
        public void Process_Consecutive_AvoidsDoubleUnvoicing()
        {
            // "チクショー" (ch i, k u, sh o, -)
            // chは無声子音、kは無声子音、shは無声子音
            // チ(ch+i): 次のク(k)が無声子音 → 無声化候補
            // ク(k+u): 次のショ(sh)が無声子音 → 無声化候補
            // でも連続回避: 前のモーラが無声化したら次は有声のまま
            var node = CreateNode("畜生", "チクショー", accentType: 0);
            var nodes = new List<NjdNode> { node };
            SetUnvoicedVowel.Process(nodes);

            var moras = node.Pronunciation.Moras;
            // 前方走査: i=0(チ)を先に処理
            // チ(ch+i): next=ク(k=無声子音) → ルール5で無声化(false)
            // チが無声化確定 → next(ク)のIsVoicedFlagをtrue（連続回避）
            // ク(k+u): IsVoicedFlag==true → スキップ（有声のまま）
            Assert.Equal(Vowel.I_Unvoiced, moras[0].Vowel);  // チ → 無声化
            Assert.Equal(Vowel.U, moras[1].Vowel);            // ク → 有声のまま（連続回避）
            Assert.Equal(Vowel.O, moras[2].Vowel);            // ショ → 有声のまま
        }

        // ===== ルール0: フィラーは無声化しない =====

        [Fact]
        public void Process_Filler_DoesNotUnvoice()
        {
            var node = CreateNode("えーっと", "スキ", POSType.Filler, accentType: 0);
            var node2 = CreateNode("です", "デス", POSType.Jodoushi, accentType: 0, chainFlag: false);
            var nodes = new List<NjdNode> { node, node2 };
            SetUnvoicedVowel.Process(nodes);

            // フィラーのモーラは無声化しない
            var moras = node.Pronunciation.Moras;
            Assert.Equal(Vowel.U, moras[0].Vowel); // ス有声のまま
            Assert.Equal(Vowel.I, moras[1].Vowel); // キ有声のまま
        }

        // ===== ルール4: アクセント核位置のモーラは無声化しない =====

        [Fact]
        public void Process_AccentNucleus_DoesNotUnvoice()
        {
            // アクセント型1 → 1番目のモーラ(0-indexed: 0)がアクセント核
            // ス(s+u)がアクセント核で、次がk(無声子音)でも無声化しない
            var node = CreateNode("好き", "スキ", accentType: 1);
            var node2 = CreateNode("だ", "ダ", POSType.Jodoushi, accentType: 0, chainFlag: true);
            var nodes = new List<NjdNode> { node, node2 };
            SetUnvoicedVowel.Process(nodes);

            // ス: アクセント核位置(accentType=1, moraIndex=0, 1==0+1) → 有声のまま
            var moras = node.Pronunciation.Moras;
            Assert.Equal(Vowel.U, moras[0].Vowel); // アクセント核なので有声のまま
        }

        // ===== 例外ペアテスト: s→s では無声化しない =====

        [Fact]
        public void Process_ExceptionPair_SToS_DoesNotUnvoice()
        {
            // "ススム" (s+u, s+u, m+u)
            // ス(s+u): 次がス(s) → s→s は例外ペアなので有声のまま
            var node = CreateNode("進む", "ススム", accentType: 0);
            var nodes = new List<NjdNode> { node };
            SetUnvoicedVowel.Process(nodes);

            var moras = node.Pronunciation.Moras;
            Assert.Equal(Vowel.U, moras[0].Vowel); // s→s 例外ペアなので有声のまま
        }

        // ===== 空リスト・nullテスト =====

        [Fact]
        public void Process_Null_DoesNotThrow()
        {
            SetUnvoicedVowel.Process(null!);
        }

        [Fact]
        public void Process_EmptyList_DoesNotThrow()
        {
            SetUnvoicedVowel.Process(new List<NjdNode>());
        }

        // ===== 母音のみモーラは対象外 =====

        [Fact]
        public void Process_VowelOnlyMora_DoesNotUnvoice()
        {
            // "アイ" → 子音なしなので無声化の対象外
            var node = CreateNode("愛", "アイ", accentType: 0);
            var nodes = new List<NjdNode> { node };
            SetUnvoicedVowel.Process(nodes);

            var moras = node.Pronunciation.Moras;
            Assert.Equal(Vowel.A, moras[0].Vowel);
            Assert.Equal(Vowel.I, moras[1].Vowel);
        }
    }
}
