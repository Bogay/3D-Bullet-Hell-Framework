﻿using UnityEngine;
using SpellBound.Combat;
using SpellBound.Core;
using Cysharp.Threading.Tasks;
using System.Threading;
using Cinemachine;
using System;

public class PlayerController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField, ShowOnly]
    private float moveSpeed = 5;
    [SerializeField, ShowOnly]
    private float sprintSpeed = 10;
    [SerializeField, ShowOnly]
    private float acclerationStrenth = 10;
    [SerializeField, ShowOnly]
    private float rotateStrenth = 0.2f;

    [Space(10)]
    [SerializeField, ShowOnly]
    private float jumpHeight = 2.25f;
    [SerializeField, ShowOnly]
    private float gravity = -15.0f;

    [field: Space(10)]
    [field: Header("Ability")]
    [field: SerializeField]
    public MainWeapon MainWeapon { get; private set; }
    [SerializeField]
    private MainWeapon secondWeapon;
    [field: SerializeField]
    public Dash dash { get; private set; }

    [field: SerializeField]
    public Character Character { get; private set; }

    [Space(10)]
    [SerializeField, ShowOnly, Tooltip("Time required to pass before being able to jump again")]
    private float jumpTimeout = 0.15f;
    [SerializeField, ShowOnly, Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    private float fallTimeout = 0.15f;

    [Header("Player Grounded")]
    [SerializeField, ShowOnly, Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    private bool grounded = true;
    [SerializeField, ShowOnly, Tooltip("Useful for rough ground")]
    private float groundedOffset = -0.14f;
    [SerializeField, ShowOnly, Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    private float groundedRadius = 0.28f;
    [SerializeField, ShowOnly, Tooltip("What layers the character uses as ground")]
    private LayerMask groundLayers;

    [Header("Camera control")]
    [SerializeField]
    private CinemachineFreeLook cinemachineFreeLook;

    // player
    public float horizontalSpeed { get; private set; }
    public float verticalSpeed { get; private set; }
    private Vector3 rawDirection = Vector3.zero;
    private Vector3 moveDirection = Vector3.zero;
    private bool isControlled = false;

    // timer
    private float jumpCooldown = 0;
    private float fallStateTimer = 0; // Total time in falling, reset to 0 when touch the ground.

    // boolean
    private bool canJump { get { return jumpCooldown <= 0 && grounded; } }
    private bool isFalling { get { return fallStateTimer >= fallTimeout; } }

    private CharacterController controller;
    private Transform mainCamera;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main.transform;

        var ct = this.GetCancellationTokenOnDestroy();
        Cursor.lockState = CursorLockMode.Locked;
        ct.Register(() =>
        {
            Cursor.lockState = CursorLockMode.None;
        });
        this.Character.Init();

        this.CharacterRegen(ct).Forget();
    }

    void Update()
    {
        DetectKeyDown();
    }

    void FixedUpdate()
    {
        DetectKey();
        GroundCheck();
        CalculateSpeed();
        Rotation();
        Move();
    }

    private void DetectKeyDown()
    {
        if (Input.GetKeyDown(KeyCode.Space) && canJump)
        {
            // the square root of H * -2 * G = how much speed needed to reach desired height
            verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCooldown = jumpTimeout;
        }

        if (Input.GetMouseButton(0))
        {
            var distance = 100f;
            var ray = this.mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            Vector3 targetPosition = this.MainWeapon.transform.position + this.mainCamera.forward * distance;
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                Debug.Log("hit: " + hit.collider.name);
                targetPosition = hit.point;
            }

            Debug.Log($"Shoot: {this.MainWeapon.transform.position} -> {targetPosition}");
            Debug.DrawLine(this.MainWeapon.transform.position, targetPosition);

            Vector3 forward = targetPosition - this.MainWeapon.transform.position;
            this.MainWeapon.Shoot(forward);
        }
        else if (Input.GetMouseButtonDown(1))
            this.secondWeapon.Shoot(this.mainCamera.forward);

        if (Input.GetKeyDown(KeyCode.LeftShift))
            this.dash.Cast(this.moveDirection);
    }

    private async UniTaskVoid CharacterRegen(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            this.Character.Regen(5);
            await UniTask.Delay(TimeSpan.FromSeconds(1));
        }
    }

    private void DetectKey()
    {
        rawDirection = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
            rawDirection += Vector3.forward;
        if (Input.GetKey(KeyCode.S))
            rawDirection += Vector3.back;
        if (Input.GetKey(KeyCode.A))
            rawDirection += Vector3.left;
        if (Input.GetKey(KeyCode.D))
            rawDirection += Vector3.right;
    }

    private void GroundCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        var checkResult = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);

        grounded = checkResult;
    }

    private void Move()
    {
        controller.Move(moveDirection * horizontalSpeed * Time.fixedDeltaTime + Vector3.up * verticalSpeed * Time.fixedDeltaTime);
    }

    private void CalculateSpeed()
    {
        CalculateVerticalSpeed();
        CalculateHorizontalSpeed();
    }

    private void CalculateHorizontalSpeed()
    {
        if (this.isControlled) return;

        if (rawDirection == Vector3.zero)
            horizontalSpeed = Mathf.Max(horizontalSpeed - acclerationStrenth * Time.fixedDeltaTime, 0);
        else
        {
            // HACK: LeftShift + Space cause keyboard ghosting... crap.
            if (Input.GetKey(KeyCode.Mouse2))
                horizontalSpeed = Mathf.Min(horizontalSpeed + acclerationStrenth * Time.fixedDeltaTime, sprintSpeed);
            else
            {
                if (horizontalSpeed > moveSpeed)
                    horizontalSpeed = Mathf.Min(horizontalSpeed, sprintSpeed) - acclerationStrenth * Time.fixedDeltaTime;
                else
                    horizontalSpeed = Mathf.Min(horizontalSpeed + acclerationStrenth * Time.fixedDeltaTime, moveSpeed);
            }
        }

        this.moveDirection = CalculateMoveDirection(this.rawDirection);
    }

    private Vector3 CalculateMoveDirection(Vector3 direction)
    {
        var ret = Vector3.zero;
        if (direction.sqrMagnitude > 0f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
            ret = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
        }
        return ret;
    }

    private void CalculateVerticalSpeed()
    {
        if (grounded)
        {
            fallStateTimer = 0;
            jumpCooldown -= Time.fixedDeltaTime;
            if (verticalSpeed < 0)
                verticalSpeed = 0;
        }
        else
        {
            fallStateTimer += Time.fixedDeltaTime;
            jumpCooldown = jumpTimeout;
            verticalSpeed += gravity * Time.fixedDeltaTime;
        }
    }

    public async UniTaskVoid Dash(
        Vector3 direction,
        float duration = 0.1f,
        CancellationToken cancellationToken = default)
    {
        direction.y = 0;

        this.isControlled = true;
        var oldHorizontalSpeed = this.horizontalSpeed;
        var oldMoveDirection = this.moveDirection;
        if (this.cinemachineFreeLook)
            this.cinemachineFreeLook.enabled = false;

        this.moveDirection = direction.normalized;
        this.horizontalSpeed = direction.magnitude;
        await UniTask.Delay(System.TimeSpan.FromSeconds(duration), cancellationToken: cancellationToken);

        this.moveDirection = oldMoveDirection;
        this.horizontalSpeed = oldHorizontalSpeed;
        this.isControlled = false;
        if (this.cinemachineFreeLook)
            this.cinemachineFreeLook.enabled = true;
    }

    public void SetPosition(Vector3 position)
    {
        this.controller.enabled = false;
        transform.position = position;
        this.controller.enabled = true;
    }

    private void Rotation()
    {
        if (rawDirection.sqrMagnitude > 0f)
        {
            float targetAngle = Mathf.Atan2(rawDirection.x, rawDirection.z) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, targetAngle, 0), rotateStrenth);
        }
    }
}