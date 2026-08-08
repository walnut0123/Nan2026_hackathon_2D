using System;
using System.Collections.Generic;
using UnityEngine;

// 방 전용 1회성 스폰러. 기존 MonsterSpawn.cs(죽으면 일정 시간 뒤 무한 리스폰하는 파밍용
// 설계)와는 목적이 달라서 별도로 분리했다 - 이쪽은 방이 잠기는 시점에 한 번 스폰하고,
// 전부 죽으면 다시는 스폰하지 않는다.
[Serializable]
public class RoomSpawnEntry
{
    public GameObject enemyPrefab;
    public Transform spawnPoint;
}

// 웨이브 하나 = 동시에 등장하는 몬스터 묶음. 이전 웨이브가 전멸해야 다음 웨이브가 스폰된다.
// 웨이브를 1개만 쓰면 예전처럼 전부 한 번에 나오는 것과 동일하다.
[Serializable]
public class RoomWave
{
    public List<RoomSpawnEntry> entries = new List<RoomSpawnEntry>();
}

public class RoomMonsterSpawner : MonoBehaviour
{
    [Tooltip("웨이브 단위로 나뉜 몬스터 스폰 목록. 한 웨이브가 전멸하면 다음 웨이브가 이어서 스폰된다.")]
    [SerializeField] private List<RoomWave> waves = new List<RoomWave>();

    [Tooltip("이미 씬에 배치되어 있는 적(보스 등) - Instantiate하지 않고 그대로 두되, 죽는 것만 추적한다. " +
        "씬에는 비활성 상태로 놔두면 방이 잠기는 시점에 웨이브와 함께 활성화된다.")]
    [SerializeField] private List<GameObject> preplacedEnemies = new List<GameObject>();

    [Header("개발 테스트 모드")]
    [Tooltip("켜면 위 웨이브/사전배치 목록을 전부 무시하고 체력 1인 몬스터 1마리만 스폰한다 - 잡으면 바로 " +
        "방이 클리어된다. 방을 빠르게 통과하고 싶을 때만 방 단위로 켠다(예: 방4 테스트 중엔 방2·3만 켜기). " +
        "보스방처럼 필요 없는 방은 꺼둔다.")]
    [SerializeField] private bool devTestMode = false;
    [Tooltip("개발 테스트 모드에서 스폰할 몬스터 프리팹. 비워두면 1웨이브의 첫 프리팹을 재사용한다.")]
    [SerializeField] private GameObject devTestEnemyPrefab;
    [Tooltip("개발 테스트 몬스터가 스폰될 위치. 비워두면 이 오브젝트 위치를 사용한다.")]
    [SerializeField] private Transform devTestSpawnPoint;

    private int currentWaveIndex;
    private int aliveCount;
    private bool preplacedSpawned;

    // RoomController가 입장 시점에 "이 방이 몬스터가 있는 방인지"를 미리 판단하기 위해 쓴다
    // (스폰하기 전에도 알 수 있어야 하므로 카운트만으로 판단한다).
    public bool HasMonsters => devTestMode || HasAnyWaveEntry() || preplacedEnemies.Count > 0;

    public event Action OnAllMonstersDefeated;

    private bool HasAnyWaveEntry()
    {
        foreach (var wave in waves)
            if (wave.entries.Count > 0)
                return true;
        return false;
    }

    public void SpawnAll()
    {
        currentWaveIndex = 0;
        aliveCount = 0;
        preplacedSpawned = false;

        if (devTestMode)
        {
            SpawnDevTestEnemy();
            return;
        }

        SpawnPreplaced();
        SpawnWave(currentWaveIndex);

        if (aliveCount <= 0)
            OnAllMonstersDefeated?.Invoke();
    }

    private void SpawnDevTestEnemy()
    {
        GameObject prefab = devTestEnemyPrefab != null ? devTestEnemyPrefab : FindFirstConfiguredPrefab();
        if (prefab == null)
        {
            Debug.LogWarning($"[RoomMonsterSpawner] {name}: 개발 테스트용 프리팹을 찾을 수 없습니다.");
            OnAllMonstersDefeated?.Invoke();
            return;
        }

        Vector3 position = devTestSpawnPoint != null ? devTestSpawnPoint.position : transform.position;
        var instance = Instantiate(prefab, position, Quaternion.identity);

        var health = instance.GetComponent<Health>();
        if (health != null)
            health.SetMaxHealthAndFullHeal(1f);

        TrackEnemy(instance);

        if (aliveCount <= 0)
            OnAllMonstersDefeated?.Invoke();
    }

    private GameObject FindFirstConfiguredPrefab()
    {
        foreach (var wave in waves)
            foreach (var entry in wave.entries)
                if (entry.enemyPrefab != null)
                    return entry.enemyPrefab;
        return null;
    }

    private void SpawnPreplaced()
    {
        if (preplacedSpawned)
            return;
        preplacedSpawned = true;

        foreach (var enemy in preplacedEnemies)
        {
            if (enemy != null)
                TrackEnemy(enemy);
        }
    }

    private void SpawnWave(int index)
    {
        if (index < 0 || index >= waves.Count)
            return;

        foreach (var entry in waves[index].entries)
        {
            if (entry.enemyPrefab == null)
                continue;

            Vector3 position = entry.spawnPoint != null ? entry.spawnPoint.position : transform.position;
            var instance = Instantiate(entry.enemyPrefab, position, Quaternion.identity);
            TrackEnemy(instance);
        }
    }

    private void TrackEnemy(GameObject enemy)
    {
        var damageable = enemy.GetComponent<IDamageable>();
        if (damageable == null)
            return;

        enemy.SetActive(true);
        aliveCount++;
        damageable.OnDeath += HandleMonsterDeath;
    }

    // 현재 무리(사전배치 + 진행 중인 웨이브)가 전멸할 때마다 호출된다. 다음 웨이브가 남아있으면
    // 이어서 스폰하고, 없으면 방을 클리어 처리한다. 다음 웨이브가 스폰 항목 없이 비어있는 등의
    // 예외 상황에서도 멈추지 않도록 재귀적으로 다음 단계를 확인한다.
    private void HandleMonsterDeath()
    {
        aliveCount--;
        if (aliveCount > 0)
            return;

        currentWaveIndex++;
        if (currentWaveIndex < waves.Count)
        {
            SpawnWave(currentWaveIndex);
            if (aliveCount <= 0)
                HandleMonsterDeath();
            return;
        }

        OnAllMonstersDefeated?.Invoke();
    }
}
