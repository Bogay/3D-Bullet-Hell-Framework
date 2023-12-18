using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

public class EnemySpawn : MonoBehaviour
{
    [SerializeField]
    private List<Vector3> spawnPoints;

    [Inject]
    private System.Func<string, Vector3, GameObject> enemyFactory;

    private int deadCount = 0;

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
            var go = this.enemyFactory("Warrior", toSpawn + offset);
            go.transform.SetParent(transform);
            go.GetCancellationTokenOnDestroy().Register(() =>
            {
                this.deadCount++;
                if (this.deadCount == 10)
                {
                    this.spawnBoss();
                }
            });
            await UniTask.Delay(1000 + Random.Range(0, 2000));
        }
    }

    private void spawnBoss()
    {
        var go = this.enemyFactory("Boss", new Vector3(0, 10, 0));
    }
}
