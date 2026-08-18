namespace UPA.Operations;

public sealed record OperationMatch(
    OperationDefinition Definition,
    string MatchedAlias,
    double Confidence);

public sealed class IntentOperationMatcher
{
    private readonly OperationRegistry _registry;

    public IntentOperationMatcher(OperationRegistry registry)
    {
        _registry = registry;
    }

    public IReadOnlyList<OperationMatch> Match(string intent)
    {
        if (string.IsNullOrWhiteSpace(intent))
            return Array.Empty<OperationMatch>();

        var normalized = NormalizePhrase(intent);
        var matches = new List<OperationMatch>();

        foreach (var definition in _registry.All())
        {
            foreach (var alias in definition.Aliases)
            {
                var a = NormalizePhrase(alias);

                if (PhraseMatches(normalized, a))
                {
                    matches.Add(new OperationMatch(
                        definition,
                        alias,
                        ConfidenceForAlias(normalized, a)));
                    break;
                }
            }
        }

        return matches
            .OrderByDescending(x => x.Confidence)
            .ThenBy(x => x.Definition.Id, StringComparer.Ordinal)
            .ToArray();
    }



    private static bool PhraseMatches(string intent, string alias)
    {
        if (string.IsNullOrWhiteSpace(intent) || string.IsNullOrWhiteSpace(alias))
            return false;

        if (intent.Contains(alias, StringComparison.Ordinal))
            return true;

        var intentTokens = intent.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var aliasTokens = alias.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (aliasTokens.Length == 0 || intentTokens.Length == 0)
            return false;

        // Match alias tokens as an ordered subsequence anywhere in the intent.
        // Natural-language connectors may occur between the alias tokens.
        // This intentionally does not require the alias to start at the
        // beginning of the intent, because one intent can contain several
        // operations: "create a gameobject and add rigidbody, collider".
        var stopWords = new HashSet<string>(StringComparer.Ordinal)
        {
            "a", "an", "the", "to", "of", "on", "for", "and",
            "with", "then", "please", "some", "another"
        };

        for (var start = 0; start < intentTokens.Length; start++)
        {
            if (!string.Equals(intentTokens[start], aliasTokens[0], StringComparison.Ordinal))
                continue;

            var ai = 1;

            for (var ii = start + 1; ii < intentTokens.Length && ai < aliasTokens.Length; ii++)
            {
                if (string.Equals(intentTokens[ii], aliasTokens[ai], StringComparison.Ordinal))
                {
                    ai++;
                    continue;
                }

                // Connectors are ignored; unrelated content is also allowed
                // because a single intent may contain multiple operations.
                if (stopWords.Contains(intentTokens[ii]))
                    continue;
            }

            if (ai == aliasTokens.Length)
                return true;
        }

        return false;
    }

    private static double ConfidenceForAlias(
        string intent,
        string alias)
    {
        if (intent.Equals(alias, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        if (intent.StartsWith(alias, StringComparison.OrdinalIgnoreCase))
            return 0.96;

        return 0.88;
    }

    private static string NormalizePhrase(string value)
    {
        var chars = value
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) ? ch : ' ')
            .ToArray();

        return string.Join(
            " ",
            new string(chars).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
