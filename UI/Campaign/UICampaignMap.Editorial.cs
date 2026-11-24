#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using Fida.Campaign;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Campaign;

public sealed partial class UICampaignMap
{
#if !FIDA_ANDROID
    private void DrawEditorialDetails(SpriteBatch batch)
    {
        var layout = new CampaignMapLayout(_graphicsDevice.Viewport);
        bool folder = _folders.Count > 0;
        if (!folder)
        {
            CampaignNode? selected = _selectedIndex >= 0 && _selectedIndex < _nodes.Count ? _nodes[_selectedIndex] : null;
            if (selected == null) return;
            var motion = ButtonMotion(layout.Action);
            _routeView.DrawDetails(batch, layout, new CampaignRouteDetails(selected.Id, selected.Name,
                selected.Story, selected.Enemies.Count, selected.SkillBudget, selected.RewardText,
                selected.Rewards.Count > 0 ? selected.Rewards[0] : null, selected.Chapter,
                !CampaignCatalog.IsUnlocked(_progress, selected), selected.Tier ?? 0, selected.Vows,
                CampaignChapters.IsSide(selected)), motion.Hover, motion.Press, ChapterStyle.For(selected));
            return;
        }
        string title = _folders[Math.Clamp(_selectedFolder,0,_folders.Count-1)];
        var path = _scope.Concat(new[]{title}).ToArray();
        var chapter = CampaignChapters.Get(path);
        var style = ChapterStyle.ForPath(path);
        var descendants = CountedDescendants(path, descendants: _hierarchy.Descendants(path));
        int completed = descendants.Count(i=>_progress.IsCompleted(_nodes[i].Id));
        var focus = descendants.Select(i=>_nodes[i]).FirstOrDefault(n=>
            CampaignCatalog.IsUnlocked(_progress,n) && !_progress.IsCompleted(n.Id))
            ?? descendants.Select(i=>_nodes[i]).FirstOrDefault();
        var actionMotion = ButtonMotion(layout.Action);
        _routeView.DrawChapterDetails(batch,layout,new CampaignChapterDetails(
            FolderNumber(_selectedFolder, chapter),title,
            string.IsNullOrWhiteSpace(chapter?.Summary) ? focus?.Story ?? "" : chapter!.Summary,
            completed,descendants.Count, chapter?.IsSide == true),
            actionMotion.Hover,actionMotion.Press, style);
    }
#endif

    /// <summary>Main chapters only count main nodes; a side chapter counts its own nodes.</summary>
    private IReadOnlyList<int> CountedDescendants(IReadOnlyList<string> path, IReadOnlyList<int> descendants) =>
        CampaignChapters.Get(path) is { IsSide: false }
            ? descendants.Where(index => !CampaignChapters.IsSide(_nodes[index])).ToArray()
            : descendants;

    /// <summary>Main folders are numbered 01, 02...; side folders S1, S2... within their level.</summary>
    private string FolderNumber(int index, CampaignChapter? chapter)
    {
        bool side = chapter?.IsSide == true;
        int counter = 0;
        for (int i = 0; i < _folders.Count; i++)
        {
            var other = CampaignChapters.Get(_scope.Concat(new[]{_folders[i]}).ToArray());
            if ((other?.IsSide == true) != side) continue;
            counter++;
            if (i != index) continue;
            return side ? $"S{counter}" : counter.ToString("00");
        }
        return side ? "S" : "";
    }

    private void DrawChapterCard(SpriteBatch batch,Rectangle bounds,int index, bool selected)
    {
        var path = _scope.Concat(new[]{_folders[index]}).ToArray();
        var chapter = CampaignChapters.Get(path);
        var descendants = CountedDescendants(path, _hierarchy.Descendants(path));
        int cleared = descendants.Count(i => _progress.IsCompleted(_nodes[i].Id));
        var motion=ButtonMotion(bounds);
        bool locked=!descendants.Any(i=>CampaignCatalog.IsUnlocked(_progress,_nodes[i]));
        _routeView.DrawChapterCard(batch,bounds,FolderNumber(index, chapter),_folders[index],
            cleared,descendants.Count, selected,locked,motion.Hover,motion.Press, ChapterStyle.ForPath(path));
    }

    private void DrawChapterPreview(SpriteBatch batch, IReadOnlyList<int> indices, Rectangle area, Color color)
    {
        if (indices.Count == 0 || area.Width <= 0 || area.Height <= 0) return;
        if (indices.Count == 1 && area.Height >= 50)
        {
            int side = Math.Min(80, area.Height - 10);
            Rectangle marker = new(area.Center.X - side / 2, area.Center.Y - side / 2, side, side);
            UIUtils.DrawRectangleOutlines(batch, marker, color, 1, _pixel);
            DrawMapText(batch, _nodes[indices[0]].Id, new Rectangle(marker.Left + 10, marker.Top,
                side - 20, side), Math.Min(32, side * .55f), color, true);
            return;
        }
        float minX = indices.Min(i => _nodes[i].MapX), minY = indices.Min(i => _nodes[i].MapY);
        float maxX = indices.Max(i => _nodes[i].MapX), maxY = indices.Max(i => _nodes[i].MapY);
        float scale = Math.Min(Math.Max(1, area.Width - 14) / Math.Max(1, maxX - minX),
            Math.Max(1, area.Height - 14) / Math.Max(1, maxY - minY));
        Vector2 center = new((minX + maxX) / 2, (minY + maxY) / 2);
        var positions = indices.ToDictionary(i => _nodes[i].Id, i => area.Center.ToVector2() +
            (new Vector2(_nodes[i].MapX, _nodes[i].MapY) - center) * scale);
        foreach (int i in indices)
        {
            Vector2 end = positions[_nodes[i].Id];
            foreach (string parent in _nodes[i].Parents)
                if (positions.TryGetValue(parent, out var start)) DrawLine(batch, start, end, color * .6f, 1);
        }
        foreach (int i in indices)
        {
            Vector2 point = positions[_nodes[i].Id];
            int size = area.Height < 50 ? 3 : 7;
            batch.Draw(_pixel, new Rectangle((int)point.X - size / 2, (int)point.Y - size / 2, size, size),
                _progress.IsCompleted(_nodes[i].Id) ? UITheme.Ink : color);
        }
    }

    private void DrawMapText(SpriteBatch batch, string text, Rectangle area, float lineHeight, Color color, bool bold = false)
    {
        var font = bold ? _titleFont : _font;
        string safe = UIFontGuard.Sanitize(font, text);
        Vector2 size = font.MeasureString(safe);
        float scale = UITextRenderer.FitScale(size, area.Width, area.Height, lineHeight / font.LineSpacing);
        UITextRenderer.DrawString(batch, font, safe, new Vector2(area.Left, area.Center.Y - size.Y * scale / 2), color, scale: scale);
    }

    private void DrawMapParagraph(SpriteBatch batch, SpriteFont font, string text, Rectangle area, float lineHeight, Color color)
    {
        string safe = UIFontGuard.Sanitize(font, text);
        float scale = lineHeight / font.LineSpacing;
        var lines = UITextRenderer.WrapLines(safe, area.Width, value => font.MeasureString(value).X * scale);
        float row = font.LineSpacing * scale * 1.16f;
        if (lines.Count * row > area.Height)
        {
            scale *= area.Height / (lines.Count * row);
            lines = UITextRenderer.WrapLines(safe, area.Width, value => font.MeasureString(value).X * scale);
            row = font.LineSpacing * scale * 1.16f;
        }
        float y = area.Top;
        foreach (string line in lines)
        {
            UITextRenderer.DrawString(batch, font, line, new Vector2(area.Left, y), color, scale: scale);
            y += row;
        }
    }
}
