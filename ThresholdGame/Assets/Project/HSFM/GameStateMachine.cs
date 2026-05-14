using System.Collections;
using UnityEngine;
using ThresholdGame.Architecture.Events;

namespace ThresholdGame.Core.GameFlow
{
    /// <summary>
    /// Máquina de estados global del juego.
    /// Persiste entre escenas (DontDestroyOnLoad).
    /// </summary>
    public sealed class GameStateMachine : MonoBehaviour
    {
        public static GameStateMachine Instance { get; private set; }

        [Header("Eventos de estado")]
        [SerializeField] private GameEventSO onEnterMainMenu;
        [SerializeField] private GameEventSO onEnterLoading;
        [SerializeField] private GameEventSO onEnterPlaying;
        [SerializeField] private GameEventSO onEnterPaused;
        [SerializeField] private GameEventSO onResumePlaying;
        [SerializeField] private GameEventSO onEnterEnding;

        public GameBootState BootState { get; private set; }
        public GameMainMenuState MainMenuState { get; private set; }
        public GamePlayingState PlayingState { get; private set; }
        public GamePausedState PausedState { get; private set; }
        public GameEndingState EndingState { get; private set; }

        public float CurrentLoadProgress { get; private set; }

        private IGameState _currentState;
        private IGameState _stateBeforePause;
        private ISceneLoader _sceneLoader;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sceneLoader = new SceneLoader();

            BootState = new GameBootState(this);
            MainMenuState = new GameMainMenuState(this, _sceneLoader);
            PlayingState = new GamePlayingState(this, _sceneLoader);
            PausedState = new GamePausedState(this);
            EndingState = new GameEndingState(this);
        }

        private void Start() => TransitionTo(BootState);

        private void Update() => _currentState?.Tick();

        // ── API pública ──────────────────────────────────────────────────────

        public void GoToMainMenu() => TransitionTo(MainMenuState);
        public void StartGame() => TransitionTo(PlayingState);
        public void EndGame() => TransitionTo(EndingState);

        public void Pause()
        {
            if (_currentState == PausedState) return;
            _stateBeforePause = _currentState;
            TransitionTo(PausedState);
        }

        /// <summary>
        /// Sale del estado de pausa SIN reentrar al estado anterior
        /// (su Enter() ya se ejecutó antes; solo restaura tiempo, cursor y notifica).
        /// </summary>
        public void Resume()
        {
            if (_currentState != PausedState || _stateBeforePause == null) return;

            // Solo ejecutamos el Exit() de Paused para restaurar timeScale.
            _currentState.Exit();
            _currentState = _stateBeforePause;
            _stateBeforePause = null;

            Debug.Log($"[GameFSM] ⏵ Resume → {_currentState.GetType().Name}");

            onResumePlaying?.Raise();
        }

        public bool IsPlaying => _currentState == PlayingState;
        public bool IsPaused => _currentState == PausedState;

        // ── Helpers para los estados ─────────────────────────────────────────

        public Coroutine RunCoroutine(IEnumerator routine) => StartCoroutine(routine);

        internal void ReportLoadProgress(float progress) => CurrentLoadProgress = progress;

        // ── Eventos internos ─────────────────────────────────────────────────

        internal void RaiseOnEnterMainMenu() => onEnterMainMenu?.Raise();
        internal void RaiseOnEnterLoading() => onEnterLoading?.Raise();
        internal void RaiseOnEnterPlaying() => onEnterPlaying?.Raise();
        internal void RaiseOnEnterPaused() => onEnterPaused?.Raise();
        internal void RaiseOnEnterEnding() => onEnterEnding?.Raise();

        // ── Transición ───────────────────────────────────────────────────────

        private void TransitionTo(IGameState newState)
        {
            if (newState == null || _currentState == newState) return;

            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();

            Debug.Log($"[GameFSM] → {newState.GetType().Name}");
        }
    }
}