using System;
using CancerTrace.UI.Controllers;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CancerTrace.EditorTools
{
    public static class MainMenuVisualBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string FontPath = "Assets/Fonts/TMP/SourceHanSansSC-Regular SDF.asset";
        private const string BackgroundPath = "Assets/Art/Background/bg_mainmenu1.png";
        private const string LogoPath = "Assets/Art/UI/Branding/logo_cancertrace.png";
        private const string StartButtonPath = "Assets/Art/UI/Buttons/ui_btn_mainmenu_start.png";
        private const string ContinueButtonPath = "Assets/Art/UI/Buttons/ui_btn_mainmenu_continue.png";
        private const string TutorialButtonPath = "Assets/Art/UI/Buttons/ui_btn_mainmenu_tutorial.png";

        [MenuItem("CancerTrace/MainMenu/Rebuild Visual")]
        public static void RebuildVisual()
        {
            TMP_FontAsset font = LoadRequired<TMP_FontAsset>(FontPath);
            Sprite background = LoadRequired<Sprite>(BackgroundPath);
            Sprite logo = LoadRequired<Sprite>(LogoPath);
            Sprite startButton = LoadRequired<Sprite>(StartButtonPath);
            Sprite continueButton = LoadRequired<Sprite>(ContinueButtonPath);
            Sprite tutorialButton = LoadRequired<Sprite>(TutorialButtonPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateMainCamera();
            GameObject root = new GameObject("MainMenuController");
            MainMenuController controller = root.AddComponent<MainMenuController>();
            controller.BuildUi(font, background, logo, startButton, continueButton, tutorialButton);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Could not save MainMenu scene.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("MAIN_MENU_VISUAL_REBUILT");
        }

        public static void RebuildAndValidate()
        {
            RebuildVisual();
            MainMenuVisualValidation.Validate();
        }

        private static void CreateMainCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(39, 48, 67, 255);
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Required asset is missing: " + path);
            return asset;
        }
    }
}
