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

        private NavMeshAgent _agent;
        private INPCState _currentState;
        private Vector3 _homePosition;

        // ── Estados (instanciados una vez) ─────────────────────────────────────
        private readonly NPCIdleState _idle = new();
        private readonly NPCFollowState _follow = new();
        private readonly NPCStopState _stop = new();
        private NPCMoveToState _moveTo;
        private NPCReturnHomeState _returnHome;

        // ── Unity ──────────────────────────────────────────────────────────────

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            _homePosition = transform.position;

            // Los estados que necesitan callback se crean aquí para tener acceso a TransitionTo
            _moveTo = new NPCMoveToState(arrivalThreshold, () => TransitionTo(_idle));
            _returnHome = new NPCReturnHomeState(arrivalThreshold, () => TransitionTo(_idle));

            if (playerTransform == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            TransitionTo(_idle);
        }

        private void Update()
        {
            _currentState?.Tick(_agent, transform, playerTransform);
        }

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
        public void Enter(NavMeshAgent a, Transform t, Transform p) { a.isStopped = true; a.ResetPath(); }
        public void Tick(NavMeshAgent a, Transform t, Transform p) { }
        public void Exit(NavMeshAgent a) { a.isStopped = false; }
    }

    internal sealed class NPCFollowState : INPCState
    {
        private const float StopDistance = 1.5f;

        public void Enter(NavMeshAgent a, Transform t, Transform p) { a.isStopped = false; }

        public void Tick(NavMeshAgent a, Transform t, Transform p)
        {
            if (p == null) return;
            float dist = Vector3.Distance(t.position, p.position);
            if (dist > StopDistance)
                a.SetDestination(p.position);
            else
                a.ResetPath();
        }

        public void Exit(NavMeshAgent a) { a.ResetPath(); }
    }

    internal sealed class NPCStopState : INPCState
    {
        public void Enter(NavMeshAgent a, Transform t, Transform p) { a.isStopped = true; a.ResetPath(); }
        public void Tick(NavMeshAgent a, Transform t, Transform p) { }
        public void Exit(NavMeshAgent a) { a.isStopped = false; }
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
            a.isStopped = false;
            a.SetDestination(_destination);
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
}