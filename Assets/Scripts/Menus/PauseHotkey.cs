using UnityEngine;

public class PauseHotkey : MonoBehaviour
{
    public KeyCode key = KeyCode.Escape;

    void Update()
    {
        if (Input.GetKeyDown(key))
            Pause.Toggle();
    }
}
