using System.Collections;
using UnityEngine;
using UnityEngine.UI;
// Se usi TMP, scommenta la riga sotto e cambia il tipo di keyLabel
using TMPro;

[DisallowMultipleComponent]
public class ColorBarSlot : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform container;  // RectTransform root dello slot
    [SerializeField] private Image barImage;           // Image della barra colorata
    [SerializeField] private TMP_Text keyLabel;            // usa TMP_Text se preferisci

    [Header("Config - Sizes")]
    [SerializeField] private Vector2 normalSize = new Vector2(220f, 24f);
    [SerializeField] private Vector2 highlightedSize = new Vector2(260f, 36f);
    [SerializeField, Range(0.05f, 0.6f)] private float animTime = 0.12f;

    [Header("Config - Colors")]
    [SerializeField] private Color dimColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [SerializeField] private float dimFactor = 0.4f;

    public Color SlotColor { get; private set; }

    Coroutine animCo;

    void Reset()
    {
        container = GetComponent<RectTransform>();
        if (!barImage) barImage = GetComponentInChildren<Image>(true);
        if (!keyLabel) keyLabel = GetComponentInChildren<TMP_Text>(true);
    }

    public void Setup(Color slotColor, string keyText)
    {
        SlotColor = slotColor;
        if (keyLabel) keyLabel.text = keyText;
        ApplyImmediate(false);
    }

    public void SetKeyLabel(string text)
    {
        if (keyLabel) keyLabel.text = text;
    }

    public void SetHighlighted(bool highlighted)
    {
        if (animCo != null) StopCoroutine(animCo);

        Color targetColor = highlighted ? SlotColor : GetDimmedColor(SlotColor);

        animCo = StartCoroutine(AnimateTo(
            highlighted ? highlightedSize : normalSize,
            targetColor
        ));
    }

    Color GetDimmedColor(Color baseColor)
    {
        // moltiplichiamo per una fattore < 1 → versione “spenta”
         // regola a gusto (0 = nero, 1 = colore pieno)
        return new Color(baseColor.r * dimFactor,
                         baseColor.g * dimFactor,
                         baseColor.b * dimFactor,
                         baseColor.a);
    }

    void ApplyImmediate(bool highlighted)
    {
        container.sizeDelta = highlighted ? highlightedSize : normalSize;
        barImage.color = highlighted ? SlotColor : GetDimmedColor(SlotColor);
    }


    IEnumerator AnimateTo(Vector2 targetSize, Color targetColor)
    {
        Vector2 startSize = container ? container.sizeDelta : Vector2.zero;
        Color startColor = barImage ? barImage.color : Color.white;

        float t = 0f;
        while (t < animTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / animTime);
            if (container) container.sizeDelta = Vector2.Lerp(startSize, targetSize, k);
            if (barImage) barImage.color = Color.Lerp(startColor, targetColor, k);
            yield return null;
        }
        if (container) container.sizeDelta = targetSize;
        if (barImage) barImage.color = targetColor;
        animCo = null;
    }
}
