using RushBank.Gameplay;
using UnityEngine;

namespace RushBank.Core
{
    public class GameSettingsManager : MonoBehaviour
    {
        public static GameSettingsManager Instance { get; private set; }

        private const string SelectedBranchKey = "SelectedBranchType";
        public const string TutorialCompletedKey = "TutorialCompleted";
        public const string TasraUnlockedKey = "Branch_Tasra_Unlocked";

        [SerializeField] private BranchSettings currentBranchSettings = BranchSettings.CreateDefault(BranchType.Tasra);

        public BranchSettings CurrentBranchSettings => currentBranchSettings;
        public BranchType CurrentBranchType => currentBranchSettings.branchType;
        public float GlobalPatienceMultiplier => currentBranchSettings.globalPatienceMultiplier;
        public float CustomerSpawnInterval => currentBranchSettings.customerSpawnInterval;
        public float ThiefAttackChance => currentBranchSettings.thiefAttackChance;
        public int TargetGoldToWin => currentBranchSettings.targetGoldToWin;
        public string SceneToLoad => currentBranchSettings.sceneToLoad;

        public static GameSettingsManager EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var settingsObject = new GameObject("GameSettingsManager");
            return settingsObject.AddComponent<GameSettingsManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPersistedBranch();
        }

        public void SetBranchSettings(BranchSettings settings)
        {
            currentBranchSettings = settings.WithClampedValues();
            PlayerPrefs.SetInt(SelectedBranchKey, (int)currentBranchSettings.branchType);
            PlayerPrefs.Save();
        }

        public void UnlockTasraBranch()
        {
            PlayerPrefs.SetInt(TutorialCompletedKey, 1);
            PlayerPrefs.SetInt(TasraUnlockedKey, 1);
            PlayerPrefs.Save();
        }

        public bool IsBranchUnlocked(BranchType branchType)
        {
            return branchType switch
            {
                BranchType.Tasra => PlayerPrefs.GetInt(TasraUnlockedKey, 0) == 1 || PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1,
                _ => true
            };
        }

        public float GetTimerPressureMultiplier()
        {
            return currentBranchSettings.branchType switch
            {
                BranchType.Tasra => 0.95f,
                BranchType.Metropol => 1.05f,
                _ => 1f
            };
        }

        private void LoadPersistedBranch()
        {
            if (!PlayerPrefs.HasKey(SelectedBranchKey))
            {
                currentBranchSettings = currentBranchSettings.WithClampedValues();
                return;
            }

            var branchType = (BranchType)PlayerPrefs.GetInt(SelectedBranchKey, (int)BranchType.Tasra);
            currentBranchSettings = BranchSettings.CreateDefault(branchType);
        }
    }
}
