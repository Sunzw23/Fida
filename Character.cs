using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{
    public class Character : Player
    {
        public String Name { get; private set; }
        public Vector2 Position { get; set; }
        public List<Texture2D> Texture2Ds { get; private set; }
        public List<Vector2> Texture2DLocations { get; private set; }
        public List<Vector2> NumberLocations { get; private set; }
        public SpriteFont Font { get; private set; }
        public Color FontColor { get; private set; }
        public Vector2 HealthLocation { get; private set; }


        public Character(int id, int health, string name, SpriteFont font, Color fontColor,
            Vector2 position, List<Texture2D> texture2Ds, List<Vector2> texture2DLocations, 
            List<Vector2> numberLocations, Vector2 healthLocation) : base(id, health)
        {
            Name = name;
            Font = font;
            FontColor = fontColor;
            Position = position;
            Texture2Ds = texture2Ds;
            Texture2DLocations = texture2DLocations;
            NumberLocations = numberLocations;
            HealthLocation = healthLocation;
        }


        public Character(int id, int health, SpriteFont font, Vector2 position) :base(id, health)
        {
            Name = "Anonymous";
            Font = font;
            FontColor = Color.White;
            Position = position;
            Texture2Ds = new List<Texture2D>();
            Texture2DLocations = new List<Vector2>();
            NumberLocations = [new Vector2(-100, 100), new Vector2(100, 100)];
            HealthLocation = new Vector2(0, 200);

        }


        public void Draw(SpriteBatch spriteBatch, MouseState mouseState)
        {
            //spriteBatch.DrawString(Font, "名称: "+ Name, Position, FontColor);
            


            for (int i = 0; i < Texture2Ds.Count; i++)
            {
                spriteBatch.Draw(Texture2Ds[i], Texture2DLocations[i] + Position, Color.White);

            }

            spriteBatch.DrawString(Font, $"{Name}", Position, FontColor);

            spriteBatch.DrawString(Font, "Health: " + Health, HealthLocation + Position, FontColor);

            for (int i = 0; i < 2; i++)
            {
                if (GetNumberRectangle(i).Contains(mouseState.Position))
                {
                    spriteBatch.Draw()
                }
                spriteBatch.DrawString(Font, Numbers[i].ToString(), NumberLocations[i] + Position, FontColor);
            }

            


        }

        public Rectangle GetTargetRectangle()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, 100, 100);
        }

        public Rectangle GetNumberRectangle(int index)
        {
            return new Rectangle((int)(Position.X + NumberLocations[index].X), (int)(Position.Y + NumberLocations[index].Y), 100, 100);
        }
    }
}
