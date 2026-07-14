using System.Collections;
using System.Collections.Generic;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public enum HeistRaidState
    {
        Normal,
        RaidActive,
        PlayerSpotted,
        RaidResolved
    }

    public class HeistRaidSystem : MonoBehaviour
    {
        [System.Serializable]
        private class HeistThiefRuntime
        {
            public GameObject Root;
            public TextMesh EyeCue;
            public TextMesh SpeechCue;
            public Transform LeftArm;
            public Transform RightArm;
            public Transform LootBag;
            public bool IsLookingAtCounter;
            public float NextLookSwitchTime;
        }

        [Header("Trigger")]
        [SerializeField, Range(0f, 1f)] private float raidChance = 0.12f;
        [SerializeField] private Collider bankEntranceTrigger;
        [SerializeField] private CashDeliverySystem cashDeliverySystem;
        [SerializeField] private PlayerInteraction playerInteraction;
        [SerializeField] private Transform player;
        [SerializeField] private Transform playerResetPoint;

        [Header("Movement")]
        [SerializeField] private MobilePlayerController mobilePlayerController;
        [SerializeField] private ChubbyTopDownInputController topDownController;
        [SerializeField, Range(0.05f, 1f)] private float raidSpeedMultiplier = 0.5f;
        [SerializeField, Min(0.1f)] private float spottedFreezeSeconds = 2f;
        [SerializeField, Min(0.001f)] private float movementDetectionThreshold = 0.012f;

        [Header("Thieves")]
        [SerializeField] private GameObject heistThiefPrefab;
        [SerializeField] private Transform[] thiefSpawnPoints;
        [SerializeField, Min(1)] private int minThiefCount = 2;
        [SerializeField, Min(1)] private int maxThiefCount = 3;
        [SerializeField, Min(0.5f)] private float lookSwitchMinSeconds = 3f;
        [SerializeField, Min(0.5f)] private float lookSwitchMaxSeconds = 4f;
        [SerializeField, Range(-1f, 1f)] private float lineOfSightDotThreshold = 0.62f;
        [SerializeField, Min(0.5f)] private float lineOfSightDistance = 7f;

        [Header("Alarm and Police")]
        [SerializeField] private Transform alarmButton;
        [SerializeField, Min(0.2f)] private float alarmUseDistance = 1.25f;
        [SerializeField] private GameObject policePrefab;
        [SerializeField] private Transform policeSpawnPoint;
        [SerializeField] private Transform policeExitPoint;
        [SerializeField, Min(0.1f)] private float policeMoveSpeed = 3.2f;
        [SerializeField, Min(0f)] private float policeArrestPauseSeconds = 0.45f;
        [SerializeField, Min(0f)] private float heistRewardMultiplier = 2f;

        [Header("Feedback")]
        [SerializeField] private GameObject playerFearEffectPrefab;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip raidMusic;
        [SerializeField] private AudioClip alarmSirenClip;
        [SerializeField] private AudioClip spottedClip;
        [SerializeField] private Light[] dimmedLights;
        [SerializeField, Range(0.05f, 1f)] private float raidLightIntensityMultiplier = 0.55f;

        public UnityEvent<HeistRaidState> OnStateChanged = new UnityEvent<HeistRaidState>();
        public UnityEvent OnRaidStarted = new UnityEvent();
        public UnityEvent OnPlayerSpotted = new UnityEvent();
        public UnityEvent OnRaidResolved = new UnityEvent();

        private readonly List<HeistThiefRuntime> thieves = new List<HeistThiefRuntime>(3);
        private readonly List<GameObject> policeOfficers = new List<GameObject>(3);
        private HeistRaidState state = HeistRaidState.Normal;
        private bool rolledForCurrentDelivery;
        private bool wasInsideEntrance;
        private bool speedPenaltyApplied;
        private bool playerFrozen;
        private GameObject activePlayerFearEffect;
        private float previousMobileSpeedMultiplier = 1f;
        private float previousTopDownSpeedMultiplier = 1f;
        private Vector3 lastPlayerPosition;
        private Color previousAmbientLight;
        private readonly List<float> previousLightIntensities = new List<float>(8);
        private Coroutine raidRoutine;
        private Coroutine spottedRoutine;

        public HeistRaidState State => state;
        public bool IsRaidActive => state != HeistRaidState.Normal;

        private void Awake()
        {
            ResolveMissingReferences();
            previousAmbientLight = RenderSettings.ambientLight;
            lastPlayerPosition = player != null ? player.position : transform.position;
            ApplySelectedBranchSettings();
        }

        private void OnDisable()
        {
            if (raidRoutine != null)
            {
                StopCoroutine(raidRoutine);
                raidRoutine = null;
            }

            if (spottedRoutine != null)
            {
                StopCoroutine(spottedRoutine);
                spottedRoutine = null;
            }

            ClearRaidObjects();
            RestoreRaidVisuals();
            DestroyPlayerFearEffect();
            ClearSpeedPenalty(true);
            SetPlayerFrozen(false);

            if (TimeManager.Instance != null && state != HeistRaidState.Normal)
            {
                TimeManager.Instance.FreezeTime(false);
            }
        }

        private void Update()
        {
            TickCashDeliveryEntryRoll();

            if (state == HeistRaidState.RaidActive)
            {
                TickThiefLookDirections();
                TickThiefComedyAnimation();
                TickPlayerFearEffect();
                TickStealthDetection();
                TickAlarmInput();
            }

            if (player != null)
            {
                lastPlayerPosition = player.position;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayerCollider(other))
            {
                return;
            }

            TryRollForRaid();
        }

        public void ForceStartRaid()
        {
            if (state == HeistRaidState.Normal)
            {
                StartRaid();
            }
        }

        public void ApplyBranchSettings(BranchSettings settings)
        {
            raidChance = settings.WithClampedValues().thiefAttackChance;
        }

        public void ApplySelectedBranchSettings()
        {
            if (GameSettingsManager.Instance == null)
            {
                return;
            }

            ApplyBranchSettings(GameSettingsManager.Instance.CurrentBranchSettings);
        }

        private void TickCashDeliveryEntryRoll()
        {
            if (cashDeliverySystem == null)
            {
                return;
            }

            if (cashDeliverySystem.State != CashDeliveryState.CarryingCashBag)
            {
                rolledForCurrentDelivery = false;
                wasInsideEntrance = false;
                return;
            }

            if (bankEntranceTrigger == null || player == null)
            {
                return;
            }

            var isInsideEntrance = bankEntranceTrigger.bounds.Contains(player.position);
            if (isInsideEntrance && !wasInsideEntrance)
            {
                TryRollForRaid();
            }

            wasInsideEntrance = isInsideEntrance;
        }

        private void TryRollForRaid()
        {
            if (rolledForCurrentDelivery || state != HeistRaidState.Normal)
            {
                return;
            }

            if (cashDeliverySystem == null || cashDeliverySystem.State != CashDeliveryState.CarryingCashBag)
            {
                return;
            }

            rolledForCurrentDelivery = true;
            if (Random.value <= raidChance)
            {
                StartRaid();
            }
        }

        private void StartRaid()
        {
            ResolveMissingReferences();
            raidRoutine = StartCoroutine(RunRaidStart());
        }

        private IEnumerator RunRaidStart()
        {
            SetState(HeistRaidState.RaidActive);
            OnRaidStarted.Invoke();

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.FreezeTime(true);
            }

            ApplyRaidVisuals();
            ApplySpeedPenalty();
            CreatePlayerFearEffect();
            SpawnThieves();

            if (audioSource != null && raidMusic != null)
            {
                audioSource.clip = raidMusic;
                audioSource.loop = true;
                audioSource.Play();
            }

            yield return null;
            raidRoutine = null;
        }

        private void TickThiefLookDirections()
        {
            for (var i = 0; i < thieves.Count; i++)
            {
                var thief = thieves[i];
                if (thief.Root == null || Time.time < thief.NextLookSwitchTime)
                {
                    continue;
                }

                SetThiefLookState(thief, !thief.IsLookingAtCounter);
                thief.NextLookSwitchTime = Time.time + Random.Range(lookSwitchMinSeconds, lookSwitchMaxSeconds);
            }
        }

        private void TickStealthDetection()
        {
            if (player == null || playerFrozen)
            {
                return;
            }

            if (!IsPlayerInOpenSight())
            {
                return;
            }

            if (HasPlayerMovedOrInteracted())
            {
                TriggerPlayerSpotted();
            }
        }

        private void TickAlarmInput()
        {
            if (player == null || alarmButton == null || playerFrozen)
            {
                return;
            }

            var distance = Vector3.Distance(player.position, alarmButton.position);
            if (distance > alarmUseDistance || !WasActionPressed())
            {
                return;
            }

            if (IsPlayerInOpenSight())
            {
                TriggerPlayerSpotted();
                return;
            }

            raidRoutine = StartCoroutine(ResolveRaidRoutine());
        }

        private bool HasPlayerMovedOrInteracted()
        {
            var moved = player != null
                && (player.position - lastPlayerPosition).sqrMagnitude > movementDetectionThreshold * movementDetectionThreshold;

            return moved || WasActionPressed();
        }

        private bool WasActionPressed()
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.F))
            {
                return true;
            }

            if (Input.GetMouseButtonDown(0))
            {
                return true;
            }

            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        }

        private bool IsPlayerInOpenSight()
        {
            if (player == null)
            {
                return false;
            }

            for (var i = 0; i < thieves.Count; i++)
            {
                var thief = thieves[i];
                if (thief.Root == null || !thief.IsLookingAtCounter)
                {
                    continue;
                }

                var toPlayer = player.position - thief.Root.transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > lineOfSightDistance * lineOfSightDistance)
                {
                    continue;
                }

                var dot = Vector3.Dot(thief.Root.transform.forward, toPlayer.normalized);
                if (dot >= lineOfSightDotThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        private void TriggerPlayerSpotted()
        {
            if (state == HeistRaidState.PlayerSpotted)
            {
                return;
            }

            if (spottedRoutine != null)
            {
                StopCoroutine(spottedRoutine);
            }

            spottedRoutine = StartCoroutine(PlayerSpottedRoutine());
        }

        private IEnumerator PlayerSpottedRoutine()
        {
            SetState(HeistRaidState.PlayerSpotted);
            OnPlayerSpotted.Invoke();
            PlaySpottedEffect();

            if (audioSource != null && spottedClip != null)
            {
                audioSource.PlayOneShot(spottedClip);
            }

            SetPlayerFrozen(true);
            yield return new WaitForSeconds(spottedFreezeSeconds);

            ResetPlayerPositionAfterSpot();
            SetPlayerFrozen(false);
            SetState(HeistRaidState.RaidActive);
            spottedRoutine = null;
        }

        private IEnumerator ResolveRaidRoutine()
        {
            SetState(HeistRaidState.RaidResolved);

            if (audioSource != null)
            {
                audioSource.loop = false;
                if (alarmSirenClip != null)
                {
                    audioSource.PlayOneShot(alarmSirenClip);
                }
            }

            yield return SpawnPoliceAndArrestRoutine();
            var restockedByCashSystem = cashDeliverySystem != null
                && cashDeliverySystem.CompleteRestockFromHeist(heistRewardMultiplier);

            if (restockedByCashSystem)
            {
                ClearSpeedPenalty(false);
            }
            else
            {
                ClearSpeedPenalty(true);
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.AddTime(heistRewardMultiplier * 8f);
                }
            }

            ClearRaidObjects();
            RestoreRaidVisuals();
            DestroyPlayerFearEffect();

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.FreezeTime(false);
            }

            OnRaidResolved.Invoke();
            SetState(HeistRaidState.Normal);
            rolledForCurrentDelivery = true;
            raidRoutine = null;
        }

        private IEnumerator SpawnPoliceAndArrestRoutine()
        {
            var count = Mathf.Max(1, thieves.Count);
            for (var i = 0; i < count; i++)
            {
                var spawnPosition = policeSpawnPoint != null ? policeSpawnPoint.position : GetDefaultPoliceSpawnPosition();
                var police = policePrefab != null
                    ? Instantiate(policePrefab, spawnPosition, Quaternion.identity)
                    : CreateFallbackPolice(spawnPosition);

                policeOfficers.Add(police);
            }

            for (var i = 0; i < thieves.Count; i++)
            {
                var thief = thieves[i];
                if (thief.Root == null)
                {
                    continue;
                }

                var police = policeOfficers[Mathf.Min(i, policeOfficers.Count - 1)];
                yield return MoveObjectTo(police.transform, thief.Root.transform.position + Vector3.back * 0.55f);
                PlayThiefSurrender(thief);
                yield return new WaitForSeconds(policeArrestPauseSeconds);
                Destroy(thief.Root);
                thief.Root = null;
            }

            var exitPosition = policeExitPoint != null ? policeExitPoint.position : GetDefaultPoliceSpawnPosition();
            for (var i = 0; i < policeOfficers.Count; i++)
            {
                var police = policeOfficers[i];
                if (police != null)
                {
                    yield return MoveObjectTo(police.transform, exitPosition);
                    Destroy(police);
                }
            }

            policeOfficers.Clear();
        }

        private void PlayThiefSurrender(HeistThiefRuntime thief)
        {
            if (thief.Root == null)
            {
                return;
            }

            if (thief.EyeCue != null)
            {
                thief.EyeCue.text = "!!";
                thief.EyeCue.color = new Color(1f, 0.88f, 0.18f);
            }

            if (thief.SpeechCue != null)
            {
                thief.SpeechCue.text = "TAMAM!";
                thief.SpeechCue.color = new Color(0.6f, 1f, 0.68f);
            }

            if (thief.LeftArm != null)
            {
                thief.LeftArm.localRotation = Quaternion.Euler(0f, 0f, 92f);
            }

            if (thief.RightArm != null)
            {
                thief.RightArm.localRotation = Quaternion.Euler(0f, 0f, -92f);
            }

            if (thief.LootBag == null)
            {
                return;
            }

            var bag = thief.LootBag.gameObject;
            thief.LootBag = null;
            bag.transform.SetParent(null, true);
            EnableColliders(bag, true);

            var body = bag.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = bag.AddComponent<Rigidbody>();
            }

            body.mass = 0.65f;
            body.AddForce((Vector3.up * 3.2f) + (Random.insideUnitSphere * 0.8f), ForceMode.Impulse);
            body.AddTorque(Random.insideUnitSphere * 2.4f, ForceMode.Impulse);
            Destroy(bag, 2.25f);
        }

        private void SpawnThieves()
        {
            ClearRaidObjects();

            var count = Random.Range(
                Mathf.Max(1, minThiefCount),
                Mathf.Max(minThiefCount, maxThiefCount) + 1);

            for (var i = 0; i < count; i++)
            {
                var spawnPosition = GetThiefSpawnPosition(i);
                var thiefObject = heistThiefPrefab != null
                    ? Instantiate(heistThiefPrefab, spawnPosition, Quaternion.identity)
                    : CreateFallbackThief(spawnPosition, i);

                var runtime = new HeistThiefRuntime
                {
                    Root = thiefObject,
                    EyeCue = EnsureEyeCue(thiefObject.transform),
                    SpeechCue = EnsureSpeechCue(thiefObject.transform),
                    LeftArm = FindOptionalChild(thiefObject.transform, "Left Waving Arm"),
                    RightArm = FindOptionalChild(thiefObject.transform, "Right Waving Arm"),
                    LootBag = FindOptionalChild(thiefObject.transform, "Tiny Loot Bag"),
                    NextLookSwitchTime = Time.time + Random.Range(lookSwitchMinSeconds, lookSwitchMaxSeconds)
                };

                SetThiefLookState(runtime, i % 2 == 0);
                thieves.Add(runtime);
            }
        }

        private void SetThiefLookState(HeistThiefRuntime thief, bool lookingAtCounter)
        {
            if (thief.Root == null)
            {
                return;
            }

            thief.IsLookingAtCounter = lookingAtCounter;
            var lookTarget = alarmButton != null ? alarmButton.position : transform.position + Vector3.back * 2f;
            var direction = lookTarget - thief.Root.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector3.forward;
            }

            var lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            thief.Root.transform.rotation = lookingAtCounter ? lookRotation : lookRotation * Quaternion.Euler(0f, 180f, 0f);

            if (thief.EyeCue != null)
            {
                thief.EyeCue.text = lookingAtCounter ? "EYE!" : "zzz";
                thief.EyeCue.color = lookingAtCounter ? new Color(1f, 0.16f, 0.12f) : new Color(0.35f, 0.72f, 1f);
            }

            if (thief.SpeechCue != null)
            {
                thief.SpeechCue.text = lookingAtCounter ? "ELLER!" : "psst...";
                thief.SpeechCue.color = lookingAtCounter ? new Color(1f, 0.78f, 0.32f) : new Color(0.72f, 0.88f, 1f);
            }
        }

        private TextMesh EnsureEyeCue(Transform thief)
        {
            var cueObject = new GameObject("Heist Eye Cue");
            cueObject.transform.SetParent(thief, false);
            cueObject.transform.localPosition = Vector3.up * 1.35f;
            cueObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

            var cue = cueObject.AddComponent<TextMesh>();
            cue.anchor = TextAnchor.MiddleCenter;
            cue.alignment = TextAlignment.Center;
            cue.characterSize = 0.22f;
            cue.fontStyle = FontStyle.Bold;
            cue.text = "EYE!";
            cue.color = Color.red;
            return cue;
        }

        private TextMesh EnsureSpeechCue(Transform thief)
        {
            var cueObject = new GameObject("Heist Speech Cue");
            cueObject.transform.SetParent(thief, false);
            cueObject.transform.localPosition = new Vector3(0f, 1.08f, 0.05f);
            cueObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

            var cue = cueObject.AddComponent<TextMesh>();
            cue.anchor = TextAnchor.MiddleCenter;
            cue.alignment = TextAlignment.Center;
            cue.characterSize = 0.16f;
            cue.fontStyle = FontStyle.Bold;
            cue.text = "ELLER!";
            cue.color = new Color(1f, 0.78f, 0.32f);
            return cue;
        }

        private GameObject CreateFallbackThief(Vector3 position, int index)
        {
            var thief = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            thief.name = $"HeistThiefNPC Prototype {index + 1}";
            thief.transform.position = position;
            thief.transform.localScale = new Vector3(0.88f, 1.04f, 0.88f);
            SetRendererColor(thief, new Color(0.22f, 0.22f, 0.28f));

            var mask = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mask.name = "Crooked Sock Mask";
            mask.transform.SetParent(thief.transform, false);
            mask.transform.localPosition = new Vector3(0.02f, 0.59f, 0.44f);
            mask.transform.localRotation = Quaternion.Euler(0f, 0f, -5f);
            mask.transform.localScale = new Vector3(0.56f, 0.2f, 0.07f);
            SetRendererColor(mask, new Color(0.08f, 0.08f, 0.1f));
            DisableCollider(mask);

            var leftEyeHole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftEyeHole.name = "Wonky Eye Hole L";
            leftEyeHole.transform.SetParent(thief.transform, false);
            leftEyeHole.transform.localPosition = new Vector3(-0.12f, 0.61f, 0.49f);
            leftEyeHole.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            leftEyeHole.transform.localScale = new Vector3(0.1f, 0.055f, 0.025f);
            SetRendererColor(leftEyeHole, new Color(0.95f, 0.95f, 0.9f));
            DisableCollider(leftEyeHole);

            var rightEyeHole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightEyeHole.name = "Wonky Eye Hole R";
            rightEyeHole.transform.SetParent(thief.transform, false);
            rightEyeHole.transform.localPosition = new Vector3(0.13f, 0.57f, 0.49f);
            rightEyeHole.transform.localRotation = Quaternion.Euler(0f, 0f, 11f);
            rightEyeHole.transform.localScale = new Vector3(0.08f, 0.065f, 0.025f);
            SetRendererColor(rightEyeHole, new Color(0.95f, 0.95f, 0.9f));
            DisableCollider(rightEyeHole);

            var leftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftArm.name = "Left Waving Arm";
            leftArm.transform.SetParent(thief.transform, false);
            leftArm.transform.localPosition = new Vector3(-0.48f, 0.22f, 0.08f);
            leftArm.transform.localScale = new Vector3(0.12f, 0.42f, 0.12f);
            SetRendererColor(leftArm, new Color(0.22f, 0.22f, 0.28f));
            DisableCollider(leftArm);

            var rightArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightArm.name = "Right Waving Arm";
            rightArm.transform.SetParent(thief.transform, false);
            rightArm.transform.localPosition = new Vector3(0.48f, 0.22f, 0.08f);
            rightArm.transform.localScale = new Vector3(0.12f, 0.42f, 0.12f);
            SetRendererColor(rightArm, new Color(0.22f, 0.22f, 0.28f));
            DisableCollider(rightArm);

            var bag = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bag.name = "Tiny Loot Bag";
            bag.transform.SetParent(thief.transform, false);
            bag.transform.localPosition = new Vector3(0.42f, -0.1f, 0.18f);
            bag.transform.localScale = new Vector3(0.32f, 0.28f, 0.32f);
            SetRendererColor(bag, new Color(0.42f, 0.32f, 0.22f));
            DisableCollider(bag);
            return thief;
        }

        private GameObject CreateFallbackPolice(Vector3 position)
        {
            var police = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            police.name = "Police Officer Prototype";
            police.transform.position = position;
            police.transform.localScale = new Vector3(0.92f, 1.08f, 0.92f);
            SetRendererColor(police, new Color(0.12f, 0.32f, 0.82f));

            var hat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hat.name = "Police Hat";
            hat.transform.SetParent(police.transform, false);
            hat.transform.localPosition = new Vector3(0f, 0.76f, 0f);
            hat.transform.localScale = new Vector3(0.48f, 0.12f, 0.44f);
            SetRendererColor(hat, new Color(0.04f, 0.12f, 0.36f));
            DisableCollider(hat);
            return police;
        }

        private void TickThiefComedyAnimation()
        {
            for (var i = 0; i < thieves.Count; i++)
            {
                var thief = thieves[i];
                if (thief.Root == null)
                {
                    continue;
                }

                var wave = Mathf.Sin(Time.time * 6.5f + i) * 18f;
                var baseArmAngle = thief.IsLookingAtCounter ? 35f : 8f;

                if (thief.LeftArm != null)
                {
                    thief.LeftArm.localRotation = Quaternion.Euler(0f, 0f, baseArmAngle + wave);
                }

                if (thief.RightArm != null)
                {
                    thief.RightArm.localRotation = Quaternion.Euler(0f, 0f, -baseArmAngle - wave);
                }
            }
        }

        private void CreatePlayerFearEffect()
        {
            DestroyPlayerFearEffect();

            if (player == null)
            {
                return;
            }

            activePlayerFearEffect = playerFearEffectPrefab != null
                ? Instantiate(playerFearEffectPrefab, player)
                : CreateFallbackPlayerFearEffect(player);

            activePlayerFearEffect.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        }

        private GameObject CreateFallbackPlayerFearEffect(Transform target)
        {
            var root = new GameObject("Shaking Knees Fear Cue");
            root.transform.SetParent(target, false);

            var leftKnee = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftKnee.name = "Left Shaking Knee";
            leftKnee.transform.SetParent(root.transform, false);
            leftKnee.transform.localPosition = new Vector3(-0.16f, 0.05f, 0.26f);
            leftKnee.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);
            SetRendererColor(leftKnee, new Color(1f, 0.86f, 0.36f));
            DisableCollider(leftKnee);

            var rightKnee = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightKnee.name = "Right Shaking Knee";
            rightKnee.transform.SetParent(root.transform, false);
            rightKnee.transform.localPosition = new Vector3(0.16f, 0.05f, 0.26f);
            rightKnee.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);
            SetRendererColor(rightKnee, new Color(1f, 0.86f, 0.36f));
            DisableCollider(rightKnee);

            var labelObject = new GameObject("Fear Label");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = "...";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.16f;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.8f, 0.92f, 1f);
            return root;
        }

        private void TickPlayerFearEffect()
        {
            if (activePlayerFearEffect == null)
            {
                return;
            }

            var left = activePlayerFearEffect.transform.Find("Left Shaking Knee");
            var right = activePlayerFearEffect.transform.Find("Right Shaking Knee");
            var jitter = Mathf.Sin(Time.time * 28f) * 0.045f;

            if (left != null)
            {
                left.localPosition = new Vector3(-0.16f + jitter, 0.05f, 0.26f);
            }

            if (right != null)
            {
                right.localPosition = new Vector3(0.16f - jitter, 0.05f, 0.26f);
            }
        }

        private void DestroyPlayerFearEffect()
        {
            if (activePlayerFearEffect == null)
            {
                return;
            }

            Destroy(activePlayerFearEffect);
            activePlayerFearEffect = null;
        }

        private void ApplySpeedPenalty()
        {
            if (speedPenaltyApplied)
            {
                return;
            }

            speedPenaltyApplied = true;
            if (mobilePlayerController != null)
            {
                previousMobileSpeedMultiplier = mobilePlayerController.MovementSpeedMultiplier;
                mobilePlayerController.MovementSpeedMultiplier = previousMobileSpeedMultiplier * raidSpeedMultiplier;
            }

            if (topDownController != null)
            {
                previousTopDownSpeedMultiplier = topDownController.MovementSpeedMultiplier;
                topDownController.MovementSpeedMultiplier = previousTopDownSpeedMultiplier * raidSpeedMultiplier;
            }
        }

        private void ClearSpeedPenalty(bool restorePrevious)
        {
            if (!speedPenaltyApplied)
            {
                return;
            }

            if (restorePrevious)
            {
                if (mobilePlayerController != null)
                {
                    mobilePlayerController.MovementSpeedMultiplier = previousMobileSpeedMultiplier;
                }

                if (topDownController != null)
                {
                    topDownController.MovementSpeedMultiplier = previousTopDownSpeedMultiplier;
                }
            }

            speedPenaltyApplied = false;
        }

        private void SetPlayerFrozen(bool frozen)
        {
            playerFrozen = frozen;

            if (mobilePlayerController != null)
            {
                mobilePlayerController.MovementSpeedMultiplier = frozen
                    ? 0f
                    : speedPenaltyApplied
                        ? previousMobileSpeedMultiplier * raidSpeedMultiplier
                        : previousMobileSpeedMultiplier;
            }

            if (topDownController != null)
            {
                topDownController.MovementSpeedMultiplier = frozen
                    ? 0f
                    : speedPenaltyApplied
                        ? previousTopDownSpeedMultiplier * raidSpeedMultiplier
                        : previousTopDownSpeedMultiplier;
            }

            if (player != null && player.TryGetComponent<Rigidbody>(out var body))
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void ResetPlayerPositionAfterSpot()
        {
            if (player == null)
            {
                return;
            }

            if (playerResetPoint != null)
            {
                player.position = playerResetPoint.position;
                return;
            }

            player.position -= player.forward * 0.75f;
        }

        private void PlaySpottedEffect()
        {
            if (player == null)
            {
                return;
            }

            var effectObject = new GameObject("Heist Spotted Exclamation");
            effectObject.transform.position = player.position + Vector3.up * 1.6f;
            var text = effectObject.AddComponent<TextMesh>();
            text.text = "!";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.55f;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(1f, 0.15f, 0.1f);
            Destroy(effectObject, 1.1f);
        }

        private void ApplyRaidVisuals()
        {
            previousAmbientLight = RenderSettings.ambientLight;
            RenderSettings.ambientLight = previousAmbientLight * 0.58f;
            previousLightIntensities.Clear();

            if (dimmedLights == null)
            {
                return;
            }

            for (var i = 0; i < dimmedLights.Length; i++)
            {
                var light = dimmedLights[i];
                if (light == null)
                {
                    previousLightIntensities.Add(0f);
                    continue;
                }

                previousLightIntensities.Add(light.intensity);
                light.intensity *= raidLightIntensityMultiplier;
            }
        }

        private void RestoreRaidVisuals()
        {
            RenderSettings.ambientLight = previousAmbientLight;
            if (dimmedLights == null)
            {
                return;
            }

            for (var i = 0; i < dimmedLights.Length; i++)
            {
                if (dimmedLights[i] != null && i < previousLightIntensities.Count)
                {
                    dimmedLights[i].intensity = previousLightIntensities[i];
                }
            }
        }

        private IEnumerator MoveObjectTo(Transform target, Vector3 destination)
        {
            while (target != null && (target.position - destination).sqrMagnitude > 0.02f)
            {
                var current = target.position;
                target.position = Vector3.MoveTowards(current, destination, policeMoveSpeed * Time.deltaTime);

                var direction = destination - current;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    target.rotation = Quaternion.Slerp(
                        target.rotation,
                        Quaternion.LookRotation(direction.normalized, Vector3.up),
                        8f * Time.deltaTime);
                }

                yield return null;
            }
        }

        private Vector3 GetThiefSpawnPosition(int index)
        {
            if (thiefSpawnPoints != null && index < thiefSpawnPoints.Length && thiefSpawnPoints[index] != null)
            {
                return thiefSpawnPoints[index].position;
            }

            return new Vector3(-0.9f + index * 0.9f, 0.9f, 0.85f);
        }

        private Vector3 GetDefaultPoliceSpawnPosition()
        {
            return player != null ? player.position + new Vector3(-4f, 0f, -3f) : transform.position + Vector3.left * 3f;
        }

        private bool IsPlayerCollider(Collider other)
        {
            if (player == null)
            {
                return false;
            }

            return other.transform == player || other.transform.IsChildOf(player);
        }

        private void ClearRaidObjects()
        {
            for (var i = 0; i < thieves.Count; i++)
            {
                if (thieves[i].Root != null)
                {
                    Destroy(thieves[i].Root);
                }
            }

            thieves.Clear();

            for (var i = 0; i < policeOfficers.Count; i++)
            {
                if (policeOfficers[i] != null)
                {
                    Destroy(policeOfficers[i]);
                }
            }

            policeOfficers.Clear();
        }

        private void ResolveMissingReferences()
        {
            if (cashDeliverySystem == null)
            {
                cashDeliverySystem = FindFirstObjectByType<CashDeliverySystem>();
            }

            if (playerInteraction == null)
            {
                playerInteraction = FindFirstObjectByType<PlayerInteraction>();
            }

            if (player == null && playerInteraction != null)
            {
                player = playerInteraction.transform;
            }

            if (mobilePlayerController == null && player != null)
            {
                mobilePlayerController = player.GetComponent<MobilePlayerController>();
            }

            if (topDownController == null && player != null)
            {
                topDownController = player.GetComponent<ChubbyTopDownInputController>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void SetState(HeistRaidState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            OnStateChanged.Invoke(state);
        }

        private static void DisableCollider(GameObject target)
        {
            if (target != null && target.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }
        }

        private static void EnableColliders(GameObject target, bool enabled)
        {
            if (target == null)
            {
                return;
            }

            var colliders = target.GetComponentsInChildren<Collider>();
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = enabled;
            }
        }

        private static Transform FindOptionalChild(Transform parent, string childName)
        {
            return parent != null ? parent.Find(childName) : null;
        }

        private static void SetRendererColor(GameObject target, Color color)
        {
            if (target == null || !target.TryGetComponent<Renderer>(out var renderer))
            {
                return;
            }

            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            renderer.sharedMaterial = material;
        }
    }
}
