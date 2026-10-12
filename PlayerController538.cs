using UnityEngine;

/// <summary>
/// Charge-jump platformer controller for "A Hop, Skip, and a Jump to the Dump!"
///
/// How it plays:
///  - On the ground, A/D walks left and right.
///  - Holding Space freezes the player and fills a charge meter.
///  - Releasing Space (or reaching full charge) launches the player. The jump
///    goes left, right, or straight up depending on which key is held at release.
///  - There is NO air control: once you jump, you're committed. That is the
///    core risk/reward of the game.
///  - Hitting a wall mid-air bounces you back off it.
///  - Falling a long way stuns the player briefly and plays a "splat" sound,
///    so a bad jump feels like a real setback.
///
/// Required setup on the player object:
///  - Rigidbody2D (Interpolate, Continuous collision, Freeze Rotation Z)
///  - A Collider2D with a zero-friction Physics Material 2D (so it never sticks to walls)
///  - An AudioSource (Play On Awake off)
///  - A child "GroundCheck" transform placed at the player's feet
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Inspector settings
    // ---------------------------------------------------------------

    [Header("Walking")]
    [Tooltip("Horizontal speed while walking on the ground.")]
    [SerializeField] private float walkSpeed = 4f;

    [Header("Charge Jump")]
    [Tooltip("Upward speed of a tap jump (almost no charge).")]
    [SerializeField] private float minJumpForce = 6f;
    [Tooltip("Upward speed of a fully charged jump.")]
    [SerializeField] private float maxJumpForce = 18f;
    [Tooltip("Seconds of holding Space needed to reach full charge. The jump fires automatically at full charge.")]
    [SerializeField] private float maxChargeTime = 0.8f;
    [Tooltip("Sideways speed applied when jumping while holding A or D.")]
    [SerializeField] private float jumpHorizontalSpeed = 5f;

    [Header("Air Behavior")]
    [Tooltip("How much horizontal speed is kept after bouncing off a wall (0 = dead stop, 1 = perfect bounce).")]
    [Range(0f, 1f)]
    [SerializeField] private float wallBounce = 0.5f;

    [Header("Falling")]
    [Tooltip("A fall at least this many units tall counts as a 'big fall' (stun + splat sound).")]
    [SerializeField] private float bigFallDistance = 6f;
    [Tooltip("How long the player is stuck on the ground after a big fall.")]
    [SerializeField] private float stunDuration = 0.6f;

    [Header("Ground Check")]
    [Tooltip("Empty child object positioned at the player's feet.")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);
    [Tooltip("Layer(s) that count as ground. Put your Tilemap/platforms on this layer.")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Visuals (optional)")]
    [Tooltip("The child holding the SpriteRenderer. Gets squashed while charging. Leave empty to skip.")]
    [SerializeField] private Transform visual;
    [Tooltip("How flat the sprite gets at full charge (1 = no squash).")]
    [SerializeField] private float maxSquash = 0.75f;
    [Tooltip("Optional Animator. Needs float 'Speed' and bools 'Grounded', 'Charging', 'Stunned'.")]
    [SerializeField] private Animator animator;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioClip bigFallClip;
    [SerializeField] private AudioClip wallBumpClip;

    // ---------------------------------------------------------------
    // Runtime state
    // ---------------------------------------------------------------

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private float moveInput;          // -1 (left), 0, or 1 (right)
    private bool isGrounded;
    private bool isCharging;
    private float chargeTimer;
    private float stunTimer;

    // Right after jumping, the feet are still touching the ground for a frame or two.
    // This short timer stops us from instantly "landing" again.
    private float ignoreGroundTimer;
    private const float JumpGroundIgnoreTime = 0.1f;

    // Highest point reached since leaving the ground, used to measure fall distance.
    private float highestY;

    // Velocity from the previous physics step. By the time OnCollisionEnter2D runs,
    // Unity has already zeroed our velocity against the wall, so we need the old value to bounce.
    private Vector2 lastVelocity;

    /// <summary>0 to 1 charge amount, read by the charge meter UI.</summary>
    public float ChargePercent => isCharging ? chargeTimer / maxChargeTime : 0f;

    // ---------------------------------------------------------------
    // Unity lifecycle
    // ---------------------------------------------------------------

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        highestY = transform.position.y;
    }

    private void Update()
    {
        // Ignore all input while the game is paused.
        if (Time.timeScale == 0f) return;

        // GetAxisRaw gives a crisp -1/0/1 for A/D (and arrow keys).
        moveInput = Input.GetAxisRaw("Horizontal");

        if (stunTimer > 0f)
        {
            // Stunned after a big fall: no input until it wears off.
            stunTimer -= Time.deltaTime;
            isCharging = false;
        }
        else if (isGrounded)
        {
            HandleChargeInput();
        }

        UpdateVisuals();
    }

    private void FixedUpdate()
    {
        UpdateGrounded();

        if (isGrounded)
        {
            // On the ground we fully control horizontal speed.
            // Charging or stunned means standing still.
            bool canWalk = !isCharging && stunTimer <= 0f;
            float xSpeed = canWalk ? moveInput * walkSpeed : 0f;
            rb.linearVelocity = new Vector2(xSpeed, rb.linearVelocity.y);
        }
        // In the air we do nothing: no air control, physics decides.

        lastVelocity = rb.linearVelocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Wall bounces only happen mid-air.
        if (isGrounded) return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            // A mostly-horizontal normal means we hit the side of something.
            if (Mathf.Abs(contact.normal.x) > 0.5f)
            {
                rb.linearVelocity = new Vector2(-lastVelocity.x * wallBounce, rb.linearVelocity.y);

                // Only play the bump sound for real hits, not light grazes.
                if (Mathf.Abs(lastVelocity.x) > 1f)
                {
                    PlaySound(wallBumpClip);
                }
                break;
            }
        }
    }

    // ---------------------------------------------------------------
    // Jumping
    // ---------------------------------------------------------------

    /// <summary>Starts, fills, and releases the jump charge.</summary>
    private void HandleChargeInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isCharging = true;
            chargeTimer = 0f;
        }

        if (!isCharging) return;

        chargeTimer = Mathf.Min(chargeTimer + Time.deltaTime, maxChargeTime);

        // Jump on release, or automatically once the meter is full.
        if (Input.GetKeyUp(KeyCode.Space) || chargeTimer >= maxChargeTime)
        {
            Jump();
        }
    }

    /// <summary>Launches the player based on how long Space was held.</summary>
    private void Jump()
    {
        float chargeAmount = chargeTimer / maxChargeTime;
        float upwardSpeed = Mathf.Lerp(minJumpForce, maxJumpForce, chargeAmount);
        float sidewaysSpeed = moveInput * jumpHorizontalSpeed;

        rb.linearVelocity = new Vector2(sidewaysSpeed, upwardSpeed);

        isCharging = false;
        chargeTimer = 0f;
        isGrounded = false;
        ignoreGroundTimer = JumpGroundIgnoreTime;

        PlaySound(jumpClip);
    }

    // ---------------------------------------------------------------
    // Ground detection and landing
    // ---------------------------------------------------------------

    /// <summary>Checks whether the feet are on ground and handles landing.</summary>
    private void UpdateGrounded()
    {
        bool wasGrounded = isGrounded;
        bool groundedNow;

        if (ignoreGroundTimer > 0f)
        {
            ignoreGroundTimer -= Time.fixedDeltaTime;
            groundedNow = false;
        }
        else
        {
            bool touchingGround = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
            // Only count as grounded when not moving upward (prevents "landing" while rising past a ledge).
            groundedNow = touchingGround && rb.linearVelocity.y <= 0.01f;
        }

        if (!wasGrounded)
        {
            highestY = Mathf.Max(highestY, transform.position.y);
        }

        if (groundedNow && !wasGrounded)
        {
            OnLanded();
        }

        // Walked or got knocked off a ledge mid-charge: cancel the charge.
        if (!groundedNow && isCharging)
        {
            isCharging = false;
        }

        isGrounded = groundedNow;

        if (isGrounded)
        {
            highestY = transform.position.y;
        }
    }

    /// <summary>Called on the first physics step back on the ground.</summary>
    private void OnLanded()
    {
        float fallDistance = highestY - transform.position.y;

        if (fallDistance >= bigFallDistance)
        {
            // Big fall: brief stun so the player feels the lost progress.
            stunTimer = stunDuration;
            PlaySound(bigFallClip);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterBigFall();
            }
        }
        else
        {
            PlaySound(landClip);
        }
    }

    // ---------------------------------------------------------------
    // Visuals and audio
    // ---------------------------------------------------------------

    /// <summary>Flips the sprite, squashes it while charging, and feeds the Animator.</summary>
    private void UpdateVisuals()
    {
        // Face the direction the player is pressing (only when they're in control).
        if (spriteRenderer != null && moveInput != 0f && stunTimer <= 0f)
        {
            spriteRenderer.flipX = moveInput < 0f;
        }

        // Squash the sprite down as the jump charges, like a spring compressing.
        if (visual != null)
        {
            float squash = Mathf.Lerp(1f, maxSquash, ChargePercent);
            visual.localScale = new Vector3(1f / squash, squash, 1f);
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
            animator.SetBool("Grounded", isGrounded);
            animator.SetBool("Charging", isCharging);
            animator.SetBool("Stunned", stunTimer > 0f);
        }
    }

    /// <summary>Plays a one-shot sound with slight pitch variation so repeats don't sound robotic.</summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;

        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(clip);
    }

    /// <summary>Draws the ground-check box in the Scene view so it's easy to position.</summary>
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}
