using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using SpellBound.Core;
using UnityEngine;

namespace SpellBound.Combat
{
    public class SkillTrigger<T> : IDisposable
    {
        public SkillTriggerSetting Setting => this.setting;
        private readonly SkillTriggerSetting setting;
        private readonly Character owner;

        private IDisposablePublisher<T> publisher;
        private ISubscriber<T> subscriber;

        private IDisposablePublisher<int> cooldownPublisher;
        private ISubscriber<int> cooldownSubscriber;
        private bool isCooldownFinished = false;

        private readonly object skillArgLock = new object();
        private T skillArg;
        private bool hasSkillArg;

        public float TriggerTimer { get; private set; }
        private CancellationToken upstreamToken;
        private CancellationTokenSource clearSkillTokenSource;

        public SkillTrigger(SkillTriggerSetting setting, Character owner)
        {
            this.setting = setting;
            this.owner = owner;
            (this.publisher, this.subscriber) = GlobalMessagePipe.CreateEvent<T>();
            (this.cooldownPublisher, this.cooldownSubscriber) = GlobalMessagePipe.CreateEvent<int>();
            this.hasSkillArg = false;
            this.TriggerTimer = this.Setting.CooldownSeconds;
        }

        public void Start(CancellationToken ct)
        {
            this.upstreamToken = ct;
            this.triggerTask(ct).Forget();
            this.AddTo(ct);
        }

        private async UniTaskVoid clearSkill(float clearTime, int clearFrame, CancellationToken ct)
        {
            await UniTask.WhenAll(
                UniTask.WaitForSeconds(clearTime, cancellationToken: ct),
                UniTask.DelayFrame(clearFrame, cancellationToken: ct)
            );

            lock (this.skillArgLock)
            {
                Debug.Log($"clear skill arg. Cooldown: {this.TriggerTimer}");
                this.hasSkillArg = false;
            }
        }

        private bool canCast()
        {
            return this.TriggerTimer <= 0f && this.owner.MP >= this.Setting.Cost;
        }

        private void updateTimer()
        {
            this.TriggerTimer = Mathf.Max(this.TriggerTimer - Time.deltaTime, 0f);
            this.checkCooldown();
        }

        private void checkCooldown()
        {
            if (this.TriggerTimer <= 0f && !this.isCooldownFinished)
            {
                this.isCooldownFinished = true;
                this.cooldownPublisher.Publish(0);
            }
            else if (this.TriggerTimer > 0)
            {
                this.isCooldownFinished = false;
            }
        }

        private async UniTaskVoid triggerTask(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (!this.canCast())
                {
                    this.updateTimer();
                    await UniTask.NextFrame(ct);
                    continue;
                }

                lock (this.skillArgLock)
                {
                    if (this.hasSkillArg)
                    {
                        cast();
                    }
                }
                await UniTask.NextFrame(ct);
            }
        }

        private void cast()
        {
            this.publisher.Publish(this.skillArg);
            this.hasSkillArg = false;
            this.TriggerTimer = this.Setting.CooldownSeconds;
            this.owner.Cast(this.Setting.Cost);
            this.rotateClearSkillTokenSource();
        }

        private CancellationTokenSource rotateClearSkillTokenSource()
        {
            this.clearSkillTokenSource?.Cancel();
            this.clearSkillTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.upstreamToken);
            return this.clearSkillTokenSource;
        }

        public void Trigger(T arg)
        {
            lock (this.skillArgLock)
            {
                this.hasSkillArg = true;
                this.skillArg = arg;

                var src = this.rotateClearSkillTokenSource();
                // TODO: configurable clear interval
                this.clearSkill(0.05f, 2, src.Token).Forget();

                if (this.canCast())
                {
                    this.cast();
                }
            }
        }

        public IDisposable Subscribe(Action<T> handler)
        {
            return this.subscriber.Subscribe(handler);
        }

        public IDisposable OnCooldownFinished(Action handler)
        {
            return this.cooldownSubscriber.Subscribe(_ => handler());
        }

        public void Dispose()
        {
            this.publisher?.Dispose();
            this.clearSkillTokenSource?.Dispose();
            this.cooldownPublisher?.Dispose();
        }
    }
}

