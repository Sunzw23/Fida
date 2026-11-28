using Fida.Campaign;
using Fida.Logic;
using Fida.UI;
using Fida.UI.Rules;
using Fida.Network;
using Microsoft.Xna.Framework.Input;

namespace Fida.States
{
    /// <summary>Page navigation, skill discovery and the keyboard page turn for the rules book.</summary>
    public sealed class RulesState : StateBase
    {
        private KeyboardState _previousKeyboard;
        private int _indexPage;

        public RulesState(UIManager uiManager, NetworkManager networkManager,
            GameStateMachine stateMachine) : base(uiManager, networkManager, stateMachine) { }

        public override void OnEnter()
        {
            _indexPage = _stateMachine.RulesIndexPage;
            _previousKeyboard = Keyboard.GetState();
            Achievements.EnsureLoaded();
            CombinationStore.EnsureLoaded();
            BindControls();
        }

        public override void OnExit()
        {
            MarkCurrentPageRead();
            base.OnExit();
        }

        public override void HandleInput(Microsoft.Xna.Framework.GameTime gameTime)
        {
            base.HandleInput(gameTime);
            // A control may have queued a page rebuild while the UI was being updated; doing it
            // there would mutate the control list mid-enumeration.
            RunPendingRebuild();

            var keyboard = Keyboard.GetState();
            bool left = keyboard.IsKeyDown(Keys.Left) && !_previousKeyboard.IsKeyDown(Keys.Left);
            bool right = keyboard.IsKeyDown(Keys.Right) && !_previousKeyboard.IsKeyDown(Keys.Right);
            bool up = keyboard.IsKeyDown(Keys.Up) && !_previousKeyboard.IsKeyDown(Keys.Up);
            bool down = keyboard.IsKeyDown(Keys.Down) && !_previousKeyboard.IsKeyDown(Keys.Down);
            _previousKeyboard = keyboard;
            if (_uiManager.IsPageTransitionActive) return;

            if (left) ChangePage(-1);
            else if (right) ChangePage(1);
            else if (_stateMachine.RulesPage == RulesBook.IndexPage && (up || down))
                ChangeIndexPage(up ? -1 : 1);
        }

        private void BindControls()
        {
            BindButton("back", _stateMachine.CloseRules);
            BindButton("prevPage", () => ChangePage(-1));
            BindButton("nextPage", () => ChangePage(1));
            BindButton("indexPrev", () => ChangeIndexPage(-1));
            BindButton("indexNext", () => ChangeIndexPage(1));

            for (int i = 0; i < RulesBook.Chapters.Length; i++)
            {
                int page = i;
                BindButton($"rulesChapter{i}", () => SelectPage(page));
            }

            var book = RulesBook.Current();
            for (int i = 0; i < book.Entries.Count; i++)
            {
                int page = RulesBook.PageForIndex(i);
                BindButton($"rulesTile{i}", () => SelectPage(page));
                BindButton($"rulesLink{i}", () => SelectPage(page));
            }

            BindNumbersAssembly();
        }

        /// <summary>
        /// The Numbers page is a drawing surface, not buttons: it raises taps, and the rebuild is
        /// deferred to the end of the input pass.
        /// </summary>
        private void BindNumbersAssembly()
        {
            var assembly = _uiManager.GetControlByName<UIRulesNumbersAssembly>("rules_numbers");
            if (assembly == null) return;
            assembly.RowTapped = row =>
            {
                int digit = UIRulesNumbersAssembly.DigitOf(row);
                if (digit < 0) return;
                _pendingRebuild = () =>
                {
                    CombinationLoadout.CycleRow(digit);
                    CombinationStore.Save();
                    Reload();
                };
            };
            assembly.DigitTapped = which =>
            {
                _pendingRebuild = () =>
                {
                    UnbindAll();
                    _stateMachine.CycleMarginDigit(which);
                    BindControls();
                };
            };
        }

        private System.Action? _pendingRebuild;

        private void RunPendingRebuild()
        {
            if (_pendingRebuild == null) return;
            var rebuild = _pendingRebuild;
            _pendingRebuild = null;
            rebuild();
        }

        private void Reload()
        {
            UnbindAll();
            _stateMachine.SetRulesPage(_stateMachine.RulesPage);
            BindControls();
        }

        private void ChangePage(int offset) => SelectPage(_stateMachine.RulesPage + offset);

        private void SelectPage(int page)
        {
            MarkCurrentPageRead();
            UnbindAll();
            _stateMachine.SetRulesPage(page, _indexPage);
            BindControls();
        }

        private void ChangeIndexPage(int offset)
        {
            var index = _uiManager.GetControlByName<UIRulesIndexPage>("rules_index");
            if (index == null) return;
            int count = System.Math.Max(1, index.PageCount);
            _indexPage = ((index.IndexPage + offset) % count + count) % count;
            UnbindAll();
            _stateMachine.SetRulesPage(RulesBook.IndexPage, _indexPage);
            BindControls();
        }

        /// <summary>A skill is no longer new once its page has been left.</summary>
        private void MarkCurrentPageRead()
        {
            var book = RulesBook.Current();
            if (book.SkillAtPage(_stateMachine.RulesPage) is not { } skill) return;
            SkillCodex.MarkRead(skill);
        }
    }
}
