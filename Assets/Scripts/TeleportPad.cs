using System.Collections;
using UnityEngine;
using Unity.Cinemachine; // CM v3

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class TeleportPad : MonoBehaviour
{
    [Header("Setup")]
    public Transform destination;
    public string playerTag = "Player";

    [Header("Options")]
    public float entryCooldown = 0.75f;
    public Vector2 spawnOffset = new Vector2(0f, 0.25f);

    private bool _isTransitioning;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isTransitioning) return;
        if (!other.CompareTag(playerTag)) return;
        if (!destination) { Debug.LogWarning("[TeleportPad] Nessuna destinazione"); return; }
        StartCoroutine(TeleportCo(other.gameObject));
    }

    private IEnumerator TeleportCo(GameObject player)
    {
        _isTransitioning = true;

        // blocca eventuale scorrimento fisico
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;

        // fade opzionale
        var fader = FindObjectOfType<ScreenFader>();
        if (fader) yield return fader.FadeOut();

        // move
        Vector3 oldPos = player.transform.position;
        Vector3 target = destination.position + (Vector3)spawnOffset;
        player.transform.position = target;

        // --- HARD SNAP CAMERA (CM3) ---
        var vcamBase = FindObjectOfType<CinemachineVirtualCameraBase>();
        if (vcamBase != null)
        {
            vcamBase.OnTargetObjectWarped(player.transform, target - oldPos);
            vcamBase.PreviousStateIsValid = false;

            // forza un CUT per un frame
            vcamBase.enabled = false;
            yield return null;          // 1 frame
            vcamBase.enabled = true;
        }

        // Confiner 2D: invalida cache (v3 o v2 via reflection)
        var conf = FindObjectOfType<CinemachineConfiner2D>();
        if (conf != null)
        {
            var t = conf.GetType();
            (t.GetMethod("InvalidateCache") ?? t.GetMethod("InvalidatePathCache"))?.Invoke(conf, null);
        }
        // --- fine hard snap ---

        if (fader) yield return fader.FadeIn();

        yield return new WaitForSeconds(entryCooldown);
        _isTransitioning = false;
    }
}
