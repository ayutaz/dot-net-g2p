// Expected values verified against misaki 0.9.4 via uv run (Phase 1-R)
// Ground truth: .claude/tmp/misaki-gold.txt (137 entries, 実測値)
// Spec reference: .claude/tmp/misaki-spec.md
//
// 重要: 本ファイルの期待値はすべて gold.txt の実測値に基づく。
// 旧実装の期待値 (ʈʂ, u̯ŋ, yan, etc.) は無効。
// Misaki 合字: ʨ (U+02A8), ʦ (U+02A6), ꭧ (U+AB67)
// Misaki 母音: ɤ (U+0264), ə (U+0259), ʊ (U+028A), ɨ (U+0268),
//              ɥ (U+0265), ɛ (U+025B), ɚ (U+025A), ɔ (U+0254)
// Misaki 声調矢印: → (U+2192), ↗ (U+2197), ↓ (U+2193), ↘ (U+2198)

using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// PinyinToMisaki.Convert / ConvertSyllable の単体テスト。
    /// 期待値はすべて uv misaki 0.9.4 実測値（.claude/tmp/misaki-gold.txt）。
    /// 旧実装との差異:
    /// <list type="bullet">
    ///   <item>zh/ch: ʈʂ → ꭧ (U+AB67 合字)</item>
    ///   <item>ong/iong: u̯ŋ → ʊŋ (U+028A)</item>
    ///   <item>ai/ei/ao/ou: 末尾 U+032F 除去</item>
    ///   <item>retroflex/alveolar apical: ɻ̩/ɹ̩ → ɨ (U+0268)</item>
    ///   <item>Er: əɻ → ɚ (U+025A)</item>
    ///   <item>声調: ˥˥ 等の IPA tone letter → 矢印 → ↗ ↓ ↘</item>
    ///   <item>üe: jɛ → ɥe (半母音 ɥ)</item>
    ///   <item>üan: jan → ɥɛn</item>
    /// </list>
    /// </summary>
    public class PinyinToMisakiConvertTests
    {
        // ════════════════════════════════════════════════════════════
        // 1. 4 声 + 軽声 × ma 系
        // gold.txt: ma1-ma5
        // ════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("mā", "ma\u2192")]   // ma1 1声 →
        [InlineData("má", "ma\u2197")]   // ma2 2声 ↗
        [InlineData("mǎ", "ma\u2193")]   // ma3 3声 ↓
        [InlineData("mà", "ma\u2198")]   // ma4 4声 ↘
        [InlineData("ma", "ma")]          // ma5 軽声 矢印なし
        public void Convert_MaSeries_AllTones(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 2. 全声母 × a/i 系代表 (21 エントリ)
        // gold.txt: 行 6-25
        // ════════════════════════════════════════════════════════════

        [Theory]
        // 両唇・唇歯音 (b/p/m/f)
        [InlineData("bā", "pa\u2192")]                   // ba1  → pa→
        [InlineData("pá", "p\u02B0a\u2197")]             // pa2  → pʰa↗
        [InlineData("fǎ", "fa\u2193")]                   // fa3  → fa↓
        // 歯茎音 (d/t/n/l)
        [InlineData("dà", "ta\u2198")]                   // da4  → ta↘
        [InlineData("tā", "t\u02B0a\u2192")]             // ta1  → tʰa→
        [InlineData("ná", "na\u2197")]                   // na2  → na↗
        [InlineData("lǎ", "la\u2193")]                   // la3  → la↓
        // 軟口蓋音 (g/k/h)
        [InlineData("gà", "ka\u2198")]                   // ga4  → ka↘
        [InlineData("kā", "k\u02B0a\u2192")]             // ka1  → kʰa→
        [InlineData("há", "xa\u2197")]                   // ha2  → xa↗
        // 歯茎硬口蓋音 (j/q/x) — 合字 ʨ を使用
        [InlineData("jī", "\u02A8i\u2192")]              // ji1  → ʨi→
        [InlineData("qí", "\u02A8\u02B0i\u2197")]        // qi2  → ʨʰi↗
        [InlineData("xǐ", "\u0255i\u2193")]              // xi3  → ɕi↓
        public void Convert_AllInitials_RepresentativeVowel(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 3. j/q/z/c/zh/ch 合字 (Misaki 固有、標準 IPA との差分)
        // 標準 IPA では tɕ/tɕʰ/ts/tsʰ/ʈʂ/ʈʂʰ
        // Misaki では ʨ/ʨʰ/ʦ/ʦʰ/ꭧ/ꭧʰ (合字 U+02A8, U+02A6, U+AB67)
        // ════════════════════════════════════════════════════════════

        [Fact]
        public void Convert_J_UsesLigatureTc()
        {
            // ji1: 標準 IPA tɕi˥˥ → Misaki ʨi→
            Assert.Equal("\u02A8i\u2192", PinyinToMisaki.Convert("jī"));
        }

        [Fact]
        public void Convert_Q_UsesLigatureTcWithAspiration()
        {
            // qi1: 標準 IPA tɕʰi˥˥ → Misaki ʨʰi→
            Assert.Equal("\u02A8\u02B0i\u2192", PinyinToMisaki.Convert("qī"));
        }

        [Fact]
        public void Convert_Z_UsesLigatureTsBeforeVowel()
        {
            // za1: Z + A → ʦ + a + → = ʦa→
            Assert.Equal("\u02A6a\u2192", PinyinToMisaki.Convert("zā"));
        }

        [Fact]
        public void Convert_C_UsesLigatureTsWithAspiration()
        {
            // ca1: C + A → ʦʰ + a + → = ʦʰa→
            Assert.Equal("\u02A6\u02B0a\u2192", PinyinToMisaki.Convert("cā"));
        }

        [Fact]
        public void Convert_Zh_UsesMisakiLigatureNotRetroflex()
        {
            // zhi1: Zh + I (retroflex apical) → ꭧ + ɨ + → = ꭧɨ→
            // 旧実装 (ʈʂɻ̩) とは異なる
            Assert.Equal("\uAB67\u0268\u2192", PinyinToMisaki.Convert("zhī"));
        }

        [Fact]
        public void Convert_Ch_UsesMisakiLigatureWithAspiration()
        {
            // chi1: Ch + I (retroflex apical) → ꭧʰ + ɨ + → = ꭧʰɨ→
            Assert.Equal("\uAB67\u02B0\u0268\u2192", PinyinToMisaki.Convert("chī"));
        }

        // ════════════════════════════════════════════════════════════
        // 4. 二重母音 U+032F strip 検証
        // 旧実装は ai/ei/ao/ou に U+032F を付けていたが
        // Misaki では strip 済みの ai/ei/au/ou を使う
        // ════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("bái", "pai\u2197")]                 // B+Ai → pai↗ (NOT pai̯↗)
        [InlineData("měi", "mei\u2193")]                 // M+Ei → mei↓
        [InlineData("mǎo", "mau\u2193")]                 // M+Ao → mau↓ (au, NOT ao)
        [InlineData("dòu", "tou\u2198")]                 // D+Ou → tou↘
        [InlineData("miáo", "mjau\u2197")]               // M+Iao → mjau↗
        [InlineData("liù", "ljou\u2198")]                // L+Iu(iou) → ljou↘
        [InlineData("guāi", "kwai\u2192")]               // G+Uai → kwai→
        [InlineData("duì", "twei\u2198")]                // D+Ui(uei) → twei↘
        public void Convert_Diphthongs_NoNonSyllabicMark(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
            // 重要: 期待値に U+032F を含まない
            Assert.DoesNotContain("\u032F", PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 5. CVC 声調位置検証 (声調が末尾ではなく coda の前)
        // 例: man1 → ma→n (NOT man→)
        // ════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("mān", "ma\u2192n")]                 // man1 → ma→n
        [InlineData("mán", "ma\u2197n")]                 // man2 → ma↗n
        [InlineData("mǎn", "ma\u2193n")]                 // man3 → ma↓n
        [InlineData("màn", "ma\u2198n")]                 // man4 → ma↘n
        [InlineData("māng", "ma\u2192\u014B")]           // mang1 → ma→ŋ
        [InlineData("máng", "ma\u2197\u014B")]           // mang2 → ma↗ŋ
        [InlineData("mǎng", "ma\u2193\u014B")]           // mang3 → ma↓ŋ
        [InlineData("màng", "ma\u2198\u014B")]           // mang4 → ma↘ŋ
        [InlineData("mēn", "m\u0259\u2192n")]            // men1 → mə→n
        [InlineData("méng", "m\u0259\u2197\u014B")]      // meng2 → mə↗ŋ
        [InlineData("dōng", "t\u028A\u2192\u014B")]      // dong1 → tʊ→ŋ
        [InlineData("xióng", "\u0255j\u028A\u2197\u014B")] // xiong2 → ɕjʊ↗ŋ
        public void Convert_CVC_ToneBetweenNucleusAndCoda(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 6. i 系韻母 j 半母音 (Ia/Ie/Iao/Iu/Ian/Iang/Iong は j 付き、
        //    In/Ing は j なし)
        // ════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("jiā",   "\u02A8ja\u2192")]                    // jia1  → ʨja→
        [InlineData("jiān",  "\u02A8j\u025B\u2192n")]              // jian1 → ʨjɛ→n
        [InlineData("jiāng", "\u02A8ja\u2192\u014B")]              // jiang1 → ʨja→ŋ
        [InlineData("jiāo",  "\u02A8jau\u2192")]                   // jiao1 → ʨjau→
        [InlineData("jiē",   "\u02A8je\u2192")]                    // jie1  → ʨje→
        [InlineData("jiū",   "\u02A8jou\u2192")]                   // jiu1 (iou) → ʨjou→
        [InlineData("jīn",   "\u02A8i\u2192n")]                    // jin1  → ʨi→n (j なし)
        [InlineData("jīng",  "\u02A8i\u2192\u014B")]               // jing1 → ʨi→ŋ (j なし)
        [InlineData("jiōng", "\u02A8j\u028A\u2192\u014B")]         // jiong1 → ʨjʊ→ŋ
        [InlineData("liā",   "lja\u2192")]                         // lia1  → lja→
        [InlineData("liē",   "lje\u2192")]                         // lie1  → lje→
        [InlineData("liān",  "lj\u025B\u2192n")]                   // lian1 → ljɛ→n
        public void Convert_IFinals_jSemivowel(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 7. u 系韻母 w 半母音
        // ════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("guā",   "kwa\u2192")]                         // gua1  → kwa→
        [InlineData("guāi",  "kwai\u2192")]                        // guai1 → kwai→
        [InlineData("guān",  "kwa\u2192n")]                        // guan1 → kwa→n
        [InlineData("guāng", "kwa\u2192\u014B")]                   // guang1 → kwa→ŋ
        [InlineData("guì",   "kwei\u2198")]                        // gui1(uei) → kwei↘
        [InlineData("gùn",   "kw\u0259\u2198n")]                   // gun1(uen) → kwə↘n
        [InlineData("guǒ",   "kwo\u2193")]                         // guo3  → kwo↓
        [InlineData("duō",   "two\u2192")]                         // duo1  → two→
        public void Convert_UFinals_wSemivowel(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 8. ü 系韻母 ɥ (撮口呼)
        // j/q/x + ü → ʨy (Vn/V は ɥ 省略)、ɥe (Ve)、ɥɛn (Van)
        // ════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("juē",  "\u02A8\u0265e\u2192")]                 // jue1  → ʨɥe→
        [InlineData("juān", "\u02A8\u0265\u025B\u2192n")]           // juan1 → ʨɥɛ→n
        [InlineData("jūn",  "\u02A8y\u2192n")]                      // jun1 (ü+n → y+n、ɥ 省略) → ʨy→n
        [InlineData("lǜ",   "ly\u2198")]                            // lv4   → ly↘
        [InlineData("lüè",  "l\u0265e\u2198")]                      // lve4  → lɥe↘
        [InlineData("nǚ",   "ny\u2193")]                            // nv3   → ny↓
        public void Convert_VFinals_yOrEta(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 9. Y + Final (Y は半母音 j に展開、一部は Initial 省略)
        // gold.txt: yi=i, yin=in, ying=iŋ, yu=y, yun=yn (omit=true)
        //           ya=ja, ye=je, yao=jau, you=jou, yan=jɛn, yang=jaŋ,
        //           yong=jʊŋ, yue=ɥe, yuan=ɥɛn (omit=false)
        // ════════════════════════════════════════════════════════════

        [Theory]
        // Omit=true (j/ɥ 省略)
        [InlineData("yī",   "i\u2192")]                             // yi1  → i→
        [InlineData("yīn",  "i\u2192n")]                            // yin1 → i→n
        [InlineData("yīng", "i\u2192\u014B")]                       // ying1 → i→ŋ
        [InlineData("yū",   "y\u2192")]                             // yu1  → y→ (ü 入力)
        [InlineData("yūn",  "y\u2192n")]                            // yun1 → y→n
        // Omit=false
        [InlineData("yā",   "ja\u2192")]                            // ya1  → ja→
        [InlineData("yē",   "je\u2192")]                            // ye1  → je→
        [InlineData("yāo",  "jau\u2192")]                           // yao1 → jau→
        [InlineData("yōu",  "jou\u2192")]                           // you1 → jou→
        [InlineData("yān",  "j\u025B\u2192n")]                      // yan1 → jɛ→n
        [InlineData("yāng", "ja\u2192\u014B")]                      // yang1 → ja→ŋ
        [InlineData("yōng", "j\u028A\u2192\u014B")]                 // yong1 → jʊ→ŋ
        [InlineData("yuē",  "\u0265e\u2192")]                       // yue1  → ɥe→
        [InlineData("yuān", "\u0265\u025B\u2192n")]                 // yuan1 → ɥɛ→n
        public void Convert_YFinals_Semivowel(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 10. W + Final (W は半母音 w に展開、wu は省略)
        // ════════════════════════════════════════════════════════════

        [Theory]
        // Omit=true (w 省略)
        [InlineData("wū",   "u\u2192")]                             // wu1  → u→
        // Omit=false
        [InlineData("wā",   "wa\u2192")]                            // wa1  → wa→
        [InlineData("wāi",  "wai\u2192")]                           // wai1 → wai→
        [InlineData("wān",  "wa\u2192n")]                           // wan1 → wa→n
        [InlineData("wāng", "wa\u2192\u014B")]                      // wang1 → wa→ŋ
        [InlineData("wēi",  "wei\u2192")]                           // wei1 → wei→
        [InlineData("wēn",  "w\u0259\u2192n")]                      // wen1 → wə→n
        [InlineData("wēng", "w\u0259\u2192\u014B")]                 // weng1 → wə→ŋ
        [InlineData("wǒ",   "wo\u2193")]                            // wo3  → wo↓
        public void Convert_WFinals_Semivowel(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 11. そり舌 (Zh/Ch/Sh/R + I → 声母 + ɨ)
        // gold.txt: 行 19-22, 63-66
        // ════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("zhī", "\uAB67\u0268\u2192")]                   // ꭧɨ→
        [InlineData("chī", "\uAB67\u02B0\u0268\u2192")]             // ꭧʰɨ→
        [InlineData("shī", "\u0282\u0268\u2192")]                   // ʂɨ→
        [InlineData("rì",  "\u027B\u0268\u2198")]                   // ɻɨ↘
        public void Convert_RetroflexPlusI_UsesIBarred(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 12. 歯茎 (Z/C/S + I → 声母 + ɨ)
        // gold.txt: 行 23-25, 60-62
        // ════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("zī", "\u02A6\u0268\u2192")]                    // ʦɨ→
        [InlineData("cī", "\u02A6\u02B0\u0268\u2192")]              // ʦʰɨ→
        [InlineData("sī", "s\u0268\u2192")]                         // sɨ→
        public void Convert_AlveolarPlusI_UsesIBarred(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 13. 感嘆詞 Er (Final.Er → ɚ 単独、声母なしケース)
        // gold.txt: 行 122-125
        // ════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("ēr", "\u025A\u2192")]                          // er1 → ɚ→
        [InlineData("ér", "\u025A\u2197")]                          // er2 → ɚ↗
        [InlineData("ěr", "\u025A\u2193")]                          // er3 → ɚ↓
        [InlineData("èr", "\u025A\u2198")]                          // er4 → ɚ↘
        public void Convert_Er_StandaloneIpaFormat(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 14. 感嘆詞 O (Initial.None + Final.O → ɔ)
        // gold.txt: 行 126-129
        // 注意: 単独 "o" は ɔ だが、bo/po/mo/fo は pwo/pʰwo/mwo/fwo
        // ════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("ō", "\u0254\u2192")]                           // o1 → ɔ→
        [InlineData("ó", "\u0254\u2197")]                           // o2 → ɔ↗
        [InlineData("ǒ", "\u0254\u2193")]                           // o3 → ɔ↓
        [InlineData("ò", "\u0254\u2198")]                           // o4 → ɔ↘
        public void Convert_Standalone_O_UsesOpenO(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 15. bpmf + o → pwo/pʰwo/mwo/fwo (Final.O template は "wo")
        // gold.txt: 行 130-133
        // ════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("bō", "pwo\u2192")]                             // bo1 → pwo→
        [InlineData("pó", "p\u02B0wo\u2197")]                       // po2 → pʰwo↗
        [InlineData("mǒ", "mwo\u2193")]                             // mo3 → mwo↓
        [InlineData("fò", "fwo\u2198")]                             // fo4 → fwo↘
        public void Convert_BpmfPlusO_UsesWoTemplate(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 16. 軽声 + 代表韻母 (矢印なし)
        // ════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("ma", "ma")]                                    // 軽声 ma
        [InlineData("de", "t\u0264")]                               // 助詞「的」 → tɤ
        [InlineData("le", "l\u0264")]                               // 助詞「了」 → lɤ
        [InlineData("ba", "pa")]                                    // 軽声 ba → pa
        [InlineData("na", "na")]                                    // 軽声 na → na
        public void Convert_NeutralTone_NoArrow(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 17. includeTones=false (声調矢印を除去)
        // ════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("mā",    "ma")]                                 // ma→ → ma
        [InlineData("nǐ",    "ni")]                                 // ni↓ → ni
        [InlineData("hǎo",   "xau")]                                // xau↓ → xau
        [InlineData("zhōng", "\uAB67\u028A\u014B")]                 // ꭧʊ→ŋ → ꭧʊŋ
        [InlineData("xióng", "\u0255j\u028A\u014B")]                // ɕjʊ↗ŋ → ɕjʊŋ
        [InlineData("ér",    "\u025A")]                             // ɚ↗ → ɚ
        public void Convert_IncludeTonesFalse_OmitsArrow(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin, includeTones: false));
        }

        // ════════════════════════════════════════════════════════════
        // 18. エッジケース
        // ════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Convert_NullOrEmpty_ReturnsEmptyString(string? pinyin)
        {
            Assert.Equal(string.Empty, PinyinToMisaki.Convert(pinyin!));
        }

        [Theory]
        [InlineData("xyz")]                                         // 不正声母/韻母の組
        [InlineData("123")]                                         // 数字のみ
        [InlineData("!!!")]                                         // 記号のみ
        public void Convert_InvalidInput_ReturnsEmptyString(string pinyin)
        {
            Assert.Equal(string.Empty, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 19. Issue #56 参照 — 個別音節の変換
        // PinyinToMisaki は声調変調を行わず、各音節を個別に変換する
        // ════════════════════════════════════════════════════════════

        [Fact]
        public void Convert_Ni_ReferenceExample()
        {
            // nǐ (3声) → ni↓
            Assert.Equal("ni\u2193", PinyinToMisaki.Convert("nǐ"));
        }

        [Fact]
        public void Convert_Hao_ReferenceExample()
        {
            // hǎo (3声) → xau↓ (U+032F なし)
            Assert.Equal("xau\u2193", PinyinToMisaki.Convert("hǎo"));
        }

        // ════════════════════════════════════════════════════════════
        // 20. 数字声調形式 (ma1, zhong1 等)
        // 注意: lv1/jv1 等の v-only 形式は ToToneMarked の placement 制約により
        //       Convert(string) 経由では動作しないため ConvertSyllable で直接テスト
        // ════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("ma1",    "ma\u2192")]                          // ma1 → ma→
        [InlineData("ma2",    "ma\u2197")]                          // ma2 → ma↗
        [InlineData("ma3",    "ma\u2193")]                          // ma3 → ma↓
        [InlineData("ma4",    "ma\u2198")]                          // ma4 → ma↘
        [InlineData("zhong1", "\uAB67\u028A\u2192\u014B")]          // zhong1 → ꭧʊ→ŋ
        [InlineData("xiong2", "\u0255j\u028A\u2197\u014B")]         // xiong2 → ɕjʊ↗ŋ
        [InlineData("bo1",    "pwo\u2192")]                         // bo1 → pwo→
        [InlineData("er1",    "\u025A\u2192")]                      // er1 → ɚ→
        public void Convert_NumericToneForm_WorksEnd2End(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ════════════════════════════════════════════════════════════
        // 21. ConvertSyllable 直接 (internal API)
        // ════════════════════════════════════════════════════════════

        [Fact]
        public void ConvertSyllable_Basic_MAFirst()
        {
            var syllable = new PinyinSyllable(Initial.M, Final.A, Tone.First);
            Assert.Equal("ma\u2192", PinyinToMisaki.ConvertSyllable(syllable, includeTones: true));
        }

        [Fact]
        public void ConvertSyllable_WithoutTones_OmitsArrow()
        {
            var syllable = new PinyinSyllable(Initial.N, Final.I, Tone.Third);
            Assert.Equal("ni", PinyinToMisaki.ConvertSyllable(syllable, includeTones: false));
        }

        [Fact]
        public void ConvertSyllable_NeutralTone_NoArrowRegardlessOfFlag()
        {
            var syllable = new PinyinSyllable(Initial.M, Final.A, Tone.Neutral);
            Assert.Equal("ma", PinyinToMisaki.ConvertSyllable(syllable, includeTones: true));
            Assert.Equal("ma", PinyinToMisaki.ConvertSyllable(syllable, includeTones: false));
        }

        [Fact]
        public void ConvertSyllable_ZeroInitial_AOnly()
        {
            var syllable = new PinyinSyllable(Initial.None, Final.A, Tone.First);
            Assert.Equal("a\u2192", PinyinToMisaki.ConvertSyllable(syllable, includeTones: true));
        }

        [Fact]
        public void ConvertSyllable_ZeroInitial_OBecomesOpenO()
        {
            // Initial.None + Final.O → ɔ (単独感嘆詞)
            var syllable = new PinyinSyllable(Initial.None, Final.O, Tone.First);
            Assert.Equal("\u0254\u2192", PinyinToMisaki.ConvertSyllable(syllable, includeTones: true));
        }

        [Fact]
        public void ConvertSyllable_LvFirst_ViaSyllableApi()
        {
            // lv1 (L + V, tone 1) → ly→ (gold row 54)
            // 注意: Convert(string) 経由では ToToneMarked が placement 不能のため動作しない
            var syllable = new PinyinSyllable(Initial.L, Final.V, Tone.First);
            Assert.Equal("ly\u2192", PinyinToMisaki.ConvertSyllable(syllable, includeTones: true));
        }

        [Fact]
        public void ConvertSyllable_JvFirst_ViaSyllableApi()
        {
            // jv1 (J + V, tone 1) → ʨy→ (gold row 56)
            var syllable = new PinyinSyllable(Initial.J, Final.V, Tone.First);
            Assert.Equal("\u02A8y\u2192", PinyinToMisaki.ConvertSyllable(syllable, includeTones: true));
        }

        [Fact]
        public void ConvertSyllable_JvnFirst_ViaSyllableApi()
        {
            // jvn1 (J + Vn, tone 1) → ʨy→n (gold row 59)
            var syllable = new PinyinSyllable(Initial.J, Final.Vn, Tone.First);
            Assert.Equal("\u02A8y\u2192n", PinyinToMisaki.ConvertSyllable(syllable, includeTones: true));
        }

        // ════════════════════════════════════════════════════════════
        // 22. PinyinToIpa との差分が期待通りに現れることの確認
        // (標準 IPA とは合字・U+032F・声調記号で差分が出る)
        // ════════════════════════════════════════════════════════════

        [Fact]
        public void Convert_DiffersFromStandardIpa_AtJInitial()
        {
            string misaki = PinyinToMisaki.Convert("jī", includeTones: false);
            string standardIpa = PinyinToIpa.Convert("jī", includeTones: false);
            Assert.NotEqual(standardIpa, misaki);
            Assert.Equal("\u02A8i", misaki);   // Misaki: ʨi
            Assert.Equal("t\u0255i", standardIpa); // 標準: tɕi
        }

        [Fact]
        public void Convert_DiffersFromStandardIpa_AtZhInitial()
        {
            string misaki = PinyinToMisaki.Convert("zhī", includeTones: false);
            string standardIpa = PinyinToIpa.Convert("zhī", includeTones: false);
            Assert.NotEqual(standardIpa, misaki);
            Assert.Equal("\uAB67\u0268", misaki); // Misaki: ꭧɨ
            // 標準 IPA は異なる表記 (ʈʂɻ̩ 等) を使う
            Assert.NotEqual(misaki, standardIpa);
        }

        [Fact]
        public void Convert_DiffersFromStandardIpa_AtToneMarker()
        {
            string misaki = PinyinToMisaki.Convert("mā", includeTones: true);
            string standardIpa = PinyinToIpa.Convert("mā", includeTones: true);
            Assert.NotEqual(standardIpa, misaki);
            Assert.Equal("ma\u2192", misaki);          // Misaki: ma→
            Assert.Equal("ma\u02E5\u02E5", standardIpa); // 標準: ma˥˥
        }

        [Fact]
        public void Convert_DiffersFromStandardIpa_AtOngFinal()
        {
            // Misaki は Ong に ʊ (U+028A) を使う、標準 IPA は u̯ 等
            string misaki = PinyinToMisaki.Convert("dōng", includeTones: false);
            Assert.Equal("t\u028A\u014B", misaki);     // Misaki: tʊŋ
            Assert.DoesNotContain("\u032F", misaki);   // U+032F は含まれない
        }
    }
}
