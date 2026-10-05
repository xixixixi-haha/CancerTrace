using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CancerTrace.EditorTools
{
    public static class PhaseB5CameraValidation
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/CaseAnalysis.unity",
            "Assets/Scenes/CancerGalaxy.unity",
            "Assets/Scenes/ResultSummary.unity"
        };

        [MenuItem("CancerTrace/Phase B.5/Validate Formal Scene Cameras")]
        public static void Run()
        {
            for (int index = 0; index < ScenePaths.Length; index++)
            {
                Scene scene = EditorSceneManager.OpenScene(ScenePaths[index], OpenSceneMode.Single);
                Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>(true);
                Require(cameras.Length == 1, "Expected exactly one Camera in " + scene.path);
                Require(cameras[0].CompareTag("MainCamera"), "Camera is not tagged MainCamera in " + scene.path);
                Require(cameras[0].enabled, "Main Camera is disabled in " + scene.path);

                Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
                Require(canvases.Length == 1, "Expected exactly one Canvas in " + scene.path);
                Require(canvases[0].renderMode == RenderMode.ScreenSpaceOverlay,
                    "Canvas render mode is not Screen Space - Overlay in " + scene.path);
                CanvasScaler scaler = canvases[0].GetComponent<CanvasScaler>();
                Require(scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                        scaler.referenceResolution == new Vector2(1920f, 1080f),
                    "Canvas Scaler is invalid in " + scene.path);

                TMP_Text[] text = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
                Require(text.Length > 0, "No TMP text exists in " + scene.path);
            }

            Debug.Log("PHASE_B5_CAMERA_VALIDATION_PASSED: all four formal scenes have an enabled Main Camera and 1920x1080 Overlay Canvas.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
