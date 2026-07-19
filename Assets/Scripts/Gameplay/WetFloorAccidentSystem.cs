using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum WetFloorAccidentState
    {
        Idle,
        Cleaning,
        AccidentActive,
        Rescuing,
        Cooldown
    }

    public class WetFloorAccidentSystem : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private Vector2 cleanUpIntervalSeconds = new Vector2(80f, 120f);
        [SerializeField, Min(3f)] private float cleanUpDurationSeconds = 18f;
        [SerializeField, Range(0f, 1f)] private float slipChance = 0.15f;
        [SerializeField, Min(0.1f)] private float rescueHelpSeconds = 1.5f;
        [SerializeField, Min(0.1f)] private float fallbackMoveSpeed = 3.8f;

        [Header("Scene References")]
        [SerializeField] private GameObject teaLadyAI;
        [SerializeField] private NavMeshAgent teaLadyAgent;
        [SerializeField] private Animator teaLadyAnimator;
        [SerializeField] private SecurityGuardAI securityGuardAI;
        [SerializeField] private NavMeshAgent guardAgent;
        [SerializeField] private Animator guardAnimator;
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private ManagerSatisfactionSystem managerSatisfactionSystem;
        [SerializeField] private Transform kitchenStation;
        [SerializeField] private Transform lobbyPatrolArea;
        [SerializeField] private Transform wetFloorCenter;
        [SerializeField] private Vector3 wetFloorZoneSize = new Vector3(3.2f, 1.2f, 2.2f);

        [Header("UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Button sendGuardButton;
        [SerializeField] private Text sendGuardButtonLabel;

        [Header("Visuals")]
        [SerializeField] private GameObject wetFloorSignPrefab;
        [SerializeField] private GameObject emergencyIconPrefab;
        [SerializeField] private ParticleSystem puddleParticlePrefab;
        [SerializeField] private ParticleSystem dizzyStarsPrefab;
        [SerializeField] private ParticleSystem compassionateParticlePrefab;
        [SerializeField] private string mopAnimationTrigger = "Mop";
        [SerializeField] private string slippedAnimationTrigger = "Slipped";
        [SerializeField] private string helpingAnimationTrigger = "HelpingUp";
        [SerializeField] private string guardRunAnimationTrigger = "Sprint";

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip mopSound;
        [SerializeField] private AudioClip slipCrashSound;
        [SerializeField] private AudioClip rescueSound;

        [Header("Balance")]
        [SerializeField, Min(0.1f)] private float fallenPatienceSeconds = 10f;
        [SerializeField, Min(1f)] private float fallenPatienceDrainMultiplier = 3f;
        [SerializeField, Min(0)] private int goldReward = 100;
        [SerializeField, Min(0f)] private float satisfactionReward = 15f;
        [SerializeField, Min(0.1f)] private float compassionateBoostSeconds = 20f;
        [SerializeField, Range(0.05f, 1f)] private float compassionatePatienceDrainMultiplier = 0.6f;

        public UnityEvent<GameObject> OnAccidentTriggered = new UnityEvent<GameObject>();
        public UnityEvent<GameObject> OnCustomerRescued = new UnityEvent<GameObject>();
        public UnityEvent OnCompassionateBoostStarted = new UnityEvent();
        public UnityEvent<WetFloorAccidentState> OnStateChanged = new UnityEvent<WetFloorAccidentState>();

        private WetFloorAccidentState state = WetFloorAccidentState.Idle;
        private Coroutine cleanUpRoutine;
        private float nextCleanUpTimer;
        private GameObject activeWetFloorZone;
        private GameObject activeWetFloorSign;
        private ParticleSystem activePuddleParticles;
        private GameObject activeEmergencyIcon;
        private ParticleSystem activeDizzyStars;
        private GameObject fallenCustomerObject;
        private QueueCustomer fallenQueueCustomer;
        private CustomerPatience fallenCustomerPatience;
        private Animator fallenAnimator;
        private bool hadFallenRigidbody;
        private bool previousFallenRigidbodyKinematic;

        public WetFloorAccidentState State => state;
        public bool HasActiveAccident => fallenCustomerObject != null;

        private void Awake()
        {
            ResolveMissingReferences();
            EnsureSendGuardButton();
            SetSendGuardButtonVisible(false);
            ResetCleanUpTimer();
        }

        private void OnEnable()
        {
            if (sendGuardButton != null)
            {
                sendGuardButton.onClick.AddListener(RescueCustomer);
            }
        }

        private void OnDisable()
        {
            if (sendGuardButton != null)
            {
                sendGuardButton.onClick.RemoveListener(RescueCustomer);
            }

            if (cleanUpRoutine != null)
            {
                StopCoroutine(cleanUpRoutine);
                cleanUpRoutine = null;
            }

            CleanupWetFloorObjects();
            CleanupAccidentVisuals();
            SetSendGuardButtonVisible(false);
        }

        private void Update()
        {
            if (cleanUpRoutine != null || state == WetFloorAccidentState.AccidentActive || state == WetFloorAccidentState.Rescuing)
            {
                return;
            }

            nextCleanUpTimer -= Time.deltaTime;
            if (nextCleanUpTimer <= 0f)
            {
                TriggerCleanUp();
            }
        }

        public void TriggerCleanUp()
        {
            if (cleanUpRoutine != null || state == WetFloorAccidentState.AccidentActive || state == WetFloorAccidentState.Rescuing)
            {
                return;
            }

            cleanUpRoutine = StartCoroutine(CleanUpRoutine());
        }

        public void TrySlipCustomer(GameObject customerObject)
        {
            if (state != WetFloorAccidentState.Cleaning || fallenCustomerObject != null || customerObject == null)
            {
                return;
            }

            if (!customerObject.TryGetComponent<QueueCustomer>(out var queueCustomer))
            {
                return;
            }

            if (Random.value > slipChance)
            {
                return;
            }

            SlipAndFall(queueCustomer);
        }

        public void SlipAndFall(QueueCustomer fallenCustomer)
        {
            if (fallenCustomer == null || fallenCustomerObject != null)
            {
                return;
            }

            fallenCustomerObject = fallenCustomer.gameObject;
            fallenQueueCustomer = fallenCustomer;
            fallenCustomerPatience = fallenCustomerObject.GetComponent<CustomerPatience>();
            fallenAnimator = fallenCustomerObject.GetComponentInChildren<Animator>();
            hadFallenRigidbody = fallenCustomerObject.TryGetComponent<Rigidbody>(out var fallenBody);
            previousFallenRigidbodyKinematic = hadFallenRigidbody && fallenBody.isKinematic;

            queueManager?.ReleaseCustomerForIncident(fallenCustomerObject);
            FreezeCustomerMovement(true);
            fallenQueueCustomer.StartPatience(fallenPatienceSeconds);
            fallenQueueCustomer.SetPatienceDrainMultiplier(fallenPatienceDrainMultiplier);
            if (fallenCustomerPatience != null)
            {
                fallenCustomerPatience.SetDrainMultiplier(fallenPatienceDrainMultiplier);
            }

            TriggerAnimator(fallenAnimator, slippedAnimationTrigger);
            PlaySound(slipCrashSound);
            SpawnAccidentVisuals(fallenCustomerObject.transform);
            SetSendGuardButtonVisible(true);
            SetState(WetFloorAccidentState.AccidentActive);
            OnAccidentTriggered.Invoke(fallenCustomerObject);
        }

        public void RescueCustomer()
        {
            if (state != WetFloorAccidentState.AccidentActive || fallenCustomerObject == null)
            {
                return;
            }

            SetSendGuardButtonVisible(false);
            StartCoroutine(RescueCustomerRoutine());
        }

        private IEnumerator CleanUpRoutine()
        {
            SetState(WetFloorAccidentState.Cleaning);
            EnsureTeaLady();
            SpawnWetFloorObjects();
            PlaySound(mopSound);
            TriggerAnimator(teaLadyAnimator, mopAnimationTrigger);

            if (teaLadyAI != null)
            {
                yield return MoveObjectTo(teaLadyAI.transform, GetWetFloorPosition(), teaLadyAgent, fallbackMoveSpeed * 0.65f);
            }

            var timer = cleanUpDurationSeconds;
            while (timer > 0f && state == WetFloorAccidentState.Cleaning)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            if (teaLadyAI != null)
            {
                yield return MoveObjectTo(teaLadyAI.transform, GetKitchenPosition(), teaLadyAgent, fallbackMoveSpeed * 0.65f);
            }

            CleanupWetFloorObjects();
            if (state == WetFloorAccidentState.Cleaning)
            {
                SetState(WetFloorAccidentState.Cooldown);
                ResetCleanUpTimer();
                SetState(WetFloorAccidentState.Idle);
            }

            cleanUpRoutine = null;
        }

        private IEnumerator RescueCustomerRoutine()
        {
            SetState(WetFloorAccidentState.Rescuing);
            ResolveMissingReferences();

            if (securityGuardAI != null)
            {
                TriggerAnimator(guardAnimator, guardRunAnimationTrigger);
                yield return MoveObjectTo(securityGuardAI.transform, fallenCustomerObject.transform.position, guardAgent, fallbackMoveSpeed);
            }

            TriggerAnimator(guardAnimator, helpingAnimationTrigger);
            TriggerAnimator(fallenAnimator, helpingAnimationTrigger);
            PlaySound(rescueSound);
            yield return new WaitForSeconds(rescueHelpSeconds);

            RestoreFallenCustomer();
            AddGold(goldReward);
            managerSatisfactionSystem?.AddSatisfaction(satisfactionReward);
            ApplyCompassionateBoost();

            if (securityGuardAI != null)
            {
                yield return MoveObjectTo(securityGuardAI.transform, GetPatrolPosition(), guardAgent, fallbackMoveSpeed);
            }

            CleanupAccidentVisuals();
            fallenCustomerObject = null;
            fallenQueueCustomer = null;
            fallenCustomerPatience = null;
            fallenAnimator = null;
            hadFallenRigidbody = false;
            previousFallenRigidbodyKinematic = false;
            SetState(WetFloorAccidentState.Cooldown);
            ResetCleanUpTimer();
            SetState(WetFloorAccidentState.Idle);
        }

        private void RestoreFallenCustomer()
        {
            if (fallenQueueCustomer == null || fallenCustomerObject == null)
            {
                return;
            }

            FreezeCustomerMovement(false);
            fallenQueueCustomer.ResetPatienceDrainMultiplier();
            fallenQueueCustomer.SetPatiencePercent(1f);
            fallenQueueCustomer.StopPatience();
            if (fallenCustomerPatience != null)
            {
                fallenCustomerPatience.ResetDrainMultiplier();
                fallenCustomerPatience.SetPatience(100f);
            }

            queueManager?.AddCustomerToQueue(fallenCustomerObject, true);
            OnCustomerRescued.Invoke(fallenCustomerObject);
        }

        private void ApplyCompassionateBoost()
        {
            queueManager?.ApplyQueueReliefBoost(0f, compassionatePatienceDrainMultiplier, compassionateBoostSeconds);
            SpawnCompassionateEffect();
            OnCompassionateBoostStarted.Invoke();
        }

        private void FreezeCustomerMovement(bool frozen)
        {
            if (fallenCustomerObject == null)
            {
                return;
            }

            if (fallenCustomerObject.TryGetComponent<NavMeshAgent>(out var agent))
            {
                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = frozen;
                }
            }

            if (fallenCustomerObject.TryGetComponent<Rigidbody>(out var body))
            {
                body.isKinematic = frozen || (hadFallenRigidbody && previousFallenRigidbodyKinematic);
            }
        }

        private void SpawnWetFloorObjects()
        {
            CleanupWetFloorObjects();
            var center = GetWetFloorPosition();

            activeWetFloorZone = new GameObject("Wet Floor Slip Zone");
            activeWetFloorZone.transform.position = center;
            var collider = activeWetFloorZone.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = wetFloorZoneSize;
            var zone = activeWetFloorZone.AddComponent<WetFloorSlipZone>();
            zone.Initialize(this);

            if (wetFloorSignPrefab != null)
            {
                activeWetFloorSign = Instantiate(wetFloorSignPrefab, center + Vector3.up * 0.1f + Vector3.right * 0.9f, Quaternion.identity);
            }
            else
            {
                activeWetFloorSign = CreateFallbackSign(center + Vector3.right * 0.9f);
            }

            if (puddleParticlePrefab != null)
            {
                activePuddleParticles = Instantiate(puddleParticlePrefab, center + Vector3.up * 0.04f, Quaternion.identity);
                activePuddleParticles.Play();
            }
        }

        private static GameObject CreateFallbackSign(Vector3 position)
        {
            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "Wet Floor Sign Prototype";
            sign.transform.position = position + Vector3.up * 0.28f;
            sign.transform.localScale = new Vector3(0.36f, 0.56f, 0.08f);
            if (sign.TryGetComponent<Renderer>(out var rendererComponent))
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(1f, 0.82f, 0.12f);
                rendererComponent.sharedMaterial = material;
            }

            return sign;
        }

        private void CleanupWetFloorObjects()
        {
            if (activeWetFloorZone != null)
            {
                Destroy(activeWetFloorZone);
                activeWetFloorZone = null;
            }

            if (activeWetFloorSign != null)
            {
                Destroy(activeWetFloorSign);
                activeWetFloorSign = null;
            }

            if (activePuddleParticles != null)
            {
                Destroy(activePuddleParticles.gameObject);
                activePuddleParticles = null;
            }
        }

        private void SpawnAccidentVisuals(Transform customerTransform)
        {
            CleanupAccidentVisuals();
            if (customerTransform == null)
            {
                return;
            }

            if (emergencyIconPrefab != null)
            {
                activeEmergencyIcon = Instantiate(emergencyIconPrefab, customerTransform);
                activeEmergencyIcon.transform.localPosition = Vector3.up * 1.95f;
            }
            else
            {
                activeEmergencyIcon = new GameObject("First Aid Emergency Icon");
                activeEmergencyIcon.transform.SetParent(customerTransform, false);
                activeEmergencyIcon.transform.localPosition = Vector3.up * 1.95f;
                activeEmergencyIcon.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
                var label = activeEmergencyIcon.AddComponent<TextMesh>();
                label.text = "HELP!";
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.fontStyle = FontStyle.Bold;
                label.characterSize = 0.2f;
                label.color = Color.red;
            }

            if (dizzyStarsPrefab != null)
            {
                activeDizzyStars = Instantiate(dizzyStarsPrefab, customerTransform);
                activeDizzyStars.transform.localPosition = Vector3.up * 1.75f;
                activeDizzyStars.Play();
            }
        }

        private void CleanupAccidentVisuals()
        {
            if (activeEmergencyIcon != null)
            {
                Destroy(activeEmergencyIcon);
                activeEmergencyIcon = null;
            }

            if (activeDizzyStars != null)
            {
                Destroy(activeDizzyStars.gameObject);
                activeDizzyStars = null;
            }
        }

        private void SpawnCompassionateEffect()
        {
            if (compassionateParticlePrefab == null)
            {
                return;
            }

            var anchor = wetFloorCenter != null ? wetFloorCenter : transform;
            var effect = Instantiate(compassionateParticlePrefab, anchor.position + Vector3.up * 1.2f, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, 2.5f);
        }

        private void EnsureTeaLady()
        {
            if (teaLadyAI != null)
            {
                ResolveTeaLadyComponents();
                return;
            }

            teaLadyAI = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            teaLadyAI.name = "Wet Floor TeaLady Prototype";
            teaLadyAI.transform.position = GetKitchenPosition();
            teaLadyAI.transform.localScale = new Vector3(0.9f, 1.08f, 0.9f);
            if (teaLadyAI.TryGetComponent<Renderer>(out var rendererComponent))
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.9f, 0.42f, 0.62f);
                rendererComponent.sharedMaterial = material;
            }
        }

        private IEnumerator MoveObjectTo(Transform target, Vector3 destination, NavMeshAgent agent, float speed)
        {
            if (target == null)
            {
                yield break;
            }

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(destination);
                while (agent.pathPending)
                {
                    yield return null;
                }

                while (agent.enabled && agent.isOnNavMesh && agent.remainingDistance > Mathf.Max(agent.stoppingDistance, 0.12f))
                {
                    yield return null;
                }

                yield break;
            }

            while ((target.position - destination).sqrMagnitude > 0.04f)
            {
                var current = target.position;
                target.position = Vector3.MoveTowards(current, destination, speed * Time.deltaTime);
                var direction = destination - current;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    target.rotation = Quaternion.Slerp(
                        target.rotation,
                        Quaternion.LookRotation(direction.normalized, Vector3.up),
                        10f * Time.deltaTime);
                }

                yield return null;
            }
        }

        private void EnsureSendGuardButton()
        {
            if (sendGuardButton != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasObject = new GameObject("Wet Floor Accident Canvas");
                targetCanvas = canvasObject.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            var buttonObject = new GameObject("Send Guard To Help Button");
            buttonObject.transform.SetParent(targetCanvas.transform, false);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-28f, -258f);
            rect.sizeDelta = new Vector2(250f, 58f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.94f, 0.22f, 0.16f, 0.96f);
            sendGuardButton = buttonObject.AddComponent<Button>();

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            sendGuardButtonLabel = labelObject.AddComponent<Text>();
            sendGuardButtonLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sendGuardButtonLabel.text = "SEND GUARD";
            sendGuardButtonLabel.alignment = TextAnchor.MiddleCenter;
            sendGuardButtonLabel.fontSize = 19;
            sendGuardButtonLabel.fontStyle = FontStyle.Bold;
            sendGuardButtonLabel.color = Color.white;

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private void SetSendGuardButtonVisible(bool visible)
        {
            if (sendGuardButton != null)
            {
                sendGuardButton.gameObject.SetActive(visible);
            }
        }

        private void ResolveMissingReferences()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance ?? FindFirstObjectByType<QueueManager>();
            }

            if (securityGuardAI == null)
            {
                securityGuardAI = FindFirstObjectByType<SecurityGuardAI>();
            }

            if (managerSatisfactionSystem == null)
            {
                managerSatisfactionSystem = FindFirstObjectByType<ManagerSatisfactionSystem>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            ResolveTeaLadyComponents();
            ResolveGuardComponents();
        }

        private void ResolveTeaLadyComponents()
        {
            if (teaLadyAI == null)
            {
                return;
            }

            if (teaLadyAgent == null)
            {
                teaLadyAgent = teaLadyAI.GetComponent<NavMeshAgent>();
            }

            if (teaLadyAnimator == null)
            {
                teaLadyAnimator = teaLadyAI.GetComponentInChildren<Animator>();
            }
        }

        private void ResolveGuardComponents()
        {
            if (securityGuardAI == null)
            {
                return;
            }

            if (guardAgent == null)
            {
                guardAgent = securityGuardAI.GetComponent<NavMeshAgent>();
            }

            if (guardAnimator == null)
            {
                guardAnimator = securityGuardAI.GetComponentInChildren<Animator>();
            }
        }

        private Vector3 GetWetFloorPosition()
        {
            return wetFloorCenter != null ? wetFloorCenter.position : transform.position + Vector3.forward * 1.5f;
        }

        private Vector3 GetKitchenPosition()
        {
            return kitchenStation != null ? kitchenStation.position : transform.position;
        }

        private Vector3 GetPatrolPosition()
        {
            return lobbyPatrolArea != null
                ? lobbyPatrolArea.position
                : securityGuardAI != null
                    ? securityGuardAI.transform.position
                    : transform.position;
        }

        private void ResetCleanUpTimer()
        {
            var min = Mathf.Max(1f, cleanUpIntervalSeconds.x);
            var max = Mathf.Max(min, cleanUpIntervalSeconds.y);
            nextCleanUpTimer = Random.Range(min, max);
        }

        private void SetState(WetFloorAccidentState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            OnStateChanged.Invoke(state);
        }

        private static void TriggerAnimator(Animator animator, string triggerName)
        {
            if (animator != null && !string.IsNullOrWhiteSpace(triggerName))
            {
                animator.SetTrigger(triggerName);
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

    internal class WetFloorSlipZone : MonoBehaviour
    {
        private WetFloorAccidentSystem accidentSystem;

        public void Initialize(WetFloorAccidentSystem system)
        {
            accidentSystem = system;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (accidentSystem == null || other == null)
            {
                return;
            }

            var customer = other.GetComponentInParent<QueueCustomer>();
            if (customer != null)
            {
                accidentSystem.TrySlipCustomer(customer.gameObject);
            }
        }
    }
}
