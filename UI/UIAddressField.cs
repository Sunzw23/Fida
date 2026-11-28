#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI
{
    /// <summary>
    /// Single-line text field for a direct-connect endpoint ("192.168.1.7:8888").
    ///
    /// Serverless play hands the guest nothing but what the host reads off its own screen, so
    /// this field has to accept an IP literal with an optional port. Like
    /// <see cref="UIRoomCodeInput"/> it polls the keyboard itself and stays outside the
    /// controller focus chain (CanFocus stays false).
    /// </summary>
    public class UIAddressField : UIControl
    {
        public const int MaxLength = 64;

        private readonly Rectangle _bounds;
        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;

        private string _value = string.Empty;
        private string _placeholder;
        private bool _focused;
        private HashSet<Keys> _lastKeys = new HashSet<Keys>();
#if FIDA_ANDROID
        private bool _mobileKeyboardPending;
        private string? _pendingMobileValue;
#endif

        public UIAddressField(int id, Rectangle bounds, string placeholder, Texture2D pixel, SpriteFont font)
            : base(id)
        {
            _bounds = bounds;
            _placeholder = placeholder ?? string.Empty;
            _pixel = pixel;
            _font = font;
        }

        /// <summary>Current value, normalised to upper case.</summary>
        public string Value => _value;

        public string Placeholder
        {
            get => _placeholder;
            set => _placeholder = value ?? string.Empty;
        }

        public bool IsInputActive => _focused;

        /// <summary>Fired when Enter is pressed or the code calls <see cref="Submit"/>.</summary>
        public event Action<string>? Submitted;

        public Rectangle Bounds => _bounds;

        public override Rectangle? GetScreenBounds(Viewport viewport) => IsVisible ? _bounds : null;

        /// <summary>Called while page transitions block normal input so no keys are replayed.</summary>
        public void SynchronizeInput() => _lastKeys = new HashSet<Keys>(Keyboard.GetState().GetPressedKeys());

        public void Clear() => _value = string.Empty;

        public void Submit() => Submitted?.Invoke(_value);

        public void SetValue(string? value) =>
            _value = new string((value ?? string.Empty).Where(IsAllowed).Take(MaxLength).ToArray())
                .ToUpperInvariant();

        public override void Update(MouseState mouseState)
        {
            if (!IsVisible) return;
            if (mouseState.LeftButton != ButtonState.Pressed) return;

            bool inside = _bounds.Contains(new Point(mouseState.X, mouseState.Y));
            if (inside && !_focused)
            {
                _focused = true;
#if FIDA_ANDROID
                ShowMobileKeyboard();
#endif
            }
            else if (!inside && _focused)
            {
                _focused = false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsVisible) return;
#if FIDA_ANDROID
            if (_pendingMobileValue != null)
            {
                string submitted = _pendingMobileValue;
                _pendingMobileValue = null;
                SetValue(submitted);
                _focused = false;
                Submitted?.Invoke(_value);
            }
            return;
#else
            UpdateKeyboard(Keyboard.GetState());
#endif
        }

        /// <summary>Processes keyboard edges separately from polling, also usable by tests.</summary>
        public void UpdateKeyboard(KeyboardState keyboard)
        {
            if (!IsVisible) return;
            Keys[] pressed = keyboard.GetPressedKeys();

            if (!_focused)
            {
                _lastKeys = new HashSet<Keys>(pressed);
                return;
            }

            foreach (Keys key in pressed)
            {
                if (_lastKeys.Contains(key)) continue;

                if (key == Keys.Back)
                {
                    if (_value.Length > 0) _value = _value[..^1];
                    continue;
                }
                if (key == Keys.Enter)
                {
                    Submit();
                    continue;
                }
                if (key == Keys.Escape)
                {
                    _focused = false;
                    continue;
                }

                char? character = CharacterFor(key, keyboard);
                if (character is { } c && IsAllowed(c) && _value.Length < MaxLength)
                    _value += char.ToUpperInvariant(c);
            }
            _lastKeys = new HashSet<Keys>(pressed);
        }

        private static char? CharacterFor(Keys key, KeyboardState keyboard)
        {
            if (key is >= Keys.A and <= Keys.Z) return (char)('A' + (key - Keys.A));
            if (key is >= Keys.D0 and <= Keys.D9) return (char)('0' + (key - Keys.D0));
            if (key is >= Keys.NumPad0 and <= Keys.NumPad9) return (char)('0' + (key - Keys.NumPad0));

            bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
            return key switch
            {
                Keys.OemPeriod or Keys.Decimal => '.',
                Keys.OemMinus or Keys.Subtract => '-',
                Keys.OemOpenBrackets => '[',
                Keys.OemCloseBrackets => ']',
                Keys.OemSemicolon => shift ? ':' : ';',
                _ => null
            };
        }

        private static bool IsAllowed(char c) =>
            c is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z'
            or '.' or ':' or '[' or ']' or '-' or '%';

#if FIDA_ANDROID
        private void ShowMobileKeyboard()
        {
            if (_mobileKeyboardPending || KeyboardInput.IsVisible) return;
            _mobileKeyboardPending = true;
            KeyboardInput.Show("Host address", "Type the address shown on the host screen, for example 192.168.1.7:8888", _value, false)
                .ContinueWith(task =>
                {
                    _mobileKeyboardPending = false;
                    if (task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion && task.Result != null)
                        _pendingMobileValue = task.Result;
                });
        }
#endif

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsVisible) return;

            UIPaper.Sheet(spriteBatch, _bounds, UIPaper.Face, _focused ? 4 : 2, 6);
            var layout = new UIEditorialLayout(spriteBatch.GraphicsDevice.Viewport);
            float preferred = UITextRenderer.FitScale(_font.MeasureString("0"), _bounds.Width - 40,
                _bounds.Height - 24, layout.Scale * 46 / _font.LineSpacing);

            bool empty = _value.Length == 0;
            string text = empty ? Placeholder : _value;
            if (text.Length == 0) text = "IP:PORT";
            text = UIFontGuard.Sanitize(_font, text.ToUpperInvariant());

            // Keep the caret visible: show the tail of an address longer than the field.
            string shown = text;
            float available = Math.Max(1, _bounds.Width - 40);
            if (!empty)
            {
                while (shown.Length > 1 && _font.MeasureString(shown).X * preferred > available)
                    shown = shown[1..];
            }

            Vector2 size = _font.MeasureString(shown);
            float scale = UITextRenderer.FitScale(size, available, _bounds.Height - 24, preferred);
            float x = _bounds.Left + 20;
            float y = _bounds.Center.Y - size.Y * scale / 2f;
            Color ink = empty ? UIPaper.MutedInk : UIPaper.Ink;
            UITextRenderer.DrawString(spriteBatch, _font, shown, new Vector2(x, y), ink, 0, Vector2.Zero, scale, SpriteEffects.None, 0);

            if (_focused && (gameTime.TotalGameTime.TotalMilliseconds / 400) % 2 == 0)
            {
                float caretX = x + size.X * scale;
                int caretHeight = Math.Max(12, (int)(_font.LineSpacing * scale));
                spriteBatch.Draw(_pixel, new Rectangle((int)caretX + 2,
                    _bounds.Center.Y - caretHeight / 2, 2, caretHeight), UIPaper.Ink);
            }
        }
    }
}
