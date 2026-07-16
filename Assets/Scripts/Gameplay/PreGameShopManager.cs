using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public enum PreRunBoosterType
    {
        TimeSlow,
        Speed,
        Patience
    }

    public class PreGameShopManager : MonoBehaviour
    {
        public const string PlayerGoldKey = "PlayerGold";
        public const string TimeSlowQtyKey = "Booster_TimeSlow_Qty";
        public const string SpeedQtyKey = "Booster_Speed_Qty";
        public const string PatienceQtyKey = "Booster_Patience_Qty";

        private const string TimeSlowEquippedKey = "Booster_TimeSlow_Equipped";
        private const string SpeedEquippedKey = "Booster_Speed_Equipped";
        private const string PatienceEquippedKey = "Booster_Patience_Equipped";

        [Header("Apply Targets")]
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private MobilePlayerController mobilePlayerController;
        [SerializeField] private ChubbyTopDownInputController topDownController;
        [SerializeField] private bool applyBoostersOnStart;

        [Header("Booster Rules")]
        [SerializeField, Range(0.1f, 1f)] private float timeSlowCountdownMultiplier = 0.85f;
        [SerializeField, Min(1f)] private float speedBoosterMultiplier = 1.2f;
        [SerializeField, Range(0.1f, 1f)] private float patienceDrainMultiplier = 0.75f;
        [SerializeField, Min(1)] private int maxActiveBoostersPerRun = 2;

        public UnityEvent<int> OnGoldChanged = new UnityEvent<int>();
        public UnityEvent OnInventoryChanged = new UnityEvent();
        public UnityEvent OnEquippedBoostersChanged = new UnityEvent();
        public UnityEvent<PreRunBoosterType> OnBoosterApplied = new UnityEvent<PreRunBoosterType>();

        private bool boostersAppliedThisRun;

        public int PlayerGold => PlayerPrefs.GetInt(PlayerGoldKey, 0);
        public int ActiveBoosterCount => GetEquippedCount();

        private void Awake()
        {
            ResolveMissingReferences();
        }

        private void Start()
        {
            if (applyBoostersOnStart)
            {
                ApplyActiveBoosters();
            }
        }

        public bool BuyBooster(string boosterType, int cost)
        {
            if (!TryParseBoosterType(boosterType, out var type) || cost < 0)
            {
                return false;
            }

            var gold = PlayerGold;
            if (gold < cost)
            {
                return false;
            }

            SetPlayerGold(gold - cost);
            SetBoosterQuantity(type, GetBoosterQuantity(type) + 1);
            OnInventoryChanged.Invoke();
            return true;
        }

        public bool BuyBundleAndEquip(int cost)
        {
            if (cost < 0)
            {
                return false;
            }

            var gold = PlayerGold;
            if (gold < cost)
            {
                return false;
            }

            SetPlayerGold(gold - cost);
            SetBoosterQuantity(PreRunBoosterType.TimeSlow, GetBoosterQuantity(PreRunBoosterType.TimeSlow) + 1);
            SetBoosterQuantity(PreRunBoosterType.Speed, GetBoosterQuantity(PreRunBoosterType.Speed) + 1);
            SetBoosterQuantity(PreRunBoosterType.Patience, GetBoosterQuantity(PreRunBoosterType.Patience) + 1);

            SetBoosterEquipped(PreRunBoosterType.TimeSlow, true);
            SetBoosterEquipped(PreRunBoosterType.Speed, true);
            SetBoosterEquipped(PreRunBoosterType.Patience, true);

            OnInventoryChanged.Invoke();
            OnEquippedBoostersChanged.Invoke();
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetPlayerGold(PlayerGold + amount);
        }

        public bool ToggleBoosterForRun(string boosterType, bool equipped)
        {
            if (!TryParseBoosterType(boosterType, out var type))
            {
                return false;
            }

            if (equipped && IsBoosterEquipped(type))
            {
                return true;
            }

            if (equipped)
            {
                if (GetBoosterQuantity(type) <= 0 || GetEquippedCount() >= maxActiveBoostersPerRun)
                {
                    return false;
                }

                SetBoosterEquipped(type, true);
            }
            else
            {
                SetBoosterEquipped(type, false);
            }

            OnEquippedBoostersChanged.Invoke();
            return true;
        }

        public void ClearEquippedBoosters()
        {
            SetBoosterEquipped(PreRunBoosterType.TimeSlow, false);
            SetBoosterEquipped(PreRunBoosterType.Speed, false);
            SetBoosterEquipped(PreRunBoosterType.Patience, false);
            OnEquippedBoostersChanged.Invoke();
        }

        public void ApplyActiveBoosters()
        {
            if (boostersAppliedThisRun)
            {
                return;
            }

            ResolveMissingReferences();
            ResetRuntimeBoosterBaselines();

            TryApplyBooster(PreRunBoosterType.TimeSlow);
            TryApplyBooster(PreRunBoosterType.Speed);
            TryApplyBooster(PreRunBoosterType.Patience);

            boostersAppliedThisRun = true;
            OnInventoryChanged.Invoke();
            OnEquippedBoostersChanged.Invoke();
        }

        public int GetBoosterQuantity(string boosterType)
        {
            return TryParseBoosterType(boosterType, out var type) ? GetBoosterQuantity(type) : 0;
        }

        public bool IsBoosterEquipped(string boosterType)
        {
            return TryParseBoosterType(boosterType, out var type) && IsBoosterEquipped(type);
        }

        private void TryApplyBooster(PreRunBoosterType type)
        {
            if (!IsBoosterEquipped(type) || GetBoosterQuantity(type) <= 0)
            {
                SetBoosterEquipped(type, false);
                return;
            }

            switch (type)
            {
                case PreRunBoosterType.TimeSlow:
                    if (timeManager != null)
                    {
                        timeManager.CountdownRateMultiplier *= timeSlowCountdownMultiplier;
                    }

                    break;

                case PreRunBoosterType.Speed:
                    if (mobilePlayerController != null)
                    {
                        mobilePlayerController.MovementSpeedMultiplier *= speedBoosterMultiplier;
                    }

                    if (topDownController != null)
                    {
                        topDownController.MovementSpeedMultiplier *= speedBoosterMultiplier;
                    }

                    break;

                case PreRunBoosterType.Patience:
                    QueueCustomer.GlobalPatienceDrainMultiplier *= patienceDrainMultiplier;
                    CustomerPatience.GlobalPatienceDrainMultiplier *= patienceDrainMultiplier;
                    break;
            }

            SetBoosterQuantity(type, GetBoosterQuantity(type) - 1);
            SetBoosterEquipped(type, false);
            OnBoosterApplied.Invoke(type);
        }

        private void ResolveMissingReferences()
        {
            if (timeManager == null)
            {
                timeManager = TimeManager.Instance != null ? TimeManager.Instance : FindFirstObjectByType<TimeManager>();
            }

            if (mobilePlayerController == null)
            {
                mobilePlayerController = FindFirstObjectByType<MobilePlayerController>();
            }

            if (topDownController == null)
            {
                topDownController = FindFirstObjectByType<ChubbyTopDownInputController>();
            }
        }

        private static void ResetRuntimeBoosterBaselines()
        {
            var branchMultiplier = GameSettingsManager.Instance != null
                ? GameSettingsManager.Instance.GlobalPatienceMultiplier
                : 1f;

            QueueCustomer.GlobalPatienceDrainMultiplier = branchMultiplier;
            CustomerPatience.GlobalPatienceDrainMultiplier = branchMultiplier;
        }

        private void SetPlayerGold(int value)
        {
            var gold = Mathf.Max(0, value);
            PlayerPrefs.SetInt(PlayerGoldKey, gold);
            PlayerPrefs.Save();
            OnGoldChanged.Invoke(gold);
        }

        private static int GetBoosterQuantity(PreRunBoosterType type)
        {
            return PlayerPrefs.GetInt(GetQuantityKey(type), 0);
        }

        private static void SetBoosterQuantity(PreRunBoosterType type, int quantity)
        {
            PlayerPrefs.SetInt(GetQuantityKey(type), Mathf.Max(0, quantity));
            PlayerPrefs.Save();
        }

        private static bool IsBoosterEquipped(PreRunBoosterType type)
        {
            return PlayerPrefs.GetInt(GetEquippedKey(type), 0) == 1;
        }

        private static void SetBoosterEquipped(PreRunBoosterType type, bool equipped)
        {
            PlayerPrefs.SetInt(GetEquippedKey(type), equipped ? 1 : 0);
            PlayerPrefs.Save();
        }

        private int GetEquippedCount()
        {
            var count = 0;
            count += IsBoosterEquipped(PreRunBoosterType.TimeSlow) ? 1 : 0;
            count += IsBoosterEquipped(PreRunBoosterType.Speed) ? 1 : 0;
            count += IsBoosterEquipped(PreRunBoosterType.Patience) ? 1 : 0;
            return count;
        }

        private static bool TryParseBoosterType(string boosterType, out PreRunBoosterType type)
        {
            if (string.IsNullOrWhiteSpace(boosterType))
            {
                type = default;
                return false;
            }

            if (System.Enum.TryParse(boosterType, true, out type))
            {
                return true;
            }

            var normalized = boosterType?.Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);
            if (string.Equals(normalized, "TimeSlower", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "TimeSlowBooster", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "ZamanBukucu", System.StringComparison.OrdinalIgnoreCase))
            {
                type = PreRunBoosterType.TimeSlow;
                return true;
            }

            if (string.Equals(normalized, "SpeedBooster", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "TurboTabanlik", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "CharacterSpeed", System.StringComparison.OrdinalIgnoreCase))
            {
                type = PreRunBoosterType.Speed;
                return true;
            }

            if (string.Equals(normalized, "AntiGrumpiness", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "PatienceBooster", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "PapatyaCayi", System.StringComparison.OrdinalIgnoreCase))
            {
                type = PreRunBoosterType.Patience;
                return true;
            }

            return System.Enum.TryParse(normalized, true, out type);
        }

        private static string GetQuantityKey(PreRunBoosterType type)
        {
            return type switch
            {
                PreRunBoosterType.TimeSlow => TimeSlowQtyKey,
                PreRunBoosterType.Speed => SpeedQtyKey,
                PreRunBoosterType.Patience => PatienceQtyKey,
                _ => string.Empty
            };
        }

        private static string GetEquippedKey(PreRunBoosterType type)
        {
            return type switch
            {
                PreRunBoosterType.TimeSlow => TimeSlowEquippedKey,
                PreRunBoosterType.Speed => SpeedEquippedKey,
                PreRunBoosterType.Patience => PatienceEquippedKey,
                _ => string.Empty
            };
        }
    }
}
