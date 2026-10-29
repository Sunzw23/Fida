using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI
{
    public class UILabel: UIControl
    {
        string Text { get; set; }
        Color Color { get; set; } = UITheme.Ink;
        Vector2 Position { get; set; }
        SpriteFont Font { get; set; }

        /// <summary>Optional text width constraint. Zero means unconstrained.</summary>
        public float MaximumWidth { get; set; }
        /// <summary>Optional text height constraint. Zero means unconstrained.</summary>
        public float MaximumHeight { get; set; }
        public float MinimumScale { get; set; } = 0.58f;
        public UITextRole? TypographyRole { get; set; }

        public UILabel(int id, string text, Vector2 position, SpriteFont font) : base(id)
        {
            Text = text;
            Position = position;
            Font = font;
        }

        public void SetColor(Color color)
        {
            Color = color;
        }

        public override void Draw(SpriteBatch sb, GameTime gameTime)
        {
#if FIDA_ANDROID
            DrawAt(sb, gameTime, Position, 1f);
#else
            DrawAt(sb, gameTime, Position, UIDesktopLayout.Scale(sb.GraphicsDevice.Viewport));
#endif
        }

        public void DrawAt(SpriteBatch sb, GameTime gameTime, Vector2 position, float visualScale)
        {
            if (!IsVisible)
            {
                return; // 如果不可见，则不绘制
            }
            if (string.IsNullOrEmpty(Text))
            {
                return;
            }

            string displayText = UIFontGuard.Sanitize(Font, Text);
            var (rect, scale) = Measure(displayText, position, visualScale, sb.GraphicsDevice.Viewport);
            UITextRenderer.DrawString(sb, Font, displayText, new Vector2(rect.Left, rect.Top), Color, 0f, Vector2.Zero, scale,
                SpriteEffects.None, 0f);
        }

        public override Rectangle? GetScreenBounds(Viewport viewport)
        {
            if (!IsVisible || string.IsNullOrEmpty(Text)) return null;
#if FIDA_ANDROID
            float visualScale = 1f;
#else
            float visualScale = UIDesktopLayout.Scale(viewport);
#endif
            string displayText = UIFontGuard.Sanitize(Font, Text);
            return Measure(displayText, Position, visualScale, viewport).Rect;
        }

        /// <summary>
        /// 标签实际绘制矩形与缩放因子。Draw 与 GetScreenBounds 共用同一份几何计算，
        /// 保证布局检测器读到的就是渲染器画出的范围。
        /// </summary>
        private (Rectangle Rect, float Scale) Measure(string displayText, Vector2 position, float visualScale, Viewport viewport)
        {
            var textSize = Font.MeasureString(displayText);
            float widthScale = MaximumWidth > 0
                ? MaximumWidth / MathHelper.Max(1f, textSize.X)
                : float.PositiveInfinity;
            float heightScale = MaximumHeight > 0
                ? MaximumHeight / MathHelper.Max(1f, textSize.Y)
                : float.PositiveInfinity;
            // MaximumWidth/Height are already physical layout bounds.
            // Fit once; readability preferences must not override either hard bound.
            float preferred = TypographyRole.HasValue ? UITypography.Scale(Font, TypographyRole.Value, viewport) : visualScale;
            float scale = MathHelper.Min(preferred, MathHelper.Min(widthScale, heightScale));
            var pos = new Vector2(
                position.X - textSize.X * scale / 2,
                position.Y - textSize.Y * scale / 2
            );
            pos = UITextRenderer.Snap(pos);
            return (new Rectangle((int)pos.X, (int)pos.Y,
                System.Math.Max(1, (int)System.Math.Round(textSize.X * scale)),
                System.Math.Max(1, (int)System.Math.Round(textSize.Y * scale))), scale);
        }



        public override void Update(string newText)
        {
            Text = newText;
        }



    }
}
