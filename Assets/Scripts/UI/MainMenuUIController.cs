using RushBank.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace RushBank.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument document;

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
        }

        private void OnEnable()
        {
            AppSettings.Apply();

            var root = document.rootVisualElement;
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
