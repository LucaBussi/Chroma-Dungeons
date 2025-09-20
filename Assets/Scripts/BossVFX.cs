using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BossVFX : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private SpriteRenderer targetRenderer;       // ⬅️ assegna qui il renderer “visibile” se non è sullo stesso GO
    [SerializeField] private Material outlineCapableMaterial;     // ⬅️ materiale che supporta _Outline* (opzionale, ma consigliato)

    private MaterialPropertyBlock mpb;

    // Shader property IDs
    private static readonly int BlurProp = Shader.PropertyToID("_BlurAmount");
    private static readonly int OutlineEnabledProp   = Shader.PropertyToID("_OutlineEnabled");
    private static readonly int OutlineColorProp     = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThicknessProp = Shader.PropertyToID("_OutlineThickness");

    [Header("Outline Stun Settings")]
    public Color stunOutlineColor = new Color(1f, 0.55f, 0f, 1f);
    [Range(0f, 4f)] public float stunOutlineThickness = 1.5f;
    public bool pulseDuringStun = true;
    [Range(0f, 1f)] public float pulseAmplitude = 0.5f;
    public float pulseSpeed = 3f;

    private bool isStunned = false;
    private float baseThickness;

    void Awake()
    {
        if (!targetRenderer)
            targetRenderer = GetComponent<SpriteRenderer>();

        if (!targetRenderer)
        {
            Debug.LogError("[BossVFX] Nessun SpriteRenderer trovato/assegnato.", this);
            enabled = false;
            return;
        }

        // Verifica materiale e proprietà outline
        EnsureOutlineCapableMaterial();

        mpb = new MaterialPropertyBlock();
        SetBlur(0f);
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(OutlineEnabledProp, 0f);
        targetRenderer.SetPropertyBlock(mpb);
    }

    private void EnsureOutlineCapableMaterial()
    {
        var mat = targetRenderer.sharedMaterial;
        bool hasProps = mat != null &&
                        mat.HasProperty(OutlineEnabledProp) &&
                        mat.HasProperty(OutlineColorProp) &&
                        mat.HasProperty(OutlineThicknessProp);

        if (!hasProps)
        {
            if (outlineCapableMaterial != null)
            {
                // Istanziamo per non toccare il materiale condiviso in progetto
                var inst = Instantiate(outlineCapableMaterial);
                targetRenderer.material = inst;
                Debug.Log("[BossVFX] Materiale non compatibile. Assegnato outlineCapableMaterial istanziato.", targetRenderer);
            }
            else
            {
                Debug.LogWarning("[BossVFX] Il materiale attuale non supporta l'outline (_Outline*). Assegna 'outlineCapableMaterial' o usa il materiale del Boss.", targetRenderer);
            }
        }
    }

    void Update()
    {
        if (!isStunned || !pulseDuringStun) return;

        float t = 1f + Mathf.PingPong(Time.time * pulseSpeed, pulseAmplitude);
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(OutlineThicknessProp, Mathf.Max(0.01f, baseThickness * t));
        targetRenderer.SetPropertyBlock(mpb);
    }

    public void SetBlur(float t01)
    {
        if (!targetRenderer) return;
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(BlurProp, Mathf.Clamp01(t01));
        targetRenderer.SetPropertyBlock(mpb);
    }

    // Uso generico (telegraph)
    public void ShowOutline(Color color, float thickness)
    {
        if (!targetRenderer) return;
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(OutlineEnabledProp, 1f);
        mpb.SetColor(OutlineColorProp, color);
        mpb.SetFloat(OutlineThicknessProp, Mathf.Max(0.01f, thickness));
        targetRenderer.SetPropertyBlock(mpb);
    }

    public void HideOutline(bool force = false)
    {
        // Se siamo in stun e non è una richiesta "forzata", non spegnere l'outline
        if (isStunned && !force) return;

        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(OutlineEnabledProp, 0f);
        targetRenderer.SetPropertyBlock(mpb);
    }

    // Hooks "stun"
    public void OnStunStart()
    {
        isStunned = true;
        baseThickness = Mathf.Max(0.01f, stunOutlineThickness);
        ShowOutline(stunOutlineColor, baseThickness);
    }

    public void OnStunEnd()
    {
        isStunned = false;
        HideOutline(force: true); // qui sì, spegniamo sempre
    }

    public void OnDefeat() => OnStunEnd();
}
