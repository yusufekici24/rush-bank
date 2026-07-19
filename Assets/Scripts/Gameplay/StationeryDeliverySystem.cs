using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum StationeryItemType
    {
        A4Paper,
        InkPen,
        Stapler
    }

    public enum StationeryDeskType
    {
        RelationshipManager,
        InsuranceSpecialist,
        CreditSpecialist
    }

    public class StationeryDeliverySystem : MonoBehaviour
    {
        public static StationeryDeliverySystem Instance { get; private set; }

        [Header("Trigger")]
        [SerializeField] private bool autoTrigger = true;
        [SerializeField, Min(5f)] private float triggerIntervalSeconds = 55f;
        [SerializeField, Range(0f, 1f)] private float triggerChance = 0.25f;

        [Header("Scene References")]
        [SerializeField] private Transform relationshipManagerDesk;
        [SerializeField] private Transform insuranceSpecialistDesk;
        [SerializeField] private Transform creditSpecialistDesk;
        [SerializeField] private Transform supplyCabinet;
        [SerializeField] private Transform playerHoldPoint;
        [SerializeField] private Animator playerAnimator;

        [Header("UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GameObject supplyMenuRoot;
        [SerializeField] private Text requestText;
        [SerializeField] private Button a4PaperButton;
        [SerializeField] private Button inkPenButton;
        [SerializeField] private Button staplerButton;

        [Header("Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip shortageSound;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private AudioClip deliverySound;
        [SerializeField] private GameObject shortageIconPrefab;
        [SerializeField] private GameObject a4PaperPrefab;
        [SerializeField] private GameObject inkPenPrefab;
        [SerializeField] private GameObject staplerPrefab;
        [SerializeField] private ParticleSystem efficiencyParticlePrefab;

        [Header("Urgency")]
        [SerializeField] private StaffRequestUrgency requestUrgency;

        [Header("Reward")]
        [SerializeField, Min(0)] private int goldReward = 80;
        [SerializeField, Min(1)] private int efficiencyBoostCharges = 3;
        [SerializeField, Min(1f)] private float boostedDeskMoveSpeedMultiplier = 2f;

        [Header("Animation")]
        [SerializeField] private string carryingBoxBool = "CarryingBox";

        public UnityEvent<StationeryDeskType, StationeryItemType> OnShortageStarted = new UnityEvent<StationeryDeskType, StationeryItemType>();
        public UnityEvent<StationeryItemType> OnSupplyPickedUp = new UnityEvent<StationeryItemType>();
        public UnityEvent<StationeryDeskType> OnSupplyDelivered = new UnityEvent<StationeryDeskType>();
        public UnityEvent<StationeryDeskType, int> OnEfficiencyBoostChargesChanged = new UnityEvent<StationeryDeskType, int>();

        private Coroutine triggerRoutine;
        private GameObject activeShortageIcon;
        private GameObject heldSupplyItem;
        private StationeryDeskType activeDeskType;
        private StationeryItemType requestedItem;
        private StationeryItemType? heldItemType;
        private int relationshipEfficiencyCharges;
        private int insuranceEfficiencyCharges;
        private int creditEfficiencyCharges;
        private bool shortageActive;
        private bool deskLockedByUrgency;

        public bool IsShortageActive => shortageActive;
        public bool IsDeskLockedByUrgency => deskLockedByUrgency;
        public StationeryDeskType ActiveDeskType => activeDeskType;
        public StationeryItemType RequestedItem => requestedItem;
        public float GlobalRedirectSpeedMultiplier { get; set; } = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveMissingReferences();
            EnsureUi();
            SetSupplyMenuVisible(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            RegisterButtons();
            RegisterUrgencyListeners();
            if (autoTrigger && triggerRoutine == null)
            {
                triggerRoutine = StartCoroutine(RandomShortageRoutine());
            }
        }

        private void OnDisable()
        {
            UnregisterButtons();
            UnregisterUrgencyListeners();
            if (triggerRoutine != null)
            {
                StopCoroutine(triggerRoutine);
                triggerRoutine = null;
            }
        }

        public bool IsDeskBlocked(Transform desk)
        {
            return (shortageActive || deskLockedByUrgency) && IsSameDesk(desk, GetActiveDesk());
        }

        public bool CanAcceptRedirect(Transform desk)
        {
            return !IsDeskBlocked(desk);
        }

        public float ConsumeRedirectSpeedMultiplier(Transform desk)
        {
            var multiplier = Mathf.Max(0.1f, GlobalRedirectSpeedMultiplier);
            var deskType = ResolveDeskType(desk);
            if (deskType == StationeryDeskType.RelationshipManager && relationshipEfficiencyCharges > 0)
            {
                relationshipEfficiencyCharges--;
                OnEfficiencyBoostChargesChanged.Invoke(deskType, relationshipEfficiencyCharges);
                return multiplier * boostedDeskMoveSpeedMultiplier;
            }

            if (deskType == StationeryDeskType.InsuranceSpecialist && insuranceEfficiencyCharges > 0)
            {
                insuranceEfficiencyCharges--;
                OnEfficiencyBoostChargesChanged.Invoke(deskType, insuranceEfficiencyCharges);
                return multiplier * boostedDeskMoveSpeedMultiplier;
            }

            if (deskType == StationeryDeskType.CreditSpecialist && creditEfficiencyCharges > 0)
            {
                creditEfficiencyCharges--;
                OnEfficiencyBoostChargesChanged.Invoke(deskType, creditEfficiencyCharges);
                return multiplier * boostedDeskMoveSpeedMultiplier;
            }

            return multiplier;
        }

        public void ForceShortage(StationeryDeskType deskType)
        {
            if (shortageActive)
            {
                return;
            }

            StartShortage(deskType, GetRandomItem());
        }

        public void OpenSupplyCabinetMenu()
        {
            if (!shortageActive)
            {
                return;
            }

            EnsureUi();
            SetSupplyMenuVisible(true);
            RefreshSupplyMenu();
        }

        public void InteractWithSupplyCabinet()
        {
            OpenSupplyCabinetMenu();
        }

        public void SelectSupplyItem(string itemName)
        {
            if (!System.Enum.TryParse(itemName, out StationeryItemType itemType))
            {
                return;
            }

            SelectSupplyItem(itemType);
        }

        public void SelectSupplyItem(StationeryItemType itemType)
        {
            if (!shortageActive)
            {
                return;
            }

            if (itemType != requestedItem)
            {
                RefreshSupplyMenu("Wrong item. Check the desk request.");
                return;
            }

            DestroyHeldItem();
            heldItemType = itemType;
            heldSupplyItem = CreateHeldSupplyItem(itemType);
            SetCarryingAnimation(true);
            SetSupplyMenuVisible(false);
            PlaySound(pickupSound);
            OnSupplyPickedUp.Invoke(itemType);
        }

        public void DeliverHeldSupplyToActiveDesk()
        {
            if (!shortageActive || heldItemType == null || heldItemType.Value != requestedItem)
            {
                return;
            }

            CompleteDelivery();
        }

        public void InteractWithDemandingDesk()
        {
            DeliverHeldSupplyToActiveDesk();
        }

        public void CancelShortage(bool clearUrgency = true)
        {
            shortageActive = false;
            heldItemType = null;
            DestroyShortageIcon();
            DestroyHeldItem();
            SetCarryingAnimation(false);
            SetSupplyMenuVisible(false);

            if (clearUrgency)
            {
                requestUrgency?.ClearRequest();
            }
        }

        public void UnlockUrgencyDesk()
        {
            deskLockedByUrgency = false;
        }

        private IEnumerator RandomShortageRoutine()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(triggerIntervalSeconds);

                if (!shortageActive && Random.value <= triggerChance)
                {
                    ForceShortage((StationeryDeskType)Random.Range(0, 2));
                }
            }
        }

        private void StartShortage(StationeryDeskType deskType, StationeryItemType itemType)
        {
            activeDeskType = deskType;
            requestedItem = itemType;
            shortageActive = true;
            deskLockedByUrgency = false;
            SpawnShortageIcon();
            requestUrgency?.BeginRequest(GetActiveDesk());
            PlaySound(shortageSound);
            OnShortageStarted.Invoke(activeDeskType, requestedItem);
        }

        private void CompleteDelivery()
        {
            requestUrgency?.ResolveRequest();
            AddGold(goldReward);
            PlaySound(deliverySound);
            SpawnEfficiencyEffect();
            AddEfficiencyCharges(activeDeskType, efficiencyBoostCharges);
            OnSupplyDelivered.Invoke(activeDeskType);
            CancelShortage(false);
        }

        private void AddEfficiencyCharges(StationeryDeskType deskType, int charges)
        {
            if (deskType == StationeryDeskType.RelationshipManager)
            {
                relationshipEfficiencyCharges += charges;
                OnEfficiencyBoostChargesChanged.Invoke(deskType, relationshipEfficiencyCharges);
                return;
            }

            if (deskType == StationeryDeskType.InsuranceSpecialist)
            {
                insuranceEfficiencyCharges += charges;
                OnEfficiencyBoostChargesChanged.Invoke(deskType, insuranceEfficiencyCharges);
                return;
            }

            creditEfficiencyCharges += charges;
            OnEfficiencyBoostChargesChanged.Invoke(deskType, creditEfficiencyCharges);
        }

        private void SpawnShortageIcon()
        {
            DestroyShortageIcon();
            var desk = GetActiveDesk();
            if (desk == null)
            {
                return;
            }

            if (shortageIconPrefab != null)
            {
                activeShortageIcon = Instantiate(shortageIconPrefab, desk);
                activeShortageIcon.transform.localPosition = Vector3.up * 1.8f;
                return;
            }

            activeShortageIcon = new GameObject("Stationery Shortage Icon");
            activeShortageIcon.transform.SetParent(desk, false);
            activeShortageIcon.transform.localPosition = Vector3.up * 1.8f;
            activeShortageIcon.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var label = activeShortageIcon.AddComponent<TextMesh>();
            label.text = GetItemLabel(requestedItem);
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.2f;
            label.color = Color.yellow;
        }

        private void DestroyShortageIcon()
        {
            if (activeShortageIcon != null)
            {
                Destroy(activeShortageIcon);
                activeShortageIcon = null;
            }
        }

        private GameObject CreateHeldSupplyItem(StationeryItemType itemType)
        {
            var prefab = itemType switch
            {
                StationeryItemType.InkPen => inkPenPrefab,
                StationeryItemType.Stapler => staplerPrefab,
                _ => a4PaperPrefab
            };

            var item = prefab != null
                ? Instantiate(prefab, GetHoldPoint())
                : CreateFallbackSupplyItem(itemType);
            item.transform.SetParent(GetHoldPoint(), false);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            return item;
        }

        private GameObject CreateFallbackSupplyItem(StationeryItemType itemType)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = $"{itemType} Supply Item";
            item.transform.localScale = itemType switch
            {
                StationeryItemType.InkPen => new Vector3(0.08f, 0.08f, 0.55f),
                StationeryItemType.Stapler => new Vector3(0.42f, 0.16f, 0.18f),
                _ => new Vector3(0.45f, 0.06f, 0.32f)
            };

            if (item.TryGetComponent<Renderer>(out var rendererComponent))
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = itemType switch
                {
                    StationeryItemType.InkPen => Color.blue,
                    StationeryItemType.Stapler => Color.red,
                    _ => Color.white
                };
                rendererComponent.sharedMaterial = material;
            }

            if (item.TryGetComponent<Collider>(out var colliderComponent))
            {
                colliderComponent.enabled = false;
            }

            return item;
        }

        private void DestroyHeldItem()
        {
            if (heldSupplyItem != null)
            {
                Destroy(heldSupplyItem);
                heldSupplyItem = null;
            }
        }

        private void SpawnEfficiencyEffect()
        {
            if (efficiencyParticlePrefab == null)
            {
                return;
            }

            var desk = GetActiveDesk();
            if (desk == null)
            {
                return;
            }

            var effect = Instantiate(efficiencyParticlePrefab, desk.position + Vector3.up * 1.2f, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, 2f);
        }

        private Transform GetActiveDesk()
        {
            return activeDeskType == StationeryDeskType.RelationshipManager
                ? relationshipManagerDesk
                : activeDeskType == StationeryDeskType.InsuranceSpecialist
                    ? insuranceSpecialistDesk
                    : creditSpecialistDesk;
        }

        private Transform GetHoldPoint()
        {
            return playerHoldPoint != null ? playerHoldPoint : transform;
        }

        private StationeryDeskType ResolveDeskType(Transform desk)
        {
            return IsSameDesk(desk, insuranceSpecialistDesk)
                ? StationeryDeskType.InsuranceSpecialist
                : IsSameDesk(desk, creditSpecialistDesk)
                    ? StationeryDeskType.CreditSpecialist
                    : StationeryDeskType.RelationshipManager;
        }

        private bool IsSameDesk(Transform a, Transform b)
        {
            return a != null && b != null && (a == b || a.IsChildOf(b) || b.IsChildOf(a));
        }

        private StationeryItemType GetRandomItem()
        {
            return (StationeryItemType)Random.Range(0, 3);
        }

        private static string GetItemLabel(StationeryItemType itemType)
        {
            return itemType switch
            {
                StationeryItemType.InkPen => "PEN",
                StationeryItemType.Stapler => "STAPLER",
                _ => "A4"
            };
        }

        private void RefreshSupplyMenu(string status = null)
        {
            if (requestText == null)
            {
                return;
            }

            requestText.text = string.IsNullOrEmpty(status)
                ? $"Need: {GetItemLabel(requestedItem)}"
                : status;
        }

        private void EnsureUi()
        {
            if (supplyMenuRoot != null && requestText != null && a4PaperButton != null && inkPenButton != null && staplerButton != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasObject = new GameObject("Stationery Delivery Canvas");
                targetCanvas = canvasObject.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            CreateFallbackUi();
        }

        private void CreateFallbackUi()
        {
            supplyMenuRoot = new GameObject("Stationery Supply Menu");
            supplyMenuRoot.transform.SetParent(targetCanvas.transform, false);

            var rootRect = supplyMenuRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(1f, 0.5f);
            rootRect.anchoredPosition = new Vector2(-24f, 0f);
            rootRect.sizeDelta = new Vector2(330f, 300f);

            var panel = supplyMenuRoot.AddComponent<Image>();
            panel.color = new Color(0.92f, 0.96f, 1f, 0.96f);

            requestText = CreateText("Stationery Request", supplyMenuRoot.transform, "Need: A4", 28, new Vector2(0f, 105f), new Vector2(280f, 55f), Color.black);
            a4PaperButton = CreateButton("A4 Paper Button", "A4 PAPER", new Vector2(0f, 38f), new Vector2(230f, 48f), Color.white);
            inkPenButton = CreateButton("Ink Pen Button", "INK PEN", new Vector2(0f, -25f), new Vector2(230f, 48f), new Color(0.65f, 0.82f, 1f));
            staplerButton = CreateButton("Stapler Button", "STAPLER", new Vector2(0f, -88f), new Vector2(230f, 48f), new Color(1f, 0.72f, 0.72f));
        }

        private Button CreateButton(string name, string label, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(supplyMenuRoot.transform, false);
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

        private void RegisterButtons()
        {
            if (a4PaperButton != null)
            {
                a4PaperButton.onClick.AddListener(() => SelectSupplyItem(StationeryItemType.A4Paper));
            }

            if (inkPenButton != null)
            {
                inkPenButton.onClick.AddListener(() => SelectSupplyItem(StationeryItemType.InkPen));
            }

            if (staplerButton != null)
            {
                staplerButton.onClick.AddListener(() => SelectSupplyItem(StationeryItemType.Stapler));
            }
        }

        private void UnregisterButtons()
        {
            if (a4PaperButton != null)
            {
                a4PaperButton.onClick.RemoveAllListeners();
            }

            if (inkPenButton != null)
            {
                inkPenButton.onClick.RemoveAllListeners();
            }

            if (staplerButton != null)
            {
                staplerButton.onClick.RemoveAllListeners();
            }
        }

        private void RegisterUrgencyListeners()
        {
            if (requestUrgency == null)
            {
                return;
            }

            requestUrgency.OnRequestFailed.RemoveListener(HandleUrgencyRequestFailed);
            requestUrgency.OnRequestFailed.AddListener(HandleUrgencyRequestFailed);
        }

        private void UnregisterUrgencyListeners()
        {
            if (requestUrgency != null)
            {
                requestUrgency.OnRequestFailed.RemoveListener(HandleUrgencyRequestFailed);
            }
        }

        private void HandleUrgencyRequestFailed()
        {
            deskLockedByUrgency = true;
            shortageActive = false;
            heldItemType = null;
            DestroyShortageIcon();
            DestroyHeldItem();
            SetCarryingAnimation(false);
            SetSupplyMenuVisible(false);
        }

        private void SetSupplyMenuVisible(bool visible)
        {
            if (supplyMenuRoot != null)
            {
                supplyMenuRoot.SetActive(visible);
            }
        }

        private void SetCarryingAnimation(bool carrying)
        {
            if (playerAnimator != null && !string.IsNullOrWhiteSpace(carryingBoxBool))
            {
                playerAnimator.SetBool(carryingBoxBool, carrying);
            }
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

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void ResolveMissingReferences()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (requestUrgency == null)
            {
                requestUrgency = GetComponent<StaffRequestUrgency>();
            }
        }
    }
}
