using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public enum UrgencyTier
    {
        Tier1_Normal,
        Tier2_Critical
    }

    public class StaffRequestUrgency : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private Transform staffRoot;
        [SerializeField] private Renderer[] staffRenderers;
        [SerializeField] private Transform iconAnchor;
        [SerializeField] private ManagerSatisfactionSystem managerSatisfactionSystem;
        [SerializeField] private Behaviour[] deskSystemsToLockOnFailure;
        [SerializeField] private GameObject deskLockedVisual;

        [Header("Timing")]
        [SerializeField, Min(1f)] private float tier1DurationSeconds = 30f;
        [SerializeField, Min(1f)] private float tier2DurationSeconds = 10f;
        [SerializeField, Min(0.1f)] private float criticalTintSeconds = 2f;

        [Header("Visuals")]
        [SerializeField] private GameObject tier1IconPrefab;
        [SerializeField] private GameObject tier2IconPrefab;
        [SerializeField] private ParticleSystem exclamationParticlePrefab;
        [SerializeField] private Color tier1Color = new Color(1f, 0.86f, 0.1f);
        [SerializeField] private Color tier2Color = new Color(1f, 0.12f, 0.08f);
        [SerializeField] private Color criticalTintColor = new Color(1f, 0.22f, 0.16f);
        [SerializeField, Min(1f)] private float tier2IconScale = 1.45f;
        [SerializeField, Min(0.1f)] private float pulseSpeed = 6f;
        [SerializeField, Min(0.01f)] private float pulseAmount = 0.18f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip tier2NervousLoop;
        [SerializeField] private AudioClip failureBuzzer;
        [SerializeField] private AudioClip tier1SuccessChime;
        [SerializeField] private AudioClip tier2SuccessChime;

        [Header("Rewards")]
        [SerializeField, Min(0)] private int tier1GoldReward = 50;
        [SerializeField, Min(0)] private int tier2GoldReward = 100;
        [SerializeField, Min(0f)] private float tier1SatisfactionReward = 10f;
        [SerializeField, Min(0f)] private float tier2SatisfactionReward = 25f;
        [SerializeField, Min(0f)] private float failureSatisfactionPenalty = 25f;

        public UnityEvent<UrgencyTier> OnTierChanged = new UnityEvent<UrgencyTier>();
        public UnityEvent<UrgencyTier> OnRequestResolved = new UnityEvent<UrgencyTier>();
        public UnityEvent OnRequestFailed = new UnityEvent();
        public UnityEvent<float> OnCountdownChanged = new UnityEvent<float>();

        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        private Coroutine countdownRoutine;
        private Coroutine tintRoutine;
        private Coroutine pulseRoutine;
        private GameObject activeIcon;
        private ParticleSystem activeExclamationParticles;
        private Color[] originalRendererColors = System.Array.Empty<Color>();
        private bool[] lockedPreviousStates = System.Array.Empty<bool>();
        private bool requestActive;
        private bool deskLocked;

        public UrgencyTier CurrentTier { get; private set; } = UrgencyTier.Tier1_Normal;
        public float RemainingSeconds { get; private set; }
        public bool IsRequestActive => requestActive;
        public bool IsDeskLocked => deskLocked;

        private void Awake()
        {
            ResolveMissingReferences();
            CaptureOriginalRendererColors();
            SetDeskLocked(false);
            ClearVisuals();
        }

        private void OnDisable()
        {
            StopAllRuntimeEffects();
        }

        public void BeginRequest()
        {
            BeginRequest(staffRoot);
        }

        public void BeginRequest(Transform requestStaffRoot)
        {
            staffRoot = requestStaffRoot != null ? requestStaffRoot : staffRoot;
            ResolveMissingReferences();
            CaptureOriginalRendererColors();
            StopAllRuntimeEffects();
            SetDeskLocked(false);
            requestActive = true;
            SetTier(UrgencyTier.Tier1_Normal);
            countdownRoutine = StartCoroutine(TierCountdownRoutine());
        }

        public void ResolveRequest()
        {
            if (!requestActive)
            {
                return;
            }

            var resolvedTier = CurrentTier;
            var gold = resolvedTier == UrgencyTier.Tier2_Critical ? tier2GoldReward : tier1GoldReward;
            var satisfaction = resolvedTier == UrgencyTier.Tier2_Critical ? tier2SatisfactionReward : tier1SatisfactionReward;
            AddGold(gold);
            managerSatisfactionSystem?.AddSatisfaction(satisfaction);
            PlayOneShot(resolvedTier == UrgencyTier.Tier2_Critical ? tier2SuccessChime : tier1SuccessChime);
            if (resolvedTier == UrgencyTier.Tier2_Critical)
            {
                SpawnFloatingText("Phew! Thank you!", tier2Color);
            }

            OnRequestResolved.Invoke(resolvedTier);
            ClearRequest();
        }

        public void FailRequest()
        {
            if (!requestActive)
            {
                return;
            }

            SetDeskLocked(true);
            managerSatisfactionSystem?.DeductSatisfaction(failureSatisfactionPenalty);
            PlayOneShot(failureBuzzer);
            SpawnFloatingText("MISSED!", Color.red);
            OnRequestFailed.Invoke();
            ClearRequest(false);
        }

        public void ClearRequest()
        {
            ClearRequest(true);
        }

        private void ClearRequest(bool unlockDesk)
        {
            requestActive = false;
            RemainingSeconds = 0f;
            StopAllRuntimeEffects();
            RestoreRendererTint();
            ClearVisuals();
            if (unlockDesk)
            {
                SetDeskLocked(false);
            }
        }

        private IEnumerator TierCountdownRoutine()
        {
            RemainingSeconds = tier1DurationSeconds;
            while (RemainingSeconds > 0f && requestActive && CurrentTier == UrgencyTier.Tier1_Normal)
            {
                RemainingSeconds -= Time.deltaTime;
                OnCountdownChanged.Invoke(Mathf.Max(0f, RemainingSeconds));
                yield return null;
            }

            if (!requestActive)
            {
                yield break;
            }

            SetTier(UrgencyTier.Tier2_Critical);
            RemainingSeconds = tier2DurationSeconds;
            while (RemainingSeconds > 0f && requestActive && CurrentTier == UrgencyTier.Tier2_Critical)
            {
                RemainingSeconds -= Time.deltaTime;
                OnCountdownChanged.Invoke(Mathf.Max(0f, RemainingSeconds));
                yield return null;
            }

            if (requestActive)
            {
                FailRequest();
            }
        }

        private void SetTier(UrgencyTier tier)
        {
            CurrentTier = tier;
            RefreshIcon();

            if (tier == UrgencyTier.Tier2_Critical)
            {
                StartCriticalEffects();
            }
            else
            {
                StopCriticalEffects();
                RestoreRendererTint();
            }

            OnTierChanged.Invoke(CurrentTier);
        }

        private void StartCriticalEffects()
        {
            if (tintRoutine != null)
            {
                StopCoroutine(tintRoutine);
            }

            tintRoutine = StartCoroutine(TintToCriticalRoutine());

            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
            }

            pulseRoutine = StartCoroutine(PulseIconRoutine());
            SpawnExclamationParticles();

            if (audioSource != null && tier2NervousLoop != null)
            {
                audioSource.clip = tier2NervousLoop;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        private void StopCriticalEffects()
        {
            if (tintRoutine != null)
            {
                StopCoroutine(tintRoutine);
                tintRoutine = null;
            }

            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }

            if (audioSource != null && audioSource.clip == tier2NervousLoop)
            {
                audioSource.Stop();
                audioSource.loop = false;
                audioSource.clip = null;
            }

            if (activeExclamationParticles != null)
            {
                Destroy(activeExclamationParticles.gameObject);
                activeExclamationParticles = null;
            }
        }

        private IEnumerator TintToCriticalRoutine()
        {
            var elapsed = 0f;
            while (elapsed < criticalTintSeconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / criticalTintSeconds);
                ApplyRendererTint(t);
                yield return null;
            }

            ApplyRendererTint(1f);
            tintRoutine = null;
        }

        private IEnumerator PulseIconRoutine()
        {
            if (activeIcon == null)
            {
                yield break;
            }

            var baseScale = Vector3.one * tier2IconScale;
            while (requestActive && CurrentTier == UrgencyTier.Tier2_Critical && activeIcon != null)
            {
                var pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                activeIcon.transform.localScale = baseScale * pulse;
                yield return null;
            }
        }

        private void RefreshIcon()
        {
            ClearIconOnly();

            var prefab = CurrentTier == UrgencyTier.Tier2_Critical ? tier2IconPrefab : tier1IconPrefab;
            var anchor = iconAnchor != null ? iconAnchor : GetIconAnchorFallback();
            if (prefab != null)
            {
                activeIcon = Instantiate(prefab, anchor);
                activeIcon.transform.localPosition = Vector3.up * 1.75f;
                activeIcon.transform.localScale = CurrentTier == UrgencyTier.Tier2_Critical
                    ? Vector3.one * tier2IconScale
                    : Vector3.one;
                TintIcon(activeIcon, CurrentTier == UrgencyTier.Tier2_Critical ? tier2Color : tier1Color);
                return;
            }

            activeIcon = CreateFallbackIcon(anchor);
        }

        private GameObject CreateFallbackIcon(Transform anchor)
        {
            var icon = new GameObject(CurrentTier == UrgencyTier.Tier2_Critical ? "Critical Staff Urgency Icon" : "Staff Urgency Icon");
            icon.transform.SetParent(anchor, false);
            icon.transform.localPosition = Vector3.up * 1.75f;
            icon.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            icon.transform.localScale = CurrentTier == UrgencyTier.Tier2_Critical ? Vector3.one * tier2IconScale : Vector3.one;

            var label = icon.AddComponent<TextMesh>();
            label.text = CurrentTier == UrgencyTier.Tier2_Critical ? "!!" : "A4";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.28f;
            label.fontStyle = FontStyle.Bold;
            label.color = CurrentTier == UrgencyTier.Tier2_Critical ? tier2Color : tier1Color;
            return icon;
        }

        private void SpawnExclamationParticles()
        {
            if (activeExclamationParticles != null)
            {
                Destroy(activeExclamationParticles.gameObject);
                activeExclamationParticles = null;
            }

            var anchor = iconAnchor != null ? iconAnchor : GetIconAnchorFallback();
            if (exclamationParticlePrefab != null)
            {
                activeExclamationParticles = Instantiate(exclamationParticlePrefab, anchor);
                activeExclamationParticles.transform.localPosition = Vector3.up * 1.95f;
                activeExclamationParticles.Play();
                return;
            }

            var particleObject = new GameObject("Critical Exclamation Particles");
            particleObject.transform.SetParent(anchor, false);
            particleObject.transform.localPosition = Vector3.up * 1.95f;
            activeExclamationParticles = particleObject.AddComponent<ParticleSystem>();
            var main = activeExclamationParticles.main;
            main.loop = true;
            main.startLifetime = 0.5f;
            main.startSpeed = 0.55f;
            main.startSize = 0.08f;
            main.startColor = tier2Color;

            var emission = activeExclamationParticles.emission;
            emission.rateOverTime = 8f;

            var shape = activeExclamationParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.28f;
            activeExclamationParticles.Play();
        }

        private void SpawnFloatingText(string message, Color color)
        {
            var anchor = GetIconAnchorFallback();
            var textObject = new GameObject("Staff Request Floating Text");
            textObject.transform.SetParent(anchor, false);
            textObject.transform.localPosition = Vector3.up * 2.25f;
            textObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

            var label = textObject.AddComponent<TextMesh>();
            label.text = message;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.16f;
            label.fontStyle = FontStyle.Bold;
            label.color = color;
            Destroy(textObject, 1.8f);
        }

        private void ApplyRendererTint(float t)
        {
            if (staffRenderers == null)
            {
                return;
            }

            for (var i = 0; i < staffRenderers.Length; i++)
            {
                var rendererComponent = staffRenderers[i];
                if (rendererComponent == null)
                {
                    continue;
                }

                var original = i < originalRendererColors.Length ? originalRendererColors[i] : Color.white;
                rendererComponent.GetPropertyBlock(propertyBlock);
                var tintColor = Color.Lerp(original, criticalTintColor, t);
                propertyBlock.SetColor("_Color", tintColor);
                propertyBlock.SetColor("_BaseColor", tintColor);
                rendererComponent.SetPropertyBlock(propertyBlock);
            }
        }

        private void RestoreRendererTint()
        {
            if (staffRenderers == null)
            {
                return;
            }

            for (var i = 0; i < staffRenderers.Length; i++)
            {
                var rendererComponent = staffRenderers[i];
                if (rendererComponent == null)
                {
                    continue;
                }

                rendererComponent.GetPropertyBlock(propertyBlock);
                var original = i < originalRendererColors.Length ? originalRendererColors[i] : Color.white;
                propertyBlock.SetColor("_Color", original);
                propertyBlock.SetColor("_BaseColor", original);
                rendererComponent.SetPropertyBlock(propertyBlock);
            }
        }

        private void CaptureOriginalRendererColors()
        {
            if (staffRenderers == null || staffRenderers.Length == 0)
            {
                originalRendererColors = System.Array.Empty<Color>();
                return;
            }

            originalRendererColors = new Color[staffRenderers.Length];
            for (var i = 0; i < staffRenderers.Length; i++)
            {
                var rendererComponent = staffRenderers[i];
                originalRendererColors[i] = rendererComponent != null && rendererComponent.sharedMaterial != null
                    ? rendererComponent.sharedMaterial.color
                    : Color.white;
            }
        }

        private void SetDeskLocked(bool locked)
        {
            if (deskLocked == locked)
            {
                return;
            }

            deskLocked = locked;
            if (deskLockedVisual != null)
            {
                deskLockedVisual.SetActive(locked);
            }

            if (deskSystemsToLockOnFailure == null || deskSystemsToLockOnFailure.Length == 0)
            {
                return;
            }

            if (locked)
            {
                lockedPreviousStates = new bool[deskSystemsToLockOnFailure.Length];
                for (var i = 0; i < deskSystemsToLockOnFailure.Length; i++)
                {
                    var system = deskSystemsToLockOnFailure[i];
                    if (system == null)
                    {
                        continue;
                    }

                    lockedPreviousStates[i] = system.enabled;
                    system.enabled = false;
                }
                return;
            }

            for (var i = 0; i < deskSystemsToLockOnFailure.Length; i++)
            {
                var system = deskSystemsToLockOnFailure[i];
                if (system == null)
                {
                    continue;
                }

                system.enabled = i < lockedPreviousStates.Length && lockedPreviousStates[i];
            }

            lockedPreviousStates = System.Array.Empty<bool>();
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

        private void PlayOneShot(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void StopAllRuntimeEffects()
        {
            if (countdownRoutine != null)
            {
                StopCoroutine(countdownRoutine);
                countdownRoutine = null;
            }

            StopCriticalEffects();
        }

        private void ClearVisuals()
        {
            ClearIconOnly();
            if (activeExclamationParticles != null)
            {
                Destroy(activeExclamationParticles.gameObject);
                activeExclamationParticles = null;
            }
        }

        private void ClearIconOnly()
        {
            if (activeIcon != null)
            {
                Destroy(activeIcon);
                activeIcon = null;
            }
        }

        private void TintIcon(GameObject icon, Color color)
        {
            var text = icon.GetComponentInChildren<TextMesh>();
            if (text != null)
            {
                text.color = color;
            }

            var renderers = icon.GetComponentsInChildren<Renderer>();
            for (var i = 0; i < renderers.Length; i++)
            {
                var rendererComponent = renderers[i];
                if (rendererComponent.sharedMaterial != null)
                {
                    var material = new Material(rendererComponent.sharedMaterial);
                    material.color = color;
                    rendererComponent.sharedMaterial = material;
                }
            }
        }

        private Transform GetIconAnchorFallback()
        {
            return staffRoot != null ? staffRoot : transform;
        }

        private void ResolveMissingReferences()
        {
            if (staffRoot == null)
            {
                staffRoot = transform;
            }

            if (iconAnchor == null)
            {
                iconAnchor = staffRoot;
            }

            if (staffRenderers == null || staffRenderers.Length == 0)
            {
                staffRenderers = staffRoot != null
                    ? staffRoot.GetComponentsInChildren<Renderer>()
                    : GetComponentsInChildren<Renderer>();
            }

            if (managerSatisfactionSystem == null)
            {
                managerSatisfactionSystem = FindFirstObjectByType<ManagerSatisfactionSystem>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }
    }
}
