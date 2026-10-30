using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>Rules and online screens share the settings header, margins and design canvas.</summary>
public sealed class UIReferenceLayout
{
    public UIEditorialLayout Canvas { get; }
    public bool Portrait => Canvas.Portrait;
    public UIReferenceLayout(Viewport viewport) { Canvas = new UIEditorialLayout(viewport); }
    public UIReferenceLayout(int width, int height) { Canvas = new UIEditorialLayout(width, height); }
    public Rectangle Chapter(int index) => Portrait
        ? Canvas.Rect(65 + index % 3 * 260, 220 + index / 3 * 90, 250, 74)
        : Canvas.Rect(150, 300 + index * 125, 330, 90);
    public Rectangle Content => Portrait ? Canvas.Rect(80, 450, 740, 920) : Canvas.Rect(600, 290, 1150, 680);
    public Rectangle Previous => Portrait ? Canvas.Rect(500, 1440, 90, 80) : Canvas.Rect(1400, 1040, 90, 80);
    public Rectangle Next => Portrait ? Canvas.Rect(730, 1440, 90, 80) : Canvas.Rect(1660, 1040, 90, 80);
    public Rectangle Page => Portrait ? Canvas.Rect(600, 1440, 120, 80) : Canvas.Rect(1510, 1040, 120, 80);
    public Rectangle HostAction => Portrait ? Canvas.Rect(80, 520, 740, 100) : Canvas.Rect(150, 900, 700, 100);
    public Rectangle JoinAction => Portrait ? Canvas.Rect(80, 1260, 740, 100) : Canvas.Rect(1070, 900, 700, 100);
    public Rectangle Code => Portrait ? Canvas.Rect(80, 1000, 740, 155) : Canvas.Rect(1070, 570, 700, 180);
    public Rectangle SessionAction => Portrait ? Canvas.Rect(80, 1200, 740, 100) : Canvas.Rect(600, 900, 720, 100);
}
