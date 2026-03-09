using System.Globalization;
using System.Text;
using System.Text.Json;
using DotNetG2P.Spanish;

var options = EvalCliOptions.Parse(args);
var repoRoot = RepositoryPaths.ResolveRepoRoot();
var inputRoot = RepositoryPaths.ResolveAgainstRepo(repoRoot, options.InputRoot ?? Path.Combine("artifacts", "spanish-eval", "corpora"));
var outputRoot = RepositoryPaths.ResolveAgainstRepo(repoRoot, options.OutputRoot ?? Path.Combine("artifacts", "spanish-eval", "reports", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)));
var thresholdsPath = RepositoryPaths.ResolveAgainstRepo(repoRoot, options.ThresholdsPath ?? Path.Combine("tools", "spanish_eval_thresholds.json"));
var exceptionMasterPath = Path.Combine(repoRoot, "src", "DotNetG2P.Spanish", "Data", "spanish_exceptions.master.tsv");

Directory.CreateDirectory(outputRoot);
Directory.CreateDirectory(Path.Combine(outputRoot, "mismatches"));

var datasetDefinitions = DatasetDefinition.CreateSet(options.DatasetSet);
var thresholds = File.Exists(thresholdsPath)
    ? EvalThresholdConfig.Load(thresholdsPath)
    : EvalThresholdConfig.Empty;
var exceptionCategories = ExceptionCategoryMap.Load(exceptionMasterPath);
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
        using var engine = new SpanishG2PEngine(new SpanishG2POptions(
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

internal sealed record DatasetDefinition(string Name, string FileName, SourceKind SourceKind, SpanishDialect Dialect)
{
    public static IReadOnlyList<DatasetDefinition> CreateSet(string setName)
    {
        return setName switch
        {
            "sample" => new[]
            {
                new DatasetDefinition("ipa_dict_es_es", "ipa_dict_es_es_sample.tsv", SourceKind.IpaDict, SpanishDialect.Castilian),
                new DatasetDefinition("ipa_dict_es_mx", "ipa_dict_es_mx_sample.tsv", SourceKind.IpaDict, SpanishDialect.LatinAmerican),
                new DatasetDefinition("wikipron_spa_latn_ca_broad_filtered", "wikipron_spa_latn_ca_broad_filtered_sample.tsv", SourceKind.WikiPron, SpanishDialect.Castilian),
                new DatasetDefinition("wikipron_spa_latn_la_broad_filtered", "wikipron_spa_latn_la_broad_filtered_sample.tsv", SourceKind.WikiPron, SpanishDialect.LatinAmerican),
            },
            "full" => new[]
            {
                new DatasetDefinition("ipa_dict_es_es", "ipa_dict_es_es_full.tsv", SourceKind.IpaDict, SpanishDialect.Castilian),
                new DatasetDefinition("ipa_dict_es_mx", "ipa_dict_es_mx_full.tsv", SourceKind.IpaDict, SpanishDialect.LatinAmerican),
                new DatasetDefinition("wikipron_spa_latn_ca_broad_filtered", "wikipron_spa_latn_ca_broad_filtered_full.tsv", SourceKind.WikiPron, SpanishDialect.Castilian),
                new DatasetDefinition("wikipron_spa_latn_la_broad_filtered", "wikipron_spa_latn_la_broad_filtered_full.tsv", SourceKind.WikiPron, SpanishDialect.LatinAmerican),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(setName), setName, "dataset-set must be 'sample' or 'full'."),
        };
    }
}

internal sealed record EvalProfile(string Name, bool EnableAllophones, SpanishAllophoneFeatures AllophoneFeatures, bool EnableExceptionDictionary)
{
    public static IReadOnlyList<EvalProfile> CreateDefault(string[] requestedNames)
    {
        var profiles = new Dictionary<string, EvalProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["base"] = new EvalProfile("base", false, SpanishAllophoneFeatures.Default, true),
            ["allophones"] = new EvalProfile("allophones", true, SpanishAllophoneFeatures.Default, true),
            ["no_exceptions"] = new EvalProfile("no_exceptions", false, SpanishAllophoneFeatures.Default, false),
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
        SpanishG2PEngine engine,
        SourceKind sourceKind,
        SpanishDialect dialect,
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
                exceptionCategories.Classify(row.Word),
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

    private static string[] NormalizePredicted(IReadOnlyList<SpanishPhoneme> phonemes)
    {
        var result = new string[phonemes.Count];
        for (var i = 0; i < phonemes.Count; i++)
            result[i] = NormalizePredictedPhoneme(phonemes[i].Phoneme);
        return result;
    }

    private static string NormalizePredictedPhoneme(SpanishIpaPhoneme phoneme)
    {
        return phoneme switch
        {
            SpanishIpaPhoneme.A => "a",
            SpanishIpaPhoneme.E => "e",
            SpanishIpaPhoneme.I => "i",
            SpanishIpaPhoneme.O => "o",
            SpanishIpaPhoneme.U => "u",
            SpanishIpaPhoneme.J => "j",
            SpanishIpaPhoneme.W => "w",
            SpanishIpaPhoneme.P => "p",
            SpanishIpaPhoneme.B or SpanishIpaPhoneme.Beta => "b",
            SpanishIpaPhoneme.T => "t",
            SpanishIpaPhoneme.D or SpanishIpaPhoneme.Dh => "d",
            SpanishIpaPhoneme.K => "k",
            SpanishIpaPhoneme.G or SpanishIpaPhoneme.Gh => "ɡ",
            SpanishIpaPhoneme.F => "f",
            SpanishIpaPhoneme.S or SpanishIpaPhoneme.Z => "s",
            SpanishIpaPhoneme.X or SpanishIpaPhoneme.Sh => "x",
            SpanishIpaPhoneme.Ch => "tʃ",
            SpanishIpaPhoneme.Y or SpanishIpaPhoneme.Ll or SpanishIpaPhoneme.YAffricate => "ʝ",
            SpanishIpaPhoneme.M or SpanishIpaPhoneme.NLabiodental => "m",
            SpanishIpaPhoneme.N or SpanishIpaPhoneme.Eng or SpanishIpaPhoneme.NDental => "n",
            SpanishIpaPhoneme.Ny => "ɲ",
            SpanishIpaPhoneme.L => "l",
            SpanishIpaPhoneme.Rr => "r",
            SpanishIpaPhoneme.R => "ɾ",
            SpanishIpaPhoneme.Th => "θ",
            _ => throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, null),
        };
    }

    private static string[] NormalizeReference(string transcription, SourceKind sourceKind, SpanishDialect dialect)
    {
        var raw = sourceKind == SourceKind.WikiPron
            ? transcription.Replace("|", " ").Trim()
            : transcription.Replace("/", string.Empty)
                .Replace("ˈ", string.Empty)
                .Replace("ˌ", string.Empty)
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

    private static string NormalizeReferenceToken(string token, SpanishDialect dialect)
    {
        return token switch
        {
            "g" or "ɡ" or "ɣ" => "ɡ",
            "β" => "b",
            "ð" => "d",
            "z" => "s",
            "ɱ" => "m",
            "ŋ" or "n̪" => "n",
            "ʎ" or "ɟʝ" => "ʝ",
            "ʃ" => dialect == SpanishDialect.Castilian ? "x" : "x",
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

            if (TryMatch(text, i, "ɟʝ", out var consumed)
                || TryMatch(text, i, "tʃ", out consumed)
                || TryMatch(text, i, "n̪", out consumed))
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

    public string Classify(string word)
    {
        if (_categories.TryGetValue(word, out var category))
            return category;

        if (LooksLikeHiatoCandidate(word))
            return "hiato_candidate";
        if (LooksLikeLoanwordCandidate(word))
            return "loanword_candidate";
        if (LooksLikeProperNounCandidate(word))
            return "proper_noun_candidate";

        return "general";
    }

    private static bool LooksLikeHiatoCandidate(string word)
    {
        var patterns = new[] { "ae", "ea", "eo", "oa", "oe", "ua", "uo", "ia", "io", "iu", "ui", "uy", "aí", "eí", "oí", "aú", "eú", "oú" };
        return patterns.Any(word.Contains);
    }

    private static bool LooksLikeLoanwordCandidate(string word)
    {
        return word.Contains('k')
            || word.Contains('w')
            || word.Contains("sh", StringComparison.Ordinal)
            || word.Contains("wh", StringComparison.Ordinal)
            || word.Contains("ps", StringComparison.Ordinal)
            || word.Contains("ck", StringComparison.Ordinal);
    }

    private static bool LooksLikeProperNounCandidate(string word)
    {
        return word.StartsWith("xo", StringComparison.Ordinal)
            || word.StartsWith("oa", StringComparison.Ordinal)
            || word.Contains("tl", StringComparison.Ordinal)
            || word.Contains("tz", StringComparison.Ordinal);
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
