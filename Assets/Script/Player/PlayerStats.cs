using System;
using UnityEngine;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 100;
    public int currentHP;

    [Header("EXP")]
    public int level = 1;

    [Tooltip("레벨업에 필요한 경험치(초기값)")]
    public int expPerLevel = 120;

    [Tooltip("레벨업마다 필요 경험치 증가 배율 (예: 1.05 = 5% 증가)")]
    public float expGrowthRate = 1.05f;

    public int currentExp = 0;

    public event Action OnChanged;
    public event Action<int> OnLevelUp;

    public event Action OnDied;

    bool isDead;
    public bool IsDead => isDead;
    public bool IsGameEnded { get; private set; }

    Dictionary<UpgradeType, int> upgradeCounts = new Dictionary<UpgradeType, int>();

    void Awake()
    {
        currentHP = maxHP;
        Notify();
    }

    public void MarkGameEnded()
    {
        IsGameEnded = true;
        var dmg = FindFirstObjectByType<PlayerDamageReceiver>();
        if (dmg != null) dmg.enabled = false;
    }
    public void ForceNotify()
    {
        Notify();
    }
    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        if (IsGameEnded) return;
        if (dmg <= 0) return;

        currentHP = Mathf.Max(0, currentHP - dmg);
        Notify();

        if (currentHP <= 0)
        {
            isDead = true;
            OnDied?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        Notify();
    }
    public void AddMaxHP(int amount, bool alsoHeal = true)
    {
        if (isDead) return;
        if (amount == 0) return;

        maxHP = Mathf.Max(1, maxHP + amount);

        if (alsoHeal)
            currentHP += amount;

        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        Notify(); // ✅ HUD 즉시 갱신 트리거
    }
    public void AddExp(int amount)
    {
        if (isDead) return;
        if (IsGameEnded) return;
        if (amount <= 0) return;

        currentExp += amount;

        while (currentExp >= expPerLevel)
        {
            currentExp -= expPerLevel;
            LevelUp();
        }

        Notify();
    }

    void LevelUp()
    {
        level++;

        // ✅ 레벨업마다 필요 경험치 5% 증가
        expPerLevel = Mathf.Max(1, Mathf.CeilToInt(expPerLevel * expGrowthRate));

        OnLevelUp?.Invoke(level);
        OnChanged?.Invoke();
    }

    public int GetUpgradeCount(UpgradeType type)
        => upgradeCounts.TryGetValue(type, out var c) ? c : 0;

    public void RegisterUpgrade(UpgradeType type)
    {
        if (!upgradeCounts.ContainsKey(type))
            upgradeCounts[type] = 0;
        upgradeCounts[type]++;
    }

    public float Hp01 => (maxHP <= 0) ? 0f : (float)currentHP / maxHP;
    public float Exp01 => (expPerLevel <= 0) ? 0f : (float)currentExp / expPerLevel;

    void Notify() => OnChanged?.Invoke();
}