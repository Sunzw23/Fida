using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Fida.Network;

namespace Fida
{
    /// <summary>
    /// 退出逻辑处理 - 负责 ESC 键检测和窗口焦点管理
    /// </summary>
    public class ExitHandler
    {
        private readonly GameStateMachine _stateMachine;
        private readonly NetworkManager _networkManager;
        private readonly Game _game;

        private bool _previousEscPressed;

        public ExitHandler(GameStateMachine stateMachine, NetworkManager networkManager, Game game)
        {
            _stateMachine = stateMachine;
            _networkManager = networkManager;
            _game = game;
        }

        /// <summary>
        /// 处理 ESC 退出输入
        /// </summary>
        public void HandleExitInput(bool dialogueActive = false)
        {
            bool escPressed = Keyboard.GetState().IsKeyDown(Keys.Escape);
            if (dialogueActive)
            {
                _previousEscPressed = escPressed;
                return;
            }

            if (_stateMachine.CurrentState == State.Playing || _stateMachine.CurrentState == State.OnlinePlaying)
            {
                if (escPressed && !_previousEscPressed)
                    _stateMachine.SwitchToState(State.Paused);
            }
            else if (_stateMachine.CurrentState == State.Paused)
            {
                if (escPressed && !_previousEscPressed)
                    _stateMachine.SwitchToState(_networkManager.IsConnected ? State.OnlinePlaying : State.Playing);
            }
            else if ((escPressed && !_previousEscPressed) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                _game.Exit();
            }

            _previousEscPressed = escPressed;
        }
    }
}
