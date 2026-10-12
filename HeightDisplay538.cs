using UnityEngine;
using TMPro;

/// <summary>
/// HUD text showing how high the player is and their best height so far.
/// Seeing your best height after a fall makes the lost progress sting (and
/// gives a reason to keep trying).
///
/// Setup: put this on a TextMeshPro UI text and drag the player into "Player".
/// </summary>
public class HeightDisplay : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private TMP_Text heightText;
    [Tooltip("The player's Y position at the start, counted as 0 m.")]
    [SerializeField] private float startY = 0f;

    private float bestHeight;

    private void Update()
    {
        float height = Mathf.Max(0f, player.position.y - startY);
        bestHeight = Mathf.Max(bestHeight, height);

        heightText.text = $"Height: {height:0}m\nBest: {bestHeight:0}m";
    }
}
