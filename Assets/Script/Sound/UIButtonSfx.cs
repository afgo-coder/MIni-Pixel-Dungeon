using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSfx : MonoBehaviour, IPointerEnterHandler
{
    [Header("SFX")]
    public bool playHover = true;
    public bool playClick = true;

    // Button의 OnClick()에 연결해서 쓰는 용도
    public void PlayClick()
    {
        if (!playClick) return;
        SoundManager.Instance?.PlayUI(SfxId.ButtonClick);
    }

    // 마우스 Hover 자동 처리
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playHover) return;
        SoundManager.Instance?.PlayUI(SfxId.ButtonHover);
    }
}

