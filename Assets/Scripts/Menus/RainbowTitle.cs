using UnityEngine;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class RainbowTitleWarmOscillate : MonoBehaviour
{
    [Header("Animazione")]
    [SerializeField, Range(0f, 10f)] float oscillationSpeed = 2.0f; // velocità avanti-indietro
    [SerializeField, Range(0f, 5f)]  float cyclesAcross = 1.2f;     // quante “onde” sul testo
    [SerializeField] bool useSpatialPhase = true;                    // OFF = tutto il titolo oscilla insieme

    [Header("Colore (HSV)")]
    [SerializeField, Range(0f, 1f)] float hueMin = 0.00f;  // rosso
    [SerializeField, Range(0f, 1f)] float hueMax = 0.12f;  // arancio/rosa
    [SerializeField, Range(0f, 1f)] float saturation = 0.9f;
    [SerializeField, Range(0f, 1f)] float value = 1.0f;

    TMP_Text tmp;
    const float TAU = Mathf.PI * 2f;

    void Awake() { tmp = GetComponent<TMP_Text>(); }
    void OnEnable() { if (!tmp) tmp = GetComponent<TMP_Text>(); tmp.ForceMeshUpdate(); }

    void Update()
    {
        if (!tmp) return;
        tmp.ForceMeshUpdate();
        var ti = tmp.textInfo;
        if (ti == null || ti.characterCount == 0) return;

        // bounds orizzontali
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        for (int i = 0; i < ti.characterCount; i++)
        {
            var ch = ti.characterInfo[i];
            if (!ch.isVisible) continue;
            minX = Mathf.Min(minX, ch.bottomLeft.x, ch.topLeft.x);
            maxX = Mathf.Max(maxX, ch.bottomRight.x, ch.topRight.x);
        }
        if (!float.IsFinite(minX) || !float.IsFinite(maxX) || maxX <= minX) return;

        float time = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;

        for (int i = 0; i < ti.characterCount; i++)
        {
            var ch = ti.characterInfo[i];
            if (!ch.isVisible) continue;

            int mi = ch.materialReferenceIndex;
            int vi = ch.vertexIndex;

            // posizione 0..1 lungo la larghezza del testo
            float cx = (ch.bottomLeft.x + ch.bottomRight.x + ch.topLeft.x + ch.topRight.x) * 0.25f;
            float x01 = Mathf.InverseLerp(minX, maxX, cx);

            // fase: oscilla tra min e max; opzionalmente sfasata nello spazio per mantenere il gradiente
            float phase = oscillationSpeed * time + (useSpatialPhase ? (x01 * TAU * cyclesAcross) : 0f);

            // 0..1 che va avanti e indietro (sin)
            float t = 0.5f * (Mathf.Sin(phase) + 1f);

            float hue = Mathf.Lerp(hueMin, hueMax, t);
            Color32 c = (Color32)Color.HSVToRGB(hue, saturation, value);

            var cols = ti.meshInfo[mi].colors32;
            cols[vi + 0] = c; cols[vi + 1] = c; cols[vi + 2] = c; cols[vi + 3] = c;
        }

        // applica
        for (int m = 0; m < ti.meshInfo.Length; m++)
        {
            var mi = ti.meshInfo[m];
            mi.mesh.colors32 = mi.colors32;
            tmp.UpdateGeometry(mi.mesh, m);
        }
    }
}
