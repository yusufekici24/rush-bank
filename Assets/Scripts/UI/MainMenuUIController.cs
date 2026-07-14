using RushBank.Core;
using RushBank.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace RushBank.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private StyleSheet themeStyleSheet;
        [SerializeField] private PreLevelPopupController preLevelPopupController;
        [SerializeField] private LevelDifficultyManager levelDifficultyManager;

        private Button startButton;
        private Button scenarioButton;
        private Button settingsButton;
        private Button closeSettingsButton;
        private Button quitButton;
        private Toggle soundToggle;
        private Toggle vibrationToggle;
        private VisualElement settingsPanel;

        private void Awake()
        {
            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }

            if (preLevelPopupController == null)
            {
                preLevelPopupController = FindFirstObjectByType<PreLevelPopupController>();
            }

            if (levelDifficultyManager == null)
            {
                levelDifficultyManager = FindFirstObjectByType<LevelDifficultyManager>();
            }
        }

        private void OnEnable()
        {
            AppSettings.Apply();

            var root = document.rootVisualElement;
            ApplyTheme(root);
            startButton = root.Q<Button>("start-button");
            scenarioButton = root.Q<Button>("scenario-button");
            settingsButton = root.Q<Button>("settings-button");
            closeSettingsButton = root.Q<Button>("close-settings-button");
            quitButton = root.Q<Button>("quit-button");
            soundToggle = root.Q<Toggle>("sound-toggle");
            vibrationToggle = root.Q<Toggle>("vibration-toggle");
            settingsPanel = root.Q<VisualElement>("settings-panel");

            BindEvents();
            SyncSettings();
            HideSettings();
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void BindEvents()
        {
            if (startButton != null)
            {
                startButton.clicked += StartGame;
            }

            if (scenarioButton != null)
            {
                scenarioButton.clicked += StartGame;
            }

            if (settingsButton != null)
            {
                settingsButton.clicked += ShowSettings;
            }

            if (closeSettingsButton != null)
            {
                closeSettingsButton.clicked += HideSettings;
            }

            if (quitButton != null)
            {
                quitButton.clicked += QuitGame;
            }

            if (soundToggle != null)
            {
                soundToggle.RegisterValueChangedCallback(OnSoundChanged);
            }

            if (vibrationToggle != null)
            {
                vibrationToggle.RegisterValueChangedCallback(OnVibrationChanged);
            }
        }

        private void ApplyTheme(VisualElement root)
        {
            if (themeStyleSheet != null)
            {
                root.styleSheets.Add(themeStyleSheet);
            }
        }

        private void UnbindEvents()
        {
            if (startButton != null)
            {
                startButton.clicked -= StartGame;
            }

            if (scenarioButton != null)
            {
                scenarioButton.clicked -= StartGame;
            }

            if (settingsButton != null)
            {
                settingsButton.clicked -= ShowSettings;
            }

            if (closeSettingsButton != null)
            {
                closeSettingsButton.clicked -= HideSettings;
            }

            if (quitButton != null)
            {
                quitButton.clicked -= QuitGame;
            }

            if (soundToggle != null)
            {
                soundToggle.UnregisterValueChangedCallback(OnSoundChanged);
            }

            if (vibrationToggle != null)
            {
                vibrationToggle.UnregisterValueChangedCallback(OnVibrationChanged);
            }
        }

        private void SyncSettings()
        {
            if (soundToggle != null)
            {
                soundToggle.SetValueWithoutNotify(AppSettings.SoundEnabled);
            }

            if (vibrationToggle != null)
            {
                vibrationToggle.SetValueWithoutNotify(AppSettings.VibrationEnabled);
            }
        }

        private void StartGame()
        {
            if (levelDifficultyManager != null)
            {
                levelDifficultyManager.PlaySelectedBranch();
                return;
            }

            if (preLevelPopupController != null)
            {
                preLevelPopupController.OpenForLevel((int)SceneId.Game);
                return;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.InGame);
            }

            SceneLoader.Load(SceneId.Game);
        }

        private void ShowSettings()
        {
            settingsPanel?.RemoveFromClassList("hidden");
        }

        private void HideSettings()
        {
            settingsPanel?.AddToClassList("hidden");
        }

        private static void QuitGame()
        {
            Application.Quit();
        }

        private static void OnSoundChanged(ChangeEvent<bool> evt)
        {
            AppSettings.SoundEnabled = evt.newValue;
        }

        private static void OnVibrationChanged(ChangeEvent<bool> evt)
        {
            AppSettings.VibrationEnabled = evt.newValue;
        }
    }
}
