using BenchmarkDotNet.Attributes;
using DotNetG2P.Multilingual;

namespace DotNetG2P.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class MultilingualBenchmarks
{
    private MultilingualG2PEngine _engine = null!;
    private string _dictionaryPath = null!;

    [GlobalSetup]
    public void Setup()
    {
        _dictionaryPath = BenchmarkDictionaryPathResolver.ResolveJapaneseDictionary();
        _engine = new MultilingualG2PEngine(_dictionaryPath);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _engine.Dispose();
    }

    [Benchmark]
    public string ToPhonemes_MixedSentence()
    {
        return _engine.ToPhonemes(BenchmarkInputs.MultilingualSentence);
    }

    [Benchmark]
    public IReadOnlyList<G2PSegment> ToSegments_MixedSentence()
    {
        return _engine.ToSegments(BenchmarkInputs.MultilingualSentence);
    }

    [Benchmark]
    public IReadOnlyList<string> ToPhonemes_Batch()
    {
        return _engine.ToPhonemesBatch(BenchmarkInputs.MultilingualBatch);
    }

    [Benchmark]
    public string ColdStart_MixedSentence()
    {
        using var engine = new MultilingualG2PEngine(_dictionaryPath);
        return engine.ToPhonemes(BenchmarkInputs.MultilingualSentence);
    }
}
