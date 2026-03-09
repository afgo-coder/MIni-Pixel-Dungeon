using UnityEngine;
using TMPro;

public class CriticalPopupUI : MonoBehaviour
{
    public TMP_Text text;
    public float duration = 1f;
    public float risePixels = 60f;

    RectTransform rect;
    float t;
    Vector2 startPos;
    Color startColor;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (text == null) text = GetComponentInChildren<TMP_Text>();
    }

    void OnEnable()
    {
        t = 0f;
        startPos = rect.anchoredPosition;
        if (text != null) startColor = text.color;
    }

    public void SetScreenPosition(Vector2 screenPos, Canvas canvas)
    {
        // Screen Space - Overlay 기준: screenPos를 그대로 쓰면 됨
        // (canvas가 Overlay가 아니라면 아래 RectTransformUtility 방식 쓰는 게 더 안전)
        rect.position = screenPos;
        startPos = rect.anchoredPosition;
    }

    void Update()
    {
        t += Time.deltaTime;
        float p = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);

        rect.anchoredPosition = startPos + Vector2.up * (risePixels * p);

        if (text != null)
        {
            var c = startColor;
            c.a = 1f - p;
            text.color = c;
        }

        if (p >= 1f) Destroy(gameObject);
    }
}