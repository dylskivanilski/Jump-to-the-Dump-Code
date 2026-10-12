using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Start menu buttons. Put this on an object in the MainMenu scene and
/// hook the Start and Quit buttons' OnClick events to these methods.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameScene = "Game";

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameScene);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        // Application.Quit does nothing in the editor, so stop play mode instead.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
