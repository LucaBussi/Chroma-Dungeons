using UnityEngine;
using UnityEngine.UI;

public class HeartUI : MonoBehaviour
{
    [SerializeField] private Image emptySlotImage; // nero sotto (Simple)
    [SerializeField] private Image fillImage;      // rosso sopra (Filled)

    void Awake() { AutoWire(); EnsureSetup(); }
#if UNITY_EDITOR
    void OnValidate() { if (!Application.isPlaying) { AutoWire(); EnsureSetup(); } }
#endif

    void AutoWire()
    {
        // se non assegnati, prende la prima Image non-Filled come slot e la prima Filled come fill
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
            fillImage.color = Color.white;
            fillImage.material = null;
        }
        if (emptySlotImage)
        {
            emptySlotImage.type = Image.Type.Simple;
            emptySlotImage.color = Color.white;
            emptySlotImage.material = null;
            emptySlotImage.enabled = true; // sempre visibile sotto
        }
    }

    // t in [0..1]
    public void SetFill(float t)
    {
        if (!fillImage) return;
        fillImage.fillAmount = Mathf.Clamp01(t);
    }
}
