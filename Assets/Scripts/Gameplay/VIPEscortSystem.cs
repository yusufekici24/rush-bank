using System.Collections;
using RushBank.Core;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace RushBank.Gameplay
{
    public enum VIPEscortState
    {
        WaitForPlayer,
        EscortingToManager,
        MeetingStarted,
        ReturningToCounter
    }

    public class VIPEscortSystem : MonoBehaviour
    {
        [Header("Actors")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform vipCustomer;
        [SerializeField] private NavMeshAgent vipAgent;
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private MobilePlayerController mobilePlayerController;
        [SerializeField] private ChubbyTopDownInputController topDownInputController;

        [Header("Interaction")]
        [SerializeField] private InputActionReference actionInput;
        [SerializeField, Min(0.1f)] private float interactionDistance = 1.8f;
        [SerializeField, Min(0.1f)] private float followDistance = 1.5f;
        [SerializeField, Min(0.1f)] private float followRefreshInterval = 0.12f;
        [SerializeField, Min(0.1f)] private float fallbackMoveSpeed = 2.3f;
        [SerializeField] private Key editorActionKey = Key.E;

        [Header("Triangle Route")]
        [SerializeField] private Transform playerCounter;
        [SerializeField] private Transform vipWaitingSpot;
        [SerializeField] private Transform managerRoomEntrance;
        [SerializeField] private Collider vipWaitingSpotZone;
        [SerializeField] private Collider managerRoomEntranceZone;
        [SerializeField] private Transform managerRoomInsidePoint;
        [SerializeField] private GameObject managerDoorVisual;

        [Header("Legacy Route Aliases")]
        [SerializeField] private Collider vipCounterZone;
        [SerializeField] private Collider safeDepositVaultZone;
        [SerializeField] private Collider bankExitZone;
        [SerializeField] private Transform vaultInsidePoint;
        [SerializeField] private Transform vaultExitPoint;

        [Header("Timing and Reward")]
        [SerializeField, Min(0.1f)] private float managerMeetingStartSeconds = 0.25f;
        [SerializeField, Min(0f)] private float completionTimeReward = 15f;
        [SerializeField] private ParticleSystem completionEffect;

        [Header("VIP Relief Boost")]
        [SerializeField] private bool applyReliefBoostOnManagerArrival = true;
        [SerializeField, Range(0f, 1f)] private float patienceRestorePercent = 0f;
        [SerializeField, Range(0f, 1f)] private float temporaryPatienceDrainMultiplier = 0.5f;
        [SerializeField, Min(0f)] private float reliefBoostSeconds = 6f;

        [Header("Managerial Praise Boost")]
        [SerializeField] private AudioSource praiseAudioSource;
        [SerializeField] private AudioClip praiseDingClip;
        [SerializeField] private Transform praiseBubbleAnchor;
        [SerializeField] private GameObject praiseBubblePrefab;
        [SerializeField] private string praiseBubbleText = "BRAVO!";
        [SerializeField, Min(0.1f)] private float praiseBubbleSeconds = 1.4f;
        [SerializeField] private ParticleSystem praiseEffectPrefab;
        [SerializeField] private ParticleSystem playerPraiseEffect;
        [SerializeField, Min(0.1f)] private float praiseEffectSeconds = 1.2f;
        [SerializeField, Min(1f)] private float playerSpeedBoostMultiplier = 1.2f;
        [SerializeField, Min(0f)] private float playerSpeedBoostSeconds = 5f;

        [Header("Animation")]
        [SerializeField] private Animator vipAnimator;
        [SerializeField] private string walkTrigger = "Walk";
        [SerializeField] private string waitTrigger = "Wait";
        [SerializeField] private string richWaveTrigger = "RichWave";
        [SerializeField] private string celebrateTrigger = "Celebrate";

        public UnityEvent<VIPEscortState> OnStateChanged = new UnityEvent<VIPEscortState>();
        public UnityEvent OnEscortStarted = new UnityEvent();
        public UnityEvent OnManagerMeetingStarted = new UnityEvent();
        public UnityEvent OnEscortCompleted = new UnityEvent();

        private Coroutine managerRoutine;
        private Coroutine speedBoostRoutine;
        private Coroutine praiseBubbleRoutine;
        private Coroutine praiseEffectRoutine;
        private float nextFollowRefreshTime;
        private bool completed;
        private bool praiseSpeedBoostApplied;
        private static AudioClip fallbackPraiseDingClip;

        public VIPEscortState State { get; private set; } = VIPEscortState.WaitForPlayer;
        public bool IsEscorting => State == VIPEscortState.EscortingToManager;
        public Transform PlayerCounter => playerCounter;
        public Transform VIPWaitingSpot => vipWaitingSpot;
        public Transform ManagerRoomEntrance => managerRoomEntrance;

        private void Awake()
        {
            if (vipCustomer == null)
            {
                vipCustomer = transform;
            }

            if (vipAgent == null && vipCustomer != null)
            {
                vipAgent = vipCustomer.GetComponent<NavMeshAgent>();
            }

            if (vipAnimator == null && vipCustomer != null)
            {
                vipAnimator = vipCustomer.GetComponentInChildren<Animator>();
            }

            if (queueManager == null)
            {
                queueManager = QueueManager.Instance;
            }

            if (mobilePlayerController == null && player != null)
            {
                mobilePlayerController = player.GetComponent<MobilePlayerController>();
            }

            if (topDownInputController == null && player != null)
            {
                topDownInputController = player.GetComponent<ChubbyTopDownInputController>();
            }

            if (praiseAudioSource == null)
            {
                praiseAudioSource = GetComponent<AudioSource>();
            }

            ResolveLegacyRouteAliases();
        }

        private void OnEnable()
        {
            if (actionInput != null && actionInput.action != null)
            {
                actionInput.action.Enable();
                actionInput.action.performed += OnActionPerformed;
            }

            SetState(VIPEscortState.WaitForPlayer);
        }

        private void OnDisable()
        {
            if (actionInput != null && actionInput.action != null)
            {
                actionInput.action.performed -= OnActionPerformed;
            }

            if (speedBoostRoutine != null)
            {
                StopCoroutine(speedBoostRoutine);
                RestorePlayerSpeedBoost();
                speedBoostRoutine = null;
            }

            if (praiseBubbleRoutine != null)
            {
                StopCoroutine(praiseBubbleRoutine);
                praiseBubbleRoutine = null;
            }

            if (praiseEffectRoutine != null)
            {
                StopCoroutine(praiseEffectRoutine);
                praiseEffectRoutine = null;
            }

            if (playerPraiseEffect != null)
            {
                playerPraiseEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[editorActionKey].wasPressedThisFrame)
            {
                Action();
            }

            if (!IsEscorting || vipCustomer == null)
            {
                return;
            }

            if (Time.time >= nextFollowRefreshTime)
            {
                nextFollowRefreshTime = Time.time + followRefreshInterval;
                FollowPlayer();
            }

            if (State == VIPEscortState.EscortingToManager && IsAtManagerRoomEntrance())
            {
                StartManagerMeeting();
            }
        }

        public void Action()
        {
            if (completed || State != VIPEscortState.WaitForPlayer || !CanStartEscort())
            {
                return;
            }

            StartEscort();
        }

        public void StartEscort()
        {
            SetState(VIPEscortState.EscortingToManager);
            ConfigureVipAgentForEscort();
            OnEscortStarted.Invoke();
            PlayTrigger(walkTrigger);
            FollowPlayer(true);
        }

        public void StartEscortToVault()
        {
            StartEscort();
        }

        private bool CanStartEscort()
        {
            if (player == null || vipCustomer == null)
            {
                return false;
            }

            if (!IsVipAtWaitingSpot())
            {
                return false;
            }

            return (player.position - vipCustomer.position).sqrMagnitude <= interactionDistance * interactionDistance;
        }

        private void FollowPlayer(bool force = false)
        {
            if (player == null || vipCustomer == null)
            {
                return;
            }

            var directionFromPlayer = vipCustomer.position - player.position;
            directionFromPlayer.y = 0f;
            if (directionFromPlayer.sqrMagnitude < 0.01f)
            {
                directionFromPlayer = -player.forward;
            }

            var targetPosition = player.position + directionFromPlayer.normalized * followDistance;
            MoveVipTo(targetPosition, force);
        }

        private void StartManagerMeeting()
        {
            if (managerRoutine != null)
            {
                StopCoroutine(managerRoutine);
            }

            managerRoutine = StartCoroutine(ManagerMeetingRoutine());
        }

        private IEnumerator ManagerMeetingRoutine()
        {
            SetState(VIPEscortState.MeetingStarted);
            OnManagerMeetingStarted.Invoke();
            PlayTrigger(richWaveTrigger);
            StopVipMovement();

            var insidePoint = managerRoomInsidePoint != null ? managerRoomInsidePoint : vaultInsidePoint;
            if (insidePoint != null)
            {
                MoveVipTo(insidePoint.position, true);
            }

            if (managerDoorVisual != null)
            {
                managerDoorVisual.SetActive(true);
            }

            ApplyReliefBoost();
            ApplyPraiseBoost();

            var elapsed = 0f;
            while (elapsed < managerMeetingStartSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            CompleteEscort();
            managerRoutine = null;
        }

        private void CompleteEscort()
        {
            if (completed)
            {
                return;
            }

            completed = true;
            StopVipMovement();
            PlayTrigger(celebrateTrigger);
            SetState(VIPEscortState.ReturningToCounter);

            if (TimeManager.Instance != null && completionTimeReward > 0f)
            {
                TimeManager.Instance.AddTime(completionTimeReward);
            }

            if (completionEffect != null)
            {
                completionEffect.Play();
            }

            OnEscortCompleted.Invoke();
        }

        private void ApplyReliefBoost()
        {
            if (!applyReliefBoostOnManagerArrival)
            {
                return;
            }

            var manager = queueManager != null ? queueManager : QueueManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.ApplyQueueReliefBoost(
                patienceRestorePercent,
                temporaryPatienceDrainMultiplier,
                reliefBoostSeconds);
        }

        private void ApplyPraiseBoost()
        {
            PlayPraiseSound();
            ShowPraiseBubble();
            PlayPraiseEffect();
            ApplyPlayerSpeedBoost();
        }

        private void PlayPraiseSound()
        {
            if (praiseAudioSource == null)
            {
                return;
            }

            praiseAudioSource.PlayOneShot(praiseDingClip != null ? praiseDingClip : GetFallbackPraiseDingClip());
        }

        private void ShowPraiseBubble()
        {
            if (praiseBubbleRoutine != null)
            {
                StopCoroutine(praiseBubbleRoutine);
            }

            praiseBubbleRoutine = StartCoroutine(PraiseBubbleRoutine());
        }

        private IEnumerator PraiseBubbleRoutine()
        {
            var bubble = CreatePraiseBubble();
            var elapsed = 0f;
            while (elapsed < praiseBubbleSeconds && bubble != null)
            {
                elapsed += Time.deltaTime;
                bubble.transform.localScale = Vector3.one * (1f + Mathf.Sin(elapsed * 12f) * 0.06f);
                yield return null;
            }

            if (bubble != null)
            {
                Destroy(bubble);
            }

            praiseBubbleRoutine = null;
        }

        private GameObject CreatePraiseBubble()
        {
            var anchor = praiseBubbleAnchor != null
                ? praiseBubbleAnchor
                : managerRoomEntrance != null
                    ? managerRoomEntrance
                    : transform;

            if (praiseBubblePrefab != null)
            {
                var instance = Instantiate(praiseBubblePrefab, anchor);
                instance.transform.localPosition = Vector3.up * 1.4f;
                instance.transform.localRotation = Quaternion.identity;
                return instance;
            }

            var bubble = new GameObject("Managerial Praise Bubble");
            bubble.transform.SetParent(anchor, false);
            bubble.transform.localPosition = Vector3.up * 1.4f;
            bubble.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);

            var text = bubble.AddComponent<TextMesh>();
            text.text = praiseBubbleText;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.26f;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(1f, 0.86f, 0.18f);
            return bubble;
        }

        private void PlayPraiseEffect()
        {
            if (playerPraiseEffect != null)
            {
                playerPraiseEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                playerPraiseEffect.Play();
                RestartPraiseEffectStopRoutine();
                return;
            }

            if (praiseEffectPrefab == null || player == null)
            {
                return;
            }

            playerPraiseEffect = Instantiate(praiseEffectPrefab, player);
            playerPraiseEffect.transform.localPosition = Vector3.up * 0.25f;
            playerPraiseEffect.Play();
            RestartPraiseEffectStopRoutine();
        }

        private void RestartPraiseEffectStopRoutine()
        {
            if (praiseEffectRoutine != null)
            {
                StopCoroutine(praiseEffectRoutine);
            }

            praiseEffectRoutine = StartCoroutine(PraiseEffectStopRoutine());
        }

        private IEnumerator PraiseEffectStopRoutine()
        {
            var elapsed = 0f;
            while (elapsed < praiseEffectSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (playerPraiseEffect != null)
            {
                playerPraiseEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            praiseEffectRoutine = null;
        }

        private void ApplyPlayerSpeedBoost()
        {
            if (playerSpeedBoostMultiplier <= 1f || playerSpeedBoostSeconds <= 0f)
            {
                return;
            }

            if (speedBoostRoutine != null)
            {
                StopCoroutine(speedBoostRoutine);
                RestorePlayerSpeedBoost();
            }

            speedBoostRoutine = StartCoroutine(PlayerSpeedBoostRoutine());
        }

        private IEnumerator PlayerSpeedBoostRoutine()
        {
            if (mobilePlayerController != null)
            {
                mobilePlayerController.MovementSpeedMultiplier *= playerSpeedBoostMultiplier;
            }

            if (topDownInputController != null)
            {
                topDownInputController.MovementSpeedMultiplier *= playerSpeedBoostMultiplier;
            }

            praiseSpeedBoostApplied = true;

            var elapsed = 0f;
            while (elapsed < playerSpeedBoostSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            RestorePlayerSpeedBoost();
            speedBoostRoutine = null;
        }

        private void RestorePlayerSpeedBoost()
        {
            if (!praiseSpeedBoostApplied)
            {
                return;
            }

            if (mobilePlayerController != null)
            {
                mobilePlayerController.MovementSpeedMultiplier /= playerSpeedBoostMultiplier;
            }

            if (topDownInputController != null)
            {
                topDownInputController.MovementSpeedMultiplier /= playerSpeedBoostMultiplier;
            }

            praiseSpeedBoostApplied = false;
        }

        private static AudioClip GetFallbackPraiseDingClip()
        {
            if (fallbackPraiseDingClip != null)
            {
                return fallbackPraiseDingClip;
            }

            const int sampleRate = 22050;
            const float duration = 0.18f;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = 1f - Mathf.Clamp01(t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * envelope * 0.28f;
            }

            fallbackPraiseDingClip = AudioClip.Create("Managerial Praise Ding", sampleCount, 1, sampleRate, false);
            fallbackPraiseDingClip.SetData(samples, 0);
            return fallbackPraiseDingClip;
        }

        private void ConfigureVipAgentForEscort()
        {
            if (vipAgent == null)
            {
                return;
            }

            vipAgent.stoppingDistance = followDistance;
        }

        private void MoveVipTo(Vector3 targetPosition, bool force = false)
        {
            if (vipAgent != null && vipAgent.enabled && vipAgent.isOnNavMesh)
            {
                vipAgent.isStopped = false;
                vipAgent.SetDestination(targetPosition);
                return;
            }

            if (force && vipCustomer != null)
            {
                vipCustomer.position = targetPosition;
                return;
            }

            if (vipCustomer != null)
            {
                vipCustomer.position = Vector3.MoveTowards(
                    vipCustomer.position,
                    targetPosition,
                    fallbackMoveSpeed * Time.deltaTime);
            }
        }

        private void StopVipMovement()
        {
            if (vipAgent != null && vipAgent.enabled && vipAgent.isOnNavMesh)
            {
                vipAgent.isStopped = true;
                vipAgent.ResetPath();
            }

            PlayTrigger(waitTrigger);
        }

        private static bool IsInsideZone(Collider zone, Vector3 position)
        {
            return zone != null && zone.bounds.Contains(position);
        }

        private bool IsVipAtWaitingSpot()
        {
            if (vipCustomer == null)
            {
                return false;
            }

            if (vipWaitingSpotZone != null)
            {
                return IsInsideZone(vipWaitingSpotZone, vipCustomer.position);
            }

            if (vipCounterZone != null)
            {
                return IsInsideZone(vipCounterZone, vipCustomer.position);
            }

            if (vipWaitingSpot == null)
            {
                return true;
            }

            return (vipCustomer.position - vipWaitingSpot.position).sqrMagnitude <= interactionDistance * interactionDistance;
        }

        private bool IsAtManagerRoomEntrance()
        {
            if (player == null || vipCustomer == null)
            {
                return false;
            }

            var zone = managerRoomEntranceZone != null ? managerRoomEntranceZone : safeDepositVaultZone;
            if (zone != null)
            {
                return IsInsideZone(zone, player.position) && IsInsideZone(zone, vipCustomer.position);
            }

            var target = managerRoomEntrance != null ? managerRoomEntrance : vaultInsidePoint;
            if (target == null)
            {
                return false;
            }

            var triggerRadius = interactionDistance * interactionDistance;
            return (player.position - target.position).sqrMagnitude <= triggerRadius
                && (vipCustomer.position - target.position).sqrMagnitude <= triggerRadius;
        }

        private void ResolveLegacyRouteAliases()
        {
            if (vipWaitingSpotZone == null)
            {
                vipWaitingSpotZone = vipCounterZone;
            }

            if (managerRoomEntranceZone == null)
            {
                managerRoomEntranceZone = safeDepositVaultZone;
            }

            if (managerRoomInsidePoint == null)
            {
                managerRoomInsidePoint = vaultInsidePoint;
            }
        }

        private void SetState(VIPEscortState newState)
        {
            if (State == newState)
            {
                return;
            }

            State = newState;
            OnStateChanged.Invoke(State);
        }

        private void PlayTrigger(string triggerName)
        {
            if (vipAnimator == null || string.IsNullOrWhiteSpace(triggerName))
            {
                return;
            }

            vipAnimator.SetTrigger(triggerName);
        }

        private void OnActionPerformed(InputAction.CallbackContext context)
        {
            Action();
        }
    }
}
