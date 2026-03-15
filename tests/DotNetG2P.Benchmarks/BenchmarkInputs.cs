namespace DotNetG2P.Benchmarks;

internal static class BenchmarkInputs
{
    public const string EnglishWord = "hello";
    public const string EnglishSentence = "The quick brown fox jumps over the lazy dog.";
    public const string ChineseSentence = "今天天气非常好，我们一起去公园散步吧。";
    public const string ChineseShortText = "你好世界";
    public const string JapaneseShortText = "こんにちは世界";
    public const string JapaneseSentence = "今日は音声合成のベンチマーク結果を確認します。";
    public const string KoreanWord = "안녕하세요";
    public const string KoreanSentence = "오늘 날씨가 좋아서 공원에 산책하러 갑니다.";
    public const string MultilingualSentence = "こんにちは DotNetG2P, hello 世界 and bonjour tout le monde.";
    public const string SpanishSentence = "La conversion fonetica mantiene la calidad del audio.";
    public const string FrenchSentence = "Bonjour tout le monde, la synthese vocale reste stable.";
    public const string PortugueseSentence = "O mecanismo fonetico precisa manter a qualidade da fala.";

    public static readonly string[] EnglishBatch =
    {
        "hello",
        "phoneme conversion",
        "benchmarking makes regressions visible",
        "the quick brown fox jumps over the lazy dog",
        "dot net g2p supports multiple languages"
    };

    public static readonly string[] ChineseBatch =
    {
        "你好世界",
        "学习中文很有意思",
        "中华人民共和国",
        "今天天气非常好",
        "机器学习改变世界"
    };

    public static readonly string[] JapaneseBatch =
    {
        "こんにちは",
        "今日は良い天気です",
        "音声合成の研究を続けます",
        "東京タワーに行きたい",
        "ベンチマークで回帰を確認します"
    };

    public static readonly string[] KoreanBatch =
    {
        "안녕하세요",
        "반갑습니다",
        "오늘 날씨가 좋습니다",
        "한국어 발음을 변환합니다",
        "벤치마크로 성능을 측정합니다"
    };

    public static readonly string[] MultilingualBatch =
    {
        "こんにちは hello",
        "Bonjour 世界",
        "Hola DotNetG2P",
        "音声 synthesis 테스트",
        "Ola mundo and 今日は晴れです"
    };

    public static readonly string[] SpanishBatch =
    {
        "hola mundo",
        "la fonetica ayuda a la sintesis",
        "las reglas deben seguir siendo estables",
        "los lotes muestran la latencia real",
        "el benchmark detecta regresiones"
    };

    public static readonly string[] FrenchBatch =
    {
        "bonjour le monde",
        "la synthese vocale doit rester fiable",
        "les benchmarks montrent les regressions",
        "chaque lot mesure la stabilite",
        "les regles phonologiques evoluent"
    };

    public static readonly string[] PortugueseBatch =
    {
        "ola mundo",
        "a sintese de fala precisa ser estavel",
        "os benchmarks mostram regressos",
        "cada lote mede a latencia",
        "as regras foneticas continuam previsiveis"
    };
}
