using System;
using Fida.Campaign;
using Fida.Logic;
using Fida.Network;
using Fida.UI.Campaign;
using Microsoft.Xna.Framework;

namespace Fida.States;

/// <summary>Online campaign map presence and independent chapter selection.</summary>
public sealed class OnlineCampaignMapState : StateBase
{
    private string _lastNode = "";

    public OnlineCampaignMapState(UI.UIManager uiManager, NetworkManager networkManager,
        GameStateMachine stateMachine) : base(uiManager, networkManager, stateMachine) { }

    public override void OnEnter()
    {
        _lastNode = "";
        _networkManager.IsCampaignSharedBattle = false;
        _networkManager.CampaignSessionId = string.Empty;
        Publish(CampaignOnlinePhase.Map, "", false);
    }

    public override void HandleInput(GameTime gameTime)
    {
        base.HandleInput(gameTime);
        _networkManager.ProcessCampaignMessages();
        var map = _uiManager.GetControlByName<UICampaignMap>("campaign_map");
        if (map == null) return;

        var remote = _networkManager.RemoteCampaignPresence;
        map.RemoteNodeId = remote.NodeId;
        map.RemoteStatusLabel = remote.Phase switch
        {
            CampaignOnlinePhase.Loadout => "ALLY · LOADOUT",
            CampaignOnlinePhase.Battle => "ALLY · IN BATTLE",
            _ => "ALLY · BROWSING"
        };

        string selected = map.SelectedNodeId;
        if (selected != _lastNode)
        {
            _lastNode = selected;
            Publish(CampaignOnlinePhase.Map, selected, false);
        }

        if (map.ConsumeBackRequest())
        {
            _stateMachine.SwitchToState(State.MainMenu);
            return;
        }

        if (map.TryConsumeSelectedNode(out string nodeId) && GameSetup.SelectCampaignNode(nodeId, online: true))
        {
            Publish(CampaignOnlinePhase.Loadout, nodeId, false);
            _stateMachine.SwitchToState(State.OnlineCampaignSkills);
        }
    }

    private void Publish(CampaignOnlinePhase phase, string nodeId, bool ready) =>
        _networkManager.SendCampaignPresence(new CampaignPresence(_networkManager.GetLocalPlayerId(), phase, nodeId, ready));
}

/// <summary>Per-player skill draft. Matching drafts require both players to ready.</summary>
public sealed class OnlineCampaignSkillsState : StateBase
{
    private bool _ready;
    private bool _coopStarted;

    public OnlineCampaignSkillsState(UI.UIManager uiManager, NetworkManager networkManager,
        GameStateMachine stateMachine) : base(uiManager, networkManager, stateMachine) { }

    public override void OnEnter()
    {
        _ready = false;
        _coopStarted = false;
        _networkManager.CampaignCoopStarting += OnCoopStarting;
        Publish(CampaignOnlinePhase.Loadout, GameSetup.SelectedNodeId, false);
    }

    public override void OnExit()
    {
        _networkManager.CampaignCoopStarting -= OnCoopStarting;
        base.OnExit();
    }

    public override void HandleInput(GameTime gameTime)
    {
        _networkManager.ProcessCampaignMessages();
        if (HandleStoryInput(gameTime)) return;
        base.HandleInput(gameTime);
        var wheel = _uiManager.GetControlByName<UICampaignSkillWheel>("skill_wheel");
        if (wheel == null) return;

        var remote = _networkManager.RemoteCampaignPresence;
        bool matchingDraft = remote.Phase == CampaignOnlinePhase.Loadout &&
            string.Equals(remote.NodeId, GameSetup.SelectedNodeId, StringComparison.OrdinalIgnoreCase);
        bool joiningBattle = remote.Phase == CampaignOnlinePhase.Battle &&
            string.Equals(remote.NodeId, GameSetup.SelectedNodeId, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(remote.SessionId);
        wheel.StatusText = matchingDraft
            ? (remote.Ready ? "ALLY READY" : "ALLY SELECTING")
            : remote.Phase == CampaignOnlinePhase.Battle && remote.NodeId == GameSetup.SelectedNodeId
                ? "ALLY IN BATTLE · REINFORCEMENT AVAILABLE"
                : "SOLO ROUTE";
        wheel.StartActionEnabled = !_ready;
        wheel.SelectionEnabled = !_ready;
        wheel.StartActionText = matchingDraft
            ? (_ready ? "WAITING FOR ALLY" : "READY")
            : joiningBattle
                ? (_ready ? "JOINING BATTLE" : "JOIN BATTLE")
            : "START BATTLE";

        if (_ready && _networkManager.PendingBattleSnapshot is { } pendingSnapshot)
        {
            if (string.Equals(pendingSnapshot.NodeId, GameSetup.SelectedNodeId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(pendingSnapshot.SessionId, remote.SessionId, StringComparison.Ordinal))
            {
                _networkManager.IsCampaignSharedBattle = true;
                _networkManager.CampaignSessionId = pendingSnapshot.SessionId;
                Publish(CampaignOnlinePhase.Battle, GameSetup.SelectedNodeId, false, GameSetup.SelectedLocalSkills,
                    pendingSnapshot.SessionId);
                _stateMachine.SwitchToState(State.OnlinePlaying);
                return;
            }
        }

        if (wheel.ConsumeBackRequest())
        {
            Publish(CampaignOnlinePhase.Map, "", false);
            _stateMachine.SwitchToState(State.OnlineCampaignMap);
            return;
        }
        if (wheel.TryConsumeToggle(out SkillType skill) && !_ready) GameSetup.ToggleCampaignSkill(skill, online: true);

        if (!_ready && (wheel.ConsumeStartRequest() ||
            !CampaignLoadout.HasSelectableSkills(CampaignRuntime.OnlineProgress, GameSetup.SelectedNode)))
        {
            _ready = true;
            Publish(CampaignOnlinePhase.Loadout, GameSetup.SelectedNodeId, true, GameSetup.SelectedLocalSkills);
            if (matchingDraft)
            {
                _networkManager.SendCampaignReady(GameSetup.SelectedNodeId);
                TryStartCoop();
            }
            else if (joiningBattle)
            {
                _networkManager.RequestBattleJoin(remote.SessionId);
                _ready = true;
            }
            else
            {
                // Solo start is independent; the other player remains on their own route.
                _networkManager.IsCampaignSharedBattle = false;
                Publish(CampaignOnlinePhase.Battle, GameSetup.SelectedNodeId, false, GameSetup.SelectedLocalSkills);
                _stateMachine.SwitchToState(State.OnlinePlaying);
                return;
            }
        }

        // If the other player leaves this node's draft after we marked ready,
        // honor the already-confirmed choice as a solo start instead of trapping us.
        if (_ready && !_coopStarted && !matchingDraft && !joiningBattle)
        {
            _networkManager.IsCampaignSharedBattle = false;
            Publish(CampaignOnlinePhase.Battle, GameSetup.SelectedNodeId, false, GameSetup.SelectedLocalSkills);
            _stateMachine.SwitchToState(State.OnlinePlaying);
            return;
        }

        if (_ready && !_coopStarted && _networkManager.IsHost) TryStartCoop();
        if (_coopStarted)
        {
            _networkManager.IsCampaignSharedBattle = true;
            Publish(CampaignOnlinePhase.Battle, GameSetup.SelectedNodeId, false, GameSetup.SelectedLocalSkills,
                _networkManager.CampaignSessionId);
            _stateMachine.SwitchToState(State.OnlinePlaying);
        }
    }

    private void TryStartCoop()
    {
        var remote = _networkManager.RemoteCampaignPresence;
        if (!_networkManager.IsHost || !remote.Ready || remote.Phase != CampaignOnlinePhase.Loadout ||
            !string.Equals(remote.NodeId, GameSetup.SelectedNodeId, StringComparison.OrdinalIgnoreCase)) return;
        _networkManager.CampaignSessionId = Guid.NewGuid().ToString("N");
        _networkManager.SendCampaignCoopStart(GameSetup.SelectedNodeId, _networkManager.CampaignSessionId);
        _networkManager.IsCampaignSharedBattle = true;
        _coopStarted = true;
    }

    private void OnCoopStarting(string nodeId, string sessionId)
    {
        if (string.Equals(nodeId, GameSetup.SelectedNodeId, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(sessionId))
        {
            _networkManager.CampaignSessionId = sessionId;
            _coopStarted = true;
        }
    }

    private void Publish(CampaignOnlinePhase phase, string nodeId, bool ready,
        System.Collections.Generic.IReadOnlyCollection<SkillType> skills = null, string sessionId = "") =>
        _networkManager.SendCampaignPresence(new CampaignPresence(_networkManager.GetLocalPlayerId(), phase,
            nodeId, ready, skills, sessionId));
}
