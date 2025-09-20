using UnityEngine;

[DisallowMultipleComponent]
public class BarrierSegment2D : MonoBehaviour
{
    [Header("Auto-wire")]
    [SerializeField] private Collider2D[] colliders;
    [SerializeField] private SpriteRenderer[] visuals;

    [Header("Aspetto")]
    [Tooltip("Se true, i visuals vengono mostrati quando la barriera è CHIUSA.")]
    public bool visibleWhenClosed = false;

    void Awake() { AutoWire(); }
#if UNITY_EDITOR
    void OnValidate() { if (!Application.isPlaying) AutoWire(); }
#endif

    void AutoWire()
    {
        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider2D>(true);
        if (visuals == null || visuals.Length == 0)
            visuals = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void SetClosed(bool closed)
    {
        if (colliders != null)
            foreach (var c in colliders) if (c) c.enabled = closed;

        if (visuals != null)
            foreach (var v in visuals) if (v) v.enabled = visibleWhenClosed && closed;
    }
}
