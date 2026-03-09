using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Move")]
    public float moveSpeed = 1.5f;
    public float stopDistance = 0.2f;

    [Header("HP")]
    public int maxHp = 3;
    int hp;
    int baseMaxHP;

    [Header("HP Scaling")]
    public int levelStep = 5;        
    public float stepMultiplier = 1.05f; 


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
    public bool isDead;
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
        baseMaxHP = maxHp;

        // ✅ 플레이어 레벨 가져오기
        int playerLevel = 1;
        var ps = FindFirstObjectByType<PlayerStats>();
        if (ps != null) playerLevel = ps.level;   // PlayerStats에 level 필드가 있다고 가정

        // ✅ 5레벨마다 5%씩 HP 증가
        int tier = Mathf.Max(0, playerLevel / 5);
        int flatBonusPerTier = 5;        // 고정 증가량
        float percentPerTier = 1.05f;    // 퍼센트 증가량

        int flat = flatBonusPerTier * tier;
        float mult = Mathf.Pow(percentPerTier, tier);

        maxHp = Mathf.Max(1, Mathf.RoundToInt((baseMaxHP + flat) * mult));
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

    // ✅ 인터페이스 구현 (float)
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        int dmgInt = Mathf.CeilToInt(damage); // <-- 규칙: 올림(추천)
        TakeDamageInt(dmgInt);
    }

    // ✅ 기존 로직을 int 전용으로 분리 (기존 TakeDamage(int) 내용을 여기로)
    void TakeDamageInt(int dmg)
    {
        if (isDead) return;

        hp -= dmg;

        ShowHpBar();

        if (hp <= 0)
        {
            Die();
            return;
        }

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
        if (ani != null)
        {
            ani.ResetTrigger("Hit");
            ani.ResetTrigger("Die");
            ani.SetFloat("Speed", 0f);

            // 1) 가장 안전: 즉시 죽음 상태로 강제 이동 (상태명은 너 Animator에 맞춰)
            // ani.Play("Die", 0, 0f);

            // 2) 상태명 모르겠으면 트리거로라도 보장
            ani.SetTrigger("Die");
        }
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

        float dieLen = 1.0f;
        if (ani != null)
        {
            // 현재 상태 길이를 못 믿는 경우가 있어서, AnimationClip을 인스펙터로 넣는게 더 확실
            var st = ani.GetCurrentAnimatorStateInfo(0);
            // 트리거 직후엔 아직 상태가 안 바뀔 수 있으니 최소 0.6~1.2로 클램프
            dieLen = Mathf.Clamp(st.length, 0.6f, 2.0f);
        }

        Destroy(gameObject, dieLen);
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
