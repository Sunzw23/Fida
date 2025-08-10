using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{
    public class AgentManager: IDisposable
    {
        int _action_index;

        private OnnxInference _infer_number_index_0;
        private OnnxInference _infer_number_index_1;

        private bool loaded = false;

        public AgentManager()
        {
            this.loaded = true;
            LoadAgents();
        }

        public void LoadAgents()
        {
            string modelsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "onnx_models");
            _infer_number_index_0 = new OnnxInference(Path.Combine(modelsDir, "agent_GamePhaseType.CHOOSE_NUMBER_INDEX_0.onnx"));
            _infer_number_index_1 = new OnnxInference(Path.Combine(modelsDir, "agent_GamePhaseType.CHOOSE_NUMBER_INDEX_1.onnx"));
        }

        public void Dispose()
        {
        }

        public Action UpdateAction(GameManager gameManager)
        {
            var sourcePlayerId = gameManager.CurrentGamePhase.SourcePlayerId;
            if (gameManager.CurrentGamePhase.Type == GamePhaseType.ChooseNumberIndex)
            {
                if (gameManager.CurrentGamePhase.Stage == 0)
                {
                    // 选择第一个数字
                    OnnxInference _infer_number_index;
                    if (gameManager.CurrentGamePhase.SourcePlayerId == 1)
                    {
                        _infer_number_index = _infer_number_index_0;
                    }
                    else
                    {
                        _infer_number_index = _infer_number_index_1;
                    }
                    _action_index = _infer_number_index.InferActionIndex(_GetState(gameManager, sourcePlayerId, 0));
                    return new Action { Type = ActionType.ChooseNumberIndex, TargetPlayerId = sourcePlayerId, NumberIndex = _action_index / 3 };
                }
                else if (gameManager.CurrentGamePhase.Stage == 1)
                {
                    // 选择第二个数字
                    if (_action_index % 3 == 0)
                    {
                        return new Action { Type = ActionType.ChooseNumberIndex, TargetPlayerId = sourcePlayerId, NumberIndex = 1 - _action_index / 3 };
                    }
                    else
                    {
                        return new Action { Type = ActionType.ChooseNumberIndex, TargetPlayerId = 0, NumberIndex = _action_index % 3 - 1 };
                    }
                }
                    
            }
            else if (gameManager.CurrentGamePhase.Type == GamePhaseType.TargetSelection)
            {
                if (gameManager.GameMessage.Contains("治愈"))
                {
                    return new Action { Type = ActionType.ChooseTarget, TargetPlayerId = sourcePlayerId };
                }
                else if (gameManager.GameMessage.Contains("攻击"))
                {
                    return new Action { Type = ActionType.ChooseTarget, TargetPlayerId = 0 };
                }

            }

            return new Action { Type = ActionType.None};
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

    
    }
    

}
