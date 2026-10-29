using System;
using Fida.Logic;
using Fida.Campaign;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI.Campaign;

/// <summary>Compact status symbols; skill artwork retains the original letter medallions.</summary>
internal static class UISkillSymbols
{
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
            UIUtils.DrawRectangleOutlines(batch, new Rectangle(bounds.X + bounds.Width/5,
                bounds.Y + bounds.Height/2, bounds.Width*3/5, bounds.Height/2), color, 2, pixel);
            Line(batch,pixel,P(.35f,.5f),P(.35f,.18f),color,2);
            Line(batch,pixel,P(.35f,.18f),P(.65f,.18f),color,2);
            Line(batch,pixel,P(.65f,.18f),P(.65f,.5f),color,2);
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
