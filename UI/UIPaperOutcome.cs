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
    public static Rectangle Action(UIEditorialLayout layout, bool primary, bool duel) =>
        layout.Element("Outcome", duel ? "DuelAction" : primary ? "PrimaryAction" : "SecondaryAction");

    /// <summary>
    /// Draws a transparent landscape strip into the scene band.
    ///
    /// The strips are authored at roughly the band's aspect and are already cropped to their
    /// silhouette, so a uniform fit plus a bottom anchor is all that is needed - the empty sky
    /// is real transparency and the panel shows through it, which is what stops the artwork
    /// from reading as a pasted rectangle. This used to read the whole texture back with
    /// GetData to trim alpha padding; that stall is gone now that the builder crops instead.
    /// </summary>
    private static void DrawStrip(SpriteBatch batch, Texture2D strip, Rectangle scene)
    {
        if (strip == null || strip.Width < 1 || strip.Height < 1) return;
        float fit = System.Math.Min(scene.Width / (float)strip.Width, scene.Height / (float)strip.Height);
        batch.Draw(strip, new Vector2(scene.Center.X - strip.Width * fit / 2f, scene.Bottom - strip.Height * fit),
            null, Color.White, 0f, Vector2.Zero, new Vector2(fit, fit), SpriteEffects.None, 0f);
    }

    public override void Draw(SpriteBatch batch, GameTime time)
    {
        var l = new UIEditorialLayout(batch.GraphicsDevice.Viewport);
        UIPaper.StackedSquare(batch, l.Element("Outcome", "Panel"), UITheme.PanelDark);
        Rectangle scene = l.Element("Outcome", "Scene");
        // Both outcomes now use one kind of artwork: a paper-cut strip with a transparent sky.
        // Defeat used to borrow a chapter cell, which put a soft painted rectangle on the card
        // and showed whichever chapter happened to be nearest rather than the loss itself.
        bool ending = _won && _node != null && _node.Endings.Count > 0;
        Texture2D strip = _resources.GetTexture(_won ? "victory_road" : "defeat_ground");
        if (ending || strip == null)
        {
            UIPaper.Scene(batch, scene, ending ? 5 : _won ? 1 : UIPaper.ChapterIndex(_node?.Chapter ?? ""),
                _won ? 1f : .55f);
        }
        else
        {
            DrawStrip(batch, strip, scene);
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

