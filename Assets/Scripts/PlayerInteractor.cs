using System.Linq;
using UnityEngine;
using TMPro;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Raggio di interazione")]
    [SerializeField, Min(0.1f)] private float interactRange = 1.6f;
    [SerializeField] private LayerMask interactMask; // layer "Interactable"

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("UI refs")]
    [SerializeField] private CanvasGroup hintGroup;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private CanvasGroup dialogGroup;
    [SerializeField] private TextMeshProUGUI dialogTitle;
    [SerializeField] private TextMeshProUGUI dialogBody;

    [Header("Auto-close")]
    [SerializeField] private bool autoCloseOnDistance = true;
    [SerializeField, Min(1f)] private float closeDistanceMultiplier = 1.3f; // isteresi: chiude oltre range*mult

    private Interactable _currentNearby;            // l'Interactable più vicino nel range
    private Interactable _activeDialogSource;       // l'Interactable che ha aperto il dialog
    private bool _dialogOpen;

    void Awake()
    {
        // Stato iniziale “pulito”
        if (hintGroup) { hintGroup.alpha = 0f; hintGroup.interactable = false; hintGroup.blocksRaycasts = false; }
        if (hintText) hintText.text = "";

        if (dialogGroup)
        {
            // svuota eventuali "New Text" nel prefab
            foreach (var t in dialogGroup.GetComponentsInChildren<TextMeshProUGUI>(true)) t.text = "";
            dialogGroup.gameObject.SetActive(false);
            dialogGroup.alpha = 0f;
            dialogGroup.interactable = false;
            dialogGroup.blocksRaycasts = false;
        }

        _dialogOpen = false;
        _activeDialogSource = null;
    }

    void Update()
    {
        // 1) Calcola SEMPRE l'oggetto più vicino (per mostrare il prompt quando il dialog è chiuso)
        _currentNearby = FindClosestInteractableInRange();

        // 2) Se il dialog è aperto, gestisci chiusura con E o distanza
        if (_dialogOpen)
        {
            // E per chiudere
            if (Input.GetKeyDown(interactKey))
            {
                HideDialog();
                _activeDialogSource = null;
                // dopo la chiusura, l'Update prosegue e potrà mostrare l'hint se vicino ad altro
            }
            else if (autoCloseOnDistance)
            {
                bool mustClose = false;

                if (_activeDialogSource == null)
                {
                    mustClose = true; // oggetto distrutto/disattivato
                }
                else
                {
                    float maxDist = interactRange * closeDistanceMultiplier;
                    float sq = (transform.position - _activeDialogSource.transform.position).sqrMagnitude;
                    if (sq > maxDist * maxDist) mustClose = true;
                }

                if (mustClose)
                {
                    HideDialog();
                    _activeDialogSource = null;
                }
            }

            // Finché il dialog è aperto non mostriamo il prompt
            HideHint();
            return;
        }

        // 3) Dialog chiuso: mostra/nascondi prompt e apri dialog con E
        if (_currentNearby)
        {
            ShowHint(_currentNearby.customPrompt, _currentNearby.title);

            if (Input.GetKeyDown(interactKey))
            {
                _activeDialogSource = _currentNearby;
                ShowDialog(_currentNearby.title, _currentNearby.message);
                HideHint();
            }
        }
        else
        {
            HideHint();
        }
    }

    // --- Ricerca oggetto più vicino ---
    private Interactable FindClosestInteractableInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRange, interactMask);
        Interactable best = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            var i = h.GetComponent<Interactable>();
            if (!i || i.consumed) continue;

            float d = (i.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }
        return best;
    }

    // --- UI helpers ---
    void ShowHint(string customPrompt, string title)
    {
        if (!hintGroup || !hintText) return;
        hintText.text = $"Press <b>E</b> to interact";
        hintGroup.alpha = 1f; hintGroup.interactable = false; hintGroup.blocksRaycasts = false;
    }

    void HideHint()
    {
        if (!hintGroup) return;
        hintGroup.alpha = 0f; hintGroup.interactable = false; hintGroup.blocksRaycasts = false;
        if (hintText) hintText.text = "";
    }

    void ShowDialog(string title, string body)
    {
        _dialogOpen = true;

        if (dialogGroup)
        {
            dialogGroup.gameObject.SetActive(true);
            dialogGroup.alpha = 1f;
            dialogGroup.interactable = true;
            dialogGroup.blocksRaycasts = true;
        }
        if (dialogTitle) dialogTitle.text = title ?? "";
        if (dialogBody)  dialogBody.text  = body  ?? "";
    }

    public void HideDialog()
    {
        _dialogOpen = false;

        if (dialogGroup)
        {
            dialogGroup.alpha = 0f;
            dialogGroup.interactable = false;
            dialogGroup.blocksRaycasts = false;
            dialogGroup.gameObject.SetActive(false);
        }
    }

    // Debug raggio
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
