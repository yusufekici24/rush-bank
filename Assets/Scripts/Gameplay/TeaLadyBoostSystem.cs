using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum TeaLadyBoostState
    {
        Inactive,
        Active
    }

    public class TeaLadyBoostSystem : MonoBehaviour
    {
        private const string TeaCupId = "tea_lady_boost_cup";

        [Header("Spawn Timing")]
        [SerializeField] private Vector2 spawnIntervalSeconds = new Vector2(50f, 70f);
        [SerializeField, Min(0.1f)] private float teaLadyMoveSpeed = 2.1f;
        [SerializeField, Min(0f)] private float cupLifetimeSeconds = 18f;

        [Header("References")]
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private Transform teaLadySpawnPoint;
        [SerializeField] private Transform teaLadyExitPoint;
        [SerializeField] private Transform teasideTable;
        [SerializeField] private Transform cupPlacementPoint;
        [SerializeField] private GameObject teaLadyPrefab;
        [SerializeField] private GameObject teaCupPrefab;
        [SerializeField] private ParticleSystem caffeineSteamEffect;
        [SerializeField] private ParticleSystem speedTrailEffectPrefab;

        [Header("Boost Targets")]
        [SerializeField] private MobilePlayerController mobilePlayerController;
        [SerializeField] private ChubbyTopDownInputController topDownController;
        [SerializeField] private FastTrackActionSystem fastTrackActionSystem;
        [SerializeField] private UtilityBillSystem utilityBillSystem;
        [SerializeField] private MobileActivationMiniGame mobileActivationMiniGame;
        [SerializeField] private BankingActionSystem bankingActionSystem;
        [SerializeField] private DocumentProcessWorkflow documentProcessWorkflow;
        [SerializeField] private GoldExchangeWorkflow goldExchangeWorkflow;
        [SerializeField] private QueueManager queueManager;

        [Header("Boost")]
        [SerializeField, Min(0.1f)] private float boostDurationSeconds = 8f;
        [SerializeField, Min(0.1f)] private float drinkBoostDurationSeconds = 10f;
        [SerializeField, Min(1f)] private float speedBoostMultiplier = 1.3f;
        [SerializeField, Range(0.05f, 1f)] private float actionTimeMultiplier = 0.6f;

        [Header("UI")]
        [SerializeField] private GameObject boostOverlay;
        [SerializeField] private Slider boostRemainingSlider;
        [SerializeField] private GameObject teaChoiceRoot;
        [SerializeField] private Button drinkButton;
        [SerializeField] private Button serveButton;

        [Header("Hospitality")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private string carryingTrayBool = "CarryingTray";
        [SerializeField] private GameObject trayPrefab;
        [SerializeField] private Transform trayHoldPoint;
        [SerializeField, Min(1)] private int maxTeaPortions = 3;
        [SerializeField, Min(0.1f)] private float hospitalityDurationSeconds = 12f;
        [SerializeField, Min(0.1f)] private float serveInteractionRange = 1.6f;
        [SerializeField, Range(0f, 1f)] private float patienceRestorePercent = 0.4f;
        [SerializeField, Range(0f, 1f)] private float patienceSlowMultiplier = 0.7f;
        [SerializeField, Min(0.1f)] private float patienceSlowSeconds = 8f;

        public UnityEvent<TeaLadyBoostState> OnBoostStateChanged = new UnityEvent<TeaLadyBoostState>();
        public UnityEvent OnTeaCupSpawned = new UnityEvent();
        public UnityEvent OnTeaCupCollected = new UnityEvent();
        public UnityEvent<int> OnTeaPortionsChanged = new UnityEvent<int>();
        public UnityEvent OnHospitalityStarted = new UnityEvent();
        public UnityEvent OnHospitalityEnded = new UnityEvent();

        private TeaLadyBoostState state = TeaLadyBoostState.Inactive;
        private GameObject activeTeaLady;
        private GameObject activeCup;
        private Coroutine teaLadyRoutine;
        private Coroutine boostRoutine;
        private float nextSpawnTimer;
        private float cupLifetimeTimer;
        private float previousFastTrackTimeMultiplier = 1f;
        private float previousUtilityBillTimeMultiplier = 1f;
        private float previousMobileActivationTimeMultiplier = 1f;
        private float previousDocumentTimeMultiplier = 1f;
        private float previousGoldTimeMultiplier = 1f;
        private ParticleSystem activeSpeedTrailEffect;
        private GameObject activeTray;
        private Coroutine hospitalityRoutine;
        private int teaPortions;
        private bool choiceOpen;
        private bool speedBoostApplied;
        private bool actionBoostApplied;
        private bool fastTrackWasEnabled;
        private bool utilityBillWasEnabled;
        private bool bankingWasEnabled;
        private bool documentWasEnabled;
        private bool goldWasEnabled;

        public TeaLadyBoostState State => state;
        public bool IsBoostActive => state == TeaLadyBoostState.Active;
        public bool IsHospitalityModeActive => hospitalityRoutine != null;

        private void Awake()
        {
            if (raycastCamera == null)
            {
                raycastCamera = Camera.main;
            }

            ResolveMissingReferences();
            ResetSpawnTimer();
            SetBoostUi(false, 0f);
            SetChoiceUi(false);
        }

        private void OnEnable()
        {
            if (drinkButton != null)
            {
                drinkButton.onClick.AddListener(ChooseDrink);
            }

            if (serveButton != null)
            {
                serveButton.onClick.AddListener(ChooseServe);
            }
        }

        private void OnDisable()
        {
            if (drinkButton != null)
            {
                drinkButton.onClick.RemoveListener(ChooseDrink);
            }

            if (serveButton != null)
            {
                serveButton.onClick.RemoveListener(ChooseServe);
            }

            if (teaLadyRoutine != null)
            {
                StopCoroutine(teaLadyRoutine);
                teaLadyRoutine = null;
            }

            if (boostRoutine != null)
            {
                StopCoroutine(boostRoutine);
                boostRoutine = null;
            }

            DestroyTeaLady();
            DestroyActiveCup();
            EndHospitalityMode();
            EndBoost();
        }

        private void Update()
        {
            TickTeaLadySpawn();
            TickCupLifetime();
            TickCupInput();
            TickChoiceInput();
            TickHospitalityInput();
        }

        public void ForceSpawnTeaLady()
        {
            if (teaLadyRoutine == null && activeCup == null)
            {
                teaLadyRoutine = StartCoroutine(TeaLadyRoutine());
            }
        }

        private void TickTeaLadySpawn()
        {
            if (teaLadyRoutine != null || activeCup != null)
            {
                return;
            }

            nextSpawnTimer -= Time.deltaTime;
            if (nextSpawnTimer <= 0f)
            {
                teaLadyRoutine = StartCoroutine(TeaLadyRoutine());
            }
        }

        private void TickCupLifetime()
        {
            if (activeCup == null || cupLifetimeSeconds <= 0f)
            {
                return;
            }

            cupLifetimeTimer -= Time.deltaTime;
            if (cupLifetimeTimer <= 0f)
            {
                DestroyActiveCup();
                ResetSpawnTimer();
            }
        }

        private void TickCupInput()
        {
            if (activeCup == null || choiceOpen)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryCollectCup(Input.mousePosition);
            }

            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    TryCollectCup(touch.position);
                }
            }
        }

        private IEnumerator TeaLadyRoutine()
        {
            SpawnTeaLady();
            yield return MoveTeaLadyTo(GetTablePosition());
            SpawnTeaCup();
            yield return PlayTeaLadyWave();
            yield return MoveTeaLadyTo(GetExitPosition());
            DestroyTeaLady();
            ResetSpawnTimer();
            teaLadyRoutine = null;
        }

        private void SpawnTeaLady()
        {
            var spawnPosition = teaLadySpawnPoint != null ? teaLadySpawnPoint.position : transform.position;
            activeTeaLady = teaLadyPrefab != null
                ? Instantiate(teaLadyPrefab, spawnPosition, Quaternion.identity)
                : CreateFallbackTeaLady(spawnPosition);
        }

        private static GameObject CreateFallbackTeaLady(Vector3 position)
        {
            var teaLady = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            teaLady.name = "TeaLadyNPC Prototype";
            teaLady.transform.position = position;
            teaLady.transform.localScale = new Vector3(0.92f, 1.08f, 0.92f);

            var renderer = teaLady.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial(new Color(0.86f, 0.52f, 0.28f));
            }

            var apron = CreatePrimitiveChild(
                teaLady.transform,
                "Floral Apron",
                PrimitiveType.Cube,
                new Vector3(0f, 0.12f, 0.46f),
                new Vector3(0.48f, 0.48f, 0.04f),
                new Color(1f, 0.78f, 0.9f));

            CreatePrimitiveChild(
                apron.transform,
                "Apron Flower A",
                PrimitiveType.Sphere,
                new Vector3(-0.24f, 0.24f, 0.58f),
                new Vector3(0.16f, 0.16f, 0.16f),
                new Color(0.98f, 0.36f, 0.48f));

            CreatePrimitiveChild(
                apron.transform,
                "Apron Flower B",
                PrimitiveType.Sphere,
                new Vector3(0.18f, -0.08f, 0.58f),
                new Vector3(0.13f, 0.13f, 0.13f),
                new Color(0.46f, 0.8f, 0.48f));

            CreatePrimitiveChild(
                apron.transform,
                "Apron Flower C",
                PrimitiveType.Sphere,
                new Vector3(0.06f, 0.32f, 0.58f),
                new Vector3(0.12f, 0.12f, 0.12f),
                new Color(1f, 0.92f, 0.28f));

            CreatePrimitiveChild(
                teaLady.transform,
                "Headscarf",
                PrimitiveType.Sphere,
                new Vector3(0f, 0.72f, 0.02f),
                new Vector3(0.56f, 0.28f, 0.56f),
                new Color(0.95f, 0.28f, 0.42f));

            var tray = CreatePrimitiveChild(
                teaLady.transform,
                "Tea Tray",
                PrimitiveType.Cube,
                new Vector3(0.45f, 0.15f, 0.35f),
                new Vector3(0.58f, 0.04f, 0.32f),
                new Color(0.9f, 0.66f, 0.32f));

            CreatePrimitiveChild(
                tray.transform,
                "Tiny Tea Glass A",
                PrimitiveType.Cylinder,
                new Vector3(-0.14f, 0.12f, 0f),
                new Vector3(0.12f, 0.18f, 0.12f),
                new Color(1f, 0.8f, 0.25f));

            CreatePrimitiveChild(
                tray.transform,
                "Tiny Tea Glass B",
                PrimitiveType.Cylinder,
                new Vector3(0.14f, 0.12f, 0f),
                new Vector3(0.12f, 0.18f, 0.12f),
                new Color(0.42f, 0.26f, 0.16f));

            CreatePrimitiveChild(
                teaLady.transform,
                "Wave Arm",
                PrimitiveType.Cube,
                new Vector3(-0.48f, 0.26f, 0.18f),
                new Vector3(0.12f, 0.45f, 0.12f),
                new Color(0.86f, 0.52f, 0.28f));

            return teaLady;
        }

        private void SpawnTeaCup()
        {
            if (activeCup != null)
            {
                return;
            }

            var position = cupPlacementPoint != null
                ? cupPlacementPoint.position
                : GetTablePosition() + Vector3.up * 0.72f;

            activeCup = teaCupPrefab != null
                ? Instantiate(teaCupPrefab, position, Quaternion.identity)
                : CreateFallbackTeaCup(position);

            activeCup.name = "TeaCup Kafein Boost";
            var marker = activeCup.GetComponent<DeliverableItem>();
            if (marker == null)
            {
                marker = activeCup.AddComponent<DeliverableItem>();
            }

            marker.Configure(TeaCupId, new Color(1f, 0.82f, 0.28f));
            EnsureCupCollider(activeCup);
            CreateCupSteam(activeCup.transform);
            cupLifetimeTimer = cupLifetimeSeconds;
            OnTeaCupSpawned.Invoke();
        }

        private static GameObject CreateFallbackTeaCup(Vector3 position)
        {
            var cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cup.transform.position = position;
            cup.transform.localScale = new Vector3(0.28f, 0.18f, 0.28f);

            var renderer = cup.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(1f, 0.86f, 0.42f);
                material.SetColor("_EmissionColor", new Color(0.7f, 0.42f, 0.06f));
                material.EnableKeyword("_EMISSION");
                renderer.sharedMaterial = material;
            }

            CreatePrimitiveChild(
                cup.transform,
                "PowerUp Glow",
                PrimitiveType.Sphere,
                new Vector3(0f, 0.05f, 0f),
                new Vector3(1.35f, 0.35f, 1.35f),
                new Color(1f, 0.9f, 0.24f, 0.45f));

            return cup;
        }

        private void CreateCupSteam(Transform cup)
        {
            if (cup == null)
            {
                return;
            }

            if (caffeineSteamEffect != null)
            {
                var effect = Instantiate(caffeineSteamEffect, cup);
                effect.transform.localPosition = Vector3.up * 0.65f;
                effect.Play();
                return;
            }

            var effectObject = new GameObject("Tea Steam Effect");
            effectObject.transform.SetParent(cup, false);
            effectObject.transform.localPosition = Vector3.up * 0.65f;
            var particles = effectObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.startColor = new Color(1f, 0.92f, 0.56f, 0.7f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.maxParticles = 28;

            var emission = particles.emission;
            emission.rateOverTime = 14f;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 16f;
            shape.radius = 0.09f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.55f),
                    new Keyframe(0.45f, 1f),
                    new Keyframe(1f, 0.1f)));

            particles.Play();
        }

        private static void EnsureCupCollider(GameObject cup)
        {
            var collider = cup.GetComponent<Collider>();
            if (collider == null)
            {
                collider = cup.AddComponent<SphereCollider>();
            }

            collider.isTrigger = false;
        }

        private void TryCollectCup(Vector2 screenPosition)
        {
            if (raycastCamera == null || activeCup == null)
            {
                return;
            }

            var ray = raycastCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out var hit, 100f))
            {
                return;
            }

            if (hit.collider == null || (hit.collider.gameObject != activeCup && !hit.collider.transform.IsChildOf(activeCup.transform)))
            {
                return;
            }

            CollectCup();
        }

        private void CollectCup()
        {
            OnTeaCupCollected.Invoke();
            SetChoiceUi(true);
        }

        public void ChooseDrink()
        {
            if (!choiceOpen)
            {
                return;
            }

            SetChoiceUi(false);
            DestroyActiveCup();

            if (boostRoutine != null)
            {
                StopCoroutine(boostRoutine);
                EndBoost();
            }

            boostRoutine = StartCoroutine(DrinkBoostRoutine());
        }

        public void ChooseServe()
        {
            if (!choiceOpen)
            {
                return;
            }

            SetChoiceUi(false);
            DestroyActiveCup();
            StartHospitalityMode();
        }

        private void TickChoiceInput()
        {
            if (!choiceOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.D))
            {
                ChooseDrink();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.S))
            {
                ChooseServe();
            }
        }

        private IEnumerator BoostRoutine()
        {
            StartBoost();
            var elapsed = 0f;
            while (elapsed < boostDurationSeconds)
            {
                elapsed += Time.deltaTime;
                var remaining01 = 1f - Mathf.Clamp01(elapsed / boostDurationSeconds);
                SetBoostUi(true, remaining01);
                yield return null;
            }

            EndBoost();
            boostRoutine = null;
        }

        private IEnumerator DrinkBoostRoutine()
        {
            StartSpeedOnlyBoost();
            var elapsed = 0f;
            while (elapsed < drinkBoostDurationSeconds)
            {
                elapsed += Time.deltaTime;
                var remaining01 = 1f - Mathf.Clamp01(elapsed / drinkBoostDurationSeconds);
                SetBoostUi(true, remaining01);
                yield return null;
            }

            EndBoost();
            boostRoutine = null;
        }

        public void ServeNearestWaitingCustomer()
        {
            if (!IsHospitalityModeActive || teaPortions <= 0)
            {
                return;
            }

            var target = FindNearestWaitingCustomer();
            if (target == null)
            {
                return;
            }

            ServeTeaToCustomer(target);
        }

        private void StartHospitalityMode()
        {
            ResolveMissingReferences();
            EndBoost();

            teaPortions = Mathf.Max(1, maxTeaPortions);
            OnTeaPortionsChanged.Invoke(teaPortions);
            SpawnTray();
            SetCounterTasksBlocked(true);

            if (playerAnimator != null && !string.IsNullOrWhiteSpace(carryingTrayBool))
            {
                playerAnimator.SetBool(carryingTrayBool, true);
            }

            if (hospitalityRoutine != null)
            {
                StopCoroutine(hospitalityRoutine);
            }

            hospitalityRoutine = StartCoroutine(HospitalityFailsafeRoutine());
            OnHospitalityStarted.Invoke();
        }

        private IEnumerator HospitalityFailsafeRoutine()
        {
            var elapsed = 0f;
            while (elapsed < hospitalityDurationSeconds && teaPortions > 0)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            EndHospitalityMode();
        }

        private void TickHospitalityInput()
        {
            if (!IsHospitalityModeActive)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                ServeNearestWaitingCustomer();
            }

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                ServeNearestWaitingCustomer();
            }
        }

        private QueueCustomer FindNearestWaitingCustomer()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance != null ? QueueManager.Instance : FindFirstObjectByType<QueueManager>();
            }

            var playerTransform = GetPlayerTransform();
            if (queueManager == null || playerTransform == null)
            {
                return null;
            }

            QueueCustomer nearest = null;
            var nearestDistance = serveInteractionRange * serveInteractionRange;
            var queue = queueManager.CustomerQueue;
            for (var i = 0; i < queue.Count; i++)
            {
                var customerObject = queue[i];
                if (customerObject == null || !customerObject.TryGetComponent<QueueCustomer>(out var customer))
                {
                    continue;
                }

                var distance = (customer.transform.position - playerTransform.position).sqrMagnitude;
                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = customer;
                }
            }

            return nearest;
        }

        private void ServeTeaToCustomer(QueueCustomer customer)
        {
            if (customer == null || teaPortions <= 0)
            {
                return;
            }

            customer.RestorePatiencePercent(patienceRestorePercent);
            customer.SetPatienceDrainMultiplier(patienceSlowMultiplier);
            StartCoroutine(ResetQueueCustomerDrainRoutine(customer, patienceSlowSeconds));

            if (customer.TryGetComponent<CustomerPatience>(out var patience))
            {
                patience.RestorePatience(patienceRestorePercent * 100f);
                patience.SetDrainMultiplier(patienceSlowMultiplier);
                StartCoroutine(ResetCustomerPatienceDrainRoutine(patience, patienceSlowSeconds));
            }

            SpawnHospitalityFeedback(customer.transform);
            teaPortions--;
            OnTeaPortionsChanged.Invoke(teaPortions);

            if (teaPortions <= 0)
            {
                EndHospitalityMode();
            }
        }

        private IEnumerator ResetQueueCustomerDrainRoutine(QueueCustomer customer, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (customer != null)
            {
                customer.ResetPatienceDrainMultiplier();
            }
        }

        private IEnumerator ResetCustomerPatienceDrainRoutine(CustomerPatience patience, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (patience != null)
            {
                patience.ResetDrainMultiplier();
            }
        }

        private void EndHospitalityMode()
        {
            var wasActive = hospitalityRoutine != null || activeTray != null || teaPortions > 0;
            if (!wasActive)
            {
                return;
            }

            if (hospitalityRoutine != null)
            {
                StopCoroutine(hospitalityRoutine);
                hospitalityRoutine = null;
            }

            teaPortions = 0;
            DestroyTray();
            SetCounterTasksBlocked(false);

            if (playerAnimator != null && !string.IsNullOrWhiteSpace(carryingTrayBool))
            {
                playerAnimator.SetBool(carryingTrayBool, false);
            }

            OnTeaPortionsChanged.Invoke(teaPortions);
            OnHospitalityEnded.Invoke();
        }

        private void SpawnTray()
        {
            DestroyTray();
            var parent = trayHoldPoint != null ? trayHoldPoint : GetPlayerTransform();
            if (parent == null)
            {
                return;
            }

            activeTray = trayPrefab != null
                ? Instantiate(trayPrefab, parent)
                : CreateFallbackTray(parent);

            activeTray.transform.localPosition = Vector3.zero;
            activeTray.transform.localRotation = Quaternion.identity;
        }

        private GameObject CreateFallbackTray(Transform parent)
        {
            var tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tray.name = "Hospitality Tea Tray";
            tray.transform.SetParent(parent, false);
            tray.transform.localScale = new Vector3(0.58f, 0.05f, 0.34f);
            var renderer = tray.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial(new Color(0.9f, 0.62f, 0.28f));
            }

            var collider = tray.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            return tray;
        }

        private void DestroyTray()
        {
            if (activeTray != null)
            {
                Destroy(activeTray);
                activeTray = null;
            }
        }

        private void SetCounterTasksBlocked(bool blocked)
        {
            if (blocked)
            {
                fastTrackWasEnabled = fastTrackActionSystem == null || fastTrackActionSystem.enabled;
                utilityBillWasEnabled = utilityBillSystem == null || utilityBillSystem.enabled;
                bankingWasEnabled = bankingActionSystem == null || bankingActionSystem.enabled;
                documentWasEnabled = documentProcessWorkflow == null || documentProcessWorkflow.enabled;
                goldWasEnabled = goldExchangeWorkflow == null || goldExchangeWorkflow.enabled;
            }

            if (fastTrackActionSystem != null)
            {
                fastTrackActionSystem.enabled = !blocked && fastTrackWasEnabled;
            }

            if (utilityBillSystem != null)
            {
                utilityBillSystem.enabled = !blocked && utilityBillWasEnabled;
            }

            if (bankingActionSystem != null)
            {
                bankingActionSystem.enabled = !blocked && bankingWasEnabled;
            }

            if (documentProcessWorkflow != null)
            {
                documentProcessWorkflow.enabled = !blocked && documentWasEnabled;
            }

            if (goldExchangeWorkflow != null)
            {
                goldExchangeWorkflow.enabled = !blocked && goldWasEnabled;
            }
        }

        private void SpawnHospitalityFeedback(Transform target)
        {
            if (target == null)
            {
                return;
            }

            var labelObject = new GameObject("Hospitality Tea Floating Text");
            labelObject.transform.SetParent(target, false);
            labelObject.transform.localPosition = Vector3.up * 1.85f;
            labelObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = "\u2615 <3";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.24f;
            label.color = new Color(1f, 0.55f, 0.78f);

            var particles = labelObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = new Color(1f, 0.42f, 0.72f, 0.86f);
            main.startLifetime = 0.75f;
            main.startSpeed = 0.75f;
            main.startSize = 0.08f;
            main.maxParticles = 18;

            var emission = particles.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)12) });

            particles.Play();
            Destroy(labelObject, 1.25f);
        }

        private void StartBoost()
        {
            ResolveMissingReferences();

            if (mobilePlayerController != null)
            {
                mobilePlayerController.MovementSpeedMultiplier *= speedBoostMultiplier;
            }

            if (topDownController != null)
            {
                topDownController.MovementSpeedMultiplier *= speedBoostMultiplier;
            }

            speedBoostApplied = mobilePlayerController != null || topDownController != null;
            actionBoostApplied = true;

            if (fastTrackActionSystem != null)
            {
                previousFastTrackTimeMultiplier = fastTrackActionSystem.ActionTimeMultiplier;
                fastTrackActionSystem.ActionTimeMultiplier = previousFastTrackTimeMultiplier * actionTimeMultiplier;
            }

            if (utilityBillSystem != null)
            {
                previousUtilityBillTimeMultiplier = utilityBillSystem.ActionTimeMultiplier;
                utilityBillSystem.ActionTimeMultiplier = previousUtilityBillTimeMultiplier * actionTimeMultiplier;
            }

            if (mobileActivationMiniGame != null)
            {
                previousMobileActivationTimeMultiplier = mobileActivationMiniGame.ActionTimeMultiplier;
                mobileActivationMiniGame.ActionTimeMultiplier = previousMobileActivationTimeMultiplier * actionTimeMultiplier;
            }

            if (documentProcessWorkflow != null)
            {
                previousDocumentTimeMultiplier = documentProcessWorkflow.ActionTimeMultiplier;
                documentProcessWorkflow.ActionTimeMultiplier = previousDocumentTimeMultiplier * actionTimeMultiplier;
            }

            if (goldExchangeWorkflow != null)
            {
                previousGoldTimeMultiplier = goldExchangeWorkflow.ActionTimeMultiplier;
                goldExchangeWorkflow.ActionTimeMultiplier = previousGoldTimeMultiplier * actionTimeMultiplier;
            }

            SetBoostUi(true, 1f);
            CreateSpeedTrailEffect();
            SetState(TeaLadyBoostState.Active);
        }

        private void StartSpeedOnlyBoost()
        {
            ResolveMissingReferences();

            if (mobilePlayerController != null)
            {
                mobilePlayerController.MovementSpeedMultiplier *= speedBoostMultiplier;
            }

            if (topDownController != null)
            {
                topDownController.MovementSpeedMultiplier *= speedBoostMultiplier;
            }

            speedBoostApplied = mobilePlayerController != null || topDownController != null;
            actionBoostApplied = false;
            SetBoostUi(true, 1f);
            CreateSpeedTrailEffect();
            SetState(TeaLadyBoostState.Active);
        }

        private void EndBoost()
        {
            if (state != TeaLadyBoostState.Active)
            {
                SetBoostUi(false, 0f);
                DestroySpeedTrailEffect();
                return;
            }

            if (mobilePlayerController != null)
            {
                mobilePlayerController.MovementSpeedMultiplier /= speedBoostApplied ? speedBoostMultiplier : 1f;
            }

            if (topDownController != null)
            {
                topDownController.MovementSpeedMultiplier /= speedBoostApplied ? speedBoostMultiplier : 1f;
            }

            speedBoostApplied = false;

            if (actionBoostApplied && fastTrackActionSystem != null)
            {
                fastTrackActionSystem.ActionTimeMultiplier = previousFastTrackTimeMultiplier;
            }

            if (actionBoostApplied && utilityBillSystem != null)
            {
                utilityBillSystem.ActionTimeMultiplier = previousUtilityBillTimeMultiplier;
            }

            if (actionBoostApplied && mobileActivationMiniGame != null)
            {
                mobileActivationMiniGame.ActionTimeMultiplier = previousMobileActivationTimeMultiplier;
            }

            if (actionBoostApplied && documentProcessWorkflow != null)
            {
                documentProcessWorkflow.ActionTimeMultiplier = previousDocumentTimeMultiplier;
            }

            if (actionBoostApplied && goldExchangeWorkflow != null)
            {
                goldExchangeWorkflow.ActionTimeMultiplier = previousGoldTimeMultiplier;
            }

            actionBoostApplied = false;
            SetBoostUi(false, 0f);
            DestroySpeedTrailEffect();
            SetState(TeaLadyBoostState.Inactive);
        }

        private IEnumerator MoveTeaLadyTo(Vector3 destination)
        {
            if (activeTeaLady == null)
            {
                yield break;
            }

            var agent = activeTeaLady.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(destination);
                while (activeTeaLady != null && agent.pathPending)
                {
                    yield return null;
                }

                while (activeTeaLady != null && agent.remainingDistance > agent.stoppingDistance)
                {
                    yield return null;
                }

                yield break;
            }

            while (activeTeaLady != null && (activeTeaLady.transform.position - destination).sqrMagnitude > 0.02f)
            {
                var current = activeTeaLady.transform.position;
                activeTeaLady.transform.position = Vector3.MoveTowards(current, destination, teaLadyMoveSpeed * Time.deltaTime);

                var direction = destination - current;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    activeTeaLady.transform.rotation = Quaternion.Slerp(
                        activeTeaLady.transform.rotation,
                        Quaternion.LookRotation(direction, Vector3.up),
                        8f * Time.deltaTime);
                }

                var wobble = Mathf.Sin(Time.time * 8f) * 5f;
                activeTeaLady.transform.rotation *= Quaternion.Euler(0f, 0f, wobble * Time.deltaTime);
                yield return null;
            }
        }

        private IEnumerator PlayTeaLadyWave()
        {
            if (activeTeaLady == null)
            {
                yield break;
            }

            var waveArm = activeTeaLady.transform.Find("Wave Arm");
            var elapsed = 0f;
            while (elapsed < 0.65f)
            {
                elapsed += Time.deltaTime;

                if (waveArm != null)
                {
                    var wave = Mathf.Sin(elapsed * 24f) * 26f;
                    waveArm.localRotation = Quaternion.Euler(0f, 0f, wave);
                }

                var bodyWobble = Mathf.Sin(elapsed * 18f) * 3f;
                activeTeaLady.transform.localRotation *= Quaternion.Euler(0f, bodyWobble * Time.deltaTime, 0f);
                yield return null;
            }

            if (waveArm != null)
            {
                waveArm.localRotation = Quaternion.identity;
            }
        }

        private Vector3 GetTablePosition()
        {
            return teasideTable != null ? teasideTable.position : transform.position;
        }

        private Vector3 GetExitPosition()
        {
            return teaLadyExitPoint != null ? teaLadyExitPoint.position : GetTablePosition() + Vector3.right * 3f;
        }

        private void DestroyTeaLady()
        {
            if (activeTeaLady != null)
            {
                Destroy(activeTeaLady);
                activeTeaLady = null;
            }
        }

        private void DestroyActiveCup()
        {
            if (activeCup != null)
            {
                Destroy(activeCup);
                activeCup = null;
            }
        }

        private void CreateSpeedTrailEffect()
        {
            DestroySpeedTrailEffect();

            var target = GetPlayerTransform();
            if (target == null)
            {
                return;
            }

            activeSpeedTrailEffect = speedTrailEffectPrefab != null
                ? Instantiate(speedTrailEffectPrefab, target)
                : CreateFallbackSpeedTrail(target);

            activeSpeedTrailEffect.transform.localPosition = new Vector3(0f, 0.22f, -0.48f);
            activeSpeedTrailEffect.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            activeSpeedTrailEffect.Play();
        }

        private ParticleSystem CreateFallbackSpeedTrail(Transform target)
        {
            var effectObject = new GameObject("KafeinMode Speed Lines");
            effectObject.transform.SetParent(target, false);
            var particles = effectObject.AddComponent<ParticleSystem>();

            var main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.38f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.7f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.92f, 0.18f, 0.95f),
                new Color(1f, 0.62f, 0.08f, 0.75f));
            main.maxParticles = 44;

            var emission = particles.emission;
            emission.rateOverTime = 34f;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 24f;
            shape.radius = 0.28f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

            var trails = particles.trails;
            trails.enabled = true;
            trails.ratio = 0.72f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.16f);

            return particles;
        }

        private void DestroySpeedTrailEffect()
        {
            if (activeSpeedTrailEffect == null)
            {
                return;
            }

            Destroy(activeSpeedTrailEffect.gameObject);
            activeSpeedTrailEffect = null;
        }

        private void SetBoostUi(bool visible, float remaining01)
        {
            if (boostOverlay != null)
            {
                boostOverlay.SetActive(visible);
            }

            if (boostRemainingSlider != null)
            {
                boostRemainingSlider.gameObject.SetActive(visible);
                boostRemainingSlider.value = Mathf.Clamp01(remaining01);
            }
        }

        private void SetChoiceUi(bool visible)
        {
            choiceOpen = visible;
            if (teaChoiceRoot != null)
            {
                teaChoiceRoot.SetActive(visible);
            }

            if (drinkButton != null)
            {
                drinkButton.gameObject.SetActive(visible);
            }

            if (serveButton != null)
            {
                serveButton.gameObject.SetActive(visible);
            }
        }

        private void ResetSpawnTimer()
        {
            var min = Mathf.Min(spawnIntervalSeconds.x, spawnIntervalSeconds.y);
            var max = Mathf.Max(spawnIntervalSeconds.x, spawnIntervalSeconds.y);
            nextSpawnTimer = Random.Range(min, max);
        }

        private void ResolveMissingReferences()
        {
            if (mobilePlayerController == null)
            {
                mobilePlayerController = FindFirstObjectByType<MobilePlayerController>();
            }

            if (topDownController == null)
            {
                topDownController = FindFirstObjectByType<ChubbyTopDownInputController>();
            }

            if (fastTrackActionSystem == null)
            {
                fastTrackActionSystem = FindFirstObjectByType<FastTrackActionSystem>();
            }

            if (utilityBillSystem == null)
            {
                utilityBillSystem = FindFirstObjectByType<UtilityBillSystem>();
            }

            if (mobileActivationMiniGame == null)
            {
                mobileActivationMiniGame = FindFirstObjectByType<MobileActivationMiniGame>();
            }

            if (bankingActionSystem == null)
            {
                bankingActionSystem = FindFirstObjectByType<BankingActionSystem>();
            }

            if (documentProcessWorkflow == null)
            {
                documentProcessWorkflow = FindFirstObjectByType<DocumentProcessWorkflow>();
            }

            if (goldExchangeWorkflow == null)
            {
                goldExchangeWorkflow = FindFirstObjectByType<GoldExchangeWorkflow>();
            }

            if (queueManager == null)
            {
                queueManager = QueueManager.Instance != null ? QueueManager.Instance : FindFirstObjectByType<QueueManager>();
            }

            if (playerAnimator == null)
            {
                var playerTransform = GetPlayerTransform();
                if (playerTransform != null)
                {
                    playerAnimator = playerTransform.GetComponentInChildren<Animator>();
                }
            }
        }

        private Transform GetPlayerTransform()
        {
            if (mobilePlayerController != null)
            {
                return mobilePlayerController.transform;
            }

            return topDownController != null ? topDownController.transform : null;
        }

        private static GameObject CreatePrimitiveChild(
            Transform parent,
            string objectName,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Color color)
        {
            var child = GameObject.CreatePrimitive(primitiveType);
            child.name = objectName;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;

            var collider = child.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial(color);
            }

            return child;
        }

        private static Material CreateMaterial(Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = color;

            if (color.a < 0.99f)
            {
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }

            return material;
        }

        private void SetState(TeaLadyBoostState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            OnBoostStateChanged.Invoke(state);
        }
    }
}
