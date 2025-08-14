using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{
    public class AgentManager : IDisposable
    {
        public event ChooseNumberIndexHandler OnChooseNumberIndex;
        public event ChooseTargetHandler OnChooseTarget;

        int actionIndex;

        private List<OnnxInference> OnnxInferences = [];

        private GameManager _gameManager;

        private bool loaded = false;

        public AgentManager(GameManager gameManager)
        {
            this.loaded = true;
            LoadAgents();
            _gameManager = gameManager;
        }

        public void LoadAgents()
        {
            string modelsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "onnx_models");
            OnnxInferences.Add(new OnnxInference(Path.Combine(modelsDir, "agent_GamePhaseType.CHOOSE_NUMBER_INDEX_0.onnx")));
            OnnxInferences.Add(new OnnxInference(Path.Combine(modelsDir, "agent_GamePhaseType.CHOOSE_NUMBER_INDEX_1.onnx")));
        }

        public void HandleActionRequired(PlayerActionRequiredEventArgs e)
        {
            switch (e.RequiredActionType)
            {
                case ActionType.ChooseNumberIndex:
                    // 如果是选择数字的操作，启用数字按钮
                    HandleChooseNumberIndex();
                    break;
                case ActionType.ChooseTarget:
                    // 如果是选择目标的操作，启用生命值按钮
                    HandleChooseTarget();
                    break;
                default:
                    // 对于其他操作，禁用所有按钮
                    break;
            }
        }


        public void HandleChooseNumberIndex()
        {
            var sourcePlayerId = _gameManager.CurrentGamePhase.SourcePlayerId;
            System.Threading.Thread.Sleep(500); // 阻塞当前线程 1 秒
            if (_gameManager.CurrentGamePhase.Stage == 0)
            {
                // 选择第一个数字
                var onnxInference = GetOnnxInference();
                actionIndex = onnxInference.InferActionIndex(_GetState(_gameManager, sourcePlayerId, 0));
                OnChooseNumberIndex?.Invoke(sourcePlayerId, actionIndex / 3);
            }
            else if (_gameManager.CurrentGamePhase.Stage == 1)
            {
                // 选择第二个数字
                if (actionIndex % 3 == 0)
                {
                    OnChooseNumberIndex?.Invoke(sourcePlayerId, 1 - actionIndex / 3);
                }
                else
                {
                    OnChooseNumberIndex?.Invoke(0, actionIndex % 3 - 1);
                }
            }
        }

        public void HandleChooseTarget()
        {
            if (_gameManager.IsDamagingButHealing())
            {
                OnChooseTarget?.Invoke(0);
            }
            else
            {
                OnChooseTarget?.Invoke(_gameManager.CurrentGamePhase.SourcePlayerId);
            }
        }



        private OnnxInference GetOnnxInference()
        {
            return OnnxInferences[0];
        }


        private float[] _GetState(GameManager gameManager, int sourcePlayerId, int targetPlayerId)
        {
            float[] state = new float[6];
            var sourcePlayer = gameManager.Players.FirstOrDefault(p => p.PlayerId == sourcePlayerId);
            var targetPlayer = gameManager.Players.FirstOrDefault(p => p.PlayerId == targetPlayerId);
            var user = gameManager.Players.FirstOrDefault(p => p.PlayerId == 0);
            state[0] = sourcePlayer.Health;
            state[1] = sourcePlayer.Numbers[0];
            state[2] = sourcePlayer.Numbers[1];
            state[3] = user.Health;
            state[4] = targetPlayer.Numbers[0];
            state[5] = targetPlayer.Numbers[1];
            return state;
        }



        public void Dispose()
        {
        }



    }
}
