using RushBank.Core;
using UnityEngine;

namespace RushBank.Gameplay
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [SerializeField, Min(1)] private int maxComboSteps = 10;
        [SerializeField, Min(0f)] private float multiplierStep = 0.25f;
        [SerializeField, Min(0)] private int goldPerCompletedTransaction = 1;
        [SerializeField, Min(1)] private int scorePerExtraGold = 50;
        [SerializeField, Min(1)] private int targetGoldToWin = 120;

        public int TotalScore { get; private set; }
        public int ComboCount { get; private set; }
        public float CurrentMultiplier { get; private set; } = 1f;
        public int RunGoldEarned { get; private set; }
        public int TargetGoldToWin => targetGoldToWin;

        public IntEvent OnScoreChanged = new IntEvent();
        public IntEvent OnComboChanged = new IntEvent();
        public FloatEvent OnMultiplierChanged = new FloatEvent();
        public IntEvent OnPointsAwarded = new IntEvent();
        public IntEvent OnRunGoldChanged = new IntEvent();
        public UnityEngine.Events.UnityEvent OnTargetGoldReached = new UnityEngine.Events.UnityEvent();

        private bool targetGoldReached;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ApplySelectedBranchSettings();
        }

        public void AddScore(int points)
        {
            if (points <= 0)
            {
                return;
            }

            TotalScore += points;
            AwardGoldFromScore(points);
            OnPointsAwarded?.Invoke(points);
            OnScoreChanged?.Invoke(TotalScore);
        }

        public int AwardRequestCompletion(CustomerRequestDefinition request, float elapsedSeconds)
        {
            if (request == null)
            {
                return 0;
            }

            var remainingSeconds = Mathf.Max(0f, request.TargetProcessingSeconds - elapsedSeconds);
            var timeBonus = Mathf.RoundToInt(remainingSeconds * request.ScorePerRemainingSecond);

            UpdateCombo(remainingSeconds);

            var rawScore = request.BaseScore + timeBonus;
            var awardedScore = Mathf.RoundToInt(rawScore * CurrentMultiplier);
            TotalScore += awardedScore;
            AwardGoldFromScore(awardedScore);

            OnPointsAwarded?.Invoke(awardedScore);
            OnScoreChanged?.Invoke(TotalScore);
            return awardedScore;
        }

        public void ResetScore()
        {
            TotalScore = 0;
            ComboCount = 0;
            CurrentMultiplier = 1f;
            RunGoldEarned = 0;
            targetGoldReached = false;

            OnScoreChanged?.Invoke(TotalScore);
            OnComboChanged?.Invoke(ComboCount);
            OnMultiplierChanged?.Invoke(CurrentMultiplier);
            OnRunGoldChanged?.Invoke(RunGoldEarned);
        }

        public void ApplyBranchSettings(BranchSettings settings)
        {
            targetGoldToWin = settings.WithClampedValues().targetGoldToWin;
        }

        public void ApplySelectedBranchSettings()
        {
            if (GameSettingsManager.Instance == null)
            {
                return;
            }

            ApplyBranchSettings(GameSettingsManager.Instance.CurrentBranchSettings);
        }

        private void UpdateCombo(float remainingSeconds)
        {
            if (remainingSeconds <= 0f)
            {
                ComboCount = 0;
                CurrentMultiplier = 1f;
            }
            else
            {
                ComboCount++;
                var multiplierSteps = Mathf.Min(ComboCount, maxComboSteps);
                CurrentMultiplier = 1f + (multiplierSteps * multiplierStep);
            }

            OnComboChanged?.Invoke(ComboCount);
            OnMultiplierChanged?.Invoke(CurrentMultiplier);
        }

        private void AwardGoldFromScore(int awardedScore)
        {
            if (goldPerCompletedTransaction <= 0 || awardedScore <= 0)
            {
                return;
            }

            var bonusGold = Mathf.FloorToInt(awardedScore / (float)scorePerExtraGold);
            var earnedGold = goldPerCompletedTransaction + bonusGold;
            RunGoldEarned += earnedGold;
            var currentGold = PlayerPrefs.GetInt(PreGameShopManager.PlayerGoldKey, 0);
            PlayerPrefs.SetInt(PreGameShopManager.PlayerGoldKey, currentGold + earnedGold);
            PlayerPrefs.Save();
            OnRunGoldChanged?.Invoke(RunGoldEarned);

            if (!targetGoldReached && RunGoldEarned >= targetGoldToWin)
            {
                targetGoldReached = true;
                OnTargetGoldReached.Invoke();
            }
        }
    }
}
