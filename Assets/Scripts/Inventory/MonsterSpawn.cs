using System.Collections;
using UnityEngine;

// Owns spawning the monster and bringing it back a few seconds after it dies.
public class MonsterSpawn : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float respawnDelay = 3f;

    private void Start()
    {
        SpawnMonster();
    }

    private void SpawnMonster()
    {
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        var instance = Instantiate(enemyPrefab, position, Quaternion.identity);

        var damageable = instance.GetComponent<IDamageable>();
        if (damageable != null)
            damageable.OnDeath += OnMonsterDeath;
    }

    private void OnMonsterDeath()
    {
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnMonster();
    }
}
