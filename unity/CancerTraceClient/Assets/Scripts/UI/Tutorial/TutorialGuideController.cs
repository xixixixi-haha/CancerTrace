using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CancerTrace.UI.Tutorial
{
    [ExecuteAlways]
    public sealed class TutorialGuideController : MonoBehaviour
    {
        [Header("Preview")]
        [SerializeField] private bool previewTutorialInPlayMode;
        [SerializeField, Min(0)] private int editModePreviewStep = 2;

        [Header("Guide Layer")]
        [SerializeField] private RectTransform guideRect;
        [SerializeField] private RectTransform dimTop;
        [SerializeField] private RectTransform dimBottom;
        [SerializeField] private RectTransform dimLeft;
        [SerializeField] private RectTransform dimRight;
        [SerializeField] private RectTransform highlightFrame;
        [SerializeField] private RectTransform tutorialArrow;
        [SerializeField] private RectTransform tutorialDialog;
        [SerializeField] private TMP_Text tutorialText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Vector2 nextButtonOffset = new Vector2(166.445068f, -148.1005f);

        [Header("Steps")]
        [SerializeField] private List<TutorialStepData> steps = new List<TutorialStepData>();

        private int currentStepIndex;
        private bool listenerBound;
        private bool runtimeTutorialRequested;
        private int requestedRuntimeStepIndex;

        public int CurrentStepIndex => currentStepIndex;
        public event Action RuntimeNextRequested;

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                if (runtimeTutorialRequested)
                {
                    BindButton();
                    ShowStep(requestedRuntimeStepIndex);
                    return;
                }

                if (!previewTutorialInPlayMode)
                {
                    gameObject.SetActive(false);
                    return;
                }

                BindButton();
                ShowStep(0);
                return;
            }

        }

        private void OnDisable()
        {
            if (listenerBound && nextButton != null)
            {
                nextButton.onClick.RemoveListener(ShowNextStep);
                listenerBound = false;
            }
        }

        private void OnValidate()
        {
            // Never write RectTransforms from OnValidate. Unity can invoke this callback
            // during CheckConsistency, where layout writes trigger SendMessage warnings.
            editModePreviewStep = Mathf.Max(0, editModePreviewStep);
        }

        private void BindButton()
        {
            if (listenerBound || nextButton == null) return;
            nextButton.onClick.AddListener(ShowNextStep);
            listenerBound = true;
        }

        public void ShowNextStep()
        {
            if (runtimeTutorialRequested)
            {
                TutorialStepData step = GetCurrentStep();
                if (step != null && step.stepType == TutorialStepType.NextButton)
                {
                    RuntimeNextRequested?.Invoke();
                }
                return;
            }

            int nextIndex = currentStepIndex + 1;
            if (nextIndex < steps.Count)
            {
                ShowStep(nextIndex);
                return;
            }

            Debug.Log("Tutorial framework preview completed.");
            gameObject.SetActive(false);
        }

        public void ShowStep(int index)
        {
            if (!HasValidBindings() || index < 0 || index >= steps.Count) return;

            TutorialStepData step = steps[index];
            if (step == null || (step.showHighlight && step.target == null)) return;

            currentStepIndex = index;
            tutorialText.text = step.text;
            tutorialDialog.anchoredPosition = step.dialogPosition;
            if (step.dialogSize.x > 0f && step.dialogSize.y > 0f)
            {
                tutorialDialog.sizeDelta = step.dialogSize;
            }
            nextButton.GetComponent<RectTransform>().anchoredPosition = step.dialogPosition + nextButtonOffset;
            ApplyNextButtonVisibility(step);
            if (step.nextButtonSprite != null && nextButton.image != null)
            {
                nextButton.image.sprite = step.nextButtonSprite;
            }

            tutorialArrow.gameObject.SetActive(step.showArrow);
            if (step.showArrow)
            {
                tutorialArrow.anchoredPosition = step.arrowPosition;
                tutorialArrow.localRotation = Quaternion.Euler(0f, 0f, step.arrowRotation);
                tutorialArrow.sizeDelta = step.arrowSize;
                tutorialArrow.localScale = new Vector3(step.arrowScale.x, step.arrowScale.y, 1f);
            }

            highlightFrame.gameObject.SetActive(step.showHighlight);
            if (!step.showHighlight)
            {
                UpdateFullScreenMask();
                return;
            }

            Vector2 highlightMin;
            Vector2 highlightMax;
            GetTargetBounds(step.target, step.highlightPadding, out highlightMin, out highlightMax);
            Vector2 holeMin = highlightMin;
            Vector2 holeMax = highlightMax;
            if (step.interactionRegion != null)
            {
                GetTargetBounds(step.interactionRegion, Vector4.zero, out holeMin, out holeMax);
            }
            UpdateMaskAndHighlight(holeMin, holeMax, highlightMin, highlightMax);
        }

        public void BeginRuntimeTutorial(int stepIndex)
        {
            runtimeTutorialRequested = true;
            requestedRuntimeStepIndex = stepIndex;
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            BindButton();
            ShowStep(requestedRuntimeStepIndex);
        }

        public void HideRuntimeTutorial()
        {
            runtimeTutorialRequested = false;
            gameObject.SetActive(false);
        }

        public void RefreshEditModePreview()
        {
            if (Application.isPlaying || steps == null || steps.Count == 0) return;
            int previewIndex = Mathf.Clamp(editModePreviewStep, 0, steps.Count - 1);
            ShowStep(previewIndex);
        }

        private bool HasValidBindings()
        {
            return guideRect != null && dimTop != null && dimBottom != null && dimLeft != null &&
                   dimRight != null && highlightFrame != null && tutorialArrow != null &&
                   tutorialDialog != null && tutorialText != null && nextButton != null && steps != null;
        }

        private TutorialStepData GetCurrentStep()
        {
            return steps != null && currentStepIndex >= 0 && currentStepIndex < steps.Count
                ? steps[currentStepIndex]
                : null;
        }

        private void ApplyNextButtonVisibility(TutorialStepData step)
        {
            bool showNextButton = step.stepType == TutorialStepType.NextButton;
            nextButton.gameObject.SetActive(showNextButton);
            nextButton.interactable = showNextButton;
        }

        private void GetTargetBounds(RectTransform target, Vector4 padding, out Vector2 min, out Vector2 max)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 first = guideRect.InverseTransformPoint(corners[0]);
            min = first;
            max = first;
            for (int index = 1; index < corners.Length; index++)
            {
                Vector3 local = guideRect.InverseTransformPoint(corners[index]);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            // Padding order: left, bottom, right, top.
            min -= new Vector2(padding.x, padding.y);
            max += new Vector2(padding.z, padding.w);

            Rect bounds = guideRect.rect;
            min = Vector2.Max(min, bounds.min);
            max = Vector2.Min(max, bounds.max);
        }

        private void UpdateMaskAndHighlight(
            Vector2 holeMin,
            Vector2 holeMax,
            Vector2 highlightMin,
            Vector2 highlightMax)
        {
            Rect bounds = guideRect.rect;
            SetRect(dimTop, new Vector2(bounds.xMin, holeMax.y), bounds.max);
            SetRect(dimBottom, bounds.min, new Vector2(bounds.xMax, holeMin.y));
            SetRect(dimLeft, new Vector2(bounds.xMin, holeMin.y), new Vector2(holeMin.x, holeMax.y));
            SetRect(dimRight, new Vector2(holeMax.x, holeMin.y), new Vector2(bounds.xMax, holeMax.y));
            SetRect(highlightFrame, highlightMin, highlightMax);
        }

        private void UpdateFullScreenMask()
        {
            Rect bounds = guideRect.rect;
            SetRect(dimTop, bounds.min, bounds.max);
            SetRect(dimBottom, bounds.min, bounds.min);
            SetRect(dimLeft, bounds.min, bounds.min);
            SetRect(dimRight, bounds.min, bounds.min);
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (min + max) * 0.5f;
            rect.sizeDelta = new Vector2(
                Mathf.Max(0f, max.x - min.x),
                Mathf.Max(0f, max.y - min.y));
        }
    }
}
