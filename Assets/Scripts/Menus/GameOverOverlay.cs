using UnityEngine;
using UnityEngine.UI;

public class GameOverOverlay : MonoBehaviour
{
    [Header("Optional autowire (per nome)")]
    public Button retryButton;
    public Button returnButton;

    void Awake()
    {
        if (!retryButton)  retryButton  = GameObject.Find("Retry")?.GetComponent<Button>();
        if (!returnButton) returnButton = GameObject.Find("ReturnToMainTitle")?.GetComponent<Button>();

        if (retryButton)  retryButton.onClick.AddListener(GameOver.Retry);
        if (returnButton) returnButton.onClick.AddListener(GameOver.ReturnToTitle);
    }
}
