using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

/// <summary>Small neutral lobby identities, built from six broad paper shapes.</summary>
public static class UIPaperAvatar
{
    private static readonly ConditionalWeakTable<GraphicsDevice, Texture2D[]> Cache = new();
    public static void Draw(SpriteBatch batch, Rectangle bounds, int variant = 0)
    {
        Texture2D texture = Cache.GetValue(batch.GraphicsDevice, Create)[Math.Abs(variant) % 2];
        float scale = Math.Min(bounds.Width / 512f, bounds.Height / 640f);
        var size = new Vector2(512,640) * scale;
        var at = new Vector2(bounds.Center.X, bounds.Bottom) - new Vector2(size.X/2,size.Y);
        batch.Draw(texture, at + new Vector2(3,5), null, Color.Black * .25f,0,Vector2.Zero,scale,SpriteEffects.None,0);
        batch.Draw(texture, at, null, Color.White,0,Vector2.Zero,scale,SpriteEffects.None,0);
    }
    private static Texture2D[] Create(GraphicsDevice device)
    {
        var result = new Texture2D[2];
        for(int variant=0;variant<2;variant++)
        {
            var pixels = new Color[512*640];
            void Shape(Color color, params int[] polygon)
            {
                for(int y=0;y<640;y++) for(int x=0;x<512;x++)
                {
                    float px=(x+.5f)/4,py=(y+.5f)/4;
                    bool inside=false;
                    for(int i=0,j=polygon.Length-2;i<polygon.Length;j=i,i+=2)
                        if((polygon[i+1]>py)!=(polygon[j+1]>py) && px<(polygon[j]-polygon[i])*(py-polygon[i+1])/(polygon[j+1]-polygon[i+1])+polygon[i]) inside=!inside;
                    if(inside) pixels[y*512+x]=color;
                }
            }
            Shape(new Color(222,216,200), 20,110,48,96,80,96,108,110,119,153,9,153);
            Shape(new Color(33,54,70), 45,101,64,115,83,101,100,153,28,153);
            Shape(new Color(206,201,187), 48,79,80,79,80,103,64,115,48,103);
            Shape(new Color(239,234,220), 34,35,57,19,85,26,97,48,88,79,64,96,40,79);
            Shape(new Color(214,209,194), 64,33,85,26,97,48,88,79,64,96);
            if(variant==0) Shape(new Color(246,240,224), 30,37,53,12,92,21,99,44,75,31,56,43,32,50);
            else Shape(new Color(244,238,222), 31,35,43,11,80,13,101,33,96,52,77,30,55,32,34,61);
            var texture = new Texture2D(device,512,640); texture.SetData(pixels); result[variant]=texture;
        }
        device.Disposing += (_,_) => { foreach(var texture in result) texture.Dispose(); };
        return result;
    }
}
