using System;
using System.Linq;
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
        string heading = _scope.LastOrDefault() ?? "Campaign";
        DrawLeft(batch, _titleFont, heading, layout.Header.Center.ToVector2() - new Vector2(layout.Header.Width / 2f, 0), layout.Header.Width, .52f, UITheme.Ink);
        if (!layout.Portrait)
            batch.Draw(_pixel, new Rectangle(area.Left, area.Top, 1, area.Height), UITheme.Line);
        else batch.Draw(_pixel, new Rectangle(area.Left, area.Top, area.Width, 1), UITheme.Line);
        CampaignNode node = _selectedIndex >= 0 && _selectedIndex < _nodes.Count ? _nodes[_selectedIndex] : null;
        string title, description, metric, reward;
        bool unlocked;
        if (folder)
        {
            title = _folders[Math.Clamp(_selectedFolder, 0, _folders.Count - 1)];
            var descendants = _hierarchy.Descendants(_scope.Concat(new[] { title }).ToArray());
            int completed = descendants.Count(i => _progress.IsCompleted(_nodes[i].Id));
            description = "";
            metric = $"{completed} / {descendants.Count} completed";
            reward = "";
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
        var titleArea = Slot(0, .22f);
        UITextRenderer.DrawParagraph(batch, _font, title, titleArea, UITheme.Ink);
        UITextRenderer.DrawParagraph(batch, _font, description, Slot(.23f, .56f), UITheme.InkMuted);
        UITextRenderer.DrawParagraph(batch, _font, metric, Slot(.58f, .73f), UITheme.Ink);
        UITextRenderer.DrawParagraph(batch, _font, reward, Slot(.76f, 1), UITheme.AccentBlue);
        batch.Draw(_pixel, layout.Action, unlocked ? UITheme.Ink : UITheme.PanelLight);
        string action = folder ? "Open chapter" : unlocked ? "Configure skills" : "Locked";
        DrawCentered(batch, _font, action, layout.Action.Center.ToVector2(), layout.Action.Width - 24,
            Math.Min(.6f, layout.Action.Height / (float)_font.LineSpacing * .65f), unlocked ? UITheme.BgTop : UITheme.InkMuted);
    }
#endif
}
