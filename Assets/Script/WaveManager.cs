using System.Collections;
using UnityEngine;
using TMPro;

public class WaveSpawnManager : MonoBehaviour
{
    public enum GameMode { Basic, Infinite }

    [Header("Mode")]
    public GameMode mode = GameMode.Basic;

    [Header("Enemy Prefabs")]
    public GameObject enemy1Prefab;
    public GameObject enemy2Prefab;
    public GameObject enemy3Prefab;
    public GameObject enemy4Prefab; // Boss

    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public float waveDuration = 60f;
    public float restDuration = 15f;
    public float spawnInterval = 1.0f;
    public int maxAlive = 35;

    [Header("Camera Cleanup")]
    public Camera targetCamera;
    public string enemyTag = "Enemy";

    [Header("UI")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI restText;

    int currentWave = 1;

    // 웨이브6에서 Enemy4 총 30마리 제한
    int enemy4SpawnedThisWave = 0;
    public int wave6Enemy4Cap = 30;

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        StartCoroutine(WaveLoop());
    }

    IEnumerator WaveLoop()
    {
        while (true)
        {
            // 웨이브 시작 UI
            SetWaveUI(currentWave);
            SetRestUI(false, 0f);

            // 웨이브 진행
            yield return StartCoroutine(RunWave(currentWave));

            // 웨이브 종료 처리
            CleanupEnemiesOutsideCamera();

            // 휴식 시작
            yield return StartCoroutine(RunRest());

            // 다음 웨이브 결정
            currentWave = GetNextWave(currentWave);

            // 기본모드는 6 끝나면 종료
            if (mode == GameMode.Basic && currentWave == -1)
                yield break;
        }
    }

    IEnumerator RunWave(int waveIndex)
    {
        enemy4SpawnedThisWave = 0;

        float timer = waveDuration;
        float spawnTimer = 0f;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            spawnTimer += Time.deltaTime;

            // UI: 남은 웨이브 시간
            SetTimeUI(timer);

            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;

                if (CountAliveEnemies() < maxAlive)
                {
                    SpawnByWaveRule(waveIndex);
                }
            }

            yield return null;
        }

        // 웨이브 끝 UI 정리
        SetTimeUI(0f);
    }

    IEnumerator RunRest()
    {
        float t = restDuration;
        SetRestUI(true, t);

        while (t > 0f)
        {
            t -= Time.deltaTime;
            SetRestUI(true, t);
            yield return null;
        }

        SetRestUI(false, 0f);
    }

    void SetWaveUI(int wave)
    {
        if (waveText != null)
            waveText.text = $"Wave {wave}";
    }

    void SetTimeUI(float seconds)
    {
        if (timeText != null)
            timeText.text = Mathf.CeilToInt(Mathf.Max(0f, seconds)).ToString();
    }

    void SetRestUI(bool show, float seconds)
    {
        if (restText == null) return;

        if (!show)
        {
            restText.text = "";
            return;
        }

        restText.text = $"Rest {Mathf.CeilToInt(Mathf.Max(0f, seconds))}";
    }

    int GetNextWave(int wave)
    {
        if (mode == GameMode.Basic)
        {
            if (wave >= 6) return -1;
            return wave + 1;
        }

        if (wave < 6) return wave + 1;
        return (wave == 6) ? 5 : 6;
    }

    void SpawnByWaveRule(int wave)
    {
        switch (wave)
        {
            case 1:
                Spawn(enemy1Prefab);
                break;

            case 2:
                Spawn(WeightedPick(
                    (enemy1Prefab, 90),
                    (enemy2Prefab, 10)
                ));
                break;

            case 3:
                Spawn(WeightedPick(
                    (enemy1Prefab, 50),
                    (enemy2Prefab, 50)
                ));
                break;

            case 4:
                Spawn(WeightedPick(
                    (enemy1Prefab, 70),
                    (enemy3Prefab, 30)
                ));
                break;

            case 5:
                Spawn(WeightedPick(
                    (enemy1Prefab, 33),
                    (enemy2Prefab, 33),
                    (enemy3Prefab, 34)
                ));
                break;

            case 6:
                if (enemy4SpawnedThisWave < wave6Enemy4Cap)
                {
                    GameObject picked = WeightedPick(
                        (enemy1Prefab, 70),
                        (enemy4Prefab, 30)
                    );

                    if (picked == enemy4Prefab) enemy4SpawnedThisWave++;
                    Spawn(picked);
                }
                else
                {
                    Spawn(enemy1Prefab);
                }
                break;

            default:
                Spawn(WeightedPick(
                    (enemy1Prefab, 33),
                    (enemy2Prefab, 33),
                    (enemy3Prefab, 34)
                ));
                break;
        }
    }

    void Spawn(GameObject prefab)
    {
        if (prefab == null) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];

        Vector3 pos = sp.position;
        pos.z = 0f;
        Instantiate(prefab, pos, Quaternion.identity);
    }

    int CountAliveEnemies()
    {
        return GameObject.FindGameObjectsWithTag(enemyTag).Length;
    }

    void CleanupEnemiesOutsideCamera()
    {
        if (targetCamera == null) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject e = enemies[i];
            if (e == null) continue;

            Vector3 vp = targetCamera.WorldToViewportPoint(e.transform.position);

            bool inFront = vp.z > 0f;
            bool inside =
                vp.x >= 0f && vp.x <= 1f &&
                vp.y >= 0f && vp.y <= 1f;

            if (!(inFront && inside))
            {
                Destroy(e);
            }
        }
    }

    GameObject WeightedPick(params (GameObject prefab, int weight)[] items)
    {
        int total = 0;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].prefab == null) continue;
            if (items[i].weight <= 0) continue;
            total += items[i].weight;
        }
        if (total <= 0) return null;

        int r = Random.Range(0, total);
        int acc = 0;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].prefab == null) continue;
            if (items[i].weight <= 0) continue;

            acc += items[i].weight;
            if (r < acc) return items[i].prefab;
        }
        return items[items.Length - 1].prefab;
    }
}
