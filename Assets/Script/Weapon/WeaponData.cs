using UnityEngine;

public enum ShotMode
{
    Single,  // 1발
    Burst,   // 연사(빠르게 여러 발)
    Spread   // 샷건처럼 퍼짐
}

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
    public int bulletsPerShot = 1;  // Burst일 때: 연사 횟수 / Spread일 때: 펠릿 수
    public float fireRate = 3f;     // 초당 발사수(Shots Per Second). 3이면 1초에 3번 "발사 시도"
    public float range = 10f;
    public int pierce = 0;
    public float bulletSpeed = 10f;
    public float bulletLifeTime = 3f;

    [Header("Mode")]
    public ShotMode shotMode = ShotMode.Single;

    [Header("Burst")]
    public float burstInterval = 0.05f; // 연사 내부 간격(초)

    [Header("Spread (Shotgun etc)")]
    public float spreadAngle = 0f;      // Spread일 때만 의미 있음
}
