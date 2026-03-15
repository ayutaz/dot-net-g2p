using BenchmarkDotNet.Attributes;
using DotNetG2P.MeCab;

namespace DotNetG2P.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class JapaneseBenchmarks
{
    private G2PEngine _engine = null!;
    private string _dictionaryPath = null!;

    [GlobalSetup]
    public void Setup()
    {
        _dictionaryPath = BenchmarkDictionaryPathResolver.ResolveJapaneseDictionary();
        _engine = new G2PEngine(new MeCabTokenizer(_dictionaryPath));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _engine.Dispose();
    }

    [Benchmark]
    public string ToPhonemes_ShortText()
    {
        return _engine.ToPhonemes(BenchmarkInputs.JapaneseShortText);
    }

    [Benchmark]
    public string ToPhonemes_Sentence()
    {
        return _engine.ToPhonemes(BenchmarkInputs.JapaneseSentence);
    }

    [Benchmark]
    public string ToKana_Sentence()
    {
        return _engine.ToKana(BenchmarkInputs.JapaneseSentence);
    }

    [Benchmark]
    public IReadOnlyList<string> ToPhonemes_Batch()
    {
        return _engine.ToPhonemesBatch(BenchmarkInputs.JapaneseBatch);
    }

    [Benchmark]
    public string ColdStart_ShortText()
    {
        using var engine = new G2PEngine(new MeCabTokenizer(_dictionaryPath));
        return engine.ToPhonemes(BenchmarkInputs.JapaneseShortText);
    }
}
