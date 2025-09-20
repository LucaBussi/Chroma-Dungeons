using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.6f;
    void Start() => Destroy(gameObject, lifeTime);
}
