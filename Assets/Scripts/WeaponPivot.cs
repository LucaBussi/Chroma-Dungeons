using UnityEngine;

[DisallowMultipleComponent]
public class WeaponPivot : MonoBehaviour
{
    [Header("Aim")]
    [SerializeField] private Transform target;                // se nullo, cerca il Player (Tag "Player")
    [SerializeField] private bool flipSpriteWithAim = true;
    [SerializeField] private SpriteRenderer bodyToFlip;       // opzionale: flip X del corpo

    [Header("Color Tint (solo sprite della melee)")]
    [SerializeField] private PlayerColorState colorState;     // se nullo, lo trova nel parent
    [SerializeField] private SpriteRenderer[] tintTargets;    // se vuoto, prende tutti gli SpriteRenderer figli
    [SerializeField] private bool onlyPrimaryChannel = true;  // true = usa solo R o G o B “puro”
    [SerializeField] private bool forceTintEveryFrame = false;// true = reimposta il colore in LateUpdate (per battere l’Animator)

    [Header("Hit Origin (opzionale, per MeleeAttack)")]
    [SerializeField] private bool autoCreateHitOrigin = true; // crea automaticamente un figlio "HitOrigin"
    [SerializeField] private float forwardOffset = 0.8f;      // distanza in avanti sul +X locale
    [SerializeField] private Transform hitOrigin;             // verrà creato se nullo e autoCreateHitOrigin = true

    private Color _currentTint = Color.white;

    public Transform HitOrigin => hitOrigin; // comodo da leggere da altri script

    void Start()
    {
        // Target di mira
        if (!target)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) target = player.transform;
        }

        // Stato colore
        if (!colorState) colorState = GetComponentInParent<PlayerColorState>();
        _currentTint = GetSelectedColor();

        // Lista dei renderer da tingere
        if (tintTargets == null || tintTargets.Length == 0)
            tintTargets = GetComponentsInChildren<SpriteRenderer>(true);

        ApplyTint(_currentTint);

        // HitOrigin: child in avanti
        if (!hitOrigin && autoCreateHitOrigin)
        {
            var go = new GameObject("HitOrigin");
            hitOrigin = go.transform;
            hitOrigin.SetParent(transform, false);
            hitOrigin.localPosition = Vector3.right * forwardOffset;
        }

        // Ascolta i cambi di colore
        if (colorState) colorState.OnColorChanged += OnColorChanged;
    }

    void OnDestroy()
    {
        if (colorState) colorState.OnColorChanged -= OnColorChanged;
    }

    void LateUpdate()
    {
        // Rotazione verso il bersaglio
        if (target)
        {
            Vector2 dir = (target.position - transform.position);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            if (flipSpriteWithAim && bodyToFlip)
                bodyToFlip.flipX = angle > 90f || angle < -90f;
        }

        // (opzionale) reimposta la tinta ogni frame per sovrascrivere eventuali curve dell'Animator
        if (forceTintEveryFrame)
        {
            var c = GetSelectedColor();
            if (c != _currentTint)
            {
                _currentTint = c;
            }
            ApplyTint(_currentTint);
        }
    }

    private void OnColorChanged(Color _)
    {
        _currentTint = GetSelectedColor();
        ApplyTint(_currentTint);
    }

    private Color GetSelectedColor()
    {
        var c = colorState ? colorState.SelectedColor : Color.white;
        return onlyPrimaryChannel ? DominantPrimary(c) : c;
    }

    private void ApplyTint(Color c)
    {
        if (tintTargets == null) return;
        for (int i = 0; i < tintTargets.Length; i++)
        {
            if (tintTargets[i]) tintTargets[i].color = c;
        }
    }

    // Converte il colore scelto nel canale primario dominante (R o G o B puro)
    private static Color DominantPrimary(Color c)
    {
        if (c.r >= c.g && c.r >= c.b) return Color.red;
        if (c.g >= c.r && c.g >= c.b) return Color.green;
        return Color.blue;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (hitOrigin)
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            Gizmos.DrawSphere(hitOrigin.position, 0.05f);
        }
    }
#endif
}
