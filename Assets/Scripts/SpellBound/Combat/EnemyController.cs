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
    [Inject]
    private readonly CollisionGroups collisionGroups;

    [SerializeField]
    private float moveSpeed = .5f;

    private Transform playerTransform;
    private CharacterController controller;

    void Start()
    {
        this.character = ScriptableObject.Instantiate(this.character);
        this.character.Init();

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

    }

    void FixedUpdate()
    {
        FollowPlayer();
    }

    private void FollowPlayer()
    {
        var direction = (playerTransform.position - transform.position).normalized;
        var g = this.groundCheck() ? Vector3.zero : Vector3.down * 15;
        controller.Move(direction * moveSpeed * Time.fixedDeltaTime + g * Time.fixedDeltaTime);
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
}
