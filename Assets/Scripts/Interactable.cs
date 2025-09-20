using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Dati")]
    public string title = "";                          // opzionale (es. "Cartello", "Terminale")
    [TextArea(2,6)] public string message = "Testo...";// testo che comparirà

    [Header("UX")]
    public string customPrompt = "";                   // opzionale (es. "Leggi", "Apri")
    public bool oneShot = false;                       // se true, dopo la prima volta non interagibile
    public bool requireFacing = false;                 // se vuoi richiedere che il player guardi l’oggetto
    public float requiredDot = 0.6f;                   // quanto “davanti” (0..1)

    [HideInInspector] public bool consumed;

    /// <summary>Opzionale: posizione da “guardare” per essere considerato “fronte”.</summary>
    public Vector3 FocusPoint => transform.position;
}
