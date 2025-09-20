using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public GameObject panel;
    public Button retryButton;
    public Button returnButton;

    private void Awake()
    {
        if (panel) panel.SetActive(false);
        if (retryButton)  retryButton.onClick.AddListener(Retry);
        if (returnButton) returnButton.onClick.AddListener(ReturnToTitle);
    }

    public void Show()
    {
        if (panel) panel.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void Retry()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene("Gameplay"); // riavvia il tutorial (la scena)
    }

    void ReturnToTitle()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene("MainMenu");
    }
}
