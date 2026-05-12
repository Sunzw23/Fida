using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Fida.Logic;

namespace Fida.Agent
{
    /// <summary>
    /// AgentManager 类负责管理 AI 代理的行为。
    /// </summary>
    public class AgentManager
    {
        // 定义委托类型用于信号
        public delegate void ChooseNumberIndexEventHandler(int playerId, int numberIndex);
        public delegate void ChooseTargetEventHandler(int targetPlayerId);
        public delegate void DecideApplyEventHandler(bool apply);
        public delegate void ChooseNumberEventHandler(int number);

        public event ChooseNumberIndexEventHandler ChooseNumberIndex;
        public event ChooseTargetEventHandler ChooseTarget;
        public event DecideApplyEventHandler DecideApply;
        public event ChooseNumberEventHandler ChooseNumber;

        private int _actionIndex = -1;
        private readonly List<OnnxInference> _onnxInferences = new List<OnnxInference>();
        private GameManager _gameManager;

        /// <summary>
        /// 设置 GameManager 引用
        /// </summary>
        public void SetGameManager(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        /// <summary>
        /// 加载 AI 模型
        /// </summary>
        public void LoadAgents(int modelNumber = 5)
        {
            _onnxInferences.Clear();
            
            // 尝试多个可能的模型路径
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string contentDirectory = Path.Combine(baseDirectory, "Content");
            string[] possibleBasePaths = new[]
            {
                contentDirectory,  // 首先尝试 Content 目录
                baseDirectory,
                Path.Combine(baseDirectory, "Assets", "Models"),
                Directory.GetCurrentDirectory(),
                Path.Combine(Directory.GetCurrentDirectory(), "Content"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Models")
            };

            for (var i = 0; i < modelNumber; i++)
            {
                string modelPath = null;
                foreach (var basePath in possibleBasePaths)
                {
                    var testPath = Path.Combine(basePath, $"agent_GamePhaseType.CHOOSE_NUMBER_INDEX_{i}.onnx");
                    if (File.Exists(testPath))
                    {
                        modelPath = testPath;
                        Console.WriteLine($"Loaded model: {testPath}");
                        break;
                    }
                }

                if (modelPath != null)
                {
                    try
                    {
                        _onnxInferences.Add(new OnnxInference(modelPath));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load model {i}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Model file not found for agent {i}");
                }
            }
            
            Console.WriteLine($"Loaded {_onnxInferences.Count} models");
        }

        /// <summary>
        /// 处理 AI 需要执行的动作
        /// </summary>
        public void HandleActionRequired(PlayerActionRequiredEventArgs e)
        {
            // 在 MonoGame 版本中，直接同步处理，不使用 async/await
            switch (e.RequiredActionType)
            {
                case ActionType.ChooseNumberIndex:
                    HandleChooseNumberIndex(e.SourcePlayerId);
                    break;
                case ActionType.ChooseTarget:
                    HandleChooseTarget(e.SourcePlayerId);
                    break;
                case ActionType.DecideApply:
                    HandleDecideApply(e.SourcePlayerId);
                    break;
                case ActionType.ChooseNumber:
                    HandleChooseNumber(e.SourcePlayerId);
                    break;
                case ActionType.None:
                    break;
                default:
                    // 对于其他操作，禁用所有按钮
                    break;
            }
        }

        /// <summary>
        /// 获取 AI 的动作（用于 Game1.cs 中调用）
        /// </summary>
        /// <param name="gameManager">GameManager 实例</param>
        /// <param name="sourcePlayerId">动作源玩家 ID，默认为 -1，表示使用 CurrentPlayerId</param>
        public GameAction GetAgentAction(GameManager gameManager, int sourcePlayerId = -1)
        {
            _gameManager = gameManager;
            // 如果没有指定 sourcePlayerId，使用 CurrentPlayerId
            var playerId = sourcePlayerId != -1 ? sourcePlayerId : gameManager.CurrentPlayerId;
            var currentActionType = gameManager.CurrentActionType;

            switch (currentActionType)
            {
                case ActionType.ChooseNumberIndex:
                    return HandleChooseNumberIndexAction(playerId);
                case ActionType.ChooseTarget:
                    return HandleChooseTargetAction(playerId);
                case ActionType.DecideApply:
                    return HandleDecideApplyAction(playerId);
                case ActionType.ChooseNumber:
                    return HandleChooseNumberAction(playerId);
                default:
                    return new GameAction { Type = ActionType.None };
            }
        }

        private GameAction HandleChooseNumberIndexAction(int sourcePlayerId)
        {
            if (_gameManager.IsChooseFirstNumberIndexState())
            {
                var onnxInference = GetOnnxInference(sourcePlayerId);
                if (onnxInference != null)
                {
                    _actionIndex = onnxInference.InferActionIndex(GetState(_gameManager, sourcePlayerId, 0));
                }
                return new GameAction { Type = ActionType.ChooseNumberIndex, TargetPlayerId = sourcePlayerId, NumberIndex = _actionIndex / 3 };
            }
            else
            {
                if (_actionIndex % 3 == 0)
                {
                    return new GameAction { Type = ActionType.ChooseNumberIndex, TargetPlayerId = sourcePlayerId, NumberIndex = 1 - _actionIndex / 3 };
                }
                else
                {
                    return new GameAction { Type = ActionType.ChooseNumberIndex, TargetPlayerId = 0, NumberIndex = _actionIndex % 3 - 1 };
                }
            }
        }

        private GameAction HandleChooseTargetAction(int sourcePlayerId)
        {
            if (_gameManager.IsDamaging())
            {
                // 攻击时，选择一个存活的敌方玩家作为目标
                // 优先选择生命值最低的敌人
                var enemyPlayers = _gameManager.Players.Where(p => p.PlayerId != sourcePlayerId && p.IsAlive()).ToList();
                if (enemyPlayers.Count > 0)
                {
                    var targetPlayer = enemyPlayers.OrderBy(p => p.Health).First();
                    return new GameAction { Type = ActionType.ChooseTarget, TargetPlayerId = targetPlayer.PlayerId };
                }
                return new GameAction { Type = ActionType.ChooseTarget, TargetPlayerId = 0 };
            }
            else if (_gameManager.IsHealing())
            {
                // 治疗时，选择自己或生命值最低的队友
                return new GameAction { Type = ActionType.ChooseTarget, TargetPlayerId = sourcePlayerId };
            }
            else
            {
                return new GameAction { Type = ActionType.ChooseTarget, TargetPlayerId = 0 };
            }
        }

        private GameAction HandleDecideApplyAction(int sourcePlayerId)
        {
            return new GameAction { Type = ActionType.DecideApply, IsApply = true };
        }

        private GameAction HandleChooseNumberAction(int sourcePlayerId)
        {
            return new GameAction { Type = ActionType.ChooseNumber, Number = new Random().Next(6, 10) };
        }

        private void HandleChooseNumberIndex(int sourcePlayerId)
        {
            if (_gameManager.IsChooseFirstNumberIndexState())
            {
                // 选择第一个数字
                var onnxInference = GetOnnxInference(sourcePlayerId);
                if (onnxInference != null)
                {
                    _actionIndex = onnxInference.InferActionIndex(GetState(_gameManager, sourcePlayerId, 0));
                }
                else
                {
                    // 没有模型时使用默认行为：选择第一个数字索引
                    _actionIndex = 0;
                }
                EmitSignalChooseNumberIndex(sourcePlayerId, _actionIndex / 3);
            }
            else
            {
                // 选择第二个数字
                if (_actionIndex % 3 == 0)
                {
                    EmitSignalChooseNumberIndex(sourcePlayerId, 1 - _actionIndex / 3);
                }
                else
                {
                    EmitSignalChooseNumberIndex(0, _actionIndex % 3 - 1);
                }
            }
        }

        private void HandleChooseTarget(int sourcePlayerId)
        {
            if (_gameManager.IsDamaging())
            {
                EmitSignalChooseTarget(0);
            }
            else if (_gameManager.IsHealing())
            {
                EmitSignalChooseTarget(sourcePlayerId);
            }
            else
            {
                EmitSignalChooseTarget(0);
            }
        }

        private void HandleDecideApply(int sourcePlayerId)
        {
            EmitSignalDecideApply(true);
        }

        private void HandleChooseNumber(int sourcePlayerId)
        {
            // 选择数字
            EmitSignalChooseNumber(new Random().Next(6, 10));
        }

        private OnnxInference GetOnnxInference(int i)
        {
            if (i > 0 && i < _onnxInferences.Count)
            {
                return _onnxInferences[i];
            }
            return _onnxInferences.Count > 0 ? _onnxInferences[0] : null;
        }

        private static float[] GetState(GameManager gameManager, int sourcePlayerId, int targetPlayerId)
        {
            var state = new float[6];
            var sourcePlayer = gameManager.Players.First(p => p.PlayerId == sourcePlayerId);
            var targetPlayer = gameManager.Players.First(p => p.PlayerId == targetPlayerId);
            var user = gameManager.Players.First(p => p.PlayerId == 0);
            state[0] = sourcePlayer.Health;
            state[1] = sourcePlayer.Numbers[0];
            state[2] = sourcePlayer.Numbers[1];
            state[3] = user.Health;
            state[4] = targetPlayer.Numbers[0];
            state[5] = targetPlayer.Numbers[1];
            return state;
        }

        // 触发事件的方法（替代 Godot 的 EmitSignal）
        private void EmitSignalChooseNumberIndex(int playerId, int numberIndex)
        {
            ChooseNumberIndex?.Invoke(playerId, numberIndex);
        }

        private void EmitSignalChooseTarget(int targetPlayerId)
        {
            ChooseTarget?.Invoke(targetPlayerId);
        }

        private void EmitSignalDecideApply(bool apply)
        {
            DecideApply?.Invoke(apply);
        }

        private void EmitSignalChooseNumber(int number)
        {
            ChooseNumber?.Invoke(number);
        }
    }
}