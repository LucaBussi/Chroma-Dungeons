using System.Collections.Generic;
using UnityEngine;

public class HeartsUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform heartsParent;   // HeartsPanel
    [SerializeField] private HeartUI heartPrefab;

    [Header("Settings")]
    [SerializeField] private int heartsCount = 5;      // numero minimo di cuori
    [SerializeField] private float hpPerHeart = 20f;   // 20 HP per cuore

    private readonly List<HeartUI> hearts = new();
    private float cachedMaxHp;
    private float cachedHp;

    // Call once when PlayerHealth is ready
    public void Init(float currentHp, float maxHp)
    {
        cachedHp = currentHp;
        cachedMaxHp = maxHp;

        int neededHearts = Mathf.CeilToInt(maxHp / Mathf.Max(1f, hpPerHeart));
        heartsCount = Mathf.Max(heartsCount, neededHearts);

        BuildHearts();
        Refresh();
    }

    public void OnHealthChanged(float currentHp, float maxHp)
    {
        cachedHp = currentHp;
        cachedMaxHp = maxHp;
        Refresh();
    }

    private void BuildHearts()
    {
        for (int i = heartsParent.childCount - 1; i >= 0; i--)
            Destroy(heartsParent.GetChild(i).gameObject);
        hearts.Clear();

        for (int i = 0; i < heartsCount; i++)
        {
            var h = Instantiate(heartPrefab, heartsParent);
            hearts.Add(h);
        }
    }

    private void Refresh()
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            float heartMin = i * hpPerHeart;
            float hpInThisHeart = Mathf.Clamp(cachedHp - heartMin, 0f, hpPerHeart);
            float fill = hpPerHeart > 0f ? (hpInThisHeart / hpPerHeart) : 0f;
            hearts[i].SetFill(fill);
        }
    }
}
