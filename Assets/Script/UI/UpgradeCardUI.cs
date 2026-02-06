using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UpgradeCardUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text titleText;
    public TMP_Text descText;
    public Button button;

    [Header("MAX UI (Optional)")]
    public TMP_Text maxText;      // "MAX" 표시용 (없어도 됨)
    public CanvasGroup canvasGroup; // 회색/투명 처리용 (없어도 됨)

    UpgradeData data;
    Action<UpgradeData> onPick;
    bool blocked;

    public void Setup(UpgradeData d, Action<UpgradeData> pickCallback)
    {
        data = d;
        onPick = pickCallback;

        if (titleText) titleText.text = d.title;
        if (descText) descText.text = d.desc;

        if (icon != null)
        {
            icon.sprite = d.icon;
            icon.enabled = (d.icon != null);
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            if (blocked) return;
            onPick?.Invoke(data);
        });

        // 기본값
        SetMax(false);
    }

    public void SetMax(bool isMax)
    {
        blocked = isMax;

        if (maxText != null)
            maxText.gameObject.SetActive(isMax);

        // 클릭 막기
        if (button != null)
            button.interactable = !isMax;

        // 회색 처리(선택)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isMax ? 0.55f : 1f;
        }
    }
}

