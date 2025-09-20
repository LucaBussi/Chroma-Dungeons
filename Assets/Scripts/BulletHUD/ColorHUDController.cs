using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorHUDController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerColorState playerColorState;
    [SerializeField] private RectTransform barContainer; // contenitore verticale a sinistra
    [SerializeField] private ColorBarSlot barPrefab;

    [Header("Layout")]
    [SerializeField] private float spacing = 10f; // se non usi un VerticalLayoutGroup

    private readonly List<ColorBarSlot> slots = new();

    void Awake()
    {
        if (!playerColorState)
            playerColorState = FindObjectOfType<PlayerColorState>();
    }

    void OnEnable()
    {
        if (playerColorState != null)
        {
            playerColorState.OnColorsChanged += Rebuild;
            playerColorState.OnColorChanged += HighlightByColor;
        }
    }

    void OnDisable()
    {
        if (playerColorState != null)
        {
            playerColorState.OnColorsChanged -= Rebuild;
            playerColorState.OnColorChanged -= HighlightByColor;
        }
    }

    void Start()
    {
        if (playerColorState != null)
        {
            Rebuild(playerColorState.AvailableColors);
            HighlightByColor(playerColorState.SelectedColor);
        }
    }

    void Rebuild(IReadOnlyList<Color> colors)
    {
        // Pulisci
        foreach (var s in slots)
            if (s) Destroy(s.gameObject);
        slots.Clear();

        if (barContainer == null || barPrefab == null || colors == null) return;

        // Se usi un VerticalLayoutGroup sul container, lo spacing è gestito da quello.
        bool usesLayoutGroup = barContainer.GetComponent<VerticalLayoutGroup>() != null;

        for (int i = 0; i < colors.Count; i++)
        {
            var slot = Instantiate(barPrefab, barContainer);
            slot.Setup(colors[i], GetKeyLabelForIndex(i)); // "1", "2", "3", ...

            if (!usesLayoutGroup && i > 0)
            {
                var rt = slot.GetComponent<RectTransform>();
                if (rt)
                {
                    var pos = rt.anchoredPosition;
                    pos.y -= i * spacing; // semplice offset se non usi layout
                    rt.anchoredPosition = pos;
                }
            }

            slots.Add(slot);
        }
    }

    void HighlightByColor(Color selected)
    {
        // Color è un struct: il confronto per valore va bene
        foreach (var slot in slots)
        {
            bool hl = slot != null && slot.SlotColor == selected;
            if (slot) slot.SetHighlighted(hl);
        }
    }

    string GetKeyLabelForIndex(int i)
    {
        // 0→"1", 1→"2", 2→"3", ...
        return (i + 1).ToString();
    }
}
