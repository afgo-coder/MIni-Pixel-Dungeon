using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LevelUpUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerStats player;
    public WeaponManager weaponManager;

    [Header("UI")]
    public GameObject root;              // 전체 패널(어두운 배경 포함)
    public Transform cardParent;        // 카드 3장이 들어갈 부모
    public UpgradeCardUI cardPrefab;    // 카드 프리팹

    [Header("Upgrades Pool")]
    public List<UpgradeData> upgradePool = new List<UpgradeData>();

    List<UpgradeCardUI> spawned = new List<UpgradeCardUI>();

    // --------------------
    // 제한 규칙
    // --------------------
    readonly HashSet<UpgradeType> unlimitedTypes = new HashSet<UpgradeType>
    {
        UpgradeType.DamagePlus,
        UpgradeType.MaxHpPlus,
        UpgradeType.MoveSpeedPlus,
        UpgradeType.PiercePlus
    };

    const int MAX_LIMITED_COUNT = 4;
    bool isOpen;

    void Awake()
    {
        if (root) root.SetActive(false);
    }

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerStats>();
        if (weaponManager == null) weaponManager = FindFirstObjectByType<WeaponManager>();

        if (player != null)
            player.OnLevelUp += HandleLevelUp;
    }

    void OnDestroy()
    {
        if (player != null)
            player.OnLevelUp -= HandleLevelUp;
    }

    void HandleLevelUp(int newLevel)
    {
        Open();
    }

    // --------------------
    // UI Open / Close
    // --------------------
    void Open()
    {
        if (isOpen) return;
        isOpen = true;

        if (root) root.SetActive(true);

        ClearCards();

        var choices = Pick3(upgradePool);
        foreach (var u in choices)
        {
            var card = Instantiate(cardPrefab, cardParent);
            card.Setup(u, OnPick);

            // MAX 표시
            bool isMax = IsMaxed(u);
            card.SetMax(isMax);

            spawned.Add(card);
        }

        Time.timeScale = 0f; // 게임 멈춤
    }

    void Close()
    {
        Time.timeScale = 1f; // 게임 재개
        ClearCards();
        if (root) root.SetActive(false);
        isOpen = false;
    }

    // --------------------
    // 선택 처리
    // --------------------
    void OnPick(UpgradeData data)
    {
        ApplyUpgrade(data);

        // 선택 횟수 기록
        if (player != null && data != null)
            player.RegisterUpgrade(data.type);

        Close();
    }

    // --------------------
    // 업그레이드 적용
    // --------------------
    void ApplyUpgrade(UpgradeData u)
    {
        if (u == null) return;

        switch (u.type)
        {
            case UpgradeType.MaxHpPlus:
                if (player != null)
                {
                    int v = Mathf.RoundToInt(u.value);
                    player.maxHP += v;
                    player.currentHP = Mathf.Min(player.currentHP + v, player.maxHP);
                }
                break;

            case UpgradeType.MoveSpeedPlus:
                var p = FindFirstObjectByType<Player>();
                if (p != null) p.moveSpeed += u.value;
                break;

            case UpgradeType.DamagePlus:
                if (weaponManager != null)
                    weaponManager.AddDamage(Mathf.RoundToInt(u.value));
                break;

            case UpgradeType.FireRateMinus:
                if (weaponManager != null)
                    weaponManager.AddFireRate(-u.value);
                break;

            case UpgradeType.BulletsPerShotPlus:
                if (weaponManager != null)
                    weaponManager.AddBulletsPerShot(Mathf.RoundToInt(u.value));
                break;

            case UpgradeType.PiercePlus:
                if (weaponManager != null)
                    weaponManager.AddPierce(Mathf.RoundToInt(u.value));
                break;
        }

        // HUD 갱신
        player?.SendMessage("OnChanged", SendMessageOptions.DontRequireReceiver);
    }

    // --------------------
    // 제한 로직
    // --------------------
    bool IsLimited(UpgradeType type)
    {
        return !unlimitedTypes.Contains(type);
    }

    bool IsMaxed(UpgradeData u)
    {
        if (u == null || player == null) return false;
        if (!IsLimited(u.type)) return false;

        return player.GetUpgradeCount(u.type) >= MAX_LIMITED_COUNT;
    }

    // --------------------
    // 카드 뽑기 (MAX는 최대 1장만)
    // --------------------
    List<UpgradeData> Pick3(List<UpgradeData> pool)
    {
        if (pool == null || pool.Count == 0)
            return new List<UpgradeData>();

        var available = new List<UpgradeData>(); // 선택 가능
        var maxed = new List<UpgradeData>();     // MAX 찍힌 것들

        foreach (var u in pool)
        {
            if (u == null) continue;

            if (!IsLimited(u.type))
            {
                available.Add(u);
                continue;
            }

            int count = player != null ? player.GetUpgradeCount(u.type) : 0;
            if (count >= MAX_LIMITED_COUNT)
                maxed.Add(u);
            else
                available.Add(u);
        }

        // 안전장치: 선택 가능한 게 하나도 없으면 무제한만이라도 나오게
        if (available.Count == 0)
        {
            available = pool
                .Where(u => u != null && !IsLimited(u.type))
                .ToList();
        }

        // 기본 3장 뽑기
        var result = available
            .OrderBy(_ => Random.value)
            .Take(Mathf.Min(3, available.Count))
            .ToList();

        // MAX는 최대 1장만 끼워넣기
        if (maxed.Count > 0 && result.Count == 3)
        {
            var maxPick = maxed[Random.Range(0, maxed.Count)];
            int replaceIndex = Random.Range(0, result.Count);

            if (!result.Contains(maxPick))
                result[replaceIndex] = maxPick;
        }

        return result;
    }

    // --------------------
    // 정리
    // --------------------
    void ClearCards()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                Destroy(spawned[i].gameObject);
        }
        spawned.Clear();
    }

    // 테스트용
    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.L))
    //        Open();
    //}
}

