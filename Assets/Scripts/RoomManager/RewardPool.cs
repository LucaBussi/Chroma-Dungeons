using System.Collections.Generic;
using UnityEngine;

public class RewardPool : MonoBehaviour
{
    [Tooltip("Possibili pickup da droppare a fine stanza.")]
    public List<GameObject> pickupPrefabs = new();
    [Tooltip("Quanti oggetti droppare.")]
    public int dropCount = 1;
    [Tooltip("Raggio in cui sparpagliare gli item.")]
    public float scatterRadius = 0.7f;

    public void Drop(Vector3 center)
    {
        if (pickupPrefabs == null || pickupPrefabs.Count == 0) return;

        for (int i = 0; i < dropCount; i++)
        {
            var prefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Count)];
            var pos = center + (Vector3)(Random.insideUnitCircle * scatterRadius);
            Instantiate(prefab, pos, Quaternion.identity);
        }
    }
}
