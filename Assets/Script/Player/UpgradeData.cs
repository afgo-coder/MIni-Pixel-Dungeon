using UnityEngine;

public enum UpgradeType
{
    DamagePlus,
    FireRateMinus,     // 발사 간격 감소(=연사 증가)
    BulletsPerShotPlus,
    PiercePlus,
    MoveSpeedPlus,
    MaxHpPlus
}

[CreateAssetMenu(menuName = "Vamp/UpgradeData")]
public class UpgradeData : ScriptableObject
{
    public string title;
    [TextArea] public string desc;
    public Sprite icon;

    public UpgradeType type;
    public float value = 1f; // 용도: 타입별로 해석
}

