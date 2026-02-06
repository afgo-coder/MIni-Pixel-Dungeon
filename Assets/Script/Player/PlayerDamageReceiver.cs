using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerDamageReceiver : MonoBehaviour
{
    public PlayerStats stats;
    public SpriteRenderer spriter;

    [Header("I-Frame")]
    public float invincibleTime = 0.4f;  // 무적 시간
    public float tickInterval = 0.3f;    // 붙어있을 때 연속 피격 간격
    public float blinkInterval = 0.08f;  // 깜빡임 속도

    float invTimer;
    float tickTimer;
    float blinkTimer;
    bool blinkState;

    void Awake()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (spriter == null) spriter = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (invTimer > 0f)
        {
            invTimer -= Time.deltaTime;
            HandleBlink();
        }
        else
        {
            if (spriter != null) spriter.color = Color.white;
        }

        if (tickTimer > 0f) tickTimer -= Time.deltaTime;
    }

    void HandleBlink()
    {
        if (spriter == null) return;

        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0f)
        {
            blinkTimer = blinkInterval;
            blinkState = !blinkState;
            spriter.color = blinkState ? Color.clear : Color.white;
        }
    }

    void OnCollisionStay2D(Collision2D other)
    {
        TryTakeContactDamage(other.collider);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryTakeContactDamage(other);
    }

    void TryTakeContactDamage(Collider2D other)
    {
        if (stats == null) return;
        if (!other.CompareTag("Enemy")) return;

        if (invTimer > 0f) return;
        if (tickTimer > 0f) return;

        // 핵심: Enemy가 가진 데미지 값을 가져온다
        int dmg = 1;
        var enemyDmg = other.GetComponent<EnemyContactDamage>();
        if (enemyDmg == null)
        {
            // EnemyContactDamage가 "Enemy 본체"에 있고, Collider가 자식일 수도 있어서 부모까지 탐색
            enemyDmg = other.GetComponentInParent<EnemyContactDamage>();
        }
        if (enemyDmg != null) dmg = enemyDmg.contactDamage;

        stats.TakeDamage(dmg);

        invTimer = invincibleTime;
        tickTimer = tickInterval;
        blinkTimer = 0f;
        blinkState = false;
    }
}

