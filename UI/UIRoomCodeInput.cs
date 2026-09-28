using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI
{
    /// <summary>
    /// 房间码输入框：键盘输入 4 位房间码（大写字母/数字），
    /// 点击框内聚焦，Enter 提交（触发 <see cref="Submitted"/>）。
    /// 自给自足：Update(GameTime) 读键盘，Update(MouseState) 处理点击聚焦，
    /// 不依赖手柄焦点系统（CanFocus 保持 false）。
    /// </summary>
    public class UIRoomCodeInput : UIControl
    {
        public const int MaxLen = 4;

        private readonly Rectangle _bounds;
        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;

        private string _value = string.Empty;
        private string _placeholder;
        private bool _focused;
        private HashSet<Keys> _lastKeys = new HashSet<Keys>();
#if FIDA_ANDROID
        private bool _mobileKeyboardPending;
        private string _pendingMobileValue;
#endif

        /// <summary>当前输入值（已归一化为大写）</summary>
        public string Value => _value;

        /// <summary>占位提示文本（值为空时显示）</summary>
        public string Placeholder
        {
            get => _placeholder;
            set => _placeholder = value ?? string.Empty;
        }

        /// <summary>是否聚焦（点击框内）</summary>
        public bool IsInputActive => _focused;

        /// <summary>Enter 提交时触发（参数为当前值）</summary>
        public event Action<string> Submitted;

        /// <summary>输入框屏幕矩形（加载时由设计坐标映射而来）</summary>
        public Rectangle Bounds => _bounds;

        public override Rectangle? GetScreenBounds(Viewport viewport) =>
            IsVisible ? _bounds : null;


        public UIRoomCodeInput(int id, Rectangle bounds, string placeholder, Texture2D pixel, SpriteFont font)
            : base(id)
        {
            _bounds = bounds;
            _placeholder = placeholder ?? string.Empty;
            _pixel = pixel;
            _font = font;
        }

        /// <summary>点击框内聚焦，点击框外失焦</summary>
        public override void Update(MouseState mouseState)
        {
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                var pos = new Point(mouseState.X, mouseState.Y);
                bool inside = _bounds.Contains(pos);
                if (inside && !_focused)
                {
                    _focused = true;
#if FIDA_ANDROID
                    ShowMobileKeyboard();
#endif
                }
                else if (!inside && _focused)
                    _focused = false;
            }
        }

        /// <summary>聚焦时读取键盘：字母/数字输入、Backspace、Enter 提交、Esc 失焦</summary>
        public override void Update(GameTime gameTime)
        {
#if FIDA_ANDROID
            if (_pendingMobileValue != null)
            {
                string submitted = _pendingMobileValue;
                _pendingMobileValue = null;
                _value = new string(submitted.ToUpperInvariant()
                    .Where(char.IsLetterOrDigit).Take(MaxLen).ToArray());
                _focused = false;
                if (_value.Length == MaxLen) Submitted?.Invoke(_value);
            }
            return;
#else
            var pressed = Keyboard.GetState().GetPressedKeys();

            if (!_focused)
            {
                _lastKeys = new HashSet<Keys>(pressed);
                return;
            }

            // 仅处理本帧新按下的键（排除上一帧仍按着的，避免长按重复叠加）
            foreach (Keys key in pressed)
            {
                if (_lastKeys.Contains(key)) continue;

                if (key >= Keys.A && key <= Keys.Z)
                {
                    if (_value.Length < MaxLen)
                        _value += (char)('A' + (key - Keys.A));
                }
                else if (key >= Keys.D0 && key <= Keys.D9)
                {
                    if (_value.Length < MaxLen)
                        _value += (char)('0' + (key - Keys.D0));
                }
                else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                {
                    if (_value.Length < MaxLen)
                        _value += (char)('0' + (key - Keys.NumPad0));
                }
                else if (key == Keys.Back)
                {
                    if (_value.Length > 0)
                        _value = _value.Substring(0, _value.Length - 1);
                }
                else if (key == Keys.Enter)
                {
                    if (_value.Length == MaxLen)
                        Submitted?.Invoke(_value);
                }
                else if (key == Keys.Escape)
                {
                    _focused = false;
                }
            }
            _lastKeys = new HashSet<Keys>(pressed);
#endif
        }

#if FIDA_ANDROID
        private void ShowMobileKeyboard()
        {
            if (_mobileKeyboardPending || KeyboardInput.IsVisible) return;
            _mobileKeyboardPending = true;
            KeyboardInput.Show("Join Game", "Enter the 4-character room code", _value, false)
                .ContinueWith(task =>
                {
                    _mobileKeyboardPending = false;
                    if (task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion && task.Result != null)
                        _pendingMobileValue = task.Result;
                });
        }
#endif

        /// <summary>清空输入</summary>
        public void Clear()
        {
            _value = string.Empty;
        }

        public override void Draw(SpriteBatch sb, GameTime gameTime)
        {
            if (!IsVisible) return;

            Color accent = _focused ? UITheme.AccentGreen : UITheme.Gold;
            sb.Draw(_pixel, _bounds, UITheme.PanelLight);
            sb.Draw(_pixel, new Rectangle(_bounds.Left, _bounds.Top, 5, _bounds.Height), accent);

            // Compact prompt + four equally spaced cells make the expected code length
            // immediately legible and avoid the appearance of a generic text field.
            string prompt = UIFontGuard.Sanitize(_font,
                string.IsNullOrWhiteSpace(_placeholder) ? "ROOM CODE" : _placeholder.ToUpperInvariant());
            Vector2 promptSize = _font.MeasureString(prompt);
            float promptScale = MathHelper.Min(0.52f,
                MathHelper.Min((_bounds.Width - 28f) / Math.Max(1f, promptSize.X),
                    (_bounds.Height * 0.28f) / Math.Max(1f, promptSize.Y)));
            sb.DrawString(_font, prompt,
                new Vector2(_bounds.Center.X - promptSize.X * promptScale / 2f, _bounds.Top + 3),
                UITheme.InkMuted, 0f, Vector2.Zero, promptScale, SpriteEffects.None, 0f);

            const int slotCount = MaxLen;
            int gap = Math.Max(5, _bounds.Width / 60);
            int slotHeight = Math.Max(20, (int)(_bounds.Height * 0.48f));
            int slotWidth = Math.Min((int)(_bounds.Width * 0.17f),
                (_bounds.Width - 32 - gap * (slotCount - 1)) / slotCount);
            int totalWidth = slotWidth * slotCount + gap * (slotCount - 1);
            int startX = _bounds.Center.X - totalWidth / 2;
            int slotY = _bounds.Bottom - slotHeight - Math.Max(5, _bounds.Height / 10);
            bool showCursor = _focused && (gameTime.TotalGameTime.TotalMilliseconds / 400) % 2 == 0;

            for (int i = 0; i < slotCount; i++)
            {
                Rectangle slot = new(startX + i * (slotWidth + gap), slotY, slotWidth, slotHeight);
                bool activeSlot = _focused && i == Math.Min(_value.Length, slotCount - 1);
                sb.Draw(_pixel, slot, activeSlot ? UITheme.PanelHover : UITheme.PanelDark);
                UIUtils.DrawRectangleOutlines(sb, slot, activeSlot ? accent : UITheme.InkMuted, activeSlot ? 2 : 1, _pixel);

                if (i < _value.Length)
                {
                    string character = _value[i].ToString();
                    Vector2 charSize = _font.MeasureString(character);
                    float charScale = MathHelper.Min(0.78f, MathHelper.Min(
                        (slot.Width - 8f) / Math.Max(1f, charSize.X),
                        (slot.Height - 4f) / Math.Max(1f, charSize.Y)));
                    sb.DrawString(_font, character,
                        new Vector2(slot.Center.X - charSize.X * charScale / 2f,
                            slot.Center.Y - charSize.Y * charScale / 2f),
                        UITheme.Ink, 0f, Vector2.Zero, charScale, SpriteEffects.None, 0f);
                }
                else if (activeSlot && showCursor)
                {
                    int cursorHeight = Math.Max(10, slot.Height / 2);
                    sb.Draw(_pixel, new Rectangle(slot.Center.X - 1,
                        slot.Center.Y - cursorHeight / 2, 2, cursorHeight), accent);
                }
                else
                {
                    int markWidth = Math.Max(8, slot.Width / 5);
                    sb.Draw(_pixel, new Rectangle(slot.Center.X - markWidth / 2,
                        slot.Center.Y, markWidth, 2), UITheme.InkMuted * 0.55f);
                }
            }

            UIUtils.DrawRectangleOutlines(sb, _bounds, _focused ? accent : UITheme.Ink, 1, _pixel);
        }
    }
}
