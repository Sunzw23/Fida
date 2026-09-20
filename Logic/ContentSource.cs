#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fida.Logic;

/// <summary>
/// Cross-platform access to data files. Desktop uses ordinary files while
/// mobile hosts can supply readers for packaged application assets.
/// </summary>
public static class ContentSource
{
    public static Func<string, Stream?>? OpenPackagedAsset { get; set; }
    public static Func<string, IReadOnlyList<string>>? ListPackagedAssets { get; set; }

    public static string? ReadText(string path)
    {
        if (File.Exists(path))
            return File.ReadAllText(path);

        string relative = Normalize(path);
        using Stream? stream = OpenPackagedAsset?.Invoke(relative);
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static IReadOnlyList<string> List(string directory, string extension)
    {
        if (Directory.Exists(directory))
            return Directory.GetFiles(directory, "*" + extension)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();

        string relative = Normalize(directory).TrimEnd('/');
        return (ListPackagedAssets?.Invoke(relative) ?? Array.Empty<string>())
            .Where(value => value.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static string Normalize(string path)
    {
        string normalized = path.Replace('\\', '/');
        int content = normalized.LastIndexOf("/Content/", StringComparison.OrdinalIgnoreCase);
        if (content >= 0) return normalized[(content + 1)..];
        if (normalized.StartsWith("Content/", StringComparison.OrdinalIgnoreCase)) return normalized;
        return normalized.TrimStart('/');
    }
}
