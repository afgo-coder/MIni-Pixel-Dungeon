using UnityEngine;

public class SceneBgm : MonoBehaviour
{
    public enum Track { Menu, Game }
    public Track track = Track.Menu;

    void Start()
    {
        if (SoundManager.Instance == null) return;

        if (track == Track.Menu) SoundManager.Instance.PlayMenuBGM();
        else SoundManager.Instance.PlayGameBGM();
    }
}

