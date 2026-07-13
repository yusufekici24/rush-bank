using UnityEngine;

namespace RushBank.Gameplay
{
    public class ScoreManager : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxComboSteps = 10;
        [SerializeField, Min(0f)] private float multiplierStep = 0.25f;

        public int TotalScore { get; private set; }
        public int ComboCount { get; private set; }
        public float CurrentMultiplier { get; private set; } = 1f;

        public IntEvent OnScoreChanged = new IntEvent();
        public IntEvent OnComboChanged = new IntEvent();
        public FloatEvent OnMultiplierChanged = new FloatEvent();
        public IntEvent OnPointsAwarded = new IntEvent();

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

            OnPointsAwarded?.Invoke(awardedScore);
            OnScoreChanged?.Invoke(TotalScore);
            return awardedScore;
        }

        public void ResetScore()
        {
            TotalScore = 0;
            ComboCount = 0;
            CurrentMultiplier = 1f;

            OnScoreChanged?.Invoke(TotalScore);
            OnComboChanged?.Invoke(ComboCount);
            OnMultiplierChanged?.Invoke(CurrentMultiplier);
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
    }
}
