#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

public sealed class UIPaperPause : UIControl
{
    private readonly ResourceManager _resources;
    public Texture2D? Backdrop { get; set; }
    public UIPaperPause(ResourceManager resources) : base(-50) { _resources=resources; }
    public static Rectangle Button(UIEditorialLayout layout,int row) => layout.Element("Pause", row switch
        { 0 => "Resume", 1 => "Settings", 2 => "Rules", _ => "MainMenu" });
    public override void Draw(SpriteBatch batch,GameTime time)
    {
        var vp=batch.GraphicsDevice.Viewport;
        if(Backdrop!=null) batch.Draw(Backdrop,vp.Bounds,Color.White);
        else UIPaper.BattleBackground(batch,vp);
        batch.Draw(_resources.Pixel,vp.Bounds,Color.Black*.64f);
        var l=new UIEditorialLayout(vp);
        var panel=l.Element("Pause", "Panel");
        // A short stack of square sheets instead of a single cut-paper shape.
        UIPaper.StackedSquare(batch,panel,UITheme.PanelDark);
        var heading=l.Element("Pause", "Title");
        UIUtils.DrawCenteredFit(batch,_resources.TitleFont,"Paused",heading.Center.ToVector2(),heading.Width,
            l.Scale * 88 / _resources.TitleFont.LineSpacing,UITheme.Ink);
    }
}

