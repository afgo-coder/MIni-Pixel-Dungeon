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

    [Header("Spawn Random Range")]
    public float horizontalSpawnRange = 8f; // 위/아래에서 좌우 퍼짐
    public float verticalSpawnRange = 4.5f; // 좌/우에서 위아래 퍼짐


    // Wave 6 클리어 이벤트
    public System.Action OnGameClear;

    //int currentWave = 1;
    int displayWave = 1;
    int ruleWave = 1;

    // 웨이브6에서 Enemy4 총 30마리 제한
    int enemy4SpawnedThisWave = 0;
    public int wave6Enemy4Cap = 30;

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        int saved = PlayerPrefs.GetInt(GameModeData.ModeKey, 0);
        mode = (saved == 1) ? GameMode.Infinite : GameMode.Basic;
        StartCoroutine(WaveLoop());
        Debug.Log($"[WaveSpawnManager] mode = {mode}");
    }

    IEnumerator WaveLoop()
    {
        while (true)
        {
            SetWaveUI(displayWave);
            SetRestUI(false, 0f);

            // ✅ 웨이브 진행: 이제 "시간 끝 + 남은 적 전부 처치"까지 여기서 기다림
            yield return StartCoroutine(RunWave(ruleWave));

            // 웨이브 종료 처리
            CleanupEnemiesOutsideCamera();

            // ✅ Basic 마지막 웨이브: 남은 적을 전부 잡고 RunWave가 끝났으면 -> 2초 뒤 클리어
            if (mode == GameMode.Basic && displayWave >= 6)
            {
                var ps = FindFirstObjectByType<PlayerStats>();
                if (ps != null)
                {
                    // 플레이어가 죽었다면 클리어 안 띄우고 종료
                    if (ps.IsDead) yield break;

                    ps.MarkGameEnded();
                }

                var dmg = FindFirstObjectByType<PlayerDamageReceiver>();
                if (dmg != null) dmg.enabled = false;

                // ✅ 2초 뒤 클리어 UI (시간 정지/일시정지 영향을 안 받게 Realtime 추천)
                yield return new WaitForSecondsRealtime(2f);

                OnGameClear?.Invoke();
                yield break;
            }

            // ✅ 마지막 웨이브가 아니면, 적 전부 처치 후 여기로 와서 Rest 진행
            yield return StartCoroutine(RunRest());

            displayWave++;

            if (mode == GameMode.Infinite)
            {
                if (displayWave <= 6) ruleWave = displayWave;
                else ruleWave = (ruleWave == 6) ? 5 : 6;
            }
            else
            {
                ruleWave = displayWave;
            }
        }
    }

    IEnumerator RunWave(int waveIndex)
    {
        enemy4SpawnedThisWave = 0;

        float timer = waveDuration;
        float spawnTimer = 0f;

        // -------------------------
        // 1) 타이머 동안: 스폰 + 시간 UI
        // -------------------------
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            spawnTimer += Time.deltaTime;

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

        // 타이머 종료 UI 정리
        SetTimeUI(0f);

        // -------------------------
        // 2) 타이머 끝난 뒤: 스폰 중단, 남은 적 전부 처치될 때까지 대기
        // -------------------------
        while (CountAliveEnemies() > 0)
        {
            // 플레이어 죽었으면 더 진행할 의미 없으니 종료
            var ps = FindFirstObjectByType<PlayerStats>();
            if (ps != null && ps.IsDead) yield break;

            yield return null;
        }

        // 여기까지 오면 "그 웨이브의 적을 전부 잡음"
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

        // 이름 기준으로 방향 판별 (Top/Bottom/Left/Right)
        string n = sp.name.ToLower();

        if (n.Contains("top") || n.Contains("bottom"))
        {
            pos.x += Random.Range(-horizontalSpawnRange, horizontalSpawnRange);
        }
        else if (n.Contains("left") || n.Contains("right"))
        {
            pos.y += Random.Range(-verticalSpawnRange, verticalSpawnRange);
        }

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
