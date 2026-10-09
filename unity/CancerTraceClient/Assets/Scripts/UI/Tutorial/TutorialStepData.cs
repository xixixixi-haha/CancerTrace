using System;
using UnityEngine;

namespace CancerTrace.UI.Tutorial
{
    public enum TutorialStepType
    {
        NextButton,
        TargetInteraction,
        NoAdvance
    }

    [Serializable]
    public sealed class TutorialStepData
    {
        public string id;
        public RectTransform target;
        [Tooltip("Optional larger raycast hole while keeping the visual highlight on target.")]
        public RectTransform interactionRegion;
        [TextArea(2, 6)] public string text;
        public TutorialStepType stepType;
        public Vector2 dialogPosition;
        [Tooltip("Optional per-step dialog size. Zero keeps the shared scene size.")]
        public Vector2 dialogSize;
        public Vector2 arrowPosition;
        public float arrowRotation;
        public Vector2 arrowSize = new Vector2(180f, 180f);
        public Vector2 arrowScale = Vector2.one;
        [Tooltip("Show the target highlight and its cutout. Disable for dialog-only steps.")]
        public bool showHighlight = true;
        [Tooltip("Show the tutorial arrow. Disable for dialog-only steps.")]
        public bool showArrow = true;
        [Tooltip("Optional per-step sprite for the shared Tutorial NextButton.")]
        public Sprite nextButtonSprite;
        [Tooltip("Highlight padding order: X = Left, Y = Bottom, Z = Right, W = Top.")]
        public Vector4 highlightPadding = new Vector4(16f, 16f, 16f, 16f);
    }
}
