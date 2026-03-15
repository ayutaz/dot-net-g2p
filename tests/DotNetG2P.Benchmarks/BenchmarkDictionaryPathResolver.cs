using System;
using DotNetG2P.MeCab;

namespace DotNetG2P.Benchmarks;

internal static class BenchmarkDictionaryPathResolver
{
    public static string ResolveJapaneseDictionary()
    {
        if (NaistJdicLocator.TryResolve(out var dictionaryPath))
            return dictionaryPath!;

        throw new InvalidOperationException(
            "Japanese and multilingual benchmarks require naist-jdic. " +
            "Set DOTNETG2P_NAIST_JDIC_PATH or NAIST_JDIC_PATH, or install the dictionary under %USERPROFILE%\\naist-jdic.");
    }
}
