using UnityEngine;

/// <summary>
/// Jump King-style checkpoint: there's no respawning, so instead, once the player
/// climbs past this point a "safety floor" closes underneath them. Falls after this
/// can only drop the player down to the floor, never all the way back to the start.
/// This keeps the lose-progress risk within each section without being punishing.
///
/// Setup:
///  - Put this on an object with a Collider2D set to "Is Trigger", stretched across
///    the full width of the level a little ABOVE where the safety floor will appear.
///  - Create the safety floor (sprite + Box Collider 2D on the Ground layer), place it
///    below the trigger, and DISABLE it (uncheck the box next to its name).
///  - Drag the floor (and anything else to reveal, like a flag sprite) into "Objects To Enable".
///  - The player must be tagged "Player".
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Objects that appear when the checkpoint is reached (the safety floor, a flag, etc.).")]
    [SerializeField] private GameObject[] objectsToEnable;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip checkpointClip;

    private bool isActivated;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only trigger once, and only for the player.
        if (isActivated || !other.CompareTag("Player")) return;

        isActivated = true;

        foreach (GameObject obj in objectsToEnable)
        {
            obj.SetActive(true);
        }

        if (audioSource != null && checkpointClip != null)
        {
            audioSource.PlayOneShot(checkpointClip);
        }
    }
}
