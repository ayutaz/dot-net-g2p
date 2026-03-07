using DotNetG2P.English.Homograph;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Homograph
{
    /// <summary>
    /// HomographResolver の単体テスト。
    /// PosGuesser + HomographDatabase を組み合わせた文脈ベースの発音バリアント選択を検証する。
    /// </summary>
    public class HomographResolverTests
    {
        // ===== record: 動詞文脈 =====

        [Fact]
        public void Record_AfterWill_ReturnsVerbVariant0()
        {
            // "will record" → record は動詞, record: Verb→0
            var words = new[] { "will", "record" };

            int result = HomographResolver.ResolveVariantIndex(words, 1);

            Assert.Equal(0, result);
        }

        // ===== record: 名詞文脈 =====

        [Fact]
        public void Record_AfterThe_ReturnsNounVariant1()
        {
            // "the record" → record は名詞, record: Noun→1
            var words = new[] { "the", "record" };

            int result = HomographResolver.ResolveVariantIndex(words, 1);

            Assert.Equal(1, result);
        }

        // ===== read: 動詞文脈 =====

        [Fact]
        public void Read_AfterTo_ReturnsVerbVariant1()
        {
            // "to read" → read は動詞（現在形）, read: Verb→1
            var words = new[] { "to", "read" };

            int result = HomographResolver.ResolveVariantIndex(words, 1);

            Assert.Equal(1, result);
        }

        // ===== live: 冠詞の後（名詞文脈）→デフォルト =====

        [Fact]
        public void Live_AfterArticle_ReturnsAdjective0()
        {
            // "a live concert" → Phase 2: 冠詞+形容詞+名詞パターンを検出し
            // HasAdjectiveRule=trueのliveを形容詞(variant 0)として解決する
            var words = new[] { "a", "live", "concert" };

            int result = HomographResolver.ResolveVariantIndex(words, 1);

            Assert.Equal(0, result);
        }

        // ===== live: 動詞文脈 =====

        [Fact]
        public void Live_AfterI_ReturnsVerbVariant1()
        {
            // "I live" → 前の単語"I"はVerbContext → PosTag.Verb
            // live: Verb→1
            var words = new[] { "I", "live" };

            int result = HomographResolver.ResolveVariantIndex(words, 1);

            Assert.Equal(1, result);
        }

        // ===== wind: 名詞文脈 =====

        [Fact]
        public void Wind_AfterThe_ReturnsNounVariant1()
        {
            // "the wind" → wind は名詞, wind: Noun→1
            var words = new[] { "the", "wind" };

            int result = HomographResolver.ResolveVariantIndex(words, 1);

            Assert.Equal(1, result);
        }

        // ===== close: 動詞文脈 =====

        [Fact]
        public void Close_AfterPlease_ReturnsVerbVariant1()
        {
            // "please close" → 前の単語"please"はVerbContext → PosTag.Verb
            // close: Verb→1
            var words = new[] { "please", "close" };

            int result = HomographResolver.ResolveVariantIndex(words, 1);

            Assert.Equal(1, result);
        }

        // ===== 非同綴異音語は 0 を返す =====

        [Theory]
        [InlineData("hello")]
        [InlineData("world")]
        [InlineData("computer")]
        public void NonHomograph_ReturnsZero(string word)
        {
            var words = new[] { "the", word };

            int result = HomographResolver.ResolveVariantIndex(words, 1);

            Assert.Equal(0, result);
        }

        // ===== 単一単語（文脈なし）→ デフォルトバリアント =====

        [Fact]
        public void SingleWord_Record_ReturnsDefault1()
        {
            // 文脈なし（先頭位置）→ PosGuesser は suffix ルールにフォールバック
            // "record" は接尾辞ルールに該当しない → PosTag.Unknown → DefaultVariantIndex=1
            var words = new[] { "record" };

            int result = HomographResolver.ResolveVariantIndex(words, 0);

            Assert.Equal(1, result);
        }

        [Fact]
        public void SingleWord_Close_ReturnsDefault1()
        {
            // "close" は suffix -ive ではなく -ose でもない → PosTag.Unknown → default=1
            var words = new[] { "close" };

            int result = HomographResolver.ResolveVariantIndex(words, 0);

            Assert.Equal(1, result);
        }

        // ===== 空配列は 0 を返す =====

        [Fact]
        public void EmptyArray_ReturnsZero()
        {
            var words = new string[0];

            int result = HomographResolver.ResolveVariantIndex(words, 0);

            Assert.Equal(0, result);
        }

        // ===== 範囲外インデックスは 0 を返す =====

        [Fact]
        public void OutOfRangeIndex_ReturnsZero()
        {
            var words = new[] { "the", "record" };

            int result = HomographResolver.ResolveVariantIndex(words, 5);

            Assert.Equal(0, result);
        }

        [Fact]
        public void NegativeIndex_ReturnsZero()
        {
            var words = new[] { "the", "record" };

            int result = HomographResolver.ResolveVariantIndex(words, -1);

            Assert.Equal(0, result);
        }

        // ===== null は 0 を返す =====

        [Fact]
        public void NullWords_ReturnsZero()
        {
            int result = HomographResolver.ResolveVariantIndex(null!, 0);

            Assert.Equal(0, result);
        }

        // ===== wound: 動詞文脈 =====

        [Fact]
        public void Wound_AfterWill_ReturnsVerbVariant0()
        {
            // "will wound" → wound は動詞(windの過去形), wound: Verb→0
            var words = new[] { "will", "wound" };

            int result = HomographResolver.ResolveVariantIndex(words, 1);

            Assert.Equal(0, result);
        }

        // ===== wound: 名詞文脈 =====

        [Fact]
        public void Wound_AfterThe_ReturnsNounVariant1()
        {
            // "the wound" → wound は名詞(傷), wound: Noun→1
            var words = new[] { "the", "wound" };

            int result = HomographResolver.ResolveVariantIndex(words, 1);

            Assert.Equal(1, result);
        }

        // ===== dove: 動詞文脈 =====

        [Fact]
        public void Dove_AfterHe_ReturnsVerbVariant0()
        {
            // "he dove" → dove は動詞(diveの過去形), dove: Verb→0
            var words = new[] { "he", "dove" };

            int result = HomographResolver.ResolveVariantIndex(words, 1);

            Assert.Equal(0, result);
        }

        // ===== dove: 名詞文脈 =====

        [Fact]
        public void Dove_AfterThe_ReturnsNounVariant1()
        {
            // "the dove" → dove は名詞(鳩), dove: Noun→1
            var words = new[] { "the", "dove" };

            int result = HomographResolver.ResolveVariantIndex(words, 1);

            Assert.Equal(1, result);
        }

        // ===== 前置詞文脈テスト =====

        [Fact]
        public void Lead_AfterOf_ReturnsNounVariant0()
        {
            // "of lead" → lead は名詞(鉛), lead: Noun→0
            var words = new[] { "made", "of", "lead" };

            int result = HomographResolver.ResolveVariantIndex(words, 2);

            Assert.Equal(0, result);
        }

        [Fact]
        public void Desert_AfterIn_ReturnsNounVariant0()
        {
            // "in the desert" → desert は名詞(砂漠), desert: Noun→0
            var words = new[] { "in", "the", "desert" };

            int result = HomographResolver.ResolveVariantIndex(words, 2);

            Assert.Equal(0, result);
        }

        // ===== 追加の文脈テスト: ストレスシフト型 =====

        [Fact]
        public void Present_AfterWill_ReturnsVerbVariant1()
        {
            // "will present" → 動詞 → present: Verb→1
            var words = new[] { "will", "present" };

            int result = HomographResolver.ResolveVariantIndex(words, 1);

            Assert.Equal(1, result);
        }

        [Fact]
        public void Present_AfterThe_ReturnsNounVariant0()
        {
            // "the present" → 名詞 → present: Noun→0
            var words = new[] { "the", "present" };

            int result = HomographResolver.ResolveVariantIndex(words, 1);

            Assert.Equal(0, result);
        }

        // ===== Phase 1A: 文頭の動詞解決 =====

        [Fact]
        public void Wind_文頭_後続up_ReturnsVerbVariant()
        {
            // "Wind up the clock" → 動詞(巻く) variant 0
            var words = new[] { "Wind", "up", "the", "clock" };
            int result = HomographResolver.ResolveVariantIndex(words, 0);
            Assert.Equal(0, result);
        }

        [Fact]
        public void Record_文頭_後続the_ReturnsVerbVariant()
        {
            // "Record the song" → 動詞 variant 0
            var words = new[] { "Record", "the", "song" };
            int result = HomographResolver.ResolveVariantIndex(words, 0);
            Assert.Equal(0, result);
        }

        // ===== Phase 1B: リンキング動詞 =====

        [Fact]
        public void Close_AfterStay_ReturnsAdjectiveVariant()
        {
            // "Stay close" → 形容詞(近い) variant 0
            var words = new[] { "Stay", "close" };
            int result = HomographResolver.ResolveVariantIndex(words, 1);
            Assert.Equal(0, result);
        }

        // ===== Phase 2: 冠詞+形容詞+名詞 =====

        [Fact]
        public void Live_AfterArticle_BeforeNoun_ReturnsAdjectiveVariant()
        {
            // "a live concert" → 形容詞 variant 0
            var words = new[] { "a", "live", "concert" };
            int result = HomographResolver.ResolveVariantIndex(words, 1);
            Assert.Equal(0, result);
        }

        // ===== Phase 3: ContextRule =====

        [Fact]
        public void Read_AfterHave_ReturnsPastParticipleVariant()
        {
            // "I have read the book" → 過去分詞 variant 0
            var words = new[] { "I", "have", "read", "the", "book" };
            int result = HomographResolver.ResolveVariantIndex(words, 2);
            Assert.Equal(0, result);
        }

        [Fact]
        public void Read_AfterHad_ReturnsPastParticipleVariant()
        {
            // "She had read it" → 過去分詞 variant 0
            var words = new[] { "She", "had", "read", "it" };
            int result = HomographResolver.ResolveVariantIndex(words, 2);
            Assert.Equal(0, result);
        }

        [Fact]
        public void Read_WithYesterday_ReturnsPastVariant()
        {
            // "I read that yesterday" → 過去形 variant 0
            var words = new[] { "I", "read", "that", "yesterday" };
            int result = HomographResolver.ResolveVariantIndex(words, 1);
            Assert.Equal(0, result);
        }

        [Fact]
        public void Bow_AfterTake_ReturnsOjigi()
        {
            // "take a bow" → お辞儀 variant 0
            var words = new[] { "take", "a", "bow" };
            int result = HomographResolver.ResolveVariantIndex(words, 2);
            Assert.Equal(0, result);
        }
    }
}
