using RushBank.Core;
using RushBank.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RushBank.UI
{
    public class PreLevelPopupController : MonoBehaviour
    {
        [System.Serializable]
        public class BoosterCard
        {
            public string boosterType;
            public int cost = 50;
            public Button cardButton;
            public Button buyAndEquipButton;
            public GameObject checkmark;
            public TMP_Text ownedQuantityText;
            public TMP_Text costText;
            public TMP_Text actionText;
        }

        [Header("References")]
        [SerializeField] private PreGameShopManager shopManager;
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private RectTransform popupRoot;
        [SerializeField] private TMP_Text playerGoldText;
        [SerializeField] private Button startLevelButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button buyBundleButton;
        [SerializeField] private TMP_Text bundleActionText;
        [SerializeField] private ParticleSystem bundleConfettiParticles;
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip cashRegisterSound;

        [Header("Booster Cards")]
        [SerializeField] private BoosterCard timeSlowCard = new BoosterCard { boosterType = "TimeSlow", cost = 50 };
        [SerializeField] private BoosterCard speedCard = new BoosterCard { boosterType = "Speed", cost = 50 };
        [SerializeField] private BoosterCard patienceCard = new BoosterCard { boosterType = "Patience", cost = 50 };

        [Header("Bundle Offer")]
        [SerializeField, Min(0)] private int singleCost = 50;
        [SerializeField, Min(0)] private int bundleCost = 120;

        [Header("Level Loading")]
        [SerializeField] private int selectedLevelBuildIndex = (int)SceneId.Game;

        [Header("Animation")]
        [SerializeField, Min(0.01f)] private float openAnimationSeconds = 0.26f;
        [SerializeField, Range(1f, 2f)] private float overshootScale = 1.08f;

        private Coroutine openAnimationRoutine;

        private void Awake()
        {
            ResolveMissingReferences();
            SetPopupVisible(false);
        }

        private void OnEnable()
        {
            RegisterCard(timeSlowCard, HandleTimeSlowClicked);
            RegisterCard(speedCard, HandleSpeedClicked);
            RegisterCard(patienceCard, HandlePatienceClicked);

            if (startLevelButton != null)
            {
                startLevelButton.onClick.AddListener(StartSelectedLevel);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePopup);
            }

            if (buyBundleButton != null)
            {
                buyBundleButton.onClick.AddListener(HandleBundleClicked);
            }
        }

        private void OnDisable()
        {
            UnregisterCard(timeSlowCard, HandleTimeSlowClicked);
            UnregisterCard(speedCard, HandleSpeedClicked);
            UnregisterCard(patienceCard, HandlePatienceClicked);

            if (startLevelButton != null)
            {
                startLevelButton.onClick.RemoveListener(StartSelectedLevel);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePopup);
            }

            if (buyBundleButton != null)
            {
                buyBundleButton.onClick.RemoveListener(HandleBundleClicked);
            }
        }

        public void OnPlayPressed()
        {
            OpenForLevel(selectedLevelBuildIndex);
        }

        public void OpenForLevel(int levelBuildIndex)
        {
            selectedLevelBuildIndex = levelBuildIndex;
            ResolveMissingReferences();
            RefreshPopup();
            SetPopupVisible(true);
            PlayOpenAnimation();
        }

        public void ClosePopup()
        {
            SetPopupVisible(false);
        }

        public void RefreshPopup()
        {
            if (shopManager == null)
            {
                return;
            }

            if (playerGoldText != null)
            {
                playerGoldText.text = shopManager.PlayerGold.ToString();
            }

            RefreshCard(timeSlowCard);
            RefreshCard(speedCard);
            RefreshCard(patienceCard);
            RefreshBundle();
        }

        public void StartSelectedLevel()
        {
            if (shopManager == null)
            {
                ResolveMissingReferences();
            }

            var timeEquipped = shopManager != null && shopManager.IsBoosterEquipped("TimeSlow");
            var speedEquipped = shopManager != null && shopManager.IsBoosterEquipped("Speed");
            var patienceEquipped = shopManager != null && shopManager.IsBoosterEquipped("Patience");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetSelectedLevel(selectedLevelBuildIndex);
                GameManager.Instance.SetPendingPreRunBoosters(timeEquipped, speedEquipped, patienceEquipped);
                GameManager.Instance.SetState(GameState.InGame);
            }

            SceneManager.LoadScene(selectedLevelBuildIndex);
        }

        private void HandleBoosterCardClicked(BoosterCard card)
        {
            if (shopManager == null || card == null)
            {
                return;
            }

            var quantity = shopManager.GetBoosterQuantity(card.boosterType);
            if (quantity > 0)
            {
                var shouldEquip = !shopManager.IsBoosterEquipped(card.boosterType);
                shopManager.ToggleBoosterForRun(card.boosterType, shouldEquip);
            }
            else if (shopManager.BuyBooster(card.boosterType, card.cost))
            {
                shopManager.ToggleBoosterForRun(card.boosterType, true);
            }

            RefreshPopup();
        }

        private void HandleTimeSlowClicked()
        {
            HandleBoosterCardClicked(timeSlowCard);
        }

        private void HandleSpeedClicked()
        {
            HandleBoosterCardClicked(speedCard);
        }

        private void HandlePatienceClicked()
        {
            HandleBoosterCardClicked(patienceCard);
        }

        private void HandleBundleClicked()
        {
            if (shopManager == null)
            {
                return;
            }

            if (!shopManager.BuyBundleAndEquip(bundleCost))
            {
                RefreshPopup();
                return;
            }

            PlayBundleFeedback();
            RefreshPopup();
        }

        private void RefreshCard(BoosterCard card)
        {
            if (shopManager == null || card == null)
            {
                return;
            }

            var quantity = shopManager.GetBoosterQuantity(card.boosterType);
            var equipped = shopManager.IsBoosterEquipped(card.boosterType);
            var hasEnoughGold = shopManager.PlayerGold >= card.cost;

            if (card.ownedQuantityText != null)
            {
                card.ownedQuantityText.text = $"Sahip olunan: {quantity}";
            }

            if (card.costText != null)
            {
                card.costText.text = quantity > 0 ? string.Empty : $"{card.cost} Altın";
            }

            if (card.actionText != null)
            {
                card.actionText.text = quantity > 0
                    ? equipped ? "Kuşanıldı" : "Kuşan"
                    : hasEnoughGold ? "Satın Al & Kuşan" : "Altın Yetersiz";
            }

            if (card.checkmark != null)
            {
                card.checkmark.SetActive(equipped);
            }

            if (card.buyAndEquipButton != null)
            {
                card.buyAndEquipButton.interactable = quantity > 0 || hasEnoughGold;
            }

            if (card.cardButton != null)
            {
                card.cardButton.interactable = quantity > 0 || hasEnoughGold;
            }
        }

        private void RefreshBundle()
        {
            if (shopManager == null)
            {
                return;
            }

            var totalSingleCost = singleCost * 3;
            var hasEnoughGold = shopManager.PlayerGold >= bundleCost;

            if (bundleActionText != null)
            {
                bundleActionText.text = hasEnoughGold
                    ? $"Tontis Paket: {totalSingleCost} yerine {bundleCost} Altin"
                    : $"Paket icin {bundleCost} Altin gerekli";
            }

            if (buyBundleButton != null)
            {
                buyBundleButton.interactable = hasEnoughGold;
            }
        }

        private void RegisterCard(BoosterCard card, UnityEngine.Events.UnityAction handler)
        {
            if (card == null)
            {
                return;
            }

            if (card.cardButton != null)
            {
                card.cardButton.onClick.AddListener(handler);
            }

            if (card.buyAndEquipButton != null)
            {
                card.buyAndEquipButton.onClick.AddListener(handler);
            }
        }

        private void UnregisterCard(BoosterCard card, UnityEngine.Events.UnityAction handler)
        {
            if (card == null)
            {
                return;
            }

            if (card.cardButton != null)
            {
                card.cardButton.onClick.RemoveListener(handler);
            }

            if (card.buyAndEquipButton != null)
            {
                card.buyAndEquipButton.onClick.RemoveListener(handler);
            }
        }

        private void ResolveMissingReferences()
        {
            SyncSingleCosts();

            if (shopManager == null)
            {
                shopManager = FindFirstObjectByType<PreGameShopManager>();
            }

            if (popupRoot == null && popupPanel != null)
            {
                popupRoot = popupPanel.GetComponent<RectTransform>();
            }

            if (buyBundleButton == null && popupPanel != null)
            {
                var bundleTransform = popupPanel.transform.Find("BuyBundleButton");
                buyBundleButton = bundleTransform != null ? bundleTransform.GetComponent<Button>() : null;
            }

            if (bundleActionText == null && buyBundleButton != null)
            {
                var labelTransform = buyBundleButton.transform.Find("Label");
                bundleActionText = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
            }

            if (bundleConfettiParticles == null && popupPanel != null)
            {
                var confettiTransform = popupPanel.transform.Find("Bundle Confetti");
                bundleConfettiParticles = confettiTransform != null ? confettiTransform.GetComponent<ParticleSystem>() : null;
            }

            if (uiAudioSource == null)
            {
                uiAudioSource = GetComponent<AudioSource>();
                if (uiAudioSource == null)
                {
                    uiAudioSource = gameObject.AddComponent<AudioSource>();
                    uiAudioSource.playOnAwake = false;
                }
            }

            if (cashRegisterSound == null)
            {
                cashRegisterSound = CreateCashRegisterClip();
            }

            AutoWireCard(timeSlowCard, "TimeSlow");
            AutoWireCard(speedCard, "Speed");
            AutoWireCard(patienceCard, "Patience");
        }

        private void SyncSingleCosts()
        {
            timeSlowCard.cost = singleCost;
            speedCard.cost = singleCost;
            patienceCard.cost = singleCost;
        }

        private void AutoWireCard(BoosterCard card, string prefix)
        {
            if (card == null || popupPanel == null)
            {
                return;
            }

            var root = popupPanel.transform;
            var cardTransform = root.Find($"{prefix} Card");
            if (cardTransform == null)
            {
                return;
            }

            if (card.cardButton == null)
            {
                card.cardButton = cardTransform.GetComponent<Button>();
            }

            if (card.buyAndEquipButton == null)
            {
                var buttonTransform = cardTransform.Find("Buy Equip Button");
                card.buyAndEquipButton = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
            }

            if (card.checkmark == null)
            {
                var checkmarkTransform = cardTransform.Find("Checkmark");
                card.checkmark = checkmarkTransform != null ? checkmarkTransform.gameObject : null;
            }

            if (card.ownedQuantityText == null)
            {
                var textTransform = cardTransform.Find("Owned Text");
                card.ownedQuantityText = textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
            }

            if (card.costText == null)
            {
                var textTransform = cardTransform.Find("Cost Text");
                card.costText = textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
            }

            if (card.actionText == null)
            {
                var textTransform = cardTransform.Find("Action Text");
                card.actionText = textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
            }
        }

        private void SetPopupVisible(bool visible)
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(visible);
            }
        }

        private void PlayOpenAnimation()
        {
            if (popupRoot == null)
            {
                return;
            }

            if (openAnimationRoutine != null)
            {
                StopCoroutine(openAnimationRoutine);
            }

            openAnimationRoutine = StartCoroutine(OpenAnimationRoutine());
        }

        private System.Collections.IEnumerator OpenAnimationRoutine()
        {
            popupRoot.localScale = Vector3.zero;
            var elapsed = 0f;

            while (elapsed < openAnimationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / openAnimationSeconds);
                var eased = EaseOutBack(t);
                popupRoot.localScale = Vector3.one * Mathf.LerpUnclamped(0f, overshootScale, eased);
                yield return null;
            }

            popupRoot.localScale = Vector3.one;
            openAnimationRoutine = null;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private void PlayBundleFeedback()
        {
            if (bundleConfettiParticles != null)
            {
                bundleConfettiParticles.Play(true);
            }

            if (uiAudioSource != null && cashRegisterSound != null)
            {
                uiAudioSource.PlayOneShot(cashRegisterSound);
            }
        }

        private static AudioClip CreateCashRegisterClip()
        {
            const int sampleRate = 22050;
            const float duration = 0.32f;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var time = i / (float)sampleRate;
                var envelope = Mathf.Clamp01(1f - time / duration);
                var ding = Mathf.Sin(2f * Mathf.PI * 1320f * time) * 0.26f;
                var click = time < 0.055f ? Mathf.Sin(2f * Mathf.PI * 360f * time) * 0.35f : 0f;
                samples[i] = (ding + click) * envelope;
            }

            var clip = AudioClip.Create("PreLevelBundleCashRegister", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
