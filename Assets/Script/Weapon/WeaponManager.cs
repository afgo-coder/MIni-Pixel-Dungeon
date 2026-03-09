using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public WeaponData[] weapons;

    public Transform weaponRoot;   // 기존 handPoint 대신
    public AutoGun autoGun;        // 자동사격 스크립트 참조(없으면 드래그)

    int currentIndex = 0;
    PlayerStats playerStats;

    // 프리팹 인스턴스(겉모습)
    GameObject currentWeaponObj;

    // 현재 무기 스탯(진짜 수치)
    WeaponData currentWeaponData;

    void Start()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
            playerStats.OnDied += HandlePlayerDied;

        // 저장된 시작 무기 인덱스로 장착 (없으면 0)
        int savedIndex = PlayerPrefs.GetInt(GameModeData.WeaponKey, 0);

        if (weapons == null || weapons.Length == 0)
        {
            Debug.LogWarning("[WeaponManager] weapons 배열이 비어있음");
            return;
        }

        if (savedIndex < 0 || savedIndex >= weapons.Length)
            savedIndex = 0;

        Equip(savedIndex);
    }

    void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnDied -= HandlePlayerDied;
    }

    void HandlePlayerDied()
    {
        // 무기 전체 삭제
        if (weaponRoot != null)
        {
            Destroy(weaponRoot.gameObject);
        }

        // 자동사격도 꺼버리기
        if (autoGun != null)
            autoGun.enabled = false;
    }

    public void NextWeapon()
    {
        if (weapons == null || weapons.Length == 0) return;

        currentIndex++;
        if (currentIndex >= weapons.Length)
            currentIndex = 0;

        Equip(currentIndex);
    }

    // 외부에서 시작 무기 지정하고 싶으면 이걸 쓰면 됨
    public void EquipByIndex(int index)
    {
        Equip(index);
    }

    void Equip(int index)
    {
        if (weapons == null || weapons.Length == 0) return;
        if (index < 0 || index >= weapons.Length) index = 0;

        currentIndex = index;

        if (currentWeaponObj != null)
            Destroy(currentWeaponObj);

        currentWeaponData = weapons[index];
        if (currentWeaponData == null) return;

        // 무기 프리팹 장착(겉모습)
        if (currentWeaponData.prefab != null && weaponRoot != null)
        {
            currentWeaponObj = Instantiate(currentWeaponData.prefab, weaponRoot);
            currentWeaponObj.transform.localPosition = currentWeaponData.localPosition;
            currentWeaponObj.transform.localRotation = Quaternion.Euler(currentWeaponData.localRotation);
            currentWeaponObj.transform.localScale = currentWeaponData.localScale;
        }
        else
        {
            currentWeaponObj = null;
        }

        // AutoGun에 스탯 전달 + FirePoint 자동 연결
        if (autoGun != null)
        {
            autoGun.SetWeapon(currentWeaponData);

            if (currentWeaponObj != null)
            {
                Transform fp = currentWeaponObj.transform.Find("FirePoint");
                if (fp != null)
                    autoGun.firePoint = fp;
            }
        }
    }

    // --------------------
    // Upgrade APIs (스탯은 WeaponData를 수정)
    // --------------------
    public void AddDamage(float amount)
    {
        if (currentWeaponData == null) return;
        currentWeaponData.damage = Mathf.Max(0, currentWeaponData.damage + amount);
    }

    public void AddFireRate(float delta)
    {
        if (currentWeaponData == null) return;
        currentWeaponData.fireRate = Mathf.Max(0.05f, currentWeaponData.fireRate + delta);
    }

    public void AddBulletsPerShot(int amount)
    {
        if (currentWeaponData == null) return;
        currentWeaponData.bulletsPerShot = Mathf.Max(1, currentWeaponData.bulletsPerShot + amount);
    }

    public void AddPierce(int amount)
    {
        if (currentWeaponData == null) return;
        currentWeaponData.pierce = Mathf.Max(0, currentWeaponData.pierce + amount);
    }

    public void AddRicochet(int amount)
    {
        if (currentWeaponData == null) return;
        currentWeaponData.ricochet = Mathf.Max(0, currentWeaponData.ricochet + amount);
    }

    // --------------------
    // Crit / Range
    // --------------------
    public void AddCritChance(float delta)
    {
        if (currentWeaponData == null) return;
        // 0~0.8 정도까지만 추천(너무 세지기 쉬움)
        currentWeaponData.critChance = Mathf.Clamp01(currentWeaponData.critChance + delta);
    }

    public void AddCritMultiplier(float delta)
    {
        if (currentWeaponData == null) return;
        // 최소 1.0, 보통 1.5~3.0 선에서 밸런스
        currentWeaponData.critMultiplier = Mathf.Max(1f, currentWeaponData.critMultiplier + delta);
    }

    public void AddRangeMultiplier(float delta)
    {
        if (currentWeaponData == null) return;
        // lifeTime에 곱해지는 값이라 너무 낮아지면 총알이 바로 사라짐
        currentWeaponData.rangeMultiplier = Mathf.Max(0.2f, currentWeaponData.rangeMultiplier + delta);
    }


    //void Update()
    //{
    //    // 무기변경 테스트
    //    if (Input.GetKeyDown(KeyCode.Q))
    //    {
    //        NextWeapon();
    //    }
    //}
}
