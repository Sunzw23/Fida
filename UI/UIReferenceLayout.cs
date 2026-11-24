using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>Online screens share the settings header, margins and design canvas.</summary>
public sealed class UIReferenceLayout
{
    public UIEditorialLayout Canvas { get; }
    public bool Portrait => Canvas.Portrait;
    public UIReferenceLayout(Viewport viewport) { Canvas = new UIEditorialLayout(viewport); }
    public UIReferenceLayout(int width, int height) { Canvas = new UIEditorialLayout(width, height); }
    public Rectangle HostAction => Portrait ? Canvas.Rect(80, 520, 740, 100) : Canvas.Rect(150, 900, 700, 100);
    public Rectangle JoinAction => Portrait ? Canvas.Rect(80, 1260, 740, 100) : Canvas.Rect(1070, 900, 700, 100);
    public Rectangle Code => Portrait ? Canvas.Rect(80, 1000, 740, 155) : Canvas.Rect(1070, 350, 700, 210);
    public Rectangle SessionAction => Portrait ? Canvas.Rect(80, 1200, 740, 100) : Canvas.Rect(700, 930, 520, 100);
    /// <summary>Connected session page: the only action, spanning the card's content column.</summary>
    public Rectangle DisconnectAction => Portrait ? Canvas.Rect(90, 940, 720, 100) : Canvas.Rect(570, 752, 780, 100);
}
