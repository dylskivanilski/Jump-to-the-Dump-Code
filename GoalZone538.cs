using UnityEngine;

/// <summary>
/// The dump at the top of the level. Touching it wins the game.
///
/// Setup: put this on an object with a Collider2D set to "Is Trigger".
/// The player object must be tagged "Player".
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GoalZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.Win();
        }
    }
}
