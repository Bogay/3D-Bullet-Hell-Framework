using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Linq;
using System;

namespace SpellBound
{
    public class ModelColorBlink : MonoBehaviour
    {
        [SerializeField]
        private Transform visualRoot;

        private List<Renderer> renderers;
        private CancellationTokenSource cancelBlinkAllTokenSource = new CancellationTokenSource();
        private UniTask blinkTask;
        private readonly object tokenSourceLock = new object();

        private void Start()
        {
            this.renderers = visualRoot.GetComponentsInChildren<Renderer>().ToList();
        }

        private CancellationToken createNewToken(CancellationToken ct)
        {
            lock (this.tokenSourceLock)
            {
                this.cancelBlinkAllTokenSource.Dispose();
                this.cancelBlinkAllTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
                return this.cancelBlinkAllTokenSource.Token;
            }

        }

        public async UniTaskVoid BlinkAll(CancellationToken ct)
        {
            var token = this.createNewToken(ct);
            await UniTask.NextFrame(cancellationToken: token);
            token.ThrowIfCancellationRequested();

            await UniTask.WaitUntil(
                () => this.blinkTask.Status != UniTaskStatus.Pending,
                cancellationToken: token
            );
            token.ThrowIfCancellationRequested();

            var tasks = this.renderers.Select(r => this.blink(r, token)).ToArray();
            this.blinkTask = UniTask.WhenAll(tasks);
            token.ThrowIfCancellationRequested();

            await this.blinkTask;
        }

        private async UniTask blink(Renderer renderer, CancellationToken ct)
        {
            if (renderer == null)
                return;

            var originalColor = renderer.material.color;
            Action<Color> changeColorChecked = (color) =>
            {
                // if the game object is not destroyed
                if (renderer != null)
                    renderer.material.color = color;
            };

            try
            {
                for (int i = 0; i < 2; i++)
                {
                    changeColorChecked(Color.red);
                    await UniTask.WaitForSeconds(0.03f, cancellationToken: ct);
                    changeColorChecked(originalColor);
                    await UniTask.WaitForSeconds(0.03f, cancellationToken: ct);
                }
            }
            finally
            {
                changeColorChecked(originalColor);
            }
        }
    }
}
