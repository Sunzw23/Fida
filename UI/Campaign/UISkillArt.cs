using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Campaign;

/// <summary>Antialiased geometric skill masks; generated once, with no external art dependency.</summary>
public static class UISkillArt
{
    private record Mask(int[] Points, bool Ink = true);
    private static readonly SkillType[] Types = Enum.GetValues<SkillType>();
    private static readonly ConditionalWeakTable<GraphicsDevice, Texture2D> Atlases = new();
    private const int Size = 100;
    public static void Draw(SpriteBatch batch, SkillType skill, Rectangle bounds, Color color)
    {
        int index = Array.IndexOf(Types, skill); if (index < 0) return;
        int side = Math.Min(bounds.Width, bounds.Height);
        batch.Draw(Atlases.GetValue(batch.GraphicsDevice, Create),
            new Rectangle(bounds.Center.X - side / 2, bounds.Center.Y - side / 2, side, side),
            new Rectangle(index * Size, 0, Size, Size), color);
    }
    private static List<Mask> Shapes(SkillType type)
    {
        var shapes = new List<Mask>();
        void Fill(params int[] points) => shapes.Add(new Mask(points));
        void Cut(params int[] points) => shapes.Add(new Mask(points, false));
        // A bold diagonal cut, wide flat base tapering to a point. Kept inside the 100 box so
        // the point never clips on the cell edge.
        void Strike(int x) => Fill(x,90, x+16,94, x+50,14, x+34,10);
        // A pointed heraldic crest: straight top, straight sides, pointed base.
        void Crest() => Fill(14,16, 86,16, 86,56, 50,94, 14,56);
        void Heart() => Fill(50,86, 20,58, 20,34, 34,22, 50,36, 66,22, 80,34, 80,58);
        // The paper punch mark that lifts a base sigil into the super tier.
        void Punch() => Fill(83,3, 89,3, 89,11, 97,11, 97,17, 89,17, 89,25, 83,25,
                             83,17, 75,17, 75,11, 83,11);
        switch (type)
        {
            // Offense: a strike, then a doubled strike, then a fang for the single great blow.
            case SkillType.PlusAttack: Strike(22); break;
            case SkillType.DoubleAttack: Strike(6); Strike(44); break;
            case SkillType.Long:
                Fill(12,14, 88,14, 50,94); Cut(34,46, 66,46, 50,74); break;
            // A dial: the damage can be set to any value.
            case SkillType.Bian:
                Fill(50,8, 70,15, 85,30, 92,50, 85,70, 70,85, 50,92, 30,85, 15,70, 8,50, 15,30, 30,15);
                Cut(50,26, 63,31, 72,40, 76,50, 72,60, 63,69, 50,74, 37,69, 28,60, 24,50, 28,40, 37,31);
                Fill(44,50, 70,16, 78,26, 58,54);
                break;
            // Defense: one crest for reducing by one, the same crest split in half for halving.
            case SkillType.PlusDefence:
                Crest(); Cut(22,32, 78,32, 78,44, 50,66, 22,44); break;
            case SkillType.DoubleDefence:
                Crest(); Cut(14,50, 86,50, 86,58, 14,58); break;
            // A flat faceted block with two facet lines, not a crumpled outline.
            case SkillType.Shi:
                Fill(24,78, 24,40, 50,16, 78,32, 76,72, 46,86);
                Cut(50,16, 42,46, 76,52);
                Cut(24,40, 46,86);
                break;
            // Vitality: one heart, then the same heart banded into three.
            case SkillType.DoubleHealth: Heart(); break;
            case SkillType.TripleHealth:
                Heart();
                Cut(37,36, 45,33, 45,72, 37,75);
                Cut(55,33, 63,36, 63,75, 55,72);
                break;
            case SkillType.Xue:
                Fill(50,8, 24,52, 24,70, 36,86, 50,92, 64,86, 76,70, 76,52);
                Cut(50,34, 63,62, 60,78, 50,84, 54,64); break;
            // Counter: damage returned along the same line.
            case SkillType.Mo:
                Fill(8,50, 34,22, 34,42, 62,42, 62,28, 92,50, 62,72, 62,58, 34,58, 34,78);
                break;
            // Control: two number tiles for "both of your numbers".
            case SkillType.Tong:
                Fill(14,34, 44,34, 44,66, 14,66);
                Fill(56,34, 86,34, 86,66, 56,66);
                break;
            case SkillType.Di:
            case SkillType.SuperEmperor:
                Fill(10,26, 34,48, 50,14, 66,48, 90,26, 82,82, 18,82);
                Cut(26,64, 74,64, 72,72, 28,72);
                break;
            case SkillType.Xian:
            case SkillType.SuperImmortal:
                Fill(50,6, 86,50, 50,94, 14,50); Cut(50,30, 70,50, 50,70, 30,50); break;
            // Extra steps: a chevron for one step, a second chevron for another.
            case SkillType.Seng:
                Fill(50,12, 88,58, 74,70, 50,40, 26,70, 12,58);
                Fill(50,52, 74,82, 62,92, 50,80, 38,92, 26,82);
                break;
            case SkillType.Dao:
                Fill(50,14, 86,58, 72,70, 50,42, 28,70, 14,58);
                Fill(26,80, 74,80, 74,92, 26,92);
                break;
            // God and anything added later: a heavy hexagon sigil with a small hole.
            case SkillType.Shen:
            case SkillType.SuperGod:
            default:
                Fill(50,8, 88,30, 88,70, 50,92, 12,70, 12,30);
                Cut(50,38, 61,44, 61,56, 50,62, 39,56, 39,44);
                break;
        }
        if (type is SkillType.SuperGod or SkillType.SuperImmortal or SkillType.SuperEmperor)
            Punch();
        return shapes;
    }
    private static Texture2D Create(GraphicsDevice device)
    {
        var data = new Color[Types.Length * Size * Size];
        for (int i = 0; i < Types.Length; i++)
        {
            var shapes = Shapes(Types[i]);
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                int coverage = 0;
                for (int sy = 0; sy < 2; sy++) for (int sx = 0; sx < 2; sx++)
                {
                    bool ink = false;
                    foreach (var shape in shapes)
                        if (Contains(shape.Points,x+(sx+.5f)/2,y+(sy+.5f)/2)) ink = shape.Ink;
                    if (ink) coverage++;
                }
                data[y*Types.Length*Size+i*Size+x] = Color.White * (coverage/4f);
            }
        }
        var texture = new Texture2D(device,Types.Length*Size,Size); texture.SetData(data);
        device.Disposing += (_, _) => texture.Dispose(); return texture;
    }
    private static bool Contains(int[] p, float x, float y)
    {
        bool inside = false;
        for (int i=0,j=p.Length-2; i<p.Length; j=i,i+=2)
            if ((p[i+1]>y)!=(p[j+1]>y) && x<(p[j]-p[i])*(y-p[i+1])/(p[j+1]-p[i+1])+p[i]) inside=!inside;
        return inside;
    }
}
