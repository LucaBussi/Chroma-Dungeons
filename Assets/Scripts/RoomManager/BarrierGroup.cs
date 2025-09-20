using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BarrierGroup : MonoBehaviour
{
    [Header("Segmenti")]
    public List<BarrierSegment2D> segments = new();

    [Header("Comportamento")]
    public bool closeWhenRoomStarts = true;
    public bool openWhenRoomCompletes = true;

    private RoomController room;

    void Awake()
    {
        room = GetComponentInParent<RoomController>();
        if (!room) Debug.LogError("[BarrierGroup] Nessun RoomController nei genitori.");
        if (segments == null || segments.Count == 0)
            segments = new List<BarrierSegment2D>(GetComponentsInChildren<BarrierSegment2D>(true));
    }

    void OnEnable()
    {
        if (room != null)
        {
            room.OnRoomStarted += HandleRoomStarted;
            room.OnRoomCompleted += HandleRoomCompleted;
        }
    }

    void OnDisable()
    {
        if (room != null)
        {
            room.OnRoomStarted -= HandleRoomStarted;
            room.OnRoomCompleted -= HandleRoomCompleted;
        }
    }

    void HandleRoomStarted(RoomController r)   { if (closeWhenRoomStarts) Close(); }
    void HandleRoomCompleted(RoomController r) { if (openWhenRoomCompletes) Open(); }

    public void Close() { foreach (var s in segments) if (s) s.SetClosed(true); }
    public void Open()  { foreach (var s in segments) if (s) s.SetClosed(false); }
}
