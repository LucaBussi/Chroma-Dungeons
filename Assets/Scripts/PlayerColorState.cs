using System;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
#endif

public class PlayerColorState : MonoBehaviour
{
    [Header("Colori iniziali disponibili")]
    [SerializeField] private List<Color> availableColors = new() { Color.red, Color.green, Color.blue };

    [Header("Input")]
    [Tooltip("Soglia anti-rumore/trackpad (più bassa = più sensibile)")]
    [SerializeField] private float scrollThreshold = 0.005f;
    [Tooltip("Inverti il verso (true = scroll su -> avanti)")]
    [SerializeField] private bool invertScroll = false;
    [Tooltip("Mostra i dettagli della sorgente d’input")]
    [SerializeField] private bool debugScroll = false;

    private int currentIndex = -1; // -1 = nessuna selezione
    private Color selectedColor = Color.clear;

#if ENABLE_INPUT_SYSTEM
    // InputAction dedicata allo scroll (nuovo Input System)
    private InputAction scrollYAction;
#endif

    // Accumulatore dalla fallback IMGUI (OnGUI)
    private float onGuiScrollAccum = 0f;

    public event Action<Color> OnColorChanged;
    public event Action<IReadOnlyList<Color>> OnColorsChanged;

    public Color SelectedColor => selectedColor;
    public IReadOnlyList<Color> AvailableColors => availableColors;

    void Awake()
    {
#if ENABLE_INPUT_SYSTEM
        // Crea l'action a runtime per evitare dipendenze da asset .inputactions
        // Binding esplicito all’asse Y dello scroll del mouse.
        scrollYAction = new InputAction(name: "ScrollY", type: InputActionType.Value, expectedControlType: "Axis");
        scrollYAction.AddBinding("<Mouse>/scroll/y");
        // Alcune versioni/device espongono solo il vettore intero: aggiungo anche il binding al 2D e leggo Y.
        scrollYAction.AddBinding("<Mouse>/scroll").WithProcessor("scaleVector2(x=0,y=1)");
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

    void Start()
    {
        OnColorsChanged?.Invoke(availableColors);
        if (availableColors.Count > 0) SelectColor(0);
    }

    void Update()
    {
        // Tasti numerici (come prima)
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectColor(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectColor(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectColor(2);

        int dir = ReadScrollDirection(); // -1 = giù, +1 = su, 0 = nessuno
        if (dir != 0)
        {
            bool forward = invertScroll ? (dir > 0) : (dir < 0); // default: giù = avanti
            if (forward) NextColor(); else PreviousColor();
        }
    }

    void OnGUI()
    {
        // Fallback IMGUI: spesso arriva anche quando gli altri non lo fanno
        Event e = Event.current;
        if (e != null && e.type == EventType.ScrollWheel)
        {
            // In IMGUI, delta.y è tipicamente negativo scroll su e positivo scroll giù (varia).
            onGuiScrollAccum += -e.delta.y * 10f;
            if (debugScroll) Debug.Log($"[OnGUI] delta={e.delta.y}");
        }
    }

    // Ritorna -1 (scroll giù), +1 (scroll su), 0 (nessuno)
    private int ReadScrollDirection()
    {
        float y = 0f;
        string src = "";

#if ENABLE_INPUT_SYSTEM
        if (scrollYAction != null)
        {
            // Provo prima come float diretto
            float v = 0f;
            try { v = scrollYAction.ReadValue<float>(); } catch { /* ignora se non è asse */ }
            if (Mathf.Abs(v) > Mathf.Abs(y)) { y = v; src = "InputSystem.Action<float>"; }

            // Se non ha dato nulla, provo a leggere come Vector2 e prendo Y
            if (Mathf.Approximately(y, 0f))
            {
                try
                {
                    Vector2 vv = scrollYAction.ReadValue<Vector2>();
                    if (Mathf.Abs(vv.y) > Mathf.Abs(y)) { y = vv.y; src = "InputSystem.Action<Vector2>.y"; }
                }
                catch { /* non è un Vector2: ok */ }
            }

            // Come ulteriore fallback, leggo direttamente dal device Mouse
            if (Mathf.Approximately(y, 0f) && Mouse.current != null)
            {
                float mv = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(mv) > Mathf.Abs(y)) { y = mv; src = "InputSystem.Mouse.scroll"; }
            }
        }
#endif

        // Legacy: mouseScrollDelta
        if (Mathf.Approximately(y, 0f))
        {
            float v = Input.mouseScrollDelta.y;
            if (Mathf.Abs(v) > Mathf.Abs(y)) { y = v; src = "Legacy.mouseScrollDelta"; }
        }

        // Legacy: asse "Mouse ScrollWheel"
        if (Mathf.Approximately(y, 0f))
        {
            float v = Input.GetAxis("Mouse ScrollWheel") * 120f; // normalizzo
            if (Mathf.Abs(v) > Mathf.Abs(y)) { y = v; src = "Legacy.Axis(Mouse ScrollWheel)"; }
        }

        // IMGUI fallback
        if (Mathf.Approximately(y, 0f) && Mathf.Abs(onGuiScrollAccum) > 0f)
        {
            y = onGuiScrollAccum;
            src = "IMGUI.OnGUI";
            onGuiScrollAccum = 0f;
        }

        if (debugScroll) Debug.Log($"[PlayerColorState] scrollY={y:0.0000} src={src}");

        if (y > scrollThreshold) return +1;
        if (y < -scrollThreshold) return -1;
        return 0;
    }

    public void SelectColor(int index)
    {
        if (index < 0 || index >= availableColors.Count) return;
        if (currentIndex == index) return;

        currentIndex = index;
        selectedColor = availableColors[index];
        OnColorChanged?.Invoke(selectedColor);
    }

    public void NextColor()
    {
        if (availableColors.Count == 0) return;
        int next = (currentIndex < 0) ? 0 : (currentIndex + 1) % availableColors.Count;
        SelectColor(next);
    }

    public void PreviousColor()
    {
        if (availableColors.Count == 0) return;
        int prev = (currentIndex < 0) ? 0 : (currentIndex - 1 + availableColors.Count) % availableColors.Count;
        SelectColor(prev);
    }

    public void AddColor(Color newColor)
    {
        if (!availableColors.Contains(newColor))
        {
            availableColors.Add(newColor);
            OnColorsChanged?.Invoke(availableColors);
        }
    }

    public void RemoveColor(Color colorToRemove)
    {
        int idx = availableColors.IndexOf(colorToRemove);
        if (idx >= 0)
        {
            availableColors.RemoveAt(idx);

            if (idx == currentIndex)
            {
                if (availableColors.Count > 0)
                {
                    currentIndex = Mathf.Clamp(idx, 0, availableColors.Count - 1);
                    SelectColor(currentIndex);
                }
                else
                {
                    currentIndex = -1;
                    selectedColor = Color.clear;
                    OnColorChanged?.Invoke(selectedColor);
                }
            }
            else if (idx < currentIndex)
            {
                currentIndex = Mathf.Max(0, currentIndex - 1);
            }

            OnColorsChanged?.Invoke(availableColors);
        }
    }
}
