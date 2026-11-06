using CancerTrace.Debugging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CancerTrace.EditorTools
{
    public static class PhaseBPlayModeSmokeLauncher
    {
        [InitializeOnLoadMethod]
        private static void RegisterCleanup()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredEditMode) return;
                SessionState.EraseString("CancerTraceSmoke.SavePath");
                SessionState.EraseString("CancerTraceSmoke.TutorialPath");
            };
        }

        public static void Run()
        {
            string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CancerTraceSmokeTests",
                System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(root);
            SessionState.SetString("CancerTraceSmoke.SavePath", System.IO.Path.Combine(root, "save.json"));
            SessionState.SetString("CancerTraceSmoke.TutorialPath", System.IO.Path.Combine(root, "tutorial.json"));
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            GameObject runner = new GameObject("PhaseBPlayModeSmokeRunner");
            runner.AddComponent<PhaseBPlayModeSmokeRunner>();
            EditorApplication.isPlaying = true;
        }
    }
}
