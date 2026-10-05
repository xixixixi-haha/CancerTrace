using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CancerTrace.EditorTools
{
    public static class MainMenuVisualValidation
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";

        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Assert(scene.IsValid(), "MainMenu scene could not be opened.");
            Assert(GameObject.Find("Main Camera")?.GetComponent<Camera>() != null, "Main Camera is missing.");

            Canvas canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            CanvasScaler scaler = canvas?.GetComponent<CanvasScaler>();
            Assert(canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay, "Canvas render mode changed.");
            Assert(scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                scaler.referenceResolution == new Vector2(1920f, 1080f), "Canvas scaler changed.");

            AssertSprite("Background", "Assets/Art/Background/bg_mainmenu1.png");
            AssertSprite("Logo", "Assets/Art/UI/Branding/logo_cancertrace.png");
            AssertButton("NewShiftButton", "Assets/Art/UI/Buttons/ui_btn_mainmenu_start.png", true);
            AssertButton("ContinueButton", "Assets/Art/UI/Buttons/ui_btn_mainmenu_continue.png", false);
            AssertButton("TutorialButton", "Assets/Art/UI/Buttons/ui_btn_mainmenu_tutorial.png", false);

            string[] removedNames = { "MenuPanel", "Detective", "CatAssistant", "Title", "Subtitle" };
            for (int index = 0; index < removedNames.Length; index++)
            {
                Assert(GameObject.Find(removedNames[index]) == null, "Legacy object remains: " + removedNames[index]);
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                Assert(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(roots[index]) == 0,
                    "Missing script on " + roots[index].name);
            }

            Debug.Log("MAIN_MENU_VISUAL_VALIDATION_PASSED");
        }

        private static void AssertButton(string name, string assetPath, bool expectedInteractable)
        {
            GameObject buttonObject = GameObject.Find(name);
            Button button = buttonObject?.GetComponent<Button>();
            Assert(button != null && button.interactable == expectedInteractable, "Unexpected button state: " + name);
            AssertSprite(name, assetPath);
            Assert(buttonObject.GetComponentsInChildren<TMP_Text>(true).Length == 0,
                "Button TMP label remains: " + name);
        }

        private static void AssertSprite(string objectName, string assetPath)
        {
            Image image = GameObject.Find(objectName)?.GetComponent<Image>();
            Sprite expected = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            Assert(image != null && image.sprite == expected, "Unexpected sprite on " + objectName);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
