#nullable enable
using System;
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
        private UIBoxLabel _gameMessageLabel = null!;
        private UIBoxLabel _actionCueLabel = null!;
        private UINumberFlowEffect _numberFlowEffect = null!;
        private UICardCombatAnimator _cardCombatAnimator = null!;
        private UIDecideApplyDialog _decideApplyDialog = null!;
        private UINumberSelectionDialog _numberSelectionDialog = null!;
        private GameManager _gameManager = null!;
        private readonly Dictionary<int, Rectangle> _unitBounds = new();

        // 玩家头像纹理字典，key 为 PlayerId，value 为头像纹理（可为 null）
        private Dictionary<int, Texture2D> _portraitTextures = new Dictionary<int, Texture2D>();

        // ─── ChooseNumber 数字面板（0-9）────────────────────────────
        // 「神/仙/迪」技能第二步"把该数字改成几"、「变」技能"变成几点"需要选择 0-9，
        // 此前只有鼠标隐形点击区、手柄完全不可用。这里构建可见按钮面板，
        // 按钮加入 _uiControls，使鼠标/手柄/焦点三条输入路径统一生效。
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
                _gameManager.EffectNumbersConsumed -= OnEffectNumbersConsumed;
            }
            _gameManager = gameManager;
            _uiPlayers.Clear();
            _unitBounds.Clear();

            Viewport viewport = _resourceManager.GraphicsDevice.Viewport;
            Rectangle safeArea = UIMobileSafeArea.Get(viewport);
            int viewportWidth = viewport.Width;
            int messageMargin = System.Math.Clamp(viewportWidth / 20, 12, 48);
            int messageWidth = System.Math.Max(1, viewportWidth - messageMargin * 2);
#if FIDA_ANDROID
            messageMargin = System.Math.Clamp(viewportWidth / 22, 24, 44);
            int pauseSize = 72;
            int pauseGap = 12;
            messageWidth = System.Math.Max(1,
                viewportWidth - messageMargin * 2 - pauseSize - pauseGap);
            var messageBounds = new Rectangle(messageMargin, safeArea.Top + 20, messageWidth, 88);
#else
            int pauseSize = 54;
            int pauseGap = 12;
            messageWidth = System.Math.Max(1,
                viewportWidth - messageMargin * 2 - pauseSize - pauseGap);
            var messageBounds = UIDesktopLayout.MapRectangle(new Rectangle(420, 555, 1080, 60), viewport);
#endif
            _gameMessageLabel = new UIBoxLabel(0, messageBounds, gameManager.GameMessage, _resourceManager.Pixel, _resourceManager.CaptionFont)
            {
                TypographyRole = UITextRole.Body,
                Alignment = UIBoxLabelAlignment.Center,
                AutoFitText = true,
                VerticalTextPadding = UIConfig.ScaleDesktopMetric(4),
#if FIDA_ANDROID
                MinimumTextScale = 0.48f,
                MaximumTextScale = 0.72f,
                TextPadding = 20,
#else
                MinimumTextScale = 0.42f,
                MaximumTextScale = 0.68f,
                TextPadding = 28,
#endif
                BackColor = Color.Transparent,
                ForeColor = UITheme.Ink,
                BorderColor = UITheme.Gold,
#if FIDA_ANDROID
                BorderThickness = 0,
                AccentBar = false,
#else
                BorderThickness = 0,
                AccentBar = false,
#endif
#if FIDA_ANDROID
                SignalRails = false
#else
                SignalRails = false
#endif
            };
#if FIDA_ANDROID
            _gameMessageLabel.Update(FormatMobileCommand(gameManager.GameMessage));
#endif
            _uiControls.Add(_gameMessageLabel);
#if FIDA_ANDROID
            _uiControls.Add(new UIIconButton(-10,
                new Rectangle(messageBounds.Right + pauseGap,
                    messageBounds.Top + (messageBounds.Height - pauseSize) / 2,
                    pauseSize, pauseSize), UIIconButton.IconKind.Pause,
                _resourceManager.Pixel, _resourceManager.Font)
            {
                Name = "pauseBattle"
            });
#else
            _uiControls.Add(new UIIconButton(-10,
                UIDesktopLayout.MapRectangle(new Rectangle(1790, 35, 54, 54), viewport), UIIconButton.IconKind.Pause,
                _resourceManager.Pixel, _resourceManager.Font)
            {
                Name = "pauseBattle"
            });
#endif

            int cueMargin = System.Math.Clamp(viewportWidth / 8, 20, 188);
#if FIDA_ANDROID
            var cueBounds = new Rectangle(cueMargin, safeArea.Top + 88,
                System.Math.Max(1, viewportWidth - cueMargin * 2), 56);
#else
            var cueBounds = messageBounds;
#endif
            _actionCueLabel = new UIBoxLabel(-1, cueBounds, "", _resourceManager.Pixel, _resourceManager.CaptionFont)
            {
                TypographyRole = UITextRole.Body,
                Alignment = UIBoxLabelAlignment.Center,
                AutoFitText = true,
                MinimumTextScale = 0.42f,
                MaximumTextScale = 0.68f,
                TextPadding = 28,
                VerticalTextPadding = UIConfig.ScaleDesktopMetric(4),
                BackColor = UITheme.BgTop,
                ForeColor = UITheme.Ink,
                BorderColor = UITheme.GoldBright,
#if FIDA_ANDROID
                BorderThickness = 2,
                AccentBar = false,
#else
                BorderThickness = 0,
                AccentBar = false,
#endif
                IsVisible = false
            };
            _uiControls.Add(_actionCueLabel);
#if !FIDA_ANDROID
            _uiControls.Add(new UIBattleChrome(-11, _resourceManager.Pixel, _resourceManager.CaptionFont, gameManager));
#endif

            // 订阅事件
            _gameManager.MessageUpdated += OnMessageUpdated;
            _gameManager.GameStateChanged += OnGameStateChanged;
            _gameManager.PlayerActionRequired += OnPlayerActionRequired;
            _gameManager.EffectNumbersConsumed += OnEffectNumbersConsumed;

            // 计算每个玩家的绘制位置（基于分组）
            var positions = ComputePlayerPositions(gameManager);

            for (int i = 0; i < gameManager.Players.Count; i++)
            {
                var player = gameManager.Players[i];
                
                // Explicit per-player overrides take precedence; data-driven portrait keys
                // then resolve through the resource catalog with a safe team fallback.
                if (!_portraitTextures.TryGetValue(player.PlayerId, out Texture2D? portraitTexture) ||
                    portraitTexture == null)
                {
                    portraitTexture = _resourceManager.GetPortrait(
                        player.PortraitKey,
                        player.PlayerId == gameManager.LocalPlayerId);
                }
                
                bool isAlly = gameManager.Groups.Any(group =>
                    group.Players.Any(member => member.PlayerId == gameManager.LocalPlayerId) &&
                    group.Players.Any(member => member.PlayerId == player.PlayerId));
                isAlly |= player.PlayerId == gameManager.LocalPlayerId;
                var uiPlayer = new UIPlayer(player.PlayerId, player, positions[player.PlayerId],
                    _resourceManager.Font, _resourceManager.Pixel, portraitTexture, isAlly,
                    _unitBounds.TryGetValue(player.PlayerId, out var unitBounds) ? unitBounds : null,
                    _resourceManager.LabelFont, _resourceManager.CaptionFont);
                if (portraitTexture == _resourceManager.GetTexture("cold_roster"))
                    uiPlayer.PortraitSource = ColdPortraitRegion(player, portraitTexture, isAlly);
                uiPlayer.CurrentTurnChecker = () => _gameManager.Query.CurrentPlayerId == player.PlayerId;
                
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

            // ─── 构建 ChooseNumber 模态窗口（0-9，2 行 × 5 列）─────────────
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

#if FIDA_ANDROID
            // Portrait layout: each team is a readable vertical stack. A card
            // always spans the safe width, so names, skill icons and number
            // controls remain large enough for touch interaction.
            int viewportWidth = _resourceManager.GraphicsDevice.Viewport.Width;
            Rectangle safeArea = UIMobileSafeArea.Get(_resourceManager.GraphicsDevice.Viewport);
            int mobileCardWidth = UIConfig.Int("cardWidth");
            int mobileCardHeight = UIConfig.Int("cardHeight");
            int gap = Math.Clamp(mobileCardHeight / 10, 16, 22);
            int centerX = Math.Max(safeArea.Left,
                safeArea.Left + (safeArea.Width - mobileCardWidth) / 2);

            Group? localGroupMobile = null;
            foreach (var group in gameManager.Groups)
            {
                if (group.Players.Any(p => p.PlayerId == gameManager.LocalPlayerId))
                {
                    localGroupMobile = group;
                    break;
                }
            }

            var localPlayersMobile = localGroupMobile?.Players.ToList() ??
                gameManager.Players.Where(p => p.PlayerId == gameManager.LocalPlayerId).ToList();
            var enemyPlayersMobile = gameManager.Players
                .Where(p => !localPlayersMobile.Any(local => local.PlayerId == p.PlayerId)).ToList();

            int localHeight = localPlayersMobile.Count * mobileCardHeight + Math.Max(0, localPlayersMobile.Count - 1) * gap;
            int localStartY = Math.Max(safeArea.Top + 150,
                safeArea.Bottom - localHeight - 20);
            for (int i = 0; i < localPlayersMobile.Count; i++)
                positions[localPlayersMobile[i].PlayerId] = new Vector2(centerX, localStartY + i * (mobileCardHeight + gap));

            for (int i = 0; i < enemyPlayersMobile.Count; i++)
                positions[enemyPlayersMobile[i].PlayerId] = new Vector2(centerX,
                    safeArea.Top + 172 + i * (mobileCardHeight + gap));

            // Defensive fallback for unusual network rosters that are not in a
            // group yet: every player still receives a deterministic card slot.
            for (int i = 0; i < gameManager.Players.Count; i++)
                if (!positions.ContainsKey(gameManager.Players[i].PlayerId))
                    positions[gameManager.Players[i].PlayerId] = new Vector2(centerX,
                        safeArea.Top + 150 + i * (mobileCardHeight + gap));
            return positions;
#else

            Viewport desktopViewport = _resourceManager.GraphicsDevice.Viewport;
            void PlaceRow(IReadOnlyList<Player> row, int designY)
            {
                if (row.Count == 0) return;
                for (int i = 0; i < row.Count; i++)
                {
                    Rectangle slot = UIBattleLayout.Unit(i, row.Count, designY == SceneConfig.RowBottomY,
                        desktopViewport.Width, desktopViewport.Height);
                    _unitBounds[row[i].PlayerId] = slot;
                    positions[row[i].PlayerId] = slot.Location.ToVector2();
                }
            }

            // 找到本地玩家所在的组
            Group? localGroup = null;
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

            // 如果没有分组（单机 PvP），使用默认的两位置布局
            if (localGroup == null)
            {
                var soloLocalPlayers = gameManager.Players
                    .Where(player => player.PlayerId == gameManager.LocalPlayerId).ToList();
                var opponents = gameManager.Players
                    .Where(player => player.PlayerId != gameManager.LocalPlayerId).ToList();
                PlaceRow(opponents, SceneConfig.RowTopY);
                PlaceRow(soloLocalPlayers, SceneConfig.RowBottomY);
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

            PlaceRow(localPlayers, SceneConfig.RowBottomY);
            PlaceRow(enemyPlayers, SceneConfig.RowTopY);

            return positions;
#endif
        }

        private static Rectangle ColdPortraitRegion(Player player, Texture2D texture, bool ally)
        {
            string key = (player.PortraitKey ?? "").ToLowerInvariant();
            string name = player.Name.ToLowerInvariant();
            int index = key.Contains("withered") || name.Contains("king") ? 3 :
                key.Contains("tutorial_ally") || key.Contains("mara") || name.Contains("mara") ? 1 :
                key.Contains("arith") || ally && player.PlayerId == 0 ? 0 :
                key.Contains("sentinel") || name.Contains("sentinel") ? 2 :
                key.Contains("echo") || name.Contains("echo") ? 5 :
                ally ? 2 : 4;
            return new Rectangle(index % 3 * (texture.Width / 3), index / 3 * (texture.Height / 2),
                texture.Width / 3, texture.Height / 2);
        }

        public void UpdateGameMessage(string message)
        {
#if FIDA_ANDROID
            _gameMessageLabel?.Update(FormatMobileCommand(message));
#else
            _gameMessageLabel?.Update(message);
#endif
        }

#if FIDA_ANDROID
        private static string FormatMobileCommand(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "AWAITING COMMAND";
            string command = message.Trim();
            int comma = command.IndexOf(',');
            if (comma >= 0 && comma + 1 < command.Length)
                command = command[(comma + 1)..].Trim();
            int separator = command.IndexOf(" - ", StringComparison.Ordinal);
            if (separator >= 0 && separator + 3 < command.Length)
                command = command[(separator + 3)..].Trim();
            return command.TrimEnd(':', '.', ' ').ToUpperInvariant();
        }
#endif

        /// <summary>Shows the current AI decision beat and highlights its card.</summary>
        public void ShowActionCue(int playerId, string message)
        {
            foreach (var uiPlayer in _uiPlayers)
                uiPlayer.IsPresentingAction = uiPlayer.Player.PlayerId == playerId;

            if (_actionCueLabel == null) return;
#if FIDA_ANDROID
            _gameMessageLabel?.Update(FormatMobileCommand(message));
            _actionCueLabel.IsVisible = false;
#else
            _actionCueLabel.Update(message);
            _actionCueLabel.IsVisible = true;
            if (_gameMessageLabel != null) _gameMessageLabel.IsVisible = false;
#endif
        }

        public void HideActionCue()
        {
            foreach (var uiPlayer in _uiPlayers)
                uiPlayer.IsPresentingAction = false;
            if (_actionCueLabel != null) _actionCueLabel.IsVisible = false;
#if !FIDA_ANDROID
            if (_gameMessageLabel != null) _gameMessageLabel.IsVisible = true;
#endif
#if FIDA_ANDROID
            if (_gameManager != null)
                _gameMessageLabel?.Update(FormatMobileCommand(_gameManager.GameMessage));
#endif
        }

        public bool IsNumberFlowActive => _numberFlowEffect?.IsPlaying == true;
        public bool IsCardCombatAnimationActive => _cardCombatAnimator?.IsPlaying == true;
        public void PlayReinforcementEntrance(IEnumerable<int> playerIds)
        {
            foreach (int id in playerIds)
                GetUIPlayer(id)?.BeginReinforcementEntrance();
        }
        public bool IsBlockingPresentationActive =>
            IsNumberFlowActive || IsCardCombatAnimationActive;

        /// <summary>
        /// Sends a brief ice-colored trace from the selected addend to the base number.
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
            if (source == null || target == null)
            {
                // Do not leave a consumed value visually frozen if a card was
                // removed between effect resolution and presentation.
                source?.CommitDeferredNumberVisual();
                return;
            }
            HideActionCue();
            _cardCombatAnimator?.Start(source, target, damaging);
        }

        private void OnMessageUpdated(object? sender, GameEventArgs e)
        {
            UpdateGameMessage(e.Message);
            // 不再需要手动更新按钮状态 - 按钮会通过 EnabledChecker 自动询问环境
        }

        /// <summary>
        /// 游戏状态变化时刷新所有按钮状态
        /// </summary>
        private void OnGameStateChanged(object? sender, GameStateEventArgs e)
        {
            HideDecideApplyDialog();
            HideNumberSelectionDialog();
            // 游戏状态变化时，强制刷新所有按钮的状态
            // 通过清除上一帧的状态记录，让按钮在下次 Update 时重新计算并通知状态变化
            foreach (var uiPlayer in _uiPlayers)
            {
                uiPlayer.RefreshButtons();
            }
            UpdateNumberPadVisibility();
        }

        /// <summary>Refreshes contextual controls whenever the required action changes.</summary>
        private void OnPlayerActionRequired(object? sender, PlayerActionRequiredEventArgs e)
        {
            // 每次步骤切换时（包括同状态内 StepIndex++），更新按钮的 Selected 状态
            UpdateButtonSelectedStates();
            UpdateNumberPadVisibility();
            if (e.RequiredActionType == ActionType.DecideApply &&
                e.SourcePlayerId == _gameManager.LocalPlayerId)
                ShowDecideApplyDialog(e.Message);
            else
                HideDecideApplyDialog();

            if (e.RequiredActionType == ActionType.ChooseNumber &&
                e.SourcePlayerId == _gameManager.LocalPlayerId)
                ShowNumberSelectionDialog(e.Message);
            else
                HideNumberSelectionDialog();
        }

        private void OnEffectNumbersConsumed(object? sender, EffectNumbersConsumedEventArgs e)
        {
            GetUIPlayer(e.SourcePlayerId)?.BeginDeferredNumberVisual();
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

        private void ShowNumberSelectionDialog(string prompt)
        {
            if (_numberSelectionDialog == null) return;
            _numberSelectionDialog.Show(prompt, _gameManager.CurrentStepValidNumbers);
            foreach (var control in _uiControls)
                if (control.IsFocused) control.SetFocus(false);

            int first = Enumerable.Range(0, 10).FirstOrDefault(number =>
                _gameManager.CurrentStepValidNumbers.Count == 0 ||
                _gameManager.CurrentStepValidNumbers.Contains(number));
            _numberSelectionDialog.NumberButtons[first].SetFocus(true);
        }

        private void HideNumberSelectionDialog()
        {
            if (_numberSelectionDialog == null) return;
            _numberSelectionDialog.Hide();
            foreach (var button in _numberSelectionDialog.NumberButtons)
                button.SetFocus(false);
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
            _numberSelectionDialog = new UINumberSelectionDialog(-5,
                _resourceManager.GraphicsDevice, _resourceManager.Pixel, _resourceManager.Font);
            _uiControls.Add(_numberSelectionDialog);
            for (int n = 0; n < _numberSelectionDialog.NumberButtons.Length; n++)
            {
                int number = n;
                var button = _numberSelectionDialog.NumberButtons[n];
                button.EnabledChecker = () => ShouldNumberPadBeEnabled(number);
                _uiControls.Add(button);
            }
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
            if (!visible) HideNumberSelectionDialog();
        }

        /// <summary>
        /// 获取指定玩家 ID 的 UI 玩家控件（该 ID 无对应卡片时返回 null）
        /// </summary>
        public UIPlayer GetUIPlayer(int playerId)
        {
            // Absent ids legitimately yield null and every caller already guards with
            // ?. / null checks. The declared type stays non-nullable so the pass-through
            // in UIManager.GetUIPlayer keeps its existing signature.
            return _uiPlayers.FirstOrDefault(p => p.Player.PlayerId == playerId)!;
        }

        /// <summary>
        /// 获取所有 UI 玩家控件
        /// </summary>
        public IReadOnlyList<UIPlayer> GetAllUIPlayers() => _uiPlayers.AsReadOnly();
    }
}
