using System;

namespace SpaceTradeEmpire.Bridge;

public static class RuntimePaths
{
    public static string Res(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path is empty", nameof(path));
        path = Normalize(path);
        if (path.StartsWith("res://", StringComparison.Ordinal)) return path;
        if (path.StartsWith("user://", StringComparison.Ordinal))
            throw new InvalidOperationException($"Expected res:// path, got: {path}");
        return "res://" + path.TrimStart('/');
    }

    public static string User(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path is empty", nameof(path));
        path = Normalize(path);
        if (path.StartsWith("user://", StringComparison.Ordinal)) return path;
        if (path.StartsWith("res://", StringComparison.Ordinal))
            throw new InvalidOperationException($"Expected user:// path, got: {path}");
        return "user://" + path.TrimStart('/');
    }

    public static void AssertAllowed(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path is empty", nameof(path));
        path = Normalize(path);
        if (path.StartsWith("res://", StringComparison.Ordinal)) return;
        if (path.StartsWith("user://", StringComparison.Ordinal)) return;
        throw new InvalidOperationException($"Runtime File Contract violation: {path}");
    }

    private static string Normalize(string path) => path.Replace('\\', '/').Trim();
}
