using System;
using System.Collections.Generic;
using CancerTrace.UI.Controllers;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CancerTrace.EditorTools
{
    public static class ResultSummaryUiValidation
    {
        private const string ScenePath = "Assets/Scenes/ResultSummary.unity";
        private const string FormalFontPath = "Assets/Fonts/TMP/SourceHanSansSC-Regular SDF.asset";

        [MenuItem("CancerTrace/Phase B/Validate Result Summary UI")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ResultSummaryController controller = UnityEngine.Object.FindObjectOfType<ResultSummaryController>();
            Require(controller != null, "ResultSummaryController is missing.");

            Component[] components = controller.transform.root.GetComponentsInChildren<Component>(true);
            for (int index = 0; index < components.Length; index++)
            {
                Require(components[index] != null, "ResultSummary contains a Missing Script.");
            }

            SerializedObject serializedController = new SerializedObject(controller);
            string[] requiredReferences =
            {
                "correctBanner", "wrongBanner", "playerDiagnosisIcon", "trueDiagnosisIcon",
                "aiPredictionIcon", "playerDiagnosisText", "trueDiagnosisText", "scoreEarnedText",
                "aiPredictionText", "confidenceText", "topCandidatesText", "nextButton",
                "lungIcon", "skinIcon", "cnsBrainIcon", "bowelIcon", "esophagusStomachIcon",
                "breastIcon", "boneIcon", "ovaryFallopianTubeIcon"
            };
            for (int index = 0; index < requiredReferences.Length; index++)
            {
                SerializedProperty property = serializedController.FindProperty(requiredReferences[index]);
                Require(property != null, "Missing serialized field: " + requiredReferences[index]);
                Require(property.objectReferenceValue != null, "Missing reference: " + requiredReferences[index]);
            }

            RequireSprite("ResultTitleBanner", "Assets/Art/UI/TitleBanners/ui_title_result_summary.png");
            RequireSprite("CorrectBanner", "Assets/Art/UI/TitleBanners/ui_result_correct.png");
            RequireSprite("WrongBanner", "Assets/Art/UI/TitleBanners/ui_result_wrong.png");
            RequireSprite("ScoreStar", "Assets/Art/Icons/Common/icon_star.png");
            RequireSprite("AiReviewIcon", "Assets/Art/Icons/Common/icon_lightbulb.png");
            RequireSprite("NextCase", "Assets/Art/UI/Buttons/ui_btn_next_normal.png.png");

            string[] forbiddenObjects =
            {
                "Title", "Verdict", "Diagnosis", "Score", "AiReview", "Feedback", "Anomaly", "Character"
            };
            for (int index = 0; index < forbiddenObjects.Length; index++)
            {
                Require(GameObject.Find(forbiddenObjects[index]) == null,
                    "Obsolete ResultSummary object remains: " + forbiddenObjects[index]);
            }

            TMP_FontAsset formalFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FormalFontPath);
            Require(formalFont != null, "Formal TMP font is missing.");
            HashSet<string> allowedStaticText = new HashSet<string>
            {
                "你的诊断", "正确诊断", "本病例得分", "AI教学复盘", "AI判断", "置信度", "前三候选"
            };
            TMP_Text[] texts = controller.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < texts.Length; index++)
            {
                Require(texts[index].font == formalFont, "Wrong TMP font on " + texts[index].name);
                Require(string.IsNullOrEmpty(texts[index].text) || allowedStaticText.Contains(texts[index].text),
                    "Unexpected static text on " + texts[index].name + ": " + texts[index].text);
            }

            Require(GameObject.Find("ResultContentArea") != null, "ResultContentArea is missing.");
            Require(GameObject.Find("ResultContentArea").GetComponent<Image>() == null,
                "ResultContentArea must not contain a foreground panel Image.");
            Require(scene.IsValid(), "ResultSummary scene failed to open.");
            Debug.Log("RESULT_SUMMARY_UI_VALIDATION_PASSED");
        }

        private static void RequireSprite(string objectName, string expectedPath)
        {
            GameObject gameObject = GameObject.Find(objectName);
            Require(gameObject != null, objectName + " is missing.");
            Image image = gameObject.GetComponent<Image>();
            Require(image != null, objectName + " has no Image component.");
            Require(image.sprite != null, objectName + " has no sprite.");
            Require(string.Equals(AssetDatabase.GetAssetPath(image.sprite), expectedPath, StringComparison.Ordinal),
                objectName + " uses the wrong sprite.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
