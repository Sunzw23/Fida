#nullable enable
using System;
using Fida.Logic;

namespace Fida.Scene
{
    /// <summary>
    /// Converts synchronous AI decisions into short, frame-based presentation beats.
    /// It owns timing only; controllers still own decision-making and GameManager
    /// remains the sole authority that commits actions.
    /// </summary>
    public sealed class AutomatedActionPresenter
    {
        private PendingAction? _pending;

        public bool HasPendingAction => _pending != null;
        public int PendingPlayerId => _pending?.Controller.PlayerId ?? -1;

        public event Action<int, PlayerActionRequiredEventArgs>? PresentationStarted;
        public event Action? PresentationEnded;

        public void Schedule(IAutomatedPlayerController controller,
            PlayerActionRequiredEventArgs args, float delaySeconds)
        {
            if (controller == null || args == null) return;
            _pending = new PendingAction(controller, args, Math.Max(0f, delaySeconds));
            PresentationStarted?.Invoke(controller.PlayerId, args);
        }

        public void Update(GameManager game, float elapsedSeconds)
        {
            if (_pending == null || game == null) return;

            _pending.RemainingSeconds -= Math.Max(0f, elapsedSeconds);
            if (_pending.RemainingSeconds > 0f) return;

            // Clear first. RequestAction may synchronously broadcast the next step,
            // which is then scheduled as a fresh presentation without being erased.
            var ready = _pending;
            _pending = null;
            PresentationEnded?.Invoke();
            ready.Controller.RequestAction(game, ready.Args);
        }

        public void Reset()
        {
            bool wasPresenting = _pending != null;
            _pending = null;
            if (wasPresenting) PresentationEnded?.Invoke();
        }

        public static float GetDelay(GameManager game, PlayerActionRequiredEventArgs args)
        {
            if (args?.RequiredActionType == ActionType.ChooseNumberIndex)
            {
                // The second beat is deliberately longer so the first locked number
                // remains visible before the AI combines it with another number.
                return game?.Query.GetFirstChosenNumberIndex() >= 0 ? 1.15f : 0.7f;
            }

            return args?.RequiredActionType switch
            {
                ActionType.ChooseTarget => 0.9f,
                ActionType.ChooseNumber => 0.85f,
                ActionType.DecideApply => 0.65f,
                _ => 0.6f
            };
        }

        private sealed class PendingAction
        {
            public IAutomatedPlayerController Controller { get; }
            public PlayerActionRequiredEventArgs Args { get; }
            public float RemainingSeconds { get; set; }

            public PendingAction(IAutomatedPlayerController controller,
                PlayerActionRequiredEventArgs args, float remainingSeconds)
            {
                Controller = controller;
                Args = args;
                RemainingSeconds = remainingSeconds;
            }
        }
    }
}
