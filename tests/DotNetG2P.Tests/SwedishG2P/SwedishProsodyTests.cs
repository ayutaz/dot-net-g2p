using System;
using System.Linq;
using DotNetG2P.Swedish;
using Xunit;

namespace DotNetG2P.Tests.SwedishG2P
{
    public class SwedishProsodyTests : IDisposable
    {
        private readonly SwedishG2PEngine _centralEngine = new SwedishG2PEngine();
        private readonly SwedishG2PEngine _finlandEngine = new SwedishG2PEngine(
            new SwedishG2POptions(dialect: SwedishDialect.FinlandSwedish));

        public void Dispose()
        {
            _centralEngine.Dispose();
            _finlandEngine.Dispose();
        }

        // =================================================================
        // A1: ピッチアクセント
        // =================================================================

        [Fact]
        public void A1_単音節語_Accent1()
        {
            // "hej" は単音節語 → accent 1
            var result = _centralEngine.ToIpaWithProsody("hej");

            Assert.True(result.Phonemes.Length > 0);
            Assert.All(result.Prosody, p => Assert.Equal(1, p.A1));
        }

        [Fact]
        public void A1_Accent2接尾辞語()
        {
            // "hundar" (複数形, -ar接尾辞) は accent 2 → A1=2 であること
            var result = _centralEngine.ToIpaWithProsody("hundar");

            Assert.True(result.Phonemes.Length > 0);
            Assert.All(result.Prosody, p => Assert.Equal(2, p.A1));
        }

        [Fact]
        public void A1_FinlandSwedish_値は0()
        {
            // FinlandSwedish ではピッチアクセント無効化 → A1=0
            var result = _finlandEngine.ToIpaWithProsody("hundar");

            Assert.True(result.Phonemes.Length > 0);
            Assert.All(result.Prosody, p => Assert.Equal(0, p.A1));
        }

        // =================================================================
        // A2: ストレスレベル
        // =================================================================

        [Fact]
        public void A2_ストレスあり_値は1()
        {
            // "hej" は内容語 → ストレスあり → A2=1
            var result = _centralEngine.ToIpaWithProsody("hej");

            Assert.True(result.Phonemes.Length > 0);
            Assert.All(result.Prosody, p => Assert.Equal(1, p.A2));
        }

        [Fact]
        public void A2_機能語_ストレスなし_値は0()
        {
            // "och" は機能語 → WithoutStress() → StressedSyllableIndex=-1 → A2=0
            var result = _centralEngine.ToIpaWithProsody("och");

            Assert.True(result.Phonemes.Length > 0);
            Assert.All(result.Prosody, p => Assert.Equal(0, p.A2));
        }

        // =================================================================
        // A3: 音節数
        // =================================================================

        [Fact]
        public void A3_単音節語_値は1()
        {
            // "hej" は1音節語 → A3=1
            var result = _centralEngine.ToIpaWithProsody("hej");

            Assert.True(result.Phonemes.Length > 0);
            Assert.All(result.Prosody, p => Assert.Equal(1, p.A3));
        }

        [Fact]
        public void A3_2音節語_値は2()
        {
            // "huset" は2音節語 → A3=2
            var result = _centralEngine.ToIpaWithProsody("huset");

            Assert.True(result.Phonemes.Length > 0);
            Assert.All(result.Prosody, p => Assert.Equal(2, p.A3));
        }

        [Fact]
        public void A3_3音節語_値は3()
        {
            // "flickorna" は3音節語 → A3=3
            var result = _centralEngine.ToIpaWithProsody("flickorna");

            Assert.True(result.Phonemes.Length > 0);
            Assert.All(result.Prosody, p => Assert.Equal(3, p.A3));
        }

        // =================================================================
        // ToIpaWithProsody — 全般
        // =================================================================

        [Fact]
        public void ToIpaWithProsody_IPA文字列が正しい()
        {
            // "hej" → IPA: h + eː + j (ストレスなし形式の音素)
            var result = _centralEngine.ToIpaWithProsody("hej");

            Assert.True(result.Phonemes.Length > 0);
            // 音素に "h" が含まれる
            Assert.Contains("h", result.Phonemes);
        }

        [Fact]
        public void ToIpaWithProsody_ProsodyInfo配列長_音素数と一致()
        {
            var inputs = new[] { "hej", "huset", "flickorna", "jag har" };
            foreach (var input in inputs)
            {
                var result = _centralEngine.ToIpaWithProsody(input);
                Assert.Equal(result.Phonemes.Length, result.Prosody.Length);
            }
        }

        [Fact]
        public void ToIpaWithProsody_空文字_空結果()
        {
            var result = _centralEngine.ToIpaWithProsody("");
            Assert.Empty(result.Phonemes);
            Assert.Empty(result.Prosody);

            var resultNull = _centralEngine.ToIpaWithProsody(null!);
            Assert.Empty(resultNull.Phonemes);
            Assert.Empty(resultNull.Prosody);
        }

        [Fact]
        public void ToIpaWithProsodyBatch_複数テキスト()
        {
            var result = _centralEngine.ToIpaWithProsodyBatch(new[] { "hej", "huset", "" });

            Assert.Equal(3, result.Count);
            Assert.True(result[0].Phonemes.Length > 0);
            Assert.True(result[1].Phonemes.Length > 0);
            Assert.Empty(result[2].Phonemes);
        }

        [Fact]
        public void ToIpaWithProsody_Dispose後_例外()
        {
            var engine = new SwedishG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToIpaWithProsody("hej"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToIpaWithProsodyBatch(new[] { "hej" }));
        }

        // =================================================================
        // SwedishProsodyInfo — Equals / GetHashCode
        // =================================================================

        [Fact]
        public void ProsodyInfo_Equals_同値_true()
        {
            var a = new SwedishProsodyInfo(1, 1, 2);
            var b = new SwedishProsodyInfo(1, 1, 2);

            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.False(a != b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void ProsodyInfo_Equals_異値_false()
        {
            var a = new SwedishProsodyInfo(1, 1, 2);
            var b = new SwedishProsodyInfo(2, 0, 3);

            Assert.False(a.Equals(b));
            Assert.False(a == b);
            Assert.True(a != b);

            // A1 のみ異なる
            Assert.False(new SwedishProsodyInfo(1, 0, 2).Equals(new SwedishProsodyInfo(2, 0, 2)));
            // A2 のみ異なる
            Assert.False(new SwedishProsodyInfo(1, 0, 2).Equals(new SwedishProsodyInfo(1, 1, 2)));
            // A3 のみ異なる
            Assert.False(new SwedishProsodyInfo(1, 0, 2).Equals(new SwedishProsodyInfo(1, 0, 3)));

            // object型
            Assert.True(a.Equals((object)new SwedishProsodyInfo(1, 1, 2)));
            Assert.False(a.Equals((object)"not a prosody info"));
            Assert.False(a.Equals(null));

            // ToString
            Assert.Equal("(a1=1, a2=1, a3=2)", a.ToString());
        }
    }
}
