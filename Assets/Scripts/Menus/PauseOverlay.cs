using UnityEngine;
using UnityEngine.UI;

public class PauseOverlay : MonoBehaviour
{
    [Header("Opzionali (se li lasci null, li trova per nome)")]
    public Button resumeButton;
    public Button settingsButton;
    public Button quitButton;
    public GameObject settingsPanel; // se hai un pannello Settings nella scena Pause

    void Awake()
    {
        // Autowire per nome se non assegnati
        if (!resumeButton)   resumeButton   = GameObject.Find("Resume")?.GetComponent<Button>();
        if (!settingsButton) settingsButton = GameObject.Find("Settings")?.GetComponent<Button>();
        if (!quitButton)     quitButton     = GameObject.Find("Quit")?.GetComponent<Button>();

        if (resumeButton)   resumeButton.onClick.AddListener(OnResume);
        if (settingsButton) settingsButton.onClick.AddListener(OnOpenSettings);
        if (quitButton)     quitButton.onClick.AddListener(OnQuit);

        if (settingsPanel) settingsPanel.SetActive(false);
    }

    public void OnResume()       => Pause.Resume();
    public void OnOpenSettings() { if (settingsPanel) settingsPanel.SetActive(true); }
    public void OnQuit()         => Pause.QuitToTitle();
}
