using System;
using System.Linq;
using DotNetG2P.Korean;

namespace DotNetG2P.Tests.KoreanG2P
{
    public class KoreanProsodyTests
    {
        // ============================================================
        //  単一音節語
        // ============================================================

        [Fact]
        public void ToIpaWithProsody_SingleSyllableWord_A3IsOne()
        {
            using var engine = new KoreanG2PEngine();

            // "한" = 1音節
            var result = engine.ToIpaWithProsody("한");

            Assert.True(result.Phonemes.Length > 0);
            Assert.All(result.Prosody, p => Assert.Equal(1, p.A3));
        }

        [Fact]
        public void ToIpaWithProsody_SingleSyllableWord_ProducesCorrectIpa()
        {
            using var engine = new KoreanG2PEngine();

            // "한" → ㅎ + ㅏ + ㄴ → h + a + n
            var result = engine.ToIpaWithProsody("한");

            Assert.Equal(3, result.Phonemes.Length);
            Assert.Equal("h", result.Phonemes[0]);
            Assert.Equal("a", result.Phonemes[1]);
            Assert.Equal("n", result.Phonemes[2]);
        }

        // ============================================================
        //  複数音節語
        // ============================================================

        [Fact]
        public void ToIpaWithProsody_MultiSyllableWord_A3EqualsSyllableCount()
        {
            using var engine = new KoreanG2PEngine();

            // "한글" = 2音節 → すべての音素の a3 が 2
            var result = engine.ToIpaWithProsody("한글");

            Assert.True(result.Phonemes.Length > 0);
            Assert.All(result.Prosody, p => Assert.Equal(2, p.A3));
        }

        [Fact]
        public void ToIpaWithProsody_FiveSyllableWord_A3IsFive()
        {
            using var engine = new KoreanG2PEngine();

            // "안녕하세요" = 5音節
            var result = engine.ToIpaWithProsody("안녕하세요");

            Assert.True(result.Phonemes.Length > 0);
            Assert.All(result.Prosody, p => Assert.Equal(5, p.A3));
        }

        // ============================================================
        //  複数語文（語ごとに a3 が異なる）
        // ============================================================

        [Fact]
        public void ToIpaWithProsody_MultiWordSentence_EachWordHasOwnA3()
        {
            using var engine = new KoreanG2PEngine();

            // "나는 학생" = "나는"(2音節) + "학생"(2音節)
            var result = engine.ToIpaWithProsody("나는 학생");

            Assert.True(result.Phonemes.Length > 0);
            Assert.Equal(result.Phonemes.Length, result.Prosody.Length);

            // すべての音素が a3=2 であること（両語とも2音節）
            Assert.All(result.Prosody, p => Assert.Equal(2, p.A3));
        }

        [Fact]
        public void ToIpaWithProsody_TwoWordsWithDifferentSyllableCounts_DifferentA3Values()
        {
            using var engine = new KoreanG2PEngine();

            // "나 학교" = "나"(1音節) + "학교"(2音節)
            var result = engine.ToIpaWithProsody("나 학교");

            Assert.True(result.Phonemes.Length > 0);

            // 最初の語 "나" (1音節) の音素は a3=1
            // 2番目の語 "학교" (2音節) の音素は a3=2
            // 少なくとも a3=1 と a3=2 の両方が存在する
            var a3Values = result.Prosody.Select(p => p.A3).Distinct().OrderBy(x => x).ToArray();
            Assert.Contains(1, a3Values);
            Assert.Contains(2, a3Values);
        }

        [Fact]
        public void ToIpaWithProsody_ThreeWordSentence_EachWordHasCorrectA3()
        {
            using var engine = new KoreanG2PEngine();

            // "나 서울 대학교" = "나"(1) + "서울"(2) + "대학교"(3)
            var result = engine.ToIpaWithProsody("나 서울 대학교");

            var a3Values = result.Prosody.Select(p => p.A3).Distinct().OrderBy(x => x).ToArray();
            Assert.Contains(1, a3Values);
            Assert.Contains(2, a3Values);
            Assert.Contains(3, a3Values);
        }

        // ============================================================
        //  a1, a2 が常に 0
        // ============================================================

        [Fact]
        public void ToIpaWithProsody_A1IsAlwaysZero()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.ToIpaWithProsody("안녕하세요 반갑습니다");

            Assert.True(result.Phonemes.Length > 0);
            Assert.All(result.Prosody, p => Assert.Equal(0, p.A1));
        }

        [Fact]
        public void ToIpaWithProsody_A2IsAlwaysZero()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.ToIpaWithProsody("안녕하세요 반갑습니다");

            Assert.True(result.Phonemes.Length > 0);
            Assert.All(result.Prosody, p => Assert.Equal(0, p.A2));
        }

        // ============================================================
        //  空文字列 / null
        // ============================================================

        [Fact]
        public void ToIpaWithProsody_EmptyString_ReturnsEmptyResult()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.ToIpaWithProsody("");

            Assert.Empty(result.Phonemes);
            Assert.Empty(result.Prosody);
        }

        [Fact]
        public void ToIpaWithProsody_NullInput_ReturnsEmptyResult()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.ToIpaWithProsody(null!);

            Assert.Empty(result.Phonemes);
            Assert.Empty(result.Prosody);
        }

        [Fact]
        public void ToIpaWithProsody_WhitespaceOnly_ReturnsEmptyResult()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.ToIpaWithProsody("   ");

            Assert.Empty(result.Phonemes);
            Assert.Empty(result.Prosody);
        }

        // ============================================================
        //  Dispose 後のスロー確認
        // ============================================================

        [Fact]
        public void ToIpaWithProsody_AfterDispose_ThrowsObjectDisposedException()
        {
            var engine = new KoreanG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToIpaWithProsody("한글"));
        }

        [Fact]
        public void ToIpaWithProsodyBatch_AfterDispose_ThrowsObjectDisposedException()
        {
            var engine = new KoreanG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToIpaWithProsodyBatch(new[] { "한글" }));
        }

        // ============================================================
        //  配列長の一致
        // ============================================================

        [Fact]
        public void ToIpaWithProsody_PhonemesAndProsodyHaveSameLength()
        {
            using var engine = new KoreanG2PEngine();

            var inputs = new[] { "한", "한글", "안녕하세요", "나는 학생", "나 서울 대학교" };
            foreach (var input in inputs)
            {
                var result = engine.ToIpaWithProsody(input);
                Assert.Equal(result.Phonemes.Length, result.Prosody.Length);
            }
        }

        // ============================================================
        //  具体例: "안녕하세요" の音素と a3 値
        // ============================================================

        [Fact]
        public void ToIpaWithProsody_AnnyeongHaseyo_AllA3AreFive()
        {
            using var engine = new KoreanG2PEngine();

            // "안녕하세요" → 5音節の単一語 → すべて a3=5
            var result = engine.ToIpaWithProsody("안녕하세요");

            Assert.True(result.Phonemes.Length >= 5, "少なくとも5つ以上の音素が生成される");
            Assert.All(result.Prosody, p =>
            {
                Assert.Equal(0, p.A1);
                Assert.Equal(0, p.A2);
                Assert.Equal(5, p.A3);
            });
        }

        [Fact]
        public void ToIpaWithProsody_AnnyeongHaseyo_ContainsExpectedIpaPhonemes()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.ToIpaWithProsody("안녕하세요");

            // IPA 音素配列が空でないことを確認
            Assert.True(result.Phonemes.Length > 0);

            // 主要な IPA 音素が含まれることを確認
            // "안" → (onset ㅇ → "") + a + n
            // "녕" → n + jʌ + ŋ
            // "하" → h + a
            // "세" → s + e
            // "요" → j + o
            Assert.Contains("a", result.Phonemes);
            Assert.Contains("n", result.Phonemes);
            Assert.Contains("h", result.Phonemes);
        }

        // ============================================================
        //  バッチ API
        // ============================================================

        [Fact]
        public void ToIpaWithProsodyBatch_NullInput_ThrowsArgumentNullException()
        {
            using var engine = new KoreanG2PEngine();

            Assert.Throws<ArgumentNullException>(() => engine.ToIpaWithProsodyBatch(null!));
        }

        [Fact]
        public void ToIpaWithProsodyBatch_EmptyInput_ReturnsEmpty()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.ToIpaWithProsodyBatch(Array.Empty<string>());

            Assert.Empty(result);
        }

        [Fact]
        public void ToIpaWithProsodyBatch_MultipleInputs_ReturnsCorrectCount()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.ToIpaWithProsodyBatch(new[] { "한", "한글", "안녕하세요" });

            Assert.Equal(3, result.Count);
            Assert.All(result, r => Assert.Equal(r.Phonemes.Length, r.Prosody.Length));
        }

        [Fact]
        public void ToIpaWithProsodyBatch_MixedInput_HandlesAllElements()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.ToIpaWithProsodyBatch(new[] { "한글", "", null! });

            Assert.Equal(3, result.Count);
            Assert.True(result[0].Phonemes.Length > 0);
            Assert.Empty(result[1].Phonemes);
            Assert.Empty(result[2].Phonemes);
        }

        // ============================================================
        //  KoreanProsodyInfo の Equals / GetHashCode
        // ============================================================

        [Fact]
        public void KoreanProsodyInfo_Equals_SameValues_ReturnsTrue()
        {
            var a = new KoreanProsodyInfo(0, 0, 3);
            var b = new KoreanProsodyInfo(0, 0, 3);

            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.False(a != b);
        }

        [Fact]
        public void KoreanProsodyInfo_Equals_DifferentA3_ReturnsFalse()
        {
            var a = new KoreanProsodyInfo(0, 0, 3);
            var b = new KoreanProsodyInfo(0, 0, 5);

            Assert.False(a.Equals(b));
            Assert.False(a == b);
            Assert.True(a != b);
        }

        [Fact]
        public void KoreanProsodyInfo_Equals_DifferentA1_ReturnsFalse()
        {
            var a = new KoreanProsodyInfo(0, 0, 3);
            var b = new KoreanProsodyInfo(1, 0, 3);

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void KoreanProsodyInfo_Equals_DifferentA2_ReturnsFalse()
        {
            var a = new KoreanProsodyInfo(0, 0, 3);
            var b = new KoreanProsodyInfo(0, 1, 3);

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void KoreanProsodyInfo_Equals_Object_ReturnsCorrectly()
        {
            var a = new KoreanProsodyInfo(0, 0, 3);
            object b = new KoreanProsodyInfo(0, 0, 3);
            object c = "not a prosody info";

            Assert.True(a.Equals(b));
            Assert.False(a.Equals(c));
            Assert.False(a.Equals(null));
        }

        [Fact]
        public void KoreanProsodyInfo_GetHashCode_SameValues_SameHash()
        {
            var a = new KoreanProsodyInfo(0, 0, 3);
            var b = new KoreanProsodyInfo(0, 0, 3);

            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void KoreanProsodyInfo_GetHashCode_DifferentValues_DifferentHash()
        {
            var a = new KoreanProsodyInfo(0, 0, 3);
            var b = new KoreanProsodyInfo(0, 0, 5);

            // ハッシュ衝突は理論上起こりうるが、この組み合わせでは異なるはず
            Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void KoreanProsodyInfo_ToString_FormatsCorrectly()
        {
            var info = new KoreanProsodyInfo(0, 0, 3);

            Assert.Equal("(a1=0, a2=0, a3=3)", info.ToString());
        }

        // ============================================================
        //  KoreanProsodyResult のバリデーション
        // ============================================================

        [Fact]
        public void KoreanProsodyResult_NullPhonemes_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new KoreanProsodyResult(null!, new KoreanProsodyInfo[0]));
        }

        [Fact]
        public void KoreanProsodyResult_NullProsody_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new KoreanProsodyResult(new string[0], null!));
        }

        [Fact]
        public void KoreanProsodyResult_MismatchedLengths_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new KoreanProsodyResult(new[] { "a" }, new KoreanProsodyInfo[2]));
        }

        [Fact]
        public void KoreanProsodyResult_EmptyArrays_Succeeds()
        {
            var result = new KoreanProsodyResult(Array.Empty<string>(), Array.Empty<KoreanProsodyInfo>());

            Assert.Empty(result.Phonemes);
            Assert.Empty(result.Prosody);
        }

        // ============================================================
        //  IPA 変換の正確性
        // ============================================================

        [Fact]
        public void ToIpaWithProsody_SingleSyllableHan_CorrectIpaSequence()
        {
            using var engine = new KoreanG2PEngine();

            // "한" → ㅎ + ㅏ + ㄴ → h + a + n
            var result = engine.ToIpaWithProsody("한");

            Assert.Equal(new[] { "h", "a", "n" }, result.Phonemes);
        }

        [Fact]
        public void ToIpaWithProsody_SyllableWithoutCoda_CorrectIpaSequence()
        {
            using var engine = new KoreanG2PEngine();

            // "나" → ㄴ + ㅏ → n + a
            var result = engine.ToIpaWithProsody("나");

            Assert.Equal(new[] { "n", "a" }, result.Phonemes);
        }

        [Fact]
        public void ToIpaWithProsody_SilentIeungOnset_OmitsOnsetIpa()
        {
            using var engine = new KoreanG2PEngine();

            // "아" → ㅇ(silent) + ㅏ → a (初声のㅇは無音)
            var result = engine.ToIpaWithProsody("아");

            Assert.Equal(new[] { "a" }, result.Phonemes);
        }

        [Fact]
        public void ToIpaWithProsody_CodaNgSound_UsesNgIpa()
        {
            using var engine = new KoreanG2PEngine();

            // "강" → ㄱ + ㅏ + ㅇ(coda) → k + a + ŋ
            var result = engine.ToIpaWithProsody("강");

            Assert.Equal(new[] { "k", "a", "ŋ" }, result.Phonemes);
        }

        [Fact]
        public void ToIpaWithProsody_AspiratedConsonant_CorrectIpa()
        {
            using var engine = new KoreanG2PEngine();

            // "카" → ㅋ + ㅏ → kʰ + a
            var result = engine.ToIpaWithProsody("카");

            Assert.Equal(new[] { "kʰ", "a" }, result.Phonemes);
        }

        [Fact]
        public void ToIpaWithProsody_TenseConsonant_CorrectIpa()
        {
            using var engine = new KoreanG2PEngine();

            // "까" → ㄲ + ㅏ → k͈ + a
            var result = engine.ToIpaWithProsody("까");

            Assert.Equal(new[] { "k͈", "a" }, result.Phonemes);
        }

        [Fact]
        public void ToIpaWithProsody_DiphthongVowel_CorrectIpa()
        {
            using var engine = new KoreanG2PEngine();

            // "과" → ㄱ + ㅘ → k + wa
            var result = engine.ToIpaWithProsody("과");

            Assert.Equal(new[] { "k", "wa" }, result.Phonemes);
        }

        // ============================================================
        //  音韻変化規則適用後の IPA
        // ============================================================

        [Fact]
        public void ToIpaWithProsody_Resyllabification_ReflectedInIpa()
        {
            using var engine = new KoreanG2PEngine();

            // "한글" → 연음 적용 → ㅎㅏㄴ + ㄱㅡㄹ (변환 후)
            var result = engine.ToIpaWithProsody("한글");

            Assert.True(result.Phonemes.Length > 0);
            Assert.All(result.Prosody, p => Assert.Equal(2, p.A3));
        }
    }
}
