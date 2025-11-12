using UnityEngine;

/// <summary>
/// Fixes Canvas positioning issues that can occur after scene transitions
/// Attach this to any Canvas that has positioning problems
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CanvasFixer : MonoBehaviour
{
    void Start()
    {
        // Force canvas to recalculate
        Canvas canvas = GetComponent<Canvas>();

        if (canvas != null)
        {
            // Temporarily disable and re-enable to force recalculation
            canvas.enabled = false;
            canvas.enabled = true;

            Debug.Log($"Canvas '{gameObject.name}' fixed - renderMode: {canvas.renderMode}");
        }

        // Also fix the RectTransform
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Force the RectTransform to update
            rectTransform.ForceUpdateRectTransforms();

            Debug.Log($"RectTransform for '{gameObject.name}' updated - pivot: {rectTransform.pivot}, scale: {rectTransform.localScale}");
        }
    }
}
