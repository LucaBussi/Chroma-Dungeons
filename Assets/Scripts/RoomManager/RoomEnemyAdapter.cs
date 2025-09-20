// RoomEnemyAdapter.cs (versione safe)
using UnityEngine;

[DisallowMultipleComponent]
public class RoomEnemyAdapter : MonoBehaviour, IAbsorptionReceiver
{
    private RoomController _room;
    private bool _alreadyNotified;

    public void BindRoom(RoomController room) => _room = room;

    // Morte per assorbimento (evento "reale")
    public void OnAbsorptionThresholdReached(float match01)
    {
        if (_alreadyNotified) return;
        _alreadyNotified = true;

        if (_room && _room.isActiveAndEnabled)
            _room.NotifyEnemyDefeated(this);

        Destroy(gameObject);
    }

    // NIENTE Notify in OnDestroy: OnDestroy scatta anche su cambio scena
    void OnDestroy() { /* vuoto di proposito */ }
}
