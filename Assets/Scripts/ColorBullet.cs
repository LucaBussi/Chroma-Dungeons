using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ColorBullet : MonoBehaviour
{
    [Header("Color Logic")]
    public Color effectColor = Color.red;    // Sovrascritto dal tuo shooter
    [Range(0f,1f)] public float addFraction = 0.25f;
    public float lifeTime = 3f;

    [Header("VFX Overlay")]
    [SerializeField] private SpriteRenderer effectRenderer; // child con Animator in loop
    [SerializeField] private int orderOffset = 1;           // effetto sopra al bullet

    private SpriteRenderer bulletRenderer;

    void Awake()
    {
        bulletRenderer = GetComponent<SpriteRenderer>();

        // Auto-wire se non impostato da Inspector
        if (!effectRenderer)
            effectRenderer = GetComponentInChildren<SpriteRenderer>(includeInactive: true);
    }

    void Start()
    {
        if (lifeTime > 0f) Destroy(gameObject, lifeTime);

        // Allinea sorting e colore dell’effetto
        if (effectRenderer)
        {
            effectRenderer.sortingLayerID = bulletRenderer.sortingLayerID;
            effectRenderer.sortingOrder   = bulletRenderer.sortingOrder + orderOffset;

            // Colora l’effetto (assicurati che i frame dell’effetto siano bianchi)
            effectRenderer.color = effectColor;

            // Se vuoi che l’effetto NON ruoti con il proiettile, sblocca questa riga:
            // effectRenderer.transform.localRotation = Quaternion.identity;
        }
    }

    // Se più avanti cambi il colore in volo, chiama questo
    public void SetEffectColor(Color c)
    {
        effectColor = c;
        if (effectRenderer) effectRenderer.color = c;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        /*var enemy = other.GetComponent<ColorEnemy>();
        if (enemy != null)
        {
            enemy.ApplyColor(effectColor, 0.2f);
            Destroy(gameObject);
            return;
        }*/

        var dummy = other.GetComponent<DummyEnemy>();
        if (dummy != null)
        {
            dummy.SetColor(effectColor, 0.2f);
        }

        Destroy(gameObject);
    }
}
