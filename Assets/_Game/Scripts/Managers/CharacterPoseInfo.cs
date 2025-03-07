using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class LayerInfo
{
    public string layerName;
    public string poseName;
}

public class CharacterPoseInfo : MonoBehaviour
{
    public static CharacterPoseInfo Instance;
    public Dictionary<string, int> layerPriority = new Dictionary<string, int>
    {
        { "Person", 0 },
        { "SitFriendly", 1 },
        { "SleepFriendly", 2 },
        { "PersonFriendly", 3 },
        { "ItemFriendly", 4 },
        { "Floor", 5 }
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [SerializeField]
    private List<string> allowedHoldingPoses = new List<string> { "standing", "sitting", "sittingholding", "holding" };

    [SerializeField]
    private List<LayerInfo> layerInfos = new List<LayerInfo>();

    public string GetPoseFromLayer(string layerName)
    {
        foreach (var layerInfo in layerInfos)
        {
            if (layerInfo.layerName == layerName)
            {
                return layerInfo.poseName;
            }
        }
        return "standing"; // Default pose
    }

    public bool IsPoseAllowedToHoldItem(string pose)
    {
        return allowedHoldingPoses.Contains(pose);
    }
}