using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class ThrowableSlotUI : MonoBehaviour
{
    [Header("Refs (assegnali in Inspector)")]
    [SerializeField] private ThrowableSlot slot;  // TRASCINA QUI lo slot del Player
    [SerializeField] private Image panelBg;
    [SerializeField] private Image itemIcon;
    [SerializeField] private Outline iconOutline;
    [SerializeField] private TMP_Text keyLabel;       // o TMP se preferisci

    [Header("Style")]
    [SerializeField, Range(0,1)] private float bgAlpha = 0.35f;
    [SerializeField] private Color bgColor = new Color(0,0,0,1);
    [SerializeField] private Color outlineColor = new Color(1f, 0.55f, 0f, 1f);
    [SerializeField] private Vector2 iconPadding = new Vector2(6,6);

    void Awake()
    {
        if (!slot) Debug.LogWarning("[ThrowableSlotUI] Slot non assegnato. Trascina il Player -> ThrowableSlot.");
        if (panelBg)
        {
            var c = bgColor; c.a = bgAlpha; panelBg.color = c;
        }
        if (iconOutline) iconOutline.effectColor = outlineColor;

        ApplyState(null, false);
    }

    void OnEnable()
    {
        if (slot != null) slot.OnSlotChanged += ApplyStateFromSlot;
    }
    void OnDisable()
    {
        if (slot != null) slot.OnSlotChanged -= ApplyStateFromSlot;
    }

    private void ApplyStateFromSlot(Sprite icon, bool hasItem)
    {
        ApplyState(icon, hasItem);
        if (keyLabel) keyLabel.text = slot ? slot.ThrowKey.ToString() : "Q";
    }

    private void ApplyState(Sprite icon, bool hasItem)
    {
        if (itemIcon)
        {
            itemIcon.sprite = icon;
            itemIcon.enabled = hasItem && icon != null;
            if (iconOutline) iconOutline.enabled = itemIcon.enabled;

            var rt = itemIcon.rectTransform;
            //rt.offsetMin = new Vector2(iconPadding.x, iconPadding.y);
            //rt.offsetMax = new Vector2(-iconPadding.x, -iconPadding.y);
        }
        if (keyLabel) keyLabel.gameObject.SetActive(hasItem);
    }
}
