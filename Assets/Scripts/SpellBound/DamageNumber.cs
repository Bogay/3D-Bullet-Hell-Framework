using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace SpellBound
{
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField]
        private float speed;
        [SerializeField]
        private float ttl;

        [Header("Juice")]
        [SerializeField]
        [Range(0.5f, 2f)]
        private float initialScaleMultiplier;
        [SerializeField]
        [Range(0.1f, 2f)]
        private float finalScale;

        private Vector3 originalScale;

        public int Value;
        private TMP_Text text;

        void Start()
        {
            this.text = GetComponent<TMP_Text>();
            this.text.text = this.Value.ToString();
            this.originalScale = transform.localScale;

            var ct = this.GetCancellationTokenOnDestroy();
            this.juiceScale(ct).Forget();

            Destroy(gameObject, this.ttl);
        }

        private async UniTask juiceScale(CancellationToken ct)
        {
            await transform
                .DOScale(this.originalScale * this.initialScaleMultiplier, 0.1f)
                .SetEase(Ease.InCubic)
                .ToUniTask(cancellationToken: ct);
            await transform
                .DOScale(this.finalScale, this.ttl)
                .SetEase(Ease.InCubic)
                .ToUniTask(cancellationToken: ct);
        }

        void Update()
        {
            transform.position += Vector3.up * (this.speed * Time.deltaTime);
            // look at main camera
            var target = Camera.main.transform.position;
            target.y = transform.position.y;
            transform.LookAt(target);
            transform.Rotate(new Vector3(0, 180, 0));
        }
    }
}
