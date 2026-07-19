using System.Collections;
using System.Collections.Generic;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum DonationCategory
    {
        Animals,
        Environment,
        Children,
        Healthcare
    }

    [System.Serializable]
    public struct DonationCategoryDetails
    {
        public DonationCategory category;
        public Sprite icon;
        public Color buttonColor;
        public string displayLabel;
    }

    public class CharityDonationSystem : MonoBehaviour
    {
        private const int MaxDonationDigits = 5;

        [Header("Queue")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private GameObject heartDonationIconPrefab;

        [Header("UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Font uiFont;
        [SerializeField] private GameObject terminalRoot;
        [SerializeField] private Text requestText;
        [SerializeField] private Text amountText;
        [SerializeField] private Text statusText;
        [SerializeField] private Transform categoryButtonContainer;
        [SerializeField] private Transform numpadContainer;
        [SerializeField] private Button donateButton;

        [Header("Donation Categories")]
        [SerializeField] private DonationCategoryDetails[] categoryDetails =
        {
            new DonationCategoryDetails { category = DonationCategory.Animals, buttonColor = new Color(0.2f, 0.55f, 1f), displayLabel = "PAW" },
            new DonationCategoryDetails { category = DonationCategory.Environment, buttonColor = new Color(0.22f, 0.8f, 0.38f), displayLabel = "TREE" },
            new DonationCategoryDetails { category = DonationCategory.Children, buttonColor = new Color(1f, 0.82f, 0.18f), displayLabel = "TOY" },
            new DonationCategoryDetails { category = DonationCategory.Healthcare, buttonColor = new Color(1f, 0.24f, 0.25f), displayLabel = "CARE" }
        };

        [Header("Rewards and Penalties")]
        [SerializeField, Min(0f)] private float rewardTimeSeconds;
        [SerializeField, Min(0)] private int rewardScore = 80;
        [SerializeField, Min(0)] private int wrongCategoryScorePenalty = 15;
        [SerializeField, Min(0.1f)] private float karmaBoostSeconds = 15f;
        [SerializeField, Range(0.05f, 1f)] private float karmaPatienceDrainMultiplier = 0.6f;

        [Header("Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip correctCategorySound;
        [SerializeField] private AudioClip wrongCategorySound;
        [SerializeField] private AudioClip donationCompleteSound;
        [SerializeField] private ParticleSystem heartBurstPrefab;
        [SerializeField] private Transform heartBurstAnchor;

        public UnityEvent<DonationCategory> OnDonationRequestAssigned = new UnityEvent<DonationCategory>();
        public UnityEvent<DonationCategory> OnDonationCategorySelected = new UnityEvent<DonationCategory>();
        public UnityEvent OnDonationWrongCategory = new UnityEvent();
        public UnityEvent<int> OnDonationCompleted = new UnityEvent<int>();
        public UnityEvent OnKarmaBoostStarted = new UnityEvent();
        public UnityEvent OnKarmaBoostEnded = new UnityEvent();

        private readonly Dictionary<GameObject, GameObject> waitingFallbackIcons = new Dictionary<GameObject, GameObject>();
        private QueueCustomer activeCustomer;
        private GameObject activeSpeechBubble;
        private Coroutine karmaRoutine;
        private DonationCategory requestedCategory;
        private DonationCategory? selectedCategory;
        private string donationAmountInput = string.Empty;
        private bool terminalActive;

        public bool IsTerminalActive => terminalActive;
        public DonationCategory RequestedCategory => requestedCategory;
        public string DonationAmountInput => donationAmountInput;

        private void Awake()
        {
            ResolveMissingReferences();
            EnsureUi();
            SetTerminalVisible(false);
        }

        private void OnEnable()
        {
            RegisterQueueListeners();
            RegisterDonateButton();
        }

        private void Start()
        {
            ResolveMissingReferences();
            RegisterQueueListeners();
        }

        private void OnDisable()
        {
            if (queueManager != null)
            {
                queueManager.OnCustomerEntered.RemoveListener(HandleCustomerEntered);
                queueManager.OnCustomerCalled.RemoveListener(HandleCustomerCalled);
            }

            if (donateButton != null)
            {
                donateButton.onClick.RemoveListener(ConfirmDonation);
            }
        }

        private void Update()
        {
            if (!terminalActive)
            {
                return;
            }

            TickEditorInput();
        }

        public void OpenDonationTerminal()
        {
            if (activeCustomer == null)
            {
                return;
            }

            EnsureUi();
            terminalActive = true;
            selectedCategory = null;
            donationAmountInput = string.Empty;
            SetNumpadVisible(false);
            SetTerminalVisible(true);
            RefreshUi("Choose the matching charity category.");
        }

        public void SelectCategory(string categoryName)
        {
            if (!System.Enum.TryParse(categoryName, out DonationCategory parsedCategory))
            {
                return;
            }

            SelectCategory(parsedCategory);
        }

        public void SelectCategory(DonationCategory category)
        {
            if (!terminalActive)
            {
                return;
            }

            if (category != requestedCategory)
            {
                HandleWrongCategory();
                return;
            }

            selectedCategory = category;
            donationAmountInput = string.Empty;
            SetNumpadVisible(true);
            PlaySound(correctCategorySound);
            RefreshUi("Correct category. Enter the donation amount.");
            OnDonationCategorySelected.Invoke(category);
        }

        public void PressDigit(string digit)
        {
            if (string.IsNullOrEmpty(digit))
            {
                return;
            }

            PressDigit(digit[0]);
        }

        public void BackspaceAmount()
        {
            if (!terminalActive || donationAmountInput.Length == 0)
            {
                return;
            }

            donationAmountInput = donationAmountInput.Substring(0, donationAmountInput.Length - 1);
            RefreshUi();
        }

        public void ConfirmDonation()
        {
            if (!terminalActive || selectedCategory != requestedCategory || string.IsNullOrEmpty(donationAmountInput))
            {
                PlaySound(wrongCategorySound);
                RefreshUi("Enter an amount before donating.");
                return;
            }

            var amount = int.TryParse(donationAmountInput, out var parsedAmount)
                ? parsedAmount
                : 0;

            CompleteDonation(amount);
        }

        public void CancelDonation()
        {
            terminalActive = false;
            activeCustomer = null;
            selectedCategory = null;
            donationAmountInput = string.Empty;
            ClearSpeechBubble();
            SetTerminalVisible(false);
        }

        private void HandleCustomerEntered(GameObject customerObject)
        {
            if (!TryGetPhilanthropist(customerObject, out var customer))
            {
                return;
            }

            ApplyCalmClientSettings(customer);

            if (heartDonationIconPrefab != null)
            {
                customer.ShowRequestIcon(heartDonationIconPrefab);
                return;
            }

            waitingFallbackIcons[customerObject] = CreateFallbackHeartIcon(customer.transform, "DON");
        }

        private void HandleCustomerCalled(GameObject customerObject)
        {
            CancelDonation();
            RemoveWaitingFallbackIcon(customerObject);

            if (!TryGetPhilanthropist(customerObject, out var customer))
            {
                return;
            }

            activeCustomer = customer;
            ApplyCalmClientSettings(customer);
            requestedCategory = GetRandomDonationCategory();
            ShowDonationSpeechBubble(customer, requestedCategory);
            OpenDonationTerminal();
            OnDonationRequestAssigned.Invoke(requestedCategory);
        }

        private bool TryGetPhilanthropist(GameObject customerObject, out QueueCustomer customer)
        {
            customer = null;
            return customerObject != null
                && customerObject.TryGetComponent(out customer)
                && customer.IsPhilanthropist;
        }

        private void ApplyCalmClientSettings(QueueCustomer customer)
        {
            if (customer == null)
            {
                return;
            }

            customer.SetPatienceDrainMultiplier(0.5f);

            if (customer.TryGetComponent<CustomerPatience>(out var patience))
            {
                patience.SetPatience(100f);
                patience.SetDrainMultiplier(0.5f);
            }
        }

        private void HandleWrongCategory()
        {
            PlaySound(wrongCategorySound);
            ScoreManager.Instance?.SubtractScore(wrongCategoryScorePenalty);
            RefreshUi("Wrong category. Try again.");
            OnDonationWrongCategory.Invoke();
        }

        private void PressDigit(char rawDigit)
        {
            if (!terminalActive || selectedCategory == null || rawDigit < '0' || rawDigit > '9')
            {
                return;
            }

            if (donationAmountInput.Length >= MaxDonationDigits)
            {
                return;
            }

            if (donationAmountInput.Length == 0 && rawDigit == '0')
            {
                return;
            }

            donationAmountInput += rawDigit;
            RefreshUi();
        }

        private void CompleteDonation(int amount)
        {
            PlaySound(donationCompleteSound);
            SpawnHeartBurst();

            if (rewardTimeSeconds > 0f && TimeManager.Instance != null)
            {
                TimeManager.Instance.AddTime(rewardTimeSeconds);
            }

            if (rewardScore > 0 && ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(rewardScore);
            }

            if (queueManager != null && queueManager.ActiveCustomer == activeCustomer)
            {
                queueManager.CompleteActiveCustomer();
            }

            ApplyKarmaBoost();
            OnDonationCompleted.Invoke(amount);
            CancelDonation();
        }

        private void ApplyKarmaBoost()
        {
            if (karmaRoutine != null)
            {
                StopCoroutine(karmaRoutine);
                queueManager?.ResetQueueReliefBoost();
            }

            karmaRoutine = StartCoroutine(KarmaBoostRoutine());
        }

        private IEnumerator KarmaBoostRoutine()
        {
            queueManager?.ApplyQueueReliefBoost(0f, karmaPatienceDrainMultiplier, karmaBoostSeconds);
            OnKarmaBoostStarted.Invoke();
            yield return new WaitForSeconds(karmaBoostSeconds);
            queueManager?.ResetQueueReliefBoost();
            OnKarmaBoostEnded.Invoke();
            karmaRoutine = null;
        }

        private void TickEditorInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SelectCategory(DonationCategory.Animals);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SelectCategory(DonationCategory.Environment);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SelectCategory(DonationCategory.Children);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SelectCategory(DonationCategory.Healthcare);
                return;
            }

            for (var i = 0; i <= 9; i++)
            {
                if (Input.GetKeyDown(i.ToString()))
                {
                    PressDigit(i.ToString());
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                BackspaceAmount();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ConfirmDonation();
            }
        }

        private void RefreshUi(string status = null)
        {
            var details = GetCategoryDetails(requestedCategory);

            if (requestText != null)
            {
                requestText.text = $"Request: {details.displayLabel}";
                requestText.color = details.buttonColor;
            }

            if (amountText != null)
            {
                amountText.text = string.IsNullOrEmpty(donationAmountInput) ? "Amount: _" : $"Amount: {donationAmountInput}";
            }

            if (statusText != null && !string.IsNullOrEmpty(status))
            {
                statusText.text = status;
            }

            if (donateButton != null)
            {
                donateButton.interactable = terminalActive
                    && selectedCategory == requestedCategory
                    && !string.IsNullOrEmpty(donationAmountInput);
            }
        }

        private void ShowDonationSpeechBubble(QueueCustomer customer, DonationCategory category)
        {
            ClearSpeechBubble();
            if (customer == null)
            {
                return;
            }

            var details = GetCategoryDetails(category);
            activeSpeechBubble = CreateFallbackHeartIcon(customer.transform, details.displayLabel);
            var label = activeSpeechBubble.GetComponent<TextMesh>();
            if (label != null)
            {
                label.color = details.buttonColor;
            }
        }

        private void ClearSpeechBubble()
        {
            if (activeSpeechBubble != null)
            {
                Destroy(activeSpeechBubble);
                activeSpeechBubble = null;
            }
        }

        private GameObject CreateFallbackHeartIcon(Transform parent, string labelText)
        {
            var icon = new GameObject("Charity Donation Icon");
            icon.transform.SetParent(parent, false);
            icon.transform.localPosition = Vector3.up * 2.1f;
            icon.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

            var label = icon.AddComponent<TextMesh>();
            label.text = labelText;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.2f;
            label.color = new Color(1f, 0.24f, 0.32f);

            icon.AddComponent<PulsingDonationIcon>();
            return icon;
        }

        private void RemoveWaitingFallbackIcon(GameObject customerObject)
        {
            if (customerObject == null || !waitingFallbackIcons.TryGetValue(customerObject, out var icon))
            {
                return;
            }

            if (icon != null)
            {
                Destroy(icon);
            }

            waitingFallbackIcons.Remove(customerObject);
        }

        private DonationCategory GetRandomDonationCategory()
        {
            var count = System.Enum.GetValues(typeof(DonationCategory)).Length;
            return (DonationCategory)Random.Range(0, count);
        }

        private DonationCategoryDetails GetCategoryDetails(DonationCategory category)
        {
            if (categoryDetails != null)
            {
                for (var i = 0; i < categoryDetails.Length; i++)
                {
                    if (categoryDetails[i].category != category)
                    {
                        continue;
                    }

                    var details = categoryDetails[i];
                    if (details.buttonColor == default(Color))
                    {
                        details.buttonColor = GetFallbackColor(category);
                    }

                    if (string.IsNullOrWhiteSpace(details.displayLabel))
                    {
                        details.displayLabel = GetFallbackLabel(category);
                    }

                    return details;
                }
            }

            return new DonationCategoryDetails
            {
                category = category,
                buttonColor = GetFallbackColor(category),
                displayLabel = GetFallbackLabel(category)
            };
        }

        private static Color GetFallbackColor(DonationCategory category)
        {
            return category switch
            {
                DonationCategory.Environment => new Color(0.22f, 0.8f, 0.38f),
                DonationCategory.Children => new Color(1f, 0.82f, 0.18f),
                DonationCategory.Healthcare => new Color(1f, 0.24f, 0.25f),
                _ => new Color(0.2f, 0.55f, 1f)
            };
        }

        private static string GetFallbackLabel(DonationCategory category)
        {
            return category switch
            {
                DonationCategory.Environment => "TREE",
                DonationCategory.Children => "TOY",
                DonationCategory.Healthcare => "CARE",
                _ => "PAW"
            };
        }

        private void SpawnHeartBurst()
        {
            if (heartBurstPrefab != null)
            {
                var parent = heartBurstAnchor != null ? heartBurstAnchor : transform;
                var effect = Instantiate(heartBurstPrefab, parent.position, parent.rotation);
                effect.Play();
                Destroy(effect.gameObject, 2f);
                return;
            }

            var effectObject = new GameObject("Charity Heart Burst");
            effectObject.transform.position = heartBurstAnchor != null
                ? heartBurstAnchor.position
                : transform.position + Vector3.up * 1.2f;
            var particles = effectObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = new Color(1f, 0.36f, 0.52f);
            main.startLifetime = 0.8f;
            main.startSpeed = 1.2f;
            main.startSize = 0.12f;
            main.maxParticles = 36;
            var emission = particles.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)24) });
            particles.Play();
            Destroy(effectObject, 1.5f);
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void SetTerminalVisible(bool visible)
        {
            if (terminalRoot != null)
            {
                terminalRoot.SetActive(visible);
            }
        }

        private void SetNumpadVisible(bool visible)
        {
            if (numpadContainer != null)
            {
                numpadContainer.gameObject.SetActive(visible);
            }
        }

        private void EnsureUi()
        {
            if (terminalRoot != null
                && requestText != null
                && amountText != null
                && categoryButtonContainer != null
                && numpadContainer != null
                && donateButton != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasObject = new GameObject("Charity Donation Canvas");
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
            terminalRoot = new GameObject("Charity Donation Terminal");
            terminalRoot.transform.SetParent(targetCanvas.transform, false);

            var rootRect = terminalRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0.5f);
            rootRect.anchorMax = new Vector2(0f, 0.5f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            rootRect.anchoredPosition = new Vector2(24f, 0f);
            rootRect.sizeDelta = new Vector2(420f, 560f);

            var panel = terminalRoot.AddComponent<Image>();
            panel.color = new Color(0.98f, 0.92f, 0.88f, 0.96f);

            requestText = CreateText("Donation Request", terminalRoot.transform, "Request: PAW", 34, new Vector2(210f, 235f), new Vector2(340f, 70f), Color.red);
            statusText = CreateText("Donation Status", terminalRoot.transform, "Choose the matching charity category.", 18, new Vector2(210f, 180f), new Vector2(360f, 52f), new Color(0.22f, 0.18f, 0.14f));

            var categoryGridObject = new GameObject("Donation Category Buttons");
            categoryGridObject.transform.SetParent(terminalRoot.transform, false);
            categoryButtonContainer = categoryGridObject.transform;
            var categoryRect = categoryGridObject.AddComponent<RectTransform>();
            categoryRect.anchorMin = new Vector2(0.5f, 0.5f);
            categoryRect.anchorMax = new Vector2(0.5f, 0.5f);
            categoryRect.pivot = new Vector2(0.5f, 0.5f);
            categoryRect.anchoredPosition = new Vector2(210f, 72f);
            categoryRect.sizeDelta = new Vector2(330f, 165f);

            var categoryGrid = categoryGridObject.AddComponent<GridLayoutGroup>();
            categoryGrid.cellSize = new Vector2(150f, 68f);
            categoryGrid.spacing = new Vector2(14f, 14f);
            categoryGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            categoryGrid.constraintCount = 2;
            categoryGrid.childAlignment = TextAnchor.MiddleCenter;

            CreateCategoryButtons();

            amountText = CreateText("Donation Amount", terminalRoot.transform, "Amount: _", 28, new Vector2(210f, -40f), new Vector2(290f, 58f), new Color(0.12f, 0.22f, 0.18f));

            var numpadObject = new GameObject("Donation Numpad");
            numpadObject.transform.SetParent(terminalRoot.transform, false);
            numpadContainer = numpadObject.transform;
            var numpadRect = numpadObject.AddComponent<RectTransform>();
            numpadRect.anchorMin = new Vector2(0.5f, 0.5f);
            numpadRect.anchorMax = new Vector2(0.5f, 0.5f);
            numpadRect.pivot = new Vector2(0.5f, 0.5f);
            numpadRect.anchoredPosition = new Vector2(210f, -155f);
            numpadRect.sizeDelta = new Vector2(270f, 180f);

            var numpadGrid = numpadObject.AddComponent<GridLayoutGroup>();
            numpadGrid.cellSize = new Vector2(64f, 42f);
            numpadGrid.spacing = new Vector2(8f, 8f);
            numpadGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            numpadGrid.constraintCount = 3;
            numpadGrid.childAlignment = TextAnchor.MiddleCenter;

            for (var i = 1; i <= 9; i++)
            {
                CreateDigitButton(i.ToString());
            }

            CreateDigitButton("0");
            var backspaceButton = CreateButton("Backspace Donation Amount", numpadContainer, "DEL", Vector2.zero, new Vector2(64f, 42f), new Color(0.92f, 0.76f, 0.64f));
            backspaceButton.onClick.AddListener(BackspaceAmount);

            donateButton = CreateButton("Donate Button", terminalRoot.transform, "DONATE", new Vector2(210f, -250f), new Vector2(220f, 58f), new Color(0.28f, 0.82f, 0.38f));
            donateButton.onClick.AddListener(ConfirmDonation);
        }

        private void CreateCategoryButtons()
        {
            for (var i = 0; i < categoryDetails.Length; i++)
            {
                var details = GetCategoryDetails(categoryDetails[i].category);
                var button = CreateButton(
                    $"{details.category} Donation Button",
                    categoryButtonContainer,
                    details.displayLabel,
                    Vector2.zero,
                    new Vector2(150f, 68f),
                    details.buttonColor);
                var category = details.category;
                button.onClick.AddListener(() => SelectCategory(category));
            }
        }

        private void CreateDigitButton(string digit)
        {
            var button = CreateButton($"Donation Digit {digit}", numpadContainer, digit, Vector2.zero, new Vector2(64f, 42f), new Color(0.9f, 0.96f, 1f));
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

        private void RegisterQueueListeners()
        {
            ResolveMissingReferences();
            if (queueManager == null)
            {
                return;
            }

            queueManager.OnCustomerEntered.RemoveListener(HandleCustomerEntered);
            queueManager.OnCustomerEntered.AddListener(HandleCustomerEntered);
            queueManager.OnCustomerCalled.RemoveListener(HandleCustomerCalled);
            queueManager.OnCustomerCalled.AddListener(HandleCustomerCalled);
        }

        private void RegisterDonateButton()
        {
            if (donateButton == null)
            {
                return;
            }

            donateButton.onClick.RemoveListener(ConfirmDonation);
            donateButton.onClick.AddListener(ConfirmDonation);
        }

        private class PulsingDonationIcon : MonoBehaviour
        {
            private Vector3 baseScale;
            private float phase;

            private void Awake()
            {
                baseScale = transform.localScale;
                phase = Random.Range(0f, 6.28f);
            }

            private void Update()
            {
                var scale = 1f + Mathf.Sin(Time.time * 4.5f + phase) * 0.08f;
                transform.localScale = baseScale * scale;
            }
        }
    }
}
