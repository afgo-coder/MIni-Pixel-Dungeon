using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Move")]
    public float moveSpeed = 1.5f;
    public float stopDistance = 0.2f;

    [Header("HP")]
    public int maxHp = 3;
    int hp;

    [Header("References")]
    public Animator ani;
    public SpriteRenderer spriter;
    public Transform target; // 플레이어

    [Header("World HP Bar")]
    public WorldHPBar hpBarPrefab;                 // 월드 체력바 프리팹(Inspector 연결)
    public Vector3 hpBarOffset = new Vector3(0f, 0.5f, 0f);
    public float hpBarVisibleTime = 2f;            // 피격 후 몇 초 표시
    WorldHPBar hpBarInstance;

    [Header("State")]
    bool isDead;
    bool isHit;

    [Header("Hit Recover")]
    public float hitStunTime = 0.15f;  // 애니메이션 없을 때도 풀리는 시간
    float hitTimer;

    Rigidbody2D rb;
    RigidbodyConstraints2D originalConstraints;

    // 죽었을 때 보상/드랍 같은 외부 처리용 이벤트
    public System.Action<Enemy> OnDied;

    void Start()
    {
        hp = maxHp;

        rb = GetComponent<Rigidbody2D>();
        if (rb != null) originalConstraints = rb.constraints;

        if (target == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    void Update()
    {
        if (isDead || target == null) return;

        if (isHit)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0f)
                EndHit();
            return;
        }

        Vector2 toTarget = target.position - transform.position;
        float dist = toTarget.magnitude;

        Vector2 dir = (dist > 0.0001f) ? (toTarget / dist) : Vector2.zero;

        if (dist > stopDistance)
        {
            transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);
            if (ani != null) ani.SetFloat("Speed", 1f);
        }
        else
        {
            if (ani != null) ani.SetFloat("Speed", 0f);
        }

        if (spriter != null)
        {
            if (dir.x > 0.01f) spriter.flipX = false;
            else if (dir.x < -0.01f) spriter.flipX = true;
        }
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        hp -= dmg;

        // ✅ 피격 시 월드 체력바 표시/갱신
        ShowHpBar();

        if (hp <= 0)
        {
            Die();
            return;
        }

        // Hit 처리 (안 죽었을 때)
        isHit = true;
        hitTimer = hitStunTime;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        if (ani != null)
        {
            ani.SetFloat("Speed", 0f);
            ani.ResetTrigger("Die");
            ani.SetTrigger("Hit");
        }

        if (spriter != null) StartCoroutine(HitFlash());
    }
    void ShowHpBar()
    {
        if (hpBarPrefab == null) return;

        if (hpBarInstance == null)
        {
            hpBarInstance = Instantiate(hpBarPrefab, transform.position, Quaternion.identity);
            hpBarInstance.Attach(transform);
        }

        // 오프셋은 자동 계산 추천
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

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        // ✅ 죽음 이벤트 먼저 호출 (경험치/드랍 처리)
        OnDied?.Invoke(this);

        // ✅ 체력바 정리
        if (hpBarInstance != null)
            Destroy(hpBarInstance.gameObject);

        if (ani != null) ani.SetTrigger("Die");
        Destroy(gameObject, 1.0f);
    }

    // 애니메이션 이벤트에서 호출
    public void EndHit()
    {
        if (isDead) return;

        isHit = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = originalConstraints;
        }
    }

    System.Collections.IEnumerator HitFlash()
    {
        spriter.color = Color.white;
        yield return new WaitForSeconds(0.05f);
        spriter.color = Color.gray;
        yield return new WaitForSeconds(0.05f);
        spriter.color = Color.white;
    }

    public float Hp01 => (maxHp <= 0) ? 0f : (float)hp / maxHp;
}
