#nullable enable
using Android.Content.Res;
using System;
using System.IO;

namespace Fida.Android;

/// <summary>
/// Unpacks the packaged Gum layout beside the app's private data.
///
/// Gum's MonoGame loader routes every read through
/// <c>ToolsUtilities.FileManager</c>, which on Android cannot open APK assets or
/// arbitrary file paths, and the relative paths it derives for screens pick up a
/// second "Content/" prefix. Giving it real files and re-mapping its requests
/// (see <see cref="AndroidGumFileHook"/>) keeps the shared initializer on the
/// same code path the desktop build uses.
/// </summary>
internal static class AndroidGumContent
{
    private const string AssetRoot = "Content/Gum";
    private const string ProjectFileName = "Fida.gumx";

    /// <summary>
    /// Extracts the Gum project and returns the absolute path of the .gumx,
    /// or null when the assets could not be read.
    /// </summary>
    public static string? Materialize(AssetManager assets, string filesDirectory)
    {
        try
        {
            string targetRoot = Path.Combine(filesDirectory, "Gum");
            CopyDirectory(assets, AssetRoot, targetRoot);
            string project = Path.Combine(targetRoot, ProjectFileName);
            return File.Exists(project) ? project : null;
        }
        catch
        {
            return null;
        }
    }

    private static void CopyDirectory(AssetManager assets, string assetPath, string targetPath)
    {
        string[]? children;
        try { children = assets.List(assetPath); }
        catch { return; }

        if (children is null || children.Length == 0)
        {
            CopyFile(assets, assetPath, targetPath);
            return;
        }

        Directory.CreateDirectory(targetPath);
        foreach (string child in children)
            CopyDirectory(assets, assetPath + "/" + child, Path.Combine(targetPath, child));
    }

    private static void CopyFile(AssetManager assets, string assetPath, string targetPath)
    {
        try
        {
            using Stream source = assets.Open(assetPath);
            using FileStream target = File.Create(targetPath);
            source.CopyTo(target);
        }
        catch
        {
            // A single unreadable entry must not abort startup; the Gum loader
            // reports anything it genuinely needs.
        }
    }
}
