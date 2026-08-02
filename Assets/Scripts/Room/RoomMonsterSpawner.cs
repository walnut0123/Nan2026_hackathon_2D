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

public class RoomMonsterSpawner : MonoBehaviour
{
    [SerializeField] private List<RoomSpawnEntry> spawnEntries = new List<RoomSpawnEntry>();

    [Tooltip("이미 씬에 배치되어 있는 적(보스 등) - Instantiate하지 않고 그대로 두되, 죽는 것만 추적한다. " +
        "씬에는 비활성 상태로 놔두면 방이 잠기는 시점에 SpawnAll()이 활성화한다.")]
    [SerializeField] private List<GameObject> preplacedEnemies = new List<GameObject>();

    private int aliveCount;

    // RoomController가 입장 시점에 "이 방이 몬스터가 있는 방인지"를 미리 판단하기 위해 쓴다
    // (스폰하기 전에도 알 수 있어야 하므로 카운트만으로 판단한다).
    public bool HasMonsters => spawnEntries.Count > 0 || preplacedEnemies.Count > 0;

    public event Action OnAllMonstersDefeated;

    public void SpawnAll()
    {
        aliveCount = 0;

        foreach (var entry in spawnEntries)
        {
            if (entry.enemyPrefab == null)
                continue;

            Vector3 position = entry.spawnPoint != null ? entry.spawnPoint.position : transform.position;
            var instance = Instantiate(entry.enemyPrefab, position, Quaternion.identity);
            TrackEnemy(instance);
        }

        foreach (var enemy in preplacedEnemies)
        {
            if (enemy != null)
                TrackEnemy(enemy);
        }

        if (aliveCount <= 0)
            OnAllMonstersDefeated?.Invoke();
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

    private void HandleMonsterDeath()
    {
        aliveCount--;
        if (aliveCount <= 0)
            OnAllMonstersDefeated?.Invoke();
    }
}
