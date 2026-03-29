using System.Collections;
using UnityEngine;

/// <summary>
/// Cross-fade scene transition effect.
/// FIX: Use unscaledDeltaTime and null check after each yield
/// </summary>
public class CrossFade : SceneTransition
{
    public CanvasGroup canvasGroup;
    public float duration = 1f;

    public override IEnumerator AnimateTransitionIn()
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("CrossFade: canvasGroup is not assigned. Skipping transition in.");
            yield break;
        }

        float time = 0f;
        canvasGroup.alpha = 0f;

        while (time < duration)
        {
            // FIX: Use unscaledDeltaTime for pause-safe transitions
            time += Time.unscaledDeltaTime;
            
            // FIX: Check for destroyed object during scene unload
            if (canvasGroup == null) yield break;
            
            canvasGroup.alpha = Mathf.Clamp01(time / duration);
            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    public override IEnumerator AnimateTransitionOut()
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("CrossFade: canvasGroup is not assigned. Skipping transition out.");
            yield break;
        }

        float time = 0f;
        canvasGroup.alpha = 1f;

        while (time < duration)
        {
            // FIX: Use unscaledDeltaTime for pause-safe transitions
            time += Time.unscaledDeltaTime;
            
            // FIX: Check for destroyed object during scene unload
            if (canvasGroup == null) yield break;
            
            canvasGroup.alpha = 1f - Mathf.Clamp01(time / duration);
            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }
}
