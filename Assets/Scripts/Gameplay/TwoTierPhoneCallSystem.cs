using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum CallType
    {
        Level1_NormalCustomer,
        Level2_Headquarters
    }

    public enum TwoTierPhoneState
    {
        Silent,
        Ringing,
        Cooldown
    }

    public class TwoTierPhoneCallSystem : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private Vector2 callIntervalSeconds = new Vector2(30f, 45f);
        [SerializeField, Min(0.5f)] private float normalAnswerWindowSeconds = 15f;
        [SerializeField, Min(0.5f)] private float headquartersAnswerWindowSeconds = 7f;
        [SerializeField, Min(0f)] private float cooldownSeconds = 8f;
        [SerializeField, Range(0f, 1f)] private float headquartersChance = 0.25f;

        [Header("Rewards")]
        [SerializeField, Min(0)] private int normalGoldReward = 40;
        [SerializeField, Min(0)] private int headquartersGoldReward = 200;
        [SerializeField, Min(0f)] private float normalSatisfactionReward = 5f;
        [SerializeField, Min(0f)] private float headquartersSatisfactionReward = 30f;
        [SerializeField, Min(0f)] private float missedHeadquartersPenalty = 30f;
        [SerializeField, Min(0.1f)] private float corporateGraceSeconds = 20f;
        [SerializeField, Range(0.05f, 1f)] private float corporatePatienceDrainMultiplier = 0.5f;
        [SerializeField, Min(1f)] private float corporateGoldMultiplier = 1.1f;

        [Header("References")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private ManagerSatisfactionSystem managerSatisfactionSystem;
        [SerializeField] private ScoreManager scoreManager;

        [Header("UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GameObject notificationRoot;
        [SerializeField] private Button answerButton;
        [SerializeField] private Image ringTimerFill;
        [SerializeField] private Image phoneBackground;
        [SerializeField] private RectTransform phoneIcon;
        [SerializeField] private Text phoneIconLabel;
        [SerializeField] private Text rewardText;
        [SerializeField] private GameObject hqFailedOverlay;
        [SerializeField] private Text hqFailedText;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip normalRingSound;
        [SerializeField] private AudioClip headquartersRingSound;
        [SerializeField] private AudioClip friendlyChatterSound;
        [SerializeField] private AudioClip headquartersConfirmSound;
        [SerializeField] private AudioClip missedHeadquartersSound;
        [SerializeField, Min(0.1f)] private float headquartersRingPitch = 1.35f;

        [Header("Motion")]
        [SerializeField, Min(0f)] private float normalShakeDegrees = 7f;
        [SerializeField, Min(0f)] private float headquartersShakeDegrees = 16f;
        [SerializeField, Min(0f)] private float shakeSpeed = 28f;
        [SerializeField, Min(0f)] private float headquartersPulseAmount = 0.18f;

        public UnityEvent<CallType> OnIncomingCall = new UnityEvent<CallType>();
        public UnityEvent<CallType> OnCallAnswered = new UnityEvent<CallType>();
        public UnityEvent OnHeadquartersMissed = new UnityEvent();
        public UnityEvent OnCorporateGraceStarted = new UnityEvent();
        public UnityEvent<TwoTierPhoneState> OnStateChanged = new UnityEvent<TwoTierPhoneState>();

        private TwoTierPhoneState state = TwoTierPhoneState.Silent;
        private CallType activeCallType = CallType.Level1_NormalCustomer;
        private Coroutine rewardTextRoutine;
        private Coroutine hqFailureRoutine;
        private float nextCallTimer;
        private float ringTimer;
        private float cooldownTimer;
        private float activeAnswerWindow;
        private bool isRinging;

        public TwoTierPhoneState State => state;
        public CallType ActiveCallType => activeCallType;
        public float RingTimer => ringTimer;
        public bool IsRinging => isRinging;

        private void Awake()
        {
            ResolveMissingReferences();
            EnsureUi();
            SetNotificationVisible(false);
            SetRewardTextVisible(false);
            SetHqFailedOverlay(false);
            ResetNextCallTimer();
        }

        private void OnEnable()
        {
            if (answerButton != null)
            {
                answerButton.onClick.AddListener(AnswerCall);
            }
        }

        private void OnDisable()
        {
            if (answerButton != null)
            {
                answerButton.onClick.RemoveListener(AnswerCall);
            }

            StopRingSound();
        }

        private void Update()
        {
            if (state == TwoTierPhoneState.Silent)
            {
                TickSilent();
                return;
            }

            if (state == TwoTierPhoneState.Ringing)
            {
                TickRinging();
                return;
            }

            if (state == TwoTierPhoneState.Cooldown)
            {
                TickCooldown();
            }
        }

        public void ForceIncomingCall()
        {
            if (state == TwoTierPhoneState.Ringing)
            {
                return;
            }

            TriggerIncomingCall();
        }

        public void TriggerIncomingCall()
        {
            activeCallType = Random.value < headquartersChance
                ? CallType.Level2_Headquarters
                : CallType.Level1_NormalCustomer;
            activeAnswerWindow = activeCallType == CallType.Level2_Headquarters
                ? headquartersAnswerWindowSeconds
                : normalAnswerWindowSeconds;
            ringTimer = activeAnswerWindow;
            isRinging = true;

            SetNotificationVisible(true);
            SetRewardTextVisible(false);
            ConfigureCallVisuals();
            PlayRingSound();
            SetState(TwoTierPhoneState.Ringing);
            OnIncomingCall.Invoke(activeCallType);
        }

        public void AnswerCall()
        {
            if (state != TwoTierPhoneState.Ringing)
            {
                return;
            }

            StopRingSound();
            isRinging = false;
            SetNotificationVisible(false);

            if (activeCallType == CallType.Level2_Headquarters)
            {
                PlayOneShot(headquartersConfirmSound);
                AddGold(headquartersGoldReward);
                managerSatisfactionSystem?.AddSatisfaction(headquartersSatisfactionReward);
                ApplyCorporateGraceBoost();
                ShowRewardText($"+{headquartersGoldReward} Gold\nHQ Grace!");
            }
            else
            {
                PlayOneShot(friendlyChatterSound);
                AddGold(normalGoldReward);
                managerSatisfactionSystem?.AddSatisfaction(normalSatisfactionReward);
                ShowRewardText($"+{normalGoldReward} Gold");
            }

            OnCallAnswered.Invoke(activeCallType);
            StartCooldown();
        }

        private void TickSilent()
        {
            nextCallTimer -= Time.deltaTime;
            if (nextCallTimer <= 0f)
            {
                TriggerIncomingCall();
            }
        }

        private void TickRinging()
        {
            ringTimer -= Time.deltaTime;
            UpdateRingFill();
            AnimatePhoneIcon();

            if (ringTimer <= 0f)
            {
                MissCall();
            }
        }

        private void TickCooldown()
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                ResetNextCallTimer();
                SetState(TwoTierPhoneState.Silent);
            }
        }

        private void MissCall()
        {
            StopRingSound();
            isRinging = false;
            SetNotificationVisible(false);

            if (activeCallType == CallType.Level2_Headquarters)
            {
                PlayOneShot(missedHeadquartersSound);
                managerSatisfactionSystem?.DeductSatisfaction(missedHeadquartersPenalty);
                ShowHqFailedOverlay();
                OnHeadquartersMissed.Invoke();
            }

            StartCooldown();
        }

        private void ApplyCorporateGraceBoost()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance;
            }

            queueManager?.ApplyQueueReliefBoost(0f, corporatePatienceDrainMultiplier, corporateGraceSeconds);

            if (scoreManager == null)
            {
                scoreManager = ScoreManager.Instance;
            }

            scoreManager?.ApplyGoldMultiplierForSeconds(corporateGoldMultiplier, corporateGraceSeconds);
            OnCorporateGraceStarted.Invoke();
        }

        private void StartCooldown()
        {
            cooldownTimer = cooldownSeconds;
            SetState(TwoTierPhoneState.Cooldown);
        }

        private void ResetNextCallTimer()
        {
            var min = Mathf.Max(1f, Mathf.Min(callIntervalSeconds.x, callIntervalSeconds.y));
            var max = Mathf.Max(min, Mathf.Max(callIntervalSeconds.x, callIntervalSeconds.y));
            nextCallTimer = Random.Range(min, max);
        }

        private void ConfigureCallVisuals()
        {
            var isHeadquarters = activeCallType == CallType.Level2_Headquarters;
            if (phoneBackground != null)
            {
                phoneBackground.color = isHeadquarters
                    ? new Color(0.86f, 0.12f, 0.1f, 0.96f)
                    : new Color(0.95f, 0.78f, 0.16f, 0.96f);
            }

            if (ringTimerFill != null)
            {
                ringTimerFill.color = isHeadquarters
                    ? new Color(1f, 0.2f, 0.12f, 0.78f)
                    : new Color(1f, 0.86f, 0.22f, 0.72f);
            }

            if (phoneIconLabel != null)
            {
                phoneIconLabel.text = isHeadquarters ? "HQ!!" : "TEL?";
                phoneIconLabel.color = Color.white;
            }

            if (phoneIcon != null)
            {
                phoneIcon.localScale = isHeadquarters ? Vector3.one * 1.18f : Vector3.one;
            }

            UpdateRingFill();
        }

        private void UpdateRingFill()
        {
            if (ringTimerFill != null)
            {
                ringTimerFill.fillAmount = activeAnswerWindow <= 0f
                    ? 0f
                    : Mathf.Clamp01(ringTimer / activeAnswerWindow);
            }
        }

        private void AnimatePhoneIcon()
        {
            if (phoneIcon == null)
            {
                return;
            }

            var isHeadquarters = activeCallType == CallType.Level2_Headquarters;
            var shake = isHeadquarters ? headquartersShakeDegrees : normalShakeDegrees;
            var angle = Mathf.Sin(Time.unscaledTime * shakeSpeed) * shake;
            phoneIcon.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (isHeadquarters)
            {
                var pulse = 1f + Mathf.Sin(Time.unscaledTime * shakeSpeed * 0.7f) * headquartersPulseAmount;
                phoneIcon.localScale = Vector3.one * (1.18f * pulse);
            }
        }

        private void ResetPhoneIcon()
        {
            if (phoneIcon != null)
            {
                phoneIcon.localRotation = Quaternion.identity;
                phoneIcon.localScale = Vector3.one;
            }
        }

        private void PlayRingSound()
        {
            if (audioSource == null)
            {
                return;
            }

            var clip = activeCallType == CallType.Level2_Headquarters && headquartersRingSound != null
                ? headquartersRingSound
                : normalRingSound;
            if (clip == null)
            {
                return;
            }

            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.pitch = activeCallType == CallType.Level2_Headquarters ? headquartersRingPitch : 1f;
            audioSource.Play();
        }

        private void StopRingSound()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.Stop();
            audioSource.clip = null;
            audioSource.loop = false;
            audioSource.pitch = 1f;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void ShowRewardText(string message)
        {
            if (rewardText == null)
            {
                return;
            }

            rewardText.text = message;
            if (rewardTextRoutine != null)
            {
                StopCoroutine(rewardTextRoutine);
            }

            rewardTextRoutine = StartCoroutine(RewardTextRoutine());
        }

        private IEnumerator RewardTextRoutine()
        {
            SetRewardTextVisible(true);
            yield return new WaitForSeconds(1.35f);
            SetRewardTextVisible(false);
            rewardTextRoutine = null;
        }

        private void ShowHqFailedOverlay()
        {
            if (hqFailureRoutine != null)
            {
                StopCoroutine(hqFailureRoutine);
            }

            hqFailureRoutine = StartCoroutine(HqFailedOverlayRoutine());
        }

        private IEnumerator HqFailedOverlayRoutine()
        {
            SetHqFailedOverlay(true);
            var elapsed = 0f;
            const float duration = 3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var flashOn = Mathf.FloorToInt(elapsed * 8f) % 2 == 0;
                if (hqFailedOverlay != null)
                {
                    hqFailedOverlay.SetActive(flashOn);
                }

                yield return null;
            }

            SetHqFailedOverlay(false);
            hqFailureRoutine = null;
        }

        private void SetNotificationVisible(bool visible)
        {
            if (notificationRoot != null)
            {
                notificationRoot.SetActive(visible);
            }

            if (!visible)
            {
                ResetPhoneIcon();
            }
        }

        private void SetRewardTextVisible(bool visible)
        {
            if (rewardText != null)
            {
                rewardText.gameObject.SetActive(visible);
            }
        }

        private void SetHqFailedOverlay(bool visible)
        {
            if (hqFailedOverlay != null)
            {
                hqFailedOverlay.SetActive(visible);
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
            if (notificationRoot != null && answerButton != null && ringTimerFill != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasObject = new GameObject("Two Tier Phone Canvas");
                targetCanvas = canvasObject.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            CreateFallbackUi();
        }

        private void CreateFallbackUi()
        {
            notificationRoot = new GameObject("Two Tier Phone Notification");
            notificationRoot.transform.SetParent(targetCanvas.transform, false);
            var rootRect = notificationRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.anchoredPosition = new Vector2(-28f, -234f);
            rootRect.sizeDelta = new Vector2(118f, 118f);

            phoneBackground = notificationRoot.AddComponent<Image>();
            phoneBackground.color = new Color(0.95f, 0.78f, 0.16f, 0.96f);

            var fillObject = new GameObject("Ring Timer Fill");
            fillObject.transform.SetParent(notificationRoot.transform, false);
            ringTimerFill = fillObject.AddComponent<Image>();
            ringTimerFill.color = new Color(1f, 0.86f, 0.22f, 0.72f);
            ringTimerFill.type = Image.Type.Filled;
            ringTimerFill.fillMethod = Image.FillMethod.Radial360;
            ringTimerFill.fillOrigin = 2;
            ringTimerFill.fillClockwise = false;
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(8f, 8f);
            fillRect.offsetMax = new Vector2(-8f, -8f);

            var buttonObject = new GameObject("Answer Two Tier Phone Button");
            buttonObject.transform.SetParent(notificationRoot.transform, false);
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.1f, 0.18f, 0.25f, 0.95f);
            answerButton = buttonObject.AddComponent<Button>();
            phoneIcon = buttonObject.GetComponent<RectTransform>();
            phoneIcon.anchorMin = new Vector2(0.5f, 0.5f);
            phoneIcon.anchorMax = new Vector2(0.5f, 0.5f);
            phoneIcon.pivot = new Vector2(0.5f, 0.5f);
            phoneIcon.anchoredPosition = Vector2.zero;
            phoneIcon.sizeDelta = new Vector2(82f, 82f);

            var labelObject = new GameObject("Phone Icon Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            phoneIconLabel = labelObject.AddComponent<Text>();
            phoneIconLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            phoneIconLabel.text = "TEL?";
            phoneIconLabel.alignment = TextAnchor.MiddleCenter;
            phoneIconLabel.fontSize = 20;
            phoneIconLabel.fontStyle = FontStyle.Bold;
            phoneIconLabel.color = Color.white;
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var rewardObject = new GameObject("Two Tier Phone Reward Text");
            rewardObject.transform.SetParent(targetCanvas.transform, false);
            rewardText = rewardObject.AddComponent<Text>();
            rewardText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            rewardText.alignment = TextAnchor.MiddleCenter;
            rewardText.fontSize = 26;
            rewardText.fontStyle = FontStyle.Bold;
            rewardText.color = new Color(0.38f, 1f, 0.66f);
            var rewardRect = rewardObject.GetComponent<RectTransform>();
            rewardRect.anchorMin = new Vector2(1f, 1f);
            rewardRect.anchorMax = new Vector2(1f, 1f);
            rewardRect.pivot = new Vector2(1f, 1f);
            rewardRect.anchoredPosition = new Vector2(-156f, -240f);
            rewardRect.sizeDelta = new Vector2(280f, 72f);

            hqFailedOverlay = new GameObject("HQ Audit Failed Overlay");
            hqFailedOverlay.transform.SetParent(targetCanvas.transform, false);
            var overlayImage = hqFailedOverlay.AddComponent<Image>();
            overlayImage.color = new Color(1f, 0.05f, 0.02f, 0.22f);
            overlayImage.raycastTarget = false;
            var overlayRect = hqFailedOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var hqTextObject = new GameObject("HQ Failed Text");
            hqTextObject.transform.SetParent(hqFailedOverlay.transform, false);
            hqFailedText = hqTextObject.AddComponent<Text>();
            hqFailedText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            hqFailedText.text = "HQ AUDIT FAILED";
            hqFailedText.alignment = TextAnchor.MiddleCenter;
            hqFailedText.fontSize = 44;
            hqFailedText.fontStyle = FontStyle.Bold;
            hqFailedText.color = Color.white;
            var hqTextRect = hqTextObject.GetComponent<RectTransform>();
            hqTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            hqTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            hqTextRect.pivot = new Vector2(0.5f, 0.5f);
            hqTextRect.anchoredPosition = Vector2.zero;
            hqTextRect.sizeDelta = new Vector2(620f, 96f);
        }

        private void ResolveMissingReferences()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance ?? FindFirstObjectByType<QueueManager>();
            }

            if (managerSatisfactionSystem == null)
            {
                managerSatisfactionSystem = FindFirstObjectByType<ManagerSatisfactionSystem>();
            }

            if (scoreManager == null)
            {
                scoreManager = ScoreManager.Instance ?? FindFirstObjectByType<ScoreManager>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void SetState(TwoTierPhoneState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            OnStateChanged.Invoke(state);
        }
    }
}
