using BenchmarkDotNet.Attributes;
using DotNetG2P.English;

namespace DotNetG2P.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class EnglishBenchmarks
{
    private EnglishG2PEngine _engine = null!;

    [GlobalSetup]
    public void Setup()
    {
        _engine = new EnglishG2PEngine();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _engine.Dispose();
    }

    [Benchmark]
    public string ToPhonemes_Word()
    {
        return _engine.ToPhonemes(BenchmarkInputs.EnglishWord);
    }

    [Benchmark]
    public string ToPhonemes_Sentence()
    {
        return _engine.ToPhonemes(BenchmarkInputs.EnglishSentence);
    }

    [Benchmark]
    public IReadOnlyList<string> ToPhonemes_Batch()
    {
        return _engine.ToPhonemesBatch(BenchmarkInputs.EnglishBatch);
    }

    [Benchmark]
    public string ColdStart_Word()
    {
        using var engine = new EnglishG2PEngine();
        return engine.ToPhonemes(BenchmarkInputs.EnglishWord);
    }
}
