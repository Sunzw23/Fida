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
        : duel ? layout.Rect(650, 1030, 620, 90) : layout.Rect(primary ? 990 : 450, 1030, 480, 90);
    public override void Draw(SpriteBatch batch, GameTime time)
    {
        var l = new UIEditorialLayout(batch.GraphicsDevice.Viewport); bool p = l.Portrait;
        UIPaper.Sheet(batch, p ? l.Rect(40, 230, 820, 1230) : l.Rect(315, 175, 1300, 1000), UITheme.PanelDark, 4, 12);
        UIPaper.Sheet(batch, p ? l.Rect(50, 220, 800, 1230) : l.Rect(330, 160, 1270, 1000), UITheme.Panel, 4, 10);
        Rectangle scene = p ? l.Rect(80, 850, 740, 260) : l.Rect(450, 720, 1020, 270);
        UIPaper.Scene(batch, scene, _won && _node?.Endings.Count > 0 ? 5 : _won ? 1 : UIPaper.ChapterIndex(_node?.Chapter ?? ""), _won ? 1 : .55f);
        string title = _duel ? _won ? "Victory" : "Defeat" : _won && _node?.Endings.Count > 0 ? "At the Ferry" : _won ? "Node Cleared" : "Defeat";
        Rectangle heading = p ? l.Rect(80, 390, 740, 100) : l.Rect(450, 360, 1020, 95);
        var seal = p ? l.Rect(390,240,120,120) : l.Rect(880,190,160,140);
        UIPaper.Sheet(batch,seal,UIPaper.Face,3,10);
        void Stroke(Vector2 a,Vector2 b)
        {
            Vector2 delta=b-a;
            batch.Draw(_resources.Pixel,a,null,UIPaper.Ink,(float)System.Math.Atan2(delta.Y,delta.X),Vector2.Zero,
                new Vector2(delta.Length(),System.Math.Max(4,seal.Width*.08f)),SpriteEffects.None,0);
        }
        Vector2 mark=seal.Center.ToVector2();
        if(_won) { Stroke(mark+new Vector2(-seal.Width*.25f,0),mark+new Vector2(-seal.Width*.04f,seal.Height*.18f)); Stroke(mark+new Vector2(-seal.Width*.04f,seal.Height*.18f),mark+new Vector2(seal.Width*.27f,-seal.Height*.2f)); }
        else { Stroke(mark-new Vector2(seal.Width*.2f,seal.Height*.2f),mark+new Vector2(seal.Width*.2f,seal.Height*.2f)); Stroke(mark+new Vector2(-seal.Width*.2f,seal.Height*.2f),mark+new Vector2(seal.Width*.2f,-seal.Height*.2f)); }
        var font = _resources.TitleFont; Vector2 size = font.MeasureString(UIFontGuard.Sanitize(font, title));
        float scale = UITextRenderer.FitScale(size, heading.Width, heading.Height, l.Scale * .8f);
        UITextRenderer.DrawString(batch, font, title, heading.Center.ToVector2() - size * scale / 2, UITheme.Ink, scale: scale);
        UITextRenderer.DrawParagraph(batch, _resources.Font, _won && _node?.Endings.Count > 0 ? "The boat is ready." : _message,
            p ? l.Rect(80, 530, 740, 150) : l.Rect(450, 490, 1020, 90), UITheme.Ink, centered:true);
        if (_won && _node?.Rewards.Count > 0)
        {
            var reward = p ? l.Rect(80, 710, 740, 90) : l.Rect(560, 610, 800, 75);
            UIPaper.Sheet(batch, reward, UIPaper.Reward, 2, 6);
            reward.Inflate(-12, -8);
            UITextRenderer.DrawParagraph(batch, _resources.CaptionFont, "Unlocked: " + _node.RewardText, reward, UIPaper.Ink, centered:true);
        }
    }
}
