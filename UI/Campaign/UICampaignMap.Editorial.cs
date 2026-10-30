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
        Rectangle area = layout.Details;
        int inset = Math.Clamp(area.Width / 14, 18, 40), left = area.Left + inset, width = area.Width - inset * 2;
        bool folder = _folders.Count > 0;
        DrawMapText(batch, "Campaign", layout.Header, 65 * new UIEditorialLayout(_graphicsDevice.Viewport).Scale, UITheme.Ink, true);
        UIPaper.Sheet(batch, area, UITheme.Panel, 4, 10);
        var shared = new UIEditorialLayout(_graphicsDevice.Viewport);
        UIPaper.Sheet(batch, shared.Portrait ? shared.Rect(30, 30, 840, 135) : shared.Rect(45, 30, 1830, 135), UITheme.Panel, 3, 9);
        DrawMapText(batch, "Campaign", layout.Header, 65 * shared.Scale, UITheme.Ink, true);
        CampaignNode node = _selectedIndex >= 0 && _selectedIndex < _nodes.Count ? _nodes[_selectedIndex] : null;
        string title, description, metric, reward;
        bool unlocked;
        if (folder)
        {
            title = _folders[Math.Clamp(_selectedFolder, 0, _folders.Count - 1)];
            var descendants = _hierarchy.Descendants(_scope.Concat(new[] { title }).ToArray());
            int completed = descendants.Count(i => _progress.IsCompleted(_nodes[i].Id));
            var focus = descendants.Select(i => _nodes[i]).FirstOrDefault(n =>
                CampaignCatalog.IsUnlocked(_progress, n) && !_progress.IsCompleted(n.Id))
                ?? descendants.Select(i => _nodes[i]).FirstOrDefault();
            description = focus?.Story ?? "";
            int available = descendants.Count(i => CampaignCatalog.IsUnlocked(_progress, _nodes[i]) && !_progress.IsCompleted(_nodes[i].Id));
            metric = $"{completed} / {descendants.Count} completed    /    {available} available";
            reward = focus == null ? "" : $"{focus.Name}";
            unlocked = true;
        }
        else if (node != null)
        {
            title = node.Name;
            description = node.Story;
            metric = $"{node.Enemies.Count} enemies    /    {node.SkillBudget} SP";
            reward = $"Reward: {node.RewardText}";
            unlocked = CampaignCatalog.IsUnlocked(_progress, node);
        }
        else
        {
            title = "Campaign"; description = "No missions available."; metric = ""; reward = ""; unlocked = false;
        }
        int contentHeight = layout.Action.Top - area.Top - 20;
        Rectangle Slot(float from, float to) => new(left, area.Top + (int)(contentHeight * from) + 14,
            width, Math.Max(1, (int)(contentHeight * (to - from)) - 14));
        var titleArea = Slot(.06f, .22f);
        DrawMapParagraph(batch, _titleFont, title, titleArea, Math.Clamp(area.Width * .10f, 30, 48), UITheme.Ink);
        string status = folder ? "CHAPTER" : _progress.IsCompleted(node?.Id ?? "") ? "COMPLETED" : unlocked ? "AVAILABLE" : "LOCKED";
        DrawMapText(batch, status, new Rectangle(left, area.Top + 10, width, 22), 18,
            unlocked ? UITheme.AccentBlue : UITheme.InkMuted);
        if (folder)
        {
            var descendants = _hierarchy.Descendants(_scope.Concat(new[] { title }).ToArray());
            var preview = Slot(.24f, .48f);
            UIPaper.Sheet(batch, preview, UIPaper.Face, 3, 6);
            preview.Inflate(-5, -5);
            UIPaper.Scene(batch, preview, UIPaper.ChapterIndex(title));
            DrawMapParagraph(batch, _font, description, Slot(.52f, .72f), 27, UITheme.InkMuted);
            DrawMapParagraph(batch, _font, metric, Slot(.75f, .86f), 25, UITheme.Ink);
            DrawMapParagraph(batch, _font, reward, Slot(.89f, 1), 23, UITheme.AccentBlue);
        }
        else
        {
            DrawMapParagraph(batch, _font, description, Slot(.26f, .55f), 29, UITheme.InkMuted);
            DrawMapParagraph(batch, _font, metric, Slot(.60f, .73f), 29, UITheme.Ink);
            DrawMapParagraph(batch, _font, reward, Slot(.78f, 1), 27, UITheme.AccentBlue);
        }
        UIPaper.Sheet(batch, layout.Action, unlocked ? UIPaper.Face : UITheme.PanelLight, 3, 8);
        string action = folder ? "Open chapter" : unlocked ? "Configure skills" : "Locked";
        DrawCentered(batch, _font, action, layout.Action.Center.ToVector2(), layout.Action.Width - 24,
            Math.Min(.6f, layout.Action.Height / (float)_font.LineSpacing * .65f), unlocked ? UIPaper.Ink : UITheme.InkMuted);
    }
#endif


    private void DrawChapterCard(SpriteBatch batch, Rectangle bounds, int index,
        IReadOnlyList<int> descendants, int cleared, bool selected)
    {
        Color accent = selected ? UITheme.AccentBlue : UITheme.Line;
        UIPaper.Sheet(batch, bounds, selected ? UITheme.AccentBlue : UIPaper.Face, selected ? 5 : 3, 7);
        int pad = Math.Clamp(bounds.Width / 25, 10, 18);
        bool compact = bounds.Height < 120;
        int header = compact ? 34 : 43;
        DrawMapText(batch, $"{index + 1:00}", new Rectangle(bounds.Left + pad, bounds.Top + 7, 26, 24), 19, UIPaper.MutedInk, true);
        DrawMapText(batch, _folders[index], new Rectangle(bounds.Left + pad + 36, bounds.Top + 5,
            bounds.Width - pad * 2 - 36, header - 7), Math.Clamp(bounds.Width * .07f, 22, 31), UIPaper.Ink, true);
        if (!compact)
            UIPaper.Scene(batch, new Rectangle(bounds.Left + pad, bounds.Top + header,
                bounds.Width - pad * 2, bounds.Height - header - 36), UIPaper.ChapterIndex(_folders[index]));
        DrawMapText(batch, $"{cleared} / {descendants.Count} cleared", new Rectangle(bounds.Left + pad,
            bounds.Bottom - 31, bounds.Width - pad * 2, 22), 19, UIPaper.MutedInk);
        Rectangle track = new(bounds.Left + pad, bounds.Bottom - 7, bounds.Width - pad * 2, 3);
        batch.Draw(_pixel, track, UIPaper.MutedInk * .35f);
        if (descendants.Count > 0 && cleared > 0)
            batch.Draw(_pixel, new Rectangle(track.Left, track.Top, Math.Max(1, track.Width * cleared / descendants.Count), track.Height), UITheme.PanelHover);
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
