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
        private GameManager _gameManager;

        // 玩家头像纹理字典，key 为 PlayerId，value 为头像纹理（可为 null）
        private Dictionary<int, Texture2D> _portraitTextures = new Dictionary<int, Texture2D>();

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
            _gameManager = gameManager;
            _uiPlayers.Clear();

            _gameMessageLabel = new UIBoxLabel(0, UIConfig.Rect("gameMessageArea"), gameManager.GameMessage, _resourceManager.Pixel, _resourceManager.Font)
            {
                Alignment = UIBoxLabelAlignment.Left,
                BorderThickness = 0
            };
            _uiControls.Add(_gameMessageLabel);

            // 订阅事件
            _gameManager.MessageUpdated += OnMessageUpdated;
            _gameManager.GameStateChanged += OnGameStateChanged;
            _gameManager.PlayerActionRequired += OnPlayerActionRequired;

            // 计算每个玩家的绘制位置（基于分组）
            var positions = ComputePlayerPositions(gameManager);

            for (int i = 0; i < gameManager.Players.Count; i++)
            {
                var player = gameManager.Players[i];
                
                // 尝试获取该玩家的头像纹理，如果未设置则为 null（不显示头像）
                _portraitTextures.TryGetValue(player.PlayerId, out Texture2D portraitTexture);
                
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
            // 游戏状态变化时，强制刷新所有按钮的状态
            // 通过清除上一帧的状态记录，让按钮在下次 Update 时重新计算并通知状态变化
            foreach (var uiPlayer in _uiPlayers)
            {
                uiPlayer.RefreshButtons();
            }
        }

        /// <summary>
        /// 玩家行动请求事件 — 在 BroadcastCurrentStep 时触发，是标记 Selected 的正确时机
        /// </summary>
        private void OnPlayerActionRequired(object sender, PlayerActionRequiredEventArgs e)
        {
            // 每次步骤切换时（包括同状态内 StepIndex++），更新按钮的 Selected 状态
            UpdateButtonSelectedStates();
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
            
            // 非本地玩家回合时禁用所有按钮
            if (_gameManager.Query.CurrentPlayerId != _gameManager.LocalPlayerId) return false;
            
            // 不是选择数字索引操作时禁用
            if (_gameManager.CurrentActionType != ActionType.ChooseNumberIndex) return false;
            
            bool isFirstStage = _gameManager.Query.IsChooseFirstNumberIndexState();
            
            // 第一阶段：只有当前操作的玩家可以选择
            if (isFirstStage)
            {
                return playerId == _gameManager.Query.CurrentPlayerId && _gameManager.Players[playerId].IsAlive();
            }
            // 第二阶段：所有存活玩家的数字都可以选择（包括自己的另一个数字）
            else
            {
                return _gameManager.Players[playerId].IsAlive();
            }
        }

        /// <summary>
        /// 玩家卡片是否应该启用交互 — ChooseTarget 时所有存活玩家的卡片可被点击
        /// </summary>
        private bool ShouldPlayerCardBeEnabled(int playerId)
        {
            if (_gameManager == null) return false;
            if (_gameManager.Query.CurrentPlayerId != _gameManager.LocalPlayerId) return false;
            if (_gameManager.CurrentActionType != ActionType.ChooseTarget) return false;
            return _gameManager.Players[playerId].IsAlive();
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