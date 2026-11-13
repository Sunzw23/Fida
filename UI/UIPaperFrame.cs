using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

public enum UIPaperFrameShape { Rectangle, Header, Fold, Node, Tab, Seal, Bolt, FoldCrease, FoldTurn, RouteNode }

/// <summary>Reusable paper silhouettes. Geometry and material are independent of page data and input.</summary>
public static class UIPaperFrame
{
    private sealed class Material
    {
        public Texture2D[] Shapes = null!;
    }
    private static readonly ConditionalWeakTable<GraphicsDevice, Material> Cache = new();

    private static Material Create(GraphicsDevice device)
    {
        Vector2[][] polygons = {
            new[] { new Vector2(0,0), new(1,0), new(1,1), new(0,1) },
            new[] { new Vector2(0,0), new(1,0), new(1,1), new(.98f,.82f), new(.575f,.82f), new(.56f,1), new(0,1) },
            new[] { new Vector2(.19f,0), new(1,.12f), new(.68f,1), new(0,1) },
            new[] { new Vector2(.025f,0), new(.975f,0), new(1,.075f), new(1,.955f), new(.98f,1), new(.018f,1), new(0,.96f), new(0,.05f) },
            new[] { new Vector2(1,0), new(1,1), new(0,.93f), new(0,.09f) },
            new[] { new Vector2(.5f,0), new(1,.22f), new(1,.78f), new(.5f,1), new(0,.78f), new(0,.22f) },
            new[] { new Vector2(.86f,0), new(.12f,.47f), new(.50f,.40f), new(.05f,1), new(.96f,.37f), new(.60f,.43f) },
            new[] { new Vector2(.19f,0), new(.46f,.67f), new(0,1) },
            new[] { new Vector2(0,1), new(.46f,.67f), new(.68f,1) },
            // A clipped paper ticket, with a larger shoulder and a shallow lower fold.
            new[] { new Vector2(.045f,0), new(.95f,0), new(1,.12f), new(1,.92f), new(.965f,1), new(.035f,1), new(0,.92f), new(0,.09f) }
        };
        var result = new Material { Shapes = new Texture2D[polygons.Length] };
        for (int shape = 0; shape < polygons.Length; shape++)
        {
            int width=shape==(int)UIPaperFrameShape.Header ? 512 : shape==(int)UIPaperFrameShape.Tab ? 64 : 256;
            int height=shape==(int)UIPaperFrameShape.Header ? 64 : shape==(int)UIPaperFrameShape.Node || shape==(int)UIPaperFrameShape.RouteNode ? 128 :
                shape==(int)UIPaperFrameShape.Rectangle || shape==(int)UIPaperFrameShape.Fold ||
                shape>=(int)UIPaperFrameShape.FoldCrease ? 384 : 256;
            var pixels = new Color[width * height];
            Vector2[] polygon = polygons[shape];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int coverage = 0;
                for (int sy = 0; sy < 2; sy++)
                for (int sx = 0; sx < 2; sx++)
                {
                    float px = (x + .25f + sx * .5f) / width, py = (y + .25f + sy * .5f) / height;
                    bool inside = false;
                    for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
                    {
                        Vector2 a = polygon[i], b = polygon[j];
                        if ((a.Y > py) != (b.Y > py) && px < (b.X - a.X) * (py - a.Y) / (b.Y - a.Y) + a.X)
                            inside = !inside;
                    }
                    if (inside) coverage++;
                }
                pixels[y * width + x] = Color.White * (coverage / 4f);
            }
            result.Shapes[shape] = new Texture2D(device, width, height,false,SurfaceFormat.Color);
            result.Shapes[shape].SetData(pixels);
        }
        device.Disposing += (_, _) => { foreach (var texture in result.Shapes) texture.Dispose(); };
        return result;
    }


    public static void Draw(SpriteBatch batch, Rectangle bounds, Color color,
        UIPaperFrameShape shape = UIPaperFrameShape.Rectangle, float depth = 3, float rotation = 0)
    {
        if(bounds.Width<1 || bounds.Height<1)return;
        var material=Cache.GetValue(batch.GraphicsDevice,Create);
        Texture2D mask=material.Shapes[(int)shape];
        Vector2 center=bounds.Center.ToVector2(), origin=new(mask.Width/2f,mask.Height/2f);
        Vector2 scale=new(bounds.Width/(float)mask.Width,bounds.Height/(float)mask.Height);
        void Render(Texture2D texture,Vector2 offset,Color tint) => batch.Draw(texture,center+offset,null,tint,
            rotation,origin,scale,SpriteEffects.None,0);
        if(depth>0)
        {
            Vector2 offset=new(.35f,Math.Min(1.5f,depth*.35f));
            Render(mask,offset,Color.Black*.12f);
            for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
                Render(mask,offset+new Vector2(x,y)*.65f,Color.Black*.012f);
        }
        // The silhouette is the whole surface now; the paper pass supplies grain.
        Render(mask,new Vector2(0,-.5f),Color.Lerp(color,Color.White,.035f));
        Render(mask,Vector2.Zero,color);
    }

    /// <summary>A folded backing sheet with two independently lit planes and a turned underside.</summary>
    public static void Origami(SpriteBatch batch,Rectangle bounds,float scale)
    {
        Draw(batch,bounds,new Color(22,40,55),UIPaperFrameShape.Fold,1.5f*scale);
        Draw(batch,bounds,new Color(30,51,67),UIPaperFrameShape.FoldCrease,0);
        Draw(batch,bounds,new Color(37,58,73),UIPaperFrameShape.FoldTurn,0);
    }

    public static void Stack(SpriteBatch batch,Rectangle front,Rectangle far,Rectangle near,float scale)
    {
        // Two backing sheets plus the main face: three visible sheets in total.
        BackingSheet(-16,22,new Color(35,53,65),-.009f);
        BackingSheet(-8,11,new Color(64,82,92),.007f);
        void BackingSheet(float left,float drop,Color color,float angle)
        {
            var sheet=front;
            sheet.Offset((int)(left*scale),(int)(drop*scale));
            var shadow=sheet; shadow.Offset(0,Math.Max(1,(int)scale));
            Draw(batch,shadow,new Color(19,32,43),depth:0,rotation:angle);
            Draw(batch,sheet,color,depth:.5f*scale,rotation:angle);
        }
        Draw(batch,front,new Color(21,35,46),depth:.5f*scale);
    }
}


