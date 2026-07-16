using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using RushBank.Core;
using RushBank.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace RushBank.Gameplay
{
    public enum BranchType
    {
        Tasra,
        Sehir,
        Metropol
    }

    [Serializable]
    public struct BranchSettings
    {
        public BranchType branchType;
        [Min(0.1f)] public float globalPatienceMultiplier;
        [Min(0.5f)] public float customerSpawnInterval;
        [Range(0f, 1f)] public float thiefAttackChance;
        [Min(1)] public int targetGoldToWin;
        public string sceneToLoad;

        public static BranchSettings CreateDefault(BranchType type)
        {
            return type switch
            {
                BranchType.Sehir => new BranchSettings
                {
                    branchType = BranchType.Sehir,
                    globalPatienceMultiplier = 1.5f,
                    customerSpawnInterval = 8f,
                    thiefAttackChance = 0.1f,
                    targetGoldToWin = 240,
                    sceneToLoad = "SehirSubesi"
                },
                BranchType.Metropol => new BranchSettings
                {
                    branchType = BranchType.Metropol,
                    globalPatienceMultiplier = 2.2f,
                    customerSpawnInterval = 4f,
                    thiefAttackChance = 0.25f,
                    targetGoldToWin = 420,
                    sceneToLoad = "MetropolSubesi"
                },
                _ => new BranchSettings
                {
                    branchType = BranchType.Tasra,
                    globalPatienceMultiplier = 1f,
                    customerSpawnInterval = 15f,
                    thiefAttackChance = 0.02f,
                    targetGoldToWin = 120,
                    sceneToLoad = "TasraSubesi"
                }
            };
        }

        public BranchSettings WithClampedValues()
        {
            globalPatienceMultiplier = Mathf.Max(0.1f, globalPatienceMultiplier);
            customerSpawnInterval = Mathf.Max(0.5f, customerSpawnInterval);
            thiefAttackChance = Mathf.Clamp01(thiefAttackChance);
            targetGoldToWin = Mathf.Max(1, targetGoldToWin);
            return this;
        }
    }

    public class LevelDifficultyManager : MonoBehaviour
    {
        [Header("Branch Settings")]
        [SerializeField] private List<BranchSettings> branchSettings = new List<BranchSettings>
        {
            BranchSettings.CreateDefault(BranchType.Tasra),
            BranchSettings.CreateDefault(BranchType.Sehir),
            BranchSettings.CreateDefault(BranchType.Metropol)
        };
        [SerializeField] private BranchType selectedBranch = BranchType.Tasra;

        [Header("Optional UI")]
        [SerializeField] private PreLevelPopupController preLevelPopupController;
        [SerializeField] private TMP_Text selectedBranchText;

        public UnityEvent<BranchSettings> OnBranchSelected = new UnityEvent<BranchSettings>();

        private Coroutine temporarySpawnIntervalRoutine;

        public BranchType SelectedBranch => selectedBranch;
        public BranchSettings SelectedSettings => GetSettings(selectedBranch);

        private void Awake()
        {
            if (preLevelPopupController == null)
            {
                preLevelPopupController = FindFirstObjectByType<PreLevelPopupController>();
            }

            var settingsManager = GameSettingsManager.EnsureInstance();
            selectedBranch = settingsManager.CurrentBranchType;
            UpdateSelectedBranchText(settingsManager.CurrentBranchSettings);
            OnBranchSelected.Invoke(settingsManager.CurrentBranchSettings);
        }

        public void SelectTasraBranch()
        {
            SelectBranch(BranchType.Tasra);
        }

        public void SelectSehirBranch()
        {
            SelectBranch(BranchType.Sehir);
        }

        public void SelectMetropolBranch()
        {
            SelectBranch(BranchType.Metropol);
        }

        public void SelectBranch(BranchType branchType)
        {
            selectedBranch = branchType;
            var settings = GetSettings(branchType).WithClampedValues();
            GameSettingsManager.EnsureInstance().SetBranchSettings(settings);
            UpdateSelectedBranchText(settings);
            OnBranchSelected.Invoke(settings);
        }

        public void PlaySelectedBranch()
        {
            var settings = SelectedSettings.WithClampedValues();
            GameSettingsManager.EnsureInstance().SetBranchSettings(settings);

            var buildIndex = GetBuildIndex(settings.sceneToLoad);
            if (preLevelPopupController != null)
            {
                preLevelPopupController.OpenForLevel(buildIndex);
                return;
            }

            SceneManager.LoadScene(buildIndex);
        }

        public void SelectAndPlayTasra()
        {
            SelectBranch(BranchType.Tasra);
            PlaySelectedBranch();
        }

        public void SelectAndPlaySehir()
        {
            SelectBranch(BranchType.Sehir);
            PlaySelectedBranch();
        }

        public void SelectAndPlayMetropol()
        {
            SelectBranch(BranchType.Metropol);
            PlaySelectedBranch();
        }

        public void ApplyTemporarySpawnIntervalMultiplier(float multiplier, float seconds)
        {
            if (temporarySpawnIntervalRoutine != null)
            {
                StopCoroutine(temporarySpawnIntervalRoutine);
                temporarySpawnIntervalRoutine = null;
                OnBranchSelected.Invoke(SelectedSettings.WithClampedValues());
            }

            temporarySpawnIntervalRoutine = StartCoroutine(TemporarySpawnIntervalRoutine(multiplier, seconds));
        }

        private BranchSettings GetSettings(BranchType branchType)
        {
            for (var i = 0; i < branchSettings.Count; i++)
            {
                if (branchSettings[i].branchType == branchType)
                {
                    return branchSettings[i];
                }
            }

            return BranchSettings.CreateDefault(branchType);
        }

        private IEnumerator TemporarySpawnIntervalRoutine(float multiplier, float seconds)
        {
            var settings = SelectedSettings.WithClampedValues();
            settings.customerSpawnInterval *= Mathf.Max(0.1f, multiplier);
            OnBranchSelected.Invoke(settings);

            yield return new WaitForSeconds(Mathf.Max(0f, seconds));

            OnBranchSelected.Invoke(SelectedSettings.WithClampedValues());
            temporarySpawnIntervalRoutine = null;
        }

        private static int GetBuildIndex(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    var buildSceneName = Path.GetFileNameWithoutExtension(scenePath);
                    if (string.Equals(buildSceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }

            return (int)SceneId.Game;
        }

        private void UpdateSelectedBranchText(BranchSettings settings)
        {
            if (selectedBranchText == null)
            {
                return;
            }

            selectedBranchText.text = settings.branchType switch
            {
                BranchType.Tasra => "Tasra Subesi - Easy",
                BranchType.Sehir => "Sehir Subesi - Medium",
                BranchType.Metropol => "Metropol Subesi - Hard",
                _ => settings.branchType.ToString()
            };
        }
    }
}
