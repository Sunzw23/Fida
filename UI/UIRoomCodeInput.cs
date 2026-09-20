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

            // 深羊皮纸底
            sb.Draw(_pixel, _bounds, UITheme.PanelDark);

            // 文字（左对齐，垂直居中）
            string text = _value;
            if (string.IsNullOrEmpty(text))
                text = _placeholder;
            text = UIFontGuard.Sanitize(_font, text);

            Color textColor = string.IsNullOrEmpty(_value) ? UITheme.InkMuted : UITheme.Ink;
            var textSize = _font.MeasureString(text);
            float textScale = MathHelper.Min(1f, MathHelper.Min(
                MathHelper.Max(1, _bounds.Width - 24) / MathHelper.Max(1, textSize.X),
                MathHelper.Max(1, _bounds.Height - 12) / MathHelper.Max(1, textSize.Y)));
            var pos = new Vector2(
                _bounds.X + 12,
                _bounds.Center.Y - textSize.Y * textScale / 2
            );
            sb.DrawString(_font, text, pos, textColor, 0f, Vector2.Zero,
                textScale, SpriteEffects.None, 0f);

            // 闪烁光标（聚焦时）
            if (_focused && (gameTime.TotalGameTime.TotalMilliseconds / 400) % 2 == 0)
            {
                float cursorX = _bounds.X + 12 +
                    _font.MeasureString(UIFontGuard.Sanitize(_font, _value)).X * textScale + 3;
                var cursor = new Rectangle(
                    (int)cursorX,
                    _bounds.Top + 8,
                    2,
                    _bounds.Height - 16
                );
                sb.Draw(_pixel, cursor, UITheme.Ink);
            }

            // 描金边（聚焦时加粗亮金）
            UIUtils.DrawRectangleOutlines(sb, _bounds, _focused ? UITheme.GoldBright : UITheme.Gold, _focused ? 3 : 1, _pixel);
        }
    }
}
