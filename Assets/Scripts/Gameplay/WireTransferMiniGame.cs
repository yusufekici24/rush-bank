using System.Collections;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public class WireTransferMiniGame : MonoBehaviour
    {
        private const string KeyboardCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        [Header("Queue")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private GameObject moneyArrowIconPrefab;

        [Header("UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Font handwrittenFont;
        [SerializeField] private GameObject miniGameRoot;
        [SerializeField] private Text codeText;
        [SerializeField] private Text typedText;
        [SerializeField] private Transform keyContainer;
        [SerializeField] private Button keyButtonPrefab;
        [SerializeField] private Button sendButton;
        [SerializeField] private GameObject perfectTransferEffectRoot;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip keyClackSound;
        [SerializeField] private AudioClip wrongKeySound;
        [SerializeField] private AudioClip transferCompleteSound;

        [Header("Rewards")]
        [SerializeField, Min(0f)] private float rewardTimeSeconds = 7f;
        [SerializeField, Min(0)] private int rewardScore = 170;
        [SerializeField, Min(0.1f)] private float perfectWindowSeconds = 5f;
        [SerializeField, Min(1f)] private float perfectGoldMultiplier = 1.2f;
        [SerializeField, Min(0.1f)] private float perfectBoostSeconds = 15f;

        public UnityEvent<string> OnTransferStarted = new UnityEvent<string>();
        public UnityEvent OnTransferCompleted = new UnityEvent();
        public UnityEvent OnTransferFailedInput = new UnityEvent();
        public UnityEvent OnPerfectTransferBoostStarted = new UnityEvent();

        private QueueCustomer activeCustomer;
        private GameObject fallbackIconInstance;
        private GameObject floatingPerfectText;
        private string transferCode = string.Empty;
        private string typedInput = string.Empty;
        private float elapsedSeconds;
        private int typoCount;
        private bool isActive;
        private float actionTimeMultiplier = 1f;

        public string TransferCode => transferCode;
        public bool IsActive => isActive;
        public float ActionTimeMultiplier
        {
            get => actionTimeMultiplier;
            set => actionTimeMultiplier = Mathf.Max(0.05f, value);
        }

        private void Awake()
        {
            ResolveMissingReferences();
            SetMiniGameVisible(false);
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
            if (queueManager != null)
            {
                queueManager.OnCustomerCalled.RemoveListener(HandleCustomerCalled);
            }
        }

        private void Update()
        {
            if (!isActive)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime * actionTimeMultiplier;
            TickEditorKeyboard();
        }

        public void PressKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            PressKey(key[0]);
        }

        public void PressSend()
        {
            if (!isActive)
            {
                return;
            }

            if (typedInput.Length < transferCode.Length)
            {
                PlaySound(wrongKeySound);
                return;
            }

            if (typedInput != transferCode)
            {
                HandleWrongInput();
                return;
            }

            CompleteTransfer();
        }

        public void CancelTransfer()
        {
            if (!isActive)
            {
                return;
            }

            typedInput = string.Empty;
            isActive = false;
            SetMiniGameVisible(false);
            ClearFallbackIcon();
            activeCustomer = null;
            RefreshTexts();
        }

        private void HandleCustomerCalled(GameObject customerObject)
        {
            ClearFallbackIcon();
            activeCustomer = null;

            if (customerObject == null || !customerObject.TryGetComponent<QueueCustomer>(out var queueCustomer))
            {
                return;
            }

            if (queueCustomer.RequestKind != CustomerRequestKind.WireTransfer)
            {
                SetMiniGameVisible(false);
                isActive = false;
                return;
            }

            activeCustomer = queueCustomer;
            if (moneyArrowIconPrefab != null)
            {
                queueCustomer.ShowRequestIcon(moneyArrowIconPrefab);
            }
            else
            {
                fallbackIconInstance = CreateFallbackMoneyArrowIcon(queueCustomer.transform);
            }

            StartTransfer();
        }

        private void StartTransfer()
        {
            EnsureUi();
            transferCode = GenerateTransferCode();
            typedInput = string.Empty;
            typoCount = 0;
            elapsedSeconds = 0f;
            isActive = true;
            RefreshTexts();
            SetMiniGameVisible(true);
            OnTransferStarted.Invoke(transferCode);
        }

        private void PressKey(char rawCharacter)
        {
            if (!isActive || typedInput.Length >= transferCode.Length)
            {
                return;
            }

            var character = char.ToUpperInvariant(rawCharacter);
            var expected = transferCode[typedInput.Length];
            if (character != expected)
            {
                HandleWrongInput();
                return;
            }

            typedInput += character;
            PlaySound(keyClackSound);
            RefreshTexts();
        }

        private void HandleWrongInput()
        {
            typoCount++;
            typedInput = string.Empty;
            PlaySound(wrongKeySound);
            RefreshTexts();
            OnTransferFailedInput.Invoke();
        }

        private void CompleteTransfer()
        {
            PlaySound(transferCompleteSound);

            var perfectTransfer = typoCount == 0 && elapsedSeconds <= perfectWindowSeconds;
            SetMiniGameVisible(false);
            isActive = false;
            ClearFallbackIcon();

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

            OnTransferCompleted.Invoke();

            if (perfectTransfer)
            {
                ApplyPerfectTransferBoost();
            }

            activeCustomer = null;
        }

        private void ApplyPerfectTransferBoost()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ApplyGoldMultiplierForSeconds(perfectGoldMultiplier, perfectBoostSeconds);
            }

            StartCoroutine(PerfectTransferFeedbackRoutine());
            OnPerfectTransferBoostStarted.Invoke();
        }

        private IEnumerator PerfectTransferFeedbackRoutine()
        {
            SetPerfectEffect(true);
            ShowFloatingPerfectText();
            yield return new WaitForSeconds(1.4f);
            SetPerfectEffect(false);
            HideFloatingPerfectText();
        }

        private static string GenerateTransferCode()
        {
            var length = Random.Range(4, 6);
            var characters = new char[length];
            for (var i = 0; i < characters.Length; i++)
            {
                characters[i] = KeyboardCharacters[Random.Range(0, KeyboardCharacters.Length)];
            }

            return new string(characters);
        }

        private void TickEditorKeyboard()
        {
            for (var i = 0; i < KeyboardCharacters.Length; i++)
            {
                if (Input.GetKeyDown(KeyboardCharacters[i].ToString().ToLowerInvariant()))
                {
                    PressKey(KeyboardCharacters[i]);
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                PressSend();
            }
        }

        private void RefreshTexts()
        {
            if (codeText != null)
            {
                codeText.text = transferCode;
            }

            if (typedText != null)
            {
                typedText.text = string.IsNullOrEmpty(typedInput)
                    ? new string('_', transferCode.Length)
                    : typedInput.PadRight(transferCode.Length, '_');
            }

            if (sendButton != null)
            {
                sendButton.interactable = isActive && typedInput == transferCode;
            }
        }

        private void EnsureUi()
        {
            if (miniGameRoot != null && codeText != null && typedText != null && keyContainer != null && sendButton != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasObject = new GameObject("Wire Transfer Canvas");
                targetCanvas = canvasObject.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            if (handwrittenFont == null)
            {
                handwrittenFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            CreateFallbackUi();
        }

        private void CreateFallbackUi()
        {
            miniGameRoot = new GameObject("Wire Transfer MiniGame UI");
            miniGameRoot.transform.SetParent(targetCanvas.transform, false);

            var rootRect = miniGameRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(1f, 0.5f);
            rootRect.anchoredPosition = new Vector2(-24f, 0f);
            rootRect.sizeDelta = new Vector2(430f, 620f);

            var panel = miniGameRoot.AddComponent<Image>();
            panel.color = new Color(0.98f, 0.9f, 0.68f, 0.96f);

            codeText = CreateText("Transfer Code", miniGameRoot.transform, string.Empty, 44, new Vector2(0f, 245f), new Vector2(360f, 90f), new Color(0.22f, 0.18f, 0.12f));
            typedText = CreateText("Typed Code", miniGameRoot.transform, string.Empty, 34, new Vector2(0f, 165f), new Vector2(360f, 70f), new Color(0.18f, 0.36f, 0.32f));

            var keysObject = new GameObject("Keyboard Grid");
            keysObject.transform.SetParent(miniGameRoot.transform, false);
            keyContainer = keysObject.transform;
            var keysRect = keysObject.AddComponent<RectTransform>();
            keysRect.anchorMin = new Vector2(0.5f, 0.5f);
            keysRect.anchorMax = new Vector2(0.5f, 0.5f);
            keysRect.pivot = new Vector2(0.5f, 0.5f);
            keysRect.anchoredPosition = new Vector2(0f, -65f);
            keysRect.sizeDelta = new Vector2(360f, 350f);

            var grid = keysObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(46f, 42f);
            grid.spacing = new Vector2(6f, 6f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (var i = 0; i < KeyboardCharacters.Length; i++)
            {
                CreateKeyButton(KeyboardCharacters[i]);
            }

            sendButton = CreateButton("Send Button", miniGameRoot.transform, "SEND", new Vector2(0f, -275f), new Vector2(250f, 66f), new Color(0.2f, 0.78f, 0.38f));
            sendButton.onClick.AddListener(PressSend);
        }

        private void CreateKeyButton(char character)
        {
            Button button;
            if (keyButtonPrefab != null)
            {
                button = Instantiate(keyButtonPrefab, keyContainer);
                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = character.ToString();
                }
            }
            else
            {
                button = CreateButton($"Key {character}", keyContainer, character.ToString(), Vector2.zero, new Vector2(46f, 42f), new Color(0.84f, 0.93f, 1f));
            }

            var capturedCharacter = character;
            button.onClick.AddListener(() => PressKey(capturedCharacter));
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
            text.font = handwrittenFont != null ? handwrittenFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        private GameObject CreateFallbackMoneyArrowIcon(Transform customer)
        {
            var icon = new GameObject("Wire Transfer Money Arrow Icon");
            icon.transform.SetParent(customer, false);
            icon.transform.localPosition = Vector3.up * 2.1f;
            icon.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

            var label = icon.AddComponent<TextMesh>();
            label.text = "$ ->";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.22f;
            label.color = new Color(0.28f, 0.95f, 0.55f);
            return icon;
        }

        private void SetMiniGameVisible(bool visible)
        {
            if (miniGameRoot != null)
            {
                miniGameRoot.SetActive(visible);
            }
        }

        private void SetPerfectEffect(bool active)
        {
            if (perfectTransferEffectRoot != null)
            {
                perfectTransferEffectRoot.SetActive(active);
            }
        }

        private void ShowFloatingPerfectText()
        {
            if (targetCanvas == null)
            {
                return;
            }

            HideFloatingPerfectText();
            floatingPerfectText = new GameObject("Perfect Transfer Floating Text");
            floatingPerfectText.transform.SetParent(targetCanvas.transform, false);
            var text = floatingPerfectText.AddComponent<Text>();
            text.font = handwrittenFont != null ? handwrittenFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "Perfect Transfer! +20% Gold!";
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 30;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(1f, 0.88f, 0.22f);

            var rect = floatingPerfectText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 160f);
            rect.sizeDelta = new Vector2(620f, 80f);
        }

        private void HideFloatingPerfectText()
        {
            if (floatingPerfectText != null)
            {
                Destroy(floatingPerfectText);
                floatingPerfectText = null;
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void ClearFallbackIcon()
        {
            if (fallbackIconInstance != null)
            {
                Destroy(fallbackIconInstance);
                fallbackIconInstance = null;
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
