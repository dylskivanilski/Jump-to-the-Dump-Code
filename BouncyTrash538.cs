using UnityEngine;

/// <summary>
/// Obstacle: a squishy trash bag that launches the player upward when they land on it.
/// Useful as a shortcut if hit cleanly, or a trap that throws you somewhere bad.
///
/// Setup: put this on a trash bag sprite with a (non-trigger) Collider2D,
/// on the Ground layer so the player can also stand at its edges.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BouncyTrash : MonoBehaviour
{
    [Tooltip("Upward speed given to the player on bounce.")]
    [SerializeField] private float bounceSpeed = 16f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bounceClip;
    [Tooltip("Optional: the sprite child to squish on bounce.")]
    [SerializeField] private Transform visual;

    private float squishTimer;
    private const float SquishDuration = 0.15f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player")) return;

        // Only bounce when the player lands on top (contact normal points down into us).
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f)
            {
                Rigidbody2D playerRb = collision.rigidbody;
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, bounceSpeed);

                if (audioSource != null && bounceClip != null)
                {
                    audioSource.PlayOneShot(bounceClip);
                }
                squishTimer = SquishDuration;
                break;
            }
        }
    }

    private void Update()
    {
        if (visual == null) return;

        // Briefly flatten the bag after a bounce for some visual feedback.
        squishTimer -= Time.deltaTime;
        visual.localScale = squishTimer > 0f ? new Vector3(1.2f, 0.7f, 1f) : Vector3.one;
    }
}
