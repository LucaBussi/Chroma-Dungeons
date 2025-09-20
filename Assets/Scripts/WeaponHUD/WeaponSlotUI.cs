using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponSlotUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WeaponSwitcher switcher;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text keyLabel;

    [Header("Settings")]
    [Tooltip("Sprite mostrato quando nessuna arma è equipaggiata")]
    [SerializeField] private Sprite emptySprite;

    void Awake()
    {
        if (!switcher) switcher = FindObjectOfType<WeaponSwitcher>();
        if (!itemIcon)
        {
            var icon = transform.Find("ItemIcon");
            if (icon) itemIcon = icon.GetComponent<Image>();
        }
        if (!keyLabel)
        {
            var label = transform.Find("KeyLabel");
            if (label) keyLabel = label.GetComponent<TMP_Text>();
        }
    }

    void Start()
    {
        UpdateUI();
    }

    void Update()
    {
        // aggiorna ogni frame (puoi ottimizzare con un evento dal WeaponSwitcher)
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (!switcher || switcher.weapons.Length == 0)
        {
            if (itemIcon) itemIcon.sprite = emptySprite;
            return;
        }

        int index = switcher.CurrentIndex;
        var current = switcher.weapons[index];
        if (current == null)
        {
            if (itemIcon) itemIcon.sprite = emptySprite;
        }
        else
        {
            // cerca uno SpriteRenderer nell'arma
            var sr = current.GetComponentInChildren<SpriteRenderer>();
            if (sr) itemIcon.sprite = sr.sprite;
            else if (itemIcon) itemIcon.sprite = emptySprite;
        }

        if (keyLabel) keyLabel.text = switcher.switchKey.ToString();
    }
}
