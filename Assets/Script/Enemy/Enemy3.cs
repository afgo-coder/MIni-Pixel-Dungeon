using UnityEngine;

public class Enemy3 : MonoBehaviour, IDamageable
{
    [Header("Move")]
    public float moveSpeed = 2.0f;
    public float stopDistance = 0.2f;

    [Header("HP")]
    public int maxHp = 5;
    int hp;

    [Header("References")]
    public Animator ani;
    public SpriteRenderer spriter;
    public Transform target; // 플레이어

    bool isDead;
    bool isHit;

    Rigidbody2D rb;
    RigidbodyConstraints2D originalConstraints;

    void Start()
    {
        hp = maxHp;

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            originalConstraints = rb.constraints;

        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    void Update()
    {
        if (isDead || isHit || target == null) return;

        Vector2 toTarget = target.position - transform.position;
        float dist = toTarget.magnitude;

        Vector2 dir = (dist > 0.0001f) ? (toTarget / dist) : Vector2.zero;

        // 이동
        if (dist > stopDistance)
        {
            transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);
            if (ani != null) ani.SetFloat("Speed", 1f);
        }
        else
        {
            if (ani != null) ani.SetFloat("Speed", 0f);
        }

        // 좌우 뒤집기
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

        
        if (hp <= 0)
        {
            Die();
            return;
        }

        // 여기부터는 "안 죽었을 때만" Hit 처리
        isHit = true;

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

    void Die()
    {
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

        if (ani != null) ani.SetTrigger("Die");
        Destroy(gameObject, 1.0f);
    }

    public void EndHit()
    {
        if (isDead) return;

        isHit = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = originalConstraints;
            // 필요하면 여기서 FreezeRotation으로 고정해도 됨
            // rb.constraints = RigidbodyConstraints2D.FreezeRotation;
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
}