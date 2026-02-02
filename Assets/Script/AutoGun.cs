using UnityEngine;

public class AutoGun : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public Transform weaponRoot;
    public float range = 10f;
    public float fireRate = 0.5f;

    float fireTimer;

    Transform target;

    void Update()
    {
        FindTarget();
        Aim();
        Shoot();
    }

    // 가장 가까운 적 찾기
    void FindTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        float minDist = Mathf.Infinity;
        Transform nearest = null;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);

            if (dist < minDist && dist <= range)
            {
                minDist = dist;
                nearest = enemy.transform;
            }
        }

        target = nearest;
    }

    // 조준
    void Aim()
    {
        if (target == null) return;

        Vector2 dir = target.position - weaponRoot.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        weaponRoot.rotation = Quaternion.Euler(0, 0, angle);

        float absAngle = Mathf.DeltaAngle(0f, angle); // -180~180

        Vector3 s = weaponRoot.localScale;
        if (absAngle > 90f || absAngle < -90f)
            s.y = -Mathf.Abs(s.y);   // 아래로 뒤집기 (거꾸로 방지)
        else
            s.y = Mathf.Abs(s.y);

        weaponRoot.localScale = s;
    }

    // 발사
    void Shoot()
    {
        if (target == null) return;

        fireTimer += Time.deltaTime;

        if (fireTimer >= fireRate)
        {
            fireTimer = 0;

            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        }
    }
}
