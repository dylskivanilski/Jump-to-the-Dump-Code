using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Runs the game scene: pausing, winning, restarting, and tracking stats
/// (time taken and number of big falls) for the victory screen.
///
/// Setup: put this on an empty "GameManager" object in the Game scene.
/// Hook the pause/victory panel buttons up to the public methods below.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>Easy global access, e.g. GameManager.Instance.Win().</summary>
    public static GameManager Instance { get; private set; }

    [Header("Panels")]
    [Tooltip("Pause menu panel (starts disabled).")]
    [SerializeField] private GameObject pausePanel;
    [Tooltip("Victory screen panel (starts disabled).")]
    [SerializeField] private GameObject victoryPanel;
    [Tooltip("Text on the victory panel that shows time and falls.")]
    [SerializeField] private TMP_Text victoryStatsText;

    [Header("Scenes")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip victoryClip;
    [SerializeField] private AudioClip pauseClip;

    private bool isPaused;
    private bool hasWon;
    private float elapsedTime;
    private int bigFallCount;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f; // In case we arrived here from a paused/won state.
    }

    private void Update()
    {
        if (hasWon) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }

        // unscaled check not needed: timer naturally stops while paused (deltaTime = 0).
        elapsedTime += Time.deltaTime;
    }

    // ---------------------------------------------------------------
    // Stats
    // ---------------------------------------------------------------

    /// <summary>Called by the player after a big fall.</summary>
    public void RegisterBigFall()
    {
        bigFallCount++;
    }

    // ---------------------------------------------------------------
    // Pause
    // ---------------------------------------------------------------

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        PlaySound(pauseClip);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        PlaySound(pauseClip);
    }

    // ---------------------------------------------------------------
    // Winning
    // ---------------------------------------------------------------

    /// <summary>Called by the GoalZone when the player reaches the dump.</summary>
    public void Win()
    {
        if (hasWon) return;
        hasWon = true;

        Time.timeScale = 0f; // Freeze the world behind the victory screen.
        victoryPanel.SetActive(true);
        PlaySound(victoryClip);

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        victoryStatsText.text = $"You made it to the dump!\nTime: {minutes}:{seconds:00}\nBig falls: {bigFallCount}";
    }

    // ---------------------------------------------------------------
    // Scene buttons
    // ---------------------------------------------------------------

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private void PlaySound(AudioClip clip)
    {
        // AudioSource ignores Time.timeScale, so sounds still play while paused/won.
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
