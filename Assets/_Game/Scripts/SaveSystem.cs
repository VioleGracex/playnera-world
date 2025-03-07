using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    private string saveDirectory;

    private void Awake()
    {
        saveDirectory = Application.persistentDataPath + "/SaveData/";
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }
        Debug.Log("Save directory: " + saveDirectory);
    }

    private string GetSaveFilePath(int sceneIndex)
    {
        return saveDirectory + "scene_" + sceneIndex + "_Save.json";
    }

    public void SaveSceneData(int sceneIndex, List<Draggable> draggables)
    {
        SceneSaveData sceneData = new SceneSaveData
        {
            sceneIndex = sceneIndex,
            draggables = new List<DraggableData>()
        };

        foreach (var draggable in draggables)
        {
            DraggableData data = new DraggableData
            {
                name = draggable.gameObject.name,
                position = draggable.transform.position,
                type = draggable.Type,
                scale = draggable.transform.localScale,
                sortingOrder = draggable.GetComponent<SpriteRenderer>().sortingOrder
            };

            if (draggable.Type == DraggableType.Person)
            {
                DraggablePerson person = draggable as DraggablePerson;
                data.heldItem = person.HeldItem != null ? person.HeldItem.gameObject.name : null;
                data.pose = person.currentPose;
            }

            sceneData.draggables.Add(data);
        }

        string json = JsonUtility.ToJson(sceneData, true);
        File.WriteAllText(GetSaveFilePath(sceneIndex), json);
    }

    public SceneSaveData LoadSceneData(int sceneIndex)
    {
        string savePath = GetSaveFilePath(sceneIndex);
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            SceneSaveData data = JsonUtility.FromJson<SceneSaveData>(json);
            return data;
        }

        // Default to empty data if no save file exists
        return new SceneSaveData { sceneIndex = sceneIndex, draggables = new List<DraggableData>() };
    }

    [System.Serializable]
    public class SceneSaveData
    {
        public int sceneIndex;
        public List<DraggableData> draggables;
    }

    [System.Serializable]
    public class DraggableData
    {
        public string name;
        public Vector3 position;
        public DraggableType type;
        public Vector3 scale;
        public int sortingOrder;
        public string heldItem;
        public string pose;
    }
}