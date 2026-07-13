using UnityEngine;

namespace RushBank.Core
{
    /// <summary>
    /// Boot sahnesindeki tek script: temel sistemleri kurar ve giriş ekranına geçer.
    /// Boot sahnesine boş bir GameObject koyup bunu ekleyin.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        private void Start()
        {
            Application.targetFrameRate = 60;

            if (GameManager.Instance == null)
            {
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
            }

            GameManager.Instance.SetState(GameState.Login);
            SceneLoader.Load(SceneId.Login);
        }
    }
}
