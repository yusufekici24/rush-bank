using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum CreditType
    {
        Housing,
        Vehicle,
        Consumer
    }

    public class CreditApplicationSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private Transform creditSpecialistDesk;
        [SerializeField] private StationeryDeliverySystem stationeryDeliverySystem;
        [SerializeField] private GameObject housingIconPrefab;
        [SerializeField] private GameObject vehicleIconPrefab;
        [SerializeField] private GameObject consumerIconPrefab;

        [Header("Terminal UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GameObject terminalRoot;
        [SerializeField] private Button checkCreditScoreButton;
        [SerializeField] private Button rejectApplicationButton;
        [SerializeField] private Button referToCreditSpecialistButton;
        [SerializeField] private Slider queryProgressBar;
        [SerializeField] private Text resultText;
        [SerializeField] private Text creditTypeText;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip queryProcessingSound;
        [SerializeField] private AudioClip deniedSound;
        [SerializeField] private AudioClip cashRegisterSound;

        [Header("Tuning")]
        [SerializeField, Min(0.05f)] private float querySeconds = 0.8f;
        [SerializeField, Range(0f, 1f)] private float approvalChance = 0.8f;
        [SerializeField, Min(0.1f)] private float redirectedCustomerMoveSpeed = 2.6f;
        [SerializeField, Min(0.1f)] private float creditBoostSeconds = 15f;
        [SerializeField, Min(1f)] private float creditBoostRedirectSpeedMultiplier = 1.3f;

        [Header("Rewards")]
        [SerializeField, Min(0)] private int deniedRewardGold = 30;
        [SerializeField, Min(0)] private int housingRewardGold = 120;
        [SerializeField, Min(0)] private int vehicleRewardGold = 80;
        [SerializeField, Min(0)] private int consumerRewardGold = 50;

        public UnityEvent<CreditType> OnCreditApplicationReady = new UnityEvent<CreditType>();
        public UnityEvent<CreditType, bool> OnCreditScoreChecked = new UnityEvent<CreditType, bool>();
        public UnityEvent<CreditType> OnCreditApplicationRejected = new UnityEvent<CreditType>();
        public UnityEvent<CreditType> OnCreditApplicationReferred = new UnityEvent<CreditType>();
        public UnityEvent OnCreditBoostStarted = new UnityEvent();
        public UnityEvent OnCreditBoostEnded = new UnityEvent();

        private QueueCustomer activeCreditCustomer;
        private GameObject fallbackIconInstance;
        private Coroutine queryRoutine;
        private Coroutine creditBoostRoutine;
        private CreditType activeCreditType;
        private bool queryCompleted;
        private bool queryApproved;
        private float actionTimeMultiplier = 1f;
        private float previousGlobalRedirectMultiplier = 1f;

        public bool HasActiveCreditCustomer => activeCreditCustomer != null;
        public CreditType ActiveCreditType => activeCreditType;
        public bool IsQueryApproved => queryCompleted && queryApproved;
        public float ActionTimeMultiplier
        {
            get => actionTimeMultiplier;
            set => actionTimeMultiplier = Mathf.Max(0.05f, value);
        }

        private void Awake()
        {
            ResolveMissingReferences();
            EnsureUi();
            SetTerminalVisible(false);
            SetQueryProgress(0f, false);
        }

        private void OnEnable()
        {
            RegisterQueueListener();
            RegisterButtons();
        }

        private void Start()
        {
            ResolveMissingReferences();
            RegisterQueueListener();
        }

        private void OnDisable()
        {
            if (queueManager != null)
            {
                queueManager.OnCustomerCalled.RemoveListener(HandleCustomerCalled);
            }

            UnregisterButtons();
        }

        public void CheckCreditScore()
        {
            if (activeCreditCustomer == null || queryRoutine != null)
            {
                return;
            }

            queryRoutine = StartCoroutine(CheckCreditScoreRoutine());
        }

        public void RejectApplication()
        {
            if (activeCreditCustomer == null || !queryCompleted || queryApproved)
            {
                return;
            }

            PlaySound(deniedSound);
            AddGold(deniedRewardGold);
            PlaySadAnimation(activeCreditCustomer);
            queueManager?.CompleteActiveCustomer();
            OnCreditApplicationRejected.Invoke(activeCreditType);
            ClearActiveState();
        }

        public void ReferToCreditSpecialist()
        {
            if (activeCreditCustomer == null || !queryCompleted || !queryApproved)
            {
                return;
            }

            var redirectedCustomer = queueManager != null
                ? queueManager.ReleaseActiveCustomerForRedirect()
                : activeCreditCustomer;

            if (redirectedCustomer == null)
            {
                redirectedCustomer = activeCreditCustomer;
            }

            var speedMultiplier = stationeryDeliverySystem != null
                ? stationeryDeliverySystem.ConsumeRedirectSpeedMultiplier(creditSpecialistDesk)
                : 1f;

            RedirectCustomerToCreditSpecialist(redirectedCustomer, speedMultiplier);
            AddGold(GetApprovedRewardGold(activeCreditType));
            PlaySound(cashRegisterSound);
            ApplyCreditBoost();
            OnCreditApplicationReferred.Invoke(activeCreditType);
            ClearActiveState();
        }

        public void CancelCreditApplication()
        {
            if (queryRoutine != null)
            {
                StopCoroutine(queryRoutine);
                queryRoutine = null;
            }

            ClearActiveState();
        }

        private void HandleCustomerCalled(GameObject customerObject)
        {
            CancelCreditApplication();

            if (customerObject == null || !customerObject.TryGetComponent<QueueCustomer>(out var customer))
            {
                return;
            }

            if (customer.RequestKind != CustomerRequestKind.CreditApproval)
            {
                return;
            }

            activeCreditCustomer = customer;
            activeCreditType = GetRandomCreditType();
            queryCompleted = false;
            queryApproved = false;
            ShowCreditIcon(customer, activeCreditType);
            EnsureUi();
            SetTerminalVisible(true);
            RefreshTerminal("Ready");
            OnCreditApplicationReady.Invoke(activeCreditType);
        }

        private IEnumerator CheckCreditScoreRoutine()
        {
            SetQueryProgress(0f, true);
            SetButtons(false, false, false);
            PlaySound(queryProcessingSound);

            var progress = 0f;
            while (progress < 1f)
            {
                progress += Time.deltaTime / Mathf.Max(0.05f, querySeconds * actionTimeMultiplier);
                SetQueryProgress(progress, true);
                yield return null;
            }

            queryCompleted = true;
            queryApproved = Random.value <= approvalChance;
            SetQueryProgress(0f, false);
            RefreshTerminal(queryApproved ? "APPROVED" : "DENIED");
            SetButtons(false, !queryApproved, queryApproved);
            OnCreditScoreChecked.Invoke(activeCreditType, queryApproved);
            queryRoutine = null;
        }

        private void RedirectCustomerToCreditSpecialist(QueueCustomer customer, float speedMultiplier)
        {
            if (customer == null || creditSpecialistDesk == null)
            {
                return;
            }

            customer.StopPatience();
            customer.ClearRequestIcon();

            var agent = customer.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                StartCoroutine(MoveAgentToDeskRoutine(agent, creditSpecialistDesk.position, speedMultiplier));
                return;
            }

            StartCoroutine(MoveCustomerToDeskRoutine(customer.transform, creditSpecialistDesk.position, speedMultiplier));
        }

        private IEnumerator MoveAgentToDeskRoutine(NavMeshAgent agent, Vector3 destination, float speedMultiplier)
        {
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

        private void ApplyCreditBoost()
        {
            if (creditBoostRoutine != null)
            {
                StopCoroutine(creditBoostRoutine);
                RestoreCreditBoost();
            }

            creditBoostRoutine = StartCoroutine(CreditBoostRoutine());
        }

        private IEnumerator CreditBoostRoutine()
        {
            if (stationeryDeliverySystem != null)
            {
                previousGlobalRedirectMultiplier = stationeryDeliverySystem.GlobalRedirectSpeedMultiplier;
                stationeryDeliverySystem.GlobalRedirectSpeedMultiplier = previousGlobalRedirectMultiplier * creditBoostRedirectSpeedMultiplier;
            }

            OnCreditBoostStarted.Invoke();
            yield return new WaitForSeconds(creditBoostSeconds);
            RestoreCreditBoost();
            OnCreditBoostEnded.Invoke();
            creditBoostRoutine = null;
        }

        private void RestoreCreditBoost()
        {
            if (stationeryDeliverySystem != null)
            {
                stationeryDeliverySystem.GlobalRedirectSpeedMultiplier = previousGlobalRedirectMultiplier;
            }
        }

        private void ShowCreditIcon(QueueCustomer customer, CreditType creditType)
        {
            var prefab = creditType switch
            {
                CreditType.Housing => housingIconPrefab,
                CreditType.Vehicle => vehicleIconPrefab,
                _ => consumerIconPrefab
            };

            if (prefab != null)
            {
                customer.ShowRequestIcon(prefab);
                return;
            }

            fallbackIconInstance = CreateFallbackCreditIcon(customer.transform, creditType);
        }

        private GameObject CreateFallbackCreditIcon(Transform customer, CreditType creditType)
        {
            ClearFallbackIcon();
            var icon = new GameObject($"{creditType} Credit Icon");
            icon.transform.SetParent(customer, false);
            icon.transform.localPosition = Vector3.up * 2.1f;
            icon.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var label = icon.AddComponent<TextMesh>();
            label.text = GetCreditShortLabel(creditType);
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.2f;
            label.color = GetCreditColor(creditType);
            return icon;
        }

        private void PlaySadAnimation(QueueCustomer customer)
        {
            var animator = customer != null ? customer.GetComponentInChildren<Animator>() : null;
            if (animator != null)
            {
                animator.SetTrigger("Sad");
            }
        }

        private void ClearActiveState()
        {
            SetTerminalVisible(false);
            SetQueryProgress(0f, false);
            ClearFallbackIcon();
            activeCreditCustomer = null;
            queryCompleted = false;
            queryApproved = false;
        }

        private void ClearFallbackIcon()
        {
            if (fallbackIconInstance != null)
            {
                Destroy(fallbackIconInstance);
                fallbackIconInstance = null;
            }
        }

        private CreditType GetRandomCreditType()
        {
            return (CreditType)Random.Range(0, 3);
        }

        private int GetApprovedRewardGold(CreditType creditType)
        {
            return creditType switch
            {
                CreditType.Housing => housingRewardGold,
                CreditType.Vehicle => vehicleRewardGold,
                _ => consumerRewardGold
            };
        }

        private static string GetCreditShortLabel(CreditType creditType)
        {
            return creditType switch
            {
                CreditType.Housing => "HOME",
                CreditType.Vehicle => "CAR",
                _ => "BAG"
            };
        }

        private static Color GetCreditColor(CreditType creditType)
        {
            return creditType switch
            {
                CreditType.Housing => new Color(0.25f, 0.85f, 0.48f),
                CreditType.Vehicle => new Color(0.28f, 0.64f, 1f),
                _ => new Color(1f, 0.76f, 0.22f)
            };
        }

        private void RefreshTerminal(string result)
        {
            if (creditTypeText != null)
            {
                creditTypeText.text = $"{GetCreditShortLabel(activeCreditType)} CREDIT";
                creditTypeText.color = GetCreditColor(activeCreditType);
            }

            if (resultText != null)
            {
                resultText.text = result;
                resultText.color = result == "APPROVED"
                    ? Color.green
                    : result == "DENIED"
                        ? Color.red
                        : Color.white;
            }

            SetButtons(!queryCompleted && queryRoutine == null, queryCompleted && !queryApproved, queryCompleted && queryApproved);
        }

        private void SetButtons(bool checkVisible, bool rejectVisible, bool referVisible)
        {
            if (checkCreditScoreButton != null)
            {
                checkCreditScoreButton.gameObject.SetActive(checkVisible);
            }

            if (rejectApplicationButton != null)
            {
                rejectApplicationButton.gameObject.SetActive(rejectVisible);
            }

            if (referToCreditSpecialistButton != null)
            {
                referToCreditSpecialistButton.gameObject.SetActive(referVisible);
            }
        }

        private void SetQueryProgress(float value, bool visible)
        {
            if (queryProgressBar == null)
            {
                return;
            }

            queryProgressBar.gameObject.SetActive(visible);
            queryProgressBar.value = Mathf.Clamp01(value);
        }

        private void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var currentGold = PlayerPrefs.GetInt(PreGameShopManager.PlayerGoldKey, 0);
            PlayerPrefs.SetInt(PreGameShopManager.PlayerGoldKey, currentGold + amount);
            PlayerPrefs.Save();
        }

        private void EnsureUi()
        {
            if (terminalRoot != null && checkCreditScoreButton != null && rejectApplicationButton != null && referToCreditSpecialistButton != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasObject = new GameObject("Credit Application Canvas");
                targetCanvas = canvasObject.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            CreateFallbackUi();
        }

        private void CreateFallbackUi()
        {
            terminalRoot = new GameObject("Credit Application Terminal");
            terminalRoot.transform.SetParent(targetCanvas.transform, false);

            var rootRect = terminalRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(1f, 0.5f);
            rootRect.anchoredPosition = new Vector2(-24f, 0f);
            rootRect.sizeDelta = new Vector2(390f, 360f);

            var panel = terminalRoot.AddComponent<Image>();
            panel.color = new Color(0.08f, 0.11f, 0.15f, 0.96f);

            creditTypeText = CreateText("Credit Type", terminalRoot.transform, "HOME CREDIT", 30, new Vector2(0f, 125f), new Vector2(330f, 58f), Color.white);
            resultText = CreateText("Credit Result", terminalRoot.transform, "Ready", 26, new Vector2(0f, 70f), new Vector2(330f, 52f), Color.white);

            var progressObject = new GameObject("Credit Query Progress");
            progressObject.transform.SetParent(terminalRoot.transform, false);
            queryProgressBar = progressObject.AddComponent<Slider>();
            var progressRect = progressObject.GetComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0.5f, 0.5f);
            progressRect.anchorMax = new Vector2(0.5f, 0.5f);
            progressRect.pivot = new Vector2(0.5f, 0.5f);
            progressRect.anchoredPosition = new Vector2(0f, 18f);
            progressRect.sizeDelta = new Vector2(260f, 28f);

            checkCreditScoreButton = CreateButton("Check Credit Score Button", "CHECK SCORE", new Vector2(0f, -50f), new Vector2(240f, 54f), new Color(0.32f, 0.68f, 1f));
            rejectApplicationButton = CreateButton("Reject Credit Button", "REJECT", new Vector2(0f, -50f), new Vector2(210f, 54f), new Color(1f, 0.32f, 0.28f));
            referToCreditSpecialistButton = CreateButton("Refer Credit Specialist Button", "REFER", new Vector2(0f, -50f), new Vector2(210f, 54f), new Color(0.24f, 0.82f, 0.38f));
        }

        private Button CreateButton(string name, string label, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(terminalRoot.transform, false);
            var image = buttonObject.AddComponent<Image>();
            image.color = color;
            var button = buttonObject.AddComponent<Button>();

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            CreateText("Label", buttonObject.transform, label, 18, Vector2.zero, size, Color.black);
            return button;
        }

        private Text CreateText(string name, Transform parent, string value, int fontSize, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = color;

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return text;
        }

        private void SetTerminalVisible(bool visible)
        {
            if (terminalRoot != null)
            {
                terminalRoot.SetActive(visible);
            }
        }

        private void RegisterButtons()
        {
            if (checkCreditScoreButton != null)
            {
                checkCreditScoreButton.onClick.AddListener(CheckCreditScore);
            }

            if (rejectApplicationButton != null)
            {
                rejectApplicationButton.onClick.AddListener(RejectApplication);
            }

            if (referToCreditSpecialistButton != null)
            {
                referToCreditSpecialistButton.onClick.AddListener(ReferToCreditSpecialist);
            }
        }

        private void UnregisterButtons()
        {
            if (checkCreditScoreButton != null)
            {
                checkCreditScoreButton.onClick.RemoveListener(CheckCreditScore);
            }

            if (rejectApplicationButton != null)
            {
                rejectApplicationButton.onClick.RemoveListener(RejectApplication);
            }

            if (referToCreditSpecialistButton != null)
            {
                referToCreditSpecialistButton.onClick.RemoveListener(ReferToCreditSpecialist);
            }
        }

        private void RegisterQueueListener()
        {
            ResolveMissingReferences();
            if (queueManager == null)
            {
                return;
            }

            queueManager.OnCustomerCalled.RemoveListener(HandleCustomerCalled);
            queueManager.OnCustomerCalled.AddListener(HandleCustomerCalled);
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

            if (stationeryDeliverySystem == null)
            {
                stationeryDeliverySystem = StationeryDeliverySystem.Instance != null
                    ? StationeryDeliverySystem.Instance
                    : FindFirstObjectByType<StationeryDeliverySystem>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }
    }
}
