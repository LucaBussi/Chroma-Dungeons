using UnityEngine;
using UnityEngine.UI;

public class ShieldUI : MonoBehaviour
{
    [SerializeField] private Image emptySlotImage; // cornice/slot
    [SerializeField] private Image fillImage;      // riempimento (Filled)

    void Awake() { AutoWire(); EnsureSetup(); }
#if UNITY_EDITOR
    void OnValidate() { if (!Application.isPlaying) { AutoWire(); EnsureSetup(); } }
#endif

    void AutoWire()
    {
        if (!emptySlotImage || !fillImage)
        {
            var imgs = GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
            {
                if (img.type == Image.Type.Filled) { if (!fillImage) fillImage = img; }
                else { if (!emptySlotImage) emptySlotImage = img; }
            }
        }
    }

    void EnsureSetup()
    {
        if (fillImage)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;   // Left
            fillImage.fillAmount = 1f;  // default pieno, poi SetFill lo aggiorna
            fillImage.enabled = true;
        }
        if (emptySlotImage)
        {
            emptySlotImage.type = Image.Type.Simple;
            emptySlotImage.color = Color.white;
            emptySlotImage.material = null;
            emptySlotImage.enabled = true;
        }
    }

    // t in [0..1]
    public void SetFill(float t)
    {
        if (!fillImage) return;
        t = Mathf.Clamp01(t);
        fillImage.enabled = t > 0f;
        fillImage.fillAmount = t;
    }

    public void SetSlotVisible(bool visible)
    {
        if (emptySlotImage) emptySlotImage.enabled = visible;
        if (!visible && fillImage) fillImage.enabled = false;
    }
}
