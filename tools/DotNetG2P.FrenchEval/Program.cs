using System.Globalization;
using System.Text;
using System.Text.Json;
using DotNetG2P.French;

var options = EvalCliOptions.Parse(args);
var repoRoot = RepositoryPaths.ResolveRepoRoot();
var inputRoot = RepositoryPaths.ResolveAgainstRepo(repoRoot, options.InputRoot ?? Path.Combine("artifacts", "french-eval", "corpora"));
var outputRoot = RepositoryPaths.ResolveAgainstRepo(repoRoot, options.OutputRoot ?? Path.Combine("artifacts", "french-eval", "reports", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)));
var thresholdsPath = RepositoryPaths.ResolveAgainstRepo(repoRoot, options.ThresholdsPath ?? Path.Combine("tools", "french_eval_thresholds.json"));
var exceptionMasterPath = Path.Combine(repoRoot, "src", "DotNetG2P.French", "Data", "french_exceptions.master.tsv");

Directory.CreateDirectory(outputRoot);
Directory.CreateDirectory(Path.Combine(outputRoot, "mismatches"));

var datasetDefinitions = DatasetDefinition.CreateSet(options.DatasetSet);
var thresholds = File.Exists(thresholdsPath)
    ? EvalThresholdConfig.Load(thresholdsPath)
    : EvalThresholdConfig.Empty;
var exceptionCategories = File.Exists(exceptionMasterPath)
    ? ExceptionCategoryMap.Load(exceptionMasterPath)
    : ExceptionCategoryMap.Empty;
var profiles = EvalProfile.CreateDefault(options.ProfileNames);

var summaries = new List<EvalSummary>();
var categorySummaries = new List<CategorySummaryRow>();
var failedThresholds = new List<string>();

foreach (var dataset in datasetDefinitions)
{
    var inputPath = Path.Combine(inputRoot, dataset.FileName);
    if (!File.Exists(inputPath))
        throw new FileNotFoundException($"Dataset not found: {inputPath}");

    var rows = File.ReadAllLines(inputPath)
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .Select(ParseRow)
        .ToArray();

    Console.WriteLine($"Evaluating {dataset.Name} ({rows.Length} rows)");

    foreach (var profile in profiles)
    {
        using var engine = new FrenchG2PEngine(new FrenchG2POptions(
            dialect: dataset.Dialect,
            includeStress: false,
            enableAllophones: profile.EnableAllophones,
            enableExceptionDictionary: profile.EnableExceptionDictionary,
            allophoneFeatures: profile.AllophoneFeatures));

        var result = CorpusEvaluator.Evaluate(rows, engine, dataset.SourceKind, dataset.Dialect, exceptionCategories, profile.Name, options.MismatchLimit);
        var threshold = thresholds.TryGetThreshold(dataset.FileName, profile.Name, out var configuredThreshold)
            ? configuredThreshold
            : (double?)null;
        var passed = threshold == null || result.PhonemeErrorRate <= threshold.Value;

        summaries.Add(new EvalSummary(
            dataset.Name,
            dataset.FileName,
            profile.Name,
            dataset.SourceKind.ToString(),
            dataset.Dialect.ToString(),
            result.Cases,
            result.EvaluatedCases,
            result.ExactMatches,
            result.WordErrorRate,
            result.PhonemeErrorRate,
            result.TotalErrors,
            result.TotalReferencePhonemes,
            threshold,
            passed));

        categorySummaries.AddRange(result.CategorySummaries.Select(category => new CategorySummaryRow(
            dataset.Name,
            profile.Name,
            category.Category,
            category.Count,
            category.AverageDistance)));

        WriteMismatchReport(Path.Combine(outputRoot, "mismatches", $"{dataset.Name}__{profile.Name}.tsv"), result.Mismatches);
        Console.WriteLine($"  {profile.Name}: PER={result.PhonemeErrorRate:P2}, WER={result.WordErrorRate:P2}, mismatches={result.Mismatches.Count}, threshold={(threshold?.ToString("P2", CultureInfo.InvariantCulture) ?? "n/a")}");

        if (options.EnforceThresholds && !passed)
            failedThresholds.Add($"{dataset.Name}/{profile.Name}: {result.PhonemeErrorRate:P2} > {threshold:P2}");
    }
}

WriteSummaryReport(Path.Combine(outputRoot, "summary.tsv"), summaries);
WriteCategoryReport(Path.Combine(outputRoot, "category_summary.tsv"), categorySummaries);
File.WriteAllText(Path.Combine(outputRoot, "summary.json"), JsonSerializer.Serialize(summaries, new JsonSerializerOptions { WriteIndented = true }));
File.WriteAllText(Path.Combine(outputRoot, "category_summary.json"), JsonSerializer.Serialize(categorySummaries, new JsonSerializerOptions { WriteIndented = true }));

if (failedThresholds.Count > 0)
{
    foreach (var failed in failedThresholds)
        Console.Error.WriteLine(failed);
    return 1;
}

return 0;

static (string Word, string Reference) ParseRow(string line)
{
    var parts = line.Split('\t');
    if (parts.Length < 2)
        throw new InvalidDataException($"Malformed TSV row: {line}");

    return (parts[0].Trim(), parts[1].Trim());
}

static void WriteSummaryReport(string path, IReadOnlyList<EvalSummary> rows)
{
    var lines = new List<string>(rows.Count + 1)
    {
        "dataset\tfile\tprofile\tsource\tdialect\tcases\tevaluated_cases\texact_matches\tword_error_rate\tphoneme_error_rate\ttotal_errors\treference_phonemes\tthreshold\tpassed"
    };

    foreach (var row in rows)
    {
        lines.Add(string.Join('\t', new[]
        {
            row.Dataset,
            row.FileName,
            row.Profile,
            row.Source,
            row.Dialect,
            row.Cases.ToString(CultureInfo.InvariantCulture),
            row.EvaluatedCases.ToString(CultureInfo.InvariantCulture),
            row.ExactMatches.ToString(CultureInfo.InvariantCulture),
            row.WordErrorRate.ToString("F6", CultureInfo.InvariantCulture),
            row.PhonemeErrorRate.ToString("F6", CultureInfo.InvariantCulture),
            row.TotalErrors.ToString(CultureInfo.InvariantCulture),
            row.TotalReferencePhonemes.ToString(CultureInfo.InvariantCulture),
            row.Threshold?.ToString("F6", CultureInfo.InvariantCulture) ?? string.Empty,
            row.Passed ? "true" : "false",
        }));
    }

    File.WriteAllLines(path, lines, new UTF8Encoding(false));
}

static void WriteCategoryReport(string path, IReadOnlyList<CategorySummaryRow> rows)
{
    var lines = new List<string>(rows.Count + 1)
    {
        "dataset\tprofile\tcategory\tcount\taverage_distance"
    };

    foreach (var row in rows.OrderBy(x => x.Dataset).ThenBy(x => x.Profile).ThenByDescending(x => x.Count).ThenBy(x => x.Category))
    {
        lines.Add(string.Join('\t', new[]
        {
            row.Dataset,
            row.Profile,
            row.Category,
            row.Count.ToString(CultureInfo.InvariantCulture),
            row.AverageDistance.ToString("F6", CultureInfo.InvariantCulture),
        }));
    }

    File.WriteAllLines(path, lines, new UTF8Encoding(false));
}

static void WriteMismatchReport(string path, IReadOnlyList<MismatchRow> rows)
{
    var lines = new List<string>(rows.Count + 1)
    {
        "word\tcategory\tdistance\tpredicted\treference"
    };

    foreach (var row in rows)
    {
        lines.Add(string.Join('\t', new[]
        {
            row.Word,
            row.Category,
            row.Distance.ToString(CultureInfo.InvariantCulture),
            row.Predicted,
            row.Reference,
        }));
    }

    File.WriteAllLines(path, lines, new UTF8Encoding(false));
}

internal sealed record EvalCliOptions
{
    public string? InputRoot { get; private init; }
    public string? OutputRoot { get; private init; }
    public string? ThresholdsPath { get; private init; }
    public string DatasetSet { get; private init; } = "full";
    public bool EnforceThresholds { get; private init; }
    public int MismatchLimit { get; private init; } = 250;
    public string[] ProfileNames { get; private init; } = Array.Empty<string>();

    public static EvalCliOptions Parse(string[] args)
    {
        var options = new EvalCliOptions();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--input-root":
                    options = options with { InputRoot = args[++i] };
                    break;
                case "--output-root":
                    options = options with { OutputRoot = args[++i] };
                    break;
                case "--thresholds":
                    options = options with { ThresholdsPath = args[++i] };
                    break;
                case "--dataset-set":
                    options = options with { DatasetSet = args[++i].ToLowerInvariant() };
                    break;
                case "--mismatch-limit":
                    options = options with { MismatchLimit = int.Parse(args[++i], CultureInfo.InvariantCulture) };
                    break;
                case "--profiles":
                    options = options with { ProfileNames = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) };
                    break;
                case "--enforce-thresholds":
                    options = options with { EnforceThresholds = true };
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }

        return options;
    }
}

internal static class RepositoryPaths
{
    public static string ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "DotNetG2P.slnx")))
                return current.FullName;
            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    public static string ResolveAgainstRepo(string repoRoot, string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(repoRoot, path));
    }
}

internal enum SourceKind : byte
{
    WikiPron = 0,
    IpaDict = 1,
}

internal sealed record DatasetDefinition(string Name, string FileName, SourceKind SourceKind, FrenchDialect Dialect)
{
    public static IReadOnlyList<DatasetDefinition> CreateSet(string setName)
    {
        return setName switch
        {
            "sample" => new[]
            {
                new DatasetDefinition("ipa_dict_fr_fr", "ipa_dict_fr_fr_sample.tsv", SourceKind.IpaDict, FrenchDialect.Metropolitan),
                new DatasetDefinition("wikipron_fra_latn_broad_filtered", "wikipron_fra_latn_broad_filtered_sample.tsv", SourceKind.WikiPron, FrenchDialect.Metropolitan),
            },
            "full" => new[]
            {
                new DatasetDefinition("ipa_dict_fr_fr", "ipa_dict_fr_fr_full.tsv", SourceKind.IpaDict, FrenchDialect.Metropolitan),
                new DatasetDefinition("wikipron_fra_latn_broad_filtered", "wikipron_fra_latn_broad_filtered_full.tsv", SourceKind.WikiPron, FrenchDialect.Metropolitan),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(setName), setName, "dataset-set must be 'sample' or 'full'."),
        };
    }
}

internal sealed record EvalProfile(string Name, bool EnableAllophones, FrenchAllophoneFeatures AllophoneFeatures, bool EnableExceptionDictionary)
{
    public static IReadOnlyList<EvalProfile> CreateDefault(string[] requestedNames)
    {
        var profiles = new Dictionary<string, EvalProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["base"] = new EvalProfile("base", false, FrenchAllophoneFeatures.Default, true),
            ["allophones"] = new EvalProfile("allophones", true, FrenchAllophoneFeatures.Default, true),
            ["no_exceptions"] = new EvalProfile("no_exceptions", false, FrenchAllophoneFeatures.Default, false),
        };

        if (requestedNames.Length == 0)
            return profiles.Values.ToArray();

        return requestedNames.Select(name => profiles.TryGetValue(name, out var profile)
            ? profile
            : throw new ArgumentException($"Unknown profile: {name}")).ToArray();
    }
}

internal static class CorpusEvaluator
{
    public static EvalResult Evaluate(
        IReadOnlyList<(string Word, string Reference)> rows,
        FrenchG2PEngine engine,
        SourceKind sourceKind,
        FrenchDialect dialect,
        ExceptionCategoryMap exceptionCategories,
        string profile,
        int mismatchLimit)
    {
        var totalErrors = 0;
        var totalReferencePhonemes = 0;
        var exactMatches = 0;
        var evaluatedCases = 0;
        var mismatches = new List<MismatchRow>();

        foreach (var row in rows)
        {
            var predicted = NormalizePredicted(engine.ToPhonemeList(row.Word));
            var reference = NormalizeReference(row.Reference, sourceKind, dialect);
            if (reference.Length == 0 || predicted.Length == 0)
                continue;

            evaluatedCases++;
            var distance = LevenshteinDistance(predicted, reference);
            totalErrors += distance;
            totalReferencePhonemes += reference.Length;

            if (distance == 0)
            {
                exactMatches++;
                continue;
            }

            mismatches.Add(new MismatchRow(
                row.Word,
                exceptionCategories.Classify(row.Word, predicted, reference),
                distance,
                string.Join(" ", predicted),
                string.Join(" ", reference)));
        }

        var per = totalReferencePhonemes == 0 ? 0d : (double)totalErrors / totalReferencePhonemes;
        var wer = evaluatedCases == 0 ? 0d : (double)(evaluatedCases - exactMatches) / evaluatedCases;
        var categorySummaries = mismatches
            .GroupBy(x => x.Category, StringComparer.Ordinal)
            .Select(group => new CategorySummary(group.Key, group.Count(), group.Average(x => x.Distance)))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Category, StringComparer.Ordinal)
            .ToArray();

        var orderedMismatches = mismatches
            .OrderByDescending(x => x.Distance)
            .ThenBy(x => x.Category, StringComparer.Ordinal)
            .ThenBy(x => x.Word, StringComparer.Ordinal)
            .Take(mismatchLimit)
            .ToArray();

        return new EvalResult(rows.Count, evaluatedCases, exactMatches, wer, per, totalErrors, totalReferencePhonemes, orderedMismatches, categorySummaries);
    }

    private static string[] NormalizePredicted(IReadOnlyList<FrenchPhoneme> phonemes)
    {
        var result = new string[phonemes.Count];
        for (var i = 0; i < phonemes.Count; i++)
            result[i] = NormalizePredictedPhoneme(phonemes[i].Phoneme);
        return result;
    }

    private static string NormalizePredictedPhoneme(FrenchIpaPhoneme phoneme)
    {
        return phoneme switch
        {
            // 口母音
            FrenchIpaPhoneme.A => "a",
            FrenchIpaPhoneme.Ah => "a",           // Metropolitan: /ɑ/ → /a/ 統合
            FrenchIpaPhoneme.E => "e",
            FrenchIpaPhoneme.Eh => "\u025B",       // ɛ
            FrenchIpaPhoneme.I => "i",
            FrenchIpaPhoneme.O => "o",
            FrenchIpaPhoneme.Oh => "\u0254",       // ɔ
            FrenchIpaPhoneme.U => "u",
            FrenchIpaPhoneme.Y => "y",
            FrenchIpaPhoneme.Oe => "\u00F8",       // ø
            FrenchIpaPhoneme.Oeh => "\u0153",      // œ
            FrenchIpaPhoneme.Schwa => "\u0259",    // ə

            // 鼻母音
            FrenchIpaPhoneme.ANasal => "\u0251\u0303",     // ɑ̃
            FrenchIpaPhoneme.ONasal => "\u0254\u0303",     // ɔ̃
            FrenchIpaPhoneme.ENasal => "\u025B\u0303",     // ɛ̃
            FrenchIpaPhoneme.OeNasal => "\u025B\u0303",   // Metropolitan: /œ̃/ → /ɛ̃/ 合流

            // 半母音
            FrenchIpaPhoneme.J => "j",
            FrenchIpaPhoneme.W => "w",
            FrenchIpaPhoneme.Uj => "\u0265",       // ɥ

            // 閉鎖音
            FrenchIpaPhoneme.P => "p",
            FrenchIpaPhoneme.B => "b",
            FrenchIpaPhoneme.T => "t",
            FrenchIpaPhoneme.D => "d",
            FrenchIpaPhoneme.K => "k",
            FrenchIpaPhoneme.G => "\u0261",        // ɡ (U+0261)

            // 摩擦音
            FrenchIpaPhoneme.F => "f",
            FrenchIpaPhoneme.V => "v",
            FrenchIpaPhoneme.S => "s",
            FrenchIpaPhoneme.Z => "z",
            FrenchIpaPhoneme.Sh => "\u0283",       // ʃ
            FrenchIpaPhoneme.Zh => "\u0292",       // ʒ

            // 鼻音
            FrenchIpaPhoneme.M => "m",
            FrenchIpaPhoneme.N => "n",
            FrenchIpaPhoneme.Ny => "\u0272",       // ɲ

            // 側面音
            FrenchIpaPhoneme.L => "l",

            // 接近音
            FrenchIpaPhoneme.R => "\u0281",        // ʁ

            // 異音 → 基底音素に正規化
            FrenchIpaPhoneme.Rh => "\u0281",       // χ → ʁ
            FrenchIpaPhoneme.Ng => "\u014B",       // ŋ
            FrenchIpaPhoneme.Ts => "ts",
            FrenchIpaPhoneme.Dz => "dz",

            _ => throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, null),
        };
    }

    private static string[] NormalizeReference(string transcription, SourceKind sourceKind, FrenchDialect dialect)
    {
        var raw = sourceKind == SourceKind.WikiPron
            ? transcription.Replace("|", " ").Trim()
            : transcription.Replace("/", string.Empty)
                .Replace("\u02C8", string.Empty)  // ˈ
                .Replace("\u02CC", string.Empty)  // ˌ
                .Replace(".", string.Empty)
                .Replace(" ", string.Empty)
                .Trim();

        var tokens = sourceKind == SourceKind.WikiPron
            ? raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            : TokenizeIpa(raw);

        for (var i = 0; i < tokens.Length; i++)
            tokens[i] = NormalizeReferenceToken(tokens[i], dialect);

        return tokens.Where(token => token.Length > 0).ToArray();
    }

    private static string NormalizeReferenceToken(string token, FrenchDialect dialect)
    {
        // Metropolitan方言の正規化
        return token switch
        {
            // /ɑ/ → /a/ (Metropolitan方言の母音統合)
            "\u0251" => "a",                   // ɑ → a
            // /œ̃/ → /ɛ̃/ (Metropolitan方言の鼻母音合流)
            "\u0153\u0303" => "\u025B\u0303",  // œ̃ → ɛ̃
            // g (ASCII) → ɡ (IPA)
            "g" or "\u0261" => "\u0261",       // ɡ
            // 長音記号の除去
            "\u02D0" => string.Empty,          // ː
            // ティルダ付き母音の統合
            "a\u0303" => "\u0251\u0303",       // ã → ɑ̃
            "o\u0303" => "\u0254\u0303",       // õ → ɔ̃
            "e\u0303" => "\u025B\u0303",       // ẽ → ɛ̃
            _ => token,
        };
    }

    private static string[] TokenizeIpa(string text)
    {
        var tokens = new List<string>();
        for (var i = 0; i < text.Length;)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                i++;
                continue;
            }

            // 長音記号をスキップ
            if (text[i] == '\u02D0') // ː
            {
                i++;
                continue;
            }

            // 結合ティルダ(鼻母音)を先行文字に結合
            if (i + 1 < text.Length && text[i + 1] == '\u0303')
            {
                tokens.Add(text.Substring(i, 2));
                i += 2;
                continue;
            }

            // マルチ文字音素の検出
            if (TryMatch(text, i, "t\u0361\u0283", out var consumed)  // t͡ʃ
                || TryMatch(text, i, "d\u0361\u0292", out consumed)   // d͡ʒ
                || TryMatch(text, i, "ts", out consumed)
                || TryMatch(text, i, "dz", out consumed))
            {
                tokens.Add(text.Substring(i, consumed));
                i += consumed;
                continue;
            }

            tokens.Add(text[i].ToString());
            i++;
        }

        return tokens.ToArray();
    }

    private static bool TryMatch(string text, int start, string match, out int consumed)
    {
        if (start + match.Length <= text.Length && string.CompareOrdinal(text, start, match, 0, match.Length) == 0)
        {
            consumed = match.Length;
            return true;
        }

        consumed = 0;
        return false;
    }

    private static int LevenshteinDistance<T>(IReadOnlyList<T> source, IReadOnlyList<T> target)
    {
        if (source.Count == 0)
            return target.Count;
        if (target.Count == 0)
            return source.Count;

        var previous = new int[target.Count + 1];
        var current = new int[target.Count + 1];

        for (var j = 0; j <= target.Count; j++)
            previous[j] = j;

        for (var i = 1; i <= source.Count; i++)
        {
            current[0] = i;
            for (var j = 1; j <= target.Count; j++)
            {
                var cost = EqualityComparer<T>.Default.Equals(source[i - 1], target[j - 1]) ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[target.Count];
    }
}

internal sealed class ExceptionCategoryMap
{
    public static readonly ExceptionCategoryMap Empty = new ExceptionCategoryMap(new Dictionary<string, string>(StringComparer.Ordinal));

    private readonly Dictionary<string, string> _categories;

    private ExceptionCategoryMap(Dictionary<string, string> categories)
    {
        _categories = categories;
    }

    public static ExceptionCategoryMap Load(string path)
    {
        var categories = File.ReadAllLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#", StringComparison.Ordinal) && !line.StartsWith("surface\t", StringComparison.Ordinal))
            .Select(line => line.Split('\t'))
            .Where(parts => parts.Length >= 3)
            .GroupBy(parts => parts[0], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First()[2], StringComparer.Ordinal);

        return new ExceptionCategoryMap(categories);
    }

    /// <summary>
    /// フランス語固有のエラーカテゴリ分類。
    /// </summary>
    public string Classify(string word, string[] predicted, string[] reference)
    {
        if (_categories.TryGetValue(word, out var category))
            return category;

        var predJoined = string.Join(" ", predicted);
        var refJoined = string.Join(" ", reference);

        // 鼻母音関連
        if (ContainsNasalVowel(predJoined) || ContainsNasalVowel(refJoined))
            return "nasal_vowel";

        // シュワー関連
        if (predJoined.Contains("\u0259") || refJoined.Contains("\u0259"))
        {
            if (predicted.Length != reference.Length)
                return "schwa";
        }

        // 母音質の開閉の誤り: /e/-/ɛ/, /o/-/ɔ/, /ø/-/œ/
        if (IsVowelQualityMismatch(predicted, reference))
            return "vowel_quality";

        // 黙字関連: 音素数の差
        if (Math.Abs(predicted.Length - reference.Length) >= 1)
            return "silent_letter";

        // 接尾辞パターン: -tion/-sion/-ill- 系
        if (word.EndsWith("tion", StringComparison.OrdinalIgnoreCase)
            || word.EndsWith("sion", StringComparison.OrdinalIgnoreCase)
            || word.Contains("ill", StringComparison.OrdinalIgnoreCase))
            return "suffix_pattern";

        // h関連
        if (word.Contains('h') || word.Contains('H'))
            return "h_aspire";

        // 外来語候補
        if (LooksLikeForeignWord(word))
            return "foreign_word";

        // 子音の誤り
        if (HasConsonantMismatch(predicted, reference))
            return "consonant";

        return "other";
    }

    private static bool ContainsNasalVowel(string text)
    {
        return text.Contains("\u0251\u0303")     // ɑ̃
            || text.Contains("\u0254\u0303")      // ɔ̃
            || text.Contains("\u025B\u0303")      // ɛ̃
            || text.Contains("\u0153\u0303");     // œ̃
    }

    private static bool IsVowelQualityMismatch(string[] predicted, string[] reference)
    {
        var openClosePairs = new HashSet<(string, string)>
        {
            ("e", "\u025B"), ("\u025B", "e"),     // e ↔ ɛ
            ("o", "\u0254"), ("\u0254", "o"),      // o ↔ ɔ
            ("\u00F8", "\u0153"), ("\u0153", "\u00F8"), // ø ↔ œ
        };

        var minLen = Math.Min(predicted.Length, reference.Length);
        for (var i = 0; i < minLen; i++)
        {
            if (predicted[i] != reference[i] && openClosePairs.Contains((predicted[i], reference[i])))
                return true;
        }

        return false;
    }

    private static bool LooksLikeForeignWord(string word)
    {
        return word.Contains('k')
            || word.Contains('w')
            || word.Contains("sh", StringComparison.Ordinal)
            || word.Contains("ck", StringComparison.Ordinal)
            || word.Contains("ph", StringComparison.Ordinal)
            || word.EndsWith("ing", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly HashSet<string> s_consonants = new(StringComparer.Ordinal)
    {
        "p", "b", "t", "d", "k", "\u0261", "f", "v", "s", "z",
        "\u0283", "\u0292", "m", "n", "\u0272", "l", "\u0281",
        "\u03C7", "\u014B", "ts", "dz",
    };

    private static bool HasConsonantMismatch(string[] predicted, string[] reference)
    {
        var minLen = Math.Min(predicted.Length, reference.Length);
        for (var i = 0; i < minLen; i++)
        {
            if (predicted[i] != reference[i]
                && (s_consonants.Contains(predicted[i]) || s_consonants.Contains(reference[i])))
                return true;
        }

        return false;
    }
}

internal sealed class EvalThresholdConfig
{
    public static readonly EvalThresholdConfig Empty = new EvalThresholdConfig(new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal));

    private readonly Dictionary<string, Dictionary<string, double>> _thresholds;

    private EvalThresholdConfig(Dictionary<string, Dictionary<string, double>> thresholds)
    {
        _thresholds = thresholds;
    }

    public static EvalThresholdConfig Load(string path)
    {
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var thresholds = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);
        if (!document.RootElement.TryGetProperty("datasets", out var datasetsElement))
            return Empty;

        foreach (var datasetProperty in datasetsElement.EnumerateObject())
        {
            var perProfile = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var profileProperty in datasetProperty.Value.EnumerateObject())
                perProfile[profileProperty.Name] = profileProperty.Value.GetDouble();
            thresholds[datasetProperty.Name] = perProfile;
        }

        return new EvalThresholdConfig(thresholds);
    }

    public bool TryGetThreshold(string fileName, string profile, out double threshold)
    {
        threshold = 0d;
        return _thresholds.TryGetValue(fileName, out var perProfile)
            && perProfile.TryGetValue(profile, out threshold);
    }
}

internal sealed record EvalResult(
    int Cases,
    int EvaluatedCases,
    int ExactMatches,
    double WordErrorRate,
    double PhonemeErrorRate,
    int TotalErrors,
    int TotalReferencePhonemes,
    IReadOnlyList<MismatchRow> Mismatches,
    IReadOnlyList<CategorySummary> CategorySummaries);

internal sealed record EvalSummary(
    string Dataset,
    string FileName,
    string Profile,
    string Source,
    string Dialect,
    int Cases,
    int EvaluatedCases,
    int ExactMatches,
    double WordErrorRate,
    double PhonemeErrorRate,
    int TotalErrors,
    int TotalReferencePhonemes,
    double? Threshold,
    bool Passed);

internal sealed record MismatchRow(string Word, string Category, int Distance, string Predicted, string Reference);

internal sealed record CategorySummary(string Category, int Count, double AverageDistance);

internal sealed record CategorySummaryRow(string Dataset, string Profile, string Category, int Count, double AverageDistance);
