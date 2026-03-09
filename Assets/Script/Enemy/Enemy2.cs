using UnityEngine;

public class Enemy2 : MonoBehaviour, IDamageable
{
    public float moveSpeed = 2.5f;
    public float stopDistance = 0.2f;
    public int maxHP = 5;
    public int currentHP;
    int baseMaxHP;

    public float hitStopTime = 0.12f;
    public Animator ani;
    public SpriteRenderer spriter;
    public Transform target;

    [Header("World HP Bar")]
    public WorldHPBar hpBarPrefab;
    public Vector3 hpBarOffset = new Vector3(0f, 0.5f, 0f);
    public float hpBarVisibleTime = 2f;
    WorldHPBar hpBarInstance;

    [Header("HP Scaling")]
    public int levelStep = 5;
    public float stepMultiplier = 1.05f;

    // ✅ EXP (EnemyReward 안 거치고 Enemy2에서 직접 지급)
    [Header("Reward")]
    public int expValue = 15;   // 인스펙터에서 조절 가능 (스크린샷 값에 맞춰 기본 15)
    PlayerStats playerStats;

    public bool isDead;
    bool isHit;
    Rigidbody2D rb;
    RigidbodyConstraints2D originalConstraints;

    // 유지해도 되지만, 이제 EXP는 Enemy2가 직접 줌
    public System.Action<Enemy2> OnDied;

    void Start()
    {
        baseMaxHP = maxHP;

        // ✅ 플레이어 캐싱(Find를 매번 하지 않게)
        playerStats = FindFirstObjectByType<PlayerStats>();

        int playerLevel = 1;
        if (playerStats != null) playerLevel = playerStats.level;

        int tier = Mathf.Max(0, playerLevel / 5);

        int flatBonusPerTier = 5;
        float percentPerTier = 1.05f;

        int flat = flatBonusPerTier * tier;
        float mult = Mathf.Pow(percentPerTier, tier);

        maxHP = Mathf.Max(1, Mathf.RoundToInt((baseMaxHP + flat) * mult));
        currentHP = maxHP;

        rb = GetComponent<Rigidbody2D>();
        if (rb != null) originalConstraints = rb.constraints;

        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    void Update()
    {
        if (isDead || isHit || target == null) return;

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        float dist = toTarget.magnitude;

        if (dist <= stopDistance) return;

        Vector2 dir = toTarget / dist;

        transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);

        if (spriter != null)
        {
            if (dir.x > 0.01f) spriter.flipX = false;
            else if (dir.x < -0.01f) spriter.flipX = true;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        int dmgInt = Mathf.CeilToInt(damage);
        TakeDamageInt(dmgInt);
    }

    void TakeDamageInt(int dmg)
    {
        if (isDead) return;

        currentHP -= dmg;

        ShowHpBar();

        if (!isHit)
            StartCoroutine(HitStop(hitStopTime));

        if (spriter != null) StartCoroutine(HitFlash());

        if (currentHP <= 0)
            Die();
    }

    void ShowHpBar()
    {
        if (hpBarPrefab == null) return;

        if (hpBarInstance == null)
        {
            hpBarInstance = Instantiate(hpBarPrefab, transform.position, Quaternion.identity);
            hpBarInstance.Attach(transform);
        }

        hpBarInstance.worldOffset = new Vector3(0f, GetAutoOffsetY(), 0f);
        hpBarInstance.visibleTime = hpBarVisibleTime;

        hpBarInstance.Show(Hp01);
    }

    float GetAutoOffsetY()
    {
        if (spriter == null) return 0.8f;
        return spriter.bounds.extents.y + 0.2f;
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        isHit = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            rb.simulated = false; // ✅ 죽는 동안 물리로 꼬이는 거 방지(옵션)
        }

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        // ✅ Enemy2가 직접 경험치 지급 (핵심)
        if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null && expValue > 0)
            playerStats.AddExp(expValue);

        // (있으면 유지) 외부에서 뭔가 듣고 있다면 호출
        OnDied?.Invoke(this);

        if (hpBarInstance != null)
            Destroy(hpBarInstance.gameObject);

        if (ani != null)
        {
            ani.ResetTrigger("Hit");
            ani.ResetTrigger("Die");
            ani.SetFloat("Speed", 0f);
            ani.SetTrigger("Die");
        }

        Destroy(gameObject, 0.6f);
    }

    System.Collections.IEnumerator HitStop(float t)
    {
        isHit = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        yield return new WaitForSeconds(t);

        if (!isDead && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = originalConstraints;
        }

        isHit = false;
    }

    System.Collections.IEnumerator HitFlash()
    {
        spriter.color = Color.gray;
        yield return new WaitForSeconds(0.05f);
        spriter.color = Color.white;
    }

    public float Hp01 => (maxHP <= 0) ? 0f : (float)currentHP / maxHP;
}



