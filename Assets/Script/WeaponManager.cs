using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public WeaponData[] weapons;

    public Transform weaponRoot;   // 기존 handPoint 대신
    public AutoGun autoGun;        // 자동사격 스크립트 참조(없으면 드래그)

    int currentIndex = 0;
    GameObject currentWeapon;

    void Start()
    {
        Equip(0);
    }

    public void NextWeapon()
    {
        currentIndex++;
        if (currentIndex >= weapons.Length)
            currentIndex = 0; // 프로토타입: 무기순환으로 무기변경 확인용

        Equip(currentIndex);
    }

    void Equip(int index)
    {
        if (currentWeapon != null)
            Destroy(currentWeapon);

        WeaponData data = weapons[index];

        currentWeapon = Instantiate(data.prefab, weaponRoot);
        currentWeapon.transform.localPosition = data.localPosition;
        currentWeapon.transform.localRotation = Quaternion.Euler(data.localRotation);

        // 총구 자동 연결
        if (autoGun != null)
        {
            autoGun.SetWeapon(data);

            Transform fp = currentWeapon.transform.Find("FirePoint");
            if (fp != null)
                autoGun.firePoint = fp;
        }
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