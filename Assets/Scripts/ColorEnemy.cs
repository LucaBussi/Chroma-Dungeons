/*using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ColorEnemy : MonoBehaviour, IDefeatable, IColorConfigurable
{
    [Header("State")]
    [SerializeField] private Color currentColor = Color.black;
    [SerializeField] private Color targetColor = Color.clear; // clear = non inizializzato

    [Header("Settings")]
    [SerializeField, Range(0f, 0.5f)] private float similarityThreshold = 0.1f;

    [Header("Refs")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Optional Floor Sampling (fallback)")]
    [Tooltip("Se non impostato dallo SpawnPoint, prova a leggere il colore del pavimento qui.")]
    [SerializeField] private MonoBehaviour floorSamplerBehaviour;
    private IFloorColorSampler floorSampler;

    public event Action OnDefeated;

    void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        floorSampler = floorSamplerBehaviour as IFloorColorSampler;
        if (spriteRenderer) spriteRenderer.color = currentColor;
    }

    void Start()
    {
        // Fallback: se non abbiamo ancora targetColor (alpha==0), campiona dal pavimento
        if (targetColor.a == 0f)
        {
            if (floorSampler == null)
                floorSampler = FindFirstObjectByType<MonoBehaviour>(FindObjectsInactive.Include) as IFloorColorSampler;

            if (floorSampler != null)
                targetColor = floorSampler.SampleAt(transform.position);
            else
                targetColor = Color.red; // fallback visivo
        }

        // Se nessuno ha settato currentColor, assegna un colore random leggibile
        if (currentColor == Color.black) // euristica semplice
            currentColor = UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.6f, 1f);

        if (spriteRenderer) spriteRenderer.color = currentColor;
        CheckForDefeat();
    }

    void Update()
    {
        if (spriteRenderer) spriteRenderer.color = currentColor;
    }

    // ------- IColorConfigurable --------
    public void SetInitialColor(Color c)
    {
        currentColor = c;
        if (spriteRenderer) spriteRenderer.color = currentColor;
        // niente CheckForDefeat qui: lo faremo dopo il SetTargetColor
    }

    public void SetTargetColor(Color c)
    {
        targetColor = c;
        CheckForDefeat();
    }
    // -----------------------------------

    // Ranged
    public void ApplyColor(Color addedColor, float fraction)
    {
        currentColor.r = Mathf.Clamp01(currentColor.r + addedColor.r * fraction);
        currentColor.g = Mathf.Clamp01(currentColor.g + addedColor.g * fraction);
        currentColor.b = Mathf.Clamp01(currentColor.b + addedColor.b * fraction);
        CheckForDefeat();
    }

    // Melee
    public void RemoveColorComponent(Color subtractColor, float amount)
    {
        currentColor.r = Mathf.Clamp01(currentColor.r - subtractColor.r * amount);
        currentColor.g = Mathf.Clamp01(currentColor.g - subtractColor.g * amount);
        currentColor.b = Mathf.Clamp01(currentColor.b - subtractColor.b * amount);
        CheckForDefeat();
    }

    private void CheckForDefeat()
    {
        // targetColor deve essere valido
        if (targetColor.a == 0f) return;

        float diff = Vector3.Distance(
            new Vector3(currentColor.r, currentColor.g, currentColor.b),
            new Vector3(targetColor.r, targetColor.g, targetColor.b));

        if (diff <= similarityThreshold)
        {
            OnDefeated?.Invoke();   // avvisa la Room
            Destroy(gameObject);    // o animazione e poi Destroy
        }
    }
}*/
