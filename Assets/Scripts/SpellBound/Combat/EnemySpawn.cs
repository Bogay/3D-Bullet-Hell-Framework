using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    [SerializeField]
    private GameObject enemyPrefab;
    [SerializeField]
    private List<Vector3> spawnPoints;

    private void Start()
    {
        var ct = gameObject.GetCancellationTokenOnDestroy();
        this.spawnTask(ct).Forget();
    }

    private async UniTaskVoid spawnTask(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var toSpawn = this.spawnPoints[Random.Range(0, this.spawnPoints.Count)];
            var offset = Random.insideUnitSphere;
            offset.y = 0;
            var go = Instantiate(this.enemyPrefab, toSpawn + offset, Quaternion.identity);
            go.transform.SetParent(transform);
            await UniTask.Delay(1000 + Random.Range(0, 2000));
        }
    }
}
