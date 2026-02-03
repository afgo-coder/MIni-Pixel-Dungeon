using UnityEngine;

public class AutoGun : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;
    public Transform weaponRoot;

    [Header("HitMask")]
    public LayerMask bulletHitMask; // Enemy, Wall 같은 레이어 넣기

    private WeaponData currentWeapon;

    float fireTimer;
    Transform target;

    public void SetWeapon(WeaponData data)
    {
        currentWeapon = data;
        fireTimer = 0f;
    }

    void Update()
    {
        if (currentWeapon == null) return;
        if (firePoint == null || weaponRoot == null) return;

        FindTarget();
        Aim();
        Shoot();
    }

    void FindTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        float minDist = Mathf.Infinity;
        Transform nearest = null;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);

            if (dist < minDist && dist <= currentWeapon.range)
            {
                minDist = dist;
                nearest = enemy.transform;
            }
        }

        target = nearest;
    }

    void Aim()
    {
        if (target == null) return;

        Vector2 dir = target.position - weaponRoot.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        weaponRoot.rotation = Quaternion.Euler(0, 0, angle);

        float absAngle = Mathf.DeltaAngle(0f, angle);
        Vector3 s = weaponRoot.localScale;

        if (absAngle > 90f || absAngle < -90f)
            s.y = -Mathf.Abs(s.y);
        else
            s.y = Mathf.Abs(s.y);

        weaponRoot.localScale = s;
    }

    void Shoot()
    {
     
        if (target == null) return;

        fireTimer += Time.deltaTime;

        if (fireTimer < currentWeapon.fireRate) return;
        fireTimer = 0f;

        if (currentWeapon.bulletPrefab == null) return;

        int count = Mathf.Max(1, currentWeapon.bulletsPerShot);
        float spread = currentWeapon.spreadAngle;

        // 중앙 기준 좌우로 퍼지게
        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0f : (i / (count - 1f));   // 0~1
            float angleOffset = (count == 1) ? 0f : Mathf.Lerp(-spread * 0.5f, spread * 0.5f, t);

            Quaternion rot = firePoint.rotation * Quaternion.Euler(0, 0, angleOffset);

            GameObject go = Instantiate(currentWeapon.bulletPrefab, firePoint.position, rot);

            var bullet = go.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.Init(
                    currentWeapon.damage,
                    currentWeapon.pierce,
                    currentWeapon.bulletSpeed,
                    currentWeapon.bulletLifeTime,
                    bulletHitMask,
                    bullet.skin // 기존 값 유지하고 싶으면 이렇게
                );

                bullet.Fire(go.transform.right);
            }
        }
    }
}

