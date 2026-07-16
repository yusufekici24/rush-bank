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

        [Header("Age Psychology")]
        [SerializeField] private float youthPatienceDrainMultiplier = 0.7f;
        [SerializeField] private float middlePatienceDrainMultiplier = 1f;
        [SerializeField] private float elderlyPatienceDrainMultiplier = 1.5f;

        [Header("VIP Pressure")]
        [SerializeField, Range(0.1f, 1f)] private float vipPatienceSecondsMultiplier = 0.55f;

        private GameObject requestIconInstance;
        private float patienceSeconds = 20f;
        private float patienceRemaining;
        private bool patienceRunning;
        private float patienceDrainMultiplier = 1f;
        private static float globalPatienceDrainMultiplier = 1f;

        public CustomerAgeGroup AgeGroup => ageGroup;
        public CustomerGender Gender => gender;
        public CustomerRequestKind RequestKind => requestKind;
        public float Patience01 => patienceSeconds <= 0f ? 0f : Mathf.Clamp01(patienceRemaining / patienceSeconds);
        public bool IsPatienceExpired => patienceRunning && patienceRemaining <= 0f;
        public float PatienceDrainMultiplier => patienceDrainMultiplier;
        public float AgePatienceDrainMultiplier => GetAgePatienceDrainMultiplier(ageGroup);
        public float EffectivePatienceDrainMultiplier => patienceDrainMultiplier * AgePatienceDrainMultiplier * GlobalPatienceDrainMultiplier;
        public bool IsVipRequest => requestKind == CustomerRequestKind.VipSafeRental;
        public bool IsScammer => requestKind == CustomerRequestKind.ScammerCustomer;
        public bool IsPhilanthropist => requestKind == CustomerRequestKind.PhilanthropistCustomer;
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
