using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Fida.UI
{
    /// <summary>
    /// 手柄输入处理核心 - 封装 GamePad 状态读取和按键检测
    /// </summary>
    public class ControllerInputHandler
    {
        private GamePadState _currentState;
        private GamePadState _previousState;
        private readonly PlayerIndex _playerIndex;

        // 死区阈值，防止摇杆漂移
        private const float ThumbStickDeadZone = 0.3f;

        // 导航重复延迟（毫秒）
        private const float NavRepeatDelay = 200f;
        private const float NavInitialDelay = 400f;

        // 导航计时
        private float _navTimer;
        private bool _navKeyHeld;
        private Direction _lastNavDirection;

        public enum Direction
        {
            None,
            Up,
            Down,
            Left,
            Right
        }

        public ControllerInputHandler(PlayerIndex playerIndex = PlayerIndex.One)
        {
            _playerIndex = playerIndex;
        }

        /// <summary>
        /// 每帧调用，更新手柄状态
        /// </summary>
        public void Update(GameTime gameTime)
        {
            _previousState = _currentState;
            _currentState = GamePad.GetState(_playerIndex);

            // 更新导航重复计时
            if (_navKeyHeld)
            {
                _navTimer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            }
        }

        /// <summary>
        /// 检测按键是否刚被按下（上升沿检测）
        /// </summary>
        public bool IsPressed(Buttons button)
        {
            return _currentState.IsButtonDown(button) && _previousState.IsButtonUp(button);
        }

        /// <summary>
        /// 检测按键是否被按住
        /// </summary>
        public bool IsHeld(Buttons button)
        {
            return _currentState.IsButtonDown(button);
        }

        /// <summary>
        /// 检测按键是否刚释放（下降沿检测）
        /// </summary>
        public bool IsReleased(Buttons button)
        {
            return _currentState.IsButtonUp(button) && _previousState.IsButtonDown(button);
        }

        /// <summary>
        /// 获取导航方向（支持 D-pad 和左摇杆）
        /// 带重复延迟：首次按下后等待 NavInitialDelay，之后每 NavRepeatDelay 重复
        /// </summary>
        public Direction GetNavigationDirection()
        {
            Direction rawDir = GetRawDirection();

            if (rawDir == Direction.None)
            {
                _navKeyHeld = false;
                _navTimer = 0;
                _lastNavDirection = Direction.None;
                return Direction.None;
            }

            if (!_navKeyHeld)
            {
                // 首次按下
                _navKeyHeld = true;
                _navTimer = 0;
                _lastNavDirection = rawDir;
                return rawDir;
            }

            // 方向改变时立即触发
            if (rawDir != _lastNavDirection)
            {
                _navTimer = 0;
                _lastNavDirection = rawDir;
                return rawDir;
            }

            // 重复延迟逻辑
            if (_navTimer >= NavInitialDelay)
            {
                float elapsedSinceInitial = _navTimer - NavInitialDelay;
                if (elapsedSinceInitial >= NavRepeatDelay)
                {
                    _navTimer = NavInitialDelay; // 重置计时器但保留初始延迟
                    return rawDir;
                }
            }

            return Direction.None;
        }

        /// <summary>
        /// 获取原始方向输入（不处理重复延迟）
        /// </summary>
        private Direction GetRawDirection()
        {
            // D-pad 优先
            if (_currentState.DPad.Up == ButtonState.Pressed)
                return Direction.Up;
            if (_currentState.DPad.Down == ButtonState.Pressed)
                return Direction.Down;
            if (_currentState.DPad.Left == ButtonState.Pressed)
                return Direction.Left;
            if (_currentState.DPad.Right == ButtonState.Pressed)
                return Direction.Right;

            // 左摇杆
            if (_currentState.ThumbSticks.Left.Y > ThumbStickDeadZone)
                return Direction.Up;
            if (_currentState.ThumbSticks.Left.Y < -ThumbStickDeadZone)
                return Direction.Down;
            if (_currentState.ThumbSticks.Left.X < -ThumbStickDeadZone)
                return Direction.Left;
            if (_currentState.ThumbSticks.Left.X > ThumbStickDeadZone)
                return Direction.Right;

            return Direction.None;
        }

        /// <summary>
        /// 检测确认键（A 按钮）
        /// </summary>
        public bool IsConfirmPressed()
        {
            return IsPressed(Buttons.A);
        }

        /// <summary>
        /// 检测取消键（B 按钮）
        /// </summary>
        public bool IsCancelPressed()
        {
            return IsPressed(Buttons.B);
        }

        /// <summary>
        /// 检测暂停键（Start 按钮）
        /// </summary>
        public bool IsPausePressed()
        {
            return IsPressed(Buttons.Start);
        }

        /// <summary>
        /// 检测返回键（Back 按钮）
        /// </summary>
        public bool IsBackPressed()
        {
            return IsPressed(Buttons.Back);
        }

        /// <summary>
        /// 手柄是否已连接
        /// </summary>
        public bool IsConnected => _currentState.IsConnected;

        /// <summary>
        /// 检测是否有任何手柄输入活动（按键、摇杆、十字键）
        /// </summary>
        public bool HasAnyInput()
        {
            if (!IsConnected) return false;

            // 检测所有按键
            ButtonState[] buttons = new ButtonState[]
            {
                _currentState.Buttons.A, _currentState.Buttons.B, _currentState.Buttons.X, _currentState.Buttons.Y,
                _currentState.Buttons.Start, _currentState.Buttons.Back, _currentState.Buttons.LeftShoulder, _currentState.Buttons.RightShoulder,
                _currentState.Buttons.LeftStick, _currentState.Buttons.RightStick
            };
            foreach (var b in buttons)
            {
                if (b == ButtonState.Pressed) return true;
            }

            // 检测 D-pad
            if (_currentState.DPad.Up == ButtonState.Pressed ||
                _currentState.DPad.Down == ButtonState.Pressed ||
                _currentState.DPad.Left == ButtonState.Pressed ||
                _currentState.DPad.Right == ButtonState.Pressed)
                return true;

            // 检测摇杆（超过死区）
            if (Math.Abs(_currentState.ThumbSticks.Left.X) > ThumbStickDeadZone ||
                Math.Abs(_currentState.ThumbSticks.Left.Y) > ThumbStickDeadZone ||
                Math.Abs(_currentState.ThumbSticks.Right.X) > ThumbStickDeadZone ||
                Math.Abs(_currentState.ThumbSticks.Right.Y) > ThumbStickDeadZone)
                return true;

            // 检测扳机
            if (_currentState.Triggers.Left > 0.1f || _currentState.Triggers.Right > 0.1f)
                return true;

            return false;
        }
    }
}
