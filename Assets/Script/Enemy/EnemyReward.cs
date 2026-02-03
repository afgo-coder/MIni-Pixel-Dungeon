using UnityEngine;

public class EnemyReward : MonoBehaviour
{
    [Header("EXP")]
    public int expValue = 10; // Enemy1=10, Enemy2=15, Enemy3=20, Enemy4=50

    [Header("Heal Drop")]
    public GameObject healPickupPrefab; // 맥주병
    [Range(0f, 1f)] public float healDropChance = 0.1f;

    Enemy enemy;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        if (enemy != null)
            enemy.OnDied += HandleEnemyDied;
    }

    void OnDestroy()
    {
        if (enemy != null)
            enemy.OnDied -= HandleEnemyDied;
    }

    void HandleEnemyDied(Enemy e)
    {
        // 1) 경험치 즉시 지급
        var player = FindFirstObjectByType<PlayerStats>();
        if (player != null)
            player.AddExp(expValue);

        // 2) 10% 힐 아이템 드랍
        if (healPickupPrefab != null && Random.value < healDropChance)
            Instantiate(healPickupPrefab, transform.position, Quaternion.identity);
    }
}
