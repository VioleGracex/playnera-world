using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class PersistentSceneManager : MonoBehaviour
{
    public static PersistentSceneManager Instance { get; private set; }
    private int currentSceneIndex;
    private int totalScenes;
    public GameObject loadingScreen;
    public Slider loadingBar;
    private SaveSystem saveSystem;

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

        saveSystem = FindFirstObjectByType<SaveSystem>();
    }

    private void Start()
    {
        totalScenes = SceneManager.sceneCountInBuildSettings;
        var savedData = saveSystem.LoadSceneData(SceneManager.GetActiveScene().buildIndex);
        if (savedData.sceneIndex != 1)
        {
            LoadSceneByIndex(savedData.sceneIndex);
        }
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
        loadingScreen.SetActive(false);

        if (currentSceneIndex == 1)
        {
             LoadSceneData();
        }
    }

    // Quit the game
    public void QuitGame()
    {
        // Save data if the current scene is scene 1
        if (currentSceneIndex == 1)
        {
            SaveCurrentSceneData();
        }

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
            LoadScene(currentSceneIndex - 1);
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
            LoadScene(currentSceneIndex + 1);
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
            if (currentSceneIndex == 1)
            {
                SaveCurrentSceneData();
            }
            LoadScene(sceneIndex);
        }
        else
        {
            Debug.Log("Invalid scene index. Please provide a valid index.");
        }
    }

    private void LoadScene(int sceneIndex)
    {
        StartCoroutine(LoadSceneAsync(sceneIndex));
    }

    private IEnumerator LoadSceneAsync(int sceneIndex)
    {
        loadingScreen.SetActive(true);
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            loadingBar.value = progress;
            yield return null;
        }
    }

    private void SaveCurrentSceneData()
    {
        List<Draggable> draggables = new List<Draggable>(FindObjectsByType<Draggable>(FindObjectsSortMode.None));
        saveSystem.SaveSceneData(currentSceneIndex, draggables);
    }

    private void LoadSceneData()
    {
        var sceneData = saveSystem.LoadSceneData(currentSceneIndex);

        foreach (var data in sceneData.draggables)
        {
            GameObject obj = GameObject.Find(data.name);
        
            if (obj != null)
            {
                Draggable draggable = obj.GetComponent<Draggable>();
                if (draggable != null)
                {
                    draggable.transform.position = data.position;
                    draggable.transform.localScale = data.scale;
                    draggable.GetComponent<SpriteRenderer>().sortingOrder = data.sortingOrder;

                    if (draggable.Type == DraggableType.Person)
                    {
                        DraggablePerson person = draggable as DraggablePerson;
                        person.currentPose = data.pose;
                        person.ChangePoseTo(data.pose);

                        if (!string.IsNullOrEmpty(data.heldItem))
                        {
                            DraggableItem heldItem = FindObjectsByType<DraggableItem>(FindObjectsSortMode.None).FirstOrDefault(i => i.name == data.heldItem);
                            if (heldItem != null)
                            {
                                person.HoldItem(heldItem);
                            }
                        }
                    }
                }
            }
        }
    }
}