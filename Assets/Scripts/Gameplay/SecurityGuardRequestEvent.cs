using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum SecurityGuardRequestState
    {
        Idle,
        WalkingToCounter,
        WaitingForGear,
        ReturningToPatrol,
        Cooldown
    }

    public class SecurityGuardRequestEvent : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private Vector2 requestIntervalSeconds = new Vector2(70f, 90f);
        [SerializeField, Min(1f)] private float waitAtCounterSeconds = 15f;
        [SerializeField, Min(0.1f)] private float fallbackMoveSpeed = 2.8f;

        [Header("Scene References")]
        [SerializeField] private SecurityGuardAI securityGuardAI;
        [SerializeField] private NavMeshAgent guardAgent;
        [SerializeField] private Animator guardAnimator;
        [SerializeField] private Transform lobbyPatrolArea;
        [SerializeField] private Transform playerCounterLocation;
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private ManagerSatisfactionSystem managerSatisfactionSystem;
        [SerializeField] private ScammerDetectionSystem scammerDetectionSystem;

        [Header("UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Button chargeRadioButton;
        [SerializeField] private Text chargeButtonLabel;

        [Header("Visuals")]
        [SerializeField] private GameObject radioIconPrefab;
        [SerializeField] private ParticleSystem vigilanceParticlePrefab;
        [SerializeField] private Transform vigilanceParticleAnchor;
        [SerializeField] private string saluteAnimationTrigger = "Salute";
        [SerializeField] private string sighAnimationTrigger = "Sigh";

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip requestRadioSound;
        [SerializeField] private AudioClip chargeRadioSound;
        [SerializeField] private AudioClip ignoredSighSound;

        [Header("Rewards")]
        [SerializeField, Min(0)] private int goldReward = 50;
        [SerializeField, Min(0f)] private float satisfactionReward = 10f;
        [SerializeField, Min(0f)] private float ignoredSatisfactionPenalty = 15f;
        [SerializeField, Min(0.1f)] private float vigilanceBoostSeconds = 25f;
        [SerializeField, Range(0.05f, 1f)] private float vigilancePatienceDrainMultiplier = 0.7f;

        public UnityEvent OnGuardRequestStarted = new UnityEvent();
        public UnityEvent OnGuardRequestCompleted = new UnityEvent();
        public UnityEvent OnGuardRequestIgnored = new UnityEvent();
        public UnityEvent OnVigilanceBoostStarted = new UnityEvent();
        public UnityEvent<SecurityGuardRequestState> OnStateChanged = new UnityEvent<SecurityGuardRequestState>();

        private GameObject activeIcon;
        private ParticleSystem activeVigilanceParticle;
        private Coroutine requestRoutine;
        private float nextRequestTimer;
        private SecurityGuardRequestState state = SecurityGuardRequestState.Idle;

        public SecurityGuardRequestState State => state;
        public bool IsWaitingForGear => state == SecurityGuardRequestState.WaitingForGear;

        private void Awake()
        {
            ResolveMissingReferences();
            EnsureChargeButton();
            SetChargeButtonVisible(false);
            ResetRequestTimer();
        }

        private void OnEnable()
        {
            if (chargeRadioButton != null)
            {
                chargeRadioButton.onClick.AddListener(ResolveGuardRequest);
            }
        }

        private void OnDisable()
        {
            if (chargeRadioButton != null)
            {
                chargeRadioButton.onClick.RemoveListener(ResolveGuardRequest);
            }

            if (requestRoutine != null)
            {
                StopCoroutine(requestRoutine);
                requestRoutine = null;
            }

            ClearIcon();
            SetChargeButtonVisible(false);
        }

        private void Update()
        {
            if (requestRoutine != null)
            {
                return;
            }

            if (securityGuardAI != null && securityGuardAI.IsBusy)
            {
                return;
            }

            nextRequestTimer -= Time.deltaTime;
            if (nextRequestTimer <= 0f)
            {
                requestRoutine = StartCoroutine(GuardRequestRoutine());
            }
        }

        public void ForceStartGuardRequest()
        {
            if (requestRoutine != null || (securityGuardAI != null && securityGuardAI.IsBusy))
            {
                return;
            }

            requestRoutine = StartCoroutine(GuardRequestRoutine());
        }

        public void ResolveGuardRequest()
        {
            if (state != SecurityGuardRequestState.WaitingForGear)
            {
                return;
            }

            PlaySound(chargeRadioSound);
            TriggerAnimator(saluteAnimationTrigger);
            AddGold(goldReward);
            managerSatisfactionSystem?.AddSatisfaction(satisfactionReward);
            ApplyVigilanceBoost();
            OnGuardRequestCompleted.Invoke();
            SetChargeButtonVisible(false);
            ClearIcon();
            SetState(SecurityGuardRequestState.ReturningToPatrol);
        }

        private IEnumerator GuardRequestRoutine()
        {
            ResolveMissingReferences();
            if (securityGuardAI == null)
            {
                FinishEvent();
                yield break;
            }

            SetState(SecurityGuardRequestState.WalkingToCounter);
            yield return MoveGuardTo(GetCounterPosition());

            SetState(SecurityGuardRequestState.WaitingForGear);
            SpawnRadioIcon();
            SetChargeButtonVisible(true);
            PlaySound(requestRadioSound);
            OnGuardRequestStarted.Invoke();

            var waitTimer = waitAtCounterSeconds;
            while (waitTimer > 0f && state == SecurityGuardRequestState.WaitingForGear)
            {
                waitTimer -= Time.deltaTime;
                yield return null;
            }

            if (state == SecurityGuardRequestState.WaitingForGear)
            {
                HandleIgnoredRequest();
            }

            yield return MoveGuardTo(GetPatrolPosition());
            SetState(SecurityGuardRequestState.Cooldown);
            FinishEvent();
        }

        private void HandleIgnoredRequest()
        {
            SetChargeButtonVisible(false);
            ClearIcon();
            TriggerAnimator(sighAnimationTrigger);
            PlaySound(ignoredSighSound);
            managerSatisfactionSystem?.DeductSatisfaction(ignoredSatisfactionPenalty);
            OnGuardRequestIgnored.Invoke();
            SetState(SecurityGuardRequestState.ReturningToPatrol);
        }

        private void ApplyVigilanceBoost()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance;
            }

            queueManager?.ApplyQueueReliefBoost(0f, vigilancePatienceDrainMultiplier, vigilanceBoostSeconds);

            if (scammerDetectionSystem == null)
            {
                scammerDetectionSystem = FindFirstObjectByType<ScammerDetectionSystem>();
            }

            scammerDetectionSystem?.EnableDiscrepancyAutoHighlight(vigilanceBoostSeconds);
            SpawnVigilanceEffect();
            OnVigilanceBoostStarted.Invoke();
        }

        private IEnumerator MoveGuardTo(Vector3 destination)
        {
            if (securityGuardAI == null)
            {
                yield break;
            }

            var guardTransform = securityGuardAI.transform;
            if (guardAgent != null && guardAgent.enabled && guardAgent.isOnNavMesh)
            {
                guardAgent.SetDestination(destination);
                while (guardAgent.pathPending)
                {
                    yield return null;
                }

                while (guardAgent.enabled && guardAgent.isOnNavMesh && guardAgent.remainingDistance > Mathf.Max(guardAgent.stoppingDistance, 0.12f))
                {
                    yield return null;
                }

                yield break;
            }

            while ((guardTransform.position - destination).sqrMagnitude > 0.04f)
            {
                var current = guardTransform.position;
                guardTransform.position = Vector3.MoveTowards(current, destination, fallbackMoveSpeed * Time.deltaTime);
                var direction = destination - current;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    guardTransform.rotation = Quaternion.Slerp(
                        guardTransform.rotation,
                        Quaternion.LookRotation(direction.normalized, Vector3.up),
                        10f * Time.deltaTime);
                }

                yield return null;
            }
        }

        private void SpawnRadioIcon()
        {
            ClearIcon();
            if (securityGuardAI == null)
            {
                return;
            }

            var anchor = securityGuardAI.transform;
            if (radioIconPrefab != null)
            {
                activeIcon = Instantiate(radioIconPrefab, anchor);
                activeIcon.transform.localPosition = Vector3.up * 1.9f;
                return;
            }

            activeIcon = new GameObject("Security Radio Request Icon");
            activeIcon.transform.SetParent(anchor, false);
            activeIcon.transform.localPosition = Vector3.up * 1.9f;
            activeIcon.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var label = activeIcon.AddComponent<TextMesh>();
            label.text = "RADIO!";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.18f;
            label.color = new Color(0.2f, 0.58f, 1f);
        }

        private void ClearIcon()
        {
            if (activeIcon != null)
            {
                Destroy(activeIcon);
                activeIcon = null;
            }
        }

        private void SpawnVigilanceEffect()
        {
            if (vigilanceParticlePrefab == null)
            {
                return;
            }

            if (activeVigilanceParticle != null)
            {
                Destroy(activeVigilanceParticle.gameObject);
            }

            var anchor = vigilanceParticleAnchor != null
                ? vigilanceParticleAnchor
                : securityGuardAI != null
                    ? securityGuardAI.transform
                    : transform;
            activeVigilanceParticle = Instantiate(vigilanceParticlePrefab, anchor);
            activeVigilanceParticle.transform.localPosition = Vector3.up * 1.2f;
            activeVigilanceParticle.Play();
            Destroy(activeVigilanceParticle.gameObject, vigilanceBoostSeconds);
        }

        private void EnsureChargeButton()
        {
            if (chargeRadioButton != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasObject = new GameObject("Security Guard Request Canvas");
                targetCanvas = canvasObject.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            var buttonObject = new GameObject("Charge Radio Button");
            buttonObject.transform.SetParent(targetCanvas.transform, false);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-28f, -188f);
            rect.sizeDelta = new Vector2(220f, 58f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.14f, 0.46f, 0.95f, 0.96f);
            chargeRadioButton = buttonObject.AddComponent<Button>();

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            chargeButtonLabel = labelObject.AddComponent<Text>();
            chargeButtonLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            chargeButtonLabel.text = "CHARGE RADIO";
            chargeButtonLabel.alignment = TextAnchor.MiddleCenter;
            chargeButtonLabel.fontSize = 19;
            chargeButtonLabel.fontStyle = FontStyle.Bold;
            chargeButtonLabel.color = Color.white;

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private void SetChargeButtonVisible(bool visible)
        {
            if (chargeRadioButton != null)
            {
                chargeRadioButton.gameObject.SetActive(visible);
            }
        }

        private void FinishEvent()
        {
            ClearIcon();
            SetChargeButtonVisible(false);
            ResetRequestTimer();
            SetState(SecurityGuardRequestState.Idle);
            requestRoutine = null;
        }

        private void ResetRequestTimer()
        {
            var min = Mathf.Max(1f, requestIntervalSeconds.x);
            var max = Mathf.Max(min, requestIntervalSeconds.y);
            nextRequestTimer = Random.Range(min, max);
        }

        private void ResolveMissingReferences()
        {
            if (securityGuardAI == null)
            {
                securityGuardAI = FindFirstObjectByType<SecurityGuardAI>();
            }

            if (securityGuardAI != null)
            {
                if (guardAgent == null)
                {
                    guardAgent = securityGuardAI.GetComponent<NavMeshAgent>();
                }

                if (guardAnimator == null)
                {
                    guardAnimator = securityGuardAI.GetComponentInChildren<Animator>();
                }
            }

            if (queueManager == null)
            {
                queueManager = QueueManager.Instance ?? FindFirstObjectByType<QueueManager>();
            }

            if (managerSatisfactionSystem == null)
            {
                managerSatisfactionSystem = FindFirstObjectByType<ManagerSatisfactionSystem>();
            }

            if (scammerDetectionSystem == null)
            {
                scammerDetectionSystem = FindFirstObjectByType<ScammerDetectionSystem>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private Vector3 GetPatrolPosition()
        {
            return lobbyPatrolArea != null
                ? lobbyPatrolArea.position
                : securityGuardAI != null
                    ? securityGuardAI.transform.position
                    : transform.position;
        }

        private Vector3 GetCounterPosition()
        {
            return playerCounterLocation != null ? playerCounterLocation.position : transform.position + Vector3.forward * 2f;
        }

        private void SetState(SecurityGuardRequestState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            OnStateChanged.Invoke(state);
        }

        private void TriggerAnimator(string triggerName)
        {
            if (guardAnimator != null && !string.IsNullOrWhiteSpace(triggerName))
            {
                guardAnimator.SetTrigger(triggerName);
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
    }
}
