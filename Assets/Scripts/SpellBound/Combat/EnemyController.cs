using System.Collections;
using System.Collections.Generic;
using Bogay.SceneAudioManager;
using SpellBound.Core;
using UnityEngine;
using VContainer;

public class EnemyController : MonoBehaviour
{
    [field: SerializeField]
    public Character character { get; private set; }

    [SerializeField]
    private GameObject deathVFX;

    [Inject]
    private readonly PlayerController playerController;

    [SerializeField, ShowOnly]
    private float moveSpeed = .5f;

    private Transform playerTransform;
    private CharacterController controller;

    void Start()
    {
        this.character = ScriptableObject.Instantiate(this.character);
        this.character.Init();

        Debug.Log(playerController);
        this.controller = GetComponent<CharacterController>();
        this.playerTransform = playerController.GetComponent<Transform>();
    }

    void Update()
    {
        if (this.character.HP <= 0)
        {
            Instantiate(this.deathVFX, transform.position, Quaternion.identity);
            SceneAudioManager.instance.PlayByName("EnemyDeath");
            Destroy(gameObject);
        }

        FollowPlayer();
    }

    private void FollowPlayer()
    {
        var direction = (playerTransform.position - transform.position).normalized;
        controller.Move(direction * moveSpeed * Time.fixedDeltaTime);
    }
}
