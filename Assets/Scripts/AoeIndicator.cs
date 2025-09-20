using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AoeIndicator : MonoBehaviour
{
    private SpriteRenderer sr;
    private float hideAt = -1f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.enabled = false;
        // Assicurati che sia sotto al boss nella sorting layer, o in una Ground FX layer
    }

    public void Show(Vector2 center, float radius, float duration)
    {
        transform.position = center;
        // Lo scaling dipende dai tuoi PPU: se il cerchio sprite ha diametro "1 unità", scale = r*2
        transform.localScale = Vector3.one * (radius * 2f);
        sr.enabled = true;
        hideAt = Time.time + duration;
    }

    public void Hide()
    {
        sr.enabled = false;
        hideAt = -1f;
    }

    private void Update()
    {
        if (hideAt > 0f && Time.time >= hideAt)
            Hide();
    }
}
