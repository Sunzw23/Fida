#nullable enable
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
            // Leaving the campaign must not leave its session behind: a Duel started
            // next inherited the co-op token, the shared-battle flag and the last
            // presence, which could route the duel as a campaign battle.
            _networkManager.SendCampaignPresence(new CampaignPresence(
                _networkManager.GetLocalPlayerId(), CampaignOnlinePhase.Map, "", false));
            _networkManager.ResetCampaignSession();
            GameSetup.ClearCampaignSelection();
            _stateMachine.SwitchToState(State.MainMenu);
            return;
        }

        if (map.TryConsumeSelectedNode(out string nodeId) && GameSetup.SelectCampaignNode(nodeId, online: true))
        {
            Publish(CampaignOnlinePhase.Loadout, nodeId, false);
            _stateMachine.SwitchToState(CampaignLoadout.HasCompanionChoices(GameSetup.SelectedNode,
                CampaignRuntime.OnlineProgress)
                ? State.OnlineCompanionSelect
                : State.OnlineCampaignSkills);
        }
    }

    private void Publish(CampaignOnlinePhase phase, string nodeId, bool ready) =>
        _networkManager.SendCampaignPresence(new CampaignPresence(_networkManager.GetLocalPlayerId(), phase, nodeId, ready));
}

/// <summary>
/// Online companion preparation. The party is a solo-route decision, so a co-op draft hides
/// the chooser: the partner already fills the second ally slot.
/// </summary>
public sealed class OnlineCompanionSelectState : StateBase
{
    public OnlineCompanionSelectState(UI.UIManager uiManager, NetworkManager networkManager,
        GameStateMachine stateMachine) : base(uiManager, networkManager, stateMachine) { }

    public override void OnEnter() => Publish();

    public override void HandleInput(GameTime gameTime)
    {
        _networkManager.ProcessCampaignMessages();
        if (HandleStoryInput(gameTime)) return;
        base.HandleInput(gameTime);
        var screen = _uiManager.GetControlByName<UICampaignCompanionSelect>("companion_select");
        if (screen == null) return;
        screen.SetSelection(GameSetup.SelectedCompanionIds);
        // A draft on the same node means the partner takes the ally slot, so say so instead
        // of letting the player believe the party will fight alongside them.
        var remote = _networkManager.RemoteCampaignPresence;
        bool coOpDraft = remote.Phase == CampaignOnlinePhase.Loadout &&
            string.Equals(remote.NodeId, GameSetup.SelectedNodeId, StringComparison.OrdinalIgnoreCase);
        screen.StatusText = coOpDraft ? "CO-OP · ALLY JOINS" : "";
        if (screen.ConsumeBackRequest())
        {
            _stateMachine.SwitchToState(State.OnlineCampaignMap);
            return;
        }
        if (screen.TryConsumeToggle(out string companionId))
        {
            GameSetup.ToggleCampaignCompanion(companionId);
            screen.SetSelection(GameSetup.SelectedCompanionIds);
        }
        if (screen.ConsumeContinueRequest())
        {
            Publish();
            _stateMachine.SwitchToState(State.OnlineCampaignSkills);
        }
    }

    private void Publish() =>
        _networkManager.SendCampaignPresence(new CampaignPresence(_networkManager.GetLocalPlayerId(),
            CampaignOnlinePhase.Loadout, GameSetup.SelectedNodeId, false));
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
        // Skill toggling also needs a skill that fits this node's budget; buddy-or-alone
        // remains available on zero-SP lessons.
        wheel.SelectionEnabled = !_ready &&
            CampaignLoadout.HasSelectableSkills(CampaignRuntime.OnlineProgress, GameSetup.SelectedNode);
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
            _stateMachine.SwitchToState(CampaignLoadout.HasCompanionChoices(GameSetup.SelectedNode,
                CampaignRuntime.OnlineProgress)
                ? State.OnlineCompanionSelect
                : State.OnlineCampaignMap);
            return;
        }
        UpdatePartyStrip(matchingDraft);
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

    /// <summary>
    /// The party summary mirrors the companion screen. A co-op draft says so explicitly,
    /// because the partner replaces the local party in a shared battle.
    /// </summary>
    private void UpdatePartyStrip(bool matchingDraft)
    {
        var strip = _uiManager.GetControlByName<UICampaignPartyStrip>("party_strip");
        if (strip == null) return;
        strip.SetParty(GameSetup.SelectedCompanionIds);
        strip.StatusOverride = matchingDraft ? "CO-OP · ALLY JOINS" : "";
        if (_ready || matchingDraft) return;
        if (strip.TryConsumeChangeRequest())
            _stateMachine.SwitchToState(State.OnlineCompanionSelect);
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
        System.Collections.Generic.IReadOnlyCollection<SkillType>? skills = null, string sessionId = "") =>
        _networkManager.SendCampaignPresence(new CampaignPresence(_networkManager.GetLocalPlayerId(), phase,
            nodeId, ready, skills, sessionId));
}
