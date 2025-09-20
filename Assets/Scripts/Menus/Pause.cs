using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;



public static class Pause
{

    // --- in cima alla classe Pause ---
    struct RB2State { public Rigidbody2D rb; public Vector2 vel; public float angVel; public bool wasSim; }
    struct RB3State { public Rigidbody rb; public Vector3 vel; public Vector3 angVel; public bool wasKinematic; }

    static readonly List<RB2State> _rb2 = new();
    static readonly List<RB3State> _rb3 = new();

    static readonly List<GameObject> DisabledRoots = new();
    static Scene frozenScene;
    public static bool IsOpen { get; private set; }

    const string PauseSceneName = "PauseMenu";
    const string MainMenuSceneName = "MainMenuScene";
    public const string DontPauseTag = "DontPause"; // usa questo tag per oggetti che NON vuoi spegnere

    public static void Toggle()
    {
        if (IsOpen) Resume();
        else Open();
    }

    public static void Open()
    {
        if (IsOpen) return;

        SnapshotRigidbodies();

        frozenScene = SceneManager.GetActiveScene();

        // ⛔️ RIMOSSO: niente DisabledRoots / SetActive(false) sulle root
        DisabledRoots.Clear();

        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(PauseSceneName, LoadSceneMode.Additive);
        IsOpen = true;

        // Notifica opzionale agli oggetti interessati
        NotifyPauseAware(paused: true);
    }


    public static void Resume()
    {
        if (!IsOpen) return;

        SceneManager.UnloadSceneAsync(PauseSceneName);

        // ⛔️ RIMOSSO: niente riattivo root
        DisabledRoots.Clear();

        Time.timeScale = 1f;
        AudioListener.pause = false;
        RestoreRigidbodies();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false; // o true, come preferisci

        IsOpen = false;

        // Notifica opzionale agli oggetti interessati
        NotifyPauseAware(paused: false);
    }


    public static void QuitToTitle()
    {
        // Sblocca tutto e torna al titolo (non serve riaccendere i root: si cambia scena)
        DisabledRoots.Clear();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(MainMenuSceneName, LoadSceneMode.Single);
        IsOpen = false;
    }

    // Facoltativo: usa un pannello Settings nella scena Pause
    public static void OpenSettings()
    {
        // qui non serve nulla a livello globale:
        // nella scena Pause fai SetActive(true) del tuo pannello Settings
    }

    static void SnapshotRigidbodies()
    {
        _rb2.Clear(); _rb3.Clear();

        // 2D
        var all2D = Object.FindObjectsByType<Rigidbody2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var r in all2D)
            _rb2.Add(new RB2State
            {
                rb = r,
                vel = r.linearVelocity,
                angVel = r.angularVelocity,
                wasSim = r.simulated
            });

        // 3D (se non usi 3D, non farà nulla)
        var all3D = Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var r in all3D)
            _rb3.Add(new RB3State
            {
                rb = r,
                vel = r.linearVelocity,
                angVel = r.angularVelocity, // <-- è Vector3 in 3D
                wasKinematic = r.isKinematic
            });
    }

    static void RestoreRigidbodies()
    {
        // 2D
        foreach (var s in _rb2)
        {
            if (!s.rb) continue;
            s.rb.simulated = s.wasSim;
            s.rb.linearVelocity = s.vel;
            s.rb.angularVelocity = s.angVel;
            s.rb.WakeUp();
        }
        _rb2.Clear();

        // 3D
        foreach (var s in _rb3)
        {
            if (!s.rb) continue;
            s.rb.isKinematic = s.wasKinematic;
            s.rb.linearVelocity = s.vel;
            s.rb.angularVelocity = s.angVel;  // Vector3
            s.rb.WakeUp();
        }
        _rb3.Clear();
    }

    public interface IPauseAware
    {
        void OnGamePaused();
        void OnGameResumed();
    }

    static void NotifyPauseAware(bool paused)
    {
        var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var mb in all)
        {
            if (mb is IPauseAware pa)
            {
                if (paused) pa.OnGamePaused();
                else pa.OnGameResumed();
            }
        }
    }
}
