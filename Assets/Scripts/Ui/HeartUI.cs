using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HeartUI : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform container;   // HeartsPanel
    [SerializeField] private Image heartPrefab;     // HeartPrefab (Image)

    [Header("Sprites")]
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite halfHeart;      // optional
    [SerializeField] private Sprite emptyHeart;

    [Header("Colors")]
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField] private Color bonusColor = Color.darkBlue;

    [Header("Heart Settings")]
    [SerializeField] private int hpPerHeart = 20;

    private readonly List<Image> hearts = new();

    private PlayerHealth playerHealth;

    private int lastTotalHearts = -1;
    private int lastBaseHearts = -1;
    private int lastHP = -1;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindPlayer();
        ForceRefresh();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        // lightweight auto-update (you can replace with events later)
        if (playerHealth == null) return;

        int hp = GetCurrentHP();
        if (hp != lastHP)
        {
            lastHP = hp;
            UpdateHearts(hp, hpPerHeart);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindPlayer();
        ForceRefresh();
    }

    private void BindPlayer()
    {
        // Find even if disabled (true)
        playerHealth = FindObjectOfType<PlayerHealth>(true);

        if (playerHealth == null)
            Debug.LogWarning("HeartUI: PlayerHealth not found in this scene.");
    }

    private void ForceRefresh()
    {
        if (playerHealth == null) return;

        int totalHearts = GetTotalHearts();
        int baseHearts = GetBaseHearts();

        lastTotalHearts = totalHearts;
        lastBaseHearts = baseHearts;

        BuildHearts(totalHearts, baseHearts);

        lastHP = GetCurrentHP();
        UpdateHearts(lastHP, hpPerHeart);
    }

    private int GetCurrentHP()
    {
        // typical: playerHealth.currentHP
        return playerHealth.currentHP;
    }

    private int GetBaseHearts()
{
    return Mathf.CeilToInt(playerHealth.BaseMaxHP / (float)hpPerHeart);
}


    private int GetTotalHearts()
{
    return Mathf.CeilToInt((playerHealth.BaseMaxHP + playerHealth.BonusHP) / (float)hpPerHeart);
}

    // =================== YOUR ORIGINAL METHODS ===================

    public void BuildHearts(int totalHearts, int baseHearts)
    {
        for (int i = 0; i < hearts.Count; i++)
            if (hearts[i] != null) Destroy(hearts[i].gameObject);

        hearts.Clear();

        for (int i = 0; i < totalHearts; i++)
        {
            Image h = Instantiate(heartPrefab, container);
            h.color = (i < baseHearts) ? baseColor : bonusColor;
            hearts.Add(h);
        }
    }
    
    

    public void UpdateHearts(int currentHP, int hpPerHeart)
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            int heartHPStart = i * hpPerHeart;
            int hpIntoThisHeart = currentHP - heartHPStart;

            if (hpIntoThisHeart >= hpPerHeart)
                hearts[i].sprite = fullHeart;
            else if (halfHeart != null && hpIntoThisHeart >= hpPerHeart / 2)
                hearts[i].sprite = halfHeart;
            else if (hpIntoThisHeart > 0 && halfHeart == null)
                hearts[i].sprite = fullHeart;
            else
                hearts[i].sprite = emptyHeart;
        }
    }
    
}
