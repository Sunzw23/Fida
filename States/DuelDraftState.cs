using System.Collections.Generic;
using System.Linq;
using Fida.Campaign;
using Fida.Logic;
using Fida.Network;
using Fida.UI;
using Microsoft.Xna.Framework;

namespace Fida.States
{
    /// <summary>
    /// Networked simultaneous-ban / independent-pick state. The host validates and
    /// publishes the canonical pair of loadouts before either peer creates combat.
    /// </summary>
    public sealed class DuelDraftState : StateBase
    {
        private readonly HashSet<SkillType> _picks = [];
        private SkillType? _localBan;
        private SkillType? _remoteBan;
        private IReadOnlyList<SkillType> _remoteLoadout;
        private bool _localLocked;
        private bool _started;
        private int LocalId => _networkManager.GetLocalPlayerId();
        private bool Picking => _localBan.HasValue && _remoteBan.HasValue;

        public DuelDraftState(UIManager uiManager, NetworkManager networkManager,
            GameStateMachine stateMachine) : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            _networkManager.DraftBanReceived += OnBanReceived;
            _networkManager.DraftLockReceived += OnLockReceived;
            _networkManager.DraftStarting += OnDraftStarting;

            foreach (SkillType skill in SkillFactory.SelectableSkills)
            {
                SkillType captured = skill;
                BindButton($"draftSkill_{skill}", () => ChooseSkill(captured));
                var button = _uiManager.GetButtonByName($"draftSkill_{skill}");
                if (button != null) button.EnabledChecker = () => CanChoose(captured);
            }
            BindButton("draftCommit", LockLoadout);
            var commit = _uiManager.GetButtonByName("draftCommit");
            if (commit != null) commit.EnabledChecker = CanLock;
            BindButton("draftBack", Disconnect);
            RefreshView();
        }

        public override void OnExit()
        {
            _networkManager.DraftBanReceived -= OnBanReceived;
            _networkManager.DraftLockReceived -= OnLockReceived;
            _networkManager.DraftStarting -= OnDraftStarting;
            base.OnExit();
        }

        public override void HandleInput(GameTime gameTime)
        {
            base.HandleInput(gameTime);
            _networkManager.ProcessDraftMessages();
        }

        private void ChooseSkill(SkillType skill)
        {
            if (!Picking)
            {
                if (_localBan.HasValue) return;
                _localBan = skill;
                _networkManager.SendDraftBan(LocalId, skill);
                RefreshView();
                return;
            }
            if (_localLocked || IsBanned(skill)) return;
            if (_picks.Remove(skill))
            {
                RefreshView();
                return;
            }
            if (CampaignSkillBalance.Conflicts(_picks, skill)) return;
            if (CampaignSkillBalance.TotalCost(_picks) + CampaignSkillBalance.Cost(skill) >
                OnlineModeRules.DuelSkillBudget) return;
            _picks.Add(skill);
            RefreshView();
        }

        private bool CanChoose(SkillType skill)
        {
            if (!Picking) return !_localBan.HasValue;
            if (_localLocked || IsBanned(skill)) return false;
            if (_picks.Contains(skill)) return true;
            return !CampaignSkillBalance.Conflicts(_picks, skill) &&
                CampaignSkillBalance.TotalCost(_picks) + CampaignSkillBalance.Cost(skill) <=
                OnlineModeRules.DuelSkillBudget;
        }

        private bool CanLock() => Picking && !_localLocked &&
            OnlineModeRules.IsLegalDuelLoadout(_picks, Bans());

        private void LockLoadout()
        {
            if (!CanLock()) return;
            _localLocked = true;
            _networkManager.SendDraftLock(LocalId, _picks);
            RefreshView();
            TryStartAsHost();
        }

        private void OnBanReceived(int playerId, SkillType skill)
        {
            if (playerId != LocalId) _remoteBan = skill;
            RefreshView();
        }

        private void OnLockReceived(int playerId, IReadOnlyList<SkillType> skills)
        {
            if (playerId == LocalId || !OnlineModeRules.IsLegalDuelLoadout(skills, Bans())) return;
            _remoteLoadout = skills.ToArray();
            RefreshView();
            TryStartAsHost();
        }

        private void TryStartAsHost()
        {
            if (!_networkManager.IsHost || !_localLocked || _remoteLoadout == null || _started) return;
            _started = true;
            var player0 = LocalId == 0 ? _picks.ToArray() : _remoteLoadout;
            var player1 = LocalId == 1 ? _picks.ToArray() : _remoteLoadout;
            OnlineModeRules.ConfigureDuelLoadouts(player0, player1);
            _networkManager.SendDraftStart(player0, player1);
            _stateMachine.SwitchToState(State.OnlinePlaying);
        }

        private void OnDraftStarting(IReadOnlyList<SkillType> player0,
            IReadOnlyList<SkillType> player1)
        {
            if (_started || !OnlineModeRules.IsLegalDuelLoadout(player0, Bans()) ||
                !OnlineModeRules.IsLegalDuelLoadout(player1, Bans())) return;
            _started = true;
            OnlineModeRules.ConfigureDuelLoadouts(player0, player1);
            _stateMachine.SwitchToState(State.OnlinePlaying);
        }

        private void RefreshView()
        {
            var phase = _uiManager.GetLabelByName("draftPhase");
            var status = _uiManager.GetLabelByName("draftStatus");
            int spent = CampaignSkillBalance.TotalCost(_picks);
            if (!Picking)
            {
                phase?.Update("BAN - choose one skill");
                status?.Update(_localBan.HasValue
                    ? "Ban submitted. Waiting for the other player..."
                    : "Both bans remain hidden until both players submit.");
            }
            else
            {
                phase?.Update($"PICK - {spent} / {OnlineModeRules.DuelSkillBudget} SP");
                string bans = _localBan == _remoteBan
                    ? GameText.SkillName(_localBan.Value)
                    : $"{GameText.SkillName(_localBan.Value)} / {GameText.SkillName(_remoteBan.Value)}";
                status?.Update(_localLocked ? $"Banned: {bans} | Locked - waiting..." :
                    $"Banned: {bans} | Spend exactly {OnlineModeRules.DuelSkillBudget} SP, then lock.");
            }
            foreach (SkillType skill in SkillFactory.SelectableSkills)
            {
                var button = _uiManager.GetButtonByName($"draftSkill_{skill}");
                button?.SetSelected((!Picking && _localBan == skill) || _picks.Contains(skill));
            }
        }

        private bool IsBanned(SkillType skill) => _localBan == skill || _remoteBan == skill;
        private IReadOnlyCollection<SkillType> Bans() =>
            new[] { _localBan, _remoteBan }.Where(s => s.HasValue).Select(s => s.Value).ToArray();

        private void Disconnect()
        {
            _networkManager.Cleanup();
            _stateMachine.SwitchToState(State.MainMenu);
        }
    }
}
