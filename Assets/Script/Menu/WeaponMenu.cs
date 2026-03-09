using UnityEngine;
using UnityEngine.SceneManagement;

public class WeaponMenuUI : MonoBehaviour
{
    public string modeMenuSceneName = "ModeMenu";
    private void Start()
    {
        SoundManager.Instance?.PlayMenuBGM();
    }
    public void OnClickPistol()
    {
        PlayerPrefs.SetInt(GameModeData.WeaponKey, 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene(modeMenuSceneName);
    }

    public void OnClickShotgun()
    {
        PlayerPrefs.SetInt(GameModeData.WeaponKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(modeMenuSceneName);
    }

    public void OnClickRifle()
    {
        PlayerPrefs.SetInt(GameModeData.WeaponKey, 2);
        PlayerPrefs.Save();
        SceneManager.LoadScene(modeMenuSceneName);
    }
}
