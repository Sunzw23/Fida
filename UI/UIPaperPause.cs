#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

public sealed class UIPaperPause : UIControl
{
    private readonly ResourceManager _resources;
    public Texture2D? Backdrop { get; set; }
    public UIPaperPause(ResourceManager resources) : base(-50) { _resources=resources; }
    public static Rectangle Button(UIEditorialLayout layout,int row) => layout.Portrait
        ? layout.Rect(190,600+row*150,520,110) : layout.Rect(650,465+row*125,620,100);
    public override void Draw(SpriteBatch batch,GameTime time)
    {
        var vp=batch.GraphicsDevice.Viewport;
        if(Backdrop!=null) batch.Draw(Backdrop,vp.Bounds,Color.White);
        else UIPaper.BattleBackground(batch,vp);
        batch.Draw(_resources.Pixel,vp.Bounds,Color.Black*.66f);
        var l=new UIEditorialLayout(vp);
        var panel=l.Portrait ? l.Rect(140,350,620,1020) : l.Rect(570,245,780,820);
        var backing=panel; backing.Offset(-9,9);
        UIPaper.Sheet(batch,backing,UITheme.PanelDark,3,10);
        UIPaper.Sheet(batch,panel,UITheme.Panel,3,10);
        var heading=l.Portrait ? l.Rect(190,425,520,100) : l.Rect(650,315,620,100);
        UIUtils.DrawCenteredFit(batch,_resources.TitleFont,"Paused",heading.Center.ToVector2(),heading.Width,
            UITypography.Scale(_resources.TitleFont,UITextRole.Heading,vp),UITheme.Ink);
    }
}
