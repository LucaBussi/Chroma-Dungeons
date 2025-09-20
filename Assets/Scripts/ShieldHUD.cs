using UnityEngine;

public class ShieldHUD : MonoBehaviour
{
    [SerializeField] private PlayerHealth player;
    [SerializeField] private ShieldUI shieldUI;
    [SerializeField] private bool hideEmptySlot = false; // true = cornice visibile solo quando c'è scudo

    void OnEnable()
    {
        if (!player) player = FindObjectOfType<PlayerHealth>();
        if (!shieldUI) shieldUI = GetComponentInChildren<ShieldUI>(true);

        if (player)
        {
            player.OnShieldChanged += HandleShieldChanged;
            // sync iniziale
            HandleShieldChanged(player.ShieldCurrent, player.ShieldMax);
        }
    }

    void OnDisable()
    {
        if (player)
            player.OnShieldChanged -= HandleShieldChanged;
    }

    void HandleShieldChanged(float current, float maximum)
    {
        if (!shieldUI) return;
        float t = (maximum > 0f) ? (current / maximum) : 0f;
        shieldUI.SetFill(t);

        if (hideEmptySlot)
            shieldUI.SetSlotVisible(t > 0f);
    }
}
