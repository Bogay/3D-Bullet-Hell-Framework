using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SpellBound.Combat;
using SpellBound.Core;
using UnityEngine;
using VContainer;

public class Dash : MonoBehaviour
{
    [SerializeField]
    private float velocity;
    [SerializeField]
    private float duration;

    [field: SerializeField]
    public SkillTriggerSetting skillTriggerSetting { get; private set; }
    private SkillTrigger<Vector3> skillTrigger;

    // TODO: DI
    [SerializeField]
    private PlayerController playerController;

    [Inject]
    private readonly Character owner;

    private List<(Action<Vector3>, CancellationToken)> subscribeRequests = new List<(Action<Vector3>, CancellationToken)>();

    void Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        this.skillTrigger = new SkillTrigger<Vector3>(
            this.skillTriggerSetting,
            this.owner
        );
        this.skillTrigger.AddTo(ct);

        this.skillTrigger.Subscribe(fwd =>
        {
            this.playerController.Dash(
                fwd * this.velocity,
                duration: this.duration,
                cancellationToken: ct
            ).Forget();
        }).AddTo(ct);
        foreach (var (action, token) in this.subscribeRequests)
        {
            this.Subscribe(action).AddTo(token);
        }
        this.skillTrigger.Start(ct);
    }

    public void Cast(Vector3 forward)
    {
        forward.y = 0;
        forward = forward.normalized;
        this.skillTrigger.Trigger(forward);
    }

    public IDisposable Subscribe(Action<Vector3> action)
    {
        return this.skillTrigger.Subscribe(action);
    }

    public void QueueSubscibe(Action<Vector3> action, CancellationToken ct = default)
    {
        if (this.skillTrigger != null)
        {
            this.Subscribe(action).AddTo(ct);
            return;
        }

        this.subscribeRequests.Add((action, ct));
    }
}
