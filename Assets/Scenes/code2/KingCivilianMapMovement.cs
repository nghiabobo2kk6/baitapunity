
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class KingCivilianMapMovement : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Camera targetCamera;
    public SpriteRenderer backgroundRenderer;

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;

    [Header("Idle")]
    public float idleDelay = 1f;

    [Header("Jump")]
    public float jumpForce = 7f;
    public float gravity = 20f;
    public int maxJumps = 3;

    [Header("Slide")]
    public float slideDuration = 1f;
    public float slideSpeed = 8f;

    [Header("Camera Follow")]
    public bool followCamera = true;
    public float cameraDeadZoneX = 2.5f;
    public float cameraSmoothTime = 0.6f;
    public float lookAheadX = 1f;

    private SpriteRenderer spriteRenderer;
    private bool facingRight = true;
    private bool isJumping;
    private bool isSliding;

    private int jumpsUsed;
    private float groundY;
    private float verticalVelocity;
    private float lastActionTime;
    private string currentState = "";

    private float cameraVelocityX;
    private float cameraStartY;
    private float cameraStartZ;

    private float mapLeft;
    private float mapRight;
    private bool validBackgroundBounds;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        groundY = transform.position.y;

        if (targetCamera != null)
        {
            cameraStartY = targetCamera.transform.position.y;
            cameraStartZ = targetCamera.transform.position.z;
        }

        RefreshBackgroundBounds();
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

    private void LateUpdate()
    {
        FollowCamera();
    }

    private void UpdateFacing()
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = !facingRight;
    }

    private void RefreshBackgroundBounds()
    {
        validBackgroundBounds = false;

        if (backgroundRenderer == null ||
            backgroundRenderer.sprite == null)
            return;

        Bounds bounds = backgroundRenderer.bounds;
        mapLeft = bounds.min.x;
        mapRight = bounds.max.x;

        validBackgroundBounds = mapRight > mapLeft;
    }

    private void HandleMovement()
    {
        float direction = 0f;

        if (Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.LeftArrow))
            direction = -1f;
        else if (Input.GetKey(KeyCode.D) ||
                 Input.GetKey(KeyCode.RightArrow))
            direction = 1f;

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
            TryJump();
    }

    private void HandleSlideInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) &&
            !isSliding && !isJumping)
            StartCoroutine(SlideRoutine());
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

        transform.position = position;

        if (!isJumping)
            PlayState("Civ_IdleRight");
    }

    private void MoveHorizontal(float direction, float speed)
    {
        Vector3 position = transform.position;
        position.x += direction * speed * Time.deltaTime;

        if (validBackgroundBounds)
        {
            float halfWidth = spriteRenderer.bounds.extents.x;

            float minX = mapLeft + halfWidth;
            float maxX = mapRight - halfWidth;

            if (minX <= maxX)
                position.x = Mathf.Clamp(position.x, minX, maxX);
            else
                position.x = (mapLeft + mapRight) * 0.5f;
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

        UpdateFacing();
        PlayState("Civ_IdleRight");

        lastActionTime = Time.time;
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
        {
            UpdateFacing();
            PlayState("Civ_IdleRight");
        }
    }

    private void FollowCamera()
    {
        if (!followCamera || targetCamera == null ||
            !targetCamera.orthographic)
            return;

        if (!validBackgroundBounds)
            RefreshBackgroundBounds();

        if (!validBackgroundBounds)
            return;

        float halfWidth =
            targetCamera.orthographicSize * targetCamera.aspect;

        float cameraX = targetCamera.transform.position.x;
        float offset = transform.position.x - cameraX;

        float targetX = cameraX;

        if (offset > cameraDeadZoneX)
            targetX = transform.position.x - cameraDeadZoneX + lookAheadX;
        else if (offset < -cameraDeadZoneX)
            targetX = transform.position.x + cameraDeadZoneX - lookAheadX;

        float minX = mapLeft + halfWidth;
        float maxX = mapRight - halfWidth;

        if (minX > maxX)
            targetX = (mapLeft + mapRight) * 0.5f;
        else
            targetX = Mathf.Clamp(targetX, minX, maxX);

        float newX = Mathf.SmoothDamp(
            cameraX,
            targetX,
            ref cameraVelocityX,
            Mathf.Max(0.05f, cameraSmoothTime));

        if (minX <= maxX)
            newX = Mathf.Clamp(newX, minX, maxX);
        else
            newX = (mapLeft + mapRight) * 0.5f;

        targetCamera.transform.position = new Vector3(
            newX, cameraStartY, cameraStartZ);
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
                "Missing Animator state: " + stateName, this);
            return;
        }

        if (currentState == stateName)
            return;

        currentState = stateName;
        animator.Play(hash, 0, 0f);
    }
}
