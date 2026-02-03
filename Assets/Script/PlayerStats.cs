using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 100;
    public int currentHP;

    [Header("EXP")]
    public int level = 1;
    public int expPerLevel = 150;
    public int currentExp = 0;

    public event Action OnChanged; // HUDUI가 구독해서 자동 갱신

    void Awake()
    {
        currentHP = maxHP;
        Notify();
    }

    public void TakeDamage(int dmg)
    {
        currentHP = Mathf.Max(0, currentHP - dmg);
        Notify();
        // TODO: 죽음 처리 필요하면 여기
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        Notify();
    }

    public void AddExp(int amount)
    {
        currentExp += amount;

        while (currentExp >= expPerLevel)
        {
            currentExp -= expPerLevel;
            level++;
            // TODO: 레벨업 카드 UI 열기
        }

        Notify();
    }

    public float Hp01 => (maxHP <= 0) ? 0f : (float)currentHP / maxHP;
    public float Exp01 => (expPerLevel <= 0) ? 0f : (float)currentExp / expPerLevel;

    void Notify() => OnChanged?.Invoke();
}