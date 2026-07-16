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

            if (PlayerSettings.companyName == "DefaultCompany")
            {
                PlayerSettings.companyName = "RushBank Team";
            }

            PlayerSettings.productName = "RushBank";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
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
