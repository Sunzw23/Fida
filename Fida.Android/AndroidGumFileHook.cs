#nullable enable
using System;
using System.IO;

namespace Fida.Android;

/// <summary>
/// Routes Gum's file requests to the layout files extracted by
/// <see cref="AndroidGumContent"/>. Gum resolves the project and every screen
/// through <c>ToolsUtilities.FileManager.CustomGetStreamFromFile</c>; on Android
/// that resolves against the packed assets rather than the file system, so the
/// requested name is reduced back to its "Gum/..." tail and read from disk.
/// </summary>
internal static class AndroidGumFileHook
{
    private const string FolderMarker = "/Gum/";

    private static string _root = string.Empty;

    /// <param name="extractedRoot">Directory containing the extracted "Gum" folder.</param>
    public static void Install(string extractedRoot)
    {
        _root = extractedRoot;
        ToolsUtilities.FileManager.CustomGetStreamFromFile = Open;
    }

    private static Stream Open(string path)
    {
        string normalized = (path ?? string.Empty).Replace('\\', '/');
        int marker = normalized.LastIndexOf(FolderMarker, StringComparison.OrdinalIgnoreCase);
        string relative = marker >= 0 ? normalized[(marker + 1)..] : normalized.TrimStart('/');

        string candidate = Path.Combine(_root, relative);
        if (File.Exists(candidate))
            return File.OpenRead(candidate);
        if (File.Exists(path))
            return File.OpenRead(path);

        // Gum turns a failed load into "Could not get the stream for <name>";
        // naming both paths makes an unexpected layout obvious in logcat.
        throw new FileNotFoundException(
            $"Gum asset '{path}' was not found (extracted as '{candidate}').", path);
    }
}
