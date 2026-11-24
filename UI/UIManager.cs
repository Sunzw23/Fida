// UIManager.cs 文件
#nullable enable
using System.Collections.Generic;
using Fida.Logic;
using Fida.Campaign;
using Fida.UI.Campaign;
using Fida.UI.Rules;
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
    /// UIManager 是 UI 子系统的外观（Facade）：
    /// - 持有 GraphicsDevice / SpriteBatch / ResourceManager 与控件列表
    /// - 把「屏幕构建」委托给 <see cref="UIScreenBuilder"/>
    /// - 把「手柄焦点 / 输入模式」委托给 <see cref="UIFocusManager"/>
    /// - 把「玩家面板」委托给 <see cref="UIPlayerManager"/>
    /// - 把「鼠标 / 键盘 / 手柄交互」委托给 <see cref="UIInputHandler"/>
    /// 公共签名保持不变，便于外部调用方无感知切换。
    /// </summary>
    public class UIManager : System.IDisposable
    {
        /// <summary>Three reference pages, one skill index, then one page per discovered skill.</summary>
        public int RulesPageCount => RulesBook.Current().TotalPages;

        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly ResourceManager _resourceManager;
        private readonly RasterizerState _uiRasterizerState;
        private readonly PaperPass _paperPass;

        private readonly List<UIControl> _uiControls = new List<UIControl>();
        private readonly UIPlayerManager _playerManager;
        private readonly UIInputHandler _inputHandler;
        private readonly UIFocusManager _focusManager;
        private readonly UIScreenBuilder _screenBuilder;
        private UIControl? _entranceAnchor;
        private double _entranceStart;
        private double _lastUiTime;
        private RenderTarget2D? _pageFrame, _previousPageFrame;
        public int RenderSampleCount => _pageFrame?.MultiSampleCount ?? 0;
#if FIDA_ANDROID
        private const int UiSamples = 0;
#else
        // UI geometry is rendered here, so back-buffer MSAA alone cannot smooth it.
        private const int UiSamples = 4;
#endif
        private Texture2D? _pausedBattleFrame;
        private bool _hasPageFrame, _showPreviousPage;
        private bool _transitionPointerHeld;
        public bool IsPageTransitionActive => !UIMotion.Reduced &&
            (_transitionPointerHeld || (_uiControls.Count > 0 && !ReferenceEquals(_uiControls[0], _entranceAnchor)) ||
             (_showPreviousPage && _lastUiTime - _entranceStart < .14));

        public GameManager GameManager { get; private set; }

        /// <summary>当前输入模式（委托给焦点管理器）</summary>
        public InputMode CurrentInputMode => _focusManager.CurrentInputMode;

        public UIManager(GraphicsDevice graphicsDevice, ResourceManager resourceManager)
        {
            UIGumLayout.Initialize(graphicsDevice);
            _graphicsDevice = graphicsDevice;
            _spriteBatch = new SpriteBatch(graphicsDevice);
            _uiRasterizerState = new RasterizerState
            {
                CullMode = CullMode.None,
                MultiSampleAntiAlias = true,
                ScissorTestEnable = false
            };
            _resourceManager = resourceManager;
            _paperPass = new PaperPass(graphicsDevice, resourceManager);

            _playerManager = new UIPlayerManager(_uiControls, _resourceManager);
            _inputHandler = new UIInputHandler(_uiControls);
            _focusManager = new UIFocusManager(_uiControls, _inputHandler);
            _screenBuilder = new UIScreenBuilder(_resourceManager, _uiControls, _focusManager);

            // 创建 GameManager 实例
            GameManager = new GameManager();
        }

        // ─── 渲染 / 生命周期 ─────────────────────────────

        public void Draw(GameTime gameTime)
        {
            var viewport = _graphicsDevice.Viewport;
            if (_pageFrame == null || _pageFrame.Width != viewport.Width || _pageFrame.Height != viewport.Height)
            {
                _pageFrame?.Dispose(); _previousPageFrame?.Dispose();
                _pageFrame = new RenderTarget2D(_graphicsDevice, viewport.Width, viewport.Height, false,
                    SurfaceFormat.Color, DepthFormat.None, UiSamples, RenderTargetUsage.PreserveContents);
                _previousPageFrame = new RenderTarget2D(_graphicsDevice, viewport.Width, viewport.Height, false,
                    SurfaceFormat.Color, DepthFormat.None, UiSamples, RenderTargetUsage.PreserveContents);
                _hasPageFrame = false; _showPreviousPage = false;
            }
            UIControl? anchor = _uiControls.Count > 0 ? _uiControls[0] : null;
            if (!ReferenceEquals(anchor, _entranceAnchor))
            {
                if (_hasPageFrame && !UIMotion.Reduced)
                {
                    (_pageFrame, _previousPageFrame) = (_previousPageFrame, _pageFrame);
                    _showPreviousPage = true;
                }
                _entranceAnchor = anchor;
                _entranceStart = gameTime.TotalGameTime.TotalSeconds;
                int order = 0;
                foreach (var control in _uiControls)
                    if (control is UIButton button) button.EntranceDelay = System.Math.Min(order++, 5) * 0.04f;
            }
            float entrance = UIMotion.Reveal(gameTime.TotalGameTime.TotalSeconds - _entranceStart, .20f);
            _graphicsDevice.SetRenderTarget(_pageFrame);
            _graphicsDevice.Clear(UITheme.BgTop);
            UIBatch.Begin(_spriteBatch, _uiRasterizerState);
            UITheme.DrawBackground(_spriteBatch, _resourceManager.Pixel, _graphicsDevice.Viewport, gameTime);
            foreach (var control in _uiControls)
                if (control is UIPlayer) { UIPaper.BattleBackground(_spriteBatch, _graphicsDevice.Viewport); break; }
            foreach (UIControl control in _uiControls)
            {
                if (control.IsVisible) control.Draw(_spriteBatch, gameTime);
            }
#if !FIDA_ANDROID
            // 布局重叠调试叠加层（F8 开关，仅桌面构建）
            UIOverlapDebugger.Draw(_spriteBatch, _uiControls, _graphicsDevice.Viewport,
                _resourceManager.Pixel, _resourceManager.Font);
#endif
            _spriteBatch.End();
            _graphicsDevice.SetRenderTarget(null);
            // One pass over the finished page: crossfade, then procedural paper tooth.
            bool crossfade = _showPreviousPage && entrance < 1 && !UIMotion.Reduced;
            if (_pageFrame is { } pageFrame)
                _paperPass.Draw(pageFrame, crossfade ? _previousPageFrame : null, crossfade ? entrance : 1f);
            if (!crossfade) _showPreviousPage = false;
            _hasPageFrame = true;
        }

        public void Clear() { CaptureBattleFrame(); _uiControls.Clear(); }

        private void CaptureBattleFrame()
        {
            bool battle=false;
            foreach(var control in _uiControls) if(control is UIPlayer) { battle=true; break; }
            if(!battle || _pageFrame==null || !_hasPageFrame) return;
            var pixels=new Color[_pageFrame.Width*_pageFrame.Height];
            _pageFrame.GetData(pixels);
            _pausedBattleFrame?.Dispose();
            _pausedBattleFrame=new Texture2D(_graphicsDevice,_pageFrame.Width,_pageFrame.Height);
            _pausedBattleFrame.SetData(pixels);
        }

        /// <summary>每帧逻辑更新 - 处理动画计时等与渲染无关的逻辑</summary>
        public void Update(GameTime gameTime)
        {
            _lastUiTime = gameTime.TotalGameTime.TotalSeconds;
            var pointer = UIPointer.GetState();
            if (pointer.LeftButton == ButtonState.Released) _transitionPointerHeld = false;
            else if (IsPageTransitionActive) _transitionPointerHeld = true;
            bool blocked = IsPageTransitionActive;
            if (blocked) _inputHandler.SynchronizeInput();
            foreach (UIControl control in _uiControls)
            {
                if (blocked && control is UICampaignMap map) { map.SynchronizeInput(); continue; }
                if (blocked && control is UIRoomCodeInput code) { code.SynchronizeInput(); continue; }
                if (blocked && control is UICampaignSkillWheel wheel)
                {
                    bool previous = wheel.InputBlocked;
                    wheel.InputBlocked = true; wheel.Update(gameTime); wheel.Update(pointer); wheel.InputBlocked = previous;
                    continue;
                }
                control.Update(gameTime);
            }
        }

        /// <summary>一键刷新所有控件 - 立即同步显示最新值，取消进行中的动画</summary>
        public void RefreshAll()
        {
            foreach (UIControl control in _uiControls)
            {
                if (control is UIAnimatedBoxLabel animatedBoxLabel)
                    animatedBoxLabel.Refresh();
                else if (control is UIBoxLabel boxLabel)
                    boxLabel.Refresh();
                else if (control is UIAnimatedButton animatedButton)
                    animatedButton.Refresh();
            }
        }

        public void Dispose()
        {
            _pageFrame?.Dispose(); _previousPageFrame?.Dispose();
            _pausedBattleFrame?.Dispose();
            _uiRasterizerState.Dispose();
            _spriteBatch.Dispose();
        }

        // ─── 屏幕构建（委托 UIScreenBuilder） ─────────────

        public void BuildMainMenu(bool connected = false, bool isHost = false) =>
            _screenBuilder.BuildMainMenu(connected, isHost);
        public void BuildPauseMenu()
        {
            CaptureBattleFrame();
            _screenBuilder.BuildPauseMenu();
            foreach(var control in _uiControls) if(control is UIPaperPause pause) pause.Backdrop=_pausedBattleFrame;
        }
        public void BuildSettingsScreen() => _screenBuilder.BuildSettingsScreen();
        public void BuildRulesScreen(int page = 0, int indexPage = 0) =>
            _screenBuilder.BuildRulesScreen(page, indexPage);
        public void BuildOnlineMenu(bool connected = false, bool isHost = false,
            string detailLabel = "Server", string detailValue = "") =>
            _screenBuilder.BuildOnlineMenu(connected, isHost, detailLabel, detailValue);
        public void BuildHostingScreen(string statusText, string roomCode = "") => _screenBuilder.BuildHostingScreen(statusText, roomCode);
        public void BuildConnectingScreen(string statusText) => _screenBuilder.BuildConnectingScreen(statusText);
        public void BuildOnlineLobby(bool isHost) => _screenBuilder.BuildOnlineLobby(isHost);
        public void BuildDuelDraft() => _screenBuilder.BuildDuelDraft();
        public void BuildLevelSelect(bool online = false) => _screenBuilder.BuildLevelSelect(online);
        public void BuildCompanionSelect(bool online = false) => _screenBuilder.BuildCompanionSelect(online);
        public void BuildSkillSelect(bool online = false) => _screenBuilder.BuildSkillSelect(online);
        public UITutorialDialogue AddTutorialDialogue(CampaignNode node) =>
            _screenBuilder.AddTutorialDialogue(node);
        public UITutorialDialogue? TutorialDialogue
        {
            get
            {
                foreach (var control in _uiControls)
                    if (control is UITutorialDialogue dialogue && dialogue.IsVisible)
                        return dialogue;
                return null;
            }
        }
        public void ShowTutorialHint(CampaignNode node, string instruction)
        {
            UITutorialDialogue? dialogue = null;
            foreach (var control in _uiControls)
                if (control is UITutorialDialogue existing)
                {
                    dialogue = existing;
                    break;
                }
            dialogue ??= _screenBuilder.AddTutorialDialogue(node);
            dialogue.PresentInstruction(instruction);
            if (CurrentInputMode == InputMode.Controller) _focusManager.FocusFirst();
        }
        public void BuildCampaignResult(bool won, string message)
        {
            _screenBuilder.BuildCampaignResult(won, message);
            _uiControls.Add(new UIResultAccent(_resourceManager.Pixel, won));
        }
        public void BuildDuelResult(bool won, string message)
        {
            _screenBuilder.BuildDuelResult(won, message);
            _uiControls.Add(new UIResultAccent(_resourceManager.Pixel, won));
        }

        /// <summary>构建对局 UI（玩家面板 + 焦点初始化）</summary>
        public void BuildPlayingUI(GameManager gameManager)
        {
            _playerManager.BuildPlayingUI(gameManager);
            // 仅在控制器模式下自动聚焦第一个控件，避免鼠标模式下显示焦点
            if (CurrentInputMode == InputMode.Controller)
            {
                _focusManager.FocusFirst();
            }
        }

        /// <summary>阵容动态变化后重建战斗控件。</summary>
        public void RebuildPlayingUI(GameManager gameManager)
        {
            Clear();
            BuildPlayingUI(gameManager);
        }

        public void PlayReinforcementEntrance(IEnumerable<int> playerIds) =>
            _playerManager.PlayReinforcementEntrance(playerIds);

        /// <summary>更新游戏消息文本</summary>
        public void UpdateGameMessage(string message) => _playerManager.UpdateGameMessage(message);

        /// <summary>Displays a paced AI action cue without exposing UI controls to scene code.</summary>
        public void ShowActionCue(int playerId, string message) =>
            _playerManager.ShowActionCue(playerId, message);

        public void HideActionCue() => _playerManager.HideActionCue();

        public void PlayNumberFlow(int addendPlayerId, int addendNumberIndex,
            int basePlayerId, int baseNumberIndex) =>
            _playerManager.PlayNumberFlow(addendPlayerId, addendNumberIndex,
                basePlayerId, baseNumberIndex);

        public bool IsNumberFlowActive => _playerManager.IsNumberFlowActive;

        public void PlayCardAction(int sourcePlayerId, int targetPlayerId, bool damaging) =>
            _playerManager.PlayCardAction(sourcePlayerId, targetPlayerId, damaging);

        public bool IsBlockingPresentationActive => _playerManager.IsBlockingPresentationActive;

        // ─── 交互更新 ───────────────────────────────────

        /// <summary>更新所有 UI 按钮的状态（让按钮通过 EnabledChecker 询问环境来决定是否启用）</summary>
        public void UpdateUIButtons(GameManager gameManager)
        {
            if (IsPageTransitionActive) return;
            var mouseState = UIPointer.GetState();
            foreach (UIControl control in _uiControls)
            {
                control.Update(mouseState);
            }
        }

        /// <summary>更新鼠标输入并检测点击，返回对应游戏动作</summary>
        public GameAction UpdateMouseAndCheckClicks(GameManager gameManager)
        {
            return IsPageTransitionActive ? new GameAction { Type = ActionType.None } : _inputHandler.UpdateMouseAndCheckClicks(gameManager);
        }

        // ─── 手柄焦点 / 输入模式（委托 UIFocusManager） ───

        /// <summary>更新手柄输入并处理焦点导航</summary>
        public void UpdateControllerInput(GameTime gameTime) => _focusManager.UpdateControllerInput(gameTime);

        /// <summary>检查手柄确认键并生成游戏动作</summary>
        public GameAction CheckControllerConfirm(GameManager gameManager) => IsPageTransitionActive ? new GameAction { Type = ActionType.None } : _focusManager.CheckControllerConfirm(gameManager);

        /// <summary>更新输入模式 - 根据最近的输入活动自动切换</summary>
        public void UpdateInputMode() => _focusManager.UpdateInputMode();

        /// <summary>清除所有控件的焦点</summary>
        public void ClearFocus() => _focusManager.ClearFocus();

        /// <summary>自动聚焦第一个可聚焦控件</summary>
        public void FocusFirst() => _focusManager.FocusFirst();

        /// <summary>获取手柄输入处理器</summary>
        public ControllerInputHandler GetControllerInput() => _focusManager.GetControllerInput();

        // ─── 查询 ────────────────────────────────────────

        public UIPlayer GetUIPlayer(int playerId) => _playerManager.GetUIPlayer(playerId);

        /// <summary>设置玩家头像纹理。如果纹理为 null 或未设置，头像区域不显示。应在 BuildPlayingUI 之前调用。</summary>
        public void SetPortraitTexture(int playerId, Texture2D texture) => _playerManager.SetPortraitTexture(playerId, texture);

        /// <summary>获取当前控件列表</summary>
        public List<UIControl> GetControls() => _uiControls;

        /// <summary>根据名称查找按钮控件</summary>
        public UIButton? GetButtonByName(string name)
        {
            foreach (var control in _uiControls)
            {
                if (control is UIButton btn && btn.Name == name)
                    return btn;
            }
            return null;
        }

        /// <summary>根据名称查找房间码输入框</summary>
        public UIRoomCodeInput? GetTextBoxByName(string name)
        {
            foreach (var control in _uiControls)
            {
                if (control is UIRoomCodeInput tb && tb.Name == name)
                    return tb;
            }
            return null;
        }

        /// <summary>根据名称查找标签控件</summary>
        public UILabel? GetLabelByName(string name)
        {
            foreach (var control in _uiControls)
            {
                if (control is UILabel lbl && lbl.Name == name)
                    return lbl;
            }
            return null;
        }

        /// <summary>Finds a named control without coupling callers to the complete control list.</summary>
        public T? GetControlByName<T>(string name) where T : UIControl
        {
            foreach (var control in _uiControls)
            {
                if (control is T typed && typed.Name == name) return typed;
            }
            return null;
        }
    }
}
