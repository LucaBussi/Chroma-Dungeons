using UnityEngine;

public class CursorFromColorStateBinder : MonoBehaviour
{
    [SerializeField] private PlayerColorState colorState;  // se lasci vuoto, prova a trovarlo
    [SerializeField] private CursorVisual cursor;

    void Reset()
    {
        cursor = GetComponent<CursorVisual>();
        if (!colorState) colorState = FindObjectOfType<PlayerColorState>();
    }

    void Awake()
    {
        if (!cursor) cursor = GetComponent<CursorVisual>();
        if (!colorState) colorState = GetComponentInParent<PlayerColorState>();
        if (!colorState) colorState = FindObjectOfType<PlayerColorState>();
    }

    void OnEnable()
    {
        if (colorState != null)
        {
            colorState.OnColorChanged += HandleColorChanged;
            // colore iniziale
            cursor?.SetColor(colorState.SelectedColor);
        }
    }

    void OnDisable()
    {
        if (colorState != null)
            colorState.OnColorChanged -= HandleColorChanged;
    }

    void HandleColorChanged(Color c)
    {
        cursor?.SetColor(c);
    }
}
