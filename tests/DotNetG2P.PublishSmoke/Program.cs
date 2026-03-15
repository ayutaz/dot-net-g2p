using System;
using System.IO;
using DotNetG2P;
using DotNetG2P.Chinese;
using DotNetG2P.English;
using DotNetG2P.French;
using DotNetG2P.Korean;
using DotNetG2P.MeCab;
using DotNetG2P.Multilingual;
using DotNetG2P.PhonemeConverter;
using DotNetG2P.Portuguese;
using DotNetG2P.Spanish;

static void Ensure(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static string EnsureNonEmpty(string name, string value)
{
    Ensure(!string.IsNullOrWhiteSpace(value), $"{name} returned an empty result.");
    Console.WriteLine($"{name}: {value}");
    return value;
}

static bool IsValidDictionaryDirectory(string path)
{
    if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
    {
        return false;
    }

    return File.Exists(Path.Combine(path, "sys.dic"))
        && File.Exists(Path.Combine(path, "matrix.bin"))
        && File.Exists(Path.Combine(path, "char.bin"))
        && File.Exists(Path.Combine(path, "unk.dic"));
}

static string? ResolveDictionaryPath(string[] args)
{
    if (args.Length > 0 && IsValidDictionaryDirectory(args[0]))
    {
        return args[0];
    }

    var dotNetG2PPath = Environment.GetEnvironmentVariable("DOTNETG2P_NAIST_JDIC_PATH");
    if (!string.IsNullOrWhiteSpace(dotNetG2PPath) && IsValidDictionaryDirectory(dotNetG2PPath))
    {
        return dotNetG2PPath;
    }

    var naistJdicPath = Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
    if (!string.IsNullOrWhiteSpace(naistJdicPath) && IsValidDictionaryDirectory(naistJdicPath))
    {
        return naistJdicPath;
    }

    if (NaistJdicLocator.TryResolve(out var resolvedPath) && resolvedPath != null && IsValidDictionaryDirectory(resolvedPath))
    {
        return resolvedPath;
    }

    return null;
}

Console.WriteLine("DotNetG2P publish smoke validation");

var moraPhonemes = EnsureNonEmpty(
    "Core.MoraMapping",
    MoraMapping.MorasToPhonemeString(MoraMapping.KatakanaToMoras("コンニチワ")));
Ensure(moraPhonemes != "コンニチワ", "Core.MoraMapping returned the original input.");

using (var english = new EnglishG2PEngine())
{
    EnsureNonEmpty("English.Dictionary", english.ToPhonemes("hello"));
    EnsureNonEmpty("English.Lts", english.ToPhonemes("codexing"));
}

using (var chinese = new ChineseG2PEngine())
{
    EnsureNonEmpty("Chinese.Pinyin", chinese.ToPinyin("你好世界"));
}

using (var korean = new KoreanG2PEngine())
{
    EnsureNonEmpty("Korean.Phonemes", korean.ToPhonemes("안녕하세요"));
}

using (var spanish = new SpanishG2PEngine())
{
    EnsureNonEmpty("Spanish.Phonemes", spanish.ToPhonemes("hola mundo"));
}

using (var french = new FrenchG2PEngine())
{
    EnsureNonEmpty("French.Phonemes", french.ToPhonemes("bonjour le monde"));
}

using (var portuguese = new PortugueseG2PEngine())
{
    EnsureNonEmpty("Portuguese.Phonemes", portuguese.ToPhonemes("ola mundo"));
}

var dictionaryPath = ResolveDictionaryPath(args);
if (dictionaryPath == null)
{
    Console.WriteLine("Japanese/Multilingual smoke skipped: naist-jdic dictionary not found.");
    return;
}

Console.WriteLine($"Japanese dictionary: {dictionaryPath}");

using (var tokenizer = new MeCabTokenizer(dictionaryPath))
using (var japanese = new G2PEngine(tokenizer))
{
    EnsureNonEmpty("Japanese.Phonemes", japanese.ToPhonemes("こんにちは"));
}

using (var multilingual = new MultilingualG2PEngine(dictionaryPath))
{
    EnsureNonEmpty("Multilingual.Phonemes", multilingual.ToPhonemes("hello こんにちは"));
}

Console.WriteLine("DotNetG2P publish smoke validation succeeded.");
