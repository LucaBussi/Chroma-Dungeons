using System;
using UnityEngine;

public class ThrowableSlot : MonoBehaviour
{
    [Header("Refs (assegnali in Inspector)")]
    [SerializeField] private Transform firePoint;            // WeaponPivot
    [SerializeField] private KeyCode throwKey = KeyCode.Q;   // tasto

    [Header("Runtime")]
    [SerializeField] private GameObject currentThrowablePrefab; // null = vuoto

    [Header("Debug")]
    [SerializeField] private bool logDebug = true;

    public event Action<Sprite, bool> OnSlotChanged; // (icon, hasItem)
    public KeyCode ThrowKey => throwKey;
    public bool HasItem => currentThrowablePrefab != null;

    void Awake()
    {
        if (!firePoint)
        {
            // fallback: prova a trovarlo
            var pivot = transform.Find("WeaponPivot");
            if (pivot) firePoint = pivot;
        }
        if (!firePoint && logDebug) Debug.LogWarning("[ThrowableSlot] FirePoint non assegnato.");
    }

    void Update()
    {
        if (currentThrowablePrefab && Input.GetKeyDown(throwKey))
            ThrowCurrent();
    }

    public bool TryStore(GameObject throwablePrefab)
    {
        if (currentThrowablePrefab)
        {
            if (logDebug) Debug.Log("[ThrowableSlot] Slot pieno, ignoro pickup.");
            return false;
        }

        currentThrowablePrefab = throwablePrefab;
        var icon = GetIconFromPrefab(throwablePrefab);

        if (logDebug) Debug.Log($"[ThrowableSlot] Pickup OK: {throwablePrefab.name}. Icona: {(icon ? icon.name : "null")}");
        OnSlotChanged?.Invoke(icon, true);
        return true;
    }

    private void ThrowCurrent()
    {
        if (!firePoint || !currentThrowablePrefab) return;

        var go  = Instantiate(currentThrowablePrefab, firePoint.position, firePoint.rotation);
        var dir = (Vector2)firePoint.right;

        var proj = go.GetComponent<IThrowable>();
        if (proj != null) proj.Launch(dir, transform);

        if (logDebug) Debug.Log($"[ThrowableSlot] Lanciato: {currentThrowablePrefab.name}. Slot svuotato.");
        currentThrowablePrefab = null;
        OnSlotChanged?.Invoke(null, false);
    }

    private Sprite GetIconFromPrefab(GameObject prefab)
    {
        if (!prefab) return null;

        var iconProvider = prefab.GetComponent<IThrowableIcon>();
        if (iconProvider != null) return iconProvider.GetIcon();

        var sr = prefab.GetComponentInChildren<SpriteRenderer>();
        return sr ? sr.sprite : null;
    }
}

public interface IThrowableIcon { Sprite GetIcon(); }
