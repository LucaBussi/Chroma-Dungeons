using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameOver
{
    static readonly List<GameObject> DisabledRoots = new();
    static Scene frozenScene;
    const string GameOverSceneName = "GameOverMenu";
    const string MainMenuSceneName = "MainMenuScene";

    public static bool IsOpen { get; private set; }

    public static void Open()
    {
        if (IsOpen) return;

        // ricorda scena attiva e spegnila tutta
        frozenScene = SceneManager.GetActiveScene();
        DisabledRoots.Clear();
        foreach (var root in frozenScene.GetRootGameObjects())
        {
            DisabledRoots.Add(root);
            root.SetActive(false);
        }

        // congela tempo/audio + cursore libero/visibile
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // carica la pagina Game Over
        SceneManager.LoadScene(GameOverSceneName, LoadSceneMode.Additive);
        IsOpen = true;
    }

    public static void Retry()
    {
        if (!IsOpen) return;

        // sblocca tempo/audio prima di cambiare scena
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // ricarica la scena di gameplay che avevamo congelato
        SceneManager.LoadScene(frozenScene.buildIndex, LoadSceneMode.Single);
        DisabledRoots.Clear();
        IsOpen = false;
    }

    public static void ReturnToTitle()
    {
        if (!IsOpen) return;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        SceneManager.LoadScene(MainMenuSceneName, LoadSceneMode.Single);
        DisabledRoots.Clear();
        IsOpen = false;
    }
}
