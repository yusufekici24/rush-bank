using UnityEngine;
using UnityEngine.SceneManagement;

namespace RushBank.Core
{
    /// <summary>
    /// Sahne geçişlerini tek noktadan yönetir.
    /// </summary>
    public static class SceneLoader
    {
        public static void Load(SceneId scene)
        {
            SceneManager.LoadScene((int)scene);
        }

        public static AsyncOperation LoadAsync(SceneId scene)
        {
            return SceneManager.LoadSceneAsync((int)scene);
        }
    }

    /// <summary>
    /// Build Settings'teki sahne sırasıyla birebir aynı tutulmalı.
    /// </summary>
    public enum SceneId
    {
        Boot = 0,
        Login = 1,
        MainMenu = 2,
        Game = 3
    }
}
