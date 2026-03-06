using System.Collections.Generic;
using DotNetG2P.Models;

namespace DotNetG2P.Tests.TestHelpers
{
    /// <summary>
    /// テスト用NjdNode生成ファクトリ。
    /// 各テストクラスに散在するヘルパーメソッドの共通化版。
    /// </summary>
    public static class NjdNodeFactory
    {
        /// <summary>
        /// 発音付きのNjdNodeを生成する。
        /// カタカナ文字列からPronunciationを生成し、ノードに設定する。
        /// </summary>
        public static NjdNode CreateWithPronunciation(
            string surface,
            string katakana,
            POSType posType = POSType.Meishi,
            string sub1 = "*",
            string sub2 = "*",
            string sub3 = "*",
            int accentType = 0,
            bool? chainFlag = null,
            string conjugationType = "*",
            string conjugationForm = "*",
            string chainRule = "*")
        {
            var pos = new POS(posType, sub1, sub2, sub3);
            var pron = Pronunciation.FromKatakana(katakana, accentType);
            var details = new WordDetails(pos, conjugationType, conjugationForm, surface, katakana, pron);
            return new NjdNode(surface, details)
            {
                AccentType = accentType,
                ChainFlag = chainFlag,
                Pronunciation = pron,
                ChainRule = chainRule,
            };
        }

        /// <summary>
        /// 発音なしのNjdNodeを生成する。
        /// WordDetailsにPronunciationを設定せず、ノードのPronunciationも空のまま。
        /// </summary>
        public static NjdNode CreateWithoutPronunciation(
            string surface,
            POSType posType = POSType.Meishi,
            string sub1 = "*",
            string reading = "*")
        {
            var pos = new POS(posType, sub1);
            var details = new WordDetails(pos, "*", "*", surface, reading, null);
            return new NjdNode(surface, details);
        }

        /// <summary>
        /// カタカナとアクセント型から簡易NjdNodeを生成する。
        /// ProsodyExtractor等のテストで使用。
        /// </summary>
        public static NjdNode CreateSimple(string katakana, int accentType)
        {
            var pron = Pronunciation.FromKatakana(katakana, accentType);
            var details = new WordDetails(new POS(POSType.Meishi), "*", "*", "*", "*", null);
            return new NjdNode(katakana, details)
            {
                AccentType = accentType,
                Pronunciation = pron,
            };
        }

        /// <summary>
        /// Touten（読点）ノードを生成する。
        /// </summary>
        public static NjdNode CreateTouten()
        {
            var details = new WordDetails(new POS(POSType.Kigou), "*", "*", "*", "*", null);
            return new NjdNode("、", details)
            {
                Pronunciation = new Pronunciation(
                    new List<Mora> { new Mora(null, null, MoraKind.Touten) }, 0)
            };
        }

        /// <summary>
        /// Question（疑問符）ノードを生成する。
        /// </summary>
        public static NjdNode CreateQuestion()
        {
            var details = new WordDetails(new POS(POSType.Kigou), "*", "*", "*", "*", null);
            return new NjdNode("？", details)
            {
                Pronunciation = new Pronunciation(
                    new List<Mora> { new Mora(null, null, MoraKind.Question) }, 0)
            };
        }

        /// <summary>
        /// 数詞ノードを生成する。
        /// </summary>
        public static NjdNode CreateKazu(
            string surface,
            string katakana,
            int accentType = 0,
            bool? chainFlag = null,
            string chainRule = "*")
        {
            return CreateWithPronunciation(
                surface, katakana,
                posType: POSType.Meishi, sub1: "数",
                accentType: accentType,
                chainFlag: chainFlag,
                chainRule: chainRule);
        }
    }
}
