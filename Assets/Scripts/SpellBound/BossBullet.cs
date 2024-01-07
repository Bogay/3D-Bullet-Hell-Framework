using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SpellBound
{
    public class BossBullet : MonoBehaviour
    {
        void Start()
        {
            var vfxPrefab = transform.Find("VFX");
            vfxPrefab.parent = null;
            this.GetCancellationTokenOnDestroy().Register(() => Destroy(vfxPrefab.gameObject));
        }
    }
}
