using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseRoot; // 패널(Resume/Option/Quit)
    public LevelUpUI levelUpUI;  // 열려있으면 Pause 금지

    bool paused;

    void Start()
    {
        if (pauseRoot) pauseRoot.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 레벨업 카드 열려있으면 무시
            if (levelUpUI != null && levelUpUI.enabled && levelUpUI.gameObject.activeInHierarchy && levelUpUI.root.activeSelf)
                return;

            TogglePause();
        }
    }

    public void TogglePause()
    {
        paused = !paused;

        if (pauseRoot) pauseRoot.SetActive(paused);
        Time.timeScale = paused ? 0f : 1f;
    }

    public void Resume()
    {
        if (!paused) return;
        TogglePause();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;     // ★ 중요: 멈춘 상태로 씬 넘어가면 다음 씬도 멈춰있음
        paused = false;

        SceneManager.LoadScene("MainMenu"); // 네 메인메뉴 씬 이름으로
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
