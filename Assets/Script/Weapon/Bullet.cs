using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("Move")]
    public float speed = 10f;
    public float lifeTime = 3f;

    [Header("Damage")]
    public float damage = 1;

    [Header("Pierce")]
    public int pierce = 0;

    [Header("Ricochet (Simple)")]
    public int ricochet = 0;            // 추가 타격 횟수(총알 1발 기준)
    public float ricochetRange = 2.5f;  // 추가 타격 탐색 반경

    [Header("Hit Filter")]
    public LayerMask hitMask;
    public float skin = 0.02f;

    [Header("Crit Popup (UI)")]
    public CriticalPopupUI critPopupPrefabUI;
    public RectTransform popupRoot; // HUD Canvas 아래 PopupRoot 할당
    public float worldYOffset = 0.6f; // 적 머리 위 오프셋

    private Rigidbody2D rb;
    private Collider2D col;

    private float lifeTimer;
    float critChance;
    float critMultiplier;
    float rangeMultiplier;
    private int remainingPierce;
    private int remainingRicochet;
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
    private void Start()
    {
        if (popupRoot == null)
        {
            var rootObj = GameObject.Find("PopupRoot");
            if (rootObj != null)
                popupRoot = rootObj.GetComponent<RectTransform>();
        }
    }

    /// 무기에서 생성 직후 스탯 주입용
    public void Init(
        float dmg,
        int prc,
        int rch,
        float rchRange,
        float spd,
        float lt,
        LayerMask mask,
        float cri,
        float crid,
        float range,
        float sk = 0.02f)
    {
        damage = dmg;
        critChance = cri;
        critMultiplier = crid;
        rangeMultiplier = range;
        pierce = prc;

        ricochet = rch;
        ricochetRange = rchRange;

        speed = spd;
        lifeTime = lt;
        hitMask = mask;
        skin = sk;

        lifeTimer = lifeTime * rangeMultiplier;          // 수명 타이머 시작
        remainingPierce = pierce;      // 관통 횟수 초기화
        remainingRicochet = ricochet;  // 튕김 횟수 초기화
        killed = false;                // (풀링 대비) 혹시 true였으면 초기화
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

    // ✅ 크리 여부를 같이 리턴
    float RollDamage(out bool isCrit)
    {
        isCrit = false;

        float finalDmg = damage;
        if (critChance > 0f && UnityEngine.Random.value < critChance)
        {
            isCrit = true;
            finalDmg *= Mathf.Max(1f, critMultiplier);
        }
        return finalDmg;
    }

    private void HandleHit(Collider2D other)
    {
        if (killed) return;

        // 적
        if (other.CompareTag("Enemy"))
        {
            IDamageable dmg = other.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                bool isCrit;
                float dealt = RollDamage(out isCrit);
                dmg.TakeDamage(dealt);

                if (isCrit) SpawnCritPopupUI(other);
            }

            // ✅ 초간단 튕김: 주변 적 1마리에게 추가 데미지 1회
            TryRicochet(other);

            if (remainingPierce > 0)
            {
                remainingPierce--;
                rb.position += (Vector2)transform.right * skin;
                return;
            }

            Kill();
        }
    }

    private void TryRicochet(Collider2D hitEnemy)
    {
        if (remainingRicochet <= 0) return;
        if (ricochetRange <= 0.01f) return;

        // hitMask 범위에서 찾되, Enemy 태그로 걸러준다.
        var hits = Physics2D.OverlapCircleAll(transform.position, ricochetRange, hitMask);
        if (hits == null || hits.Length == 0) return;

        Collider2D best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            var c = hits[i];
            if (c == null) continue;
            if (c == hitEnemy) continue;
            if (!c.CompareTag("Enemy")) continue;

            float d = (c.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = c;
            }
        }

        if (best == null) return;

        IDamageable dmg = best.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            bool isCrit;
            float dealt = RollDamage(out isCrit);
            dmg.TakeDamage(dealt);

            if (isCrit) SpawnCritPopupUI(best);

            remainingRicochet--;
        }
    }

    void SpawnCritPopupUI(Collider2D enemyCol)
    {
        if (critPopupPrefabUI == null) return;
        if (popupRoot == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = enemyCol.bounds.center + Vector3.up * (enemyCol.bounds.extents.y + worldYOffset);
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        // 카메라 뒤면 안 띄움
        if (screenPos.z < 0f) return;

        var ui = Instantiate(critPopupPrefabUI, popupRoot);
        var canvas = popupRoot.GetComponentInParent<Canvas>();
        ui.SetScreenPosition(screenPos, canvas);
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
