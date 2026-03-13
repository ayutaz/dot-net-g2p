using BenchmarkDotNet.Attributes;
using DotNetG2P.Korean;

namespace DotNetG2P.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class KoreanBenchmarks
{
    private KoreanG2PEngine _engine = null!;

    [GlobalSetup]
    public void Setup()
    {
        _engine = new KoreanG2PEngine();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _engine.Dispose();
    }

    [Benchmark]
    public string ToPhonemes_Word()
    {
        return _engine.ToPhonemes(BenchmarkInputs.KoreanWord);
    }

    [Benchmark]
    public string ToPhonemes_Sentence()
    {
        return _engine.ToPhonemes(BenchmarkInputs.KoreanSentence);
    }

    [Benchmark]
    public string ToJamo_Sentence()
    {
        return _engine.ToJamo(BenchmarkInputs.KoreanSentence);
    }

    [Benchmark]
    public IReadOnlyList<string> ToPhonemes_Batch()
    {
        return _engine.ToPhonemesBatch(BenchmarkInputs.KoreanBatch);
    }
}
