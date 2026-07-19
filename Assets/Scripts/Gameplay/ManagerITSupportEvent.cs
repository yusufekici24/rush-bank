using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum ManagerCrashType
    {
        BlueScreen,
        LooseCable,
        OverheatingFan
    }

    public class ManagerITSupportEvent : MonoBehaviour
    {
        [Header("Trigger")]
        [SerializeField, Range(0f, 1f)] private float triggerChance = 0.15f;
        [SerializeField, Min(1f)] private float triggerCheckSeconds = 45f;
        [SerializeField] private bool autoTrigger = true;

        [Header("Scene References")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private MobilePlayerController mobilePlayerController;
        [SerializeField] private ChubbyTopDownInputController topDownController;
        [SerializeField] private Transform managerDoor;
        [SerializeField] private Transform managerPc;
        [SerializeField] private Transform playerEffectAnchor;

        [Header("UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GameObject supportPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text instructionText;
        [SerializeField] private Slider fanProgressSlider;
        [SerializeField] private Button ctrlButton;
        [SerializeField] private Button altButton;
        [SerializeField] private Button delButton;
        [SerializeField] private Button looseCableButton;
        [SerializeField] private Button fanHoldButton;

        [Header("Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip sparksSound;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip repairSuccessSound;
        [SerializeField] private GameObject exclamationPrefab;
        [SerializeField] private ParticleSystem graceParticlePrefab;

        [Header("Rewards")]
        [SerializeField, Min(0)] private int goldReward = 150;
        [SerializeField, Min(0.1f)] private float graceBoostSeconds = 15f;
        [SerializeField, Min(1f)] private float speedBoostMultiplier = 1.2f;

        public UnityEvent<ManagerCrashType> OnSupportEventStarted = new UnityEvent<ManagerCrashType>();
        public UnityEvent OnSupportEventCompleted = new UnityEvent();
        public UnityEvent OnManagerGraceBoostStarted = new UnityEvent();
        public UnityEvent OnManagerGraceBoostEnded = new UnityEvent();

        private Coroutine triggerRoutine;
        private Coroutine graceRoutine;
        private GameObject activeExclamation;
        private ParticleSystem activeGraceParticle;
        private ManagerCrashType activeCrashType;
        private int blueScreenStep;
        private float fanProgress;
        private bool supportActive;
        private bool miniGameOpen;
        private bool holdingFanButton;
        private bool speedBoostApplied;

        public bool IsSupportActive => supportActive;
        public ManagerCrashType ActiveCrashType => activeCrashType;

        private void Awake()
        {
            ResolveMissingReferences();
            EnsureUi();
            SetPanelVisible(false);
        }

        private void OnEnable()
        {
            RegisterButtons();
            if (autoTrigger && triggerRoutine == null)
            {
                triggerRoutine = StartCoroutine(RandomTriggerRoutine());
            }
        }

        private void OnDisable()
        {
            UnregisterButtons();
            if (triggerRoutine != null)
            {
                StopCoroutine(triggerRoutine);
                triggerRoutine = null;
            }
        }

        private void Update()
        {
            if (!miniGameOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetPanelVisible(false);
                miniGameOpen = false;
                return;
            }

            if (activeCrashType == ManagerCrashType.OverheatingFan)
            {
                TickFanRepair();
            }
        }

        public void ForceTriggerEvent()
        {
            if (supportActive)
            {
                return;
            }

            StartSupportEvent((ManagerCrashType)Random.Range(0, 3));
        }

        public void InteractWithManagerPc()
        {
            if (!supportActive)
            {
                return;
            }

            EnsureUi();
            miniGameOpen = true;
            SetPanelVisible(true);
            ConfigureMiniGameUi();
        }

        public void PressBlueScreenButton(string key)
        {
            if (!miniGameOpen || activeCrashType != ManagerCrashType.BlueScreen)
            {
                return;
            }

            var expected = blueScreenStep switch
            {
                0 => "Ctrl",
                1 => "Alt",
                _ => "Del"
            };

            if (key != expected)
            {
                blueScreenStep = 0;
                SetInstruction("Wrong key. Start again: Ctrl -> Alt -> Del.");
                return;
            }

            PlaySound(buttonClickSound);
            blueScreenStep++;
            if (blueScreenStep >= 3)
            {
                CompleteRepair();
                return;
            }

            SetInstruction(blueScreenStep == 1 ? "Now press Alt." : "Now press Del.");
        }

        public void CompleteLooseCableDrag()
        {
            if (!miniGameOpen || activeCrashType != ManagerCrashType.LooseCable)
            {
                return;
            }

            PlaySound(buttonClickSound);
            CompleteRepair();
        }

        public void SetFanHold(bool held)
        {
            holdingFanButton = held;
        }

        private IEnumerator RandomTriggerRoutine()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(triggerCheckSeconds);

                if (!supportActive && Random.value <= triggerChance)
                {
                    ForceTriggerEvent();
                }
            }
        }

        private void StartSupportEvent(ManagerCrashType crashType)
        {
            activeCrashType = crashType;
            supportActive = true;
            miniGameOpen = false;
            blueScreenStep = 0;
            fanProgress = 0f;
            SpawnExclamation();
            PlaySound(sparksSound);
            OnSupportEventStarted.Invoke(activeCrashType);
        }

        private void ConfigureMiniGameUi()
        {
            SetAllMiniGameControls(false);
            if (titleText != null)
            {
                titleText.text = "Manager IT Support";
            }

            switch (activeCrashType)
            {
                case ManagerCrashType.BlueScreen:
                    SetInstruction("Blue screen! Press Ctrl -> Alt -> Del.");
                    SetBlueScreenControls(true);
                    break;
                case ManagerCrashType.LooseCable:
                    SetInstruction("Loose cable! Drag the plug into the outlet.");
                    if (looseCableButton != null)
                    {
                        looseCableButton.gameObject.SetActive(true);
                    }
                    break;
                case ManagerCrashType.OverheatingFan:
                    SetInstruction("Overheating fan! Hold to blow the dust away.");
                    if (fanProgressSlider != null)
                    {
                        fanProgressSlider.gameObject.SetActive(true);
                        fanProgressSlider.value = 0f;
                    }

                    if (fanHoldButton != null)
                    {
                        fanHoldButton.gameObject.SetActive(true);
                    }
                    break;
            }
        }

        private void TickFanRepair()
        {
            if (!holdingFanButton && !Input.GetMouseButton(0) && Input.touchCount == 0)
            {
                return;
            }

            fanProgress = Mathf.Clamp01(fanProgress + Time.deltaTime / 2f);
            if (fanProgressSlider != null)
            {
                fanProgressSlider.value = fanProgress;
            }

            if (fanProgress >= 1f)
            {
                CompleteRepair();
            }
        }

        private void CompleteRepair()
        {
            supportActive = false;
            miniGameOpen = false;
            holdingFanButton = false;
            DestroyExclamation();
            SetPanelVisible(false);
            PlaySound(repairSuccessSound);
            AddGold(goldReward);
            ApplyManagerGraceBoost();
            OnSupportEventCompleted.Invoke();
        }

        private void ApplyManagerGraceBoost()
        {
            if (graceRoutine != null)
            {
                StopCoroutine(graceRoutine);
                RestoreManagerGraceBoost();
            }

            graceRoutine = StartCoroutine(ManagerGraceBoostRoutine());
        }

        private IEnumerator ManagerGraceBoostRoutine()
        {
            queueManager?.ApplyQueueReliefBoost(0f, 0f, graceBoostSeconds);

            if (mobilePlayerController != null)
            {
                mobilePlayerController.MovementSpeedMultiplier *= speedBoostMultiplier;
                speedBoostApplied = true;
            }

            if (topDownController != null)
            {
                topDownController.MovementSpeedMultiplier *= speedBoostMultiplier;
                speedBoostApplied = true;
            }

            SpawnGraceParticle();
            OnManagerGraceBoostStarted.Invoke();

            yield return new WaitForSeconds(graceBoostSeconds);

            RestoreManagerGraceBoost();
            OnManagerGraceBoostEnded.Invoke();
            graceRoutine = null;
        }

        private void RestoreManagerGraceBoost()
        {
            queueManager?.ResetQueueReliefBoost();

            if (speedBoostApplied)
            {
                if (mobilePlayerController != null)
                {
                    mobilePlayerController.MovementSpeedMultiplier /= speedBoostMultiplier;
                }

                if (topDownController != null)
                {
                    topDownController.MovementSpeedMultiplier /= speedBoostMultiplier;
                }
            }

            speedBoostApplied = false;
            DestroyGraceParticle();
        }

        private void SpawnExclamation()
        {
            DestroyExclamation();
            var parent = managerDoor != null ? managerDoor : transform;
            if (exclamationPrefab != null)
            {
                activeExclamation = Instantiate(exclamationPrefab, parent);
                activeExclamation.transform.localPosition = Vector3.up * 1.8f;
                return;
            }

            activeExclamation = new GameObject("Manager IT Alert Icon");
            activeExclamation.transform.SetParent(parent, false);
            activeExclamation.transform.localPosition = Vector3.up * 1.8f;
            activeExclamation.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var label = activeExclamation.AddComponent<TextMesh>();
            label.text = "!";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.55f;
            label.color = Color.red;
            activeExclamation.AddComponent<PulsingWorldIcon>();
        }

        private void DestroyExclamation()
        {
            if (activeExclamation != null)
            {
                Destroy(activeExclamation);
                activeExclamation = null;
            }
        }

        private void SpawnGraceParticle()
        {
            DestroyGraceParticle();
            if (graceParticlePrefab == null)
            {
                return;
            }

            var parent = playerEffectAnchor != null
                ? playerEffectAnchor
                : mobilePlayerController != null
                    ? mobilePlayerController.transform
                    : transform;
            activeGraceParticle = Instantiate(graceParticlePrefab, parent);
            activeGraceParticle.transform.localPosition = Vector3.up * 1.2f;
            activeGraceParticle.Play();
        }

        private void DestroyGraceParticle()
        {
            if (activeGraceParticle != null)
            {
                Destroy(activeGraceParticle.gameObject);
                activeGraceParticle = null;
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

        private void EnsureUi()
        {
            if (supportPanel != null && titleText != null && instructionText != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasObject = new GameObject("Manager IT Support Canvas");
                targetCanvas = canvasObject.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            CreateFallbackUi();
        }

        private void CreateFallbackUi()
        {
            supportPanel = new GameObject("Manager IT Support Panel");
            supportPanel.transform.SetParent(targetCanvas.transform, false);

            var panelRect = supportPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(520f, 430f);

            var panel = supportPanel.AddComponent<Image>();
            panel.color = new Color(0.08f, 0.1f, 0.13f, 0.96f);

            titleText = CreateText("Title", supportPanel.transform, "Manager IT Support", 34, new Vector2(0f, 165f), new Vector2(460f, 60f), Color.white);
            instructionText = CreateText("Instructions", supportPanel.transform, string.Empty, 22, new Vector2(0f, 110f), new Vector2(460f, 62f), new Color(0.95f, 0.86f, 0.42f));

            ctrlButton = CreateButton("Ctrl Button", "Ctrl", new Vector2(-150f, 25f), new Vector2(105f, 62f), new Color(0.25f, 0.58f, 1f));
            altButton = CreateButton("Alt Button", "Alt", new Vector2(0f, 25f), new Vector2(105f, 62f), new Color(0.25f, 0.58f, 1f));
            delButton = CreateButton("Del Button", "Del", new Vector2(150f, 25f), new Vector2(105f, 62f), new Color(0.25f, 0.58f, 1f));
            looseCableButton = CreateButton("Loose Cable Button", "PLUG IN", new Vector2(0f, 10f), new Vector2(230f, 70f), new Color(0.92f, 0.58f, 0.24f));
            fanHoldButton = CreateButton("Fan Hold Button", "HOLD TO BLOW", new Vector2(0f, -80f), new Vector2(250f, 68f), new Color(0.5f, 0.88f, 1f));

            var sliderObject = new GameObject("Fan Progress");
            sliderObject.transform.SetParent(supportPanel.transform, false);
            fanProgressSlider = sliderObject.AddComponent<Slider>();
            var sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = new Vector2(0f, 10f);
            sliderRect.sizeDelta = new Vector2(310f, 28f);
        }

        private Button CreateButton(string name, string label, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(supportPanel.transform, false);
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

        private void SetPanelVisible(bool visible)
        {
            if (supportPanel != null)
            {
                supportPanel.SetActive(visible);
            }
        }

        private void SetAllMiniGameControls(bool visible)
        {
            SetBlueScreenControls(visible);
            if (looseCableButton != null)
            {
                looseCableButton.gameObject.SetActive(visible);
            }

            if (fanHoldButton != null)
            {
                fanHoldButton.gameObject.SetActive(visible);
            }

            if (fanProgressSlider != null)
            {
                fanProgressSlider.gameObject.SetActive(visible);
            }
        }

        private void SetBlueScreenControls(bool visible)
        {
            if (ctrlButton != null)
            {
                ctrlButton.gameObject.SetActive(visible);
            }

            if (altButton != null)
            {
                altButton.gameObject.SetActive(visible);
            }

            if (delButton != null)
            {
                delButton.gameObject.SetActive(visible);
            }
        }

        private void SetInstruction(string text)
        {
            if (instructionText != null)
            {
                instructionText.text = text;
            }
        }

        private void RegisterButtons()
        {
            if (ctrlButton != null)
            {
                ctrlButton.onClick.AddListener(() => PressBlueScreenButton("Ctrl"));
            }

            if (altButton != null)
            {
                altButton.onClick.AddListener(() => PressBlueScreenButton("Alt"));
            }

            if (delButton != null)
            {
                delButton.onClick.AddListener(() => PressBlueScreenButton("Del"));
            }

            if (looseCableButton != null)
            {
                looseCableButton.onClick.AddListener(CompleteLooseCableDrag);
            }

            RegisterFanHoldEvents();
        }

        private void UnregisterButtons()
        {
            if (ctrlButton != null)
            {
                ctrlButton.onClick.RemoveAllListeners();
            }

            if (altButton != null)
            {
                altButton.onClick.RemoveAllListeners();
            }

            if (delButton != null)
            {
                delButton.onClick.RemoveAllListeners();
            }

            if (looseCableButton != null)
            {
                looseCableButton.onClick.RemoveListener(CompleteLooseCableDrag);
            }
        }

        private void RegisterFanHoldEvents()
        {
            if (fanHoldButton == null)
            {
                return;
            }

            var eventTrigger = fanHoldButton.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = fanHoldButton.gameObject.AddComponent<EventTrigger>();
            }

            eventTrigger.triggers.Clear();
            AddEventTriggerEntry(eventTrigger, EventTriggerType.PointerDown, () => SetFanHold(true));
            AddEventTriggerEntry(eventTrigger, EventTriggerType.PointerUp, () => SetFanHold(false));
            AddEventTriggerEntry(eventTrigger, EventTriggerType.PointerExit, () => SetFanHold(false));
        }

        private static void AddEventTriggerEntry(EventTrigger eventTrigger, EventTriggerType type, UnityEngine.Events.UnityAction callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => callback());
            eventTrigger.triggers.Add(entry);
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

            if (mobilePlayerController == null)
            {
                mobilePlayerController = FindFirstObjectByType<MobilePlayerController>();
            }

            if (topDownController == null)
            {
                topDownController = FindFirstObjectByType<ChubbyTopDownInputController>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private class PulsingWorldIcon : MonoBehaviour
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
                transform.localScale = baseScale * (1f + Mathf.Sin(Time.time * 7f + phase) * 0.1f);
            }
        }
    }
}
