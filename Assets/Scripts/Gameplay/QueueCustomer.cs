using UnityEngine;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum CustomerAgeGroup
    {
        Youth,
        Middle,
        Elderly
    }

    public enum CustomerGender
    {
        Male,
        Female
    }

    public enum CustomerRequestKind
    {
        Withdraw,
        Deposit,
        OpenAccount,
        CardApplication,
        LoanApplication,
        PassbookPrinting,
        BillPayment,
        CardBlockRemoval,
        CashWithdrawDeposit,
        CurrencyExchange,
        GoldExchange,
        CreditApproval,
        VipSafeRental,
        Thief,
        WireTransfer,
        InsuranceReferral,
        BarutCustomer,
        MobileActivation,
        ScammerCustomer,
        PhilanthropistCustomer
    }

    public class QueueCustomer : MonoBehaviour
    {
        [Header("Runtime Identity")]
        [SerializeField] private CustomerAgeGroup ageGroup;
        [SerializeField] private CustomerGender gender;
        [SerializeField] private CustomerRequestKind requestKind;

        [Header("Visuals")]
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Transform requestIconAnchor;
        [SerializeField] private Image patienceFillImage;
        [SerializeField] private GameObject umbrellaAccessory;
        [SerializeField] private Transform umbrellaAnchor;
        [SerializeField] private bool createFallbackUmbrellaWhenMissing = true;

        [Header("Age Psychology")]
        [SerializeField] private float youthPatienceDrainMultiplier = 0.7f;
        [SerializeField] private float middlePatienceDrainMultiplier = 1f;
        [SerializeField] private float elderlyPatienceDrainMultiplier = 1.5f;

        [Header("VIP Pressure")]
        [SerializeField, Range(0.1f, 1f)] private float vipPatienceSecondsMultiplier = 0.55f;

        private GameObject requestIconInstance;
        private GameObject fallbackUmbrellaInstance;
        private float patienceSeconds = 20f;
        private float patienceRemaining;
        private bool patienceRunning;
        private float patienceDrainMultiplier = 1f;
        private bool hasUmbrella;
        private static float globalPatienceDrainMultiplier = 1f;

        public CustomerAgeGroup AgeGroup => ageGroup;
        public CustomerGender Gender => gender;
        public CustomerRequestKind RequestKind => requestKind;
        public float Patience01 => patienceSeconds <= 0f ? 0f : Mathf.Clamp01(patienceRemaining / patienceSeconds);
        public bool IsPatienceExpired => patienceRunning && patienceRemaining <= 0f;
        public float PatienceDrainMultiplier => patienceDrainMultiplier;
        public float AgePatienceDrainMultiplier => GetAgePatienceDrainMultiplier(ageGroup);
        public float WeatherPatienceDrainMultiplier => DynamicWeatherSystem.Instance != null
            ? DynamicWeatherSystem.Instance.activePatienceMultiplier
            : 1f;
        public float EffectivePatienceDrainMultiplier => patienceDrainMultiplier * AgePatienceDrainMultiplier * GlobalPatienceDrainMultiplier * WeatherPatienceDrainMultiplier;
        public bool IsVipRequest => requestKind == CustomerRequestKind.VipSafeRental;
        public bool IsScammer => requestKind == CustomerRequestKind.ScammerCustomer;
        public bool IsPhilanthropist => requestKind == CustomerRequestKind.PhilanthropistCustomer;
        public bool HasUmbrella => hasUmbrella;
        public virtual float PatienceSecondsMultiplier => IsVipRequest ? vipPatienceSecondsMultiplier : 1f;
        public static float GlobalPatienceDrainMultiplier
        {
            get => globalPatienceDrainMultiplier;
            set => globalPatienceDrainMultiplier = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<Renderer>();
            }
        }

        private void Update()
        {
            if (!patienceRunning)
            {
                return;
            }

            patienceRemaining = Mathf.Max(0f, patienceRemaining - Time.deltaTime * EffectivePatienceDrainMultiplier);
            UpdatePatienceBar();
        }

        public void Initialize(
            CustomerAgeGroup customerAge,
            CustomerGender customerGender,
            CustomerRequestKind customerRequest,
            Material material)
        {
            ageGroup = customerAge;
            gender = customerGender;
            requestKind = customerRequest;

            if (bodyRenderer != null && material != null)
            {
                bodyRenderer.sharedMaterial = material;
            }

            gameObject.name = $"Customer - {ageGroup} {gender} {requestKind}";
            SetUmbrellaActive(DynamicWeatherSystem.Instance != null && DynamicWeatherSystem.Instance.HasUmbrellaForNewCustomers);
            OnInitialized();
        }

        public void StartPatience(float seconds)
        {
            patienceSeconds = Mathf.Max(0.1f, seconds);
            patienceRemaining = patienceSeconds;
            patienceRunning = true;
            UpdatePatienceBar();
        }

        public void SetPatiencePercent(float percent)
        {
            patienceRemaining = patienceSeconds * Mathf.Clamp01(percent);
            patienceRunning = true;
            UpdatePatienceBar();
        }

        public void StopPatience()
        {
            patienceRunning = false;
            UpdatePatienceBar();
        }

        public void SetPatienceDrainMultiplier(float multiplier)
        {
            patienceDrainMultiplier = Mathf.Max(0f, multiplier);
        }

        public void ResetPatienceDrainMultiplier()
        {
            patienceDrainMultiplier = 1f;
        }

        public void RestorePatiencePercent(float percent)
        {
            if (!patienceRunning)
            {
                return;
            }

            var restoreAmount = patienceSeconds * Mathf.Clamp01(percent);
            patienceRemaining = Mathf.Min(patienceSeconds, patienceRemaining + restoreAmount);
            UpdatePatienceBar();
        }

        public void ResetPatienceToFull()
        {
            if (!patienceRunning)
            {
                return;
            }

            patienceRemaining = patienceSeconds;
            UpdatePatienceBar();
        }

        public void SetUmbrellaActive(bool active)
        {
            hasUmbrella = active;

            if (umbrellaAccessory != null)
            {
                umbrellaAccessory.SetActive(active);
                return;
            }

            if (!createFallbackUmbrellaWhenMissing)
            {
                return;
            }

            if (active)
            {
                EnsureFallbackUmbrella();
            }
            else
            {
                DestroyFallbackUmbrella();
            }
        }

        protected virtual void OnInitialized()
        {
        }

        private float GetAgePatienceDrainMultiplier(CustomerAgeGroup customerAgeGroup)
        {
            return customerAgeGroup switch
            {
                CustomerAgeGroup.Youth => youthPatienceDrainMultiplier,
                CustomerAgeGroup.Elderly => elderlyPatienceDrainMultiplier,
                _ => middlePatienceDrainMultiplier
            };
        }

        private void EnsureFallbackUmbrella()
        {
            if (fallbackUmbrellaInstance != null)
            {
                fallbackUmbrellaInstance.SetActive(true);
                return;
            }

            var anchor = umbrellaAnchor != null ? umbrellaAnchor : transform;
            fallbackUmbrellaInstance = new GameObject("Rainy Umbrella");
            fallbackUmbrellaInstance.transform.SetParent(anchor, false);
            fallbackUmbrellaInstance.transform.localPosition = new Vector3(0.32f, 1.25f, 0.08f);
            fallbackUmbrellaInstance.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(fallbackUmbrellaInstance.transform, false);
            canopy.transform.localPosition = Vector3.up * 0.34f;
            canopy.transform.localScale = new Vector3(0.48f, 0.16f, 0.48f);

            var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Handle";
            handle.transform.SetParent(fallbackUmbrellaInstance.transform, false);
            handle.transform.localPosition = Vector3.up * 0.04f;
            handle.transform.localScale = new Vector3(0.025f, 0.32f, 0.025f);

            ApplyUmbrellaMaterial(canopy, new Color(0.45f, 0.82f, 1f));
            ApplyUmbrellaMaterial(handle, new Color(0.25f, 0.18f, 0.12f));
            DisableUmbrellaCollider(canopy);
            DisableUmbrellaCollider(handle);
        }

        private void DestroyFallbackUmbrella()
        {
            if (fallbackUmbrellaInstance != null)
            {
                Destroy(fallbackUmbrellaInstance);
                fallbackUmbrellaInstance = null;
            }
        }

        private static void ApplyUmbrellaMaterial(GameObject target, Color color)
        {
            if (target != null && target.TryGetComponent<Renderer>(out var rendererComponent))
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = color;
                rendererComponent.sharedMaterial = material;
            }
        }

        private static void DisableUmbrellaCollider(GameObject target)
        {
            if (target != null && target.TryGetComponent<Collider>(out var colliderComponent))
            {
                colliderComponent.enabled = false;
            }
        }

        public void ShowRequestIcon(GameObject requestIconPrefab)
        {
            ClearRequestIcon();

            if (requestIconPrefab == null)
            {
                return;
            }

            var anchor = requestIconAnchor != null ? requestIconAnchor : transform;
            requestIconInstance = Instantiate(requestIconPrefab, anchor);
            requestIconInstance.transform.localPosition = Vector3.up * 1.25f;
            requestIconInstance.transform.localRotation = Quaternion.identity;
        }

        public void ClearRequestIcon()
        {
            if (requestIconInstance != null)
            {
                Destroy(requestIconInstance);
                requestIconInstance = null;
            }
        }

        public void MoveTowards(Vector3 targetPosition, float moveSpeed)
        {
            var currentPosition = transform.position;
            var nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.deltaTime);
            transform.position = nextPosition;

            var direction = targetPosition - currentPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction, Vector3.up),
                    8f * Time.deltaTime);
            }
        }

        private void UpdatePatienceBar()
        {
            if (patienceFillImage != null)
            {
                patienceFillImage.fillAmount = Patience01;
            }
        }
    }
}
