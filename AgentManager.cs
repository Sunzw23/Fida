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

        int actionIndex = -1;

        private List<OnnxInference> OnnxInferences = [];

        private bool loaded = false;
        private GameManager _gameManager;

        public AgentManager(GameManager gameManager)
        {
            this.loaded = true;
            LoadAgents();
            _gameManager = gameManager;
        }

        public void LoadAgents()
        {
            string modelsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "onnx_models");
            OnnxInferences.Add(new OnnxInference(Path.Combine(modelsDir, "agent_GamePhaseType.CHOOSE_NUMBER_INDEX_4.onnx")));
            OnnxInferences.Add(new OnnxInference(Path.Combine(modelsDir, "agent_GamePhaseType.CHOOSE_NUMBER_INDEX_5.onnx")));
        }

        public void HandleActionRequired(PlayerActionRequiredEventArgs e)
        {
            switch (e.RequiredActionType)
            {
                case ActionType.ChooseNumberIndex:
                    HandleChooseNumberIndex(e.SourcePlayerId);
                    break;
                case ActionType.ChooseTarget:
                    HandleChooseTarget();
                    break;
                default:
                    // 对于其他操作，禁用所有按钮
                    break;
            }
        }


        public async void HandleChooseNumberIndex(int sourcePlayerId)
        {
            await Task.Delay(500);

            if (_gameManager.IsChooseFirstNumberIndexState())
            {
                // 选择第一个数字
                var onnxInference = GetOnnxInference(sourcePlayerId);
                actionIndex = onnxInference.InferActionIndex(_GetState(_gameManager, sourcePlayerId, 0));
                OnChooseNumberIndex?.Invoke(sourcePlayerId, actionIndex / 3);
            }
            else
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
            if (_gameManager.IsDamaging())
            {
                OnChooseTarget?.Invoke(0);
            }
            else
            {
                OnChooseTarget?.Invoke(_gameManager.CurrentPlayerId);
            }
        }



        private OnnxInference GetOnnxInference(int i)
        {
            if (i > 0 && i < OnnxInferences.Count) 
            {
                return OnnxInferences[i];
            }
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
