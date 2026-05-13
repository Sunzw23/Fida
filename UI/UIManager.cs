// UIManager.cs 文件
#nullable enable
using System.Collections.Generic;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI
{
    /// <summary>
    /// 输入模式枚举
    /// </summary>
    public enum InputMode
    {
        Mouse,
        Controller
    }

    /// <summary>
    /// UIManager 类负责游戏的所有绘制操作和 UI 元素的协调管理。
    /// The UIManager class is responsible for all drawing operations and UI element coordination in the game.
    /// </summary>
    public class UIManager
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly ResourceManager _resourceManager;

        private readonly List<UIControl> _uiControls = new List<UIControl>();
        private readonly UIPlayerManager _playerManager;
        private readonly UIInputHandler _inputHandler;

        /// <summary>
        /// 当前输入模式
        /// </summary>
        public InputMode CurrentInputMode { get; private set; } = InputMode.Mouse;

        public GameManager GameManager { get; private set; }

        public UIManager(GraphicsDevice graphicsDevice, ResourceManager resourceManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = new SpriteBatch(graphicsDevice);
            _resourceManager = resourceManager;

            _playerManager = new UIPlayerManager(_uiControls, _resourceManager);
            _inputHandler = new UIInputHandler(_uiControls);
            
            // 创建 GameManager 实例
            GameManager = new GameManager();
        }

        public void Draw(GameTime gameTime)
        {
            _graphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();
            foreach (UIControl control in _uiControls)
            {
                control.Draw(_spriteBatch, gameTime);
            }
            _spriteBatch.End();
        }

        public void Clear()
        {
            _uiControls.Clear();
        }

        public void BuildMainMenu()
        {
            Clear();
            BuildFromScreen("MainMenu");
        }

        public void BuildPlayingUI(GameManager gameManager)
        {
            _playerManager.BuildPlayingUI(gameManager);
            // 仅在控制器模式下自动聚焦第一个控件，避免鼠标模式下显示焦点
            if (CurrentInputMode == InputMode.Controller)
            {
                FocusFirst();
            }
        }

        public void UpdateGameMessage(string message)
        {
            _playerManager.UpdateGameMessage(message);
        }

        /// <summary>
        /// 每帧逻辑更新 - 处理动画计时等与渲染无关的逻辑
        /// </summary>
        public void Update(GameTime gameTime)
        {
            foreach (UIControl control in _uiControls)
            {
                control.Update(gameTime);
            }
        }

        /// <summary>
        /// 一键刷新所有控件 - 立即同步显示最新值，取消进行中的动画
        /// </summary>
        public void RefreshAll()
        {
            foreach (UIControl control in _uiControls)
            {
                if (control is AnimatedBoxLabel animatedBoxLabel)
                    animatedBoxLabel.Refresh();
                else if (control is UIBoxLabel boxLabel)
                    boxLabel.Refresh();
                else if (control is UIAnimatedButton animatedButton)
                    animatedButton.Refresh();
            }
        }

        public void UpdateUIButtons(GameManager gameManager)
        {
            // 更新所有 UI 按钮的状态（让按钮通过 EnabledChecker 询问环境来决定是否启用）
            var mouseState = Mouse.GetState();
            foreach (UIControl control in _uiControls)
            {
                control.Update(mouseState);
            }
        }

        public GameAction UpdateMouseAndCheckClicks(GameManager gameManager)
        {
            return _inputHandler.UpdateMouseAndCheckClicks(gameManager);
        }

        // ─── 手柄焦点管理 ──────────────────────────────────

        /// <summary>
        /// 更新手柄输入并处理焦点导航
        /// </summary>
        public void UpdateControllerInput(GameTime gameTime)
        {
            _inputHandler.UpdateController(gameTime);

            // 仅在控制器模式下管理焦点，避免鼠标模式下 EnsureFocus 重新设置焦点
            if (CurrentInputMode == InputMode.Controller)
            {
                UpdateFocusNavigation();
                EnsureFocus();
            }
        }

        /// <summary>
        /// 检查手柄确认键并生成游戏动作
        /// </summary>
        public GameAction CheckControllerConfirm(GameManager gameManager)
        {
            return _inputHandler.CheckControllerConfirm(gameManager);
        }

        // ─── 输入模式管理 ──────────────────────────────────

        /// <summary>
        /// 更新输入模式 - 根据最近的输入活动自动切换
        /// </summary>
        public void UpdateInputMode()
        {
            if (_inputHandler.ControllerInput.HasAnyInput())
            {
                SetInputMode(InputMode.Controller);
            }
            else if (_inputHandler.HasMouseActivity() || _inputHandler.HasKeyboardActivity())
            {
                SetInputMode(InputMode.Mouse);
            }
        }

        /// <summary>
        /// 设置输入模式，切换时清理另一种模式的 UI 状态
        /// </summary>
        private void SetInputMode(InputMode mode)
        {
            if (CurrentInputMode == mode) return;
            CurrentInputMode = mode;

            if (mode == InputMode.Controller)
            {
                // 切换到控制器模式：隐藏鼠标指针，清除 hover，确保有焦点
                // 鼠标可见性由 Game1 控制
                ClearHoverStates();
                EnsureFocus();
            }
            else
            {
                // 切换到鼠标模式：清除焦点
                ClearFocus();
            }
        }

        /// <summary>
        /// 清除所有控件的 hover 状态
        /// </summary>
        private void ClearHoverStates()
        {
            foreach (var control in _uiControls)
            {
                if (control is UIButton btn)
                    btn.SetHover(false);
                if (control is UIAnimatedButton animBtn)
                    animBtn.SetHover(false);
            }
        }

        /// <summary>
        /// 获取当前可聚焦的控件列表（按 FocusIndex 排序）
        /// </summary>
        private List<UIControl> GetFocusableControls()
        {
            var focusable = new List<UIControl>();
            foreach (var control in _uiControls)
            {
                if (control.CanFocus)
                    focusable.Add(control);
            }
            focusable.Sort((a, b) => a.FocusIndex.CompareTo(b.FocusIndex));
            return focusable;
        }

        /// <summary>
        /// 根据手柄导航方向更新焦点
        /// </summary>
        private void UpdateFocusNavigation()
        {
            var controller = _inputHandler.ControllerInput;
            if (!controller.IsConnected) return;

            var direction = controller.GetNavigationDirection();
            if (direction == ControllerInputHandler.Direction.None) return;

            var focusable = GetFocusableControls();
            if (focusable.Count == 0) return;

            // 找到当前聚焦的控件索引
            int currentIndex = focusable.FindIndex(c => c.IsFocused);

            int newIndex = currentIndex;

            switch (direction)
            {
                case ControllerInputHandler.Direction.Up:
                    // 向上导航：找上一行（按 FocusIndex 递减）
                    newIndex = currentIndex > 0 ? currentIndex - 1 : focusable.Count - 1;
                    break;
                case ControllerInputHandler.Direction.Down:
                    // 向下导航：找下一行（按 FocusIndex 递增）
                    newIndex = currentIndex < focusable.Count - 1 ? currentIndex + 1 : 0;
                    break;
                case ControllerInputHandler.Direction.Left:
                    // 向左导航：找上一个
                    newIndex = currentIndex > 0 ? currentIndex - 1 : focusable.Count - 1;
                    break;
                case ControllerInputHandler.Direction.Right:
                    // 向右导航：找下一个
                    newIndex = currentIndex < focusable.Count - 1 ? currentIndex + 1 : 0;
                    break;
            }

            if (newIndex != currentIndex)
            {
                // 取消旧焦点
                if (currentIndex >= 0 && currentIndex < focusable.Count)
                    focusable[currentIndex].SetFocus(false);

                // 设置新焦点
                focusable[newIndex].SetFocus(true);
            }
            else if (currentIndex < 0 && focusable.Count > 0)
            {
                // 没有焦点时，默认聚焦第一个
                focusable[0].SetFocus(true);
            }
        }

        /// <summary>
        /// 清除所有控件的焦点
        /// </summary>
        public void ClearFocus()
        {
            foreach (var control in _uiControls)
            {
                if (control.IsFocused)
                    control.SetFocus(false);
            }
        }

        /// <summary>
        /// 自动聚焦第一个可聚焦控件
        /// </summary>
        public void FocusFirst()
        {
            var focusable = GetFocusableControls();
            if (focusable.Count > 0)
            {
                ClearFocus();
                focusable[0].SetFocus(true);
            }
        }

        /// <summary>
        /// 确保至少有一个控件获得焦点（当没有控件聚焦时自动聚焦第一个）
        /// </summary>
        private void EnsureFocus()
        {
            var focusable = GetFocusableControls();
            if (focusable.Count == 0) return;

            bool anyFocused = false;
            foreach (var c in focusable)
            {
                if (c.IsFocused)
                {
                    anyFocused = true;
                    break;
                }
            }

            if (!anyFocused)
            {
                focusable[0].SetFocus(true);
            }
        }

        public UIPlayer GetUIPlayer(int playerId)
        {
            return _playerManager.GetUIPlayer(playerId);
        }

        /// <summary>
        /// 设置玩家头像纹理。如果纹理为 null 或未设置，头像区域不显示。
        /// 应在 BuildPlayingUI 之前调用。
        /// </summary>
        public void SetPortraitTexture(int playerId, Texture2D texture)
        {
            _playerManager.SetPortraitTexture(playerId, texture);
        }

        /// <summary>
        /// 获取手柄输入处理器
        /// </summary>
        public ControllerInputHandler GetControllerInput()
        {
            return _inputHandler.ControllerInput;
        }

        public List<UIControl> GetControls()
        {
            return _uiControls;
        }

        /// <summary>
        /// 根据名称查找按钮控件
        /// </summary>
        public UIButton? GetButtonByName(string name)
        {
            foreach (var control in _uiControls)
            {
                if (control is UIButton btn && btn.Name == name)
                    return btn;
            }
            return null;
        }

        public void BuildPauseMenu()
        {
            Clear();
            BuildFromScreen("PauseMenu");
        }

        public void BuildSettingsScreen()
        {
            Clear();
        }

        public void BuildRulesScreen()
        {
            Clear();
        }

        public void BuildOnlineMenu()
        {
            Clear();
            BuildFromScreen("OnlineMenu");
        }

        public void BuildHostingScreen(string statusText)
        {
            Clear();
            var replacements = new Dictionary<string, string> { { "status", statusText } };
            BuildFromScreen("HostingScreen", replacements);
        }

        public void BuildConnectingScreen(string statusText)
        {
            Clear();
            var replacements = new Dictionary<string, string> { { "status", statusText } };
            BuildFromScreen("ConnectingScreen", replacements);
        }

        public void BuildOnlineLobby(bool isHost)
        {
            Clear();
            BuildFromScreen("OnlineLobby");
        }

        /// <summary>
        /// 从 JSON 屏幕布局文件构建控件。
        /// 自动为按钮设置 FocusIndex（按添加顺序），并聚焦第一个按钮。
        /// </summary>
        private void BuildFromScreen(string screenName, Dictionary<string, string>? replacements = null)
        {
            var descriptors = UIScreenLoader.Load(screenName, replacements);
            int focusIndex = 0;
            foreach (var desc in descriptors)
            {
                UIControl control = desc.Type switch
                {
                    "button" => new UIButton(desc.Id, desc.Bounds!.Value, desc.Text, _resourceManager.Pixel, _resourceManager.Font),
                    "label" => new UILabel(desc.Id, desc.Text, desc.Position!.Value, _resourceManager.Font),
                    _ => throw new System.NotSupportedException($"Unknown control type: {desc.Type}")
                };
                // 设置控件名称（用于事件绑定查找）
                control.Name = desc.Name;
                // 为按钮设置 FocusIndex，使其可被手柄聚焦
                if (control is UIButton)
                {
                    control.FocusIndex = focusIndex++;
                }
                _uiControls.Add(control);
            }
            // 仅在控制器模式下自动聚焦第一个按钮，避免鼠标模式下显示焦点
            if (CurrentInputMode == InputMode.Controller)
            {
                FocusFirst();
            }
        }

        public void Dispose()
        {
            _spriteBatch?.Dispose();
        }
    }
}