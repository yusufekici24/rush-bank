using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public class RedAlertRedirectionSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private Transform relationshipManagerDesk;
        [SerializeField] private Button emergencyRedirectButton;
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private GameObject emergencyWarningIconPrefab;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip redirectSound;
        [SerializeField] private AudioClip triumphSound;

        [Header("Barut Customer Tuning")]
        [SerializeField, Range(0.01f, 1f)] private float startingPatiencePercent = 0.2f;
        [SerializeField, Min(1f)] private float patienceDrainMultiplier = 2f;
        [SerializeField, Min(0.01f)] private float redirectAnimationSeconds = 0.3f;
        [SerializeField, Min(0.1f)] private float redirectedCustomerMoveSpeed = 2.8f;

        [Header("Rewards")]
        [SerializeField, Min(0)] private int megaGoldReward = 200;
        [SerializeField, Range(0f, 1f)] private float vipReliefRestorePercent = 0.5f;

        [Header("Feedback")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GameObject globalReliefEffectRoot;

        public UnityEvent<QueueCustomer> OnBarutCustomerEntered = new UnityEvent<QueueCustomer>();
        public UnityEvent<QueueCustomer> OnBarutCustomerPrioritized = new UnityEvent<QueueCustomer>();
        public UnityEvent<QueueCustomer> OnBarutCustomerRedirected = new UnityEvent<QueueCustomer>();
        public UnityEvent<QueueCustomer> OnBarutCustomerMeltdown = new UnityEvent<QueueCustomer>();
        public UnityEvent OnVIPReliefApplied = new UnityEvent();

        private QueueCustomer activeBarutCustomer;
        private GameObject fallbackActiveIcon;
        private GameObject floatingRewardText;
        private Coroutine redirectRoutine;
        private bool queueListenersRegistered;

        private void Awake()
        {
            ResolveMissingReferences();
            SetEmergencyButton(false);
        }

        private void OnEnable()
        {
            RegisterQueueListeners();
            if (emergencyRedirectButton != null)
            {
                emergencyRedirectButton.onClick.AddListener(TriggerEmergencyRedirect);
            }
        }

        private void Start()
        {
            ResolveMissingReferences();
            RegisterQueueListeners();
        }

        private void OnDisable()
        {
            if (queueManager != null && queueListenersRegistered)
            {
                queueManager.OnCustomerEntered.RemoveListener(HandleCustomerEntered);
                queueManager.OnCustomerCalled.RemoveListener(HandleCustomerCalled);
                queueListenersRegistered = false;
            }

            if (emergencyRedirectButton != null)
            {
                emergencyRedirectButton.onClick.RemoveListener(TriggerEmergencyRedirect);
            }
        }

        private void Update()
        {
            TickCustomerPriorityInput();
            TickQueuedBarutTimeouts();
        }

        public void TriggerEmergencyRedirect()
        {
            if (activeBarutCustomer == null || redirectRoutine != null)
            {
                return;
            }

            redirectRoutine = StartCoroutine(EmergencyRedirectRoutine());
        }

        public void CancelActiveRedirection()
        {
            if (redirectRoutine != null)
            {
                StopCoroutine(redirectRoutine);
                redirectRoutine = null;
            }

            activeBarutCustomer = null;
            SetEmergencyButton(false);
            ClearFallbackActiveIcon();
        }

        private void HandleCustomerEntered(GameObject customerObject)
        {
            if (!TryGetBarutCustomer(customerObject, out var customer))
            {
                return;
            }

            ConfigureBarutCustomer(customer);
            OnBarutCustomerEntered.Invoke(customer);
        }

        private void HandleCustomerCalled(GameObject customerObject)
        {
            ClearFallbackActiveIcon();
            SetEmergencyButton(false);
            activeBarutCustomer = null;

            if (!TryGetBarutCustomer(customerObject, out var customer))
            {
                return;
            }

            activeBarutCustomer = customer;
            ConfigureBarutCustomer(customer);
            customer.SetPatiencePercent(startingPatiencePercent);
            customer.SetPatienceDrainMultiplier(patienceDrainMultiplier);
            ShowEmergencyIcon(customer);
            SetEmergencyButton(true);
        }

        private IEnumerator EmergencyRedirectRoutine()
        {
            SetEmergencyButton(false);
            PlaySound(redirectSound);
            yield return new WaitForSeconds(redirectAnimationSeconds);

            var redirectedCustomer = queueManager != null
                ? queueManager.ReleaseActiveCustomerForRedirect()
                : activeBarutCustomer;

            if (redirectedCustomer == null)
            {
                redirectedCustomer = activeBarutCustomer;
            }

            RedirectCustomerToRelationshipManager(redirectedCustomer);
            AwardMegaGold();
            ApplyVIPRelief();
            PlaySound(triumphSound);
            OnBarutCustomerRedirected.Invoke(redirectedCustomer);

            activeBarutCustomer = null;
            redirectRoutine = null;
        }

        private void TickCustomerPriorityInput()
        {
            if (queueManager == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryPrioritizeFromScreenPoint(Input.mousePosition);
            }

            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    TryPrioritizeFromScreenPoint(touch.position);
                }
            }
        }

        private void TryPrioritizeFromScreenPoint(Vector2 screenPoint)
        {
            if (raycastCamera == null)
            {
                raycastCamera = Camera.main;
            }

            if (raycastCamera == null)
            {
                return;
            }

            var ray = raycastCamera.ScreenPointToRay(screenPoint);
            if (!Physics.Raycast(ray, out var hit, 100f) || hit.collider == null)
            {
                return;
            }

            var customer = hit.collider.GetComponentInParent<QueueCustomer>();
            if (customer == null || customer.RequestKind != CustomerRequestKind.BarutCustomer)
            {
                return;
            }

            if (!queueManager.MoveCustomerToFront(customer.gameObject))
            {
                return;
            }

            OnBarutCustomerPrioritized.Invoke(customer);
            if (queueManager.ActiveCustomer == null)
            {
                queueManager.CallNextCustomer();
            }
        }

        private void TickQueuedBarutTimeouts()
        {
            if (queueManager == null || queueManager.CustomerQueue.Count == 0)
            {
                return;
            }

            var queue = queueManager.CustomerQueue;
            for (var i = 0; i < queue.Count; i++)
            {
                var customerObject = queue[i];
                if (!TryGetBarutCustomer(customerObject, out var customer))
                {
                    continue;
                }

                if (!customerObject.TryGetComponent<CustomerPatience>(out var patience) || patience.Patience > 0f)
                {
                    continue;
                }

                OnBarutCustomerMeltdown.Invoke(customer);
                var incidentManager = CounterIncidentManager.Instance;
                if (incidentManager != null)
                {
                    incidentManager.TriggerCounterMeltdown(customerObject);
                }

                return;
            }
        }

        private void ConfigureBarutCustomer(QueueCustomer customer)
        {
            if (customer == null)
            {
                return;
            }

            customer.SetPatienceDrainMultiplier(patienceDrainMultiplier);
            ShowEmergencyIcon(customer);

            if (customer.TryGetComponent<CustomerPatience>(out var patience))
            {
                patience.SetPatience(startingPatiencePercent * 100f);
                patience.SetDrainMultiplier(patienceDrainMultiplier);
            }
        }

        private void ShowEmergencyIcon(QueueCustomer customer)
        {
            if (customer == null)
            {
                return;
            }

            if (emergencyWarningIconPrefab != null)
            {
                customer.ShowRequestIcon(emergencyWarningIconPrefab);
                return;
            }

            fallbackActiveIcon = CreateFallbackEmergencyIcon(customer.transform);
            StartCoroutine(PulseIconRoutine(fallbackActiveIcon.transform));
        }

        private GameObject CreateFallbackEmergencyIcon(Transform customer)
        {
            ClearFallbackActiveIcon();
            var icon = new GameObject("Barut Customer Emergency Flash Icon");
            icon.transform.SetParent(customer, false);
            icon.transform.localPosition = Vector3.up * 2.2f;
            icon.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var label = icon.AddComponent<TextMesh>();
            label.text = "!!";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.34f;
            label.color = Color.red;
            return icon;
        }

        private IEnumerator PulseIconRoutine(Transform icon)
        {
            while (icon != null)
            {
                var pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.2f;
                icon.localScale = Vector3.one * pulse;
                yield return null;
            }
        }

        private void RedirectCustomerToRelationshipManager(QueueCustomer customer)
        {
            if (customer == null || relationshipManagerDesk == null)
            {
                return;
            }

            customer.StopPatience();
            customer.ClearRequestIcon();
            ClearFallbackActiveIcon();

            var agent = customer.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(relationshipManagerDesk.position);
                return;
            }

            StartCoroutine(MoveCustomerToDeskRoutine(customer.transform, relationshipManagerDesk.position));
        }

        private IEnumerator MoveCustomerToDeskRoutine(Transform customer, Vector3 destination)
        {
            while (customer != null && (customer.position - destination).sqrMagnitude > 0.04f)
            {
                var current = customer.position;
                customer.position = Vector3.MoveTowards(current, destination, redirectedCustomerMoveSpeed * Time.deltaTime);

                var direction = destination - current;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    customer.rotation = Quaternion.Slerp(
                        customer.rotation,
                        Quaternion.LookRotation(direction.normalized, Vector3.up),
                        9f * Time.deltaTime);
                }

                yield return null;
            }
        }

        private void AwardMegaGold()
        {
            if (megaGoldReward <= 0)
            {
                return;
            }

            var currentGold = PlayerPrefs.GetInt(PreGameShopManager.PlayerGoldKey, 0);
            PlayerPrefs.SetInt(PreGameShopManager.PlayerGoldKey, currentGold + megaGoldReward);
            PlayerPrefs.Save();
            ShowFloatingRewardText($"+{megaGoldReward} Gold");
        }

        private void ApplyVIPRelief()
        {
            if (queueManager == null)
            {
                return;
            }

            var queue = queueManager.CustomerQueue;
            for (var i = 0; i < queue.Count; i++)
            {
                var customerObject = queue[i];
                if (customerObject == null)
                {
                    continue;
                }

                if (customerObject.TryGetComponent<QueueCustomer>(out var queueCustomer))
                {
                    queueCustomer.RestorePatiencePercent(vipReliefRestorePercent);
                }

                if (customerObject.TryGetComponent<CustomerPatience>(out var patience))
                {
                    patience.RestorePatience(vipReliefRestorePercent * 100f);
                }
            }

            PlayGlobalReliefEffect();
            OnVIPReliefApplied.Invoke();
        }

        private void PlayGlobalReliefEffect()
        {
            if (globalReliefEffectRoot != null)
            {
                globalReliefEffectRoot.SetActive(true);
                StartCoroutine(HideReliefEffectRoutine());
            }
        }

        private IEnumerator HideReliefEffectRoutine()
        {
            yield return new WaitForSeconds(1.4f);
            if (globalReliefEffectRoot != null)
            {
                globalReliefEffectRoot.SetActive(false);
            }
        }

        private void ShowFloatingRewardText(string value)
        {
            EnsureCanvas();
            if (targetCanvas == null)
            {
                return;
            }

            if (floatingRewardText != null)
            {
                Destroy(floatingRewardText);
            }

            floatingRewardText = new GameObject("Red Alert Reward Text");
            floatingRewardText.transform.SetParent(targetCanvas.transform, false);
            var text = floatingRewardText.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 36;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(1f, 0.82f, 0.16f);

            var rect = floatingRewardText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 170f);
            rect.sizeDelta = new Vector2(420f, 80f);
            Destroy(floatingRewardText, 1.6f);
        }

        private bool TryGetBarutCustomer(GameObject customerObject, out QueueCustomer customer)
        {
            customer = null;
            return customerObject != null
                && customerObject.TryGetComponent(out customer)
                && customer.RequestKind == CustomerRequestKind.BarutCustomer;
        }

        private void SetEmergencyButton(bool visible)
        {
            if (emergencyRedirectButton != null)
            {
                emergencyRedirectButton.gameObject.SetActive(visible);
            }
        }

        private void ClearFallbackActiveIcon()
        {
            if (fallbackActiveIcon != null)
            {
                Destroy(fallbackActiveIcon);
                fallbackActiveIcon = null;
            }
        }

        private void EnsureCanvas()
        {
            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void ResolveMissingReferences()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance != null ? QueueManager.Instance : FindFirstObjectByType<QueueManager>();
            }

            if (raycastCamera == null)
            {
                raycastCamera = Camera.main;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void RegisterQueueListeners()
        {
            ResolveMissingReferences();
            if (queueManager == null || queueListenersRegistered)
            {
                return;
            }

            queueManager.OnCustomerEntered.AddListener(HandleCustomerEntered);
            queueManager.OnCustomerCalled.AddListener(HandleCustomerCalled);
            queueListenersRegistered = true;
        }
    }
}
