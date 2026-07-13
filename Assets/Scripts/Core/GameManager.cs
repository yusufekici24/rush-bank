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
