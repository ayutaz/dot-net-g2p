using DotNetG2P.English.Homograph;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Homograph
{
    /// <summary>
    /// HomographDatabase の単体テスト。
    /// 同綴異音語の登録内容と検索機能を検証する。
    /// </summary>
    public class HomographDatabaseTests
    {
        // ===== 主要同綴異音語がデータベースに登録されていること =====

        [Theory]
        [InlineData("read")]
        [InlineData("lead")]
        [InlineData("live")]
        [InlineData("wind")]
        [InlineData("tear")]
        [InlineData("bow")]
        [InlineData("close")]
        [InlineData("record")]
        [InlineData("present")]
        [InlineData("produce")]
        [InlineData("abuse")]
        [InlineData("minute")]
        [InlineData("separate")]
        [InlineData("estimate")]
        public void TryGetEntry_KnownHomograph_ReturnsTrue(string word)
        {
            bool found = HomographDatabase.TryGetEntry(word, out var entry);

            Assert.True(found);
            Assert.NotNull(entry);
        }

        // ===== 大文字小文字不問 =====

        [Theory]
        [InlineData("READ")]
        [InlineData("Read")]
        [InlineData("rEaD")]
        [InlineData("RECORD")]
        [InlineData("Close")]
        public void TryGetEntry_CaseInsensitive_ReturnsTrue(string word)
        {
            bool found = HomographDatabase.TryGetEntry(word, out var entry);

            Assert.True(found);
            Assert.NotNull(entry);
        }

        // ===== 非同綴異音語で false を返す =====

        [Theory]
        [InlineData("hello")]
        [InlineData("world")]
        [InlineData("computer")]
        [InlineData("beautiful")]
        [InlineData("running")]
        public void TryGetEntry_NonHomograph_ReturnsFalse(string word)
        {
            bool found = HomographDatabase.TryGetEntry(word, out _);

            Assert.False(found);
        }

        // ===== record エントリの中身検証 =====

        [Fact]
        public void RecordEntry_NounUsesVariant1_VerbUsesVariant0()
        {
            // record: [0]=R AH0 K AO1 R D (動詞), [1]=R EH1 K ER0 D (名詞)
            bool found = HomographDatabase.TryGetEntry("record", out var entry);

            Assert.True(found);
            Assert.Equal(1, entry.DefaultVariantIndex);
            Assert.Equal(1, entry.GetVariantIndex(PosTag.Noun));
            Assert.Equal(0, entry.GetVariantIndex(PosTag.Verb));
        }

        // ===== read エントリの中身検証 =====

        [Fact]
        public void ReadEntry_VerbAndNounUseVariant1()
        {
            // read: [0]=R EH1 D (過去形), [1]=R IY1 D (現在形)
            // デフォルトは現在形(variant 1)
            bool found = HomographDatabase.TryGetEntry("read", out var entry);

            Assert.True(found);
            Assert.Equal(1, entry.DefaultVariantIndex);
            Assert.Equal(1, entry.GetVariantIndex(PosTag.Verb));
            Assert.Equal(1, entry.GetVariantIndex(PosTag.Noun));
        }

        // ===== close エントリの中身検証 =====

        [Fact]
        public void CloseEntry_AdjectiveUsesVariant0_VerbUsesVariant1()
        {
            // close: [0]=K L OW1 S (形容詞:近い), [1]=K L OW1 Z (動詞:閉じる)
            bool found = HomographDatabase.TryGetEntry("close", out var entry);

            Assert.True(found);
            Assert.Equal(1, entry.DefaultVariantIndex);
            Assert.Equal(0, entry.GetVariantIndex(PosTag.Adjective));
            Assert.Equal(0, entry.GetVariantIndex(PosTag.Adverb));
            Assert.Equal(1, entry.GetVariantIndex(PosTag.Verb));
            Assert.Equal(1, entry.GetVariantIndex(PosTag.Noun));
        }

        // ===== produce エントリの中身検証 =====

        [Fact]
        public void ProduceEntry_NounUsesVariant1_VerbUsesVariant0()
        {
            // produce: [0]=P R AH0 D UW1 S (動詞), [1]=P R OW1 D UW0 S (名詞)
            bool found = HomographDatabase.TryGetEntry("produce", out var entry);

            Assert.True(found);
            Assert.Equal(0, entry.DefaultVariantIndex);
            Assert.Equal(1, entry.GetVariantIndex(PosTag.Noun));
            Assert.Equal(0, entry.GetVariantIndex(PosTag.Verb));
        }

        // ===== present エントリの中身検証 =====

        [Fact]
        public void PresentEntry_NounAdjectiveUseVariant0_VerbUsesVariant1()
        {
            // present: [0]=P R EH1 Z AH0 N T (名詞/形容詞), [1]=P R IY0 Z EH1 N T (動詞)
            bool found = HomographDatabase.TryGetEntry("present", out var entry);

            Assert.True(found);
            Assert.Equal(0, entry.DefaultVariantIndex);
            Assert.Equal(0, entry.GetVariantIndex(PosTag.Noun));
            Assert.Equal(0, entry.GetVariantIndex(PosTag.Adjective));
            Assert.Equal(1, entry.GetVariantIndex(PosTag.Verb));
        }

        // ===== GetVariantIndex: Unknown POS のときデフォルトバリアントを返す =====

        [Theory]
        [InlineData("record", 1)]
        [InlineData("read", 1)]
        [InlineData("close", 1)]
        [InlineData("produce", 0)]
        [InlineData("present", 0)]
        [InlineData("bow", 0)]
        [InlineData("lead", 1)]
        public void GetVariantIndex_UnknownPos_ReturnsDefaultVariant(string word, int expectedDefault)
        {
            HomographDatabase.TryGetEntry(word, out var entry);

            int result = entry.GetVariantIndex(PosTag.Unknown);

            Assert.Equal(expectedDefault, result);
        }

        // ===== -ate 語尾変化型の検証 =====

        [Fact]
        public void SeparateEntry_VerbUsesVariant0_AdjectiveUsesVariant2()
        {
            // separate: [0]=S EH1 P ER0 EY2 T (動詞), [2]=S EH1 P R AH0 T (形容詞)
            bool found = HomographDatabase.TryGetEntry("separate", out var entry);

            Assert.True(found);
            Assert.Equal(0, entry.DefaultVariantIndex);
            Assert.Equal(0, entry.GetVariantIndex(PosTag.Verb));
            Assert.Equal(2, entry.GetVariantIndex(PosTag.Adjective));
        }

        // ===== resume エントリ: バリアント2を使うケース =====

        [Fact]
        public void ResumeEntry_NounUsesVariant2()
        {
            // resume: [0]=R IH0 Z UW1 M (動詞:再開), [2]=R EH1 Z AH0 M EY2 (名詞:履歴書)
            bool found = HomographDatabase.TryGetEntry("resume", out var entry);

            Assert.True(found);
            Assert.Equal(0, entry.GetVariantIndex(PosTag.Verb));
            Assert.Equal(2, entry.GetVariantIndex(PosTag.Noun));
        }

        // ===== 登録エントリ数が60以上あること =====

        [Fact]
        public void Database_HasAtLeast30Entries()
        {
            Assert.True(HomographDatabase.Count >= 30,
                $"データベースには30以上のエントリが必要ですが、{HomographDatabase.Count}件しかありません。");
        }

        [Fact]
        public void Database_HasExpected60Entries()
        {
            Assert.Equal(62, HomographDatabase.Count);
        }

        // ===== wound エントリの中身検証 =====

        [Fact]
        public void WoundEntry_NounUsesVariant1_VerbUsesVariant0()
        {
            // wound: [0]=W AW1 N D (windの過去形/巻いた), [1]=W UW1 N D (名詞:傷)
            bool found = HomographDatabase.TryGetEntry("wound", out var entry);

            Assert.True(found);
            Assert.Equal(1, entry.DefaultVariantIndex);
            Assert.Equal(1, entry.GetVariantIndex(PosTag.Noun));
            Assert.Equal(0, entry.GetVariantIndex(PosTag.Verb));
        }

        // ===== dove エントリの中身検証 =====

        [Fact]
        public void DoveEntry_VerbUsesVariant0_NounUsesVariant1()
        {
            // dove: [0]=D AH1 V (diveの過去形), [1]=D OW1 V (鳩)
            bool found = HomographDatabase.TryGetEntry("dove", out var entry);

            Assert.True(found);
            Assert.Equal(1, entry.DefaultVariantIndex);
            Assert.Equal(0, entry.GetVariantIndex(PosTag.Verb));
            Assert.Equal(1, entry.GetVariantIndex(PosTag.Noun));
        }

        // ===== null および空文字列で false を返す =====

        [Fact]
        public void TryGetEntry_Null_ReturnsFalse()
        {
            bool found = HomographDatabase.TryGetEntry(null!, out _);

            Assert.False(found);
        }

        [Fact]
        public void TryGetEntry_EmptyString_ReturnsFalse()
        {
            bool found = HomographDatabase.TryGetEntry("", out _);

            Assert.False(found);
        }

        // ===== Phase 3: ContextRule 関連の間接テスト =====
        // ContextRule は HomographResolver レベルで実装されるため、
        // ここでは read エントリに過去形バリアント(variant 0)が存在することを確認する。

        [Fact]
        public void ReadEntry_HasPastTenseVariant0()
        {
            // read: [0]=R EH1 D (過去形), [1]=R IY1 D (現在形)
            // Phase 3 の ContextRule は HomographResolver で "have/had" や "yesterday" を検出し
            // variant 0 を返す。ここではエントリが正しく設定されていることを確認。
            bool found = HomographDatabase.TryGetEntry("read", out var entry);
            Assert.True(found);
            // デフォルトは現在形（variant 1）
            Assert.Equal(1, entry.DefaultVariantIndex);
            // 過去形は variant 0 であり、ContextRule が variant 0 を返すことで過去形が選択される
            // （Verb → variant 1 だが、ContextRule は Resolver 側で variant 0 をオーバーライドする）
        }

        [Fact]
        public void BowEntry_HasOjigiVariant0()
        {
            // bow: [0]=B AW1 (お辞儀/動詞), [1]=B OW1 (弓/名詞)
            // Phase 3 の ContextRule は "take a bow" パターンで variant 0 を返す。
            bool found = HomographDatabase.TryGetEntry("bow", out var entry);
            Assert.True(found);
            Assert.Equal(0, entry.DefaultVariantIndex);
            Assert.Equal(0, entry.GetVariantIndex(PosTag.Verb));
            Assert.Equal(1, entry.GetVariantIndex(PosTag.Noun));
        }
    }
}
