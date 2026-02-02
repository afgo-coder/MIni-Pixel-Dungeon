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
    [Tooltip("0이면 관통 없음(첫 적 맞으면 삭제). 1이면 1마리 관통(총 2마리까지 맞힘)")]
    public int pierce = 0;

    [Header("Hit Filter")]
    [Tooltip("총알이 검사할 레이어")]
    public LayerMask hitMask;

    [Tooltip("캐스트 거리 보정(너무 딱 붙어서 맞을 때 튕김 방지)")]
    public float skin = 0.02f;

    private Rigidbody2D rb;
    private Collider2D col;

    private float lifeTimer;
    private int remainingPierce;
    private bool killed;

    // Cast 결과 재사용(할당 줄이기)
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Trigger든 아니든 상관없지만, 보통 총알은 Trigger로 두는 편이 편함
        // (벽/적 판정만 할 거라면)
        // col.isTrigger = true;
    }

    void OnEnable()
    {
        killed = false;
        lifeTimer = lifeTime;

        remainingPierce = pierce;

        // 발사 방향(현재 회전 기준 right)
        rb.linearVelocity = (Vector2)transform.right * speed;
    }

    void FixedUpdate()
    {
        if (killed) return;

        // 수명 처리(Invoke 대신 타이머)
        lifeTimer -= Time.fixedDeltaTime;
        if (lifeTimer <= 0f)
        {
            Kill();
            return;
        }

        // 현재 프레임에 이동할 거리만큼 "스윕"해서 먼저 맞는 것을 찾음
        Vector2 v = rb.linearVelocity;
        float dist = v.magnitude * Time.fixedDeltaTime;

        if (dist <= 0.0001f) return;

        // ContactFilter 구성 (Trigger도 맞추고 싶으면 useTriggers = true)
        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = hitMask,
            useTriggers = true
        };

        int hitCount = rb.Cast(v.normalized, filter, castHits, dist + skin);

        if (hitCount > 0)
        {
            // 가장 가까운 히트 찾기
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

                // 충돌 지점 바로 앞에 위치시키고(겹침 방지)
                Vector2 newPos = hit.point - v.normalized * skin;
                rb.position = newPos;

                HandleHit(hit.collider);
            }
        }
    }

    // 혹시 Cast에서 놓치는 경우를 대비한 "보험"
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
            if (dmg != null)
                dmg.TakeDamage(damage);

            // 관통 처리
            if (remainingPierce > 0)
            {
                remainingPierce--;
                // 관통이면 계속 날아가게 두되, 같은 프레임에 연속 판정 방지용으로
                // 아주 살짝 앞으로 밀어줌(겹침 방지)
                rb.position += (Vector2)transform.right * skin;
                return;
            }

            Kill();
            return;
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
