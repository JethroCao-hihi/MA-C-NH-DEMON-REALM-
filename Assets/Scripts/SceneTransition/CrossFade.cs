using System.Collections;
using UnityEngine;

public class CrossFade : SceneTransition
{
    public CanvasGroup canvasGroup;
    public float duration = 1f;

    public override IEnumerator AnimateTransitionIn()
    {
        float time = 0f;
        canvasGroup.alpha = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(time / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    public override IEnumerator AnimateTransitionOut()
    {
        float time = 0f;
        canvasGroup.alpha = 1f;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(time / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}