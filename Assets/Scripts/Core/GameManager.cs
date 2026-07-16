using UnityEngine;

namespace RushBank.Core
{
    /// <summary>
    /// Oyunun genel durumunu tutan, sahneler arası yaşayan singleton.
    /// Boot sahnesinde bir kez oluşturulur, bir daha yok olmaz.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Boot;
        public int SelectedLevelBuildIndex { get; private set; } = -1;
        public bool PendingTimeSlowBooster { get; private set; }
        public bool PendingSpeedBooster { get; private set; }
        public bool PendingPatienceBooster { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetState(GameState newState)
        {
            State = newState;
        }

        public void SetSelectedLevel(int buildIndex)
        {
            SelectedLevelBuildIndex = buildIndex;
        }

        public void SetPendingPreRunBoosters(bool timeSlow, bool speed, bool patience)
        {
            PendingTimeSlowBooster = timeSlow;
            PendingSpeedBooster = speed;
            PendingPatienceBooster = patience;
        }

        public void ClearPendingPreRunBoosters()
        {
            PendingTimeSlowBooster = false;
            PendingSpeedBooster = false;
            PendingPatienceBooster = false;
        }
    }

    public enum GameState
    {
        Boot,
        Login,
        MainMenu,
        InGame,
        Paused
    }
}
