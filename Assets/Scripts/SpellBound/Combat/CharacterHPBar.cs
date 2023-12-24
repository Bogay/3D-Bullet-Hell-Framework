using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using SpellBound.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SpellBound.Combat
{
    public class CharacterHPBar : MonoBehaviour
    {
        [SerializeField]
        private Character character;
        [SerializeField]
        private Slider hpSlider;

        private void Start()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            this.hpSlider.gameObject.SetActive(false);
            this.startAsync(ct).Forget();
        }

        private async UniTask startAsync(CancellationToken ct)
        {
            BossEnemyController bossEnemyController = null;
            while (!ct.IsCancellationRequested)
            {
                bossEnemyController = FindObjectOfType<BossEnemyController>();
                if (bossEnemyController != null)
                {
                    break;
                }
                await UniTask.WaitForSeconds(0.5f, cancellationToken: ct);
            }

            this.character = bossEnemyController.character;
            this.hpSlider.gameObject.SetActive(true);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, bossEnemyController.GetCancellationTokenOnDestroy());
            await foreach (var _ in UniTaskAsyncEnumerable.EveryUpdate().WithCancellation(cts.Token))
            {
                this.hpSlider.maxValue = this.character.MaxHP.Value();
                this.hpSlider.value = this.character.HP;
            }
        }
    }
}
