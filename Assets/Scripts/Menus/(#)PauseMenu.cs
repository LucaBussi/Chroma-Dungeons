using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;   // <-- se usi il New Input System

public class PauseMenu : MonoBehaviour
{
    [Header("Refs UI")]
    public GameObject pausePanel;
    public GameObject settingsPanel;

    [Header("Options")]
    public KeyCode pauseKey = KeyCode.Escape;
    public bool lockCursorDuringPlay = true;  // se lo metti a false, il cursore sarà sempre libero

    [Header("Disattiva questi componenti in pausa")]
    public List<Behaviour> disableOnPause = new();  // es: PlayerMovement, WeaponPivot, EnemySpawners...

    [Header("Input System (opzionale ma consigliato)")]
    public PlayerInput playerInput;                 // il PlayerInput del player
    public string gameplayActionMap = "Gameplay";   // cambia col tuo nome
    public string uiActionMap = "UI";               // cambia col tuo nome
    public List<InputActionReference> disableTheseActions = new(); // azioni specifiche (melee, shoot)

    public static bool IsPaused { get; private set; }

    void Start()
    {
        SetPaused(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        ApplyCursor(true);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame
            || Input.GetKeyDown(pauseKey)) // fallback vecchio input
        {
            if (!IsPaused) OpenPause();
            else ClosePause();
        }

        // 👇 forza ogni frame lo stato del cursore (vince contro altri script “testardi”)
        EnforceCursor();
    }

    void LateUpdate()
    {
        // ulteriore assicurazione nel frame
        EnforceCursor();
    }

    public void OpenPause()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        if (pausePanel)    pausePanel.SetActive(true);
        SetPaused(true);
    }

    public void ClosePause()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        if (pausePanel)    pausePanel.SetActive(false);
        SetPaused(false);
    }

    public void OpenSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(true);
    }

    public void QuitToTitle()
    {
        SetPaused(false);
        SceneManager.LoadScene("MainMenu");
    }

    void SetPaused(bool pause)
    {
        IsPaused = pause;

        // tempo & audio
        Time.timeScale = pause ? 0f : 1f;
        AudioListener.pause = pause;

        // abilita/disabilita componenti gameplay
        foreach (var b in disableOnPause) if (b) b.enabled = !pause;

        // 🔒 New Input System: switch di Action Map (o disabilita singole azioni)
        if (playerInput)
        {
            if (pause)
                playerInput.SwitchCurrentActionMap(uiActionMap);
            else
                playerInput.SwitchCurrentActionMap(gameplayActionMap);
        }
        foreach (var ar in disableTheseActions)
        {
            if (ar && ar.action != null)
            {
                if (pause) ar.action.Disable();
                else       ar.action.Enable();
            }
        }

        ApplyCursor();
        // piccola “spinta” per 0.25s dopo il cambio, in caso altri script rilockino
        StopAllCoroutines();
        StartCoroutine(NudgeCursorForAWhile());
    }

    void ApplyCursor(bool force = false)
    {
        if (IsPaused || !lockCursorDuringPlay)
        {
            if (force || Cursor.lockState != CursorLockMode.None)  Cursor.lockState = CursorLockMode.None;
            if (force || !Cursor.visible)                           Cursor.visible   = true;
        }
        else
        {
            if (force || Cursor.lockState != CursorLockMode.Locked) Cursor.lockState = CursorLockMode.Locked;
            if (force ||  Cursor.visible)                           Cursor.visible   = false;
        }
    }

    void EnforceCursor()
    {
        // applica lo stato desiderato se qualcuno lo cambia
        if (IsPaused && Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        if (!IsPaused && lockCursorDuringPlay && Cursor.lockState != CursorLockMode.Locked) Cursor.lockState = CursorLockMode.Locked;
    }

    IEnumerator NudgeCursorForAWhile()
    {
        float t = 0f;
        while (t < 0.25f)
        {
            EnforceCursor();
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) ApplyCursor(true);
    }
}
