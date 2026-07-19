using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RushBank.EditorTools
{
    public static class RushBankBuildTool
    {
        private const string OutputPath = "Builds/RushBank.apk";
        private const string PackageName = "com.yusufekici.rushbank";

        [MenuItem("RushBank/Configure Android Portrait Settings")]
        public static void ConfigureAndroidPortraitSettings()
        {
            if (PlayerSettings.companyName == "DefaultCompany")
            {
                PlayerSettings.companyName = "RushBank Team";
            }

            PlayerSettings.productName = "RushBank";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            var androidSupported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
            if (androidSupported && EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            EditorUtility.DisplayDialog(
                "RushBank Portrait Setup",
                androidSupported
                    ? "Android hedefi ve dikey ekran ayarlari yapildi.\n\nGame view icin 9:16 veya 1080x1920 aspect secmeyi unutma."
                    : "Dikey ekran ayarlari yapildi.\n\nAndroid Build Support bu Unity kurulumunda aktif degil. APK almak icin Unity Hub uzerinden Android Build Support modulunu eklemen gerekecek.\n\nGame view icin 9:16 veya 1080x1920 aspect secmeyi unutma.",
                "Tamam");
        }

        [MenuItem("RushBank/Build Android APK")]
        public static void BuildAndroidApk()
        {
            var scenes = CollectScenes();
            if (scenes.Length == 0)
            {
                var runSetup = EditorUtility.DisplayDialog(
                    "RushBank Build",
                    "Build listesinde sahne yok. Önce prototype sahnelerinin kurulması gerekiyor. Şimdi kurulsun mu?",
                    "Kur ve Devam Et",
                    "Vazgeç");
                if (!runSetup)
                {
                    return;
                }

                RushBankPrototypeSetup.SetupPrototypeScenes();
                scenes = CollectScenes();
                if (scenes.Length == 0)
                {
                    return;
                }
            }

            ConfigureAndroidPortraitSettings();
            EditorUserBuildSettings.buildAppBundle = false;

            Directory.CreateDirectory("Builds");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(OutputPath);
                EditorUtility.DisplayDialog(
                    "RushBank Build",
                    "APK hazır: " + OutputPath + "\nBoyut: " + (report.summary.totalSize / (1024 * 1024)) + " MB",
                    "Tamam");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "RushBank Build",
                    "Build başarısız oldu. Console'daki hatalara bakın.",
                    "Tamam");
            }
        }

        private static string[] CollectScenes()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled && File.Exists(scene.path))
                .Select(scene => scene.path)
                .ToArray();
        }
    }
}
