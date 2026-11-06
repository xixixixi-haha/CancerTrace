using UnityEditor;
namespace CancerTrace.EditorTools
{
    public static class WindowsReleaseSettings
    {
        [MenuItem("CancerTrace/Release/Select Windows x64 (No Build)")]
        public static void SelectWindowsX64()
        {
            EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.StandaloneWindows64;
            UnityEngine.Debug.Log("Windows x64 selected. No Build executed.");
        }
    }
}
