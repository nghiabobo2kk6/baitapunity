
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class KingCivilianMovement : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Camera targetCamera;

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float edgePadding = 0.3f;

    [Header("Idle")]
    public float idleDelay = 1f;

    [Header("Jump")]
    public float jumpForce = 7f;
    public float gravity = 20f;
    public int maxJumps = 3;

    [Header("Slide")]
    public float slideDuration = 1f;
    public float slideSpeed = 8f;

    private SpriteRenderer spriteRenderer;

    private bool facingRight = true;
    private bool isJumping;
    private bool isSliding;

    private int jumpsUsed;
    private float groundY;
    private float verticalVelocity;
    private float lastActionTime;
    private string currentState = "";

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        groundY = transform.position.y;
        UpdateFacing();
    }

    private void Start()
    {
        PlayState("Civ_IdleRight");
        lastActionTime = Time.time;
    }

    private void Update()
    {
        HandleMovement();
        HandleJumpInput();
        HandleSlideInput();
        UpdateJumpPhysics();
        UpdateIdle();
    }

    private void UpdateFacing()
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = !facingRight;
    }

    private void HandleMovement()
    {
        float direction = 0f;

        if (Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.LeftArrow))
        {
            direction = -1f;
        }
        else if (Input.GetKey(KeyCode.D) ||
                 Input.GetKey(KeyCode.RightArrow))
        {
            direction = 1f;
        }

        if (direction == 0f || isSliding)
            return;

        facingRight = direction > 0f;
        UpdateFacing();

        lastActionTime = Time.time;

        float speed = Input.GetKey(KeyCode.S)
            ? runSpeed
            : walkSpeed;

        MoveHorizontal(direction, speed);

        if (!isJumping)
            PlayState("Civ_WalkRight");
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.UpArrow))
        {
            TryJump();
        }
    }

    private void HandleSlideInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) &&
            !isSliding && !isJumping)
        {
            StartCoroutine(SlideRoutine());
        }
    }

    private void TryJump()
    {
        if (isSliding || jumpsUsed >= maxJumps)
            return;

        if (!isJumping)
        {
            groundY = transform.position.y;
            verticalVelocity = 0f;
            jumpsUsed = 0;
        }

        jumpsUsed++;
        isJumping = true;
        verticalVelocity = jumpForce;
        lastActionTime = Time.time;

        UpdateFacing();
        PlayState("Civ_JumpRight");
    }

    private void UpdateJumpPhysics()
    {
        if (!isJumping)
            return;

        verticalVelocity -= gravity * Time.deltaTime;

        Vector3 position = transform.position;
        position.y += verticalVelocity * Time.deltaTime;

        if (position.y <= groundY && verticalVelocity < 0f)
        {
            position.y = groundY;
            verticalVelocity = 0f;
            isJumping = false;
            jumpsUsed = 0;
            lastActionTime = Time.time;
        }

        if (targetCamera != null && targetCamera.orthographic)
        {
            float cameraTop =
                targetCamera.transform.position.y +
                targetCamera.orthographicSize - 0.3f;

            if (position.y > cameraTop)
            {
                position.y = cameraTop;

                if (verticalVelocity > 0f)
                    verticalVelocity = 0f;
            }
        }

        transform.position = position;

        if (!isJumping)
            PlayIdle();
    }

    private void MoveHorizontal(float direction, float speed)
    {
        Vector3 position = transform.position;
        position.x += direction * speed * Time.deltaTime;

        if (targetCamera != null && targetCamera.orthographic)
        {
            float halfWidth =
                targetCamera.orthographicSize *
                targetCamera.aspect;

            float leftEdge =
                targetCamera.transform.position.x -
                halfWidth + edgePadding;

            float rightEdge =
                targetCamera.transform.position.x +
                halfWidth - edgePadding;

            position.x = Mathf.Clamp(
                position.x,
                leftEdge,
                rightEdge);
        }

        transform.position = position;
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;

        float slideDirection = facingRight ? 1f : -1f;

        lastActionTime = Time.time;
        UpdateFacing();

        PlayState("Civ_SlideRight");

        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            MoveHorizontal(slideDirection, slideSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isSliding = false;
        currentState = "";

        PlayIdle();
        lastActionTime = Time.time;
    }

    private void PlayIdle()
    {
        UpdateFacing();
        PlayState("Civ_IdleRight");
    }

    private void UpdateIdle()
    {
        if (isJumping || isSliding)
            return;

        bool moving =
            Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.LeftArrow) ||
            Input.GetKey(KeyCode.RightArrow);

        if (moving)
            return;

        if (Time.time - lastActionTime >= idleDelay)
            PlayIdle();
    }

    private void PlayState(string stateName)
    {
        if (animator == null ||
            animator.runtimeAnimatorController == null)
            return;

        int hash = Animator.StringToHash(stateName);

        if (!animator.HasState(0, hash))
        {
            Debug.LogWarning(
                "State '" + stateName +
                "' not found in KingCivilian.controller.",
                this);
            return;
        }

        if (currentState == stateName)
            return;

        currentState = stateName;
        animator.Play(hash, 0, 0f);
    }
}
