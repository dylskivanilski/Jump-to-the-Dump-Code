using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the player's jump charge as a filling bar.
/// The bar is hidden when not charging and changes color as it fills
/// (e.g. green for a short hop, red for a full-power jump).
///
/// Setup: put this on a UI object with a CanvasGroup. Inside it, add an Image
/// set to Image Type = Filled and drag it into "Fill Image".
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ChargeMeterUI : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [Tooltip("Image with Image Type set to Filled.")]
    [SerializeField] private Image fillImage;
    [Tooltip("Bar color from empty (left) to full (right).")]
    [SerializeField] private Gradient chargeColor;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        float charge = player.ChargePercent;

        // Fade the meter out instead of disabling the object, so this script keeps running.
        canvasGroup.alpha = charge > 0f ? 1f : 0f;

        fillImage.fillAmount = charge;
        fillImage.color = chargeColor.Evaluate(charge);
    }
}
