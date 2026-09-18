using System.Collections.Generic;
using System.Linq;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// UIPlayerManager 类负责管理玩家 UI 控件和按钮状态。
    /// </summary>
    public class UIPlayerManager
    {
        private readonly List<UIPlayer> _uiPlayers = new List<UIPlayer>();
        private readonly List<UIControl> _uiControls;
        private readonly ResourceManager _resourceManager;
        private UIBoxLabel _gameMessageLabel;
        private UIBoxLabel _actionCueLabel;
        private UINumberFlowEffect _numberFlowEffect;
        private UICardCombatAnimator _cardCombatAnimator;
        private UIDecideApplyDialog _decideApplyDialog;
        private GameManager _gameManager;

        // 玩家头像纹理字典，key 为 PlayerId，value 为头像纹理（可为 null）
        private Dictionary<int, Texture2D> _portraitTextures = new Dictionary<int, Texture2D>();

        // ─── ChooseNumber 数字面板（0-9）────────────────────────────
        // 「神/仙/迪」技能第二步"把该数字改成几"、「变」技能"变成几点"需要选择 0-9，
        // 此前只有鼠标隐形点击区、手柄完全不可用。这里构建可见按钮面板，
        // 按钮加入 _uiControls，使鼠标/手柄/焦点三条输入路径统一生效。
        private UIAnimatedButton[] _numberButtons = null;

        public UIPlayerManager(List<UIControl> uiControls, ResourceManager resourceManager)
        {
            _uiControls = uiControls;
            _resourceManager = resourceManager;
        }

        /// <summary>
        /// 设置玩家头像纹理。如果纹理为 null 或未设置，头像区域不显示。
        /// </summary>
        public void SetPortraitTexture(int playerId, Texture2D texture)
        {
            _portraitTextures[playerId] = texture;
        }

        public void BuildPlayingUI(GameManager gameManager)
        {
            if (_gameManager != null)
            {
                _gameManager.MessageUpdated -= OnMessageUpdated;
                _gameManager.GameStateChanged -= OnGameStateChanged;
                _gameManager.PlayerActionRequired -= OnPlayerActionRequired;
            }
            _gameManager = gameManager;
            _uiPlayers.Clear();

            int viewportWidth = _resourceManager.GraphicsDevice.Viewport.Width;
            int messageMargin = System.Math.Clamp(viewportWidth / 20, 12, 48);
            int messageWidth = System.Math.Max(1, viewportWidth - messageMargin * 2);
            var messageBounds = new Rectangle(messageMargin, 24, messageWidth, 64);
            _gameMessageLabel = new UIBoxLabel(0, messageBounds, gameManager.GameMessage, _resourceManager.Pixel, _resourceManager.Font)
            {
                Alignment = UIBoxLabelAlignment.Center,
                BorderThickness = 1,
                AutoFitText = true,
                MinimumTextScale = 0.45f,
                TextPadding = 18,
                BackColor = UITheme.Ink,
                ForeColor = UITheme.PanelLight
            };
            _uiControls.Add(_gameMessageLabel);

            int cueMargin = System.Math.Clamp(viewportWidth / 8, 20, 188);
            var cueBounds = new Rectangle(cueMargin, 100,
                System.Math.Max(1, viewportWidth - cueMargin * 2), 56);
            _actionCueLabel = new UIBoxLabel(-1, cueBounds, "", _resourceManager.Pixel, _resourceManager.Font)
            {
                Alignment = UIBoxLabelAlignment.Center,
                AutoFitText = true,
                MinimumTextScale = 0.5f,
                TextPadding = 16,
                BackColor = UITheme.PanelLight,
                ForeColor = UITheme.Ink,
                BorderColor = UITheme.GoldBright,
                BorderThickness = 2,
                IsVisible = false
            };
            _uiControls.Add(_actionCueLabel);

            // 订阅事件
            _gameManager.MessageUpdated += OnMessageUpdated;
            _gameManager.GameStateChanged += OnGameStateChanged;
            _gameManager.PlayerActionRequired += OnPlayerActionRequired;

            // 计算每个玩家的绘制位置（基于分组）
            var positions = ComputePlayerPositions(gameManager);

            for (int i = 0; i < gameManager.Players.Count; i++)
            {
                var player = gameManager.Players[i];
                
                // Explicit per-player overrides take precedence; data-driven portrait keys
                // then resolve through the resource catalog with a safe team fallback.
                if (!_portraitTextures.TryGetValue(player.PlayerId, out Texture2D portraitTexture))
                {
                    portraitTexture = _resourceManager.GetPortrait(
                        player.PortraitKey,
                        player.PlayerId == gameManager.LocalPlayerId);
                }
                
                var uiPlayer = new UIPlayer(player.PlayerId, player, positions[player.PlayerId], _resourceManager.Font, _resourceManager.Pixel, portraitTexture);
                
                // 卡片本身在 ChooseTarget 时可交互
                int capturedPlayerId = player.PlayerId;
                uiPlayer.EnabledChecker = () => ShouldPlayerCardBeEnabled(capturedPlayerId);

                // 为按钮设置环境检查器，让按钮根据游戏状态自动决定是否启用
                uiPlayer.Number0Button.EnabledChecker = () => ShouldNumberButtonBeEnabled(capturedPlayerId, 0);
                uiPlayer.Number1Button.EnabledChecker = () => ShouldNumberButtonBeEnabled(capturedPlayerId, 1);

                // ─── 设置手柄焦点索引 ──────────────────────
                // 玩家卡片：100 + playerId * 10
                uiPlayer.FocusIndex = 100 + capturedPlayerId * 10;
                // 数字按钮：101 + playerId * 10（数字0），102 + playerId * 10（数字1）
                uiPlayer.Number0Button.FocusIndex = 101 + capturedPlayerId * 10;
                uiPlayer.Number1Button.FocusIndex = 102 + capturedPlayerId * 10;
                
                _uiControls.Add(uiPlayer);
                // 将数字按钮也加入主控件列表，使其可被手柄聚焦
                _uiControls.Add(uiPlayer.Number0Button);
                _uiControls.Add(uiPlayer.Number1Button);
                _uiPlayers.Add(uiPlayer);
            }

            // Draw last so the stream remains visible above cards and number buttons.
            _numberFlowEffect = new UINumberFlowEffect(-2, _resourceManager.Pixel)
            {
                IsVisible = false
            };
            _cardCombatAnimator = new UICardCombatAnimator(-3);

            // ─── 构建 ChooseNumber 数字面板（0-9，2 行 × 5 列）─────────────
            // 位置取自 uiconfig 的 numberSelectionArea（10,100,260×110），
            // 即原先 UIInputHandler.ActionChooseNumber 中硬编码的隐形点击区位置。
            BuildNumberPad();
            _uiControls.Add(_numberFlowEffect);
            _uiControls.Add(_cardCombatAnimator);

            _decideApplyDialog = new UIDecideApplyDialog(-4,
                _resourceManager.GraphicsDevice, _resourceManager.Pixel, _resourceManager.Font);
            _decideApplyDialog.ApplyButton.EnabledChecker = ShouldDecisionButtonsBeEnabled;
            _decideApplyDialog.PassButton.EnabledChecker = ShouldDecisionButtonsBeEnabled;
            _uiControls.Add(_decideApplyDialog);
            _uiControls.Add(_decideApplyDialog.ApplyButton);
            _uiControls.Add(_decideApplyDialog.PassButton);
        }

        /// <summary>
        /// 基于分组计算每个玩家的绘制位置：
        /// - 本地玩家所在组的成员 → 底部行（RowBottomY）
        /// - 其他组的成员 → 顶部行（RowTopY）
        /// - 每行内水平均匀分布
        /// </summary>
        private Dictionary<int, Vector2> ComputePlayerPositions(GameManager gameManager)
        {
            var positions = new Dictionary<int, Vector2>();

            // 找到本地玩家所在的组
            Group localGroup = null;
            foreach (var group in gameManager.Groups)
            {
                foreach (var p in group.Players)
                {
                    if (p.PlayerId == gameManager.LocalPlayerId)
                    {
                        localGroup = group;
                        break;
                    }
                }
                if (localGroup != null) break;
            }

            int cardWidth = UIConfig.Int("cardWidth");
            int screenWidth = _resourceManager.GraphicsDevice.Viewport.Width;

            // 如果没有分组（单机 PvP），使用默认的两位置布局
            if (localGroup == null)
            {
                for (int i = 0; i < gameManager.Players.Count; i++)
                {
                    int y = (i == 0) ? SceneConfig.RowBottomY : SceneConfig.RowTopY;
                    positions[gameManager.Players[i].PlayerId] = new Vector2(100, y);
                }
                return positions;
            }

            // 按组分：本地组在底部，其他组在顶部
            var localPlayers = localGroup.Players;
            var enemyPlayers = new List<Player>();
            foreach (var group in gameManager.Groups)
            {
                if (group.GroupId != localGroup.GroupId)
                {
                    enemyPlayers.AddRange(group.Players);
                }
            }

            // 计算底部行（本地组）位置
            int localCount = localPlayers.Count;
            int totalLocalWidth = localCount * cardWidth + (localCount - 1) * 20; // 20px 间距
            int startLocalX = (screenWidth - totalLocalWidth) / 2;
            for (int i = 0; i < localCount; i++)
            {
                positions[localPlayers[i].PlayerId] = new Vector2(startLocalX + i * (cardWidth + 20), SceneConfig.RowBottomY);
            }

            // 计算顶部行（敌方组）位置
            int enemyCount = enemyPlayers.Count;
            int totalEnemyWidth = enemyCount * cardWidth + (enemyCount - 1) * 20;
            int startEnemyX = (screenWidth - totalEnemyWidth) / 2;
            for (int i = 0; i < enemyCount; i++)
            {
                positions[enemyPlayers[i].PlayerId] = new Vector2(startEnemyX + i * (cardWidth + 20), SceneConfig.RowTopY);
            }

            return positions;
        }

        public void UpdateGameMessage(string message)
        {
            _gameMessageLabel?.Update(message);
        }

        /// <summary>Shows the current AI decision beat and highlights its card.</summary>
        public void ShowActionCue(int playerId, string message)
        {
            foreach (var uiPlayer in _uiPlayers)
                uiPlayer.IsPresentingAction = uiPlayer.Player.PlayerId == playerId;

            if (_actionCueLabel == null) return;
            _actionCueLabel.Update(message);
            _actionCueLabel.IsVisible = true;
        }

        public void HideActionCue()
        {
            foreach (var uiPlayer in _uiPlayers)
                uiPlayer.IsPresentingAction = false;
            if (_actionCueLabel != null) _actionCueLabel.IsVisible = false;
        }

        public bool IsNumberFlowActive => _numberFlowEffect?.IsPlaying == true;
        public bool IsCardCombatAnimationActive => _cardCombatAnimator?.IsPlaying == true;
        public bool IsBlockingPresentationActive =>
            IsNumberFlowActive || IsCardCombatAnimationActive;

        /// <summary>
        /// Sends a golden stream from the selected addend to the base number.
        /// Direction alone communicates the operation; no equation is rendered.
        /// </summary>
        public void PlayNumberFlow(int addendPlayerId, int addendNumberIndex,
            int basePlayerId, int baseNumberIndex)
        {
            var addendPlayer = GetUIPlayer(addendPlayerId);
            var basePlayer = GetUIPlayer(basePlayerId);
            if (addendPlayer == null || basePlayer == null) return;

            foreach (var uiPlayer in _uiPlayers)
                uiPlayer.IsPresentingAction = false;
            if (_actionCueLabel != null) _actionCueLabel.IsVisible = false;

            _numberFlowEffect?.Start(
                addendPlayer.GetNumberBounds(addendNumberIndex),
                basePlayer.GetNumberBounds(baseNumberIndex));
        }

        public void PlayCardAction(int sourcePlayerId, int targetPlayerId, bool damaging)
        {
            var source = GetUIPlayer(sourcePlayerId);
            var target = GetUIPlayer(targetPlayerId);
            if (source == null || target == null) return;
            HideActionCue();
            _cardCombatAnimator?.Start(source, target, damaging);
        }

        private void OnMessageUpdated(object sender, GameEventArgs e)
        {
            UpdateGameMessage(e.Message);
            // 不再需要手动更新按钮状态 - 按钮会通过 EnabledChecker 自动询问环境
        }

        /// <summary>
        /// 游戏状态变化时刷新所有按钮状态
        /// </summary>
        private void OnGameStateChanged(object sender, GameStateEventArgs e)
        {
            HideDecideApplyDialog();
            // 游戏状态变化时，强制刷新所有按钮的状态
            // 通过清除上一帧的状态记录，让按钮在下次 Update 时重新计算并通知状态变化
            foreach (var uiPlayer in _uiPlayers)
            {
                uiPlayer.RefreshButtons();
            }
            UpdateNumberPadVisibility();
        }

        /// <summary>
        private void OnPlayerActionRequired(object sender, PlayerActionRequiredEventArgs e)
        {
            // 每次步骤切换时（包括同状态内 StepIndex++），更新按钮的 Selected 状态
            UpdateButtonSelectedStates();
            UpdateNumberPadVisibility();
            if (e.RequiredActionType == ActionType.DecideApply &&
                e.SourcePlayerId == _gameManager.LocalPlayerId)
                ShowDecideApplyDialog(e.Message);
            else
                HideDecideApplyDialog();
        }

        private bool ShouldDecisionButtonsBeEnabled() =>
            _decideApplyDialog?.IsVisible == true &&
            _gameManager != null &&
            _gameManager.CurrentActionType == ActionType.DecideApply &&
            _gameManager.Query.IsLocalPlayerSource();

        private void ShowDecideApplyDialog(string prompt)
        {
            if (_decideApplyDialog == null) return;
            _decideApplyDialog.Show(prompt);
            foreach (var control in _uiControls)
                if (control.IsFocused) control.SetFocus(false);
            _decideApplyDialog.ApplyButton.SetFocus(true);
        }

        private void HideDecideApplyDialog()
        {
            if (_decideApplyDialog == null) return;
            _decideApplyDialog.Hide();
            _decideApplyDialog.ApplyButton.SetFocus(false);
            _decideApplyDialog.PassButton.SetFocus(false);
        }

        /// <summary>
        /// 在 ChooseNumberIndexState 第二阶段时，将第一步已选择的按钮标记为 Selected，
        /// 同时取消另一个按钮的 Selected 状态。
        /// </summary>
        private void UpdateButtonSelectedStates()
        {
            if (_gameManager == null) return;

            // 找到当前回合玩家的 UIPlayer
            var uiPlayer = _uiPlayers.FirstOrDefault(p => p.Player.PlayerId == _gameManager.Query.CurrentPlayerId);
            if (uiPlayer == null) return;

            int firstChosenIndex = _gameManager.Query.GetFirstChosenNumberIndex();

            if (firstChosenIndex < 0)
            {
                uiPlayer.Number0Button.SetSelected(false);
                uiPlayer.Number1Button.SetSelected(false);
                return;
            }

            uiPlayer.Number0Button.SetSelected(firstChosenIndex == 0);
            uiPlayer.Number1Button.SetSelected(firstChosenIndex == 1);
        }

        /// <summary>
        /// 数字按钮是否应该启用 - 由按钮主动调用 EnabledChecker 来询问
        /// </summary>
        private bool ShouldNumberButtonBeEnabled(int playerId, int buttonIndex)
        {
            if (_gameManager == null) return false;
            
            // 非本地玩家操作时禁用所有按钮（按操作者判断，而非回合玩家）
            // 神/仙/迪等在对方回合触发的本方技能，操作者是本地玩家，需放行
            if (!_gameManager.Query.IsLocalPlayerSource()) return false;

            // 不是选择数字索引操作时禁用
            if (_gameManager.CurrentActionType != ActionType.ChooseNumberIndex) return false;

            // 只启用当前步骤允许目标范围内的存活玩家按钮。
            // ValidTargets 由状态机广播（普通回合/神仙迪均携带），与 Handle 校验一致，
            // 空列表表示无限制。
            var validTargets = _gameManager.CurrentStepValidTargets;
            if (validTargets.Count > 0)
            {
                return validTargets.Contains(playerId) && _gameManager.Players[playerId].IsAlive();
            }
            return _gameManager.Players[playerId].IsAlive();
        }

        /// <summary>
        /// 玩家卡片是否应该启用交互 — ChooseTarget 时所有存活玩家的卡片可被点击
        /// </summary>
        private bool ShouldPlayerCardBeEnabled(int playerId)
        {
            if (_gameManager == null) return false;
            if (!_gameManager.Query.IsLocalPlayerSource()) return false;
            if (_gameManager.CurrentActionType != ActionType.ChooseTarget) return false;
            var player = _gameManager.Players.FirstOrDefault(p => p.PlayerId == playerId);
            return player != null && player.IsAlive() &&
                (_gameManager.CurrentStepValidTargets.Count == 0 || _gameManager.CurrentStepValidTargets.Contains(playerId));
        }

        /// <summary>
        /// 生命值按钮是否应该启用 - 由按钮主动调用 EnabledChecker 来询问
        /// </summary>
        private bool ShouldHealthButtonBeEnabled(int playerId)
        {
            if (_gameManager == null) return false;
            
            // 非本地玩家回合时禁用所有按钮
            if (_gameManager.Query.CurrentPlayerId != _gameManager.LocalPlayerId) return false;
            
            // 不是选择目标操作时禁用
            if (_gameManager.CurrentActionType != ActionType.ChooseTarget) return false;
            
            // 选择目标时，所有存活的玩家都可以被选择
            return _gameManager.Players[playerId].IsAlive();
        }

        /// <summary>
        /// 构建 ChooseNumber 数字面板（0-9，2 行 × 5 列），水平居中于窗口顶带。
        /// 原先 ChooseNumber 仅接受 UIInputHandler 中硬编码的隐形点击区（左上角），
        /// 且手柄路径完全不可用；现改为真实按钮并接入共享控件列表 _uiControls，
        /// 使鼠标（UpdateUIButtons）、手柄（FocusManager）与点击分发三条输入路径统一生效。
        /// 按钮 id 为 200+n，与玩家卡片数字按钮(10/12)区分；FocusIndex 300+，避开卡片(100+)与索引按钮(101/102)。
        /// </summary>
        private void BuildNumberPad()
        {
            _numberButtons = new UIAnimatedButton[10];

            int cellW = UIConfig.Int("numberButtonWidth", 50);
            int cellH = UIConfig.Int("numberButtonHeight", 50);
            int cols = UIConfig.Int("numberColumns", 5);
            int gap = 4;
            int panelW = cols * cellW + (cols - 1) * gap;
            // 水平居中于窗口顶带（y=10），避开两侧玩家卡片与左上角信息区，
            // 使 0-9 面板在单机/联机、有/无分组布局下均清晰可见且无遮挡。
            int viewportW = _resourceManager.GraphicsDevice.Viewport.Width;
            int areaX = (viewportW - panelW) / 2;
            int areaY = 10;

            for (int n = 0; n < 10; n++)
            {
                int num = n; // 闭包捕获：for 循环变量 n 在 lambda 中会指向循环结束后的值(10)，需用局部变量固定每个按钮自己的数字
                int col = n % cols;
                int row = n / cols;
                int x = areaX + col * (cellW + gap);
                int y = areaY + row * (cellH + gap);
                var bounds = new Rectangle(x, y, cellW, cellH);

                var btn = new UIAnimatedButton(200 + n, bounds, n.ToString(), _resourceManager.Pixel, _resourceManager.Font)
                {
                    EnabledChecker = () => ShouldNumberPadBeEnabled(num),
                    FocusIndex = 300 + n  // 300+ 段，避开玩家卡片(100+)与数字索引按钮(101/102)
                };

                _numberButtons[n] = btn;
                _uiControls.Add(btn);
            }

            UpdateNumberPadVisibility();
        }

        /// <summary>
        /// 数字面板按钮是否应该启用 — 由按钮主动调用 EnabledChecker 询问。
        /// 需同时满足：本方操作、处于 ChooseNumber 步骤、且该数字在步骤 ValidNumbers 内
        /// （空列表 = 无限制，即 0-9 全可选）。与状态机 GameState.Handle 的校验保持一致。
        /// </summary>
        private bool ShouldNumberPadBeEnabled(int number)
        {
            if (_gameManager == null) return false;

            if (!_gameManager.Query.IsLocalPlayerSource()) return false;

            if (_gameManager.CurrentActionType != ActionType.ChooseNumber) return false;

            var validNumbers = _gameManager.CurrentStepValidNumbers;
            if (validNumbers.Count > 0)
            {
                return validNumbers.Contains(number);
            }
            return true;
        }

        /// <summary>
        /// 刷新数字面板可见性 — 仅在「本方操作 + ChooseNumber 步骤」时显示，其余步骤隐藏。
        /// 与 ShouldNumberPadBeEnabled 的启用条件一致，避免面板在非相关步骤以灰色禁用态常驻屏幕。
        /// </summary>
        private void UpdateNumberPadVisibility()
        {
            bool visible = _gameManager != null
                && _gameManager.Query.IsLocalPlayerSource()
                && _gameManager.CurrentActionType == ActionType.ChooseNumber;
            if (_numberButtons != null)
                foreach (var btn in _numberButtons)
                    btn.IsVisible = visible;
        }

        /// <summary>
        /// 获取指定玩家 ID 的 UI 玩家控件
        /// </summary>
        public UIPlayer GetUIPlayer(int playerId)
        {
            return _uiPlayers.FirstOrDefault(p => p.Player.PlayerId == playerId);
        }

        /// <summary>
        /// 获取所有 UI 玩家控件
        /// </summary>
        public IReadOnlyList<UIPlayer> GetAllUIPlayers() => _uiPlayers.AsReadOnly();
    }
}
