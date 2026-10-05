using System;
using CancerTrace.UI.Controllers;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CancerTrace.EditorTools
{
    public static class PhaseBSceneBuilder
    {
        private const string SceneDirectory = "Assets/Scenes";
        private const string FontPath = "Assets/Fonts/TMP/SourceHanSansSC-Regular SDF.asset";
        private const string SharedBackgroundPath = "Assets/Art/Background/SharedBackground.png";
        private const string MainMenuBackgroundPath = "Assets/Art/Background/bg_mainmenu1.png";
        private const string MainMenuLogoPath = "Assets/Art/UI/Branding/logo_cancertrace.png";
        private const string MainMenuStartButtonPath = "Assets/Art/UI/Buttons/ui_btn_mainmenu_start.png";
        private const string MainMenuContinueButtonPath = "Assets/Art/UI/Buttons/ui_btn_mainmenu_continue.png";
        private const string MainMenuTutorialButtonPath = "Assets/Art/UI/Buttons/ui_btn_mainmenu_tutorial.png";
        private const string CasePanelPath = "Assets/Art/UI/Panels/ui_panel_case_info.png";
        private const string EvidencePanelPath = "Assets/Art/UI/Panels/ui_panel_evidence_board.png";
        private const string NotebookPanelPath = "Assets/Art/UI/Panels/ui_panel_notebook.png";
        private const string DialogPanelPath = "Assets/Art/UI/Panels/ui_panel_dialog.png";
        private const string GeneButtonPath = "Assets/Art/UI/Buttons/ui_btn_gene_scan_normal.png.png";
        private const string ObserveButtonPath = "Assets/Art/UI/Buttons/ui_btn_observe_normal.png.png";
        private const string HintButtonPath = "Assets/Art/UI/Buttons/ui_btn_hint_normal.png.png";
        private const string SubmitButtonPath = "Assets/Art/UI/Buttons/ui_btn_submit_normal.png.png";
        private const string BackButtonPath = "Assets/Art/UI/Buttons/ui_btn_back_normal.png.png";
        private const string NextButtonPath = "Assets/Art/UI/Buttons/ui_btn_next_normal.png.png";
        private const string DetectiveSuccessPath = "Assets/Art/Character/Detective/char_detective_success.png";

        [MenuItem("CancerTrace/Phase B/Build Formal Scenes")]
        public static void BuildFormalScenes()
        {
            TMP_FontAsset font = LoadRequired<TMP_FontAsset>(FontPath);
            Sprite sharedBackground = LoadRequired<Sprite>(SharedBackgroundPath);
            Sprite mainMenuBackground = LoadRequired<Sprite>(MainMenuBackgroundPath);
            Sprite mainMenuLogo = LoadRequired<Sprite>(MainMenuLogoPath);
            Sprite mainMenuStartButton = LoadRequired<Sprite>(MainMenuStartButtonPath);
            Sprite mainMenuContinueButton = LoadRequired<Sprite>(MainMenuContinueButtonPath);
            Sprite mainMenuTutorialButton = LoadRequired<Sprite>(MainMenuTutorialButtonPath);
            Sprite casePanel = LoadRequired<Sprite>(CasePanelPath);
            Sprite evidencePanel = LoadRequired<Sprite>(EvidencePanelPath);
            Sprite notebookPanel = LoadRequired<Sprite>(NotebookPanelPath);
            Sprite dialogPanel = LoadRequired<Sprite>(DialogPanelPath);
            Sprite geneButton = LoadRequired<Sprite>(GeneButtonPath);
            Sprite observeButton = LoadRequired<Sprite>(ObserveButtonPath);
            Sprite hintButton = LoadRequired<Sprite>(HintButtonPath);
            Sprite submitButton = LoadRequired<Sprite>(SubmitButtonPath);
            Sprite backButton = LoadRequired<Sprite>(BackButtonPath);
            Sprite nextButton = LoadRequired<Sprite>(NextButtonPath);
            Sprite detectiveSuccess = LoadRequired<Sprite>(DetectiveSuccessPath);

            BuildScene("MainMenu", font, root =>
            {
                MainMenuController controller = root.AddComponent<MainMenuController>();
                controller.BuildUi(
                    font,
                    mainMenuBackground,
                    mainMenuLogo,
                    mainMenuStartButton,
                    mainMenuContinueButton,
                    mainMenuTutorialButton);
            });
            BuildScene("CaseAnalysis", font, root =>
            {
                CaseAnalysisController controller = root.AddComponent<CaseAnalysisController>();
                controller.BuildUi(
                    font,
                    sharedBackground,
                    casePanel,
                    evidencePanel,
                    notebookPanel,
                    geneButton,
                    observeButton,
                    hintButton,
                    submitButton);
            });
            BuildScene("CancerGalaxy", font, root =>
            {
                CancerGalaxyController controller = root.AddComponent<CancerGalaxyController>();
                controller.BuildUi(font, sharedBackground, notebookPanel, backButton);
            });
            BuildScene("ResultSummary", font, root =>
            {
                ResultSummaryController controller = root.AddComponent<ResultSummaryController>();
                controller.BuildUi(font, sharedBackground, dialogPanel, nextButton, detectiveSuccess);
            });

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneDirectory + "/MainMenu.unity", true),
                new EditorBuildSettingsScene(SceneDirectory + "/CaseAnalysis.unity", true),
                new EditorBuildSettingsScene(SceneDirectory + "/CancerGalaxy.unity", true),
                new EditorBuildSettingsScene(SceneDirectory + "/ResultSummary.unity", true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PHASE_B_SCENES_BUILT: MainMenu, CaseAnalysis, CancerGalaxy, ResultSummary");
        }

        private static void BuildScene(
            string sceneName,
            TMP_FontAsset formalFont,
            Action<GameObject> build)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateMainCamera();
            GameObject root = new GameObject(sceneName + "Controller");
            build(root);
            ApplyFormalFont(root, formalFont);
            string path = SceneDirectory + "/" + sceneName + ".unity";
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new InvalidOperationException("Could not save formal scene: " + path);
            }
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

        private static void ApplyFormalFont(GameObject root, TMP_FontAsset formalFont)
        {
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < texts.Length; index++)
            {
                TMP_Text text = texts[index];
                SerializedObject serializedText = new SerializedObject(text);
                SerializedProperty fontProperty = serializedText.FindProperty("m_fontAsset");
                SerializedProperty materialProperty = serializedText.FindProperty("m_sharedMaterial");
                if (fontProperty == null || materialProperty == null)
                {
                    throw new InvalidOperationException(
                        "TMP serialized font fields are unavailable on " + text.name + ".");
                }

                fontProperty.objectReferenceValue = formalFont;
                materialProperty.objectReferenceValue = formalFont.material;
                serializedText.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(text);
            }
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Required asset is missing: " + path);
            return asset;
        }
    }
}
