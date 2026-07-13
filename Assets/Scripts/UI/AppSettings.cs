using UnityEngine;

namespace RushBank.UI
{
    public static class AppSettings
    {
        private const string SoundEnabledKey = "rushbank.sound.enabled";
        private const string VibrationEnabledKey = "rushbank.vibration.enabled";

        public static bool SoundEnabled
        {
            get => PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(SoundEnabledKey, value ? 1 : 0);
                PlayerPrefs.Save();
                ApplyAudio();
            }
        }

        public static bool VibrationEnabled
        {
            get => PlayerPrefs.GetInt(VibrationEnabledKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(VibrationEnabledKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void Apply()
        {
            ApplyAudio();
        }

        private static void ApplyAudio()
        {
            AudioListener.volume = SoundEnabled ? 1f : 0f;
        }
    }
}
