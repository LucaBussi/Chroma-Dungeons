using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    public Button tutorialButton;
    public Button newGameButton;  // sarà disattivato
    public Button settingsButton;

    [Header("Panels")]
    public GameObject settingsPanel; // lo useremo in più posti

    private void Awake()
    {
        if (tutorialButton)  tutorialButton.onClick.AddListener(StartTutorial);
        if (settingsButton)  settingsButton.onClick.AddListener(ToggleSettings);
        if (newGameButton)   newGameButton.interactable = false; // “Coming soon”
        if (settingsPanel)   settingsPanel.SetActive(false);
    }

    private void StartTutorial()
    {
        // Carica la scena di gioco (quella dove oggi testavi tutto)
        SceneManager.LoadScene("Gameplay");
    }

    private void ToggleSettings()
    {
        if (!settingsPanel) return;
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
}
