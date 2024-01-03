using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SpellBound.Combat;
using SpellBound.Core;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class PlayerStatsUI : MonoBehaviour
{
    [SerializeField]
    private Slider hpSlider;
    [SerializeField]
    private Slider mpSlider;

    // TODO: extract to component
    // TODO: DI
    [SerializeField]
    private Image cooldownImage;
    [SerializeField]
    private MainWeapon weapon;

    [Header("Juice")]
    [SerializeField]
    private float jumpScale;
    [SerializeField]
    [Range(0f, 1f)]
    private float resumeFactor;

    private Transform jumpTarget;

    [Inject]
    private Character character;

    private void Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        this.jumpTarget = this.cooldownImage.transform.parent;
        this.weapon.OnCooldownFinished(() =>
        {
            this.jumpTarget.localScale = Vector3.one * this.jumpScale;
        }).AddTo(ct);
        this.updateCooldown(ct).Forget();
    }

    void Update()
    {
        this.hpSlider.maxValue = this.character.MaxHP.Value();
        this.hpSlider.value = this.character.HP;
        this.mpSlider.maxValue = this.character.MaxMP.Value();
        this.mpSlider.value = this.character.MP;
    }

    private async UniTask updateCooldown(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var progress = 1 - this.weapon.ShootTimer / this.weapon.ShootCooldownSeconds;
            this.cooldownImage.fillAmount = progress;
            this.jumpTarget.localScale = Vector3.Slerp(this.jumpTarget.localScale, Vector3.one, this.resumeFactor);
            await UniTask.NextFrame(ct);
        }
    }
}
