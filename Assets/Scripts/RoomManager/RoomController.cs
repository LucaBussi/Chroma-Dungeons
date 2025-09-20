using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum RoomMode { Finite, Infinite }
public enum RoomState { Idle, Active, Completed }

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class RoomController : MonoBehaviour
{
    [Header("Tilemaps (solo riferimenti)")]
    public Tilemap groundTilemap; // qui camminano player e nemici
    public Tilemap wallTilemap;   // future collisioni porte
    public Tilemap fluidTilemap;  // opzionale

    [Header("Spawn")]
    public List<RoomSpawnPoint> spawnPoints = new();
    public RoomMode mode = RoomMode.Finite;

    [Tooltip("Finite: nemici totali della stanza. Infinite: nemici per ondata.")]
    public int totalEnemies = 6;

    [Tooltip("Limite contemporanei vivi (Finite e Infinite).")]
    public int concurrentMax = 6;

    [Tooltip("Tempo tra uno spawn e l'altro.")]
    public float spawnInterval = 0.5f;

    [Header("Infinite mode - ondate")]
    [Tooltip("Ritardo tra la fine di un’ondata e l’inizio della successiva (solo Infinite).")]
    public float interWaveDelay = 1.0f;

    [Header("Ricompense")]
    public RewardPool rewardPool;
    public Transform rewardDropCenter; // se nullo, usa il centro del RoomController

    [Header("Debug")]
    public bool logDebug = false;

    public RoomState State { get; private set; } = RoomState.Idle;

    // runtime
    private readonly HashSet<RoomEnemyAdapter> _alive = new();
    private int _spawnedTotal;       // Finite: totale stanza | Infinite: totale dall’inizio (solo debug)
    private int _spawnedThisWave;    // Infinite: spawn per ondata corrente
    private int _waveIndex;          // Infinite: contatore ondate (0-based)
    private bool _playerInside;
    private Collider2D _trigger;

    // Eventi per porte/FX futuri
    public event System.Action<RoomController> OnRoomStarted;
    public event System.Action<RoomController> OnRoomCompleted;

    // (Opzionale) evento fine ondata in Infinite
    public event System.Action<RoomController, int> OnWaveCompleted;

    void Awake()
    {
        _trigger = GetComponent<Collider2D>();
        _trigger.isTrigger = true;
        if (!groundTilemap) Debug.LogWarning("[RoomController] GroundTilemap non assegnata.");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_playerInside) return;
        if (!other.CompareTag("Player")) return;

        _playerInside = true;
        if (State == RoomState.Idle) Activate();
    }

    public void Activate()
    {
        if (State != RoomState.Idle) return;
        State = RoomState.Active;
        _spawnedTotal = 0;
        _spawnedThisWave = 0;
        _waveIndex = 0;
        _alive.Clear();

        if (logDebug) Debug.Log($"[Room] START {name} ({mode})");

        CloseDoors(); // placeholder
        OnRoomStarted?.Invoke(this);

        StopAllCoroutines();
        if (mode == RoomMode.Finite) StartCoroutine(SpawnFinite());
        else StartCoroutine(SpawnInfinite());
    }

    IEnumerator SpawnFinite()
    {
        while (State == RoomState.Active && _spawnedTotal < totalEnemies)
        {
            if (_alive.Count >= concurrentMax)
            {
                yield return null;
                continue;
            }

            var sp = PickRandomSpawnPoint();
            if (sp != null)
            {
                var adapter = sp.Spawn(this, groundTilemap);
                if (adapter)
                {
                    RegisterEnemy(adapter);
                    _spawnedTotal++;
                    if (logDebug) Debug.Log($"[Room] Spawned {_spawnedTotal}/{totalEnemies}");
                }
            }

            yield return new WaitForSeconds(spawnInterval);
        }

        // attendo che muoiano tutti
        while (State == RoomState.Active && _alive.Count > 0)
            yield return null;

        if (State == RoomState.Active)
            CompleteRoom();
    }

    IEnumerator SpawnInfinite()
    {
        while (State == RoomState.Active)
        {
            // Se non abbiamo ancora finito di spawnare l’ondata corrente…
            if (_spawnedThisWave < totalEnemies && _alive.Count < concurrentMax)
            {
                var sp = PickRandomSpawnPoint();
                if (sp != null)
                {
                    var adapter = sp.Spawn(this, groundTilemap);
                    if (adapter)
                    {
                        RegisterEnemy(adapter);
                        _spawnedThisWave++;
                        _spawnedTotal++;
                        if (logDebug) Debug.Log($"[Room] Wave {_waveIndex} spawned {_spawnedThisWave}/{totalEnemies} (alive={_alive.Count})");
                    }
                }

                yield return new WaitForSeconds(spawnInterval);
                continue;
            }

            // Se abbiamo completato lo spawn dell’ondata e non c’è più nessuno vivo ⇒ fine ondata
            if (_spawnedThisWave >= totalEnemies && _alive.Count == 0)
            {
                if (logDebug) Debug.Log($"[Room] Wave {_waveIndex} COMPLETED in {name} — dropping reward.");
                DropReward();
                OnWaveCompleted?.Invoke(this, _waveIndex);

                // prepara prossima ondata
                _waveIndex++;
                _spawnedThisWave = 0;

                // attesa tra ondate
                if (interWaveDelay > 0f)
                    yield return new WaitForSeconds(interWaveDelay);

                continue;
            }

            // Altrimenti aspetta un frame (o poco) e ricontrolla
            yield return null;
        }
    }

    RoomSpawnPoint PickRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Count == 0) return null;
        int idx = Random.Range(0, spawnPoints.Count);
        return spawnPoints[idx];
    }

    void RegisterEnemy(RoomEnemyAdapter adapter)
    {
        if (!_alive.Add(adapter)) return;
        adapter.BindRoom(this);
    }

    // RoomController.cs (aggiunte)
    private bool _shuttingDown;

    private void OnDisable() { _shuttingDown = true; }
    private void OnDestroy() { _shuttingDown = true; }

    internal void NotifyEnemyDefeated(RoomEnemyAdapter adapter)
    {
        if (_shuttingDown || !this) return;
        _alive.Remove(adapter);
        if (logDebug) Debug.Log($"[Room] Enemy defeated. Alive={_alive.Count}");

        if (mode == RoomMode.Finite && _spawnedTotal >= totalEnemies && _alive.Count == 0)
            CompleteRoom();
    }

    void CompleteRoom()
    {
        if (_shuttingDown || !this) return;
        if (State != RoomState.Active) return;

        State = RoomState.Completed;
        if (logDebug) Debug.Log($"[Room] COMPLETED {name}");

        DropReward();
        OpenDoors();
        OnRoomCompleted?.Invoke(this);

        StopAllCoroutines();
    }


    void DropReward()
    {
        if (!rewardPool) return;
        var center = rewardDropCenter ? rewardDropCenter.position : transform.position;
        rewardPool.Drop(center);
    }

    void CloseDoors() { /* aggancia qui i tuoi DoorController in futuro */ }
    void OpenDoors() { /* idem */ }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = State == RoomState.Completed ? Color.green : Color.cyan;
        Gizmos.DrawWireCube(GetBoundsCenter(), new Vector3(4, 4, 0)); // segnaposto
    }
    Vector3 GetBoundsCenter() => rewardDropCenter ? rewardDropCenter.position : transform.position;
#endif

    public void OnGamePaused()
    {
        // niente di obbligatorio qui per ora
    }

    public void OnGameResumed()
    {
        if (State != RoomState.Active) return;

        if (mode == RoomMode.Finite)
        {
            if (_spawnedTotal >= totalEnemies && _alive.Count == 0)
                CompleteRoom();
        }
        else // Infinite
        {
            // Se l’ondata corrente è “spawnata” e non c’è più nessuno vivo, chiudi l’ondata
            if (_spawnedThisWave >= totalEnemies && _alive.Count == 0)
            {
                if (logDebug) Debug.Log($"[Room] Wave {_waveIndex} COMPLETED (resume) — dropping reward.");
                DropReward();
                OnWaveCompleted?.Invoke(this, _waveIndex);
                _waveIndex++;
                _spawnedThisWave = 0;
            }
        }
    }


}
