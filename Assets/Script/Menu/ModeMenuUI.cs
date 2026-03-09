using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeMenuUI : MonoBehaviour
{
    [Header("Scene")]
    public string gameSceneName = "Game";

    // Basic 버튼 OnClick에 연결
    void Start()
    {
        SoundManager.Instance?.PlayMenuBGM();
    }
    public void OnClickBasic()
    {
        PlayerPrefs.SetInt(GameModeData.ModeKey, 0); // Basic
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }

    // Infinite 버튼 OnClick에 연결
    public void OnClickInfinite()
    {
        PlayerPrefs.SetInt(GameModeData.ModeKey, 1); // Infinite
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }
}
