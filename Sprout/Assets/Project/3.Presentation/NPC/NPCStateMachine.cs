using UnityEngine;
using UnityEngine.AI;
using ThresholdGame.Core.NPC;
using ThresholdGame.Infraestructure.NPC;
using ThresholdGame.Application.NPC;

namespace ThresholdGame.Presentation.NPC
{
    /// <summary>
    /// Máquina de estados de comportamiento del NPC.
    /// Gestiona el NavMeshAgent y las transiciones entre estados.
    /// Expone una API pública que DialogueRunner usa para ejecutar órdenes.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCStateMachine : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private SceneDestinationRegistry destinationRegistry;

        [Header("Configuración")]
        [SerializeField] private float arrivalThreshold = 0.5f;

        [Header("Paseo autónomo")]
        [Tooltip("Si está activo, el NPC pasea solo a puntos aleatorios cerca de su casa cuando no habla ni recibe órdenes.")]
        [SerializeField] private bool wander = true;
        [Tooltip("Radio (m) alrededor de su posición inicial dentro del que pasea.")]
        [SerializeField] private float wanderRadius = 6f;
        [Tooltip("Tiempo parado entre paseo y paseo (mín/máx segundos).")]
        [SerializeField] private float wanderIdleMin = 2f;
        [SerializeField] private float wanderIdleMax = 5f;

        private NavMeshAgent _agent;
        private INPCState _currentState;
        private Vector3 _homePosition;
        private bool _suspended;               // pausado por diálogo

        // ── Estados (instanciados una vez) ─────────────────────────────────────
        private readonly NPCIdleState _idle = new();
        private readonly NPCFollowState _follow = new();
        private readonly NPCStopState _stop = new();
        private NPCMoveToState _moveTo;
        private NPCReturnHomeState _returnHome;
        private NPCWanderState _wander;

        // ── Unity ──────────────────────────────────────────────────────────────

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            _homePosition = transform.position;

            // Los estados que necesitan callback se crean aquí para tener acceso a TransitionTo
            _moveTo = new NPCMoveToState(arrivalThreshold, () => TransitionTo(DefaultState()));
            _returnHome = new NPCReturnHomeState(arrivalThreshold, () => TransitionTo(DefaultState()));
            // El paseo ya no avisa a ningún animador procedural: la animación de andar/idle la lleva
            // el Animator real (NpcAnimatorDriver la deduce de la velocidad del NavMeshAgent).
            _wander = new NPCWanderState(wanderRadius, wanderIdleMin, wanderIdleMax, arrivalThreshold, null);
            _wander.SetHome(_homePosition);

            if (playerTransform == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            TransitionTo(DefaultState());
        }

        private void Update()
        {
            _currentState?.Tick(_agent, transform, playerTransform);
        }

        // Estado por defecto: paseo si está activado, si no idle quieto.
        private INPCState DefaultState() => wander ? (INPCState)_wander : _idle;
        private bool IsDefault() => _currentState == _wander || _currentState == _idle;

        // ── API pública para DialogueRunner ────────────────────────────────────

        /// <summary>
        /// Ejecuta la orden recibida del intérprete.
        /// Devuelve false si el destino no existe en el registro.
        /// </summary>
        public bool ExecuteOrder(NPCOrder order)
        {
            switch (order.Type)
            {
                case NPCOrderType.Follow:
                    TransitionTo(_follow);
                    return true;

                case NPCOrderType.Stop:
                    TransitionTo(_stop);
                    return true;

                case NPCOrderType.ReturnHome:
                    _returnHome.SetDestination(_homePosition);
                    TransitionTo(_returnHome);
                    return true;

                case NPCOrderType.MoveToDestination:
                    if (destinationRegistry == null)
                    {
                        Debug.LogWarning("[NPCStateMachine] No hay SceneDestinationRegistry.", this);
                        return false;
                    }
                    var dest = destinationRegistry.GetDestination(order.DestinationId);
                    if (dest == null) return false;
                    _moveTo.SetDestination(dest.position);
                    TransitionTo(_moveTo);
                    return true;

                default:
                    return false;
            }
        }

        // ── Pausa por diálogo (la llama NPCInteractionTrigger) ─────────────────

        /// <summary>Para el paseo mientras hablas con el NPC (para que no se aleje).</summary>
        public void SuspendAutonomy()
        {
            _suspended = true;
            if (IsDefault()) TransitionTo(_stop);
        }

        /// <summary>Reanuda el paseo al cerrar el diálogo (salvo que una orden lo tenga en otro estado).</summary>
        public void ResumeAutonomy()
        {
            _suspended = false;
            if (_currentState == _stop) TransitionTo(DefaultState());
        }

        // ── Transición ─────────────────────────────────────────────────────────

        private void TransitionTo(INPCState newState)
        {
            if (_currentState == newState) return;
            _currentState?.Exit(_agent);
            _currentState = newState;
            _currentState.Enter(_agent, transform, playerTransform);
            Debug.Log($"[NPC:{name}] → {newState.GetType().Name}");
        }
    }

    // ── Estados como clases internas privadas ──────────────────────────────────
    // Al ser simples y estar acoplados a NavMeshAgent no justifican archivos separados.
    // Si un estado crece o se reutiliza en otro contexto, se extrae a su propio archivo.

    internal sealed class NPCIdleState : INPCState
    {
        public void Enter(NavMeshAgent a, Transform t, Transform p) { if (a == null || !a.isOnNavMesh) return; a.isStopped = true; a.ResetPath(); }
        public void Tick(NavMeshAgent a, Transform t, Transform p) { }
        public void Exit(NavMeshAgent a) { if (a == null || !a.isOnNavMesh) return; a.isStopped = false; }
    }

    internal sealed class NPCFollowState : INPCState
    {
        private const float StopDistance = 1.5f;

        public void Enter(NavMeshAgent a, Transform t, Transform p) { if (a == null || !a.isOnNavMesh) return; a.isStopped = false; }

        public void Tick(NavMeshAgent a, Transform t, Transform p)
        {
            if (p == null || a == null || !a.isOnNavMesh) return;
            float dist = Vector3.Distance(t.position, p.position);
            if (dist > StopDistance)
                a.SetDestination(p.position);
            else
                a.ResetPath();
        }

        public void Exit(NavMeshAgent a) { if (a == null || !a.isOnNavMesh) return; a.ResetPath(); }
    }

    internal sealed class NPCStopState : INPCState
    {
        public void Enter(NavMeshAgent a, Transform t, Transform p) { if (a == null || !a.isOnNavMesh) return; a.isStopped = true; a.ResetPath(); }
        public void Tick(NavMeshAgent a, Transform t, Transform p) { }
        public void Exit(NavMeshAgent a) { if (a == null || !a.isOnNavMesh) return; a.isStopped = false; }
    }

    internal sealed class NPCMoveToState : INPCState
    {
        private readonly float _threshold;
        private readonly System.Action _onArrived;
        private Vector3 _destination;

        public NPCMoveToState(float threshold, System.Action onArrived)
        {
            _threshold = threshold;
            _onArrived = onArrived;
        }

        public void SetDestination(Vector3 dest) => _destination = dest;

        public void Enter(NavMeshAgent a, Transform t, Transform p)
        {
            if (a == null || !a.isOnNavMesh) return;
            a.isStopped = false;
            a.SetDestination(_destination);
        }

        public void Tick(NavMeshAgent a, Transform t, Transform p)
        {
            if (a == null || !a.isOnNavMesh) return;
            if (a.pathPending) return;
            if (a.remainingDistance <= _threshold)
            {
                a.ResetPath();
                _onArrived?.Invoke();
            }
        }

        public void Exit(NavMeshAgent a) { a.ResetPath(); }
    }

    internal sealed class NPCReturnHomeState : INPCState
    {
        private readonly float _threshold;
        private readonly System.Action _onArrived;
        private Vector3 _home;

        public NPCReturnHomeState(float threshold, System.Action onArrived)
        {
            _threshold = threshold;
            _onArrived = onArrived;
        }

        public void SetDestination(Vector3 home) => _home = home;

        public void Enter(NavMeshAgent a, Transform t, Transform p)
        {
            a.isStopped = false;
            a.SetDestination(_home);
        }

        public void Tick(NavMeshAgent a, Transform t, Transform p)
        {
            if (a.pathPending) return;
            if (a.remainingDistance <= _threshold)
            {
                a.ResetPath();
                _onArrived?.Invoke();
            }
        }

        public void Exit(NavMeshAgent a) { a.ResetPath(); }
    }

    /// <summary>
    /// Paseo autónomo: elige puntos aleatorios cerca de "casa", camina hasta ellos, se para un
    /// rato y repite. Avisa al animador (setMoving) para alternar andar/idle.
    /// </summary>
    internal sealed class NPCWanderState : INPCState
    {
        private readonly float _radius, _idleMin, _idleMax, _threshold;
        private readonly System.Action<bool> _setMoving;
        private Vector3 _home;
        private float _idleTimer;
        private bool _moving;

        public NPCWanderState(float radius, float idleMin, float idleMax, float threshold, System.Action<bool> setMoving)
        {
            _radius = radius; _idleMin = idleMin; _idleMax = idleMax; _threshold = threshold; _setMoving = setMoving;
        }

        public void SetHome(Vector3 home) => _home = home;

        public void Enter(NavMeshAgent a, Transform t, Transform p)
        {
            if (a == null || !a.isOnNavMesh) return;
            a.isStopped = false;
            _moving = false;
            _setMoving?.Invoke(false);
            _idleTimer = Random.Range(_idleMin, _idleMax); // empieza esperando un poco
        }

        public void Tick(NavMeshAgent a, Transform t, Transform p)
        {
            if (a == null || !a.isOnNavMesh) return;

            if (_moving)
            {
                // ¿Ha llegado? -> a descansar.
                if (!a.pathPending && a.remainingDistance <= _threshold)
                {
                    a.ResetPath();
                    _moving = false;
                    _setMoving?.Invoke(false);
                    _idleTimer = Random.Range(_idleMin, _idleMax);
                }
                return;
            }

            // Descansando: cuando se acaba el temporizador, elige nuevo destino.
            _idleTimer -= Time.deltaTime;
            if (_idleTimer > 0f) return;

            Vector2 r = Random.insideUnitCircle * _radius;
            Vector3 target = _home + new Vector3(r.x, 0f, r.y);
            if (NavMesh.SamplePosition(target, out var hit, 2f, NavMesh.AllAreas))
            {
                a.SetDestination(hit.position);
                _moving = true;
                _setMoving?.Invoke(true);
            }
            else
            {
                _idleTimer = 0.5f; // punto inválido, reintenta pronto
            }
        }

        public void Exit(NavMeshAgent a)
        {
            _setMoving?.Invoke(false);
            if (a == null || !a.isOnNavMesh) return;
            a.ResetPath();
        }
    }
}