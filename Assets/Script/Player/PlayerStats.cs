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
    public int expPerLevel = 150;
    public int currentExp = 0;

    public event Action OnChanged;
    public event Action<int> OnLevelUp;

    // ✅ 추가: 죽음 이벤트
    public event Action OnDied;

    bool isDead;

    // 업그레이드 누적 횟수 저장
    Dictionary<UpgradeType, int> upgradeCounts = new Dictionary<UpgradeType, int>();

    void Awake()
    {
        currentHP = maxHP;
        Notify();
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;
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

    public void AddExp(int amount)
    {
        if (isDead) return;
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
