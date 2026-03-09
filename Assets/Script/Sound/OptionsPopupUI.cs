using UnityEngine;
using UnityEngine.UI;

public class OptionsPopupUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelRoot;
    public Button openButton;
    public Button closeButton;

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Slider uiSlider;

    [Header("Pause While Open")]
    public bool pauseGameWhileOpen = true;

    bool isOpen;
    float prevTimeScale = 1f;

    void Start()
    {
        if (panelRoot) panelRoot.SetActive(false);

        if (openButton) openButton.onClick.AddListener(Open);
        if (closeButton) closeButton.onClick.AddListener(Close);

        // 슬라이더 변경 → SoundManager 반영
        if (masterSlider) masterSlider.onValueChanged.AddListener(v => SoundManager.Instance?.SetMaster(v));
        if (bgmSlider) bgmSlider.onValueChanged.AddListener(v => SoundManager.Instance?.SetBgm(v));
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(v => SoundManager.Instance?.SetSfx(v));
        if (uiSlider) uiSlider.onValueChanged.AddListener(v => SoundManager.Instance?.SetUI(v));
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        // 게임 씬이면 일시정지(메뉴 씬에서도 timeScale 만지면 UI 애니 꼬일 수 있어서 옵션)
        if (pauseGameWhileOpen)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        // 현재 값으로 슬라이더 갱신 (이벤트 발동 방지)
        var sm = SoundManager.Instance;
        if (sm != null)
        {
            if (masterSlider) masterSlider.SetValueWithoutNotify(sm.masterVolume);
            if (bgmSlider) bgmSlider.SetValueWithoutNotify(sm.bgmVolume);
            if (sfxSlider) sfxSlider.SetValueWithoutNotify(sm.sfxVolume);
            if (uiSlider) uiSlider.SetValueWithoutNotify(sm.uiVolume);
        }

        if (panelRoot) panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        if (panelRoot) panelRoot.SetActive(false);

        if (pauseGameWhileOpen)
            Time.timeScale = prevTimeScale;
    }
}

