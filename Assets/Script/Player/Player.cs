using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 2f;

    [Header("Refs")]
    public PlayerStats stats;

    [Header("Death UI")]
    public GameObject gameOverPanel;

    [Header("I-Frame Visual")]
    public float blinkDuration = 0.4f;
    public float blinkInterval = 0.06f;

    [Header("Boundary")]
    public LayerMask wallMask;

    Animator ani;
    SpriteRenderer spriter;

    bool isDead;
    Coroutine blinkCo;

    // ✅ 슬라이딩 충돌 체크용
    Rigidbody2D rb;
    Collider2D col;
    ContactFilter2D filter;
    RaycastHit2D[] hits = new RaycastHit2D[8];

    // ✅ 입력 저장(Update에서 갱신, FixedUpdate에서 사용)
    Vector2 moveInput;

    void Start()
    {
        ani = GetComponent<Animator>();
        spriter = GetComponent<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = wallMask;
        filter.useTriggers = true;

        if (stats == null) stats = GetComponent<PlayerStats>();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (stats != null)
            stats.OnDied += HandleDied;
    }

    void OnDestroy()
    {
        if (stats != null)
            stats.OnDied -= HandleDied;
    }

    void Update()
    {
        if (isDead) return;

        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");

        // ✅ 입력은 Update에서 저장
        moveInput = new Vector2(inputX, inputY).normalized;

        // 애니/플립은 Update에서 처리 (부드럽게)
        float speed = new Vector2(inputX, inputY).magnitude;
        if (ani != null) ani.SetFloat("Speed", speed);

        if (spriter != null)
        {
            if (inputX > 0.01f) spriter.flipX = false;
            else if (inputX < -0.01f) spriter.flipX = true;
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // ✅ 이동은 물리 프레임에서
        Vector2 delta = moveInput * moveSpeed * Time.fixedDeltaTime;
        MoveWithSlide(delta);
    }

    void MoveWithSlide(Vector2 delta)
    {
        if (delta == Vector2.zero) return;

        // Rigidbody2D/Collider2D 없으면 안전 fallback
        if (rb == null || col == null)
        {
            transform.Translate(delta, Space.World);
            return;
        }

        Vector2 pos = rb.position;

        // X 먼저
        if (Mathf.Abs(delta.x) > 0f)
        {
            Vector2 stepX = new Vector2(delta.x, 0f);
            if (!WillHit(stepX))
                pos += stepX;
        }

        // Y 다음 (벽 타기)
        if (Mathf.Abs(delta.y) > 0f)
        {
            Vector2 stepY = new Vector2(0f, delta.y);
            if (!WillHit(stepY))
                pos += stepY;
        }

        rb.MovePosition(pos);
    }

    bool WillHit(Vector2 delta)
    {
        int count = col.Cast(delta.normalized, filter, hits, delta.magnitude);
        return count > 0;
    }

    // ------------------------
    // Hit Blink
    // ------------------------
    public void PlayHitBlink()
    {
        if (isDead) return;
        if (spriter == null) return;

        if (blinkCo != null) StopCoroutine(blinkCo);
        blinkCo = StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        float t = 0f;
        while (t < blinkDuration)
        {
            spriter.enabled = !spriter.enabled;
            yield return new WaitForSeconds(blinkInterval);
            t += blinkInterval;
        }
        spriter.enabled = true;
        blinkCo = null;
    }

    // ------------------------
    // Death
    // ------------------------
    void HandleDied()
    {
        if (isDead) return;
        isDead = true;

        var c = GetComponent<Collider2D>();
        if (c != null) c.enabled = false;

        if (ani != null) ani.SetTrigger("Die");

        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(1f);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }
}

