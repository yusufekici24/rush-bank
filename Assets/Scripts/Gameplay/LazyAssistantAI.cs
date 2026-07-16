using System.Collections;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public enum LazyAssistantState
    {
        Inactive,
        WalkingToCounter,
        Serving,
        GoingToBreak
    }

    public class LazyAssistantAI : MonoBehaviour
    {
        public static LazyAssistantAI Instance { get; private set; }

        [Header("Navigation")]
        [SerializeField] private Transform assistantRoot;
        [SerializeField] private Transform secondaryCounter;
        [SerializeField] private Transform breakExitPoint;
        [SerializeField] private Vector3 customerCounterOffset = new Vector3(0f, 0f, 1.15f);
        [SerializeField, Min(0.1f)] private float moveSpeed = 1.45f;
        [SerializeField, Min(0.01f)] private float arriveDistance = 0.08f;

        [Header("Serving")]
        [SerializeField, Range(0.05f, 1f)] private float serveSpeedMultiplier = 0.5f;
        [SerializeField, Min(1)] private int maxTasksBeforeBreak = 2;
        [SerializeField, Min(1)] private int currentTasksBeforeBreak = 2;
        [SerializeField, Min(1f)] private float snackSlowingPenalty = 1.2f;
        [SerializeField, Min(1)] private int snackExtraTasks = 1;
        [SerializeField, Min(0.1f)] private float withdrawalBaseSeconds = 5f;
        [SerializeField, Min(0.1f)] private float passbookBaseSeconds = 4f;
        [SerializeField, Min(0.1f)] private float complexTaskBaseSeconds = 7f;
        [SerializeField, Min(0f)] private float reducedTimeBonus = 2f;
        [SerializeField, Min(0)] private int reducedScore = 25;

        [Header("References")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private CashDeliverySystem cashDeliverySystem;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform coffeeMugHoldPoint;
        [SerializeField] private GameObject coffeeMugPrefab;
        [SerializeField] private Transform snackIndicatorAnchor;
        [SerializeField] private GameObject snackIndicatorPrefab;

        [Header("Animator Triggers")]
        [SerializeField] private string walkTrigger = "Walk";
        [SerializeField] private string stretchTrigger = "Stretch";
        [SerializeField] private string yawnTrigger = "Yawn";
        [SerializeField] private string serveTrigger = "Serve";
        [SerializeField] private string waveTrigger = "Wave";
        [SerializeField] private string grabCoffeeTrigger = "GrabCoffee";
        [SerializeField] private string munchTrigger = "Munch";

        public UnityEvent OnAssistantActivated = new UnityEvent();
        public UnityEvent<int> OnAssistantTaskCompleted = new UnityEvent<int>();
        public UnityEvent<int> OnAssistantFedSnack = new UnityEvent<int>();
        public UnityEvent OnAssistantLeave = new UnityEvent();

        private Coroutine assistantRoutine;
        private GameObject coffeeMugInstance;
        private GameObject snackIndicatorInstance;
        private float defaultAnimatorSpeed = 1f;
        private float snackServeTimeMultiplier = 1f;
        private int snacksFed;

        public LazyAssistantState State { get; private set; } = LazyAssistantState.Inactive;
        public int TasksCompleted { get; private set; }
        public float ServeSpeedMultiplier => serveSpeedMultiplier;
        public int MaxTasksBeforeBreak => maxTasksBeforeBreak;
        public int CurrentTasksBeforeBreak => currentTasksBeforeBreak;
        public int RemainingTaskCapacity => Mathf.Max(0, currentTasksBeforeBreak - TasksCompleted);
        public bool CanReceiveSnack => State == LazyAssistantState.WalkingToCounter || State == LazyAssistantState.Serving;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (assistantRoot == null)
            {
                assistantRoot = transform;
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (cashDeliverySystem == null)
            {
                cashDeliverySystem = FindFirstObjectByType<CashDeliverySystem>();
            }

            if (animator != null)
            {
                defaultAnimatorSpeed = animator.speed;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ActivateAssistant()
        {
            if (assistantRoutine != null)
            {
                return;
            }

            TasksCompleted = 0;
            snacksFed = 0;
            snackServeTimeMultiplier = 1f;
            currentTasksBeforeBreak = maxTasksBeforeBreak;
            RefreshSnackIndicator();
            assistantRoutine = StartCoroutine(RunAssistantShift());
        }

        public void ActivateFromManager()
        {
            ActivateAssistant();
        }

        public void StopAssistant()
        {
            if (assistantRoutine != null)
            {
                StopCoroutine(assistantRoutine);
                assistantRoutine = null;
            }

            SetAnimatorSpeed(defaultAnimatorSpeed);
            ClearSnackIndicator();
            State = LazyAssistantState.Inactive;
        }

        public bool FeedSnack(GameObject snackItem)
        {
            if (!CanReceiveSnack || snackItem == null)
            {
                return false;
            }

            currentTasksBeforeBreak += Mathf.Max(1, snackExtraTasks);
            snackServeTimeMultiplier *= Mathf.Max(1f, snackSlowingPenalty);
            snacksFed++;

            PlayTrigger(munchTrigger);
            RefreshSnackIndicator();
            Destroy(snackItem);
            OnAssistantFedSnack.Invoke(RemainingTaskCapacity);
            return true;
        }

        private IEnumerator RunAssistantShift()
        {
            OnAssistantActivated.Invoke();
            State = LazyAssistantState.WalkingToCounter;
            PlayTrigger(walkTrigger);

            yield return MoveRootTo(GetCounterPosition());

            while (TasksCompleted < currentTasksBeforeBreak)
            {
                var customer = TakeNextCustomer();
                if (customer == null)
                {
                    break;
                }

                yield return MoveCustomerToAssistantCounter(customer);
                yield return ServeCustomer(customer);
            }

            yield return GoToBreak();
            assistantRoutine = null;
            State = LazyAssistantState.Inactive;
        }

        private QueueCustomer TakeNextCustomer()
        {
            var manager = queueManager != null ? queueManager : QueueManager.Instance;
            return manager != null ? manager.TakeNextCustomerForAssistant() : null;
        }

        private IEnumerator MoveRootTo(Vector3 targetPosition)
        {
            while ((assistantRoot.position - targetPosition).sqrMagnitude > arriveDistance * arriveDistance)
            {
                MoveTransform(assistantRoot, targetPosition);
                yield return null;
            }
        }

        private IEnumerator MoveCustomerToAssistantCounter(QueueCustomer customer)
        {
            if (customer == null)
            {
                yield break;
            }

            var targetPosition = GetCustomerCounterPosition();
            while (customer != null && (customer.transform.position - targetPosition).sqrMagnitude > arriveDistance * arriveDistance)
            {
                customer.MoveTowards(targetPosition, moveSpeed);
                yield return null;
            }
        }

        private IEnumerator ServeCustomer(QueueCustomer customer)
        {
            if (customer == null)
            {
                yield break;
            }

            State = LazyAssistantState.Serving;
            PlayTrigger(stretchTrigger);
            PlayTrigger(serveTrigger);
            SetAnimatorSpeed(Mathf.Max(0.05f, defaultAnimatorSpeed * serveSpeedMultiplier));

            var serveDuration = GetServeSeconds(customer.RequestKind);
            var elapsed = 0f;
            while (elapsed < serveDuration && customer != null)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetAnimatorSpeed(defaultAnimatorSpeed);

            if (RequiresVaultCash(customer.RequestKind)
                && cashDeliverySystem != null
                && !cashDeliverySystem.TryConsumeCashForWithdrawal())
            {
                TasksCompleted = currentTasksBeforeBreak;
                yield break;
            }

            if (AddsVaultCash(customer.RequestKind))
            {
                cashDeliverySystem?.AddCashFromDeposit();
            }

            if (customer != null)
            {
                customer.StopPatience();
                customer.ClearRequestIcon();
                Destroy(customer.gameObject);
            }

            if (TimeManager.Instance != null && reducedTimeBonus > 0f)
            {
                TimeManager.Instance.AddTime(reducedTimeBonus);
            }

            if (ScoreManager.Instance != null && reducedScore > 0)
            {
                ScoreManager.Instance.AddScore(reducedScore);
            }

            TasksCompleted++;
            RefreshSnackIndicator();
            OnAssistantTaskCompleted.Invoke(TasksCompleted);
        }

        private IEnumerator GoToBreak()
        {
            State = LazyAssistantState.GoingToBreak;
            PlayTrigger(yawnTrigger);
            PlayTrigger(waveTrigger);
            GrabCoffeeMug();
            PlayTrigger(grabCoffeeTrigger);
            PlayTrigger(walkTrigger);

            yield return MoveRootTo(GetBreakExitPosition());

            SetAnimatorSpeed(defaultAnimatorSpeed);
            ClearSnackIndicator();
            OnAssistantLeave.Invoke();
        }

        private float GetServeSeconds(CustomerRequestKind requestKind)
        {
            var baseSeconds = requestKind switch
            {
                CustomerRequestKind.Withdraw => withdrawalBaseSeconds,
                CustomerRequestKind.Deposit => withdrawalBaseSeconds,
                CustomerRequestKind.CashWithdrawDeposit => withdrawalBaseSeconds,
                CustomerRequestKind.OpenAccount => passbookBaseSeconds,
                CustomerRequestKind.PassbookPrinting => passbookBaseSeconds,
                CustomerRequestKind.BillPayment => passbookBaseSeconds,
                CustomerRequestKind.MobileActivation => passbookBaseSeconds,
                CustomerRequestKind.PhilanthropistCustomer => passbookBaseSeconds,
                CustomerRequestKind.CardBlockRemoval => passbookBaseSeconds,
                CustomerRequestKind.InsuranceReferral => passbookBaseSeconds,
                CustomerRequestKind.ScammerCustomer => complexTaskBaseSeconds,
                CustomerRequestKind.BarutCustomer => complexTaskBaseSeconds,
                _ => complexTaskBaseSeconds
            };

            return baseSeconds / Mathf.Max(0.05f, serveSpeedMultiplier) * snackServeTimeMultiplier;
        }

        private static bool RequiresVaultCash(CustomerRequestKind requestKind)
        {
            return requestKind == CustomerRequestKind.Withdraw
                || requestKind == CustomerRequestKind.CashWithdrawDeposit;
        }

        private static bool AddsVaultCash(CustomerRequestKind requestKind)
        {
            return requestKind == CustomerRequestKind.Deposit;
        }

        private Vector3 GetCounterPosition()
        {
            return secondaryCounter != null ? secondaryCounter.position : assistantRoot.position;
        }

        private Vector3 GetCustomerCounterPosition()
        {
            if (secondaryCounter == null)
            {
                return assistantRoot.position + assistantRoot.forward * customerCounterOffset.z;
            }

            return secondaryCounter.position + secondaryCounter.TransformDirection(customerCounterOffset);
        }

        private Vector3 GetBreakExitPosition()
        {
            return breakExitPoint != null ? breakExitPoint.position : assistantRoot.position - assistantRoot.forward * 3f;
        }

        private void MoveTransform(Transform target, Vector3 targetPosition)
        {
            var currentPosition = target.position;
            var nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.deltaTime);
            target.position = nextPosition;

            var direction = targetPosition - currentPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                target.rotation = Quaternion.Slerp(
                    target.rotation,
                    Quaternion.LookRotation(direction, Vector3.up),
                    8f * Time.deltaTime);
            }
        }

        private void GrabCoffeeMug()
        {
            if (coffeeMugPrefab == null || coffeeMugInstance != null)
            {
                return;
            }

            var parent = coffeeMugHoldPoint != null ? coffeeMugHoldPoint : assistantRoot;
            coffeeMugInstance = Instantiate(coffeeMugPrefab, parent);
            coffeeMugInstance.transform.localPosition = Vector3.zero;
            coffeeMugInstance.transform.localRotation = Quaternion.identity;
        }

        private void RefreshSnackIndicator()
        {
            if (snacksFed <= 0)
            {
                ClearSnackIndicator();
                return;
            }

            if (snackIndicatorInstance == null)
            {
                snackIndicatorInstance = CreateSnackIndicator();
            }

            if (snackIndicatorInstance != null)
            {
                snackIndicatorInstance.SetActive(true);
                snackIndicatorInstance.transform.localScale = Vector3.one * Mathf.Clamp(0.75f + RemainingTaskCapacity * 0.08f, 0.75f, 1.25f);
            }
        }

        private GameObject CreateSnackIndicator()
        {
            var parent = snackIndicatorAnchor != null ? snackIndicatorAnchor : assistantRoot;
            if (snackIndicatorPrefab != null)
            {
                var instance = Instantiate(snackIndicatorPrefab, parent);
                instance.transform.localPosition = Vector3.up * 1.65f;
                instance.transform.localRotation = Quaternion.identity;
                return instance;
            }

            var cookie = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cookie.name = "Assistant Snack Indicator";
            cookie.transform.SetParent(parent, false);
            cookie.transform.localPosition = Vector3.up * 1.65f;
            cookie.transform.localScale = new Vector3(0.22f, 0.08f, 0.22f);

            if (cookie.TryGetComponent<Collider>(out var colliderComponent))
            {
                Destroy(colliderComponent);
            }

            if (cookie.TryGetComponent<Renderer>(out var rendererComponent))
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.76f, 0.42f, 0.16f);
                rendererComponent.sharedMaterial = material;
            }

            return cookie;
        }

        private void ClearSnackIndicator()
        {
            if (snackIndicatorInstance == null)
            {
                return;
            }

            Destroy(snackIndicatorInstance);
            snackIndicatorInstance = null;
        }

        private void PlayTrigger(string triggerName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            {
                return;
            }

            animator.SetTrigger(triggerName);
        }

        private void SetAnimatorSpeed(float speed)
        {
            if (animator != null)
            {
                animator.speed = speed;
            }
        }
    }
}
