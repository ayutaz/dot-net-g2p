using System;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// piper-plus 互換 IPA 変換の正確性を検証するテスト。
    /// ChineseG2PEngine の ToPiperIPA() メソッド経由で、
    /// piper-plus 方式の声母・韻母IPAマッピング、特殊母音、声調マーカー非出力を検証する。
    /// </summary>
    public class ChinesePiperIpaTests : IDisposable
    {
        private readonly ChineseG2PEngine _engine;

        public ChinesePiperIpaTests()
        {
            _engine = new ChineseG2PEngine();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =====================================================================
        // 1. 声母IPAマッピング（piper-plus と既存IPAの差異を重点的に検証）
        // =====================================================================

        [Fact]
        public void ToPiperIPA_zh声母_tʂを返す()
        {
            // piper-plus: zh → tʂ (NOT ʈʂ)
            // "知" (zhī)
            var result = _engine.ToPiperIPA("\u77E5");
            Assert.Contains("t\u0282", result); // tʂ
            // 既存IPA の ʈʂ (U+0288 U+0282) ではないことを確認
            Assert.DoesNotContain("\u0288\u0282", result);
        }

        [Fact]
        public void ToPiperIPA_ch声母_tʂʰを返す()
        {
            // piper-plus: ch → tʂʰ (NOT ʈʂʰ)
            // "吃" (chī)
            var result = _engine.ToPiperIPA("\u5403");
            Assert.Contains("t\u0282\u02B0", result); // tʂʰ
            // 既存IPA の ʈʂʰ (U+0288 U+0282 U+02B0) ではないことを確認
            Assert.DoesNotContain("\u0288\u0282\u02B0", result);
        }

        [Theory]
        [InlineData("\u5988", "m")]     // 妈 (mā) → m は共通
        [InlineData("\u7238", "p")]     // 爸 (bà) → p (b→p) は共通
        [InlineData("\u5927", "t")]     // 大 (dà) → t (d→t) は共通
        [InlineData("\u6765", "l")]     // 来 (lái) → l は共通
        [InlineData("\u5403", "t\u0282\u02B0")] // 吃 (chī) → tʂʰ (piper-plus固有)
        public void ToPiperIPA_各声母_既存IPAと共通の声母は一致(string hanzi, string expectedInitial)
        {
            // piper-plus でも変わらない声母は既存IPAと同じマッピングであることを確認
            var result = _engine.ToPiperIPA(hanzi);
            Assert.Contains(expectedInitial, result);
        }

        // =====================================================================
        // 2. 韻母IPAマッピング（piper-plus 差異箇所）
        // =====================================================================

        [Fact]
        public void ToPiperIPA_ong韻母_uŋを返す()
        {
            // piper-plus: ong → uŋ (NOT ʊŋ)
            // "东" (dōng) → tuŋ 相当
            var result = _engine.ToPiperIPA("\u4E1C");
            Assert.Contains("u\u014B", result); // uŋ
            // 既存IPA の ʊŋ (U+028A U+014B) ではないことを確認
            Assert.DoesNotContain("\u028A\u014B", result);
        }

        [Fact]
        public void ToPiperIPA_iu韻母_iouを返す()
        {
            // piper-plus: iu → iou (NOT ioʊ)
            // "六" (liù) → liou 相当
            var result = _engine.ToPiperIPA("\u516D");
            Assert.Contains("iou", result);
            // 既存IPA の ioʊ (io U+028A) ではないことを確認
            Assert.DoesNotContain("io\u028A", result);
        }

        [Fact]
        public void ToPiperIPA_er韻母_ɚを返す()
        {
            // piper-plus: er → ɚ (U+025A) (NOT əɻ)
            // "二" (èr) → ɚ 相当
            var result = _engine.ToPiperIPA("\u4E8C");
            Assert.Contains("\u025A", result); // ɚ
            // 既存IPA の əɻ (U+0259 U+027B) ではないことを確認
            Assert.DoesNotContain("\u0259\u027B", result);
        }

        [Fact]
        public void ToPiperIPA_iong韻母_iuŋを返す()
        {
            // piper-plus: iong → iuŋ (NOT iʊŋ)
            // "穷" (qióng) → tɕʰiuŋ 相当（Q+Iong で Iong 韻母を使用）
            var result = _engine.ToPiperIPA("\u7A77");
            Assert.Contains("iu\u014B", result); // iuŋ
            // 既存IPA の iʊŋ (i U+028A U+014B) ではないことを確認
            Assert.DoesNotContain("i\u028A\u014B", result);
        }

        [Fact]
        public void ToPiperIPA_van韻母_yɛnを返す()
        {
            // piper-plus: üan (van) → yɛn (NOT yan)
            // "元" (yuán) → yɛn 相当
            var result = _engine.ToPiperIPA("\u5143");
            Assert.Contains("y\u025Bn", result); // yɛn
            // 既存IPA の yan ではないことを確認
            Assert.DoesNotContain("yan", result);
        }

        // =====================================================================
        // 3. 特殊母音（そり舌・歯茎の空韻）
        // =====================================================================

        [Fact]
        public void ToPiperIPA_z_c_s加i_ɨを返す()
        {
            // piper-plus: z/c/s + i → ɨ (U+0268)
            // "四" (sì) → sɨ 相当
            var result = _engine.ToPiperIPA("\u56DB");
            Assert.Contains("\u0268", result); // ɨ
            // 既存IPA の ɹ̩ (U+0279 U+0329) ではないことを確認
            Assert.DoesNotContain("\u0279\u0329", result);
        }

        [Fact]
        public void ToPiperIPA_zi_sɨ相当()
        {
            // "子" (zǐ) → tsɨ 相当
            var result = _engine.ToPiperIPA("\u5B50");
            Assert.Contains("ts", result);
            Assert.Contains("\u0268", result); // ɨ
        }

        [Fact]
        public void ToPiperIPA_ci_tsʰɨ相当()
        {
            // "次" (cì) → tsʰɨ 相当
            var result = _engine.ToPiperIPA("\u6B21");
            Assert.Contains("ts\u02B0", result); // tsʰ
            Assert.Contains("\u0268", result); // ɨ
        }

        [Fact]
        public void ToPiperIPA_zh_ch_sh_r加i_ɻ̩を返す()
        {
            // piper-plus: zh/ch/sh/r + i → ɻ̩ (U+027B U+0329)
            // "十" (shí) → ʂɻ̩ 相当
            var result = _engine.ToPiperIPA("\u5341");
            Assert.Contains("\u027B\u0329", result); // ɻ̩
        }

        [Fact]
        public void ToPiperIPA_zhi_tʂɻ̩相当()
        {
            // "知" (zhī) → tʂɻ̩ 相当
            var result = _engine.ToPiperIPA("\u77E5");
            Assert.Contains("t\u0282", result); // tʂ
            Assert.Contains("\u027B\u0329", result); // ɻ̩
        }

        [Fact]
        public void ToPiperIPA_chi_tʂʰɻ̩相当()
        {
            // "吃" (chī) → tʂʰɻ̩ 相当
            var result = _engine.ToPiperIPA("\u5403");
            Assert.Contains("t\u0282\u02B0", result); // tʂʰ
            Assert.Contains("\u027B\u0329", result); // ɻ̩
        }

        [Fact]
        public void ToPiperIPA_ri_ɻɻ̩相当()
        {
            // "日" (rì) → ɻɻ̩ 相当
            var result = _engine.ToPiperIPA("\u65E5");
            Assert.Contains("\u027B\u027B\u0329", result); // ɻɻ̩
        }

        // =====================================================================
        // 4. 声調マーカーなし確認
        // =====================================================================

        [Theory]
        [InlineData("\u5988")]  // 妈 (mā) 第1声
        [InlineData("\u9EBB")]  // 麻 (má) 第2声
        [InlineData("\u9A6C")]  // 马 (mǎ) 第3声
        [InlineData("\u9A82")]  // 骂 (mà) 第4声
        public void ToPiperIPA_声調letterを含まない(string hanzi)
        {
            var result = _engine.ToPiperIPA(hanzi);
            // IPA tone letters が含まれないことを確認
            Assert.DoesNotContain("\u02E5", result); // ˥
            Assert.DoesNotContain("\u02E6", result); // ˦
            Assert.DoesNotContain("\u02E7", result); // ˧
            Assert.DoesNotContain("\u02E8", result); // ˨
            Assert.DoesNotContain("\u02E9", result); // ˩
        }

        [Fact]
        public void ToPiperIPA_文テキスト_声調letterなし()
        {
            // 複数文字のテキストでも声調 letter が含まれないこと
            var result = _engine.ToPiperIPA("\u4F60\u597D\u4E16\u754C"); // 你好世界
            Assert.DoesNotContain("\u02E5", result);
            Assert.DoesNotContain("\u02E6", result);
            Assert.DoesNotContain("\u02E7", result);
            Assert.DoesNotContain("\u02E8", result);
            Assert.DoesNotContain("\u02E9", result);
        }

        [Fact]
        public void ToPiperIPA_軽声_声調letterなし()
        {
            // 軽声でも声調 letter が含まれないこと
            // "吗" (ma, 軽声)
            var result = _engine.ToPiperIPA("\u5417");
            Assert.DoesNotContain("\u02E5", result);
            Assert.DoesNotContain("\u02E9", result);
        }

        // =====================================================================
        // 5. 空/null テスト
        // =====================================================================

        [Fact]
        public void ToPiperIPA_null入力_空文字列を返す()
        {
            var result = _engine.ToPiperIPA(null!);
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPiperIPA_空文字列_空文字列を返す()
        {
            var result = _engine.ToPiperIPA("");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPiperIPA_空白のみ_空文字列を返す()
        {
            var result = _engine.ToPiperIPA("   ");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPiperIpaPhonemes_null入力_空配列を返す()
        {
            var result = _engine.ToPiperIpaPhonemes(null!);
            Assert.Empty(result);
        }

        [Fact]
        public void ToPiperIpaPhonemes_空文字列_空配列を返す()
        {
            var result = _engine.ToPiperIpaPhonemes("");
            Assert.Empty(result);
        }

        // =====================================================================
        // 6. 音素配列テスト (ToPiperIpaPhonemes)
        // =====================================================================

        [Fact]
        public void ToPiperIpaPhonemes_単字_正しい音素配列を返す()
        {
            // "妈" (mā) → ["m", "a"] (声母 + 韻母で2要素)
            var result = _engine.ToPiperIpaPhonemes("\u5988");
            Assert.Equal(2, result.Length);
            Assert.Equal("m", result[0]);
            Assert.Equal("a", result[1]);
        }

        [Fact]
        public void ToPiperIpaPhonemes_2文字_4要素の配列を返す()
        {
            // "你好" → 4要素（各文字の声母+韻母 = 2要素×2文字）
            var result = _engine.ToPiperIpaPhonemes("\u4F60\u597D");
            Assert.Equal(4, result.Length);
        }

        [Fact]
        public void ToPiperIpaPhonemes_zh声母_tʂを含む()
        {
            // "知" (zhī) → ["tʂ", "ɻ̩"] (声母 + 韻母で2要素)
            var result = _engine.ToPiperIpaPhonemes("\u77E5");
            Assert.Equal(2, result.Length);
            Assert.Equal("t\u0282", result[0]); // tʂ
            Assert.DoesNotContain("\u0288", result[0]); // ʈ が含まれない
        }

        [Fact]
        public void ToPiperIpaPhonemes_ong韻母_uŋを含む()
        {
            // "东" (dōng) → ["t", "uŋ"] (声母 + 韻母で2要素)
            var result = _engine.ToPiperIpaPhonemes("\u4E1C");
            Assert.Equal(2, result.Length);
            Assert.Contains("u\u014B", result); // uŋ が配列要素として含まれる
        }

        [Fact]
        public void ToPiperIpaPhonemes_er韻母_ɚを含む()
        {
            // "二" (èr) → ɚ (声調なし)
            var result = _engine.ToPiperIpaPhonemes("\u4E8C");
            Assert.Single(result);
            Assert.Contains("\u025A", result[0]); // ɚ
        }

        [Fact]
        public void ToPiperIpaPhonemes_声調letterを含まない()
        {
            // 音素配列の各要素に声調 letter が含まれないこと
            var result = _engine.ToPiperIpaPhonemes("\u5988\u9EBB\u9A6C\u9A82"); // 妈麻马骂
            foreach (var phoneme in result)
            {
                Assert.DoesNotContain("\u02E5", phoneme);
                Assert.DoesNotContain("\u02E6", phoneme);
                Assert.DoesNotContain("\u02E7", phoneme);
                Assert.DoesNotContain("\u02E8", phoneme);
                Assert.DoesNotContain("\u02E9", phoneme);
            }
        }

        // =====================================================================
        // 7. バッチAPI テスト (ToPiperIPABatch)
        // =====================================================================

        [Fact]
        public void ToPiperIPABatch_複数テキスト_正しい件数を返す()
        {
            var texts = new[] { "\u4F60\u597D", "\u4E16\u754C", "\u4E2D\u56FD" }; // 你好, 世界, 中国
            var results = _engine.ToPiperIPABatch(texts);
            Assert.Equal(3, results.Count);
        }

        [Fact]
        public void ToPiperIPABatch_各結果が非空()
        {
            var texts = new[] { "\u5988", "\u7238" }; // 妈, 爸
            var results = _engine.ToPiperIPABatch(texts);
            foreach (var result in results)
            {
                Assert.NotEmpty(result);
            }
        }

        [Fact]
        public void ToPiperIPABatch_個別呼び出しと同一結果()
        {
            var texts = new[] { "\u4E1C", "\u5143", "\u516D" }; // 东, 元, 六
            var batchResults = _engine.ToPiperIPABatch(texts);
            for (int i = 0; i < texts.Length; i++)
            {
                var individual = _engine.ToPiperIPA(texts[i]);
                Assert.Equal(individual, batchResults[i]);
            }
        }

        [Fact]
        public void ToPiperIPABatch_声調letterなし()
        {
            var texts = new[] { "\u5988", "\u9EBB", "\u9A6C", "\u9A82" }; // 妈麻马骂
            var results = _engine.ToPiperIPABatch(texts);
            foreach (var result in results)
            {
                Assert.DoesNotContain("\u02E5", result);
                Assert.DoesNotContain("\u02E6", result);
                Assert.DoesNotContain("\u02E7", result);
                Assert.DoesNotContain("\u02E8", result);
                Assert.DoesNotContain("\u02E9", result);
            }
        }

        [Fact]
        public void ToPiperIPABatch_空配列_空リストを返す()
        {
            var results = _engine.ToPiperIPABatch(Array.Empty<string>());
            Assert.Empty(results);
        }

        // =====================================================================
        // 8. Dispose後テスト
        // =====================================================================

        [Fact]
        public void Dispose後_ToPiperIPA_ObjectDisposedExceptionを投げる()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPiperIPA("\u4F60\u597D")); // 你好
        }

        [Fact]
        public void Dispose後_ToPiperIpaPhonemes_ObjectDisposedExceptionを投げる()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPiperIpaPhonemes("\u4F60\u597D")); // 你好
        }

        [Fact]
        public void Dispose後_ToPiperIPABatch_ObjectDisposedExceptionを投げる()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPiperIPABatch(new[] { "\u4F60\u597D" })); // 你好
        }

        // =====================================================================
        // 9. 声母マッピング網羅テスト（piper-plus固有差異 + 共通声母）
        // =====================================================================

        [Theory]
        [InlineData("\u77E5", "t\u0282")]       // 知 (zhī): zh → tʂ (piper-plus固有: NOT ʈʂ)
        [InlineData("\u5403", "t\u0282\u02B0")]  // 吃 (chī): ch → tʂʰ (piper-plus固有: NOT ʈʂʰ)
        [InlineData("\u5C71", "\u0282")]         // 山 (shān): sh → ʂ (共通)
        [InlineData("\u4EBA", "\u027B")]         // 人 (rén): r → ɻ (共通)
        [InlineData("\u5988", "m")]              // 妈 (mā): m → m (共通)
        [InlineData("\u5427", "p")]              // 吧 (ba): b → p (共通)
        [InlineData("\u6015", "p\u02B0")]        // 怕 (pà): p → pʰ (共通)
        [InlineData("\u98DE", "f")]              // 飞 (fēi): f → f (共通)
        [InlineData("\u5927", "t")]              // 大 (dà): d → t (共通)
        [InlineData("\u5929", "t\u02B0")]        // 天 (tiān): t → tʰ (共通)
        [InlineData("\u5973", "n")]              // 女 (nǚ): n → n (共通)
        [InlineData("\u6765", "l")]              // 来 (lái): l → l (共通)
        [InlineData("\u5E72", "k")]              // 干 (gān): g → k (共通)
        [InlineData("\u770B", "k\u02B0")]        // 看 (kàn): k → kʰ (共通)
        [InlineData("\u597D", "x")]              // 好 (hǎo): h → x (共通)
        public void ToPiperIPA_声母マッピング確認(string hanzi, string expectedInitialIpa)
        {
            var result = _engine.ToPiperIPA(hanzi);
            Assert.Contains(expectedInitialIpa, result);
        }

        // =====================================================================
        // 10. 韻母マッピング網羅テスト（piper-plus差異箇所の統合確認）
        // =====================================================================

        [Theory]
        [InlineData("\u4E1C", "u\u014B")]        // 东 (dōng): ong → uŋ (NOT ʊŋ)
        [InlineData("\u7EA2", "u\u014B")]        // 红 (hóng): ong → uŋ (NOT ʊŋ)
        [InlineData("\u516D", "iou")]            // 六 (liù): iu → iou (NOT ioʊ)
        [InlineData("\u4E8C", "\u025A")]         // 二 (èr): er → ɚ (NOT əɻ)
        [InlineData("\u7A77", "iu\u014B")]       // 穷 (qióng): iong → iuŋ (NOT iʊŋ)
        [InlineData("\u5143", "y\u025Bn")]       // 元 (yuán): üan(van) → yɛn (NOT yan)
        public void ToPiperIPA_韻母差異マッピング確認(string hanzi, string expectedFinalIpa)
        {
            var result = _engine.ToPiperIPA(hanzi);
            Assert.Contains(expectedFinalIpa, result);
        }

        // =====================================================================
        // 11. 特殊母音の追加統合テスト
        // =====================================================================

        [Theory]
        [InlineData("\u56DB", "\u0268")]  // 四 (sì): s+i → sɨ
        [InlineData("\u5B50", "\u0268")]  // 子 (zǐ): z+i → tsɨ
        [InlineData("\u6B21", "\u0268")]  // 次 (cì): c+i → tsʰɨ
        public void ToPiperIPA_歯茎声母加i_ɨを含む(string hanzi, string expectedVowel)
        {
            var result = _engine.ToPiperIPA(hanzi);
            Assert.Contains(expectedVowel, result);
        }

        [Theory]
        [InlineData("\u5341", "\u027B\u0329")]  // 十 (shí): sh+i → ʂɻ̩
        [InlineData("\u77E5", "\u027B\u0329")]  // 知 (zhī): zh+i → tʂɻ̩
        [InlineData("\u65E5", "\u027B\u0329")]  // 日 (rì): r+i → ɻɻ̩
        public void ToPiperIPA_そり舌声母加i_ɻ̩を含む(string hanzi, string expectedVowel)
        {
            var result = _engine.ToPiperIPA(hanzi);
            Assert.Contains(expectedVowel, result);
        }

        // =====================================================================
        // 12. 複合テキストテスト
        // =====================================================================

        [Fact]
        public void ToPiperIPA_複数漢字_スペース区切り()
        {
            // "中国" → 2音節がスペース区切り
            var result = _engine.ToPiperIPA("\u4E2D\u56FD");
            Assert.Contains(" ", result);
        }

        [Fact]
        public void ToPiperIPA_既存IPA出力と異なることの確認()
        {
            // "东" (dōng) の piper-plus IPA と既存 IPA は異なるはず
            var piperResult = _engine.ToPiperIPA("\u4E1C");
            var standardResult = _engine.ToIPA("\u4E1C", false);
            // piper-plus: tuŋ, 既存: tʊŋ → 異なる
            Assert.NotEqual(piperResult, standardResult);
        }
    }
}
