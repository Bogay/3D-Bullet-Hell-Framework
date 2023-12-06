using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    [SerializeField]
    private GameObject enemyPrefab;

    private void Start()
    {
        var ct = gameObject.GetCancellationTokenOnDestroy();
        this.spawnTask(ct).Forget();
    }

    private async UniTaskVoid spawnTask(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Instantiate(this.enemyPrefab, transform);
            await UniTask.Delay(3000 + Random.Range(0, 2000));
        }
    }
}
