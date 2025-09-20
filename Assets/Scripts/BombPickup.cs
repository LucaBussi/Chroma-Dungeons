using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BombPickup : MonoBehaviour
{
    [SerializeField] private GameObject bombProjectilePrefab;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool logDebug = true;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        var slot = other.GetComponentInChildren<ThrowableSlot>() ?? other.GetComponent<ThrowableSlot>();
        if (!slot)
        {
            if (logDebug) Debug.LogWarning("[BombPickup] Player trovato, ma nessun ThrowableSlot.");
            return;
        }

        if (slot.TryStore(bombProjectilePrefab))
        {
            if (logDebug) Debug.Log("[BombPickup] Raccolta bomba OK. Distruggo il pickup.");
            Destroy(gameObject);
        }
        else
        {
            if (logDebug) Debug.Log("[BombPickup] Slot pieno. Nessuna raccolta.");
        }
    }
}
