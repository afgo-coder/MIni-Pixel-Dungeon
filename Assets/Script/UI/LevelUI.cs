using UnityEngine;
using TMPro;

public class LevelTextUI : MonoBehaviour
{
    public PlayerStats player;
    public TMP_Text levelText;

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerStats>();

        if (levelText == null)
            levelText = GetComponent<TMP_Text>();

        if (player != null)
        {
            player.OnChanged += Refresh;
            Refresh();
        }
    }

    void OnDestroy()
    {
        if (player != null)
            player.OnChanged -= Refresh;
    }

    void Refresh()
    {
        if (player == null || levelText == null) return;
        levelText.text = $"Lv {player.level}";
    }
}
