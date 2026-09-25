using System.Collections.Generic;
using System.Linq;
using Fida.Campaign;
using Fida.Logic;
using Fida.Network;
using Fida.UI;
using Fida.UI.Campaign;
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
        private UICampaignSkillWheel _wheel;
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
            _picks.Clear();
            _localBan = null;
            _remoteBan = null;
            _remoteLoadout = null;
            _localLocked = false;
            _started = false;
            _networkManager.DraftBanReceived += OnBanReceived;
            _networkManager.DraftLockReceived += OnLockReceived;
            _networkManager.DraftStarting += OnDraftStarting;
            _wheel = _uiManager.GetControls().OfType<UICampaignSkillWheel>()
                .FirstOrDefault(control => control.Name == "duel_draft_wheel");
            if (_wheel != null)
            {
                _wheel.SetSelection(_picks);
                _wheel.AvailabilityOverride = Availability;
                _wheel.AvailabilityLabelOverride = AvailabilityLabel;
                _wheel.AvailableActionText = "BAN";
                _wheel.SelectedActionText = "BANNED";
                _wheel.BackActionText = "EXIT";
                _wheel.StartActionText = "LOCK";
            }
            RefreshView();
        }

        public override void OnExit()
        {
            _networkManager.DraftBanReceived -= OnBanReceived;
            _networkManager.DraftLockReceived -= OnLockReceived;
            _networkManager.DraftStarting -= OnDraftStarting;
            _wheel = null;
            base.OnExit();
        }

        public override void HandleInput(GameTime gameTime)
        {
            base.HandleInput(gameTime);
            if (_wheel != null)
            {
                if (_wheel.TryConsumeToggle(out SkillType skill)) ChooseSkill(skill);
                if (_wheel.ConsumeBackRequest()) Disconnect();
                if (_wheel.ConsumeStartRequest()) LockLoadout();
            }
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
            int spent = CampaignSkillBalance.TotalCost(_picks);
            if (_wheel != null)
            {
                _wheel.ShowSkillPoints = Picking;
                _wheel.HeaderText = Picking ? "PICK" : "BAN";
                _wheel.AvailableActionText = "BAN";
                _wheel.SelectedActionText = "BANNED";
                _wheel.StartActionEnabled = CanLock();
                _wheel.StartActionText = !Picking ? "PICK" : _localLocked ? "WAIT" : "LOCK";
                if (!Picking)
                    _wheel.StatusText = _localBan.HasValue
                        ? "BAN SENT · WAITING FOR OPPONENT"
                        : "CHOOSE ONE SKILL";
                else
                {
                    string bans = _localBan == _remoteBan
                        ? GameText.SkillName(_localBan.Value)
                        : $"{GameText.SkillName(_localBan.Value)} / {GameText.SkillName(_remoteBan.Value)}";
                    _wheel.StatusText = _localLocked
                        ? $"LOCKED · BANNED {bans}"
                        : $"{spent} / {OnlineModeRules.DuelSkillBudget} SP · BANNED {bans}";
                    _wheel.AvailableActionText = "ADD";
                    _wheel.SelectedActionText = "REMOVE";
                }
            }
        }

        private CampaignSkillAvailability Availability(SkillType skill)
        {
            if (!Picking)
            {
                if (_localBan == skill) return CampaignSkillAvailability.Selected;
                return _localBan.HasValue
                    ? CampaignSkillAvailability.Locked
                    : CampaignSkillAvailability.Available;
            }

            if (IsBanned(skill)) return CampaignSkillAvailability.Locked;
            if (_picks.Contains(skill)) return CampaignSkillAvailability.Selected;
            if (_localLocked) return CampaignSkillAvailability.Locked;
            if (CampaignSkillBalance.Conflicts(_picks, skill)) return CampaignSkillAvailability.Conflicting;
            return CampaignSkillBalance.TotalCost(_picks) + CampaignSkillBalance.Cost(skill) >
                   OnlineModeRules.DuelSkillBudget
                ? CampaignSkillAvailability.OverBudget
                : CampaignSkillAvailability.Available;
        }

        private string AvailabilityLabel(SkillType skill) =>
            !Picking && _localBan == skill ? "BANNED" : Availability(skill) switch
            {
                CampaignSkillAvailability.Available => "AVAILABLE",
                CampaignSkillAvailability.Selected => "PICKED",
                CampaignSkillAvailability.Locked when IsBanned(skill) => "BANNED",
                CampaignSkillAvailability.Locked when _localLocked => "LOCKED",
                CampaignSkillAvailability.Locked => "WAIT",
                CampaignSkillAvailability.OverBudget => "NO SP",
                CampaignSkillAvailability.Conflicting => "CONFLICT",
                _ => "UNAVAILABLE"
            };

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
