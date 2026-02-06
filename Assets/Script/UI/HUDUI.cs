using UnityEngine;
using UnityEngine.UI;

public class HUDUI : MonoBehaviour
{
    [Header("Bars (Filled Image)")]
    public Image playerHpFill; // ÃÊ·Ï
    public Image expFill;      // ÆÄ¶û

    [Header("Refs")]
    public PlayerStats player;

    void Start()
    {
        if (player != null)
            player.OnChanged += Refresh;

        Refresh();
    }

    void OnDestroy()
    {
        if (player != null)
            player.OnChanged -= Refresh;
    }

    void Refresh()
    {
        if (playerHpFill != null && player != null)
            playerHpFill.fillAmount = player.Hp01;

        if (expFill != null && player != null)
            expFill.fillAmount = player.Exp01;
    }
}
