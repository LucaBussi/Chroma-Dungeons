// ColorRoomInterfaces.cs
using System;
using UnityEngine;

public interface IDefeatable
{
    event Action OnDefeated; // chiamalo quando il nemico viene assorbito
}

public interface IColorConfigurable
{
    void SetInitialColor(Color c); // chiamata dallo SpawnPoint allo spawn
    void SetTargetColor(Color c);  // colore del pavimento sulla cella di spawn
}

public interface IFloorColorSampler
{
    Color SampleAt(Vector2 worldPos); // ritorna il colore del pavimento a worldPos
}

// Comodo per proiettili/melee
public interface IColorAffectable
{
    void ApplyColor(Color color, float fraction);
    void RemoveColorComponent(Color subtract, float amount);
}
