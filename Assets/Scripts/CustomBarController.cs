using UnityEngine;
using UnityEngine.UI;

public enum ChevronMode { None, Up, Down }

[DisallowMultipleComponent]
public class CustomBarController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image barFrame;      // BarBG: Image Sliced (sprite cornice con Border)
    [SerializeField] private Image barFill;       // Image → Filled / Horizontal / Origin Left
    [SerializeField] private RectTransform rangeOverlay; // (opzionale) fascia finestra
    [SerializeField] private Image chevronImage;  // freccia laterale fissa (fuori dalla cornice)

    [Header("Chevron Sprites")]
    [SerializeField] private Sprite chevronUp;
    [SerializeField] private Sprite chevronDown;

    [Header("Theme")]
    [SerializeField] private Color themeColor = Color.white;   // R/G/B della riga

    [Header("Glow-as-Color (cornice)")]
    [SerializeField] private Color frameNormal = new Color(0.12f, 0.12f, 0.12f, 1f); // spento
    [SerializeField, Range(0f,1f)] private float glowAlpha = 0.9f;                    // acceso
    [SerializeField] private bool pulseGlow = false;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField, Range(0f,1f)] private float pulseMin = 0.6f;
    [SerializeField, Range(0f,1f)] private float pulseMax = 1.0f;

    [Header("Auto-Fit")]
    [Tooltip("Applica automaticamente gli offset del Fill/Overlay uguali ai Border della cornice.")]
    [SerializeField] private bool autoFitToFrame = true;
    [Tooltip("Pixel extra da aggiungere all'interno (Left, Bottom, Right, Top) dopo il fit.")]
    [SerializeField] private Vector4 extraInnerPadding = Vector4.zero; // L,B,R,T in pixel UI

    private bool glowOn;

    // ---------- API ----------
    public void SetValue(float v01)
    {
        v01 = Mathf.Clamp01(v01);
        if (barFill)
        {
            if (barFill.type != Image.Type.Filled)
            {
                barFill.type = Image.Type.Filled;
                barFill.fillMethod = Image.FillMethod.Horizontal;
                barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
            barFill.fillAmount = v01;
        }
    }

    public void SetGlowEnabled(bool on)
    {
        glowOn = on;
        ApplyFrameColorImmediate();
    }

    public void SetChevron(ChevronMode mode)
{
    if (!chevronImage) return;

    switch (mode)
    {
        case ChevronMode.None:
            chevronImage.enabled = false;
            break;

        case ChevronMode.Up:
            chevronImage.enabled = true;
            if (chevronUp)
            {
                chevronImage.sprite = chevronUp;
                chevronImage.rectTransform.localScale = Vector3.one; // reset scala normale
            }
            break;

        case ChevronMode.Down:
            chevronImage.enabled = true;
            if (chevronDown)
            {
                chevronImage.sprite = chevronDown;
                chevronImage.rectTransform.localScale = Vector3.one; // scala normale
            }
            else if (chevronUp)
            {
                chevronImage.sprite = chevronUp;
                // flip verticale
                Vector3 scale = chevronImage.rectTransform.localScale;
                chevronImage.rectTransform.localScale = new Vector3(scale.x, -Mathf.Abs(scale.y), scale.z);
            }
            break;
    }
}


    public void SetThemeColor(Color c)
    {
        themeColor = c;

        if (barFill)
        {
            var fc = barFill.color; fc.r = c.r; fc.g = c.g; fc.b = c.b; barFill.color = fc;
        }
        if (chevronImage)
        {
            var cc = chevronImage.color; cc.r = c.r; cc.g = c.g; cc.b = c.b; chevronImage.color = cc;
        }

        ApplyFrameColorImmediate();
    }

    // ---------- Setup ----------
    private void Awake()
    {
        if (barFill)
        {
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        /*if (barFrame)
        {
            //if (barFrame.type != Image.Type.Sliced) barFrame.type = Image.Type.Sliced;
            //barFrame.raycastTarget = false;
        }*/

        ApplyFrameColorImmediate();
        FitFillToFrame();
    }

    private void OnEnable() { FitFillToFrame(); }
#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyFrameColorImmediate();
        FitFillToFrame();
    }
#endif
    private void OnRectTransformDimensionsChange() { FitFillToFrame(); }

    private void Update()
    {
        if (!barFrame || !pulseGlow || !glowOn) return;

        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float a = Mathf.Lerp(glowAlpha * pulseMin, glowAlpha * pulseMax, t);
        var c = new Color(themeColor.r, themeColor.g, themeColor.b, Mathf.Clamp01(a));
        barFrame.color = c;
    }

    private void ApplyFrameColorImmediate()
    {
        if (!barFrame) return;

        if (!glowOn)
        {
            barFrame.color = frameNormal; // spento
        }
        else
        {
            var c = new Color(themeColor.r, themeColor.g, themeColor.b, glowAlpha);
            barFrame.color = c;           // acceso
        }
    }

    // ---------- Core: inset automatico uguale ai Border della cornice ----------
    private void FitFillToFrame()
    {
        if (!autoFitToFrame || !barFrame || !barFrame.sprite) return;

        // Assicurati che la cornice sia Sliced
        //if (barFrame.type != Image.Type.Sliced) barFrame.type = Image.Type.Sliced;

        // Border del sub-sprite in PIXEL (L, B, R, T) — attenzione: order Unity è x=Left, y=Bottom, z=Right, w=Top
        Vector4 b = barFrame.sprite.border;

        // Conversione pixel -> unità UI (stessa logica di Image.SetNativeSize)
        float ppu = barFrame.sprite.pixelsPerUnit / Mathf.Max(0.0001f, barFrame.pixelsPerUnitMultiplier);

        float left   = b.x / ppu + extraInnerPadding.x;
        float bottom = b.y / ppu + extraInnerPadding.y;
        float right  = b.z / ppu + extraInnerPadding.z;
        float top    = b.w / ppu + extraInnerPadding.w;

        // Inset del Fill dentro la cornice
        /*if (barFill)
        {
            var rt = barFill.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }*/

        // Inset del RangeOverlay (mantiene anchor X gestiti altrove)
        if (rangeOverlay)
        {
            var rt = rangeOverlay;
            // manteniamo gli anchor X come sono, insettiamo solo i margini verticali;
            // per sicurezza applichiamo anche il margine orizzontale minimo.
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
