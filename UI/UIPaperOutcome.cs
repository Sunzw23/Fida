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
    private Rectangle? _roadSource;
    public UIPaperOutcome(ResourceManager resources, CampaignNode? node, bool won, string message, bool duel) : base(0)
    { _resources = resources; _node = node; _won = won; _message = message; _duel = duel; }
    public static Rectangle Action(UIEditorialLayout layout, bool primary, bool duel) =>
        layout.Element("Outcome", duel ? "DuelAction" : primary ? "PrimaryAction" : "SecondaryAction");
    public override void Draw(SpriteBatch batch, GameTime time)
    {
        var l = new UIEditorialLayout(batch.GraphicsDevice.Viewport);
        UIPaper.StackedSquare(batch, l.Element("Outcome", "Panel"), UITheme.PanelDark);
        Rectangle scene = l.Element("Outcome", "Scene");
        var victoryRoad = _resources.GetTexture("victory_road");
        if (_won && !_duel && _node != null && _node.Endings.Count == 0 && victoryRoad != null)
        {
            if (_roadSource == null)
            {
                // Trim transparent padding, then fit uniformly so the moon stays round.
                var pixels = new Color[victoryRoad.Width * victoryRoad.Height];
                victoryRoad.GetData(pixels);
                int left = victoryRoad.Width, top = victoryRoad.Height, right = -1, bottom = -1;
                for (int y = 0; y < victoryRoad.Height; y++)
                for (int x = 0; x < victoryRoad.Width; x++)
                {
                    if (pixels[y * victoryRoad.Width + x].A < 128) continue;
                    left = System.Math.Min(left, x); top = System.Math.Min(top, y);
                    right = System.Math.Max(right, x); bottom = System.Math.Max(bottom, y);
                }
                _roadSource = right >= left ? new Rectangle(left, top, right - left + 1, bottom - top + 1)
                    : victoryRoad.Bounds;
            }
            Rectangle source = _roadSource.Value;
            float fit = System.Math.Min(scene.Width / (float)source.Width, scene.Height / (float)source.Height);
            // Use the same floating-point scale on both axes, including portrait layouts.
            batch.Draw(victoryRoad, new Vector2(scene.Center.X - source.Width * fit / 2, scene.Bottom - source.Height * fit),
                source, Color.White, 0, Vector2.Zero, fit, SpriteEffects.None, 0);
        }
        else
        {
            UIPaper.Scene(batch, scene, _won && _node?.Endings.Count > 0 ? 5 :
                _won ? 1 : UIPaper.ChapterIndex(_node?.Chapter ?? ""), _won ? 1 : .55f);
        }
        string title = _duel ? _won ? "Victory" : "Defeat" : _won && _node?.Endings.Count > 0 ? "At the Ferry" : _won ? "Node Cleared" : "Defeat";
        Rectangle heading = l.Element("Outcome", "Heading");
        var seal = l.Element("Outcome", "Seal");
        UIPaper.Medal(batch,seal);
        void Stroke(Vector2 a,Vector2 b)
        {
            Vector2 delta=b-a;
            batch.Draw(_resources.Pixel,a,null,UIPaper.Ink,(float)System.Math.Atan2(delta.Y,delta.X),Vector2.Zero,
                new Vector2(delta.Length(),System.Math.Max(4,seal.Width*.08f)),SpriteEffects.None,0);
        }
        Vector2 mark=seal.Center.ToVector2();
        if(_won) UIPaper.Checkmark(batch, seal);
        else { Stroke(mark-new Vector2(seal.Width*.2f,seal.Height*.2f),mark+new Vector2(seal.Width*.2f,seal.Height*.2f)); Stroke(mark+new Vector2(-seal.Width*.2f,seal.Height*.2f),mark+new Vector2(seal.Width*.2f,-seal.Height*.2f)); }
        var font = _resources.TitleFont; Vector2 size = font.MeasureString(UIFontGuard.Sanitize(font, title));
        float scale = UITextRenderer.FitScale(size, heading.Width, heading.Height, l.Scale * .8f);
        UITextRenderer.DrawString(batch, font, title, heading.Center.ToVector2() - size * scale / 2, UITheme.Ink, scale: scale);
        var divider = l.Element("Outcome", "Divider");
        batch.Draw(_resources.Pixel, divider, UITheme.InkMuted * .65f);
        UITextRenderer.DrawParagraph(batch, _resources.Font, _won && _node?.Endings.Count > 0 ? "The boat is ready." : _message,
            l.Element("Outcome", "Message"), UITheme.Ink, centered:true);
        if (_won && _node?.Rewards.Count > 0)
        {
            var reward = l.Element("Outcome", "Reward");
            UIPaper.Sheet(batch, reward, UIPaper.Reward, 2, 6);
            var iconCenter = new Vector2(reward.Left + reward.Height * .45f, reward.Center.Y);
            float iconSize = reward.Height * .22f;
            batch.Draw(_resources.Pixel, iconCenter, null, UIPaper.Ink, MathHelper.PiOver4,
                new Vector2(.5f), new Vector2(iconSize), SpriteEffects.None, 0);
            UIUtils.DrawCenteredFit(batch, _resources.LabelFont, "Unlocked: " + _node.RewardText,
                reward.Center.ToVector2() + new Vector2(reward.Height * .12f, 0), reward.Width - reward.Height * 1.2f,
                l.Scale * 44 / _resources.LabelFont.LineSpacing, UIPaper.Ink);
        }
    }
}

