using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace SpellBound
{
    public class ModelColorBlink : MonoBehaviour
    {
        [SerializeField]
        private Transform visualRoot;

        private List<Renderer> renderers;

        private void Start()
        {
            this.renderers = visualRoot.GetComponentsInChildren<Renderer>().ToList();
        }

        // void Update()
        // {
        //     if (Input.GetKeyDown(KeyCode.B))
        //     {
        //         var ct = gameObject.GetCancellationTokenOnDestroy();
        //         this.blinkAll(ct).Forget();
        //     }
        // }

        public async UniTaskVoid BlinkAll(CancellationToken ct)
        {
            var tasks = this.renderers.Select(r => this.blink(r, ct)).ToArray();
            await UniTask.WhenAll(tasks);
        }

        private async UniTask blink(Renderer renderer, CancellationToken ct)
        {
            var originalColor = renderer.material.color;
            for (int i = 0; i < 3; i++)
            {
                renderer.material.color = Color.red;
                await UniTask.WaitForSeconds(0.01f, cancellationToken: ct);
                renderer.material.color = originalColor;
                await UniTask.WaitForSeconds(0.01f, cancellationToken: ct);
            }
        }
    }
}