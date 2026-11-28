using Fida.Campaign;
using Fida.Network;
using Fida.UI;
using Microsoft.Xna.Framework.Input;

namespace Fida.States
{
    /// <summary>The achievement sheet: page turns, and the ledger flush on the way out.</summary>
    public sealed class AchievementsState : StateBase
    {
        private KeyboardState _previousKeyboard;

        public AchievementsState(UIManager uiManager, NetworkManager networkManager,
            GameStateMachine stateMachine) : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            Achievements.EnsureLoaded();
            _previousKeyboard = Keyboard.GetState();
            BindControls();
        }

        public override void OnExit()
        {
            Achievements.Save();
            base.OnExit();
        }

        public override void HandleInput(Microsoft.Xna.Framework.GameTime gameTime)
        {
            base.HandleInput(gameTime);

            var keyboard = Keyboard.GetState();
            bool left = keyboard.IsKeyDown(Keys.Left) && !_previousKeyboard.IsKeyDown(Keys.Left);
            bool right = keyboard.IsKeyDown(Keys.Right) && !_previousKeyboard.IsKeyDown(Keys.Right);
            _previousKeyboard = keyboard;
            if (_uiManager.IsPageTransitionActive) return;
            if (left) ChangePage(-1);
            else if (right) ChangePage(1);
        }

        private void BindControls()
        {
            BindButton("back", _stateMachine.CloseAchievements);
            BindButton("prevPage", () => ChangePage(-1));
            BindButton("nextPage", () => ChangePage(1));
        }

        private void ChangePage(int offset)
        {
            UnbindAll();
            _stateMachine.SetAchievementsPage(_stateMachine.AchievementsPage + offset);
            BindControls();
        }
    }
}
