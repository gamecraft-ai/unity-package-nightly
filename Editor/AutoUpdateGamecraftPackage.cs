#if UNITY_EDITOR// Strip from player builds
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

[InitializeOnLoad]
internal static class GamecraftPackageUpdater
{
    const string TogglePref = "Gamecraft.UseNightlyPackage";
    const string SessionFlag = "Gamecraft.UpdatedThisSession";
    const string PackageId = "com.gamecraft.gamecraft";

    const string StableRepo = "https://github.com/gamecraft-ai/unity-package.git";
    const string NightlyRepo = "https://github.com/gamecraft-ai/unity-package-nightly.git";

    static GamecraftPackageUpdater()
    {
        // Run once per editor session
        if (SessionState.GetBool(SessionFlag, false)) return;
        SessionState.SetBool(SessionFlag, true);

        EditorApplication.delayCall += TryUpdate;
    }

    [MenuItem("Tools/Gamecraft/Switch to Nightly Package", priority = 2000)]
    static void ToggleNightly()
    {
        EditorPrefs.SetBool(TogglePref, !EditorPrefs.GetBool(TogglePref, false));
        TryUpdate();
    }

    [MenuItem("Tools/Gamecraft/Switch to Nightly Package", validate = true)]
    static bool ValidateNightly()
    {
        Menu.SetChecked("Tools/Gamecraft/Switch to Nightly Package", EditorPrefs.GetBool(TogglePref, false));
        return true;
    }

    static void TryUpdate()
    {
        string targetRepo = EditorPrefs.GetBool(TogglePref, false) ? NightlyRepo : StableRepo;

        if (RewriteManifestIfRequired(targetRepo))
            Debug.Log($"[Gamecraft] Switched Gamecraft feed to {(targetRepo == NightlyRepo ? "nightly" : "stable")}.");

        // Even when the repo URL is the same, Resolve() pings HEAD and updates if needed.
        Client.Resolve();
    }

    static bool RewriteManifestIfRequired(string repoUrl)
    {
        string path = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        string json = File.ReadAllText(path);

        string currentPattern = $"\"{Regex.Escape(PackageId)}\"\\s*:\\s*\"([^\"]+)\"";
        Match match = Regex.Match(json, currentPattern);

        if (!match.Success)
        {
            Debug.LogError($"[Gamecraft] Could not find dependency \"{PackageId}\" in manifest.json.");
            return false;
        }

        string currentUrl = match.Groups[1].Value;
        if (currentUrl == repoUrl) return false;// already correct

        string updated = json.Replace(currentUrl, repoUrl);
        File.WriteAllText(path, updated);
        AssetDatabase.Refresh();
        return true;
    }
}
#endif
