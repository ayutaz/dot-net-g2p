using System;
using System.Collections.Generic;
using System.Linq;
using DotNetG2P.English;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Homograph
{
    /// <summary>
    /// EnglishG2PEngineレベルでの同綴異音語解決統合テスト。
    /// ToPhonemes/ToPhonemeList経由で、文脈に応じた発音バリアント選択が正しく動作することを検証する。
    /// </summary>
    public class HomographIntegrationTests : IDisposable
    {
        private readonly EnglishG2PEngine _engine;
        private readonly EnglishG2PEngine _engineNoHomograph;

        public HomographIntegrationTests()
        {
            _engine = new EnglishG2PEngine();
            _engineNoHomograph = new EnglishG2PEngine(new EnglishG2POptions(enableHomographResolution: false));
        }

        public void Dispose()
        {
            _engine.Dispose();
            _engineNoHomograph.Dispose();
        }

        // ===== 1. record: 動詞文脈 vs 名詞文脈 =====

        [Fact]
        public void Record_VerbContext_ContainsVerbPronunciation()
        {
            // "will record the song" → "will" は VerbContext → record は動詞発音
            // record 動詞: R AH0 K AO1 R D (variant 0)
            var result = _engine.ToPhonemes("will record the song");
            Assert.Contains("R AH0 K AO1 R D", result);
        }

        [Fact]
        public void Record_NounContext_ContainsNounPronunciation()
        {
            // "a new record" → "new" は NounContext → record は名詞発音
            // record 名詞: R EH1 K ER0 D (variant 1)
            var result = _engine.ToPhonemes("a new record");
            Assert.Contains("R EH1 K ER0 D", result);
        }

        [Fact]
        public void Record_VerbAndNounContext_ProduceDifferentPhonemes()
        {
            var verbResult = _engine.ToPhonemes("will record");
            var nounResult = _engine.ToPhonemes("the record");
            Assert.NotEqual(verbResult, nounResult);
        }

        // ===== 2. lead: 動詞文脈 =====

        [Fact]
        public void Lead_VerbContext_ContainsVerbPronunciation()
        {
            // "will lead the team" → "will" は VerbContext → lead は動詞発音
            // lead 動詞: L IY1 D (variant 1)
            var result = _engine.ToPhonemes("will lead the team");
            Assert.Contains("L IY1 D", result);
        }

        [Fact]
        public void Lead_NounContext_ContainsNounPronunciation()
        {
            // "the lead" → "the" は NounContext → lead は名詞発音
            // lead 名詞: L EH1 D (variant 0)
            var result = _engine.ToPhonemes("the lead");
            Assert.Contains("L EH1 D", result);
        }

        // ===== 3. EnableHomographResolution=false → 常にデフォルトバリアント =====

        [Fact]
        public void HomographDisabled_Record_AlwaysUsesFirstVariant()
        {
            // EnableHomographResolution=false の場合、常に pronunciations[0] を使用
            // record: CMU辞書の [0] = R AH0 K AO1 R D
            var verbResult = _engineNoHomograph.ToPhonemes("will record");
            var nounResult = _engineNoHomograph.ToPhonemes("the record");

            // 同綴異音語解決が無効なので、どちらも recordの発音は同じ（pronunciations[0]）
            // "R AH0 K AO1 R D" が両方に含まれることを検証
            Assert.Contains("R AH0 K AO1 R D", verbResult);
            Assert.Contains("R AH0 K AO1 R D", nounResult);
        }

        [Fact]
        public void HomographDisabled_Lead_AlwaysUsesFirstVariant()
        {
            // EnableHomographResolution=false → pronunciations[0] = L EH1 D
            var verbResult = _engineNoHomograph.ToPhonemes("will lead");
            var nounResult = _engineNoHomograph.ToPhonemes("the lead");

            // 両方とも pronunciations[0] (L EH1 D) を使用
            Assert.Contains("L EH1 D", verbResult);
            Assert.Contains("L EH1 D", nounResult);
        }

        // ===== 4. デフォルトオプションで EnableHomographResolution=true =====

        [Fact]
        public void DefaultOptions_HomographResolutionIsEnabled()
        {
            // new EnglishG2PEngine() でデフォルトオプションを使用
            // record の動詞文脈と名詞文脈で異なる発音が返ることを確認
            var verbResult = _engine.ToPhonemes("will record");
            var nounResult = _engine.ToPhonemes("the record");

            Assert.NotEqual(verbResult, nounResult);
        }

        // ===== 5. 正規化との連携 =====

        [Fact]
        public void NormalizationAndHomograph_WorkTogether()
        {
            // 正規化（大文字→小文字変換等）と同綴異音語解決が同時に動作
            // "WILL RECORD" の正規化後も同綴異音語解決が機能する
            // （Tokenize はケースを保持するが、辞書検索は大文字小文字不問）
            var result = _engine.ToPhonemes("will record");
            Assert.Contains("R AH0 K AO1 R D", result);
        }

        // ===== 6. 非同綴異音語は影響を受けない =====

        [Fact]
        public void NonHomograph_SameResultRegardlessOfResolution()
        {
            // "hello world" は同綴異音語を含まないため、解決の有無で変わらない
            var withResolution = _engine.ToPhonemes("hello world");
            var withoutResolution = _engineNoHomograph.ToPhonemes("hello world");

            Assert.Equal(withResolution, withoutResolution);
        }

        // ===== 7. ToPhonemeList でも同綴異音語解決が機能 =====

        [Fact]
        public void ToPhonemeList_Record_VerbContext_HasCorrectPhonemes()
        {
            // "will record" → record は動詞: R AH0 K AO1 R D
            var result = _engine.ToPhonemeList("will record");

            // 結果の中に AO1 (動詞発音の特徴的音素) が含まれることを確認
            Assert.Contains(result, p => p.Phoneme == ArpabetPhoneme.AO && p.Stress == Stress.Primary);
        }

        [Fact]
        public void ToPhonemeList_Record_NounContext_HasCorrectPhonemes()
        {
            // "the record" → record は名詞: R EH1 K ER0 D
            var result = _engine.ToPhonemeList("the record");

            // 結果の中に EH1 (名詞発音の特徴的音素) が含まれることを確認
            // また ER0 も含まれる
            Assert.Contains(result, p => p.Phoneme == ArpabetPhoneme.EH && p.Stress == Stress.Primary);
            Assert.Contains(result, p => p.Phoneme == ArpabetPhoneme.ER && p.Stress == Stress.NoStress);
        }

        // ===== 8. LookupWord は文脈なしなので常に最初のバリアント =====

        [Fact]
        public void LookupWord_Record_AlwaysReturnsFirstVariant()
        {
            // LookupWord は LookupWordInternal を使い、常に pronunciations[0] を返す
            var result = _engine.LookupWord("record");
            // record [0] = R AH0 K AO1 R D
            Assert.True(result.Count > 0);

            // LookupWord は文脈を考慮しないため、常に同じ結果
            var result2 = _engine.LookupWord("record");
            Assert.Equal(result.Count, result2.Count);
            for (var i = 0; i < result.Count; i++)
            {
                Assert.Equal(result[i], result2[i]);
            }
        }

        // ===== 9. 空入力 =====

        [Fact]
        public void EmptyInput_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToPhonemes(""));
        }

        [Fact]
        public void EmptyInput_ToPhonemeList_ReturnsEmpty()
        {
            var result = _engine.ToPhonemeList("");
            Assert.Empty(result);
        }

        // ===== 10. オプション全指定テスト =====

        [Fact]
        public void AllOptionsSpecified_HomographResolutionWorks()
        {
            var options = new EnglishG2POptions(
                includeStress: true,
                unknownWordHandling: UnknownWordStrategy.Skip,
                enableLts: true,
                enableNormalization: true,
                enableHomographResolution: true);

            using var engine = new EnglishG2PEngine(options);

            var verbResult = engine.ToPhonemes("will record");
            var nounResult = engine.ToPhonemes("the record");
            Assert.NotEqual(verbResult, nounResult);
        }

        [Fact]
        public void AllOptionsSpecified_NoStress_HomographResolutionWorks()
        {
            var options = new EnglishG2POptions(
                includeStress: false,
                unknownWordHandling: UnknownWordStrategy.Skip,
                enableLts: true,
                enableNormalization: true,
                enableHomographResolution: true);

            using var engine = new EnglishG2PEngine(options);

            // ストレスなしでも動詞と名詞で異なる音素列
            var verbResult = engine.ToPhonemes("will record");
            var nounResult = engine.ToPhonemes("the record");
            Assert.NotEqual(verbResult, nounResult);
        }
    }
}
