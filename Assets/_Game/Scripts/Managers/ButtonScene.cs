using UnityEngine;
using UnityEngine.UI;

public class ButtonScene : MonoBehaviour
{
    [SerializeField]
    private int sceneIndex;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }

    private void OnButtonClick()
    {
        if (PersistentSceneManager.Instance != null)
        {
            PersistentSceneManager.Instance.LoadSceneByIndex(sceneIndex);
        }
        else
        {
            Debug.LogError("PersistentSceneManager instance is not found in the scene.");
        }
    }
}