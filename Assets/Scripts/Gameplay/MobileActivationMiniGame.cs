using System.Collections;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public class MobileActivationMiniGame : MonoBehaviour
    {
        private const int CodeLength = 4;

        [Header("Queue")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private GameObject smartphoneLockIconPrefab;

        [Header("UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Font uiFont;
        [SerializeField] private Button sendSmsButton;
        [SerializeField] private Slider smsLoadingBar;
        [SerializeField] private GameObject phoneMockupRoot;
        [SerializeField] private Text phoneSmsCodeText;
        [SerializeField] private GameObject numpadRoot;
        [SerializeField] private Transform numpadKeyContainer;
        [SerializeField] private Text typedCodeText;
        [SerializeField] private Button verifyButton;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip sendSmsSound;
        [SerializeField] private AudioClip keyTapSound;
        [SerializeField] private AudioClip wrongDigitSound;
        [SerializeField] private AudioClip successSound;

        [Header("Reward")]
        [SerializeField, Min(0f)] private float rewardTimeSeconds = 5f;
        [SerializeField, Min(0)] private int rewardScore = 90;
        [SerializeField, Min(0.1f)] private float sendSmsSeconds = 0.5f;
        [SerializeField, Min(0.1f)] private float digitalBoostSeconds = 15f;
        [SerializeField, Min(1f)] private float spawnIntervalMultiplier = 1.3f;

        public UnityEvent<string> OnActivationCodeGenerated = new UnityEvent<string>();
        public UnityEvent OnSmsSent = new UnityEvent();
        public UnityEvent OnActivationCompleted = new UnityEvent();
        public UnityEvent OnDigitalBoostStarted = new UnityEvent();
        public UnityEvent OnActivationFailedInput = new UnityEvent();

        private QueueCustomer activeCustomer;
        private GameObject fallbackIconInstance;
        private Coroutine sendSmsRoutine;
        private string activationCode = string.Empty;
        private string typedCode = string.Empty;
        private float actionTimeMultiplier = 1f;
        private bool isNumpadOpen;

        public string ActivationCode => activationCode;
        public bool IsActive => activeCustomer != null || isNumpadOpen || sendSmsRoutine != null;
        public float ActionTimeMultiplier
        {
            get => actionTimeMultiplier;
            set => actionTimeMultiplier = Mathf.Max(0.05f, value);
        }

        private void Awake()
        {
            ResolveMissingReferences();
            EnsureUi();
            SetSendSmsButton(false);
            SetPhoneAndNumpad(false);
            SetLoading(0f, false);
        }

        private void OnEnable()
        {
            RegisterQueueListener();
            if (sendSmsButton != null)
            {
                sendSmsButton.onClick.AddListener(SendSmsActivation);
            }
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

            if (sendSmsButton != null)
            {
                sendSmsButton.onClick.RemoveListener(SendSmsActivation);
            }
        }

        private void Update()
        {
            if (!isNumpadOpen)
            {
                return;
            }

            TickEditorInput();
        }

        public void SendSmsActivation()
        {
            if (activeCustomer == null || sendSmsRoutine != null)
            {
                return;
            }

            sendSmsRoutine = StartCoroutine(SendSmsRoutine());
        }

        public void PressDigit(string digit)
        {
            if (string.IsNullOrEmpty(digit))
            {
                return;
            }

            PressDigit(digit[0]);
        }

        public void VerifyCode()
        {
            if (!isNumpadOpen)
            {
                return;
            }

            if (typedCode.Length < CodeLength)
            {
                PlaySound(wrongDigitSound);
                return;
            }

            if (typedCode != activationCode)
            {
                HandleWrongInput();
                return;
            }

            CompleteActivation();
        }

        public void CancelActivation()
        {
            if (sendSmsRoutine != null)
            {
                StopCoroutine(sendSmsRoutine);
                sendSmsRoutine = null;
            }

            activeCustomer = null;
            activationCode = string.Empty;
            typedCode = string.Empty;
            isNumpadOpen = false;
            ClearFallbackIcon();
            SetSendSmsButton(false);
            SetPhoneAndNumpad(false);
            SetLoading(0f, false);
            RefreshTexts();
        }

        private void HandleCustomerCalled(GameObject customerObject)
        {
            CancelActivation();

            if (customerObject == null || !customerObject.TryGetComponent<QueueCustomer>(out var queueCustomer))
            {
                return;
            }

            if (queueCustomer.RequestKind != CustomerRequestKind.MobileActivation)
            {
                return;
            }

            activeCustomer = queueCustomer;
            if (smartphoneLockIconPrefab != null)
            {
                queueCustomer.ShowRequestIcon(smartphoneLockIconPrefab);
            }
            else
            {
                fallbackIconInstance = CreateFallbackSmartphoneIcon(queueCustomer.transform);
            }

            SetSendSmsButton(true);
        }

        private IEnumerator SendSmsRoutine()
        {
            SetSendSmsButton(false);
            PlaySound(sendSmsSound);
            SetLoading(0f, true);

            var progress = 0f;
            while (progress < 1f)
            {
                var duration = Mathf.Max(0.05f, sendSmsSeconds * actionTimeMultiplier);
                progress += Time.deltaTime / duration;
                SetLoading(progress, true);
                yield return null;
            }

            SetLoading(0f, false);
            activationCode = GenerateActivationCode();
            typedCode = string.Empty;
            isNumpadOpen = true;
            SetPhoneAndNumpad(true);
            RefreshTexts();
            OnSmsSent.Invoke();
            OnActivationCodeGenerated.Invoke(activationCode);
            sendSmsRoutine = null;
        }

        private void PressDigit(char rawDigit)
        {
            if (!isNumpadOpen || typedCode.Length >= CodeLength || rawDigit < '0' || rawDigit > '9')
            {
                return;
            }

            var expected = activationCode[typedCode.Length];
            if (rawDigit != expected)
            {
                HandleWrongInput();
                return;
            }

            typedCode += rawDigit;
            PlaySound(keyTapSound);
            RefreshTexts();
        }

        private void HandleWrongInput()
        {
            typedCode = string.Empty;
            PlaySound(wrongDigitSound);
            StartCoroutine(PhoneShakeRoutine());
            RefreshTexts();
            OnActivationFailedInput.Invoke();
        }

        private void CompleteActivation()
        {
            PlaySound(successSound);
            SetPhoneAndNumpad(false);
            SetSendSmsButton(false);
            ClearFallbackIcon();
            isNumpadOpen = false;

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.AddTime(rewardTimeSeconds);
            }

            if (ScoreManager.Instance != null && rewardScore > 0)
            {
                ScoreManager.Instance.AddScore(rewardScore);
            }

            if (queueManager != null && queueManager.ActiveCustomer == activeCustomer)
            {
                queueManager.CompleteActiveCustomer();
            }

            ApplyDigitalBoost();
            OnActivationCompleted.Invoke();
            activeCustomer = null;
        }

        private void ApplyDigitalBoost()
        {
            var appliedToRuntimeSpawner = false;
            var spawner = FindFirstObjectByType<QuestSpawner>();
            if (spawner != null)
            {
                spawner.ApplySpawnIntervalMultiplierForSeconds(spawnIntervalMultiplier, digitalBoostSeconds);
                appliedToRuntimeSpawner = true;
            }

            var poolDirector = FindFirstObjectByType<QuestPoolDirector>();
            if (poolDirector != null)
            {
                poolDirector.ApplySpawnIntervalMultiplierForSeconds(spawnIntervalMultiplier, digitalBoostSeconds);
                appliedToRuntimeSpawner = true;
            }

            if (!appliedToRuntimeSpawner)
            {
                var difficultyManager = FindFirstObjectByType<LevelDifficultyManager>();
                if (difficultyManager != null)
                {
                    difficultyManager.ApplyTemporarySpawnIntervalMultiplier(spawnIntervalMultiplier, digitalBoostSeconds);
                }
            }

            OnDigitalBoostStarted.Invoke();
        }

        private IEnumerator PhoneShakeRoutine()
        {
            if (phoneMockupRoot == null)
            {
                yield break;
            }

            var rect = phoneMockupRoot.GetComponent<RectTransform>();
            if (rect == null)
            {
                yield break;
            }

            var start = rect.anchoredPosition;
            var elapsed = 0f;
            while (elapsed < 0.22f)
            {
                elapsed += Time.deltaTime;
                rect.anchoredPosition = start + Vector2.right * (Mathf.Sin(elapsed * 80f) * 10f);
                yield return null;
            }

            rect.anchoredPosition = start;
        }

        private void TickEditorInput()
        {
            for (var i = 0; i <= 9; i++)
            {
                var key = i.ToString();
                if (Input.GetKeyDown(key))
                {
                    PressDigit(key[0]);
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                VerifyCode();
            }
        }

        private static string GenerateActivationCode()
        {
            return Random.Range(0, 10000).ToString("0000");
        }

        private void EnsureUi()
        {
            if (sendSmsButton != null && phoneMockupRoot != null && numpadRoot != null && phoneSmsCodeText != null && typedCodeText != null && verifyButton != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasObject = new GameObject("Mobile Activation Canvas");
                targetCanvas = canvasObject.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            CreateFallbackUi();
        }

        private void CreateFallbackUi()
        {
            var root = new GameObject("Mobile Activation UI");
            root.transform.SetParent(targetCanvas.transform, false);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0.5f);
            rootRect.anchorMax = new Vector2(0f, 0.5f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            rootRect.anchoredPosition = new Vector2(22f, 0f);
            rootRect.sizeDelta = new Vector2(390f, 610f);

            sendSmsButton = CreateButton("Send SMS Activation Button", root.transform, "SEND SMS", new Vector2(195f, 260f), new Vector2(250f, 62f), new Color(0.24f, 0.62f, 1f));

            var loadingObject = new GameObject("SMS Loading Bar");
            loadingObject.transform.SetParent(root.transform, false);
            smsLoadingBar = loadingObject.AddComponent<Slider>();
            var loadingRect = loadingObject.GetComponent<RectTransform>();
            loadingRect.anchorMin = new Vector2(0.5f, 0.5f);
            loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
            loadingRect.pivot = new Vector2(0.5f, 0.5f);
            loadingRect.anchoredPosition = new Vector2(195f, 210f);
            loadingRect.sizeDelta = new Vector2(250f, 28f);

            phoneMockupRoot = new GameObject("Customer Smartphone Mockup");
            phoneMockupRoot.transform.SetParent(root.transform, false);
            var phoneRect = phoneMockupRoot.AddComponent<RectTransform>();
            phoneRect.anchorMin = new Vector2(0.5f, 0.5f);
            phoneRect.anchorMax = new Vector2(0.5f, 0.5f);
            phoneRect.pivot = new Vector2(0.5f, 0.5f);
            phoneRect.anchoredPosition = new Vector2(195f, 95f);
            phoneRect.sizeDelta = new Vector2(190f, 250f);
            var phoneImage = phoneMockupRoot.AddComponent<Image>();
            phoneImage.color = new Color(0.05f, 0.07f, 0.12f, 0.96f);

            phoneSmsCodeText = CreateText("SMS Code Bubble", phoneMockupRoot.transform, string.Empty, 34, new Vector2(0f, 30f), new Vector2(150f, 82f), new Color(0.35f, 1f, 0.64f));
            typedCodeText = CreateText("Typed SMS Code", root.transform, "____", 30, new Vector2(195f, -55f), new Vector2(220f, 54f), Color.white);

            numpadRoot = new GameObject("Mobile Activation Numpad");
            numpadRoot.transform.SetParent(root.transform, false);
            var padRect = numpadRoot.AddComponent<RectTransform>();
            padRect.anchorMin = new Vector2(0.5f, 0.5f);
            padRect.anchorMax = new Vector2(0.5f, 0.5f);
            padRect.pivot = new Vector2(0.5f, 0.5f);
            padRect.anchoredPosition = new Vector2(195f, -190f);
            padRect.sizeDelta = new Vector2(250f, 210f);

            var gridObject = new GameObject("Numpad Grid");
            gridObject.transform.SetParent(numpadRoot.transform, false);
            numpadKeyContainer = gridObject.transform;
            var gridRect = gridObject.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = Vector2.zero;
            gridRect.sizeDelta = new Vector2(210f, 170f);

            var grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(58f, 42f);
            grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (var i = 1; i <= 9; i++)
            {
                CreateDigitButton(i.ToString());
            }

            CreateDigitButton("0");
            verifyButton = CreateButton("Verify SMS Button", numpadRoot.transform, "VERIFY", new Vector2(0f, -126f), new Vector2(180f, 50f), new Color(0.2f, 0.78f, 0.38f));
            verifyButton.onClick.AddListener(VerifyCode);
        }

        private void CreateDigitButton(string digit)
        {
            var button = CreateButton($"Digit {digit}", numpadKeyContainer, digit, Vector2.zero, new Vector2(58f, 42f), new Color(0.9f, 0.96f, 1f));
            button.onClick.AddListener(() => PressDigit(digit));
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
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
            text.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        private void RefreshTexts()
        {
            if (phoneSmsCodeText != null)
            {
                phoneSmsCodeText.text = string.IsNullOrEmpty(activationCode) ? string.Empty : $"SMS\n{activationCode}";
            }

            if (typedCodeText != null)
            {
                typedCodeText.text = string.IsNullOrEmpty(typedCode) ? "____" : typedCode.PadRight(CodeLength, '_');
            }

            if (verifyButton != null)
            {
                verifyButton.interactable = isNumpadOpen && typedCode == activationCode;
            }
        }

        private GameObject CreateFallbackSmartphoneIcon(Transform customer)
        {
            var icon = new GameObject("Mobile Activation Smartphone Icon");
            icon.transform.SetParent(customer, false);
            icon.transform.localPosition = Vector3.up * 2.1f;
            icon.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var label = icon.AddComponent<TextMesh>();
            label.text = "PHONE+LOCK";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.15f;
            label.color = new Color(0.34f, 0.82f, 1f);
            return icon;
        }

        private void SetSendSmsButton(bool visible)
        {
            if (sendSmsButton != null)
            {
                sendSmsButton.gameObject.SetActive(visible);
            }
        }

        private void SetPhoneAndNumpad(bool visible)
        {
            if (phoneMockupRoot != null)
            {
                phoneMockupRoot.SetActive(visible);
            }

            if (numpadRoot != null)
            {
                numpadRoot.SetActive(visible);
            }
        }

        private void SetLoading(float value, bool visible)
        {
            if (smsLoadingBar == null)
            {
                return;
            }

            smsLoadingBar.value = Mathf.Clamp01(value);
            smsLoadingBar.gameObject.SetActive(visible);
        }

        private void ClearFallbackIcon()
        {
            if (fallbackIconInstance != null)
            {
                Destroy(fallbackIconInstance);
                fallbackIconInstance = null;
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

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void RegisterQueueListener()
        {
            ResolveMissingReferences();
            if (queueManager != null)
            {
                queueManager.OnCustomerCalled.RemoveListener(HandleCustomerCalled);
                queueManager.OnCustomerCalled.AddListener(HandleCustomerCalled);
            }
        }
    }
}
