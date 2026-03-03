using DotNetG2P.PhonemeConverter;

namespace DotNetG2P.Tests.PhonemeConverter
{
    /// <summary>
    /// MoraMappingの全165種カタカナ⇔音素マッピングの全数検証テスト。
    /// _mappingテーブルの全エントリに対してKatakanaToPhonemeString()の正しさを検証する。
    /// </summary>
    public class MoraMappingFullTests
    {
        // ===== ア行（母音） =====

        [Theory]
        [InlineData("ア", "a")]
        [InlineData("ァ", "a")]
        [InlineData("イ", "i")]
        [InlineData("ィ", "i")]
        [InlineData("ウ", "u")]
        [InlineData("ゥ", "u")]
        [InlineData("エ", "e")]
        [InlineData("ェ", "e")]
        [InlineData("オ", "o")]
        [InlineData("ォ", "o")]
        public void カタカナ変換_ア行_母音が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== カ行 =====

        [Theory]
        [InlineData("カ", "k a")]
        [InlineData("キ", "k i")]
        [InlineData("ク", "k u")]
        [InlineData("ケ", "k e")]
        [InlineData("コ", "k o")]
        public void カタカナ変換_カ行_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ガ行 =====

        [Theory]
        [InlineData("ガ", "g a")]
        [InlineData("ギ", "g i")]
        [InlineData("グ", "g u")]
        [InlineData("ゲ", "g e")]
        [InlineData("ゴ", "g o")]
        public void カタカナ変換_ガ行_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== サ行 =====

        [Theory]
        [InlineData("サ", "s a")]
        [InlineData("シ", "sh i")]
        [InlineData("ス", "s u")]
        [InlineData("セ", "s e")]
        [InlineData("ソ", "s o")]
        public void カタカナ変換_サ行_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ザ行 =====

        [Theory]
        [InlineData("ザ", "z a")]
        [InlineData("ジ", "j i")]
        [InlineData("ズ", "z u")]
        [InlineData("ゼ", "z e")]
        [InlineData("ゾ", "z o")]
        public void カタカナ変換_ザ行_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== タ行 =====

        [Theory]
        [InlineData("タ", "t a")]
        [InlineData("チ", "ch i")]
        [InlineData("ツ", "ts u")]
        [InlineData("テ", "t e")]
        [InlineData("ト", "t o")]
        public void カタカナ変換_タ行_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ダ行 =====

        [Theory]
        [InlineData("ダ", "d a")]
        [InlineData("ヂ", "j i")]
        [InlineData("ヅ", "z u")]
        [InlineData("デ", "d e")]
        [InlineData("ド", "d o")]
        public void カタカナ変換_ダ行_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ナ行 =====

        [Theory]
        [InlineData("ナ", "n a")]
        [InlineData("ニ", "n i")]
        [InlineData("ヌ", "n u")]
        [InlineData("ネ", "n e")]
        [InlineData("ノ", "n o")]
        public void カタカナ変換_ナ行_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ハ行 =====

        [Theory]
        [InlineData("ハ", "h a")]
        [InlineData("ヒ", "h i")]
        [InlineData("フ", "f u")]
        [InlineData("ヘ", "h e")]
        [InlineData("ホ", "h o")]
        public void カタカナ変換_ハ行_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== バ行 =====

        [Theory]
        [InlineData("バ", "b a")]
        [InlineData("ビ", "b i")]
        [InlineData("ブ", "b u")]
        [InlineData("ベ", "b e")]
        [InlineData("ボ", "b o")]
        public void カタカナ変換_バ行_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== パ行 =====

        [Theory]
        [InlineData("パ", "p a")]
        [InlineData("ピ", "p i")]
        [InlineData("プ", "p u")]
        [InlineData("ペ", "p e")]
        [InlineData("ポ", "p o")]
        public void カタカナ変換_パ行_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== マ行 =====

        [Theory]
        [InlineData("マ", "m a")]
        [InlineData("ミ", "m i")]
        [InlineData("ム", "m u")]
        [InlineData("メ", "m e")]
        [InlineData("モ", "m o")]
        public void カタカナ変換_マ行_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ヤ行 =====

        [Theory]
        [InlineData("ヤ", "y a")]
        [InlineData("ャ", "y a")]
        [InlineData("ユ", "y u")]
        [InlineData("ュ", "y u")]
        [InlineData("ヨ", "y o")]
        [InlineData("ョ", "y o")]
        public void カタカナ変換_ヤ行_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ラ行 =====

        [Theory]
        [InlineData("ラ", "r a")]
        [InlineData("リ", "r i")]
        [InlineData("ル", "r u")]
        [InlineData("レ", "r e")]
        [InlineData("ロ", "r o")]
        public void カタカナ変換_ラ行_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ワ行 =====

        [Theory]
        [InlineData("ワ", "w a")]
        [InlineData("ヮ", "w a")]
        [InlineData("ヰ", "i")]
        [InlineData("ヱ", "e")]
        [InlineData("ヲ", "o")]
        public void カタカナ変換_ワ行_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== カ行拗音 =====

        [Theory]
        [InlineData("キャ", "ky a")]
        [InlineData("キュ", "ky u")]
        [InlineData("キョ", "ky o")]
        [InlineData("キェ", "ky e")]
        [InlineData("クヮ", "kw a")]
        public void カタカナ変換_カ行拗音_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ガ行拗音 =====

        [Theory]
        [InlineData("ギャ", "gy a")]
        [InlineData("ギュ", "gy u")]
        [InlineData("ギョ", "gy o")]
        [InlineData("ギェ", "gy e")]
        [InlineData("グヮ", "gw a")]
        public void カタカナ変換_ガ行拗音_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== サ行拗音 =====

        [Theory]
        [InlineData("シャ", "sh a")]
        [InlineData("シュ", "sh u")]
        [InlineData("ショ", "sh o")]
        [InlineData("シェ", "sh e")]
        [InlineData("スィ", "s i")]
        public void カタカナ変換_サ行拗音_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ザ行拗音 =====

        [Theory]
        [InlineData("ジャ", "j a")]
        [InlineData("ジュ", "j u")]
        [InlineData("ジョ", "j o")]
        [InlineData("ジェ", "j e")]
        [InlineData("ズィ", "z i")]
        public void カタカナ変換_ザ行拗音_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== タ行拗音・外来音 =====

        [Theory]
        [InlineData("チャ", "ch a")]
        [InlineData("チュ", "ch u")]
        [InlineData("チョ", "ch o")]
        [InlineData("チェ", "ch e")]
        [InlineData("ツァ", "ts a")]
        [InlineData("ツィ", "ts i")]
        [InlineData("ツェ", "ts e")]
        [InlineData("ツォ", "ts o")]
        [InlineData("ティ", "t i")]
        [InlineData("テャ", "ty a")]
        [InlineData("テュ", "ty u")]
        [InlineData("テョ", "ty o")]
        [InlineData("トゥ", "t u")]
        public void カタカナ変換_タ行拗音外来音_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ダ行外来音 =====

        [Theory]
        [InlineData("ディ", "d i")]
        [InlineData("デャ", "dy a")]
        [InlineData("デュ", "dy u")]
        [InlineData("デョ", "dy o")]
        [InlineData("ドゥ", "d u")]
        public void カタカナ変換_ダ行外来音_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ナ行拗音 =====

        [Theory]
        [InlineData("ニャ", "ny a")]
        [InlineData("ニュ", "ny u")]
        [InlineData("ニョ", "ny o")]
        [InlineData("ニェ", "ny e")]
        public void カタカナ変換_ナ行拗音_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ハ行拗音・外来音 =====

        [Theory]
        [InlineData("ヒャ", "hy a")]
        [InlineData("ヒュ", "hy u")]
        [InlineData("ヒョ", "hy o")]
        [InlineData("ヒェ", "hy e")]
        [InlineData("ファ", "f a")]
        [InlineData("フィ", "f i")]
        [InlineData("フェ", "f e")]
        [InlineData("フォ", "f o")]
        public void カタカナ変換_ハ行拗音外来音_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== バ行拗音 =====

        [Theory]
        [InlineData("ビャ", "by a")]
        [InlineData("ビュ", "by u")]
        [InlineData("ビョ", "by o")]
        [InlineData("ビェ", "by e")]
        public void カタカナ変換_バ行拗音_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== パ行拗音 =====

        [Theory]
        [InlineData("ピャ", "py a")]
        [InlineData("ピュ", "py u")]
        [InlineData("ピョ", "py o")]
        [InlineData("ピェ", "py e")]
        public void カタカナ変換_パ行拗音_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== マ行拗音 =====

        [Theory]
        [InlineData("ミャ", "my a")]
        [InlineData("ミュ", "my u")]
        [InlineData("ミョ", "my o")]
        [InlineData("ミェ", "my e")]
        public void カタカナ変換_マ行拗音_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ラ行拗音 =====

        [Theory]
        [InlineData("リャ", "ry a")]
        [InlineData("リュ", "ry u")]
        [InlineData("リョ", "ry o")]
        [InlineData("リェ", "ry e")]
        public void カタカナ変換_ラ行拗音_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ワ行外来音 =====

        [Theory]
        [InlineData("ウィ", "w i")]
        [InlineData("ウェ", "w e")]
        [InlineData("ウォ", "w o")]
        public void カタカナ変換_ワ行外来音_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ヴ行外来音 =====

        [Theory]
        [InlineData("ヴァ", "v a")]
        [InlineData("ヴィ", "v i")]
        [InlineData("ヴェ", "v e")]
        [InlineData("ヴォ", "v o")]
        [InlineData("ヴ", "v u")]
        public void カタカナ変換_ヴ行_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ヴ行拗音（by子音にマッピング） =====

        [Theory]
        [InlineData("ヴャ", "by a")]
        [InlineData("ヴュ", "by u")]
        [InlineData("ヴョ", "by o")]
        public void カタカナ変換_ヴ行拗音_by子音にマッピングされる(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== イェ（ヤ行外来音） =====

        [Fact]
        public void カタカナ変換_イェ_y_eを返す()
        {
            Assert.Equal("y e", MoraMapping.KatakanaToPhonemeString("イェ"));
        }

        // ===== 特殊モーラ =====

        [Theory]
        [InlineData("ン", "N")]
        [InlineData("ッ", "cl")]
        [InlineData("ー", "-")]  // 単独の長音は直前の母音がないため "-" のまま
        public void カタカナ変換_特殊モーラ_音素が正しい(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== ヶ（小書きケ） =====

        [Fact]
        public void カタカナ変換_ヶ_k_eを返す()
        {
            Assert.Equal("k e", MoraMapping.KatakanaToPhonemeString("ヶ"));
        }

        // ===== 句読点・疑問符（空文字列を返す） =====

        [Theory]
        [InlineData("、", "")]
        [InlineData("？", "")]
        public void カタカナ変換_句読点疑問符_空文字列を返す(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== 全エントリ数の検証 =====

        /// <summary>
        /// 上記のTheoryテストで全165種のマッピングエントリをカバーしていることを
        /// 確認するための補助テスト。全カタカナをリストアップし、すべて変換できることを検証する。
        /// </summary>
        [Fact]
        public void カタカナ変換_全165種_すべて変換に成功する()
        {
            // MoraMapping._mappingテーブルに含まれる全カタカナ（165種）
            var allKatakana = new[]
            {
                // カ行拗音 (5)
                "キャ", "キュ", "キョ", "キェ", "クヮ",
                // ガ行拗音 (5)
                "ギャ", "ギュ", "ギョ", "ギェ", "グヮ",
                // サ行拗音 (5)
                "シャ", "シュ", "ショ", "シェ", "スィ",
                // ザ行拗音 (5)
                "ジャ", "ジュ", "ジョ", "ジェ", "ズィ",
                // タ行拗音・外来音 (13)
                "チャ", "チュ", "チョ", "チェ",
                "ツァ", "ツィ", "ツェ", "ツォ",
                "ティ", "テャ", "テュ", "テョ", "トゥ",
                // ダ行外来音 (5)
                "ディ", "デャ", "デュ", "デョ", "ドゥ",
                // ナ行拗音 (4)
                "ニャ", "ニュ", "ニョ", "ニェ",
                // ハ行拗音・外来音 (8)
                "ヒャ", "ヒュ", "ヒョ", "ヒェ",
                "ファ", "フィ", "フェ", "フォ",
                // バ行拗音 (4)
                "ビャ", "ビュ", "ビョ", "ビェ",
                // パ行拗音 (4)
                "ピャ", "ピュ", "ピョ", "ピェ",
                // マ行拗音 (4)
                "ミャ", "ミュ", "ミョ", "ミェ",
                // ラ行拗音 (4)
                "リャ", "リュ", "リョ", "リェ",
                // ワ行外来音 (3)
                "ウィ", "ウェ", "ウォ",
                // ヴ行外来音（拗音含む） (7)
                "ヴァ", "ヴィ", "ヴェ", "ヴォ",
                "ヴャ", "ヴュ", "ヴョ",
                // イェ (1)
                "イェ",
                // ア行 (10)
                "ア", "ァ", "イ", "ィ", "ウ", "ゥ", "エ", "ェ", "オ", "ォ",
                // カ行 (5)
                "カ", "キ", "ク", "ケ", "コ",
                // ガ行 (5)
                "ガ", "ギ", "グ", "ゲ", "ゴ",
                // サ行 (5)
                "サ", "シ", "ス", "セ", "ソ",
                // ザ行 (5)
                "ザ", "ジ", "ズ", "ゼ", "ゾ",
                // タ行 (5)
                "タ", "チ", "ツ", "テ", "ト",
                // ダ行 (5)
                "ダ", "ヂ", "ヅ", "デ", "ド",
                // ナ行 (5)
                "ナ", "ニ", "ヌ", "ネ", "ノ",
                // ハ行 (5)
                "ハ", "ヒ", "フ", "ヘ", "ホ",
                // バ行 (5)
                "バ", "ビ", "ブ", "ベ", "ボ",
                // パ行 (5)
                "パ", "ピ", "プ", "ペ", "ポ",
                // マ行 (5)
                "マ", "ミ", "ム", "メ", "モ",
                // ヤ行 (6)
                "ヤ", "ャ", "ユ", "ュ", "ヨ", "ョ",
                // ラ行 (5)
                "ラ", "リ", "ル", "レ", "ロ",
                // ワ行 (5)
                "ワ", "ヮ", "ヰ", "ヱ", "ヲ",
                // ヴ単独 (1)
                "ヴ",
                // 特殊モーラ (3)
                "ン", "ッ", "ー",
                // ヶ (1)
                "ヶ",
                // 句読点・疑問符 (2)
                "、", "？",
            };

            Assert.Equal(165, allKatakana.Length);

            foreach (var kana in allKatakana)
            {
                // 例外を投げずに変換できることを検証
                var result = MoraMapping.KatakanaToPhonemeString(kana);
                Assert.NotNull(result);
            }
        }
    }
}
