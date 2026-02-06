using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("Move")]
    public float speed = 10f;
    public float lifeTime = 3f;

    [Header("Damage")]
    public int damage = 1;

    [Header("Pierce")]
    public int pierce = 0;

    [Header("Hit Filter")]
    public LayerMask hitMask;
    public float skin = 0.02f;

    private Rigidbody2D rb;
    private Collider2D col;

    private float lifeTimer;
    private int remainingPierce;
    private bool killed;

    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // 보통 총알은 Trigger 권장
        col.isTrigger = true;
    }

 
    /// 무기에서 생성 직후 스탯 주입용
    public void Init(int dmg, int prc, float spd, float lt, LayerMask mask, float sk = 0.02f)
    {
        damage = dmg;
        pierce = prc;
        speed = spd;
        lifeTime = lt;
        hitMask = mask;
        skin = sk;

        lifeTimer = lifeTime;        // 수명 타이머 시작
        remainingPierce = pierce;    // 관통 횟수 초기화
        killed = false;              // (풀링 대비) 혹시 true였으면 초기화
    }

    /// <summary>
    /// 스탯 주입 끝난 뒤 마지막에 호출해서 발사
    /// </summary>
    public void Fire(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        // 회전도 맞춰두면 rb.Cast 방향/transform.right가 일치함
        transform.right = dir;
        rb.linearVelocity = dir * speed;
    }

    void FixedUpdate()
    {
        if (killed) return;

        lifeTimer -= Time.fixedDeltaTime;
        if (lifeTimer <= 0f)
        {
            Kill();
            return;
        }

        Vector2 v = rb.linearVelocity;
        float dist = v.magnitude * Time.fixedDeltaTime;
        if (dist <= 0.0001f) return;

        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = hitMask,
            useTriggers = true
        };

        int hitCount = rb.Cast(v.normalized, filter, castHits, dist + skin);

        if (hitCount > 0)
        {
            int best = -1;
            float bestDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                var h = castHits[i];
                if (h.collider == null) continue;

                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    best = i;
                }
            }

            if (best != -1)
            {
                var hit = castHits[best];

                Vector2 newPos = hit.point - v.normalized * skin;
                rb.position = newPos;

                HandleHit(hit.collider);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {        

        if (killed) return;
        if (((1 << other.gameObject.layer) & hitMask) == 0) return;

        HandleHit(other);
    }

    private void HandleHit(Collider2D other)
    {
        if (killed) return;

        // 적
        if (other.CompareTag("Enemy"))
        {
            IDamageable dmg = other.GetComponentInParent<IDamageable>();
            if (dmg != null) dmg.TakeDamage(damage);

            if (remainingPierce > 0)
            {
                remainingPierce--;
                rb.position += (Vector2)transform.right * skin;
                return;
            }

            Kill();
        }
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }

    private void Kill()
    {
        if (killed) return;
        killed = true;
        Destroy(gameObject);
    }




}
