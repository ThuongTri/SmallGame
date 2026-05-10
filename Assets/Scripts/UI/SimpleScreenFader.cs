using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class SimpleScreenFader : MonoBehaviour
{
    CanvasGroup cg;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    public IEnumerator FadeOut(float dur)
    {
        cg.blocksRaycasts = true;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / dur);
            yield return null;
        }
        cg.alpha = 1f;
    }

    public IEnumerator FadeIn(float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = 1f - Mathf.Clamp01(t / dur);
            yield return null;
        }
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
    }

    public void SetAlpha(float a)
    {
        cg.alpha = Mathf.Clamp01(a);
    }

    public IEnumerator PulseAlpha(float targetAlpha, float attack, float hold, float release)
    {
        float start = cg.alpha;
        float t = 0f;
        while (t < attack)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, Mathf.Clamp01(targetAlpha), Mathf.Clamp01(t / attack));
            yield return null;
        }
        cg.alpha = Mathf.Clamp01(targetAlpha);

        if (hold > 0f)
            yield return new WaitForSecondsRealtime(hold);

        t = 0f;
        float end = 0f;
        while (t < release)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(cg.alpha, end, Mathf.Clamp01(t / release));
            yield return null;
        }
        cg.alpha = end;
        cg.blocksRaycasts = false;
    }
}