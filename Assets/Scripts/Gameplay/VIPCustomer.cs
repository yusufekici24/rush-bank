using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public class VIPCustomer : QueueCustomer
    {
        [Header("VIP Identity")]
        [SerializeField, Range(0.1f, 1f)] private float urgentPatienceSecondsMultiplier = 0.5f;
        [SerializeField] private GameObject sparkleAuraObject;
        [SerializeField] private ParticleSystem sparkleAuraParticles;
        [SerializeField] private AudioSource arrivalAudioSource;
        [SerializeField] private bool playArrivalAudio = true;
        [SerializeField] private bool createFallbackRichVisuals = true;
        [SerializeField] private Color shinySuitColor = new Color(1f, 0.72f, 0.18f);
        [SerializeField] private Color sunglassesColor = new Color(1f, 0.88f, 0.22f);

        public UnityEvent OnVipVisualsEnabled = new UnityEvent();

        public override float PatienceSecondsMultiplier => IsVipRequest
            ? urgentPatienceSecondsMultiplier
            : base.PatienceSecondsMultiplier;

        protected override void OnInitialized()
        {
            ApplyVipPresence(IsVipRequest);
        }

        public void ApplyVipPresence(bool active)
        {
            if (active)
            {
                ApplyRichVisualLook();
            }

            if (sparkleAuraObject != null)
            {
                sparkleAuraObject.SetActive(active);
            }

            if (sparkleAuraParticles != null)
            {
                if (active)
                {
                    sparkleAuraParticles.Play();
                }
                else
                {
                    sparkleAuraParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }

            if (active)
            {
                OnVipVisualsEnabled.Invoke();

                if (playArrivalAudio && arrivalAudioSource != null)
                {
                    arrivalAudioSource.Play();
                }
            }
        }

        private void ApplyRichVisualLook()
        {
            var mainRenderer = GetComponent<Renderer>();
            if (mainRenderer != null)
            {
                var sourceMaterial = mainRenderer.sharedMaterial != null
                    ? mainRenderer.sharedMaterial
                    : new Material(Shader.Find("Standard"));
                var material = new Material(sourceMaterial);
                material.color = shinySuitColor;
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", shinySuitColor * 0.18f);
                mainRenderer.sharedMaterial = material;
            }

            if (createFallbackRichVisuals && transform.Find("VIP Golden Sunglasses") == null)
            {
                CreateFallbackSunglasses();
            }
        }

        private void CreateFallbackSunglasses()
        {
            var sunglasses = new GameObject("VIP Golden Sunglasses");
            sunglasses.transform.SetParent(transform, false);
            sunglasses.transform.localPosition = new Vector3(0f, 0.38f, 0.48f);

            CreateLens("Left Lens", sunglasses.transform, new Vector3(-0.16f, 0f, 0f));
            CreateLens("Right Lens", sunglasses.transform, new Vector3(0.16f, 0f, 0f));

            var bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bridge.name = "Bridge";
            bridge.transform.SetParent(sunglasses.transform, false);
            bridge.transform.localPosition = Vector3.zero;
            bridge.transform.localScale = new Vector3(0.14f, 0.04f, 0.04f);
            ApplySunglassesMaterial(bridge);
        }

        private void CreateLens(string lensName, Transform parent, Vector3 localPosition)
        {
            var lens = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lens.name = lensName;
            lens.transform.SetParent(parent, false);
            lens.transform.localPosition = localPosition;
            lens.transform.localScale = new Vector3(0.2f, 0.08f, 0.05f);
            ApplySunglassesMaterial(lens);
        }

        private void ApplySunglassesMaterial(GameObject target)
        {
            if (target.TryGetComponent<Collider>(out var colliderComponent))
            {
                if (Application.isPlaying)
                {
                    Destroy(colliderComponent);
                }
                else
                {
                    DestroyImmediate(colliderComponent);
                }
            }

            if (!target.TryGetComponent<Renderer>(out var targetRenderer))
            {
                return;
            }

            var material = new Material(Shader.Find("Standard"));
            material.color = sunglassesColor;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", sunglassesColor * 0.4f);
            targetRenderer.sharedMaterial = material;
        }
    }
}
