using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scene Name")]
    public string weaponSceneName = "WeaponMenu";
    void Start()
    {
        SoundManager.Instance?.PlayMenuBGM();

        PlayerPrefs.DeleteKey(GameModeData.WeaponKey);
        PlayerPrefs.DeleteKey(GameModeData.ModeKey);
    }
    public void OnClickStart()
    {
        Debug.Log($"[MainMenuUI] Start Clicked. Will load: {weaponSceneName}");
        Debug.Log($"[MainMenuUI] CanLoad? {Application.CanStreamedLevelBeLoaded(weaponSceneName)}");
        Time.timeScale = 1f; // »§Ω√ ¿Ã¿¸ æ¿ø°º≠ ∏ÿ√Ë¿ª ∞ÊøÏ ¥Î∫Ò
        SceneManager.LoadScene(weaponSceneName);
    }

    public void OnClickQuit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
