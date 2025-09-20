using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UltimateScrollBridge : MonoBehaviour, IScrollHandler
{
    [Header("Target")]
    public PlayerColorState target;

    [Header("Options")]
    [Tooltip("true: scroll su = avanti; false: scroll giù = avanti")]
    public bool invertScroll = false;
    [Tooltip("Soglia anti-rumore/trackpad")]
    public float threshold = 0.005f;
    [Tooltip("Debounce per evitare doppi trigger da più sorgenti")]
    public float minInterval = 0.05f;
    public bool debugLog = false;

#if ENABLE_INPUT_SYSTEM
    private InputAction scrollYAction; // callback del nuovo Input System
#endif
    private float lastTriggerTime = -999f;
    private float onGuiAccum = 0f;

    void Awake()
    {
#if ENABLE_INPUT_SYSTEM
        scrollYAction = new InputAction("ScrollY", InputActionType.PassThrough, "<Mouse>/scroll/y", expectedControlType: "Axis");
        scrollYAction.performed += ctx =>
        {
            float y = 0f;
            try { y = ctx.ReadValue<float>(); } catch { }
            TryTrigger(y, "InputSystem.Callback");
        };
#endif
    }

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        scrollYAction?.Enable();
#endif
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        scrollYAction?.Disable();
#endif
    }

    void Update()
    {
        // Legacy fallback 1
        float y = Input.mouseScrollDelta.y;
        if (Mathf.Abs(y) > 0f) TryTrigger(y, "Legacy.mouseScrollDelta");

        // Legacy fallback 2
        float ax = Input.GetAxis("Mouse ScrollWheel") * 120f;
        if (Mathf.Abs(ax) > 0f) TryTrigger(ax, "Legacy.Axis(Mouse ScrollWheel)");

        // IMGUI accumulato (gestito in OnGUI)
        if (Mathf.Abs(onGuiAccum) > 0f)
        {
            TryTrigger(onGuiAccum, "IMGUI.OnGUI");
            onGuiAccum = 0f;
        }
    }

    // UI/EventSystem: riceve scroll anche da trackpad e UI
    public void OnScroll(PointerEventData eventData)
    {
        if (eventData == null) return;
        TryTrigger(eventData.scrollDelta.y, "UI.IScrollHandler");
    }

    void OnGUI()
    {
        var e = Event.current;
        if (e != null && e.type == EventType.ScrollWheel)
        {
            // Nota: segno variabile per piattaforma; moltiplico per 10 per normalizzare
            onGuiAccum += -e.delta.y * 10f;
            if (debugLog) Debug.Log($"[UltimateScrollBridge] OnGUI delta={e.delta.y}");
        }
    }

    private void TryTrigger(float rawY, string src)
    {
        if (!target) return;

        if (Mathf.Abs(rawY) < threshold) return;
        if (Time.unscaledTime - lastTriggerTime < minInterval) return; // debounce

        bool forward = invertScroll ? (rawY > 0f) : (rawY < 0f); // default: giù = avanti
        if (forward) target.NextColor(); else target.PreviousColor();

        lastTriggerTime = Time.unscaledTime;
        if (debugLog) Debug.Log($"[UltimateScrollBridge] y={rawY:0.####} src={src}");
    }
}
