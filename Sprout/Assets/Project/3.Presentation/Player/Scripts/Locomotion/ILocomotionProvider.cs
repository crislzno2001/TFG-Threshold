namespace ThresholdGame.Presentation.Player.Locomotion
{
    /// <summary>
    /// Contrato que expone el estado de locomoción del jugador
    /// a otros sistemas (animación, audio, UI) sin acoplarlos
    /// a la implementación concreta del controller.
    /// </summary>
    public interface ILocomotionProvider
    {
        /// <summary>Velocidad horizontal actual del personaje.</summary>
        float CurrentSpeed { get; }

        /// <summary>Valor suavizado para blend trees de animación.</summary>
        float AnimationBlend { get; }

        /// <summary>Magnitud normalizada del input (0-1).</summary>
        float InputMagnitude { get; }

        /// <summary>Velocidad vertical (saltos / caídas).</summary>
        float VerticalVelocity { get; }

        /// <summary>True si el personaje toca el suelo.</summary>
        bool Grounded { get; }

        /// <summary>Habilita o deshabilita el control del jugador.</summary>
        void SetControlEnabled(bool enabled);

        /// <summary>Detiene cualquier movimiento residual de forma segura.</summary>
        void ForceStop();
    }
}