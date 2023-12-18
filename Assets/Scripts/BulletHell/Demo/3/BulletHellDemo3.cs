using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BulletHell3D;
using DG.Tweening;
using VContainer;
using MessagePipe;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;
using SpellBound.Combat;

public class BulletHellDemo3 : MonoBehaviour
{
    [SerializeField]
    private BHRenderObject demoRenderObj;
    [SerializeField]
    private BHCustomUpdater demoUpdater;
    [SerializeField]
    private float burstBulletCount = 60;

    [Space(10)]
    [SerializeField]
    private BHPattern pattern;
    [SerializeField]
    private int spawnPatternCount;
    [SerializeField]
    private float spawnPatternGap;
    [SerializeField]
    private float scale;
    [SerializeField]
    private float highSpeed;
    [SerializeField]
    private float lowSpeed;
    [SerializeField]
    private float speedDownTime;
    [SerializeField]
    private float speedUpTime;
    [SerializeField]
    private float gapTime;
    [SerializeField]
    private Ease speedDownEase;
    [SerializeField]
    private Ease speedUpEase;

    [Inject]
    private Player player;

    private System.Guid groupId;

    [Inject]
    private readonly ISubscriber<System.Guid, CollisionEvent> subscriber;

    private void Start()
    {
        this.groupId = System.Guid.NewGuid();
        this.subscriber.Subscribe(this.groupId, evt =>
        {
            Debug.Log("demo3 hit");
            var player = evt.contact.GetComponentInChildren<PlayerController>();
            if (player != null)
            {
                player.Character.Hurt(1);
            }

            if (evt.contact.GetComponentInChildren<EnemyController>() != null)
            {
                evt.bullet.isAlive = true;
            }
        });
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3))
            Showcase(this.GetCancellationTokenOnDestroy()).Forget();
    }

    public async UniTask Showcase(CancellationToken ct = default)
    {
        for (int i = 0; i < burstBulletCount; i++)
            demoUpdater.AddBullet(demoRenderObj, transform.position, UnityEngine.Random.insideUnitSphere);
        UniTask[] tasks = new UniTask[this.spawnPatternCount];
        for (int i = 0; i < spawnPatternCount; i++)
        {
            tasks[i] = this.CreatePattern(ct);
            await UniTask.Delay(TimeSpan.FromSeconds(this.spawnPatternGap), cancellationToken: ct);
        }

        await UniTask.WhenAll(tasks);
    }

    async UniTask CreatePattern(CancellationToken ct = default)
    {
        GameObject go = new GameObject();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, go.GetCancellationTokenOnDestroy());

        go.transform.position = transform.position;
        Vector3 toPlayer = player.transform.position - transform.position;
        Vector3 newForward = new Vector3(toPlayer.x, 0, toPlayer.z).normalized + Vector3.up * UnityEngine.Random.Range(-0.15f, -0.05f);
        go.transform.rotation = Quaternion.LookRotation(newForward, new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 1, UnityEngine.Random.Range(-0.4f, 0.4f)));
        go.transform.localScale = Vector3.zero;

        BHTransformUpdater updater = go.AddComponent<BHTransformUpdater>();
        updater.groupId = this.groupId;
        updater.SetPattern(pattern);

        float speed = highSpeed;
        var speedDownCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
        speedDownCts.CancelAfterSlim(TimeSpan.FromSeconds(speedDownTime));

        try
        {
            await UniTask.WhenAll(
                go.transform
                    .DOScale(Vector3.one * scale, speedDownTime)
                    .SetEase(speedDownEase)
                    .ToUniTask(cancellationToken: speedDownCts.Token),
                DOTween.To(
                        () => speed,
                        x => speed = x,
                        lowSpeed,
                        speedDownTime
                    )
                    .SetEase(speedDownEase)
                    .ToUniTask(cancellationToken: speedDownCts.Token),
                this.moveForward(go.transform, () => go.transform.forward * speed, speedDownCts.Token)
            );
        }
        catch (OperationCanceledException e) when (e.CancellationToken == speedDownCts.Token)
        {
            Debug.Log("to low speed");
        }

        var gapCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
        gapCts.CancelAfterSlim(TimeSpan.FromSeconds(this.gapTime));
        try
        {
            await this.moveForward(go.transform, () => go.transform.forward * speed, gapCts.Token);
        }
        catch (OperationCanceledException e) when (e.CancellationToken == gapCts.Token)
        {
            Debug.Log("gap end");
        }

        this.moveForward(go.transform, () => go.transform.forward * speed, cts.Token).Forget();

        await DOTween.To(
              () => speed,
              x => speed = x,
              highSpeed,
              speedUpTime
          )
          .SetEase(speedUpEase)
          .ToUniTask(cancellationToken: cts.Token);
    }

    private async UniTask moveForward(Transform tf, Func<Vector3> fwd, CancellationToken ct)
    {
        while (true)
        {
            tf.position += fwd() * Time.deltaTime;
            await UniTask.NextFrame(ct);
        }
    }
}
