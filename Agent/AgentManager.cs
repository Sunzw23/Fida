#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Fida.Logic;

namespace Fida.Agent
{
    /// <summary>保留现有事件与控制器入口，决策使用无外部推理依赖的轻量 Agent。</summary>
    public class AgentManager
    {
        public delegate void ChooseNumberIndexEventHandler(int playerId, int numberIndex);
        public delegate void ChooseTargetEventHandler(int targetPlayerId);
        public delegate void DecideApplyEventHandler(bool apply);
        public delegate void ChooseNumberEventHandler(int number);
        public event ChooseNumberIndexEventHandler? ChooseNumberIndex;
        public event ChooseTargetEventHandler? ChooseTarget;
        public event DecideApplyEventHandler? DecideApply;
        public event ChooseNumberEventHandler? ChooseNumber;
        // 由 SetGameManager/GetAgentAction 在游戏开始后赋值；Decide 中仍保留空值保护。
        private GameManager _gameManager = null!;
        private readonly Dictionary<int, LightweightAgent> _agents = new();
        public void SetGameManager(GameManager gameManager) => _gameManager = gameManager;

        public void LoadAgents(int modelNumber = 5)
        {
            _agents.Clear();
            string path = Path.Combine(AppContext.BaseDirectory, "Content", "agent-settings.json");
            try { AgentDefaults.Profile = AgentProfile.Load(path); }
            catch (Exception ex) when (ex is IOException || ex is System.Text.Json.JsonException || ex is ArgumentException)
            {
                AgentDefaults.Profile = new();
                Console.WriteLine($"Invalid agent settings, using strength 65: {ex.Message}");
            }
            var table = AgentDefaults.Table.Value; // 必须成功加载，模型缺失或损坏时显式报错。
            Console.WriteLine($"Lightweight agents ready, strength {AgentDefaults.Profile.Strength}, table {table != null}");
        }

        public void SetStrength(int strength)
        {
            var profile = AgentDefaults.Profile with { Strength = strength };
            profile.Validate();
            AgentDefaults.Profile = profile;
            _agents.Clear();
        }

        public GameAction GetAgentAction(GameManager gameManager, int sourcePlayerId = -1)
        {
            _gameManager = gameManager;
            int playerId = sourcePlayerId >= 0 ? sourcePlayerId : gameManager.CurrentState?.ActorId ?? gameManager.Query.CurrentPlayerId;
            return Decide(new PlayerActionRequiredEventArgs
            {
                SourcePlayerId = playerId,
                RequiredActionType = gameManager.CurrentActionType,
                ValidTargets = gameManager.CurrentStepValidTargets,
                ValidNumbers = gameManager.CurrentStepValidNumbers
            });
        }

        private GameAction Decide(PlayerActionRequiredEventArgs args)
        {
            if (_gameManager == null) return new GameAction(ActionType.None);
            if (!_agents.TryGetValue(args.SourcePlayerId, out var agent))
                _agents[args.SourcePlayerId] = agent = new LightweightAgent();
            return agent.Decide(_gameManager, args);
        }

        public void HandleActionRequired(PlayerActionRequiredEventArgs e)
        {
            var action = Decide(e);
            switch (action.Type)
            {
                case ActionType.ChooseNumberIndex: ChooseNumberIndex?.Invoke(action.TargetPlayerId, action.NumberIndex); break;
                case ActionType.ChooseTarget: ChooseTarget?.Invoke(action.TargetPlayerId); break;
                case ActionType.DecideApply: DecideApply?.Invoke(action.IsApply); break;
                case ActionType.ChooseNumber: ChooseNumber?.Invoke(action.Number); break;
            }
        }
    }
}
