using DotNetG2P;
using DotNetG2P.Chinese;
using DotNetG2P.English;
using DotNetG2P.French;
using DotNetG2P.Korean;
using DotNetG2P.MeCab;
using DotNetG2P.Multilingual;
using DotNetG2P.Portuguese;
using DotNetG2P.Spanish;

var dictionaryPath = ResolveDictionaryPath(args);

PrintSection("Core MoraMapping");
foreach (var kana in new[] { "コンニチワ", "オハヨウゴザイマス", "アリガトウ", "セカイ" })
{
    var moras = DotNetG2P.PhonemeConverter.MoraMapping.KatakanaToMoras(kana);
    Console.WriteLine($"{kana} -> {DotNetG2P.PhonemeConverter.MoraMapping.MorasToPhonemeString(moras)}");
}

PrintSection("Standalone Language Engines");

using (var english = new EnglishG2PEngine())
{
    Console.WriteLine($"English phonemes: {english.ToPhonemes("benchmarking makes regressions visible")}");
    Console.WriteLine($"English IPA:      {english.ToIPA("hello world")}");
}

using (var chinese = new ChineseG2PEngine())
{
    Console.WriteLine($"Chinese pinyin:   {chinese.ToPinyin("你好世界")}");
    Console.WriteLine($"Chinese zhuyin:   {chinese.ToZhuyin("你好世界")}");
}

using (var korean = new KoreanG2PEngine())
{
    Console.WriteLine($"Korean phonemes:  {korean.ToPhonemes("안녕하세요")}");
    Console.WriteLine($"Korean jamo:      {korean.ToJamo("안녕하세요")}");
}

using (var spanish = new SpanishG2PEngine())
{
    Console.WriteLine($"Spanish phonemes: {spanish.ToPhonemes("hola mundo")}");
    Console.WriteLine($"Spanish IPA:      {spanish.ToIPA("la fonetica ayuda")}");
}

using (var french = new FrenchG2PEngine())
{
    Console.WriteLine($"French phonemes:  {french.ToPhonemes("bonjour le monde")}");
    Console.WriteLine($"French X-SAMPA:   {french.ToXSampa("synthese vocale")}");
}

using (var portuguese = new PortugueseG2PEngine())
{
    Console.WriteLine($"Portuguese phonemes: {portuguese.ToPhonemes("ola mundo")}");
    Console.WriteLine($"Portuguese X-SAMPA:  {portuguese.ToXSampa("sintese de fala")}");
}

if (dictionaryPath is null)
{
    PrintSection("Japanese And Multilingual Samples");
    Console.WriteLine("naist-jdic was not found.");
    Console.WriteLine("Pass a dictionary path as the first argument or set DOTNETG2P_NAIST_JDIC_PATH / NAIST_JDIC_PATH.");
    Console.WriteLine("Install helper: pwsh -File tools/install_naist_jdic.ps1");
    return;
}

PrintSection("Japanese Engine");
using (var engine = new G2PEngine(new MeCabTokenizer(dictionaryPath)))
{
    var text = "東京タワーに行きたい";
    Console.WriteLine($"Input:            {text}");
    Console.WriteLine($"Kana:             {engine.ToKana(text)}");
    Console.WriteLine($"Phonemes:         {engine.ToPhonemes(text)}");
    Console.WriteLine($"Prosody:          {engine.ToProsody(text)}");
    Console.WriteLine($"Batch phonemes:   {string.Join(" | ", engine.ToPhonemesBatch(new[] { "こんにちは", "今日は良い天気です" }))}");
}

PrintSection("Multilingual Engine");
using (var multilingual = new MultilingualG2PEngine(dictionaryPath))
{
    var text = "こんにちは DotNetG2P, hello 世界 and bonjour tout le monde.";
    Console.WriteLine($"Input:            {text}");
    Console.WriteLine($"Phonemes:         {multilingual.ToPhonemes(text)}");
    Console.WriteLine("Segments:");
    foreach (var segment in multilingual.ToSegments(text))
    {
        Console.WriteLine($"  {segment.Language}: {segment.SourceText} -> {segment.Phonemes}");
    }
}

static string? ResolveDictionaryPath(string[] args)
{
    if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        return args[0];

    return NaistJdicLocator.TryResolve(out var resolvedPath)
        ? resolvedPath
        : null;
}

static void PrintSection(string title)
{
    Console.WriteLine();
    Console.WriteLine($"=== {title} ===");
}
