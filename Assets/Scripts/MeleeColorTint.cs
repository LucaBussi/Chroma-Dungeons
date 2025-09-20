using UnityEngine;

public class MeleeColorTint : MonoBehaviour
{
    [SerializeField] private PlayerColorState colorState; 
    [SerializeField] private SpriteRenderer[] targets; 
    [SerializeField] private bool forceEveryFrame = false;   // metti true se l’animazione continua a sovrascrivere il colore
    [SerializeField] private bool onlyPrimaryChannel = true; // true = usa solo R o G o B “puro”

    private Color _current;

    void Awake()
    {
        if (!colorState) colorState = GetComponentInParent<PlayerColorState>();
        if (targets == null || targets.Length == 0)
            targets = GetComponentsInChildren<SpriteRenderer>(true); // prende tutti gli sprite sotto WeaponPivot
    }

    void OnEnable()
    {
        _current = GetSelected();
        Apply(_current);

        if (colorState != null)
            colorState.OnColorChanged += OnColorChanged;
    }

    void OnDisable()
    {
        if (colorState != null)
            colorState.OnColorChanged -= OnColorChanged;
    }

    void LateUpdate()
    {
        if (!forceEveryFrame) return;

        var c = GetSelected();
        if (c != _current)
        {
            _current = c;
            Apply(_current);
        }
        else
        {
            // anche se non cambia, riapplica per “battere” l’Animator
            Apply(_current);
        }
    }

    private void OnColorChanged(Color c)
    {
        _current = GetSelected(); // rileggi in caso tu cambi il PlayerColorState fuori dall’Update
        Apply(_current);
    }

    private Color GetSelected()
    {
        var c = colorState ? colorState.SelectedColor : Color.white;
        return onlyPrimaryChannel ? DominantPrimary(c) : c;
    }

    private void Apply(Color c)
    {
        if (targets == null) return;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i]) targets[i].color = c;
        }
    }

    // Usa solo il canale dominante: R o G o B “puro”
    private static Color DominantPrimary(Color c)
    {
        if (c.r >= c.g && c.r >= c.b) return Color.red;
        if (c.g >= c.r && c.g >= c.b) return Color.green;
        return Color.blue;
    }
}
