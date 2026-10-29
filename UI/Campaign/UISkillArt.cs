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
        void Slash(int x) => Fill(x,82, x+5,46, x+35,14, x+30,45);
        void Shield(int x, int w)
        {
            Fill(x,24,x+w/2,14,x+w,24,x+w,62,x+w/2,85,x,62);
            Cut(x+8,30,x+w/2,24,x+w-8,30,x+w-8,57,x+w/2,72,x+8,57);
        }
        switch (type)
        {
            case SkillType.PlusAttack: Slash(30); break;
            case SkillType.DoubleAttack: Slash(13); Slash(49); break;
            case SkillType.PlusDefence: Shield(25,50); break;
            case SkillType.DoubleDefence: Shield(13,42); Shield(47,42); break;
            case SkillType.DoubleHealth:
            case SkillType.TripleHealth:
                Fill(50,83,22,55,22,32,35,22,50,34,65,22,78,32,78,55);
                if (type == SkillType.TripleHealth)
                { Fill(14,27,7,36,7,61,25,78,25,65,15,54); Fill(86,27,93,36,93,61,75,78,75,65,85,54); }
                break;
            case SkillType.Xue:
                Fill(50,12,26,54,25,70,36,84,56,88,74,73,73,56);
                Cut(50,43,63,64,61,76,52,80,54,65); break;
            case SkillType.Shi:
                Fill(28,18,56,12,75,31,58,44,33,38);
                Fill(23,24,29,43,51,52,38,82,16,60); Fill(61,50,81,35,88,57,66,85,45,86); break;
            case SkillType.Long:
                Fill(28,15,65,15,81,34,62,34,68,41,45,50,73,62,64,84,36,89,17,70,36,70,30,63,54,56,26,43,37,31,19,31); break;
            case SkillType.Bian:
                Fill(47,12,70,23,85,46,74,55,61,32,45,25,29,35,20,29);
                Fill(83,58,82,77,52,90,45,76,66,68,66,48,80,43);
                Fill(34,87,14,73,14,45,30,40,29,62,44,70,49,57,61,65); break;
            case SkillType.Tong:
                Fill(20,31,80,31,80,43,20,43); Fill(20,58,80,58,80,70,20,70); break;
            case SkillType.Mo: Fill(12,50,39,22,39,41,64,41,64,20,90,47,64,75,64,57,39,57,39,78); break;
            case SkillType.Seng:
            case SkillType.Dao:
                Fill(50,12,86,75,74,84,50,42,26,84,14,75);
                if (type == SkillType.Dao) Fill(22,48,78,48,84,58,16,58); break;
            case SkillType.Xian:
            case SkillType.SuperImmortal:
                Fill(50,9,71,49,50,88,29,49); Cut(50,28,59,49,50,68,41,49);
                Fill(22,34,32,66,13,53,5,27); Fill(78,34,68,66,87,53,95,27); break;
            case SkillType.Di:
            case SkillType.SuperEmperor:
                Fill(12,31,35,49,50,17,65,49,88,31,78,77,22,77); Cut(27,62,73,62,71,68,29,68); break;
            default:
                Fill(50,10,84,30,84,70,50,90,16,70,16,30); Cut(50,25,71,38,71,62,50,75,29,62,29,38); break;
        }
        if (type is SkillType.SuperGod or SkillType.SuperImmortal or SkillType.SuperEmperor)
            Fill(83,3,89,3,89,11,97,11,97,17,89,17,89,25,83,25,83,17,75,17,75,11,83,11);
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
