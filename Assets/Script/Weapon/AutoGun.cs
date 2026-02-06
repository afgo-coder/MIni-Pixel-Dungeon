using UnityEngine;
using System.Collections;

public class AutoGun : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;
    public Transform weaponRoot;

    [Header("HitMask")]
    public LayerMask bulletHitMask;

    private WeaponData currentWeapon;

    float fireTimer;
    Transform target;

    bool isBursting; // 버스트 중 중복 발사 방지

    public void SetWeapon(WeaponData data)
    {
        currentWeapon = data;
        fireTimer = 0f;
        isBursting = false;
        StopAllCoroutines();
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
            if (enemy == null) continue;

            //죽은 적 제외 (Enemy / Enemy2 둘 다 커버)
            var e1 = enemy.GetComponent<Enemy>();
            if (e1 != null && e1.isDead) continue;

            var e2 = enemy.GetComponent<Enemy2>();
            if (e2 != null && e2.isDead) continue;

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
        if (currentWeapon.bulletPrefab == null) return;

        // fireRate = 발사 간격(초)
        fireTimer += Time.deltaTime;
        if (fireTimer < currentWeapon.fireRate) return;
        fireTimer = 0f;

        if (isBursting) return;

        switch (currentWeapon.shotMode)
        {
            case ShotMode.Single:
                FireOne(firePoint.rotation);
                break;

            case ShotMode.Burst:
                StartCoroutine(BurstFire());
                break;

            case ShotMode.Spread:
                FireSpread();
                break;
        }
    }

    IEnumerator BurstFire()
    {
        isBursting = true;

        int count = Mathf.Max(1, currentWeapon.bulletsPerShot);
        float gap = Mathf.Max(0.01f, currentWeapon.burstInterval);

        for (int i = 0; i < count; i++)
        {
            // 타겟이 중간에 죽어도, 마지막으로 조준된 방향으로 계속 나가게 하려면 Aim을 여기서 안해도 됨.
            // "연사 중에도 계속 추적"하고 싶으면 아래 2줄을 켜면 됨:
            // FindTarget();
            // Aim();

            FireOne(firePoint.rotation);
            yield return new WaitForSeconds(gap);
        }

        isBursting = false;
    }

    void FireSpread()
    {
        int count = Mathf.Max(1, currentWeapon.bulletsPerShot);
        float spread = currentWeapon.spreadAngle;

        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0f : (i / (count - 1f));
            float angleOffset = (count == 1) ? 0f : Mathf.Lerp(-spread * 0.5f, spread * 0.5f, t);

            Quaternion rot = firePoint.rotation * Quaternion.Euler(0, 0, angleOffset);
            FireOne(rot);
        }
    }

    void FireOne(Quaternion rot)
    {
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
                bullet.skin
            );

            bullet.Fire(go.transform.right);
        }
    }
}
