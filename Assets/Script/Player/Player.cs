using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 2f;

    [Header("Refs")]
    public PlayerStats stats;

    [Header("Death UI")]
    public GameObject gameOverPanel; // 씬에 있는 패널(처음엔 비활성 추천)

    [Header("I-Frame Visual")]
    public float blinkDuration = 0.4f;
    public float blinkInterval = 0.06f;

    Animator ani;
    SpriteRenderer spriter;
    

    bool isDead;
    Coroutine blinkCo;

    void Start()
    {
        ani = GetComponent<Animator>();
        spriter = GetComponent<SpriteRenderer>();

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

        Vector2 dir = new Vector2(inputX, inputY).normalized;
        transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);

        float speed = new Vector2(inputX, inputY).magnitude;
        if (ani != null) ani.SetFloat("Speed", speed);

        if (spriter != null)
        {
            if (inputX > 0.01f) spriter.flipX = false;
            else if (inputX < -0.01f) spriter.flipX = true;
        }
    }

    // PlayerDamageReceiver가 맞을 때 이거 호출해주면 깜빡임 가능
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

    void HandleDied()
    {
        if (isDead) return;
        isDead = true;

        // 이동/조작 막기 원하면 collider/rigidbody도 끄기 가능
        GetComponent<Collider2D>().enabled = false;

        if (ani != null) ani.SetTrigger("Die");

        // 1초 뒤 게임오버
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(1f);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }
}
