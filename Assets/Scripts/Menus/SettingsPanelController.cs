using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    public Toggle fullscreenToggle;
    public Slider musicSlider;
    public Toggle sfxToggle;
    public Toggle colorblindToggle;
    public Button closeButton;

    private void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        // Placeholder: non facciamo nulla di “vero”, ma se vuoi puoi salvare PlayerPrefs qui.
        if (fullscreenToggle) fullscreenToggle.isOn = Screen.fullScreen;
    }
}
