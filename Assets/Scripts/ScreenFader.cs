using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    [SerializeField] private float duration = 0.25f;
    private CanvasGroup _cg;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        _cg.alpha = 0f;
    }

    public IEnumerator FadeOut()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _cg.alpha = Mathf.InverseLerp(0f, duration, t);
            yield return null;
        }
        _cg.alpha = 1f;
    }

    public IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _cg.alpha = 1f - Mathf.InverseLerp(0f, duration, t);
            yield return null;
        }
        _cg.alpha = 0f;
    }
}
