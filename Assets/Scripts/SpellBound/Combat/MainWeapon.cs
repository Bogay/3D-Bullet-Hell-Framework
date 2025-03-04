using System;
using System.Collections;
using System.Collections.Generic;
using Bogay.SceneAudioManager;
using BulletHell3D;
using MessagePipe;
using SpellBound.BulletHell;
using SpellBound.Core;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;

namespace SpellBound.Combat
{
    public class MainWeapon : MonoBehaviour
    {
        [SerializeField]
        private BHPattern pattern;
        [SerializeField]
        private GameObject vfxPrefab;
        [SerializeField]
        private string sfxName;

        [SerializeField]
        private SkillTriggerSetting skillTriggerSetting;
        private SkillTrigger<Vector3> skillTrigger;

        // TODO: maybe use DI to collect these config in one place?
        [SerializeField]
        private float distance;
        [SerializeField]
        private float speed;
        [SerializeField]
        private DamageNumber damageNumber;

        public float Heat { get; private set; }
        public float HeatNormalized { get => this.Heat / MainWeapon.MAX_HEAT; }

        private const float MAX_HEAT = 100;

        private System.Guid groupId;
        public float ShootCooldownSeconds { get => this.skillTrigger.Setting.CooldownSeconds; }
        public int Cost { get => this.skillTrigger.Setting.Cost; }
        public float ShootTimer { get => this.skillTrigger.TriggerTimer; }

        [Inject]
        private readonly ISubscriber<System.Guid, CollisionEvent> subscriber;
        [Inject]
        private readonly Character owner;
        [Inject]
        private readonly CollisionGroups collisionGroups;

        private void Awake()
        {
            this.groupId = System.Guid.NewGuid();
            Debug.Log($"main weapon guid: {this.groupId}");
            this.skillTrigger = new SkillTrigger<Vector3>(
                this.skillTriggerSetting,
                this.owner
            );
        }

        private void Start()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            this.subscriber.Subscribe(this.groupId, evt =>
            {
                var layer = 1 << evt.contact.layer;
                // TODO: make it usable on enemy (i.e. don't hard-coded enemy mask / owner)
                if ((layer & this.collisionGroups.enemyMask) != 0)
                {
                    Debug.Log("Hit enemy");
                    int dmg = Mathf.FloorToInt(this.owner.Power.Value() * (1.2f + this.HeatNormalized));

                    var num = Instantiate(this.damageNumber);
                    num.transform.position = new Vector3(
                        evt.hit.point.x,
                        evt.hit.collider.bounds.max.y + 3,
                        evt.hit.point.z
                    );
                    num.transform.position += UnityEngine.Random.insideUnitSphere;
                    num.Value = dmg;

                    var controller = evt.contact.GetComponent<EnemyController>();
                    if (controller != null)
                    {
                        controller.character.Hurt(dmg);
                    }
                    else
                    {
                        var bossController = evt.contact.GetComponent<BossEnemyController>();
                        bossController.character.Hurt(dmg);
                    }
                }
            }).AddTo(ct);
            this.skillTrigger.Subscribe(fwd => StartCoroutine(this.shootCoro(fwd))).AddTo(ct);
            this.skillTrigger.Start(ct);
        }

        private void Update()
        {
            this.Heat = Mathf.Max(0, this.Heat - 20 * Time.deltaTime);
        }

        public void Shoot(Vector3 forward)
        {
            this.skillTrigger.Trigger(forward);
        }

        private IEnumerator shootCoro(Vector3 forward)
        {
            forward.Normalize();
            this.Heat = Mathf.Min(this.Heat + 10, MainWeapon.MAX_HEAT);

            var go = new GameObject("Bullet");
            go.transform.position = transform.position + forward * distance;
            go.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            var updater = go.AddComponent<BHTransformVFXUpdater>();
            updater.groupId = this.groupId;
            updater.SetPattern(this.pattern);
            updater.vfxPrefab = this.vfxPrefab;

            SceneAudioManager.instance.PlayByName(this.sfxName);

            while (go != null)
            {
                go.transform.position += forward * (this.speed * Time.deltaTime);
                yield return null;
            }
        }

        public IDisposable Subscribe(Action<Vector3> action)
        {
            return this.skillTrigger.Subscribe(action);
        }

        public IDisposable OnCooldownFinished(Action handler)
        {
            return this.skillTrigger.OnCooldownFinished(handler);
        }
    }
}
