using UnityEngine;
using UnityEngine.Rendering; // SortingGroup

[ExecuteAlways]
[DisallowMultipleComponent]
public class YSortOrder : MonoBehaviour
{
    [Header("Target point for sorting (optional)")]
    [Tooltip("Se assegnato, userà la Y di questo transform (es. un Empty sui 'piedi').")]
    public Transform feetPivot;

    [Header("Sorting")]
    [Tooltip("Sorting Layer da usare. Lascia vuoto per non modificarlo.")]
    public string sortingLayerName = "YSort";
    [Tooltip("Base per mantenere positivo l'ordine (più grande = più margine).")]
    public int sortingOrderBase = 5000;
    [Tooltip("Precisione: 100 = 1 unità mondo -> 100 livelli di ordine.")]
    public int precision = 100;
    [Tooltip("Offset per risolvere pareggi o differenze di pivot tra mob.")]
    public int offset = 0;

    SortingGroup sg;
    SpriteRenderer sr;
    Transform tf;

    void Awake()
    {
        Cache();
        ApplyLayerIfNeeded();
    }

    void OnEnable()
    {
        Cache();
        ApplyLayerIfNeeded();
        UpdateOrder();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        Cache();
        ApplyLayerIfNeeded();
        UpdateOrder();
    }
#endif

    void Update()
    {
        // In Edit Mode aggiorna anche senza Play
        if (!Application.isPlaying)
            UpdateOrder();
    }

    void LateUpdate()
    {
        if (Application.isPlaying)
            UpdateOrder();
    }

    void Cache()
    {
        if (!tf) tf = transform;
        if (!sg) sg = GetComponent<SortingGroup>();
        if (!sr) sr = GetComponent<SpriteRenderer>();
    }

    void ApplyLayerIfNeeded()
    {
        if (string.IsNullOrEmpty(sortingLayerName)) return;

        if (sg) sg.sortingLayerName = sortingLayerName;
        if (sr) sr.sortingLayerName = sortingLayerName;

        // Se hai ParticleSystem/TrailRenderer come figli, mettili manualmente su YSort
        // (il SortingGroup non cambia il loro sorting layer, solo l'ordine totale).
    }

    void UpdateOrder()
    {
        float y = feetPivot ? feetPivot.position.y : tf.position.y;
        int order = sortingOrderBase - Mathf.RoundToInt(y * precision) + offset;

        if (sg) sg.sortingOrder = order;
        else if (sr) sr.sortingOrder = order;
    }
}
