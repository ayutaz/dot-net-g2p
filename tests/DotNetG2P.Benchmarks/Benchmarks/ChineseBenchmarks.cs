using BenchmarkDotNet.Attributes;
using DotNetG2P.Chinese;

namespace DotNetG2P.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class ChineseBenchmarks
{
    private ChineseG2PEngine _engine = null!;

    [GlobalSetup]
    public void Setup()
    {
        _engine = new ChineseG2PEngine();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _engine.Dispose();
    }

    [Benchmark]
    public string ToPinyin_ShortText()
    {
        return _engine.ToPinyin(BenchmarkInputs.ChineseShortText);
    }

    [Benchmark]
    public string ToPinyin_Sentence()
    {
        return _engine.ToPinyin(BenchmarkInputs.ChineseSentence);
    }

    [Benchmark]
    public string[] ToPinyinList_ShortText()
    {
        return _engine.ToPinyinList(BenchmarkInputs.ChineseShortText);
    }

    [Benchmark]
    public IReadOnlyList<string> ToPinyin_Batch()
    {
        return _engine.ToPinyinBatch(BenchmarkInputs.ChineseBatch);
    }
}
