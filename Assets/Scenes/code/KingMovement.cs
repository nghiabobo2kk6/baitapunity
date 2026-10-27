using System.Collections;
using UnityEngine;

public class KingMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;

    [Header("Camera Boundary")]
    public Camera mainCamera;
    public float sidePadding = 0.3f;

    [Header("One Walking Animation")]
    public AnimationClip walkAnimation;
    public string walkState = "WalkRight";

    [Header("Default Sprite Direction")]
    public bool spriteFacesLeftByDefault = false;

    [Header("Jump - Code Only")]
    public float jumpHeight = 2f;
    public float jumpDuration = 0.65f;
    public bool allowDoubleJump = true;
    public int maxJumps = 3;

    [Header("Slide")]
    public float slideSpeed = 8f;
    public float slideDuration = 0.35f;
    public float slideCooldown = 4f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool facingLeft;
    private bool isJumping;
    private bool isSliding;

    private int jumpsUsed;
    private float groundY;
    private float jumpStartY;
    private float jumpTimer;
    private float nextSlideTime;

    private string currentState = "";
    private Coroutine slideCoroutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        groundY = transform.position.y;
        facingLeft = spriteFacesLeftByDefault;
    }

    private void Start()
    {
        UpdateFacing();
        StopWalkingAnimation();
    }

    private void Update()
    {
        float horizontal = ReadHorizontalInput();

        if (horizontal < 0f)
            facingLeft = true;
        else if (horizontal > 0f)
            facingLeft = false;

        UpdateFacing();

        if (Input.GetKeyDown(KeyCode.W))
            TryJump();

        if (Input.GetKeyDown(KeyCode.Space))
            TrySlide(horizontal);

        if (!isSliding)
            HandleMovement(horizontal);

        UpdateJump();
        ClampToCamera();
    }

    private float ReadHorizontalInput()
    {
        bool left = Input.GetKey(KeyCode.A)
                    || Input.GetKey(KeyCode.LeftArrow);

        bool right = Input.GetKey(KeyCode.D)
                     || Input.GetKey(KeyCode.RightArrow);

        if (left == right)
            return 0f;

        return left ? -1f : 1f;
    }

    private void HandleMovement(float horizontal)
    {
        if (horizontal == 0f)
        {
            if (!isJumping)
                StopWalkingAnimation();

            return;
        }

        float speed = Input.GetKey(KeyCode.S)
            ? runSpeed
            : walkSpeed;

        Vector3 position = transform.position;
        position.x += horizontal * speed * Time.deltaTime;
        transform.position = position;

        if (!isJumping)
            PlayWalkAnimation();
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX =
            facingLeft != spriteFacesLeftByDefault;
    }

    private void PlayWalkAnimation()
    {
        if (animator == null || walkAnimation == null)
            return;

        if (!animator.enabled)
            animator.enabled = true;

        int stateHash = Animator.StringToHash(walkState);

        if (!animator.HasState(0, stateHash))
        {
            Debug.LogError(
                "KingMovement: Cannot find Animator State '" +
                walkState + "'. Check the Animator Controller.", this);
            return;
        }

        if (currentState != walkState)
        {
            animator.Play(stateHash, 0, 0f);
            currentState = walkState;
        }
    }

    private void StopWalkingAnimation()
    {
        if (animator != null)
            animator.enabled = false;

        currentState = "";
    }

    private void TryJump()
    {
        if (isSliding)
            return;

        if (!isJumping)
        {
            groundY = transform.position.y;
            jumpsUsed = 0;
            StartJump();
        }
        else if (allowDoubleJump && jumpsUsed < maxJumps)
        {
            StartJump();
        }
    }

    private void StartJump()
    {
        isJumping = true;
        jumpsUsed++;
        jumpStartY = transform.position.y;
        jumpTimer = 0f;
    }

    private void UpdateJump()
    {
        if (!isJumping)
            return;

        jumpTimer += Time.deltaTime;

        float duration = Mathf.Max(0.05f, jumpDuration);
        float t = Mathf.Clamp01(jumpTimer / duration);

        float baseY = Mathf.Lerp(jumpStartY, groundY, t);
        float arc = 4f * jumpHeight * t * (1f - t);

        Vector3 position = transform.position;
        position.y = baseY + arc;
        transform.position = position;

        if (t >= 1f)
        {
            position = transform.position;
            position.y = groundY;
            transform.position = position;

            isJumping = false;
            jumpsUsed = 0;
            StopWalkingAnimation();
        }
    }

    private void TrySlide(float horizontal)
    {
        if (isSliding || Time.time < nextSlideTime)
            return;

        nextSlideTime = Time.time + Mathf.Max(0f, slideCooldown);

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = StartCoroutine(SlideRoutine(horizontal));
    }

    private IEnumerator SlideRoutine(float horizontal)
    {
        isSliding = true;

        if (isJumping)
        {
            isJumping = false;
            jumpsUsed = 0;

            Vector3 position = transform.position;
            position.y = groundY;
            transform.position = position;
        }

        float direction = horizontal != 0f
            ? Mathf.Sign(horizontal)
            : (facingLeft ? -1f : 1f);

        facingLeft = direction < 0f;
        UpdateFacing();

        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, slideDuration);

        while (elapsed < duration)
        {
            float input = ReadHorizontalInput();

            if (input != 0f)
            {
                direction = Mathf.Sign(input);
                facingLeft = direction < 0f;
                UpdateFacing();
            }

            Vector3 position = transform.position;
            position.x += direction * slideSpeed * Time.deltaTime;
            transform.position = position;

            elapsed += Time.deltaTime;
            ClampToCamera();

            yield return null;
        }

        isSliding = false;
        slideCoroutine = null;
        StopWalkingAnimation();
    }

    private void ClampToCamera()
    {
        if (mainCamera == null || !mainCamera.orthographic)
            return;

        float halfWidth =
            mainCamera.orthographicSize * mainCamera.aspect;

        float leftLimit =
            mainCamera.transform.position.x - halfWidth + sidePadding;

        float rightLimit =
            mainCamera.transform.position.x + halfWidth - sidePadding;

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(
            position.x, leftLimit, rightLimit);

        transform.position = position;
    }
}
