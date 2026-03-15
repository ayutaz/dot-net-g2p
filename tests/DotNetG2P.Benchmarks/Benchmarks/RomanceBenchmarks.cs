using BenchmarkDotNet.Attributes;
using DotNetG2P.French;
using DotNetG2P.Portuguese;
using DotNetG2P.Spanish;

namespace DotNetG2P.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class RomanceBenchmarks
{
    private SpanishG2PEngine _spanish = null!;
    private FrenchG2PEngine _french = null!;
    private PortugueseG2PEngine _portuguese = null!;

    [GlobalSetup]
    public void Setup()
    {
        _spanish = new SpanishG2PEngine();
        _french = new FrenchG2PEngine();
        _portuguese = new PortugueseG2PEngine();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _spanish.Dispose();
        _french.Dispose();
        _portuguese.Dispose();
    }

    [Benchmark]
    public string Spanish_ToPhonemes_Sentence()
    {
        return _spanish.ToPhonemes(BenchmarkInputs.SpanishSentence);
    }

    [Benchmark]
    public IReadOnlyList<string> Spanish_ToPhonemes_Batch()
    {
        return _spanish.ToPhonemesBatch(BenchmarkInputs.SpanishBatch);
    }

    [Benchmark]
    public string French_ToIPA_Sentence()
    {
        return _french.ToIPA(BenchmarkInputs.FrenchSentence);
    }

    [Benchmark]
    public IReadOnlyList<string> French_ToPhonemes_Batch()
    {
        return _french.ToPhonemesBatch(BenchmarkInputs.FrenchBatch);
    }

    [Benchmark]
    public string Portuguese_ToXSampa_Sentence()
    {
        return _portuguese.ToXSampa(BenchmarkInputs.PortugueseSentence);
    }

    [Benchmark]
    public IReadOnlyList<string> Portuguese_ToPhonemes_Batch()
    {
        return _portuguese.ToPhonemesBatch(BenchmarkInputs.PortugueseBatch);
    }
}
