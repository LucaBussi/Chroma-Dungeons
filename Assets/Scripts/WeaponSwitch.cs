using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    [Tooltip("Armi figlie del WeaponPivot, in ordine di scorrimento")]
    public Transform[] weapons;

    public int startIndex = 0;
    public KeyCode switchKey = KeyCode.Tab;

    private int current;

    void Awake()
    {
        // Se non assegnato, prendi tutti i figli del pivot
        if (weapons == null || weapons.Length == 0)
        {
            weapons = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                weapons[i] = transform.GetChild(i);
        }
    }

    void Start() => Equip(startIndex);

    void Update()
    {
        if (Input.GetKeyDown(switchKey))
            Next();
    }

    public void Next() => Equip((current + 1) % weapons.Length);

    public void Equip(int index)
    {
        if (weapons == null || weapons.Length == 0) return;
        index = Mathf.Clamp(index, 0, weapons.Length - 1);

        for (int i = 0; i < weapons.Length; i++)
            if (weapons[i]) weapons[i].gameObject.SetActive(i == index);

        current = index;
    }

    public int CurrentIndex => current;
}
