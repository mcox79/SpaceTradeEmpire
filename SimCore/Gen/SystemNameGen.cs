using System;

namespace SimCore.Gen;

/// <summary>
/// Deterministic procedural system name generator.
/// Uses syllable combination with RNG for consistent, space-flavored names.
/// </summary>
public static class SystemNameGen
{
    // Syllable pools — blended sci-fi / frontier / classical feel.
    private static readonly string[] Prefixes =
    {
        "Al", "Ar", "Bel", "Cor", "Del", "Dra", "El", "Fal", "Gor", "Hel",
        "Ith", "Kal", "Ler", "Mar", "Ner", "Ol", "Par", "Qel", "Ral", "Sar",
        "Tar", "Val", "Vor", "Zel", "Kin", "Mol", "Nev", "Sil", "Thal", "Ven",
    };

    private static readonly string[] Middles =
    {
        "a", "an", "ar", "el", "en", "er", "i", "in", "ir", "is",
        "o", "on", "or", "u", "un", "us", "ax", "ex", "ix", "os",
    };

    private static readonly string[] Suffixes =
    {
        "a", "ax", "is", "os", "us", "on", "ar", "el", "um", "en",
        "ia", "ra", "na", "ux", "ix", "al", "an", "or", "ek", "il",
    };

    // Optional designators appended to ~25% of names for variety.
    private static readonly string[] Designators =
    {
        "Prime", "Major", "Minor", "Alpha", "Beta", "Nexus", "Gate", "Reach",
    };

    /// <summary>
    /// Generate a unique system name from a deterministic RNG.
    /// </summary>
    public static string Generate(Random rng)
    {
        string prefix = Prefixes[rng.Next(Prefixes.Length)];
        string middle = Middles[rng.Next(Middles.Length)];
        string suffix = Suffixes[rng.Next(Suffixes.Length)];

        string name = prefix + middle + suffix;

        // STRUCTURAL: percentage modulus and threshold for designator chance.
        if (rng.Next(100) < 25)
        {
            name += " " + Designators[rng.Next(Designators.Length)];
        }

        return name;
    }

    /// <summary>
    /// Generate N unique names. Falls back to appending index on collision.
    /// </summary>
    public static string[] GenerateUnique(Random rng, int count)
    {
        var names = new string[count];
        var used = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = STRUCT_LOOP_START; i < count; i++)
        {
            string name = Generate(rng);
            int attempt = STRUCT_LOOP_START;
            while (!used.Add(name))
            {
                attempt++;
                name = Generate(rng);
                if (attempt > STRUCT_MAX_RETRIES)
                {
                    // Exhausted retries — append index to guarantee uniqueness.
                    name += $" {i}";
                    used.Add(name);
                    break;
                }
            }
            names[i] = name;
        }

        return names;
    }

    // STRUCTURAL: loop initialization constants.
    private const int STRUCT_LOOP_START = 0;
    private const int STRUCT_MAX_RETRIES = 20;
}
