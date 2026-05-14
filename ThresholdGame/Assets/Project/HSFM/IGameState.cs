namespace ThresholdGame.Core.GameFlow
{
    /// <summary>
    /// Contrato que implementa cada estado global del juego.
    /// Clases puras de C#, sin dependencia de MonoBehaviour.
    /// </summary>
    public interface IGameState
    {
        void Enter();
        void Tick();
        void Exit();
    }
}