using UnityEngine;

/// <summary>
/// Smoothly follows the player vertically while keeping a fixed X position,
/// so the level always stays framed the same way side to side.
///
/// Setup: put this on the Main Camera and drag the player into "Target".
/// Set Min Y so the camera never shows below the bottom of the level,
/// and Max Y so it never shows above the top.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Tooltip("How quickly the camera catches up (smaller = snappier).")]
    [SerializeField] private float smoothTime = 0.2f;
    [Tooltip("Shifts the camera up so the player sees more of what's above them.")]
    [SerializeField] private float verticalOffset = 2f;
    [SerializeField] private float minY = 0f;
    [SerializeField] private float maxY = 100f;

    private float yVelocity; // Used internally by SmoothDamp.

    // LateUpdate runs after the player has moved this frame, which prevents jitter.
    private void LateUpdate()
    {
        if (target == null) return;

        float desiredY = Mathf.Clamp(target.position.y + verticalOffset, minY, maxY);
        float newY = Mathf.SmoothDamp(transform.position.y, desiredY, ref yVelocity, smoothTime);

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
