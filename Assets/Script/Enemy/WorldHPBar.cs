using UnityEngine;
using UnityEngine.UI;

public class WorldHPBar : MonoBehaviour
{
    public Image fill;
    public Vector3 worldOffset = new Vector3(0f, 0.8f, 0f);
    public float visibleTime = 2f;

    Transform followTarget;
    float timer;

    Camera cam;

    void Awake()
    {
        cam = Camera.main;
        Hide();
    }

    public void Attach(Transform target)
    {
        followTarget = target;
    }

    public void Show(float hp01)
    {
        timer = visibleTime;
        gameObject.SetActive(true);
        SetHP(hp01);
    }

    public void SetHP(float hp01)
    {
        if (fill != null) fill.fillAmount = Mathf.Clamp01(hp01);
    }

    void Update()
    {
        if (followTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        // 위치 따라다니기
        transform.position = followTarget.position + worldOffset;

        // 카메라 바라보기(2D에서도 월드 캔버스는 카메라 향하게 해주는 게 깔끔)
        if (cam != null)
            transform.rotation = cam.transform.rotation;

        // 타이머
        timer -= Time.deltaTime;
        if (timer <= 0f)
            Hide();
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }
}
