using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SpellBound.Core;
using UnityEngine;
using VContainer;

public class PlayerPresenter : MonoBehaviour
{
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private GameObject vfx;

    [Inject]
    private readonly Character character;
    private PlayerController controller;

    void Start()
    {
        this.controller = GetComponent<PlayerController>();
        var ct = gameObject.GetCancellationTokenOnDestroy();
        this.controller.dash.QueueSubscibe(dir =>
        {
            var go = Instantiate(this.vfx, transform.position, Quaternion.identity);
        }, ct);
    }

    void Update()
    {
        this.animator.SetFloat("Velocity", this.controller.horizontalSpeed);
    }
}
