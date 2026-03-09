using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LevelUpUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerStats player;
    public WeaponManager weaponManager;
    public Player playerMove; // ✅ MoveSpeed 업그레이드용 캐싱

    [Header("UI")]
    public GameObject root;              // 전체 패널(어두운 배경 포함)
    public Transform cardParent;         // 카드 3장이 들어갈 부모
    public UpgradeCardUI cardPrefab;     // 카드 프리팹

    [Header("Upgrades Pool")]
    public List<UpgradeData> upgradePool = new List<UpgradeData>();

    List<UpgradeCardUI> spawned = new List<UpgradeCardUI>();

    // --------------------
    // 최대치 규칙 (타입별)
    // --------------------
    Dictionary<UpgradeType, int> maxLevelByType;

    // 무제한(제한 없음)
    readonly HashSet<UpgradeType> unlimitedTypes = new HashSet<UpgradeType>
    {
        UpgradeType.MaxHpPlus,
        UpgradeType.MoveSpeedPlus,
        UpgradeType.CritChancePlus,
    };

    bool isOpen;

    // --------------------
    // ✅ 관통 vs 튕김 택1 상태 (초간단)
    // --------------------
    bool pickedBulletPathChoice = false;
    bool pickedPierce = false;
    bool pickedRicochet = false;

    void Awake()
    {
        if (root) root.SetActive(false);

        //최대치 테이블
        maxLevelByType = new Dictionary<UpgradeType, int>()
        {
            // 3레벨 MAX
            { UpgradeType.PiercePlus, 3 },
            { UpgradeType.FireRateMinus, 3 },            
            { UpgradeType.RicochetPlus, 3 },
            // 4레벨 MAX
            { UpgradeType.BulletsPerShotPlus, 4 },
            // 5레벨 MAX
            { UpgradeType.RangePlus, 5 },           
            // 10레벨 MAX
            { UpgradeType.CritMultiplierPlus, 5 },
            // 20레벨 MAX
            { UpgradeType.DamagePlus, 20 },
        };
    }

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerStats>();
        if (weaponManager == null) weaponManager = FindFirstObjectByType<WeaponManager>();
        if (playerMove == null) playerMove = FindFirstObjectByType<Player>(); // 캐싱

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
        // ✅ 관통 vs 튕김 택1 확정 기록
        if (data != null)
        {
            if (data.type == UpgradeType.PiercePlus)
            {
                pickedBulletPathChoice = true;
                pickedPierce = true;
                pickedRicochet = false;
            }
            else if (data.type == UpgradeType.RicochetPlus)
            {
                pickedBulletPathChoice = true;
                pickedRicochet = true;
                pickedPierce = false;
            }
        }

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
                    player.ForceNotify();
                }
                break;

            case UpgradeType.MoveSpeedPlus:
                if (playerMove != null) playerMove.moveSpeed += u.value;
                break;

            case UpgradeType.RangePlus:
                if (weaponManager != null)
                    weaponManager.AddRangeMultiplier(u.value);
                break;

            case UpgradeType.DamagePlus:
                if (weaponManager != null)
                    weaponManager.AddDamage(Mathf.RoundToInt(u.value));
                break;

            case UpgradeType.CritChancePlus:
                if (weaponManager != null)
                    weaponManager.AddCritChance(u.value);
                break;

            case UpgradeType.CritMultiplierPlus:
                if (weaponManager != null)
                    weaponManager.AddCritMultiplier(u.value);
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

            case UpgradeType.RicochetPlus:
                if (weaponManager != null)
                    weaponManager.AddRicochet(Mathf.RoundToInt(u.value));
                break;

        }

        // HUD 갱신
        player?.SendMessage("OnChanged", SendMessageOptions.DontRequireReceiver);
    }

    // --------------------
    // 제한 로직 (타입별 최대치)
    // --------------------
    int GetMaxLevel(UpgradeType type)
    {
        if (unlimitedTypes.Contains(type))
            return int.MaxValue;

        if (maxLevelByType != null && maxLevelByType.TryGetValue(type, out int max))
            return max;

        return int.MaxValue; // 정의 안 된 타입은 무제한
    }

    bool IsLimited(UpgradeType type)
    {
        if (unlimitedTypes.Contains(type)) return false;
        return maxLevelByType != null && maxLevelByType.ContainsKey(type);
    }

    bool IsMaxed(UpgradeData u)
    {
        if (u == null || player == null) return false;

        int max = GetMaxLevel(u.type);
        if (max == int.MaxValue) return false;

        return player.GetUpgradeCount(u.type) >= max;
    }

    // --------------------
    // ✅ 카드 뽑기 (관통 vs 튕김 택1 + MAX는 최대 1장만)
    // --------------------

    // --------------------
    // ✅ 카드 뽑기
    //  - 관통(Pierce) vs 튕김(Ricochet)은 "같이 등장 금지"
    //  - 이미 하나를 선택했다면 반대편은 이후 영구 제외
    //  - 무한 단계(제한 카드 전부 MAX)면: HP 1 + Speed 1 + (HP/Speed 랜덤 1)
    //  - 그 외: 3장 항상 유지 + MAX 카드는 최대 1장만 섞어줌
    // --------------------
    List<UpgradeData> Pick3(List<UpgradeData> pool)
    {
        if (pool == null || pool.Count == 0)
            return new List<UpgradeData>();

        bool IsBlockedByPickedPath(UpgradeType t)
        {
            if (!pickedBulletPathChoice) return false;
            if (pickedPierce && t == UpgradeType.RicochetPlus) return true;
            if (pickedRicochet && t == UpgradeType.PiercePlus) return true;
            return false;
        }

        bool Conflicts(UpgradeType a, UpgradeType b)
        {
            return (a == UpgradeType.PiercePlus && b == UpgradeType.RicochetPlus) ||
                   (a == UpgradeType.RicochetPlus && b == UpgradeType.PiercePlus);
        }

        bool ConflictsWithPicked(List<UpgradeData> picked, UpgradeData cand)
        {
            if (cand == null) return true;
            for (int i = 0; i < picked.Count; i++)
            {
                if (picked[i] == null) continue;
                if (Conflicts(picked[i].type, cand.type)) return true;
            }
            return false;
        }

        // 무한(제한 없음) 카드들 (현재 프로젝트 기준: HP/Speed/Range)
        var unlimited = pool.Where(u => u != null && !IsLimited(u.type) && !IsBlockedByPickedPath(u.type)).ToList();

        // -----------------------------
        // 1) "무한 단계" 감지: 제한 카드 중 MAX 안 찍힌 게 있으면 아직 무한 단계 아님
        // -----------------------------
        bool hasNonMaxLimited = false;
        foreach (var u in pool)
        {
            if (u == null) continue;
            if (IsBlockedByPickedPath(u.type)) continue;
            if (!IsLimited(u.type)) continue;

            int count = player != null ? player.GetUpgradeCount(u.type) : 0;
            int max = GetMaxLevel(u.type);
            if (count < max)
            {
                hasNonMaxLimited = true;
                break;
            }
        }

        // -----------------------------
        // 2) 무한 단계면: HP 1 + Speed 1 + 랜덤 1 (항상 둘 다 한 장씩)
        // -----------------------------
        if (!hasNonMaxLimited)
        {
            var hp = pool.FirstOrDefault(u => u != null && u.type == UpgradeType.MaxHpPlus);
            var sp = pool.FirstOrDefault(u => u != null && u.type == UpgradeType.MoveSpeedPlus);

            if (hp != null && sp != null)
            {
                var fixedResult = new List<UpgradeData>(3)
                {
                    hp,
                    sp,
                    (Random.value < 0.5f) ? hp : sp
                };
                return fixedResult.OrderBy(_ => Random.value).ToList();
            }

            // 혹시 hp/sp가 풀에 없으면(설정 누락) 무한 카드들로 3장 채움(중복 허용)
            if (unlimited.Count > 0)
            {
                var fallback = new List<UpgradeData>(3);
                while (fallback.Count < 3)
                    fallback.Add(unlimited[Random.Range(0, unlimited.Count)]);
                return fallback;
            }
        }

        // -----------------------------
        // 3) 무한 단계가 아니면: 기존처럼 available/maxed로 나누고 3장 구성
        // -----------------------------
        var available = new List<UpgradeData>();
        var maxed = new List<UpgradeData>();

        foreach (var u in pool)
        {
            if (u == null) continue;
            if (IsBlockedByPickedPath(u.type)) continue;

            if (!IsLimited(u.type))
            {
                available.Add(u);
                continue;
            }

            int count = player != null ? player.GetUpgradeCount(u.type) : 0;
            int max = GetMaxLevel(u.type);

            if (count >= max) maxed.Add(u);
            else available.Add(u);
        }

        // available이 0이면 무한 카드만으로
        if (available.Count == 0)
            available = unlimited;

        // -----------------------------
        // 4) 3장 뽑기 (중복 최소화 + 관통/튕김 동시 등장 금지)
        // -----------------------------
        var result = new List<UpgradeData>(3);

        // 후보 풀을 섞어두고 하나씩 주워담는 방식(제약 적용이 쉬움)
        var shuffled = available.OrderBy(_ => Random.value).ToList();
        for (int i = 0; i < shuffled.Count && result.Count < 3; i++)
        {
            var cand = shuffled[i];
            if (cand == null) continue;

            // 중복 방지(가능하면)
            if (result.Contains(cand)) continue;

            // 관통/튕김 동시 등장 금지
            if (ConflictsWithPicked(result, cand)) continue;

            result.Add(cand);
        }

        // 부족하면 unlimited로 채움(관통/튕김 충돌 방지)
        int safety = 0;
        while (result.Count < 3 && unlimited.Count > 0 && safety++ < 50)
        {
            var cand = unlimited[Random.Range(0, unlimited.Count)];
            if (cand == null) continue;
            if (result.Contains(cand)) continue;
            if (ConflictsWithPicked(result, cand)) continue;
            result.Add(cand);
        }

        // 그래도 부족하면 pool에서라도 채움(마지막 안전망, 충돌만 피함)
        safety = 0;
        while (result.Count < 3 && safety++ < 50)
        {
            var cand = pool[Random.Range(0, pool.Count)];
            if (cand == null) continue;
            if (ConflictsWithPicked(result, cand)) continue;
            result.Add(cand);
        }

        // -----------------------------
        // 5) MAX 카드가 있으면 3장 중 "최대 1장만" MAX로 보이게 (단, 관통/튕김 규칙은 유지)
        // -----------------------------
        if (maxed.Count > 0 && result.Count == 3)
        {
            bool hasMax = result.Any(x => x != null && IsMaxed(x));
            if (!hasMax)
            {
                // result와 충돌하지 않는 MAX 후보만 고르기
                var maxCandidates = maxed
                    .Where(x => x != null && !IsBlockedByPickedPath(x.type) && !ConflictsWithPicked(result, x))
                    .ToList();

                if (maxCandidates.Count > 0)
                {
                    var maxPick = maxCandidates[Random.Range(0, maxCandidates.Count)];
                    int replaceIndex = Random.Range(0, result.Count);

                    // 교체로 인해 충돌이 생기면(아주 예외) 다른 인덱스로 한 번 더 시도
                    if (!ConflictsWithPicked(result.Where((_, idx) => idx != replaceIndex).ToList(), maxPick))
                        result[replaceIndex] = maxPick;
                }
            }
        }

        return result.OrderBy(_ => Random.value).ToList();
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
}
