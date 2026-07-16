using System.Collections;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum PhoneInterruptionState
    {
        Silent,
        Ringing,
        Talking,
        Cooldown
    }

    public class PhoneInterruptionSystem : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private Vector2 ringIntervalSeconds = new Vector2(30f, 45f);
        [SerializeField, Min(0.5f)] private float ringTimeout = 4f;
        [SerializeField, Min(0.1f)] private float callDuration = 2f;
        [SerializeField, Min(0f)] private float cooldownSeconds = 8f;

        [Header("Reward")]
        [SerializeField, Min(0f)] private float basePhoneReward = 4f;
        [SerializeField] private float perfectAnswerSeconds = 1f;
        [SerializeField] private float fastAnswerSeconds = 2.5f;
        [SerializeField] private float perfectMultiplier = 2f;
        [SerializeField] private float fastMultiplier = 1.5f;
        [SerializeField] private float slowMultiplier = 1.1f;

        [Header("UI")]
        [SerializeField] private GameObject notificationRoot;
        [SerializeField] private Button answerButton;
        [SerializeField] private Image ringTimeoutFill;
        [SerializeField] private RectTransform shakingPhoneIcon;
        [SerializeField] private Text rewardFloatingText;
        [SerializeField, Min(0f)] private float shakeDegrees = 10f;
        [SerializeField, Min(0f)] private float shakeSpeed = 28f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip ringClip;
        [SerializeField] private AudioClip gibberishDialogueClip;
        [SerializeField] private AudioClip busyLineClip;

        [Header("During Call Restrictions")]
        [SerializeField] private Selectable[] blockedControlsDuringCall;

        public UnityEvent<PhoneInterruptionState> OnStateChanged = new UnityEvent<PhoneInterruptionState>();
        public FloatEvent OnPhoneRewardGranted = new FloatEvent();
        public UnityEvent OnPhoneMissed = new UnityEvent();

        private PhoneInterruptionState state = PhoneInterruptionState.Silent;
        private bool[] blockedControlPreviousStates = System.Array.Empty<bool>();
        private float nextRingTimer;
        private float ringElapsed;
        private float callElapsed;
        private float cooldownTimer;
        private float currentMultiplier = 1f;
        private Coroutine floatingTextRoutine;
        private bool controlsBlocked;
        private bool timeFrozenByPhone;

        public PhoneInterruptionState State => state;
        public bool IsActive => state == PhoneInterruptionState.Ringing || state == PhoneInterruptionState.Talking;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (answerButton != null)
            {
                answerButton.onClick.AddListener(AnswerPhone);
            }

            SetNotificationVisible(false);
            SetRewardTextVisible(false);
            ResetNextRingTimer();
        }

        private void OnDestroy()
        {
            if (answerButton != null)
            {
                answerButton.onClick.RemoveListener(AnswerPhone);
            }

            SetControlsBlocked(false);
            FreezeGameTime(false);
        }

        private void Update()
        {
            switch (state)
            {
                case PhoneInterruptionState.Silent:
                    TickSilent();
                    break;
                case PhoneInterruptionState.Ringing:
                    TickRinging();
                    break;
                case PhoneInterruptionState.Talking:
                    TickTalking();
                    break;
                case PhoneInterruptionState.Cooldown:
                    TickCooldown();
                    break;
            }
        }

        public void ForceRing()
        {
            if (state == PhoneInterruptionState.Silent || state == PhoneInterruptionState.Cooldown)
            {
                StartRinging();
            }
        }

        public void AnswerPhone()
        {
            if (state != PhoneInterruptionState.Ringing)
            {
                return;
            }

            currentMultiplier = CalculateMultiplier(ringElapsed);
            callElapsed = 0f;
            StopRingSound();
            PlayOneShot(gibberishDialogueClip);
            FreezeGameTime(true);
            SetControlsBlocked(true);
            SetState(PhoneInterruptionState.Talking);
        }

        private void TickSilent()
        {
            nextRingTimer -= Time.deltaTime;
            if (nextRingTimer <= 0f)
            {
                StartRinging();
            }
        }

        private void TickRinging()
        {
            ringElapsed += Time.deltaTime;
            UpdateRingTimeoutFill();
            ShakePhoneIcon();

            if (ringElapsed >= ringTimeout)
            {
                MissCall();
            }
        }

        private void TickTalking()
        {
            callElapsed += Time.deltaTime;
            if (callElapsed >= callDuration)
            {
                CompleteCall();
            }
        }

        private void TickCooldown()
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                ResetNextRingTimer();
                SetState(PhoneInterruptionState.Silent);
            }
        }

        private void StartRinging()
        {
            ringElapsed = 0f;
            currentMultiplier = 1f;
            SetNotificationVisible(true);
            SetRewardTextVisible(false);
            UpdateRingTimeoutFill();
            PlayRingSound();
            SetState(PhoneInterruptionState.Ringing);
        }

        private void CompleteCall()
        {
            var reward = basePhoneReward * currentMultiplier;
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.AddTime(reward);
            }

            ShowRewardText(reward, GetReactionLabel(currentMultiplier));
            OnPhoneRewardGranted.Invoke(reward);
            FinishPhoneEvent();
        }

        private void MissCall()
        {
            StopRingSound();
            PlayOneShot(busyLineClip);
            SetNotificationVisible(false);
            SetRewardTextVisible(false);
            OnPhoneMissed.Invoke();
            StartCooldown();
        }

        private void FinishPhoneEvent()
        {
            FreezeGameTime(false);
            SetControlsBlocked(false);
            StopRingSound();
            SetNotificationVisible(false);
            StartCooldown();
        }

        private void StartCooldown()
        {
            cooldownTimer = cooldownSeconds;
            SetState(PhoneInterruptionState.Cooldown);
        }

        private float CalculateMultiplier(float answerSeconds)
        {
            if (answerSeconds < perfectAnswerSeconds)
            {
                return perfectMultiplier;
            }

            if (answerSeconds < fastAnswerSeconds)
            {
                return fastMultiplier;
            }

            return slowMultiplier;
        }

        private string GetReactionLabel(float multiplier)
        {
            if (Mathf.Approximately(multiplier, perfectMultiplier))
            {
                return "Perfect!";
            }

            if (Mathf.Approximately(multiplier, fastMultiplier))
            {
                return "Fast!";
            }

            return "Answered!";
        }

        private void ResetNextRingTimer()
        {
            var min = Mathf.Min(ringIntervalSeconds.x, ringIntervalSeconds.y);
            var max = Mathf.Max(ringIntervalSeconds.x, ringIntervalSeconds.y);
            nextRingTimer = Random.Range(min, max);
        }

        private void UpdateRingTimeoutFill()
        {
            if (ringTimeoutFill != null)
            {
                ringTimeoutFill.fillAmount = 1f - Mathf.Clamp01(ringElapsed / ringTimeout);
            }
        }

        private void ShakePhoneIcon()
        {
            if (shakingPhoneIcon == null)
            {
                return;
            }

            var angle = Mathf.Sin(Time.unscaledTime * shakeSpeed) * shakeDegrees;
            shakingPhoneIcon.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void ResetPhoneIcon()
        {
            if (shakingPhoneIcon != null)
            {
                shakingPhoneIcon.localRotation = Quaternion.identity;
            }
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

        private void ShowRewardText(float rewardSeconds, string label)
        {
            if (rewardFloatingText == null)
            {
                return;
            }

            rewardFloatingText.text = $"+{Mathf.RoundToInt(rewardSeconds)}s ({label})";
            if (floatingTextRoutine != null)
            {
                StopCoroutine(floatingTextRoutine);
            }

            floatingTextRoutine = StartCoroutine(RewardTextRoutine());
        }

        private IEnumerator RewardTextRoutine()
        {
            SetRewardTextVisible(true);
            var timer = 0f;
            const float visibleSeconds = 1.1f;
            var rect = rewardFloatingText.rectTransform;
            var start = rect.anchoredPosition;

            while (timer < visibleSeconds)
            {
                timer += Time.unscaledDeltaTime;
                rect.anchoredPosition = start + Vector2.up * (timer / visibleSeconds * 34f);
                yield return null;
            }

            rect.anchoredPosition = start;
            SetRewardTextVisible(false);
            floatingTextRoutine = null;
        }

        private void SetRewardTextVisible(bool visible)
        {
            if (rewardFloatingText != null)
            {
                rewardFloatingText.gameObject.SetActive(visible);
            }
        }

        private void SetControlsBlocked(bool blocked)
        {
            if (controlsBlocked == blocked)
            {
                return;
            }

            if (blockedControlsDuringCall == null || blockedControlsDuringCall.Length == 0)
            {
                controlsBlocked = blocked;
                return;
            }

            if (blocked)
            {
                controlsBlocked = true;
                blockedControlPreviousStates = new bool[blockedControlsDuringCall.Length];
                for (var i = 0; i < blockedControlsDuringCall.Length; i++)
                {
                    var control = blockedControlsDuringCall[i];
                    if (control == null)
                    {
                        continue;
                    }

                    blockedControlPreviousStates[i] = control.interactable;
                    control.interactable = false;
                }

                return;
            }

            for (var i = 0; i < blockedControlsDuringCall.Length; i++)
            {
                var control = blockedControlsDuringCall[i];
                if (control == null)
                {
                    continue;
                }

                control.interactable = i < blockedControlPreviousStates.Length && blockedControlPreviousStates[i];
            }

            blockedControlPreviousStates = System.Array.Empty<bool>();
            controlsBlocked = false;
        }

        private void FreezeGameTime(bool freeze)
        {
            if (TimeManager.Instance == null)
            {
                return;
            }

            if (freeze)
            {
                TimeManager.Instance.FreezeTime(true);
                timeFrozenByPhone = true;
                return;
            }

            if (timeFrozenByPhone)
            {
                TimeManager.Instance.FreezeTime(false);
                timeFrozenByPhone = false;
            }
        }

        private void PlayRingSound()
        {
            if (audioSource == null || ringClip == null)
            {
                return;
            }

            audioSource.clip = ringClip;
            audioSource.loop = true;
            audioSource.Play();
        }

        private void StopRingSound()
        {
            if (audioSource == null || audioSource.clip != ringClip)
            {
                return;
            }

            audioSource.Stop();
            audioSource.clip = null;
            audioSource.loop = false;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void SetState(PhoneInterruptionState nextState)
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
