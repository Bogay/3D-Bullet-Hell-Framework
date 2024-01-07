using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SpellBound.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

public class EnemySpawn : MonoBehaviour
{
    [SerializeField]
    private List<Vector3> spawnPoints;

    // TODO: DI
    [SerializeField]
    private Dialog dialog;

    [Inject]
    private System.Func<string, Vector3, GameObject> enemyFactory;

    private int deadCount = 0;

    private void Start()
    {
        var ct = gameObject.GetCancellationTokenOnDestroy();
        this.startAsync(ct).Forget();
    }

    private async UniTask startAsync(CancellationToken ct)
    {
        string[] dialogue = {
            "Welcome, Elara, to the realm now cloaked in shadows.",
            "Your feeble attempts to reclaim this land shall be in vain.",
            "My undead legions hunger for the magic that flows within you.",
            "Witness the darkness that shall consume your world,",
            "for I am the Lich Lord, and your futile resistance merely amuses me."
        };
        await this.playBossDialogue(dialogue, ct);

        await this.spawnTask(ct);
    }

    private async UniTask spawnTask(CancellationToken ct)
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
                if (this.deadCount == 5)
                {
                    this.spawnBoss(ct).Forget();
                }
            });
            await UniTask.Delay(1000 + Random.Range(0, 2000));
        }
    }

    private async UniTask spawnBoss(CancellationToken ct)
    {
        string[] dialogue = {
            "Your feeble victories against my minions amuse me, Elara.",
            "Witness the true might of the Lich Lord!",
            "I descend upon your whimsical world, an unstoppable force.",
            "Your magic is powerless against my darkness.",
            "Prepare for your ultimate defeat, for I, the Lich Lord, am now your inevitable end."
        };
        await this.playBossDialogue(dialogue, ct);

        var go = this.enemyFactory("Boss", new Vector3(0, 10, 0));
        go.GetCancellationTokenOnDestroy().Register(() =>
        {
            SceneManager.LoadScene("Win");
        });
    }

    private async UniTask playBossDialogue(string[] dialogue, CancellationToken ct)
    {
        this.dialog.gameObject.SetActive(true);
        await UniTask.NextFrame(ct);

        const string BOSS_NAME = "Lich Lord";
        foreach (var d in dialogue)
        {
            this.dialog.SetMessage(BOSS_NAME, d);
            await UniTask.WaitForSeconds(d.Length * 0.08f, cancellationToken: ct);
        }
        this.dialog.gameObject.SetActive(false);
    }
}
