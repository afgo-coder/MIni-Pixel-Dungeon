using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDUI : MonoBehaviour
{
    [Header("Bars (Filled Image)")]
    public Image playerHpFill;
    public Image expFill;

    [Header("Value Text (TMP)")]
    public TextMeshProUGUI hpValueText;   // ¿¹: 100/100
    public TextMeshProUGUI expValueText;  // ¿¹: 30/120

    [Header("Refs")]
    public PlayerStats player;

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerStats>();

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
        if (player == null) return;

        if (playerHpFill != null)
            playerHpFill.fillAmount = player.Hp01;

        if (expFill != null)
            expFill.fillAmount = player.Exp01;

        if (hpValueText != null)
            hpValueText.text = $"{player.currentHP}/{player.maxHP}";

        if (expValueText != null)
            expValueText.text = $"{player.currentExp}/{player.expPerLevel}";
    }
}
