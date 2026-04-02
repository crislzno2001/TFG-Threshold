namespace ThresholdGame.Core.Commands
{
    /// <summary>
    /// Contrato base del Command Pattern.
    /// Execute aplica el efecto; Undo lo revierte.
    /// Toda acción del juego que necesite historial, replay o undo debe implementar esta interfaz.
    /// </summary>
    public interface ICommand
    {
        /// <summary>Aplica el comando.</summary>
        void Execute();

        /// <summary>Revierte el efecto del comando.</summary>
        void Undo();
    }
}
