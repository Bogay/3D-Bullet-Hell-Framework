using UnityEngine;
using BulletHell3D;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using VContainer;
using MessagePipe;
using SpellBound.BulletHell;

public class BulletHellDemo2 : MonoBehaviour
{
    [SerializeField]
    private BHPattern pattern;

    [Space(10)]
    [SerializeField]
    private float minRadius = 5;
    [SerializeField]
    private int patternCount = 25;
    [SerializeField]
    private float rotatePerPattern = 11;
    [SerializeField]
    private float radiusPerPattern = 0.8f;
    [SerializeField]
    private float spawnPatternGap = 0.05f;
    [SerializeField]
    private float patternExpandTime = 0.5f;
    [SerializeField]
    private float waitToDropTime = 1;
    [SerializeField]
    private float dropTime = 1;
    [SerializeField]
    private Ease dropEase;
    [SerializeField]
    private GameObject vfxPrefab;

    private Guid groupId;

    [Inject]
    private readonly ISubscriber<Guid, CollisionEvent> subscriber;

    private void Start()
    {
        this.groupId = Guid.NewGuid();
        this.subscriber.Subscribe(this.groupId, evt =>
        {
            var player = evt.contact.GetComponentInChildren<PlayerController>();
            if (player != null)
            {
                player.Character.Hurt(1);
            }
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2))
            Showcase().Forget();
    }

    public async UniTask Showcase(CancellationToken ct = default)
    {
        GameObject[] objects = new GameObject[patternCount];
        UniTask[] bulletTasks = new UniTask[patternCount];
        for (int i = 0; i < patternCount; i++)
        {
            objects[i] = new GameObject();
            objects[i].transform.SetPositionAndRotation(
                transform.position,
                Quaternion.Euler(0, rotatePerPattern * i, 0)
            );
            objects[i].transform.localScale = Vector3.zero;
            var updater = objects[i].AddComponent<BHTransformVFXUpdater>();
            updater.vfxPrefab = this.vfxPrefab;
            updater.SetPattern(pattern);
            bulletTasks[i] = objects[i].transform
                .DOScale(Vector3.one * (minRadius + i * radiusPerPattern), patternExpandTime)
                .SetLink(objects[i])
                .ToUniTask();
            await UniTask.Delay(TimeSpan.FromSeconds(spawnPatternGap));
        }
        await UniTask.WhenAll(bulletTasks);
        await UniTask.Delay(TimeSpan.FromSeconds(waitToDropTime));

        for (int i = 0; i < patternCount; i++)
        {
            bulletTasks[i] = objects[i].transform
                .DOMoveY(0, dropTime)
                .SetEase(dropEase)
                .SetLink(objects[i])
                .ToUniTask();
            await UniTask.Delay(TimeSpan.FromSeconds(spawnPatternGap));
        }
        await UniTask.WhenAll(bulletTasks);
    }
}
