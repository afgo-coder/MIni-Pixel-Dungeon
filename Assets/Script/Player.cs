using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 2f;

    Animator ani;
    SpriteRenderer spriter;

    void Start()
    {
        ani = GetComponent<Animator>();
        spriter = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");

        Vector2 dir = new Vector2(inputX, inputY).normalized;

        //  이동은 Player 본체만
        transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);

        //  Speed는 magnitude로(대각선 안정)
        float speed = new Vector2(inputX, inputY).magnitude;
        if (ani != null) ani.SetFloat("Speed", speed);

        //  Flip도 Player 본체 스프라이트만
        if (spriter != null)
        {
            if (inputX > 0.01f) spriter.flipX = false;
            else if (inputX < -0.01f) spriter.flipX = true;
        }
    }
}