using UnityEngine;

[System.Serializable]
public class WeaponData
{
    [Header("Info")]
    public string weaponName;

    [Header("View / Equip")]
    public GameObject prefab;
    public Vector3 localPosition;
    public Vector3 localRotation;
    public Vector3 localScale = Vector3.one;

    [Header("Shoot")]
    public GameObject bulletPrefab;
    public int damage = 1;
    public int bulletsPerShot = 1;   // 샷건 펠릿 수 같은 개념
    public float fireRate = 3f;      // 초당 발사수 (3이면 1초에 3발)
    public float range = 10f;
    public int pierce = 0;          // 관통 수
    public float bulletSpeed = 10f;
    public float bulletLifeTime = 3f;

    [Header("Spread (Shotgun etc)")]
    public float spreadAngle = 0f;   // 총알 퍼짐(도). 권총/라이플=0, 샷건=예: 15~25
}