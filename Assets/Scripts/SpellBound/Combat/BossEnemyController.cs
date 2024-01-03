using System.Threading;
using System.Collections;
using System.Collections.Generic;
using Bogay.SceneAudioManager;
using Cysharp.Threading.Tasks;
using SpellBound.Core;
using UnityEngine;
using VContainer;
using System;

namespace SpellBound.Combat
{
    public class BossEnemyController : MonoBehaviour
    {
        [field: SerializeField]
        public Character character { get; private set; }

        [SerializeField]
        private GameObject deathVFX;

        private ModelColorBlink blink;

        [Inject]
        private readonly PlayerController playerController;
        [Inject]
        private readonly CollisionGroups collisionGroups;

        [SerializeField]
        private float moveSpeed = .5f;

        [Header("Juice")]
        [SerializeField]
        [Range(0.5f, 2f)]
        private float jumpScale;
        [SerializeField]
        [Range(0f, 1f)]
        private float resumeFactor;

        [Header("Bullet Hell")]
        [SerializeField]
        private float demo2Offset = 5;
        [SerializeField]
        private float demo3Offset = 1;

        private Vector3 originalScale = Vector3.one;

        private Transform playerTransform;
        private CharacterController controller;

        private BulletHellDemo1 demo1;
        private BulletHellDemo2 demo2;
        private BulletHellDemo3 demo3;

        void Start()
        {
            this.character = ScriptableObject.Instantiate(this.character);
            this.character.Init();

            var ct = this.GetCancellationTokenOnDestroy();
            this.originalScale = transform.localScale;
            this.character.OnHurt(_ =>
            {
                this.blink.BlinkAll(ct).Forget();
                transform.localScale = new Vector3(
                    this.originalScale.x / this.jumpScale,
                    this.originalScale.y * this.jumpScale,
                    this.originalScale.z);
            });

            this.controller = GetComponent<CharacterController>();
            this.playerTransform = playerController.GetComponent<Transform>();
            this.blink = GetComponent<ModelColorBlink>();
            this.demo1 = FindObjectOfType<BulletHellDemo1>();
            this.demo2 = FindObjectOfType<BulletHellDemo2>();
            this.demo3 = FindObjectOfType<BulletHellDemo3>();

            this.startAsync(ct).Forget();
        }

        void Update()
        {
            if (this.character.HP <= 0)
            {
                Instantiate(this.deathVFX, transform.position, Quaternion.identity);
                SceneAudioManager.instance.PlayByName("EnemyDeath");
                Destroy(gameObject);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.K))
                this.character.Hurt(9999);
            if (Input.GetKeyDown(KeyCode.I))
            {
                this.demo3.transform.position = transform.position + Vector3.up * this.demo3Offset;
                this.demo3.Showcase(this.GetCancellationTokenOnDestroy()).Forget();
            }
            if (Input.GetKeyDown(KeyCode.U))
            {
                this.demo2.transform.position = transform.position + Vector3.up * this.demo2Offset;
                this.demo2.Showcase(this.GetCancellationTokenOnDestroy()).Forget();
            }
#endif
            transform.localScale = Vector3.Lerp(transform.localScale, this.originalScale, this.resumeFactor);
        }

        private void MoveToPlayer()
        {
            var direction = (playerTransform.position - transform.position).normalized;
            direction.y = 0;
            var g = this.groundCheck() ? Vector3.zero : Vector3.down * 15;
            controller.Move((direction * moveSpeed + g) * Time.fixedDeltaTime);
        }

        private void LookAtPlayer()
        {
            transform.LookAt(this.playerController.transform);
        }

        private bool groundCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, this.controller.bounds.min.y, transform.position.z);
            var checkResult = Physics.Raycast(
                spherePosition,
                Vector3.down,
                0.2f,
                this.collisionGroups.obstacleMask,
                QueryTriggerInteraction.Ignore);
            return checkResult;
        }

        private async UniTaskVoid startAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await this.walkToPlayer(ct);

                // TODO: animation to warn player
                Debug.Log("starting demo3");
                this.demo3.transform.position = transform.position + Vector3.up * this.demo3Offset;
                await this.demo3.Showcase(ct);

                if (Vector3.Distance(transform.position, this.playerController.transform.position) < 15f && UnityEngine.Random.Range(0f, 1f) < 0.5f)
                {
                    Debug.Log("starting demo2");
                    this.demo2.transform.position = transform.position + Vector3.up * this.demo2Offset;
                    await this.demo2.Showcase(ct);
                }

                await UniTask.WaitForSeconds(1, cancellationToken: ct);
            }
        }

        private async UniTask walkToPlayer(CancellationToken ct)
        {
            float remainSeconds = UnityEngine.Random.Range(2f, 5f);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfterSlim(TimeSpan.FromSeconds(remainSeconds));

            while (!cts.IsCancellationRequested)
            {
                MoveToPlayer();
                LookAtPlayer();
                try
                {
                    await UniTask.WaitForFixedUpdate(cts.Token);
                }
                catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
                {
                }
            }
        }
    }
}