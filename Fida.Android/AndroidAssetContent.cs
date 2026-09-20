#nullable enable
using Android.Content.Res;
using Fida.Logic;
using System;
using System.Collections.Generic;

namespace Fida.Android;

internal static class AndroidAssetContent
{
    public static void Initialize(AssetManager assets)
    {
        ContentSource.OpenPackagedAsset = path =>
        {
            try { return assets.Open(path); }
            catch { return null; }
        };
        ContentSource.ListPackagedAssets = path => ListFiles(assets, path);
    }

    private static IReadOnlyList<string> ListFiles(AssetManager assets, string directory)
    {
        var result = new List<string>();
        Visit(assets, directory.Trim('/'), result);
        return result;
    }

    private static void Visit(AssetManager assets, string path, List<string> result)
    {
        string[] entries;
        try { entries = assets.List(path) ?? Array.Empty<string>(); }
        catch { return; }

        foreach (string entry in entries)
        {
            string child = string.IsNullOrEmpty(path) ? entry : $"{path}/{entry}";
            string[] children;
            try { children = assets.List(child) ?? Array.Empty<string>(); }
            catch { children = Array.Empty<string>(); }
            if (children.Length == 0) result.Add(child);
            else Visit(assets, child, result);
        }
    }
}
