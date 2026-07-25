using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Owns spawning multiple monster types and bringing each back a few seconds after it dies.
// Add a new MonsterSpawnEntry to spawnEntries to introduce another monster type at this location.
[Serializable]
public class MonsterSpawnEntry
{
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public float respawnDelay = 3f;
}

public class MonsterSpawn : MonoBehaviour
{
    [SerializeField] private List<MonsterSpawnEntry> spawnEntries = new List<MonsterSpawnEntry>();

    private void Start()
    {
        foreach (var entry in spawnEntries)
            SpawnMonster(entry);
    }

    private void SpawnMonster(MonsterSpawnEntry entry)
    {
        Vector3 position = entry.spawnPoint != null ? entry.spawnPoint.position : transform.position;
        var instance = Instantiate(entry.enemyPrefab, position, Quaternion.identity);

        var damageable = instance.GetComponent<IDamageable>();
        if (damageable != null)
            damageable.OnDeath += () => OnMonsterDeath(entry);
    }

    private void OnMonsterDeath(MonsterSpawnEntry entry)
    {
        StartCoroutine(RespawnAfterDelay(entry));
    }

    private IEnumerator RespawnAfterDelay(MonsterSpawnEntry entry)
    {
        yield return new WaitForSeconds(entry.respawnDelay);
        SpawnMonster(entry);
    }
}
