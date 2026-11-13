using System;
using System.Runtime.CompilerServices;
using Fida.Logic;
using Fida.Campaign;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Campaign;

/// <summary>Compact status symbols; skill artwork retains the original letter medallions.</summary>
internal static class UISkillSymbols
{
    private static readonly ConditionalWeakTable<GraphicsDevice,Texture2D> LockMasks = new();

    private static Texture2D CreateLockMask(GraphicsDevice device)
    {
        const int size=128;
        var data=new Color[size*size];
        static bool Inside(float x,float y)
        {
            // Rounded solid body, an arched shackle, and a transparent keyhole.
            float dx=Math.Max(Math.Abs(x-.5f)-.245f,0);
            float dy=Math.Max(Math.Abs(y-.675f)-.175f,0);
            bool body=dx*dx+dy*dy<=.055f*.055f;
            float r=(x-.5f)*(x-.5f)+(y-.34f)*(y-.34f);
            bool arch=y<=.34f && r<=.225f*.225f && r>=.14f*.14f;
            bool legs=y>=.34f && y<=.5f && Math.Abs(x-.5f)>=.14f && Math.Abs(x-.5f)<=.225f;
            bool keyhole=(x-.5f)*(x-.5f)+(y-.635f)*(y-.635f)<.057f*.057f ||
                (y>=.635f && y<=.775f && Math.Abs(x-.5f)<.026f);
            return (body || arch || legs) && !keyhole;
        }
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            int coverage=0;
            for(int sy=0;sy<4;sy++)for(int sx=0;sx<4;sx++)
                if(Inside((x+(sx+.5f)/4)/size,(y+(sy+.5f)/4)/size))coverage++;
            data[y*size+x]=Color.White*(coverage/16f);
        }
        var texture=new Texture2D(device,size,size);
        texture.SetData(data);
        device.Disposing+=(_,_)=>texture.Dispose();
        return texture;
    }

    private static void Line(SpriteBatch batch, Texture2D pixel, Vector2 a, Vector2 b, Color color, float width = 3)
    {
        Vector2 delta = b - a;
        batch.Draw(pixel, a, null, color, MathF.Atan2(delta.Y, delta.X), Vector2.Zero,
            new Vector2(delta.Length(), width), SpriteEffects.None, 0);
    }

    public static void Status(SpriteBatch batch, Texture2D pixel, Rectangle bounds,
        CampaignSkillAvailability status, Color color)
    {
        Vector2 P(float x, float y) => new(bounds.X + x * bounds.Width, bounds.Y + y * bounds.Height);
        if (status == CampaignSkillAvailability.Locked)
        {
            int side=Math.Min(bounds.Width,bounds.Height);
            var area=new Rectangle(bounds.Center.X-side/2,bounds.Center.Y-side/2,side,side);
            batch.Draw(LockMasks.GetValue(batch.GraphicsDevice,CreateLockMask),area,color);
        }
        else if (status == CampaignSkillAvailability.Selected)
        {
            Line(batch,pixel,P(.15f,.5f),P(.4f,.75f),color);
            Line(batch,pixel,P(.4f,.75f),P(.85f,.2f),color);
        }
        else if (status == CampaignSkillAvailability.Available)
        {
            Line(batch,pixel,P(.2f,.5f),P(.8f,.5f),color);
            Line(batch,pixel,P(.5f,.2f),P(.5f,.8f),color);
        }
        else
        {
            Line(batch,pixel,P(.2f,.2f),P(.8f,.8f),color);
            Line(batch,pixel,P(.2f,.8f),P(.8f,.2f),color);
        }
    }

    public static void Play(SpriteBatch batch, Texture2D pixel, Vector2 center, Color color)
    {
        Line(batch, pixel, center + new Vector2(-7, -10), center + new Vector2(7, 0), color, 2);
        Line(batch, pixel, center + new Vector2(7, 0), center + new Vector2(-7, 10), color, 2);
    }
}
