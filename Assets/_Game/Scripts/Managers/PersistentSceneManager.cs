using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentSceneManager : MonoBehaviour
{
    public static PersistentSceneManager Instance { get; private set; }
    private int currentSceneIndex;
    private int totalScenes;

    private void Awake()
    {
        // Ensure the SceneManager is a singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        totalScenes = SceneManager.sceneCountInBuildSettings;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneIndex = scene.buildIndex;
    }

    // Quit the game
    public void QuitGame()
    {
        #if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying needs to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // Load the previous scene
    public void LoadPreviousScene()
    {
        if (currentSceneIndex > 0)
        {
            SceneManager.LoadScene(currentSceneIndex - 1);
        }
        else
        {
            Debug.Log("This is the first scene. There is no previous scene to load.");
        }
    }

    // Load the next scene
    public void LoadNextScene()
    {
        if (currentSceneIndex < totalScenes - 1)
        {
            SceneManager.LoadScene(currentSceneIndex + 1);
        }
        else
        {
            Debug.Log("This is the last scene. There is no next scene to load.");
        }
    }

    // Load a scene by index
    public void LoadSceneByIndex(int sceneIndex)
    {
        if (sceneIndex >= 0 && sceneIndex < totalScenes)
        {
            SceneManager.LoadScene(sceneIndex);
        }
        else
        {
            Debug.Log("Invalid scene index. Please provide a valid index.");
        }
    }
}