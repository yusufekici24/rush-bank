using System.Collections;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public enum StaffInterruptionState
    {
        Idle,
        StaffArriving,
        TaskActive,
        Resolved
    }

    public class StaffInterruptionSystem : MonoBehaviour
    {
        private const string UrgentDocumentId = "urgent_office_document";

        [Header("References")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private PlayerInteraction playerInteraction;
        [SerializeField] private Transform player;
        [SerializeField] private Transform coworkerSpawnPoint;
        [SerializeField] private Transform counterInterruptionPoint;
        [SerializeField] private Transform coworkerExitPoint;
        [SerializeField] private Collider archiveDeskZone;
        [SerializeField] private GameObject coworkerPrefab;
        [SerializeField] private GameObject urgentDocumentPrefab;
        [SerializeField] private ParticleSystem completionEffect;

        [Header("Counter Pause")]
        [SerializeField] private Behaviour[] pausableCounterSystems;

        [Header("Timing")]
        [SerializeField] private Vector2 interruptionIntervalSeconds = new Vector2(45f, 60f);
        [SerializeField, Min(0.1f)] private float coworkerMoveSpeed = 2.6f;
        [SerializeField, Min(0f)] private float rewardTime = 5f;
        [SerializeField] private bool autoStartTimer = true;

        [Header("Patience Pressure")]
        [SerializeField] private float interruptionPatiencePressureMultiplier = 1f;

        public UnityEvent<StaffInterruptionState> OnStateChanged = new UnityEvent<StaffInterruptionState>();
        public UnityEvent OnCounterTransactionPaused = new UnityEvent();
        public UnityEvent OnCounterTransactionResumed = new UnityEvent();
        public UnityEvent OnInterruptionCompleted = new UnityEvent();

        private StaffInterruptionState state = StaffInterruptionState.Idle;
        private GameObject activeCoworker;
        private GameObject activeDocument;
        private QueueCustomer pausedCustomer;
        private CustomerPatience pausedCustomerPatience;
        private bool[] pausablePreviousStates = System.Array.Empty<bool>();
        private float nextInterruptionTimer;
        private Coroutine routine;
        private bool counterSystemsPaused;

        public StaffInterruptionState State => state;
        public bool IsActive => state != StaffInterruptionState.Idle;

        private void Awake()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance;
            }

            if (playerInteraction == null)
            {
                playerInteraction = FindFirstObjectByType<PlayerInteraction>();
            }

            if (player == null && playerInteraction != null)
            {
                player = playerInteraction.transform;
            }
        }

        private void OnEnable()
        {
            ResetInterruptionTimer();
        }

        private void OnDisable()
        {
            ResumePausedCounterTransaction();
        }

        private void Update()
        {
            if (state == StaffInterruptionState.Idle)
            {
                TickInterruptionTimer();
                return;
            }

            if (state == StaffInterruptionState.TaskActive && IsUrgentDocumentDelivered())
            {
                CompleteArchiveDelivery();
            }
        }

        public void ForceStartInterruption()
        {
            if (state != StaffInterruptionState.Idle)
            {
                return;
            }

            StartInterruption();
        }

        private void TickInterruptionTimer()
        {
            if (!autoStartTimer)
            {
                return;
            }

            nextInterruptionTimer -= Time.deltaTime;
            if (nextInterruptionTimer <= 0f)
            {
                StartInterruption();
            }
        }

        private void StartInterruption()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(RunInterruption());
        }

        private IEnumerator RunInterruption()
        {
            SetState(StaffInterruptionState.StaffArriving);
            activeCoworker = SpawnCoworker();

            var targetPosition = GetCounterPoint();
            yield return MoveObjectTo(activeCoworker.transform, targetPosition);

            PauseActiveCounterTransaction();
            activeDocument = SpawnUrgentDocument();
            SetState(StaffInterruptionState.TaskActive);
            routine = null;
        }

        private GameObject SpawnCoworker()
        {
            var spawnPosition = coworkerSpawnPoint != null ? coworkerSpawnPoint.position : transform.position;
            if (coworkerPrefab != null)
            {
                return Instantiate(coworkerPrefab, spawnPosition, Quaternion.identity);
            }

            var coworker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            coworker.name = "CoworkerNPC Prototype";
            coworker.transform.position = spawnPosition;
            coworker.transform.localScale = new Vector3(1.05f, 1.1f, 1.05f);

            var renderer = coworker.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.28f, 0.74f, 0.86f);
                renderer.sharedMaterial = material;
            }

            return coworker;
        }

        private GameObject SpawnUrgentDocument()
        {
            var spawnPosition = GetCounterPoint() + Vector3.up * 0.35f;
            var document = urgentDocumentPrefab != null
                ? Instantiate(urgentDocumentPrefab, spawnPosition, Quaternion.identity)
                : CreateFallbackUrgentDocument(spawnPosition);

            document.name = "Urgent Office Document";
            var item = document.GetComponent<DeliverableItem>();
            if (item == null)
            {
                item = document.AddComponent<DeliverableItem>();
            }

            item.Configure(UrgentDocumentId, new Color(0.98f, 0.48f, 0.24f));
            TrySetTag(document, "Interactable");
            return document;
        }

        private static GameObject CreateFallbackUrgentDocument(Vector3 position)
        {
            var document = GameObject.CreatePrimitive(PrimitiveType.Cube);
            document.transform.position = position;
            document.transform.localScale = new Vector3(0.72f, 0.05f, 0.48f);

            var renderer = document.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.98f, 0.48f, 0.24f);
                renderer.sharedMaterial = material;
            }

            var body = document.AddComponent<Rigidbody>();
            body.mass = 0.2f;
            body.angularDamping = 4f;
            return document;
        }

        private void PauseActiveCounterTransaction()
        {
            pausedCustomer = queueManager != null ? queueManager.ActiveCustomer : null;
            pausedCustomerPatience = pausedCustomer != null
                ? pausedCustomer.GetComponent<CustomerPatience>()
                : null;

            if (pausedCustomerPatience != null && pausedCustomer != null)
            {
                pausedCustomerPatience.SetDrainMultiplier(interruptionPatiencePressureMultiplier);
            }

            if (pausedCustomer != null)
            {
                pausedCustomer.SetPatienceDrainMultiplier(interruptionPatiencePressureMultiplier);
            }

            SetCounterSystemsPaused(true);
            OnCounterTransactionPaused.Invoke();
        }

        private void ResumePausedCounterTransaction()
        {
            if (pausedCustomer == null && pausedCustomerPatience == null && !counterSystemsPaused)
            {
                return;
            }

            if (pausedCustomerPatience != null)
            {
                pausedCustomerPatience.ResetDrainMultiplier();
            }

            if (pausedCustomer != null)
            {
                pausedCustomer.ResetPatienceDrainMultiplier();
            }

            pausedCustomer = null;
            pausedCustomerPatience = null;
            SetCounterSystemsPaused(false);
            OnCounterTransactionResumed.Invoke();
        }

        private void SetCounterSystemsPaused(bool paused)
        {
            if (paused == counterSystemsPaused)
            {
                return;
            }

            if (pausableCounterSystems == null || pausableCounterSystems.Length == 0)
            {
                counterSystemsPaused = paused;
                return;
            }

            if (paused)
            {
                counterSystemsPaused = true;
                pausablePreviousStates = new bool[pausableCounterSystems.Length];
                for (var i = 0; i < pausableCounterSystems.Length; i++)
                {
                    var system = pausableCounterSystems[i];
                    if (system == null)
                    {
                        continue;
                    }

                    pausablePreviousStates[i] = system.enabled;
                    system.enabled = false;
                }

                return;
            }

            for (var i = 0; i < pausableCounterSystems.Length; i++)
            {
                var system = pausableCounterSystems[i];
                if (system == null)
                {
                    continue;
                }

                var wasEnabled = i < pausablePreviousStates.Length && pausablePreviousStates[i];
                system.enabled = wasEnabled;
            }

            pausablePreviousStates = System.Array.Empty<bool>();
            counterSystemsPaused = false;
        }

        private bool IsUrgentDocumentDelivered()
        {
            if (archiveDeskZone == null || player == null || playerInteraction == null)
            {
                return false;
            }

            if (!archiveDeskZone.bounds.Contains(player.position))
            {
                return false;
            }

            var heldObject = playerInteraction.HeldObject;
            if (heldObject == null)
            {
                return false;
            }

            if (heldObject == activeDocument)
            {
                return true;
            }

            return heldObject.TryGetComponent<DeliverableItem>(out var item)
                && item.ItemId == UrgentDocumentId;
        }

        private void CompleteArchiveDelivery()
        {
            if (playerInteraction != null && playerInteraction.HeldObject != null)
            {
                playerInteraction.TryDestroyHeldObject();
            }
            else if (activeDocument != null)
            {
                Destroy(activeDocument);
            }

            activeDocument = null;

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.AddTime(rewardTime);
            }

            if (completionEffect != null)
            {
                completionEffect.Play();
            }

            ResumePausedCounterTransaction();
            OnInterruptionCompleted.Invoke();

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(ResolveAndLeave());
        }

        private IEnumerator ResolveAndLeave()
        {
            SetState(StaffInterruptionState.Resolved);

            if (activeCoworker != null)
            {
                var exitPosition = coworkerExitPoint != null ? coworkerExitPoint.position : activeCoworker.transform.position;
                yield return MoveObjectTo(activeCoworker.transform, exitPosition);
                Destroy(activeCoworker);
                activeCoworker = null;
            }

            ResetInterruptionTimer();
            SetState(StaffInterruptionState.Idle);
            routine = null;
        }

        private IEnumerator MoveObjectTo(Transform target, Vector3 destination)
        {
            if (target == null)
            {
                yield break;
            }

            while ((target.position - destination).sqrMagnitude > 0.01f)
            {
                var currentPosition = target.position;
                var nextPosition = Vector3.MoveTowards(currentPosition, destination, coworkerMoveSpeed * Time.deltaTime);
                target.position = nextPosition;

                var direction = destination - currentPosition;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    target.rotation = Quaternion.Slerp(
                        target.rotation,
                        Quaternion.LookRotation(direction, Vector3.up),
                        8f * Time.deltaTime);
                }

                yield return null;
            }
        }

        private Vector3 GetCounterPoint()
        {
            if (counterInterruptionPoint != null)
            {
                return counterInterruptionPoint.position;
            }

            return player != null ? player.position + player.forward : transform.position;
        }

        private void ResetInterruptionTimer()
        {
            var min = Mathf.Min(interruptionIntervalSeconds.x, interruptionIntervalSeconds.y);
            var max = Mathf.Max(interruptionIntervalSeconds.x, interruptionIntervalSeconds.y);
            nextInterruptionTimer = Random.Range(min, max);
        }

        private void SetState(StaffInterruptionState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            OnStateChanged.Invoke(state);
        }

        private static void TrySetTag(GameObject target, string tagName)
        {
            if (target == null)
            {
                return;
            }

            try
            {
                target.tag = tagName;
            }
            catch (UnityException)
            {
                // The prototype setup creates this tag. This keeps hand-built scenes from throwing.
            }
        }
    }
}
