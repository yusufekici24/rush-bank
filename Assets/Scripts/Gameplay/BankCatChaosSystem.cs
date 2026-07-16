using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum BankCatChaosState
    {
        Idle,
        CalmingClients,
        PanicTriggered,
        GuardChasing,
        Resolved
    }

    public class BankCatChaosSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private SecurityGuardAI securityGuardAI;
        [SerializeField] private Transform entrancePoint;
        [SerializeField] private Transform catExitPoint;
        [SerializeField] private Transform guardIdlePost;
        [SerializeField] private GameObject catPrefab;
        [SerializeField] private Transform[] catRunAwayWaypoints;

        [Header("UI")]
        [SerializeField] private Button callSecurityButton;
        [SerializeField] private GameObject noCatsWarningIconPrefab;
        [SerializeField] private float buttonFlashSpeed = 6f;

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float calmingSeconds = 15f;
        [SerializeField, Min(0.1f)] private float chaseSeconds = 10f;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float catWalkSpeed = 1.7f;
        [SerializeField, Min(0.1f)] private float guardChaseSpeed = 3.6f;
        [SerializeField, Min(1f)] private float catChaseSpeedMultiplier = 1.5f;
        [SerializeField, Min(0.1f)] private float waypointReachDistance = 0.35f;

        [Header("Animation Triggers")]
        [SerializeField] private string customerScaredTrigger = "Scared";
        [SerializeField] private string guardChasingTrigger = "Chasing";
        [SerializeField] private string guardStumbleTrigger = "Stumble";
        [SerializeField] private string guardTiredTrigger = "Tired";
        [SerializeField] private string guardIdleTrigger = "Idle";

        public UnityEvent<BankCatChaosState> OnStateChanged = new UnityEvent<BankCatChaosState>();
        public UnityEvent OnCatSpawned = new UnityEvent();
        public UnityEvent<GameObject> OnCustomerScared = new UnityEvent<GameObject>();
        public UnityEvent OnSecurityCalledForCat = new UnityEvent();
        public UnityEvent OnCatChaosResolved = new UnityEvent();

        private BankCatChaosState state = BankCatChaosState.Idle;
        private GameObject activeCat;
        private NavMeshAgent activeCatAgent;
        private Animator guardAnimator;
        private NavMeshAgent guardAgent;
        private Coroutine stateRoutine;
        private Coroutine buttonFlashRoutine;
        private GameObject activeNoCatsIcon;
        private Color baseButtonColor = Color.white;
        private Image callSecurityButtonImage;

        public BankCatChaosState State => state;
        public bool IsActive => state != BankCatChaosState.Idle && state != BankCatChaosState.Resolved;

        private void Awake()
        {
            ResolveMissingReferences();
            CacheButtonVisuals();
            SetCallSecurityButton(false);
        }

        private void OnEnable()
        {
            if (callSecurityButton != null)
            {
                callSecurityButton.onClick.AddListener(CallSecurityForCat);
            }
        }

        private void OnDisable()
        {
            if (callSecurityButton != null)
            {
                callSecurityButton.onClick.RemoveListener(CallSecurityForCat);
            }

            StopActiveRoutine();
            ResetQueueDrainMultipliersToNormal();
            SetCallSecurityButton(false);
            DestroyActiveCat();
            ClearNoCatsIcon();
        }

        public void SpawnCat()
        {
            if (state != BankCatChaosState.Idle && state != BankCatChaosState.Resolved)
            {
                return;
            }

            StopActiveRoutine();
            stateRoutine = StartCoroutine(CatChaosRoutine());
        }

        public void CallSecurityForCat()
        {
            if (state != BankCatChaosState.PanicTriggered || activeCat == null)
            {
                return;
            }

            StopActiveRoutine();
            stateRoutine = StartCoroutine(GuardChaseRoutine());
        }

        private IEnumerator CatChaosRoutine()
        {
            SpawnCatObject();
            SetState(BankCatChaosState.CalmingClients);
            SetQueueDrainMultiplier(0f);

            var elapsed = 0f;
            while (elapsed < calmingSeconds && activeCat != null)
            {
                elapsed += Time.deltaTime;
                MoveCatNearWaitingCustomer();
                yield return null;
            }

            TriggerPanic();
        }

        private void TriggerPanic()
        {
            if (activeCat == null)
            {
                ResolveChaos();
                return;
            }

            SetState(BankCatChaosState.PanicTriggered);
            var scaredCustomer = PickRandomWaitingCustomer();
            if (scaredCustomer != null)
            {
                PlayScaredFeedback(scaredCustomer);
                OnCustomerScared.Invoke(scaredCustomer.gameObject);
            }

            SetQueueDrainMultiplier(2f);
            SetCallSecurityButton(true);
        }

        private IEnumerator GuardChaseRoutine()
        {
            SetState(BankCatChaosState.GuardChasing);
            SetCallSecurityButton(false);
            ClearNoCatsIcon();
            ResetQueueDrainMultipliersToNormal();
            OnSecurityCalledForCat.Invoke();

            var guard = securityGuardAI != null ? securityGuardAI.transform : null;
            guardAnimator = securityGuardAI != null ? securityGuardAI.GetComponentInChildren<Animator>() : guardAnimator;
            guardAgent = securityGuardAI != null ? securityGuardAI.GetComponent<NavMeshAgent>() : guardAgent;

            PlayGuardTrigger(guardChasingTrigger);
            var elapsed = 0f;
            var nextCatTarget = PickRunAwayPosition();
            while (elapsed < chaseSeconds && activeCat != null)
            {
                elapsed += Time.deltaTime;
                MoveCatToward(nextCatTarget, guardChaseSpeed * catChaseSpeedMultiplier);

                if ((activeCat.transform.position - nextCatTarget).sqrMagnitude <= waypointReachDistance * waypointReachDistance)
                {
                    nextCatTarget = PickRunAwayPosition();
                    PlayGuardTrigger(guardStumbleTrigger);
                }

                if (guard != null)
                {
                    MoveGuardToward(activeCat.transform.position);
                }

                yield return null;
            }

            yield return ResolveAfterChaseRoutine();
        }

        private IEnumerator ResolveAfterChaseRoutine()
        {
            SetState(BankCatChaosState.Resolved);
            ResetQueueDrainMultipliersToNormal();
            var exitPosition = catExitPoint != null
                ? catExitPoint.position
                : entrancePoint != null
                    ? entrancePoint.position
                    : transform.position + Vector3.back * 5f;

            while (activeCat != null && (activeCat.transform.position - exitPosition).sqrMagnitude > 0.04f)
            {
                MoveCatToward(exitPosition, guardChaseSpeed * catChaseSpeedMultiplier);
                yield return null;
            }

            DestroyActiveCat();
            PlayGuardTrigger(guardTiredTrigger);
            yield return MoveGuardHomeRoutine();
            PlayGuardTrigger(guardIdleTrigger);
            OnCatChaosResolved.Invoke();
            SetState(BankCatChaosState.Idle);
            stateRoutine = null;
        }

        private void SpawnCatObject()
        {
            DestroyActiveCat();
            var spawnPosition = entrancePoint != null ? entrancePoint.position : transform.position;
            activeCat = catPrefab != null ? Instantiate(catPrefab, spawnPosition, Quaternion.identity) : CreateFallbackCat(spawnPosition);
            activeCat.name = "Pati Bank Cat";
            activeCatAgent = activeCat.GetComponent<NavMeshAgent>();
            if (activeCatAgent != null)
            {
                activeCatAgent.speed = catWalkSpeed;
            }

            OnCatSpawned.Invoke();
        }

        private GameObject CreateFallbackCat(Vector3 position)
        {
            var cat = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cat.transform.position = position;
            cat.transform.localScale = new Vector3(0.42f, 0.34f, 0.62f);

            if (cat.TryGetComponent<Renderer>(out var rendererComponent))
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.95f, 0.58f, 0.28f);
                rendererComponent.sharedMaterial = material;
            }

            CreatePrimitiveChild(cat.transform, "Cat Head", PrimitiveType.Sphere, new Vector3(0f, 0.22f, 0.34f), new Vector3(0.58f, 0.48f, 0.48f), new Color(0.95f, 0.58f, 0.28f));
            CreatePrimitiveChild(cat.transform, "Cat Tail", PrimitiveType.Cylinder, new Vector3(0f, 0.1f, -0.42f), new Vector3(0.12f, 0.52f, 0.12f), new Color(0.95f, 0.58f, 0.28f));
            return cat;
        }

        private static void CreatePrimitiveChild(Transform parent, string name, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var child = GameObject.CreatePrimitive(primitiveType);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;

            if (child.TryGetComponent<Renderer>(out var rendererComponent))
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = color;
                rendererComponent.sharedMaterial = material;
            }

            var collider = child.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        private void MoveCatNearWaitingCustomer()
        {
            var target = PickRandomWaitingCustomer();
            if (target == null)
            {
                MoveCatToward(GetEntrancePosition(), catWalkSpeed);
                return;
            }

            var offset = new Vector3(Mathf.Sin(Time.time * 2f) * 0.55f, 0f, Mathf.Cos(Time.time * 2f) * 0.55f);
            MoveCatToward(target.transform.position + offset, catWalkSpeed);
        }

        private void MoveCatToward(Vector3 destination, float speed)
        {
            if (activeCat == null)
            {
                return;
            }

            if (activeCatAgent != null && activeCatAgent.enabled && activeCatAgent.isOnNavMesh)
            {
                activeCatAgent.speed = speed;
                activeCatAgent.SetDestination(destination);
                return;
            }

            var current = activeCat.transform.position;
            activeCat.transform.position = Vector3.MoveTowards(current, destination, speed * Time.deltaTime);
            RotateToward(activeCat.transform, destination - current, 12f);
        }

        private void MoveGuardToward(Vector3 destination)
        {
            var guard = securityGuardAI != null ? securityGuardAI.transform : null;
            if (guard == null)
            {
                return;
            }

            if (guardAgent != null && guardAgent.enabled && guardAgent.isOnNavMesh)
            {
                guardAgent.speed = guardChaseSpeed;
                guardAgent.SetDestination(destination);
                return;
            }

            var current = guard.position;
            guard.position = Vector3.MoveTowards(current, destination, guardChaseSpeed * Time.deltaTime);
            RotateToward(guard, destination - current, 10f);
        }

        private IEnumerator MoveGuardHomeRoutine()
        {
            var guard = securityGuardAI != null ? securityGuardAI.transform : null;
            if (guard == null || guardIdlePost == null)
            {
                yield break;
            }

            while ((guard.position - guardIdlePost.position).sqrMagnitude > 0.04f)
            {
                MoveGuardToward(guardIdlePost.position);
                yield return null;
            }
        }

        private static void RotateToward(Transform target, Vector3 direction, float speed)
        {
            direction.y = 0f;
            if (target == null || direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            target.rotation = Quaternion.Slerp(
                target.rotation,
                Quaternion.LookRotation(direction.normalized, Vector3.up),
                speed * Time.deltaTime);
        }

        private QueueCustomer PickRandomWaitingCustomer()
        {
            if (queueManager == null || queueManager.CustomerQueue.Count == 0)
            {
                return null;
            }

            var attempts = queueManager.CustomerQueue.Count;
            for (var i = 0; i < attempts; i++)
            {
                var customerObject = queueManager.CustomerQueue[Random.Range(0, queueManager.CustomerQueue.Count)];
                if (customerObject != null && customerObject.TryGetComponent<QueueCustomer>(out var customer))
                {
                    return customer;
                }
            }

            return null;
        }

        private Vector3 PickRunAwayPosition()
        {
            if (catRunAwayWaypoints != null && catRunAwayWaypoints.Length > 0)
            {
                var waypoint = catRunAwayWaypoints[Random.Range(0, catRunAwayWaypoints.Length)];
                if (waypoint != null)
                {
                    return waypoint.position;
                }
            }

            var center = activeCat != null ? activeCat.transform.position : GetEntrancePosition();
            var randomCircle = Random.insideUnitCircle.normalized * Random.Range(2.2f, 4.5f);
            return center + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }

        private void PlayScaredFeedback(QueueCustomer customer)
        {
            if (customer == null)
            {
                return;
            }

            var animator = customer.GetComponentInChildren<Animator>();
            if (animator != null && !string.IsNullOrWhiteSpace(customerScaredTrigger))
            {
                animator.SetTrigger(customerScaredTrigger);
            }

            if (noCatsWarningIconPrefab != null)
            {
                activeNoCatsIcon = Instantiate(noCatsWarningIconPrefab, customer.transform);
                activeNoCatsIcon.transform.localPosition = Vector3.up * 2.15f;
                return;
            }

            activeNoCatsIcon = new GameObject("No Cats Warning Icon");
            activeNoCatsIcon.transform.SetParent(customer.transform, false);
            activeNoCatsIcon.transform.localPosition = Vector3.up * 2.15f;
            activeNoCatsIcon.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var label = activeNoCatsIcon.AddComponent<TextMesh>();
            label.text = "NO CAT!";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.18f;
            label.color = new Color(1f, 0.16f, 0.18f);
        }

        private void SetQueueDrainMultiplier(float multiplier)
        {
            if (queueManager == null)
            {
                return;
            }

            var queue = queueManager.CustomerQueue;
            for (var i = 0; i < queue.Count; i++)
            {
                ApplyDrainMultiplier(queue[i], multiplier);
            }

            if (queueManager.ActiveCustomer != null)
            {
                ApplyDrainMultiplier(queueManager.ActiveCustomer.gameObject, multiplier);
            }
        }

        private void ApplyDrainMultiplier(GameObject customerObject, float multiplier)
        {
            if (customerObject == null)
            {
                return;
            }

            if (customerObject.TryGetComponent<QueueCustomer>(out var queueCustomer))
            {
                queueCustomer.SetPatienceDrainMultiplier(multiplier);
            }

            if (customerObject.TryGetComponent<CustomerPatience>(out var patience))
            {
                patience.SetDrainMultiplier(multiplier);
            }
        }

        private void ResetQueueDrainMultipliersToNormal()
        {
            if (queueManager != null)
            {
                var queue = queueManager.CustomerQueue;
                for (var i = 0; i < queue.Count; i++)
                {
                    ApplyDrainMultiplier(queue[i], 1f);
                }

                if (queueManager.ActiveCustomer != null)
                {
                    ApplyDrainMultiplier(queueManager.ActiveCustomer.gameObject, 1f);
                }
            }
        }

        private void SetCallSecurityButton(bool visible)
        {
            if (callSecurityButton == null)
            {
                return;
            }

            callSecurityButton.gameObject.SetActive(visible);
            if (visible)
            {
                if (buttonFlashRoutine != null)
                {
                    StopCoroutine(buttonFlashRoutine);
                }

                buttonFlashRoutine = StartCoroutine(ButtonFlashRoutine());
                return;
            }

            if (buttonFlashRoutine != null)
            {
                StopCoroutine(buttonFlashRoutine);
                buttonFlashRoutine = null;
            }

            if (callSecurityButtonImage != null)
            {
                callSecurityButtonImage.color = baseButtonColor;
            }
        }

        private IEnumerator ButtonFlashRoutine()
        {
            while (callSecurityButton != null && callSecurityButton.gameObject.activeSelf)
            {
                if (callSecurityButtonImage != null)
                {
                    var pulse = (Mathf.Sin(Time.time * buttonFlashSpeed) + 1f) * 0.5f;
                    callSecurityButtonImage.color = Color.Lerp(baseButtonColor, Color.red, pulse);
                }

                yield return null;
            }
        }

        private void PlayGuardTrigger(string triggerName)
        {
            if (guardAnimator == null && securityGuardAI != null)
            {
                guardAnimator = securityGuardAI.GetComponentInChildren<Animator>();
            }

            if (guardAnimator != null && !string.IsNullOrWhiteSpace(triggerName))
            {
                guardAnimator.SetTrigger(triggerName);
            }
        }

        private void SetState(BankCatChaosState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            OnStateChanged.Invoke(state);
        }

        private void ResolveChaos()
        {
            StopActiveRoutine();
            ResetQueueDrainMultipliersToNormal();
            SetCallSecurityButton(false);
            DestroyActiveCat();
            ClearNoCatsIcon();
            SetState(BankCatChaosState.Idle);
        }

        private void StopActiveRoutine()
        {
            if (stateRoutine != null)
            {
                StopCoroutine(stateRoutine);
                stateRoutine = null;
            }
        }

        private void DestroyActiveCat()
        {
            if (activeCat != null)
            {
                Destroy(activeCat);
                activeCat = null;
                activeCatAgent = null;
            }
        }

        private void ClearNoCatsIcon()
        {
            if (activeNoCatsIcon != null)
            {
                Destroy(activeNoCatsIcon);
                activeNoCatsIcon = null;
            }
        }

        private Vector3 GetEntrancePosition()
        {
            return entrancePoint != null ? entrancePoint.position : transform.position;
        }

        private void CacheButtonVisuals()
        {
            if (callSecurityButton == null)
            {
                return;
            }

            callSecurityButtonImage = callSecurityButton.GetComponent<Image>();
            if (callSecurityButtonImage != null)
            {
                baseButtonColor = callSecurityButtonImage.color;
            }
        }

        private void ResolveMissingReferences()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance != null ? QueueManager.Instance : FindFirstObjectByType<QueueManager>();
            }

            if (securityGuardAI == null)
            {
                securityGuardAI = FindFirstObjectByType<SecurityGuardAI>();
            }

            if (securityGuardAI != null)
            {
                guardAgent = securityGuardAI.GetComponent<NavMeshAgent>();
                guardAnimator = securityGuardAI.GetComponentInChildren<Animator>();
            }
        }
    }
}
