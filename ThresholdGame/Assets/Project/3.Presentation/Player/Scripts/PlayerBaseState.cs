namespace ThresholdGame.Presentation.Player
{
    public abstract class PlayerBaseState
    {
        protected PlayerStateMachine StateMachine { get; }

        protected PlayerBaseState(PlayerStateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }

        public abstract void Enter();
        public abstract void Update();
        public abstract void Exit();
    }
}
