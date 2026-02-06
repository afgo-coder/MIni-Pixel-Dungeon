using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class GameClearUI : MonoBehaviour
{
    [Header("Refs")]
    public WaveSpawnManager wave;
    public CanvasGroup panel;
    public MonoBehaviour playerMove; // Player 이동 스크립트(너 프로젝트에 맞게)
    public AutoGun autoGun;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "Game";

    void Awake()
    {
        if (panel != null)
        {
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
            panel.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        if (wave == null) wave = FindFirstObjectByType<WaveSpawnManager>();
        if (autoGun == null) autoGun = FindFirstObjectByType<AutoGun>();

        // playerMove는 "이동 스크립트"만 넣어주면 됨 (예: PlayerMovement)
        // Find로 자동 연결하고 싶으면 아래처럼 타입 바꿔서 찾는 게 좋음.

        if (wave != null)
            wave.OnGameClear += Show;
    }

    void OnDestroy()
    {
        if (wave != null)
            wave.OnGameClear -= Show;
    }

    public void Show()
    {
        // 1) 진행 멈춤 (TimeScale 0 안 쓰는 방식)
        if (wave != null) wave.enabled = false;
        if (autoGun != null) autoGun.enabled = false;
        if (playerMove != null) playerMove.enabled = false;

        // 2) 패널 표시
        if (panel == null) return;

        panel.gameObject.SetActive(true);
        panel.DOKill();
        panel.transform.DOKill(); // 스케일 트윈이 패널에 걸리도록

        panel.alpha = 0f;
        panel.interactable = true;
        panel.blocksRaycasts = true;

        panel.DOFade(1f, 0.25f).SetUpdate(true);

        panel.transform.localScale = Vector3.one;
        panel.transform.DOScale(1.02f, 0.12f).SetUpdate(true)
            .OnComplete(() => panel.transform.DOScale(1f, 0.12f).SetUpdate(true));
    }

    public void OnClickRetry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnClickMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}