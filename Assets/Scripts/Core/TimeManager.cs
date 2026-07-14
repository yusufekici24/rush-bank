using TMPro;
using RushBank.Gameplay;
using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Core
{
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("Timer")]
        [SerializeField, Min(1f)] private float timeLimitSeconds = 60f;
        [SerializeField] private bool startOnAwake = true;

        [Header("UI")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private GameObject freezeLockIcon;
        [SerializeField] private Color normalTimerColor = Color.white;
        [SerializeField] private Color frozenTimerColor = new Color(0.35f, 0.75f, 1f);
        [SerializeField, Min(1f)] private float feedbackScale = 1.18f;
        [SerializeField, Min(0.01f)] private float feedbackDuration = 0.16f;

        public UnityEvent OnGameOver = new UnityEvent();
        public FloatEvent OnRemainingTimeChanged = new FloatEvent();
        public FloatEvent OnTimeAdded = new FloatEvent();

        private readonly char[] timeBuffer = { '0', '1', ':', '0', '0' };
        private float remainingSeconds;
        private float feedbackTimer;
        private int lastDisplayedSecond = -1;
        private bool isRunning;
        private bool gameOverRaised;
        private bool isTimeFrozen;
        private Vector3 baseTextScale = Vector3.one;
        private Color baseTextColor = Color.white;
        private float countdownRateMultiplier = 1f;

        public float RemainingSeconds => remainingSeconds;
        public float TimeLimitSeconds => timeLimitSeconds;
        public bool IsRunning => isRunning;
        public bool IsTimeFrozen => isTimeFrozen;
        public float CountdownRateMultiplier
        {
            get => countdownRateMultiplier;
            set => countdownRateMultiplier = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            remainingSeconds = timeLimitSeconds;

            if (timerText != null)
            {
                baseTextScale = timerText.transform.localScale;
                baseTextColor = timerText.color;
                normalTimerColor = baseTextColor;
            }

            ApplyFreezeVisualState();
            UpdateTimerText(true);
            ApplySelectedBranchSettings();

            if (startOnAwake)
            {
                StartTimer();
            }
        }

        private void Update()
        {
            TickTimer();
            TickFeedback();
        }

        public void StartTimer()
        {
            isRunning = true;
        }

        public void PauseTimer()
        {
            isRunning = false;
        }

        public void FreezeTime(bool freeze)
        {
            if (isTimeFrozen == freeze)
            {
                return;
            }

            isTimeFrozen = freeze;
            ApplyFreezeVisualState();
        }

        public void ResetTimer()
        {
            remainingSeconds = timeLimitSeconds;
            lastDisplayedSecond = -1;
            gameOverRaised = false;
            isTimeFrozen = false;
            isRunning = startOnAwake;
            countdownRateMultiplier = 1f;
            ApplySelectedBranchSettings();
            UpdateTimerText(true);
            ResetFeedbackScale();
            ApplyFreezeVisualState();
        }

        public void ApplySelectedBranchSettings()
        {
            if (GameSettingsManager.Instance == null)
            {
                return;
            }

            countdownRateMultiplier *= GameSettingsManager.Instance.GetTimerPressureMultiplier();
        }

        public void AddTime(float seconds)
        {
            if (seconds <= 0f || gameOverRaised)
            {
                return;
            }

            remainingSeconds += seconds;
            UpdateTimerText(true);
            PlayTimeFeedback();
            OnTimeAdded.Invoke(seconds);
            OnRemainingTimeChanged.Invoke(remainingSeconds);
        }

        public void SubtractTime(float seconds)
        {
            if (seconds <= 0f || gameOverRaised)
            {
                return;
            }

            remainingSeconds = Mathf.Max(0f, remainingSeconds - seconds);
            UpdateTimerText(true);
            OnRemainingTimeChanged.Invoke(remainingSeconds);

            if (remainingSeconds <= 0f)
            {
                TriggerGameOver();
            }
        }

        private void TickTimer()
        {
            if (!isRunning || gameOverRaised || isTimeFrozen)
            {
                return;
            }

            remainingSeconds -= Time.deltaTime * countdownRateMultiplier;
            if (remainingSeconds <= 0f)
            {
                remainingSeconds = 0f;
                UpdateTimerText(true);
                OnRemainingTimeChanged.Invoke(remainingSeconds);
                TriggerGameOver();
                return;
            }

            UpdateTimerText(false);
            OnRemainingTimeChanged.Invoke(remainingSeconds);
        }

        private void UpdateTimerText(bool force)
        {
            if (timerText == null)
            {
                return;
            }

            var displaySeconds = Mathf.CeilToInt(remainingSeconds);
            if (!force && displaySeconds == lastDisplayedSecond)
            {
                return;
            }

            lastDisplayedSecond = displaySeconds;
            var minutes = displaySeconds / 60;
            var seconds = displaySeconds - (minutes * 60);

            if (minutes > 99)
            {
                minutes = 99;
            }

            timeBuffer[0] = (char)('0' + (minutes / 10));
            timeBuffer[1] = (char)('0' + (minutes % 10));
            timeBuffer[3] = (char)('0' + (seconds / 10));
            timeBuffer[4] = (char)('0' + (seconds % 10));
            timerText.SetCharArray(timeBuffer, 0, timeBuffer.Length);
        }

        private void TriggerGameOver()
        {
            if (gameOverRaised)
            {
                return;
            }

            isRunning = false;
            gameOverRaised = true;
            OnGameOver.Invoke();
        }

        private void PlayTimeFeedback()
        {
            if (timerText == null)
            {
                return;
            }

            feedbackTimer = feedbackDuration;
        }

        private void TickFeedback()
        {
            if (timerText == null || feedbackTimer <= 0f)
            {
                return;
            }

            feedbackTimer -= Time.deltaTime;
            var progress = 1f - Mathf.Clamp01(feedbackTimer / feedbackDuration);
            var wave = Mathf.Sin(progress * Mathf.PI);
            var scale = Mathf.Lerp(1f, feedbackScale, wave);
            timerText.transform.localScale = baseTextScale * scale;

            if (feedbackTimer <= 0f)
            {
                ResetFeedbackScale();
            }
        }

        private void ResetFeedbackScale()
        {
            if (timerText != null)
            {
                timerText.transform.localScale = baseTextScale;
            }
        }

        private void ApplyFreezeVisualState()
        {
            if (timerText != null)
            {
                timerText.color = isTimeFrozen ? frozenTimerColor : normalTimerColor;
            }

            if (freezeLockIcon != null)
            {
                freezeLockIcon.SetActive(isTimeFrozen);
            }
        }
    }
}
