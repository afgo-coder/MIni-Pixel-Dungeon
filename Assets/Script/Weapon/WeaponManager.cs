using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public WeaponData[] weapons;

    public Transform weaponRoot;   // 기존 handPoint 대신
    public AutoGun autoGun;        // 자동사격 스크립트 참조(없으면 드래그)

    int currentIndex = 0;
    PlayerStats playerStats;
    // ✅ 프리팹 인스턴스(겉모습)
    GameObject currentWeaponObj;

    // ✅ 현재 무기 스탯(진짜 수치)
    WeaponData currentWeaponData;
    //시발 버그 존나많네
    void Start()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
            playerStats.OnDied += HandlePlayerDied;

        Equip(0);
    }
    void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnDied -= HandlePlayerDied;
    }

    void HandlePlayerDied()
    {
        // 🔥 무기 전체 삭제
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
        currentIndex++;
        if (currentIndex >= weapons.Length)
            currentIndex = 0;

        Equip(currentIndex);
    }

    void Equip(int index)
    {
        if (currentWeaponObj != null)
            Destroy(currentWeaponObj);

        currentWeaponData = weapons[index];
        if (currentWeaponData == null) return;

        // ✅ 무기 프리팹 장착(겉모습)
        if (currentWeaponData.prefab != null)
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

        // ✅ AutoGun에 스탯 전달 + FirePoint 자동 연결
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
    public void AddDamage(int amount)
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

    //무기변경 테스트
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            NextWeapon();
        }
    }
}
