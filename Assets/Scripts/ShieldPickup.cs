using UnityEngine;

[DisallowMultipleComponent]
public class ShieldHeartPickup : MonoBehaviour
{
    [Header("Feedback (opzionali)")]
    [SerializeField] private AudioClip sfxOnPickup;
    [SerializeField] private ParticleSystem vfxOnPickup;
    [SerializeField] private float destroyDelay = 0.0f;

    bool _consumed;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed) return;

        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (!ph || !ph.IsAlive) return;

        // Prova a dare lo scudo (slot unico)
        if (!ph.TryGiveShield())
        {
            // Slot già occupato → NON raccogliere
            return;
        }

        _consumed = true;

        if (sfxOnPickup)
            AudioSource.PlayClipAtPoint(sfxOnPickup, transform.position);

        if (vfxOnPickup)
            Instantiate(vfxOnPickup, transform.position, Quaternion.identity);

        Destroy(gameObject, destroyDelay);
    }
}
