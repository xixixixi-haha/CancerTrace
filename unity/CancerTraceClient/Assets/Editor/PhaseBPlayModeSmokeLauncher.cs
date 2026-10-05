using CancerTrace.Debugging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CancerTrace.EditorTools
{
    public static class PhaseBPlayModeSmokeLauncher
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            GameObject runner = new GameObject("PhaseBPlayModeSmokeRunner");
            runner.AddComponent<PhaseBPlayModeSmokeRunner>();
            EditorApplication.isPlaying = true;
        }
    }
}
