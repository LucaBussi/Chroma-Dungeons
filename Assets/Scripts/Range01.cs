using UnityEngine;

[System.Serializable]
public struct Range01
{
    public float min;
    public float max;

    public Range01(float min, float max)
    {
        // ordina e clampa in [0..1]
        if (min > max) { var tmp = min; min = max; max = tmp; }
        this.min = Mathf.Clamp01(min);
        this.max = Mathf.Clamp01(max);
    }

    public bool Contains(float v) => v >= min && v <= max;
    public float Clamp(float v) => Mathf.Clamp(v, min, max);
    public float Width => Mathf.Max(0f, max - min);

    public override string ToString() => $"[{min:0.###}, {max:0.###}]";
}
