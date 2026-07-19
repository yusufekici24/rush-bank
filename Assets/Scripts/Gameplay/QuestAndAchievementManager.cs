using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum CustomerType
    {
        Any,
        Standard,
        Vip,
        Philanthropist,
        Barut,
        Scammer
    }

    public enum PassiveRewardType
    {
        None,
        TemporaryPatience,
        TemporarySpeed,
        TemporaryActionSpeed,
        TemporaryDigitalCalm,
        PermanentWalkSpeed,
        PermanentVipServiceSpeed,
        PermanentFeastDuration,
        PermanentAmbientMusic
    }

    public class QuestAndAchievementManager : MonoBehaviour
    {
        public static QuestAndAchievementManager Instance { get; private set; }

        private const string DailyDateKey = "DailyQuest_Date";
        private const string QuestPrefix = "DailyQuest_";
        private const string AchievementPrefix = "Achievement_";
        public const string PermanentWalkSpeedBonusKey = "Achievement_PermanentWalkSpeedBonus";
        public const string PermanentVipServiceSpeedBonusKey = "Achievement_PermanentVipServiceSpeedBonus";
        public const string PermanentFeastDurationBonusKey = "Achievement_PermanentFeastDurationBonus";
        public const string PermanentAmbientMusicUnlockedKey = "Achievement_PermanentAmbientMusicUnlocked";

        [Serializable]
        public class Quest
        {
            public string ID;
            public string title;
            [Min(1)] public int targetValue = 1;
            [Min(0)] public int currentValue;
            [Min(0)] public int goldReward;
            public bool isCompleted;
            public PassiveRewardType rewardBoostType;
            [Min(0f)] public float rewardBoostValue;
            [Min(0f)] public float rewardBoostSeconds;
        }

        [Serializable]
        public class Achievement
        {
            public string ID;
            public string title;
            [Min(1)] public int targetValue = 1;
            [Min(0)] public int currentValue;
            [Min(0)] public int goldReward;
            public bool isCompleted;
            public PassiveRewardType permanentRewardType;
            [Min(0f)] public float permanentRewardValue;
        }

        [Header("Content")]
        [SerializeField] private List<Quest> dailyQuests = new List<Quest>();
        [SerializeField] private List<Achievement> achievements = new List<Achievement>();
        [SerializeField] private bool resetDailyQuestsByCalendarDay = true;

        [Header("Optional Event Sources")]
        [SerializeField] private TeaLadyBoostSystem teaLadyBoostSystem;
        [SerializeField] private WireTransferMiniGame wireTransferMiniGame;
        [SerializeField] private MobileActivationMiniGame mobileActivationMiniGame;
        [SerializeField] private ManagerITSupportEvent managerITSupportEvent;
        [SerializeField] private ScammerDetectionSystem scammerDetectionSystem;
        [SerializeField] private RedAlertRedirectionSystem redAlertRedirectionSystem;
        [SerializeField] private CharityDonationSystem charityDonationSystem;
        [SerializeField] private CreditApplicationSystem creditApplicationSystem;
        [SerializeField] private ManagerSatisfactionSystem managerSatisfactionSystem;
        [SerializeField] private BankCatChaosSystem bankCatChaosSystem;
        [SerializeField] private StationeryDeliverySystem stationeryDeliverySystem;

        [Header("Boost Targets")]
        [SerializeField] private MobilePlayerController mobilePlayerController;
        [SerializeField] private ChubbyTopDownInputController topDownController;
        [SerializeField] private BankingActionSystem bankingActionSystem;
        [SerializeField] private FastTrackActionSystem fastTrackActionSystem;
        [SerializeField] private UtilityBillSystem utilityBillSystem;
        [SerializeField] private MobileActivationMiniGame mobileActivationTarget;
        [SerializeField] private WireTransferMiniGame wireTransferTarget;
        [SerializeField] private CardBlockMiniGame cardBlockMiniGame;
        [SerializeField] private DocumentProcessWorkflow documentProcessWorkflow;
        [SerializeField] private GoldExchangeWorkflow goldExchangeWorkflow;
        [SerializeField] private AccountOpeningSystem accountOpeningSystem;
        [SerializeField] private InsuranceReferralSystem insuranceReferralSystem;
        [SerializeField] private CreditApplicationSystem creditApplicationTarget;
        [SerializeField] private QuestPoolDirector questPoolDirector;

        [Header("Notification UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private RectTransform notificationRoot;
        [SerializeField] private Text notificationText;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip rewardChime;
        [SerializeField, Min(0.1f)] private float notificationSeconds = 2.6f;

        public UnityEvent<Quest> OnQuestProgressChanged = new UnityEvent<Quest>();
        public UnityEvent<Quest> OnQuestCompleted = new UnityEvent<Quest>();
        public UnityEvent<Achievement> OnAchievementProgressChanged = new UnityEvent<Achievement>();
        public UnityEvent<Achievement> OnAchievementCompleted = new UnityEvent<Achievement>();
        public UnityEvent<int> OnGoldRewarded = new UnityEvent<int>();

        private readonly Dictionary<string, Quest> questsById = new Dictionary<string, Quest>();
        private readonly Dictionary<string, Achievement> achievementsById = new Dictionary<string, Achievement>();
        private Coroutine notificationRoutine;
        private int previousTeaPortions = -1;

        public IReadOnlyList<Quest> DailyQuests => dailyQuests;
        public IReadOnlyList<Achievement> Achievements => achievements;
        public float PermanentWalkSpeedMultiplier => 1f + PlayerPrefs.GetFloat(PermanentWalkSpeedBonusKey, 0f);
        public float PermanentVipServiceSpeedMultiplier => 1f + PlayerPrefs.GetFloat(PermanentVipServiceSpeedBonusKey, 0f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureDefaultContent();
            BuildLookups();
            ResetDailyQuestsIfNeeded();
            LoadProgress();
            ResolveMissingReferences();
            EnsureNotificationUi();
            ApplyPersistedPermanentBoosts();
        }

        private void OnEnable()
        {
            ResolveMissingReferences();
            RegisterEventHooks();
        }

        private void OnDisable()
        {
            UnregisterEventHooks();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void OnCustomerServed(CustomerType type)
        {
            IncrementQuest("daily_serve_3_customers", 1);
            IncrementAchievement("ach_serve_100_customers", 1);
        }

        public void OnCustomerServed(CustomerRequestKind requestKind)
        {
            OnCustomerServed(ToCustomerType(requestKind));
        }

        public void OnTeaServed()
        {
            IncrementQuest("daily_serve_tea_once", 1);
        }

        public void OnPerfectTransfer()
        {
            IncrementQuest("daily_solve_password_2", 1);
            IncrementAchievement("ach_perfect_typing_15", 1);
        }

        public void OnManagerRepaired()
        {
            IncrementQuest("daily_manager_praise_once", 1);
        }

        public void OnScammerCaught()
        {
            IncrementAchievement("ach_detect_10_scammers", 1);
        }

        public void OnMobileActivationCompleted()
        {
            OnCustomerServed(CustomerType.Standard);
            IncrementQuest("daily_solve_password_2", 1);
        }

        public void OnCreditApplicationReferred()
        {
            IncrementQuest("daily_refer_credit_2", 1);
        }

        public void OnCatSecurityCalled()
        {
            IncrementQuest("daily_cat_love_once", 1);
        }

        public void OnBarutCustomerRedirected()
        {
            IncrementAchievement("ach_redirect_10_barut", 1);
        }

        public void OnDonationCompleted()
        {
            IncrementAchievement("ach_charity_20", 1);
        }

        public void OnStationeryDelivered()
        {
            IncrementAchievement("ach_stationery_15", 1);
        }

        public void OnStaffFeastTriggered()
        {
            IncrementAchievement("ach_feast_5", 1);
        }

        public void IncrementQuest(string questId, int amount = 1)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(questId) || !questsById.TryGetValue(questId, out var quest))
            {
                return;
            }

            if (quest.isCompleted)
            {
                return;
            }

            quest.currentValue = Mathf.Min(quest.targetValue, quest.currentValue + amount);
            if (quest.currentValue >= quest.targetValue)
            {
                CompleteQuest(quest);
            }
            else
            {
                SaveQuest(quest);
                OnQuestProgressChanged.Invoke(quest);
            }
        }

        public void IncrementAchievement(string achievementId, int amount = 1)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(achievementId) || !achievementsById.TryGetValue(achievementId, out var achievement))
            {
                return;
            }

            if (achievement.isCompleted)
            {
                return;
            }

            achievement.currentValue = Mathf.Min(achievement.targetValue, achievement.currentValue + amount);
            if (achievement.currentValue >= achievement.targetValue)
            {
                CompleteAchievement(achievement);
            }
            else
            {
                SaveAchievement(achievement);
                OnAchievementProgressChanged.Invoke(achievement);
            }
        }

        public void ResetDailyQuests()
        {
            for (var i = 0; i < dailyQuests.Count; i++)
            {
                dailyQuests[i].currentValue = 0;
                dailyQuests[i].isCompleted = false;
                SaveQuest(dailyQuests[i]);
            }

            PlayerPrefs.SetString(DailyDateKey, GetTodayKey());
            PlayerPrefs.Save();
        }

        private void CompleteQuest(Quest quest)
        {
            quest.currentValue = quest.targetValue;
            quest.isCompleted = true;
            SaveQuest(quest);
            RewardGold(quest.goldReward);
            ApplyPassiveReward(quest.rewardBoostType, quest.rewardBoostValue, quest.rewardBoostSeconds, false);
            PlayRewardFeedback($"GOAL COMPLETED: {quest.title}! +{quest.goldReward} Gold!");
            OnQuestProgressChanged.Invoke(quest);
            OnQuestCompleted.Invoke(quest);
        }

        private void CompleteAchievement(Achievement achievement)
        {
            achievement.currentValue = achievement.targetValue;
            achievement.isCompleted = true;
            SaveAchievement(achievement);
            RewardGold(achievement.goldReward);
            ApplyPassiveReward(achievement.permanentRewardType, achievement.permanentRewardValue, 0f, true);
            PlayRewardFeedback($"ACHIEVEMENT: {achievement.title}! +{achievement.goldReward} Gold!");
            OnAchievementProgressChanged.Invoke(achievement);
            OnAchievementCompleted.Invoke(achievement);
        }

        private void RewardGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var currentGold = PlayerPrefs.GetInt(PreGameShopManager.PlayerGoldKey, 0);
            PlayerPrefs.SetInt(PreGameShopManager.PlayerGoldKey, currentGold + amount);
            PlayerPrefs.Save();
            OnGoldRewarded.Invoke(amount);
        }

        private void ApplyPassiveReward(PassiveRewardType rewardType, float value, float seconds, bool permanent)
        {
            if (rewardType == PassiveRewardType.None)
            {
                return;
            }

            if (permanent)
            {
                ApplyPermanentReward(rewardType, value);
                return;
            }

            StartCoroutine(TemporaryRewardRoutine(rewardType, value, seconds));
        }

        private IEnumerator TemporaryRewardRoutine(PassiveRewardType rewardType, float value, float seconds)
        {
            var duration = Mathf.Max(0.1f, seconds);
            switch (rewardType)
            {
                case PassiveRewardType.TemporaryPatience:
                    QueueCustomer.GlobalPatienceDrainMultiplier *= Mathf.Clamp(value, 0.05f, 1f);
                    CustomerPatience.GlobalPatienceDrainMultiplier *= Mathf.Clamp(value, 0.05f, 1f);
                    yield return new WaitForSeconds(duration);
                    QueueCustomer.GlobalPatienceDrainMultiplier /= Mathf.Clamp(value, 0.05f, 1f);
                    CustomerPatience.GlobalPatienceDrainMultiplier /= Mathf.Clamp(value, 0.05f, 1f);
                    break;

                case PassiveRewardType.TemporarySpeed:
                    ApplySpeedMultiplier(value);
                    yield return new WaitForSeconds(duration);
                    RestoreSpeedMultiplier(value);
                    break;

                case PassiveRewardType.TemporaryActionSpeed:
                    ApplyActionTimeMultiplier(value);
                    yield return new WaitForSeconds(duration);
                    RestoreActionTimeMultiplier(value);
                    break;

                case PassiveRewardType.TemporaryDigitalCalm:
                    if (questPoolDirector != null)
                    {
                        questPoolDirector.ApplySpawnIntervalMultiplierForSeconds(1f / Mathf.Max(0.1f, value), duration);
                    }

                    yield return new WaitForSeconds(duration);
                    break;
            }
        }

        private void ApplyPermanentReward(PassiveRewardType rewardType, float value)
        {
            switch (rewardType)
            {
                case PassiveRewardType.PermanentWalkSpeed:
                    PlayerPrefs.SetFloat(PermanentWalkSpeedBonusKey, PlayerPrefs.GetFloat(PermanentWalkSpeedBonusKey, 0f) + value);
                    ApplySpeedMultiplier(1f + value);
                    break;

                case PassiveRewardType.PermanentVipServiceSpeed:
                    PlayerPrefs.SetFloat(PermanentVipServiceSpeedBonusKey, PlayerPrefs.GetFloat(PermanentVipServiceSpeedBonusKey, 0f) + value);
                    break;

                case PassiveRewardType.PermanentFeastDuration:
                    PlayerPrefs.SetFloat(PermanentFeastDurationBonusKey, PlayerPrefs.GetFloat(PermanentFeastDurationBonusKey, 0f) + value);
                    managerSatisfactionSystem?.ApplyPermanentFeastDurationBonus();
                    break;

                case PassiveRewardType.PermanentAmbientMusic:
                    PlayerPrefs.SetInt(PermanentAmbientMusicUnlockedKey, 1);
                    break;
            }

            PlayerPrefs.Save();
        }

        private void ApplyPersistedPermanentBoosts()
        {
            var speedBonus = PlayerPrefs.GetFloat(PermanentWalkSpeedBonusKey, 0f);
            if (speedBonus > 0f)
            {
                ApplySpeedMultiplier(1f + speedBonus);
            }
        }

        private void ApplySpeedMultiplier(float multiplier)
        {
            var safeMultiplier = Mathf.Max(0.1f, multiplier);
            if (mobilePlayerController != null)
            {
                mobilePlayerController.MovementSpeedMultiplier *= safeMultiplier;
            }

            if (topDownController != null)
            {
                topDownController.MovementSpeedMultiplier *= safeMultiplier;
            }
        }

        private void RestoreSpeedMultiplier(float multiplier)
        {
            var safeMultiplier = Mathf.Max(0.1f, multiplier);
            if (mobilePlayerController != null)
            {
                mobilePlayerController.MovementSpeedMultiplier /= safeMultiplier;
            }

            if (topDownController != null)
            {
                topDownController.MovementSpeedMultiplier /= safeMultiplier;
            }
        }

        private void ApplyActionTimeMultiplier(float multiplier)
        {
            var safeMultiplier = Mathf.Clamp(multiplier, 0.05f, 5f);
            if (fastTrackActionSystem != null) fastTrackActionSystem.ActionTimeMultiplier *= safeMultiplier;
            if (utilityBillSystem != null) utilityBillSystem.ActionTimeMultiplier *= safeMultiplier;
            if (mobileActivationTarget != null) mobileActivationTarget.ActionTimeMultiplier *= safeMultiplier;
            if (wireTransferTarget != null) wireTransferTarget.ActionTimeMultiplier *= safeMultiplier;
            if (cardBlockMiniGame != null) cardBlockMiniGame.ActionTimeMultiplier *= safeMultiplier;
            if (documentProcessWorkflow != null) documentProcessWorkflow.ActionTimeMultiplier *= safeMultiplier;
            if (goldExchangeWorkflow != null) goldExchangeWorkflow.ActionTimeMultiplier *= safeMultiplier;
            if (accountOpeningSystem != null) accountOpeningSystem.ActionTimeMultiplier *= safeMultiplier;
            if (creditApplicationTarget != null) creditApplicationTarget.ActionTimeMultiplier *= safeMultiplier;
        }

        private void RestoreActionTimeMultiplier(float multiplier)
        {
            var safeMultiplier = Mathf.Clamp(multiplier, 0.05f, 5f);
            if (fastTrackActionSystem != null) fastTrackActionSystem.ActionTimeMultiplier /= safeMultiplier;
            if (utilityBillSystem != null) utilityBillSystem.ActionTimeMultiplier /= safeMultiplier;
            if (mobileActivationTarget != null) mobileActivationTarget.ActionTimeMultiplier /= safeMultiplier;
            if (wireTransferTarget != null) wireTransferTarget.ActionTimeMultiplier /= safeMultiplier;
            if (cardBlockMiniGame != null) cardBlockMiniGame.ActionTimeMultiplier /= safeMultiplier;
            if (documentProcessWorkflow != null) documentProcessWorkflow.ActionTimeMultiplier /= safeMultiplier;
            if (goldExchangeWorkflow != null) goldExchangeWorkflow.ActionTimeMultiplier /= safeMultiplier;
            if (accountOpeningSystem != null) accountOpeningSystem.ActionTimeMultiplier /= safeMultiplier;
            if (creditApplicationTarget != null) creditApplicationTarget.ActionTimeMultiplier /= safeMultiplier;
        }

        private void RegisterEventHooks()
        {
            if (teaLadyBoostSystem != null)
            {
                teaLadyBoostSystem.OnTeaPortionsChanged.RemoveListener(HandleTeaPortionsChanged);
                teaLadyBoostSystem.OnTeaPortionsChanged.AddListener(HandleTeaPortionsChanged);
            }

            if (wireTransferMiniGame != null)
            {
                wireTransferMiniGame.OnPerfectTransferBoostStarted.RemoveListener(OnPerfectTransfer);
                wireTransferMiniGame.OnPerfectTransferBoostStarted.AddListener(OnPerfectTransfer);
                wireTransferMiniGame.OnTransferCompleted.RemoveListener(HandleWireTransferCompleted);
                wireTransferMiniGame.OnTransferCompleted.AddListener(HandleWireTransferCompleted);
            }

            if (mobileActivationMiniGame != null)
            {
                mobileActivationMiniGame.OnActivationCompleted.RemoveListener(OnMobileActivationCompleted);
                mobileActivationMiniGame.OnActivationCompleted.AddListener(OnMobileActivationCompleted);
            }

            if (bankingActionSystem != null)
            {
                bankingActionSystem.OnActionCompleted.RemoveListener(HandleBankingActionCompleted);
                bankingActionSystem.OnActionCompleted.AddListener(HandleBankingActionCompleted);
            }

            if (fastTrackActionSystem != null)
            {
                fastTrackActionSystem.OnFastTrackCompleted.RemoveListener(HandleFastTrackCompleted);
                fastTrackActionSystem.OnFastTrackCompleted.AddListener(HandleFastTrackCompleted);
            }

            if (utilityBillSystem != null)
            {
                utilityBillSystem.OnBillCompleted.RemoveListener(HandleBillCompleted);
                utilityBillSystem.OnBillCompleted.AddListener(HandleBillCompleted);
            }

            if (cardBlockMiniGame != null)
            {
                cardBlockMiniGame.OnMiniGameCompleted.RemoveListener(HandleCardBlockCompleted);
                cardBlockMiniGame.OnMiniGameCompleted.AddListener(HandleCardBlockCompleted);
            }

            if (documentProcessWorkflow != null)
            {
                documentProcessWorkflow.OnWorkflowCompleted.RemoveListener(HandleDocumentWorkflowCompleted);
                documentProcessWorkflow.OnWorkflowCompleted.AddListener(HandleDocumentWorkflowCompleted);
            }

            if (goldExchangeWorkflow != null)
            {
                goldExchangeWorkflow.OnGoldExchangeCompleted.RemoveListener(HandleGoldExchangeCompleted);
                goldExchangeWorkflow.OnGoldExchangeCompleted.AddListener(HandleGoldExchangeCompleted);
            }

            if (accountOpeningSystem != null)
            {
                accountOpeningSystem.OnAccountOpeningRedirected.RemoveListener(HandleAccountOpeningRedirected);
                accountOpeningSystem.OnAccountOpeningRedirected.AddListener(HandleAccountOpeningRedirected);
            }

            if (insuranceReferralSystem != null)
            {
                insuranceReferralSystem.OnInsuranceCustomerRedirected.RemoveListener(HandleInsuranceRedirected);
                insuranceReferralSystem.OnInsuranceCustomerRedirected.AddListener(HandleInsuranceRedirected);
            }

            if (managerITSupportEvent != null)
            {
                managerITSupportEvent.OnSupportEventCompleted.RemoveListener(OnManagerRepaired);
                managerITSupportEvent.OnSupportEventCompleted.AddListener(OnManagerRepaired);
            }

            if (scammerDetectionSystem != null)
            {
                scammerDetectionSystem.OnScammerSecurityCalled.RemoveListener(HandleScammerCaught);
                scammerDetectionSystem.OnScammerSecurityCalled.AddListener(HandleScammerCaught);
            }

            if (redAlertRedirectionSystem != null)
            {
                redAlertRedirectionSystem.OnBarutCustomerRedirected.RemoveListener(HandleBarutCustomerRedirected);
                redAlertRedirectionSystem.OnBarutCustomerRedirected.AddListener(HandleBarutCustomerRedirected);
            }

            if (charityDonationSystem != null)
            {
                charityDonationSystem.OnDonationCompleted.RemoveListener(HandleDonationCompleted);
                charityDonationSystem.OnDonationCompleted.AddListener(HandleDonationCompleted);
            }

            if (creditApplicationSystem != null)
            {
                creditApplicationSystem.OnCreditApplicationReferred.RemoveListener(HandleCreditApplicationReferred);
                creditApplicationSystem.OnCreditApplicationReferred.AddListener(HandleCreditApplicationReferred);
            }

            if (managerSatisfactionSystem != null)
            {
                managerSatisfactionSystem.OnStaffFeastStarted.RemoveListener(OnStaffFeastTriggered);
                managerSatisfactionSystem.OnStaffFeastStarted.AddListener(OnStaffFeastTriggered);
            }

            if (bankCatChaosSystem != null)
            {
                bankCatChaosSystem.OnSecurityCalledForCat.RemoveListener(OnCatSecurityCalled);
                bankCatChaosSystem.OnSecurityCalledForCat.AddListener(OnCatSecurityCalled);
            }

            if (stationeryDeliverySystem != null)
            {
                stationeryDeliverySystem.OnSupplyDelivered.RemoveListener(HandleStationeryDelivered);
                stationeryDeliverySystem.OnSupplyDelivered.AddListener(HandleStationeryDelivered);
            }
        }

        private void UnregisterEventHooks()
        {
            if (teaLadyBoostSystem != null)
            {
                teaLadyBoostSystem.OnTeaPortionsChanged.RemoveListener(HandleTeaPortionsChanged);
            }

            if (wireTransferMiniGame != null)
            {
                wireTransferMiniGame.OnPerfectTransferBoostStarted.RemoveListener(OnPerfectTransfer);
                wireTransferMiniGame.OnTransferCompleted.RemoveListener(HandleWireTransferCompleted);
            }

            if (mobileActivationMiniGame != null)
            {
                mobileActivationMiniGame.OnActivationCompleted.RemoveListener(OnMobileActivationCompleted);
            }

            if (bankingActionSystem != null)
            {
                bankingActionSystem.OnActionCompleted.RemoveListener(HandleBankingActionCompleted);
            }

            if (fastTrackActionSystem != null)
            {
                fastTrackActionSystem.OnFastTrackCompleted.RemoveListener(HandleFastTrackCompleted);
            }

            if (utilityBillSystem != null)
            {
                utilityBillSystem.OnBillCompleted.RemoveListener(HandleBillCompleted);
            }

            if (cardBlockMiniGame != null)
            {
                cardBlockMiniGame.OnMiniGameCompleted.RemoveListener(HandleCardBlockCompleted);
            }

            if (documentProcessWorkflow != null)
            {
                documentProcessWorkflow.OnWorkflowCompleted.RemoveListener(HandleDocumentWorkflowCompleted);
            }

            if (goldExchangeWorkflow != null)
            {
                goldExchangeWorkflow.OnGoldExchangeCompleted.RemoveListener(HandleGoldExchangeCompleted);
            }

            if (accountOpeningSystem != null)
            {
                accountOpeningSystem.OnAccountOpeningRedirected.RemoveListener(HandleAccountOpeningRedirected);
            }

            if (insuranceReferralSystem != null)
            {
                insuranceReferralSystem.OnInsuranceCustomerRedirected.RemoveListener(HandleInsuranceRedirected);
            }

            if (managerITSupportEvent != null)
            {
                managerITSupportEvent.OnSupportEventCompleted.RemoveListener(OnManagerRepaired);
            }

            if (scammerDetectionSystem != null)
            {
                scammerDetectionSystem.OnScammerSecurityCalled.RemoveListener(HandleScammerCaught);
            }

            if (redAlertRedirectionSystem != null)
            {
                redAlertRedirectionSystem.OnBarutCustomerRedirected.RemoveListener(HandleBarutCustomerRedirected);
            }

            if (charityDonationSystem != null)
            {
                charityDonationSystem.OnDonationCompleted.RemoveListener(HandleDonationCompleted);
            }

            if (creditApplicationSystem != null)
            {
                creditApplicationSystem.OnCreditApplicationReferred.RemoveListener(HandleCreditApplicationReferred);
            }

            if (managerSatisfactionSystem != null)
            {
                managerSatisfactionSystem.OnStaffFeastStarted.RemoveListener(OnStaffFeastTriggered);
            }

            if (bankCatChaosSystem != null)
            {
                bankCatChaosSystem.OnSecurityCalledForCat.RemoveListener(OnCatSecurityCalled);
            }

            if (stationeryDeliverySystem != null)
            {
                stationeryDeliverySystem.OnSupplyDelivered.RemoveListener(HandleStationeryDelivered);
            }
        }

        private void HandleTeaPortionsChanged(int portions)
        {
            if (previousTeaPortions >= 0 && portions < previousTeaPortions)
            {
                OnTeaServed();
            }

            previousTeaPortions = portions;
        }

        private void HandleWireTransferCompleted()
        {
            OnCustomerServed(CustomerRequestKind.WireTransfer);
        }

        private void HandleBankingActionCompleted(ActionType actionType)
        {
            OnCustomerServed(CustomerType.Standard);
        }

        private void HandleFastTrackCompleted(FastTrackTaskType taskType)
        {
            OnCustomerServed(CustomerType.Standard);
        }

        private void HandleBillCompleted(BillType billType)
        {
            OnCustomerServed(CustomerRequestKind.BillPayment);
        }

        private void HandleCardBlockCompleted()
        {
            OnCustomerServed(CustomerRequestKind.CardBlockRemoval);
        }

        private void HandleDocumentWorkflowCompleted()
        {
            OnCustomerServed(CustomerRequestKind.CardApplication);
        }

        private void HandleGoldExchangeCompleted()
        {
            OnCustomerServed(CustomerRequestKind.GoldExchange);
        }

        private void HandleAccountOpeningRedirected(QueueCustomer customer)
        {
            OnCustomerServed(CustomerRequestKind.OpenAccount);
        }

        private void HandleInsuranceRedirected(QueueCustomer customer)
        {
            OnCustomerServed(CustomerRequestKind.InsuranceReferral);
        }

        private void HandleScammerCaught(GameObject scammer)
        {
            OnScammerCaught();
        }

        private void HandleBarutCustomerRedirected(QueueCustomer customer)
        {
            OnBarutCustomerRedirected();
        }

        private void HandleDonationCompleted(int amount)
        {
            OnCustomerServed(CustomerRequestKind.PhilanthropistCustomer);
            OnDonationCompleted();
        }

        private void HandleCreditApplicationReferred(CreditType creditType)
        {
            OnCustomerServed(CustomerRequestKind.CreditApproval);
            OnCreditApplicationReferred();
        }

        private void HandleStationeryDelivered(StationeryDeskType deskType)
        {
            OnStationeryDelivered();
        }

        private void BuildLookups()
        {
            questsById.Clear();
            for (var i = 0; i < dailyQuests.Count; i++)
            {
                var quest = dailyQuests[i];
                if (quest != null && !string.IsNullOrWhiteSpace(quest.ID))
                {
                    questsById[quest.ID] = quest;
                }
            }

            achievementsById.Clear();
            for (var i = 0; i < achievements.Count; i++)
            {
                var achievement = achievements[i];
                if (achievement != null && !string.IsNullOrWhiteSpace(achievement.ID))
                {
                    achievementsById[achievement.ID] = achievement;
                }
            }
        }

        private void ResetDailyQuestsIfNeeded()
        {
            if (!resetDailyQuestsByCalendarDay)
            {
                return;
            }

            var today = GetTodayKey();
            if (PlayerPrefs.GetString(DailyDateKey, string.Empty) != today)
            {
                PlayerPrefs.SetString(DailyDateKey, today);
                for (var i = 0; i < dailyQuests.Count; i++)
                {
                    PlayerPrefs.DeleteKey(GetQuestCurrentKey(dailyQuests[i].ID));
                    PlayerPrefs.DeleteKey(GetQuestCompletedKey(dailyQuests[i].ID));
                }

                PlayerPrefs.Save();
            }
        }

        private void LoadProgress()
        {
            for (var i = 0; i < dailyQuests.Count; i++)
            {
                var quest = dailyQuests[i];
                quest.currentValue = PlayerPrefs.GetInt(GetQuestCurrentKey(quest.ID), quest.currentValue);
                quest.isCompleted = PlayerPrefs.GetInt(GetQuestCompletedKey(quest.ID), quest.isCompleted ? 1 : 0) == 1;
            }

            for (var i = 0; i < achievements.Count; i++)
            {
                var achievement = achievements[i];
                achievement.currentValue = PlayerPrefs.GetInt(GetAchievementCurrentKey(achievement.ID), achievement.currentValue);
                achievement.isCompleted = PlayerPrefs.GetInt(GetAchievementCompletedKey(achievement.ID), achievement.isCompleted ? 1 : 0) == 1;
            }
        }

        private void SaveQuest(Quest quest)
        {
            PlayerPrefs.SetInt(GetQuestCurrentKey(quest.ID), quest.currentValue);
            PlayerPrefs.SetInt(GetQuestCompletedKey(quest.ID), quest.isCompleted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void SaveAchievement(Achievement achievement)
        {
            PlayerPrefs.SetInt(GetAchievementCurrentKey(achievement.ID), achievement.currentValue);
            PlayerPrefs.SetInt(GetAchievementCompletedKey(achievement.ID), achievement.isCompleted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void PlayRewardFeedback(string message)
        {
            if (audioSource != null && rewardChime != null)
            {
                audioSource.PlayOneShot(rewardChime);
            }

            EnsureNotificationUi();
            if (notificationText != null)
            {
                notificationText.text = message;
            }

            if (notificationRoutine != null)
            {
                StopCoroutine(notificationRoutine);
            }

            notificationRoutine = StartCoroutine(NotificationRoutine());
        }

        private IEnumerator NotificationRoutine()
        {
            if (notificationRoot == null)
            {
                yield break;
            }

            notificationRoot.gameObject.SetActive(true);
            var shownPosition = new Vector2(-24f, -90f);
            var hiddenPosition = new Vector2(420f, -90f);
            var elapsed = 0f;

            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / 0.25f);
                notificationRoot.anchoredPosition = Vector2.Lerp(hiddenPosition, shownPosition, t);
                yield return null;
            }

            notificationRoot.anchoredPosition = shownPosition;
            yield return new WaitForSeconds(notificationSeconds);

            elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / 0.2f);
                notificationRoot.anchoredPosition = Vector2.Lerp(shownPosition, hiddenPosition, t);
                yield return null;
            }

            notificationRoot.gameObject.SetActive(false);
            notificationRoutine = null;
        }

        private void EnsureNotificationUi()
        {
            if (notificationRoot != null && notificationText != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasObject = new GameObject("Quest Notification Canvas");
                targetCanvas = canvasObject.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            var banner = new GameObject("Quest Achievement Notification");
            banner.transform.SetParent(targetCanvas.transform, false);
            notificationRoot = banner.AddComponent<RectTransform>();
            notificationRoot.anchorMin = new Vector2(1f, 1f);
            notificationRoot.anchorMax = new Vector2(1f, 1f);
            notificationRoot.pivot = new Vector2(1f, 1f);
            notificationRoot.anchoredPosition = new Vector2(420f, -90f);
            notificationRoot.sizeDelta = new Vector2(390f, 74f);

            var image = banner.AddComponent<Image>();
            image.color = new Color(0.1f, 0.14f, 0.18f, 0.96f);

            var textObject = new GameObject("Notification Text");
            textObject.transform.SetParent(banner.transform, false);
            notificationText = textObject.AddComponent<Text>();
            notificationText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            notificationText.alignment = TextAnchor.MiddleCenter;
            notificationText.fontSize = 18;
            notificationText.fontStyle = FontStyle.Bold;
            notificationText.color = new Color(1f, 0.88f, 0.32f);

            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);

            notificationRoot.gameObject.SetActive(false);
        }

        private void ResolveMissingReferences()
        {
            if (teaLadyBoostSystem == null) teaLadyBoostSystem = FindFirstObjectByType<TeaLadyBoostSystem>();
            if (wireTransferMiniGame == null) wireTransferMiniGame = FindFirstObjectByType<WireTransferMiniGame>();
            if (mobileActivationMiniGame == null) mobileActivationMiniGame = FindFirstObjectByType<MobileActivationMiniGame>();
            if (managerITSupportEvent == null) managerITSupportEvent = FindFirstObjectByType<ManagerITSupportEvent>();
            if (scammerDetectionSystem == null) scammerDetectionSystem = FindFirstObjectByType<ScammerDetectionSystem>();
            if (redAlertRedirectionSystem == null) redAlertRedirectionSystem = FindFirstObjectByType<RedAlertRedirectionSystem>();
            if (charityDonationSystem == null) charityDonationSystem = FindFirstObjectByType<CharityDonationSystem>();
            if (creditApplicationSystem == null) creditApplicationSystem = FindFirstObjectByType<CreditApplicationSystem>();
            if (managerSatisfactionSystem == null) managerSatisfactionSystem = FindFirstObjectByType<ManagerSatisfactionSystem>();
            if (bankCatChaosSystem == null) bankCatChaosSystem = FindFirstObjectByType<BankCatChaosSystem>();
            if (stationeryDeliverySystem == null) stationeryDeliverySystem = StationeryDeliverySystem.Instance != null ? StationeryDeliverySystem.Instance : FindFirstObjectByType<StationeryDeliverySystem>();
            if (mobilePlayerController == null) mobilePlayerController = FindFirstObjectByType<MobilePlayerController>();
            if (topDownController == null) topDownController = FindFirstObjectByType<ChubbyTopDownInputController>();
            if (bankingActionSystem == null) bankingActionSystem = FindFirstObjectByType<BankingActionSystem>();
            if (fastTrackActionSystem == null) fastTrackActionSystem = FindFirstObjectByType<FastTrackActionSystem>();
            if (utilityBillSystem == null) utilityBillSystem = FindFirstObjectByType<UtilityBillSystem>();
            if (mobileActivationTarget == null) mobileActivationTarget = mobileActivationMiniGame != null ? mobileActivationMiniGame : FindFirstObjectByType<MobileActivationMiniGame>();
            if (wireTransferTarget == null) wireTransferTarget = wireTransferMiniGame != null ? wireTransferMiniGame : FindFirstObjectByType<WireTransferMiniGame>();
            if (cardBlockMiniGame == null) cardBlockMiniGame = FindFirstObjectByType<CardBlockMiniGame>();
            if (documentProcessWorkflow == null) documentProcessWorkflow = FindFirstObjectByType<DocumentProcessWorkflow>();
            if (goldExchangeWorkflow == null) goldExchangeWorkflow = FindFirstObjectByType<GoldExchangeWorkflow>();
            if (accountOpeningSystem == null) accountOpeningSystem = AccountOpeningSystem.Instance != null ? AccountOpeningSystem.Instance : FindFirstObjectByType<AccountOpeningSystem>();
            if (insuranceReferralSystem == null) insuranceReferralSystem = FindFirstObjectByType<InsuranceReferralSystem>();
            if (creditApplicationTarget == null) creditApplicationTarget = creditApplicationSystem != null ? creditApplicationSystem : FindFirstObjectByType<CreditApplicationSystem>();
            if (questPoolDirector == null) questPoolDirector = FindFirstObjectByType<QuestPoolDirector>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        private void EnsureDefaultContent()
        {
            if (dailyQuests.Count == 0)
            {
                dailyQuests.Add(new Quest { ID = "daily_serve_3_customers", title = "Gune Tontis Basla", targetValue = 3, goldReward = 50, rewardBoostType = PassiveRewardType.TemporaryPatience, rewardBoostValue = 0.9f, rewardBoostSeconds = 45f });
                dailyQuests.Add(new Quest { ID = "daily_serve_tea_once", title = "Sicak Ikram", targetValue = 1, goldReward = 75, rewardBoostType = PassiveRewardType.TemporaryPatience, rewardBoostValue = 0.85f, rewardBoostSeconds = 30f });
                dailyQuests.Add(new Quest { ID = "daily_manager_praise_once", title = "Mudurun Gozdesi", targetValue = 1, goldReward = 100, rewardBoostType = PassiveRewardType.TemporarySpeed, rewardBoostValue = 1.1f, rewardBoostSeconds = 60f });
                dailyQuests.Add(new Quest { ID = "daily_solve_password_2", title = "Sifreyi Coz", targetValue = 2, goldReward = 80, rewardBoostType = PassiveRewardType.TemporaryDigitalCalm, rewardBoostValue = 0.8f, rewardBoostSeconds = 25f });
                dailyQuests.Add(new Quest { ID = "daily_refer_credit_2", title = "Dogru Adres", targetValue = 2, goldReward = 120, rewardBoostType = PassiveRewardType.TemporaryActionSpeed, rewardBoostValue = 0.75f, rewardBoostSeconds = 35f });
                dailyQuests.Add(new Quest { ID = "daily_cat_love_once", title = "Pati Sevgisi", targetValue = 1, goldReward = 60, rewardBoostType = PassiveRewardType.TemporaryPatience, rewardBoostValue = 0.8f, rewardBoostSeconds = 30f });
            }

            if (achievements.Count == 0)
            {
                achievements.Add(new Achievement { ID = "ach_detect_10_scammers", title = "Dedektif Biyigi", targetValue = 10, goldReward = 500, permanentRewardType = PassiveRewardType.None });
                achievements.Add(new Achievement { ID = "ach_perfect_typing_15", title = "Yildirim Parmaklar", targetValue = 15, goldReward = 400, permanentRewardType = PassiveRewardType.None });
                achievements.Add(new Achievement { ID = "ach_redirect_10_barut", title = "Kriz Savar", targetValue = 10, goldReward = 600, permanentRewardType = PassiveRewardType.PermanentVipServiceSpeed, permanentRewardValue = 0.05f });
                achievements.Add(new Achievement { ID = "ach_feast_5", title = "Lahmacun Selalesi", targetValue = 5, goldReward = 1000, permanentRewardType = PassiveRewardType.PermanentFeastDuration, permanentRewardValue = 5f });
                achievements.Add(new Achievement { ID = "ach_stationery_15", title = "Kirtasiye Kuryesi", targetValue = 15, goldReward = 300, permanentRewardType = PassiveRewardType.None });
                achievements.Add(new Achievement { ID = "ach_charity_20", title = "Hayirsever Sube", targetValue = 20, goldReward = 500, permanentRewardType = PassiveRewardType.PermanentAmbientMusic, permanentRewardValue = 1f });
                achievements.Add(new Achievement { ID = "ach_serve_100_customers", title = "Yilin Memuru", targetValue = 100, goldReward = 750, permanentRewardType = PassiveRewardType.PermanentWalkSpeed, permanentRewardValue = 0.05f });
            }
        }

        private static CustomerType ToCustomerType(CustomerRequestKind requestKind)
        {
            return requestKind switch
            {
                CustomerRequestKind.VipSafeRental => CustomerType.Vip,
                CustomerRequestKind.PhilanthropistCustomer => CustomerType.Philanthropist,
                CustomerRequestKind.BarutCustomer => CustomerType.Barut,
                CustomerRequestKind.ScammerCustomer => CustomerType.Scammer,
                _ => CustomerType.Standard
            };
        }

        private static string GetTodayKey()
        {
            return DateTime.Now.ToString("yyyyMMdd");
        }

        private static string GetQuestCurrentKey(string id)
        {
            return $"{QuestPrefix}{id}_Current";
        }

        private static string GetQuestCompletedKey(string id)
        {
            return $"{QuestPrefix}{id}_Completed";
        }

        private static string GetAchievementCurrentKey(string id)
        {
            return $"{AchievementPrefix}{id}_Current";
        }

        private static string GetAchievementCompletedKey(string id)
        {
            return $"{AchievementPrefix}{id}_Completed";
        }
    }
}
