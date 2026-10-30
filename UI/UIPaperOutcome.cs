#nullable enable
using Fida.Campaign;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>Presentation only; outcome and ending actions remain owned by the existing states.</summary>
public sealed class UIPaperOutcome : UIControl
{
    private readonly ResourceManager _resources;
    private readonly CampaignNode? _node;
    private readonly bool _won, _duel;
    private readonly string _message;
    public UIPaperOutcome(ResourceManager resources, CampaignNode? node, bool won, string message, bool duel) : base(0)
    { _resources = resources; _node = node; _won = won; _message = message; _duel = duel; }
    public static Rectangle Action(UIEditorialLayout layout, bool primary, bool duel) => layout.Portrait
        ? layout.Rect(80, duel ? 1310 : primary ? 1310 : 1190, 740, 100)
        : duel ? layout.Rect(650, 900, 620, 90) : layout.Rect(primary ? 990 : 450, 900, 480, 90);
    public override void Draw(SpriteBatch batch, GameTime time)
    {
        var l = new UIEditorialLayout(batch.GraphicsDevice.Viewport); bool p = l.Portrait;
        UIPaper.Sheet(batch, p ? l.Rect(40, 230, 820, 1230) : l.Rect(390, 180, 1140, 860), UITheme.PanelLight, 4, 12);
        UIPaper.Sheet(batch, p ? l.Rect(50, 220, 800, 1230) : l.Rect(400, 170, 1120, 860), UITheme.Panel, 4, 10);
        Rectangle scene = p ? l.Rect(80, 450, 740, 350) : l.Rect(450, 350, 1020, 320);
        UIPaper.Scene(batch, scene, _won && _node?.Endings.Count > 0 ? 5 : UIPaper.ChapterIndex(_node?.Chapter ?? ""), _won ? 1 : .55f);
        string title = _duel ? _won ? "Victory" : "Defeat" : _won && _node?.Endings.Count > 0 ? "At the Ferry" : _won ? "Node Cleared" : "Defeat";
        Rectangle heading = p ? l.Rect(80, 290, 740, 100) : l.Rect(450, 230, 1020, 85);
        var font = _resources.TitleFont; Vector2 size = font.MeasureString(UIFontGuard.Sanitize(font, title));
        float scale = UITextRenderer.FitScale(size, heading.Width, heading.Height, l.Scale * .8f);
        UITextRenderer.DrawString(batch, font, title, heading.Center.ToVector2() - size * scale / 2, UITheme.Ink, scale: scale);
        UITextRenderer.DrawParagraph(batch, _resources.Font, _won && _node?.Endings.Count > 0 ? "The boat is ready." : _message,
            p ? l.Rect(80, 850, 740, 150) : l.Rect(450, 705, 1020, 90), UITheme.Ink);
        if (_won && _node?.Rewards.Count > 0)
        {
            var reward = p ? l.Rect(80, 1040, 740, 90) : l.Rect(450, 810, 1020, 55);
            UIPaper.Sheet(batch, reward, UIPaper.Reward, 2, 6);
            reward.Inflate(-12, -8);
            UITextRenderer.DrawParagraph(batch, _resources.CaptionFont, "Unlocked: " + _node.RewardText, reward, UIPaper.Ink);
        }
    }
}
