using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum TeaLadyRefillState
    {
        Idle,
        WalkingToCounter,
        WaitingForRefill,
        ReturningToKitchen,
        Cooldown
    }

    public class TeaLadyRefillEvent : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private Vector2 refillIntervalSeconds = new Vector2(60f, 80f);
        [SerializeField, Min(1f)] private float waitAtCounterSeconds = 15f;
        [SerializeField, Min(0.1f)] private float fallbackMoveSpeed = 2.1f;
        [SerializeField, Range(0.2f, 1f)] private float rainyIntervalMultiplier = 0.75f;

        [Header("Scene References")]
        [SerializeField] private GameObject teaLadyAI;
        [SerializeField] private NavMeshAgent teaLadyAgent;
        [SerializeField] private Animator teaLadyAnimator;
        [SerializeField] private Transform kitchenStation;
        [SerializeField] private Transform playerCounterLocation;
        [SerializeField] private Transform queueSteamAnchor;
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private ManagerSatisfactionSystem managerSatisfactionSystem;

        [Header("UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Button refillBrewButton;
        [SerializeField] private Text refillButtonLabel;

        [Header("Visuals")]
        [SerializeField] private GameObject emptyTeacupIconPrefab;
        [SerializeField] private ParticleSystem teaSteamParticlePrefab;
        [SerializeField] private string happyAnimationTrigger = "Happy";
        [SerializeField] private string sighAnimationTrigger = "Sigh";

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip refillRequestSound;
        [SerializeField] private AudioClip bubblingSound;
        [SerializeField] private AudioClip ignoredSighSound;

        [Header("Rewards")]
        [SerializeField, Min(0)] private int goldReward = 50;
        [SerializeField, Min(0f)] private float satisfactionReward = 10f;
        [SerializeField, Min(0f)] private float ignoredSatisfactionPenalty = 15f;
        [SerializeField, Min(0.1f)] private float freshBrewBoostSeconds = 15f;
        [SerializeField, Range(0.05f, 1f)] private float freshBrewPatienceDrainMultiplier = 0.7f;

        public UnityEvent OnRefillRequested = new UnityEvent();
        public UnityEvent OnRefillCompleted = new UnityEvent();
        public UnityEvent OnRefillIgnored = new UnityEvent();
        public UnityEvent<TeaLadyRefillState> OnStateChanged = new UnityEvent<TeaLadyRefillState>();

        private GameObject activeIcon;
        private Coroutine eventRoutine;
        private float nextRefillTimer;
        private TeaLadyRefillState state = TeaLadyRefillState.Idle;

        public TeaLadyRefillState State => state;
        public bool IsWaitingForRefill => state == TeaLadyRefillState.WaitingForRefill;

        private void Awake()
        {
            ResolveMissingReferences();
            EnsureRefillButton();
            SetRefillButtonVisible(false);
            ResetRefillTimer();
        }

        private void OnEnable()
        {
            if (refillBrewButton != null)
            {
                refillBrewButton.onClick.AddListener(ResolveRefill);
            }
        }

        private void OnDisable()
        {
            if (refillBrewButton != null)
            {
                refillBrewButton.onClick.RemoveListener(ResolveRefill);
            }

            if (eventRoutine != null)
            {
                StopCoroutine(eventRoutine);
                eventRoutine = null;
            }

            ClearIcon();
            SetRefillButtonVisible(false);
        }

        private void Update()
        {
            if (eventRoutine != null)
            {
                return;
            }

            nextRefillTimer -= Time.deltaTime;
            if (nextRefillTimer <= 0f)
            {
                eventRoutine = StartCoroutine(RefillRoutine());
            }
        }

        public void ForceStartRefillEvent()
        {
            if (eventRoutine != null)
            {
                return;
            }

            eventRoutine = StartCoroutine(RefillRoutine());
        }

        public void ResolveRefill()
        {
            if (state != TeaLadyRefillState.WaitingForRefill)
            {
                return;
            }

            PlaySound(bubblingSound);
            TriggerAnimator(happyAnimationTrigger);
            AddGold(goldReward);
            managerSatisfactionSystem?.AddSatisfaction(satisfactionReward);
            ApplyFreshBrewBoost();
            OnRefillCompleted.Invoke();
            SetRefillButtonVisible(false);
            ClearIcon();
            SetState(TeaLadyRefillState.ReturningToKitchen);
        }

        private IEnumerator RefillRoutine()
        {
            EnsureTeaLady();
            SetState(TeaLadyRefillState.WalkingToCounter);
            yield return MoveTeaLadyTo(GetCounterPosition());

            if (teaLadyAI == null)
            {
                FinishEvent();
                yield break;
            }

            SetState(TeaLadyRefillState.WaitingForRefill);
            SpawnEmptyCupIcon();
            SetRefillButtonVisible(true);
            PlaySound(refillRequestSound);
            OnRefillRequested.Invoke();

            var waitTimer = waitAtCounterSeconds;
            while (waitTimer > 0f && state == TeaLadyRefillState.WaitingForRefill)
            {
                waitTimer -= Time.deltaTime;
                yield return null;
            }

            if (state == TeaLadyRefillState.WaitingForRefill)
            {
                HandleIgnoredRefill();
            }

            yield return MoveTeaLadyTo(GetKitchenPosition());
            SetState(TeaLadyRefillState.Cooldown);
            FinishEvent();
        }

        private void HandleIgnoredRefill()
        {
            SetRefillButtonVisible(false);
            ClearIcon();
            TriggerAnimator(sighAnimationTrigger);
            PlaySound(ignoredSighSound);
            managerSatisfactionSystem?.DeductSatisfaction(ignoredSatisfactionPenalty);
            OnRefillIgnored.Invoke();
            SetState(TeaLadyRefillState.ReturningToKitchen);
        }

        private void ApplyFreshBrewBoost()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance;
            }

            queueManager?.ApplyQueueReliefBoost(0f, freshBrewPatienceDrainMultiplier, freshBrewBoostSeconds);
            SpawnQueueSteamEffect();
        }

        private IEnumerator MoveTeaLadyTo(Vector3 destination)
        {
            if (teaLadyAI == null)
            {
                yield break;
            }

            if (teaLadyAgent != null && teaLadyAgent.enabled && teaLadyAgent.isOnNavMesh)
            {
                teaLadyAgent.SetDestination(destination);
                while (teaLadyAgent.pathPending)
                {
                    yield return null;
                }

                while (teaLadyAI != null && teaLadyAgent.remainingDistance > teaLadyAgent.stoppingDistance)
                {
                    yield return null;
                }

                yield break;
            }

            while (teaLadyAI != null && (teaLadyAI.transform.position - destination).sqrMagnitude > 0.04f)
            {
                var current = teaLadyAI.transform.position;
                teaLadyAI.transform.position = Vector3.MoveTowards(current, destination, fallbackMoveSpeed * Time.deltaTime);
                var direction = destination - current;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    teaLadyAI.transform.rotation = Quaternion.Slerp(
                        teaLadyAI.transform.rotation,
                        Quaternion.LookRotation(direction.normalized, Vector3.up),
                        12f * Time.deltaTime);
                }

                yield return null;
            }
        }

        private void EnsureTeaLady()
        {
            if (teaLadyAI != null)
            {
                teaLadyAI.SetActive(true);
                teaLadyAI.transform.position = GetKitchenPosition();
                ResolveTeaLadyComponents();
                return;
            }

            teaLadyAI = CreateFallbackTeaLady(GetKitchenPosition());
            ResolveTeaLadyComponents();
        }

        private static GameObject CreateFallbackTeaLady(Vector3 position)
        {
            var teaLady = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            teaLady.name = "TeaLadyRefillNPC Prototype";
            teaLady.transform.position = position;
            teaLady.transform.localScale = new Vector3(0.9f, 1.08f, 0.9f);

            if (teaLady.TryGetComponent<Renderer>(out var rendererComponent))
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.9f, 0.42f, 0.62f);
                rendererComponent.sharedMaterial = material;
            }

            return teaLady;
        }

        private void SpawnEmptyCupIcon()
        {
            ClearIcon();
            if (teaLadyAI == null)
            {
                return;
            }

            var anchor = teaLadyAI.transform;
            if (emptyTeacupIconPrefab != null)
            {
                activeIcon = Instantiate(emptyTeacupIconPrefab, anchor);
                activeIcon.transform.localPosition = Vector3.up * 1.85f;
                return;
            }

            activeIcon = new GameObject("Empty Teacup Refill Icon");
            activeIcon.transform.SetParent(anchor, false);
            activeIcon.transform.localPosition = Vector3.up * 1.85f;
            activeIcon.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var label = activeIcon.AddComponent<TextMesh>();
            label.text = "TEA!";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.2f;
            label.color = new Color(0.18f, 0.72f, 0.28f);
        }

        private void ClearIcon()
        {
            if (activeIcon != null)
            {
                Destroy(activeIcon);
                activeIcon = null;
            }
        }

        private void SpawnQueueSteamEffect()
        {
            if (teaSteamParticlePrefab == null)
            {
                return;
            }

            var anchor = queueSteamAnchor != null ? queueSteamAnchor : playerCounterLocation;
            var position = anchor != null ? anchor.position + Vector3.up * 1.2f : transform.position + Vector3.up * 1.2f;
            var effect = Instantiate(teaSteamParticlePrefab, position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, Mathf.Max(1f, freshBrewBoostSeconds));
        }

        private void EnsureRefillButton()
        {
            if (refillBrewButton != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasObject = new GameObject("Tea Lady Refill Canvas");
                targetCanvas = canvasObject.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            var buttonObject = new GameObject("Refill Brew Button");
            buttonObject.transform.SetParent(targetCanvas.transform, false);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-28f, -118f);
            rect.sizeDelta = new Vector2(210f, 58f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.76f, 0.38f, 0.95f);
            refillBrewButton = buttonObject.AddComponent<Button>();

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            refillButtonLabel = labelObject.AddComponent<Text>();
            refillButtonLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            refillButtonLabel.text = "REFILL BREW";
            refillButtonLabel.alignment = TextAnchor.MiddleCenter;
            refillButtonLabel.fontSize = 20;
            refillButtonLabel.fontStyle = FontStyle.Bold;
            refillButtonLabel.color = Color.white;

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private void SetRefillButtonVisible(bool visible)
        {
            if (refillBrewButton != null)
            {
                refillBrewButton.gameObject.SetActive(visible);
            }
        }

        private void FinishEvent()
        {
            ClearIcon();
            SetRefillButtonVisible(false);
            ResetRefillTimer();
            SetState(TeaLadyRefillState.Idle);
            eventRoutine = null;
        }

        private void ResetRefillTimer()
        {
            var min = Mathf.Max(1f, refillIntervalSeconds.x);
            var max = Mathf.Max(min, refillIntervalSeconds.y);
            var interval = Random.Range(min, max);
            if (DynamicWeatherSystem.Instance != null && DynamicWeatherSystem.Instance.CurrentState == WeatherState.Rainy)
            {
                interval *= rainyIntervalMultiplier;
            }

            nextRefillTimer = interval;
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

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            ResolveTeaLadyComponents();
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

        private Vector3 GetKitchenPosition()
        {
            return kitchenStation != null ? kitchenStation.position : transform.position;
        }

        private Vector3 GetCounterPosition()
        {
            return playerCounterLocation != null ? playerCounterLocation.position : transform.position + Vector3.forward * 2f;
        }

        private void SetState(TeaLadyRefillState nextState)
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
            if (teaLadyAnimator != null && !string.IsNullOrWhiteSpace(triggerName))
            {
                teaLadyAnimator.SetTrigger(triggerName);
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
