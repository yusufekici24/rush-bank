using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public class AccountOpeningSystem : MonoBehaviour
    {
        public static AccountOpeningSystem Instance { get; private set; }

        [Header("References")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private Transform relationshipManagerDesk;
        [SerializeField] private GameObject folderAndPenIconPrefab;
        [SerializeField] private GameObject quickBoostIconRoot;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip wetInkStampSound;
        [SerializeField] private StationeryDeliverySystem stationeryDeliverySystem;

        [Header("Stamp")]
        [SerializeField, Min(0.01f)] private float stampSeconds = 0.5f;
        [SerializeField, Min(0.1f)] private float redirectedCustomerMoveSpeed = 2.4f;

        public UnityEvent<QueueCustomer> OnAccountOpeningCustomerReady = new UnityEvent<QueueCustomer>();
        public UnityEvent<QueueCustomer> OnAccountOpeningRedirected = new UnityEvent<QueueCustomer>();
        public UnityEvent<int> OnQuickBoostChargesChanged = new UnityEvent<int>();

        private QueueCustomer activeAccountCustomer;
        private GameObject fallbackIconInstance;
        private Coroutine stampRoutine;
        private int quickBoostCharges;
        private bool queueListenerRegistered;
        private float actionTimeMultiplier = 1f;

        public int QuickBoostCharges => quickBoostCharges;
        public bool HasQuickBoost => quickBoostCharges > 0;
        public float ActionTimeMultiplier
        {
            get => actionTimeMultiplier;
            set => actionTimeMultiplier = Mathf.Max(0.05f, value);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveMissingReferences();
            SetQuickBoostIcon(false);
        }

        private void OnEnable()
        {
            RegisterQueueListener();
        }

        private void Start()
        {
            ResolveMissingReferences();
            RegisterQueueListener();
        }

        private void OnDisable()
        {
            if (queueManager != null && queueListenerRegistered)
            {
                queueManager.OnCustomerCalled.RemoveListener(HandleCustomerCalled);
                queueListenerRegistered = false;
            }

            if (stampRoutine != null)
            {
                StopCoroutine(stampRoutine);
                stampRoutine = null;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static bool TryConsumeQuickBoostCharge()
        {
            return Instance != null && Instance.ConsumeQuickBoostCharge();
        }

        public void Interact()
        {
            TryStampAndRedirect();
        }

        public void TryStampAndRedirect()
        {
            if (activeAccountCustomer == null || stampRoutine != null)
            {
                return;
            }

            stampRoutine = StartCoroutine(StampAndRedirectRoutine());
        }

        public void ApplyQuickBoost()
        {
            quickBoostCharges += 2;
            SetQuickBoostIcon(quickBoostCharges > 0);
            OnQuickBoostChargesChanged.Invoke(quickBoostCharges);
        }

        public void CancelStamp()
        {
            if (stampRoutine != null)
            {
                StopCoroutine(stampRoutine);
                stampRoutine = null;
            }

            activeAccountCustomer = null;
            ClearFallbackIcon();
        }

        private bool ConsumeQuickBoostCharge()
        {
            if (quickBoostCharges <= 0)
            {
                return false;
            }

            quickBoostCharges--;
            SetQuickBoostIcon(quickBoostCharges > 0);
            OnQuickBoostChargesChanged.Invoke(quickBoostCharges);
            return true;
        }

        private IEnumerator StampAndRedirectRoutine()
        {
            yield return new WaitForSeconds(stampSeconds * actionTimeMultiplier);

            if (stationeryDeliverySystem != null && !stationeryDeliverySystem.CanAcceptRedirect(relationshipManagerDesk))
            {
                stampRoutine = null;
                yield break;
            }

            if (audioSource != null && wetInkStampSound != null)
            {
                audioSource.PlayOneShot(wetInkStampSound);
            }

            var redirectedCustomer = queueManager != null
                ? queueManager.ReleaseActiveCustomerForRedirect()
                : activeAccountCustomer;

            if (redirectedCustomer == null)
            {
                redirectedCustomer = activeAccountCustomer;
            }

            ClearFallbackIcon();
            var speedMultiplier = stationeryDeliverySystem != null
                ? stationeryDeliverySystem.ConsumeRedirectSpeedMultiplier(relationshipManagerDesk)
                : 1f;
            RedirectCustomerToRelationshipManager(redirectedCustomer, speedMultiplier);
            ApplyQuickBoost();
            OnAccountOpeningRedirected.Invoke(redirectedCustomer);

            activeAccountCustomer = null;
            stampRoutine = null;
        }

        private void HandleCustomerCalled(GameObject customerObject)
        {
            ClearFallbackIcon();
            activeAccountCustomer = null;

            if (customerObject == null || !customerObject.TryGetComponent<QueueCustomer>(out var queueCustomer))
            {
                return;
            }

            if (queueCustomer.RequestKind != CustomerRequestKind.OpenAccount)
            {
                return;
            }

            activeAccountCustomer = queueCustomer;
            if (folderAndPenIconPrefab != null)
            {
                queueCustomer.ShowRequestIcon(folderAndPenIconPrefab);
            }
            else
            {
                fallbackIconInstance = CreateFallbackFolderPenIcon(queueCustomer.transform);
            }

            OnAccountOpeningCustomerReady.Invoke(queueCustomer);
        }

        private void RedirectCustomerToRelationshipManager(QueueCustomer customer, float speedMultiplier)
        {
            if (customer == null || relationshipManagerDesk == null)
            {
                return;
            }

            var agent = customer.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                StartCoroutine(MoveAgentToDeskRoutine(agent, relationshipManagerDesk.position, speedMultiplier));
                return;
            }

            StartCoroutine(MoveCustomerToDeskRoutine(customer.transform, relationshipManagerDesk.position, speedMultiplier));
        }

        private IEnumerator MoveAgentToDeskRoutine(NavMeshAgent agent, Vector3 destination, float speedMultiplier)
        {
            if (agent == null)
            {
                yield break;
            }

            var previousSpeed = agent.speed;
            agent.speed = previousSpeed * Mathf.Max(0.1f, speedMultiplier);
            agent.SetDestination(destination);
            while (agent != null && agent.enabled && agent.isOnNavMesh && agent.pathPending)
            {
                yield return null;
            }

            while (agent != null
                && agent.enabled
                && agent.isOnNavMesh
                && agent.remainingDistance > Mathf.Max(agent.stoppingDistance, 0.1f))
            {
                yield return null;
            }

            if (agent != null)
            {
                agent.speed = previousSpeed;
            }
        }

        private IEnumerator MoveCustomerToDeskRoutine(Transform customer, Vector3 destination, float speedMultiplier)
        {
            while (customer != null && (customer.position - destination).sqrMagnitude > 0.04f)
            {
                var current = customer.position;
                customer.position = Vector3.MoveTowards(
                    current,
                    destination,
                    redirectedCustomerMoveSpeed * Mathf.Max(0.1f, speedMultiplier) * Time.deltaTime);

                var direction = destination - current;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    customer.rotation = Quaternion.Slerp(
                        customer.rotation,
                        Quaternion.LookRotation(direction.normalized, Vector3.up),
                        8f * Time.deltaTime);
                }

                yield return null;
            }
        }

        private GameObject CreateFallbackFolderPenIcon(Transform customer)
        {
            var icon = new GameObject("Account Opening Folder Pen Icon");
            icon.transform.SetParent(customer, false);
            icon.transform.localPosition = Vector3.up * 2.1f;
            icon.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var label = icon.AddComponent<TextMesh>();
            label.text = "FOLDER+PEN";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.16f;
            label.color = new Color(1f, 0.88f, 0.28f);
            return icon;
        }

        private void ClearFallbackIcon()
        {
            if (fallbackIconInstance != null)
            {
                Destroy(fallbackIconInstance);
                fallbackIconInstance = null;
            }
        }

        private void SetQuickBoostIcon(bool active)
        {
            if (quickBoostIconRoot != null)
            {
                quickBoostIconRoot.SetActive(active);
            }
        }

        private void ResolveMissingReferences()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance != null ? QueueManager.Instance : FindFirstObjectByType<QueueManager>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (stationeryDeliverySystem == null)
            {
                stationeryDeliverySystem = StationeryDeliverySystem.Instance != null
                    ? StationeryDeliverySystem.Instance
                    : FindFirstObjectByType<StationeryDeliverySystem>();
            }
        }

        private void RegisterQueueListener()
        {
            ResolveMissingReferences();
            if (queueManager == null || queueListenerRegistered)
            {
                return;
            }

            queueManager.OnCustomerCalled.AddListener(HandleCustomerCalled);
            queueListenerRegistered = true;
        }
    }
}
