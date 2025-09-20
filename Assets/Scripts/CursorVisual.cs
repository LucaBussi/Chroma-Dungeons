using UnityEngine;
using UnityEngine.UI;

public class CursorVisual : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform cursorRect; // l'Image del cursore
    [SerializeField] private Image cursorImage;

    [Header("Behavior")]
    [SerializeField] private float hoverScale = 1.2f;     // quando c'è un target
    [SerializeField] private float normalScale = 1.0f;
    [SerializeField] private float scaleLerp = 12f;       // morbidezza
    [SerializeField] private bool hideSystemCursor = true;

    Vector3 _targetScale;

    void Reset()
    {
        cursorRect = GetComponent<RectTransform>();
        cursorImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        _targetScale = Vector3.one * normalScale;
        if (hideSystemCursor) Cursor.visible = false;
    }

    void OnDisable()
    {
        if (hideSystemCursor) Cursor.visible = true;
    }

    void LateUpdate()
    {
        // Segui il mouse in overlay
        Vector2 mp = Input.mousePosition;
        cursorRect.position = mp;

        // Interpola scala per feedback
        cursorRect.localScale = Vector3.Lerp(cursorRect.localScale, _targetScale, Time.unscaledDeltaTime * scaleLerp);
    }

    public void SetColor(Color c)
    {
        if (cursorImage) cursorImage.color = c;
    }

    public void SetTargeting(bool hasTarget)
    {
        _targetScale = Vector3.one * (hasTarget ? hoverScale : normalScale);
    }
}
