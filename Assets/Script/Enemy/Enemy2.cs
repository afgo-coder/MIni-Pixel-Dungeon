using UnityEngine;

public class Enemy2 : MonoBehaviour, IDamageable
{
    public float moveSpeed = 2.5f;
    public float stopDistance = 0.2f;
    public int maxHP = 5;
    public int currentHP;

    public float hitStopTime = 0.12f;
    public Animator ani;
    public SpriteRenderer spriter;
    public Transform target;

    [Header("World HP Bar")]
    public WorldHPBar hpBarPrefab;                 // 월드 체력바 프리팹(Inspector 연결)
    public Vector3 hpBarOffset = new Vector3(0f, 0.5f, 0f);
    public float hpBarVisibleTime = 2f;            // 피격 후 몇 초 표시
    WorldHPBar hpBarInstance;

    public bool isDead;
    bool isHit;
    Rigidbody2D rb;
    RigidbodyConstraints2D originalConstraints;

    public System.Action<Enemy2> OnDied;

    void Start()
    {
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

        // 좌우 뒤집기(선택)
        if (spriter != null)
        {
            if (dir.x > 0.01f) spriter.flipX = false;
            else if (dir.x < -0.01f) spriter.flipX = true;
        }
    }

    public void TakeDamage(int dmg)
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
        isDead = true;
        isHit = false;


        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        // ✅ 죽음 이벤트 먼저 호출 (경험치/드랍 처리)
        OnDied?.Invoke(this);

        // ✅ 체력바 정리
        if (hpBarInstance != null)
            Destroy(hpBarInstance.gameObject);

        if (ani != null) ani.SetTrigger("Die");
        // death 클립 길이에 맞게 조절 (대충 0.6~1.2)
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
        // 흰색/회색 깜빡(원하면 색 변경)
        spriter.color = Color.gray;
        yield return new WaitForSeconds(0.05f);
        spriter.color = Color.white;
    }
    public float Hp01 => (maxHP <= 0) ? 0f : (float)currentHP / maxHP;
}


