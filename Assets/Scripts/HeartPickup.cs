using UnityEngine;

[DisallowMultipleComponent]
public class HeartPickup : MonoBehaviour
{
    [Header("Cura")]
    [Tooltip("HP restituiti al pickup (100 HP = vita piena).")]
    [SerializeField] private float healAmount = 20f; // un cuore = 20 HP

    [Tooltip("Se false, non raccoglie se il player è già full HP.")]
    [SerializeField] private bool allowPickupAtFullHealth = false;

    [Header("Feedback (opzionali)")]
    [SerializeField] private AudioClip sfxOnPickup;
    [SerializeField] private ParticleSystem vfxOnPickup;
    [SerializeField] private float destroyDelay = 0.0f;

    bool _consumed;

    void Reset()
    {
        // Se aggiungi lo script all'oggetto, prova a configurare il collider come trigger.
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed) return;

        // Recupera PlayerHealth dal collider entrato nel trigger (o da suoi parent)
        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (!ph) return;

        if (!ph.IsAlive) return;

        // Se non vogliamo raccogliere a full HP, esci se già al massimo
        if (!allowPickupAtFullHealth && Mathf.Approximately(ph.CurrentHealth, ph.MaxHealth))
            return;

        // Esegui la cura (clippata al Max all'interno di Heal)
        float before = ph.CurrentHealth;
        ph.Heal(healAmount);

        // Se non ha curato nulla (es. già full e allowPickupAtFullHealth=true), puoi decidere se consumare lo stesso
        // Qui consumiamo comunque se allowPickupAtFullHealth è true
        if (Mathf.Approximately(before, ph.CurrentHealth) && !allowPickupAtFullHealth)
            return;

        // Evita doppi trigger
        _consumed = true;

        // Feedback
        if (sfxOnPickup)
            AudioSource.PlayClipAtPoint(sfxOnPickup, transform.position);

        if (vfxOnPickup)
            Instantiate(vfxOnPickup, transform.position, Quaternion.identity);

        // Distruggi (subito o con un piccolo delay)
        Destroy(gameObject, destroyDelay);
    }
}
