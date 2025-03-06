using System.Collections.Generic;
using UnityEngine;

public class DraggablePerson : Draggable
{
    public override DraggableType Type => DraggableType.Person;

    [SerializeField]
    private List<PoseSprite> poseSpritesList;

    [SerializeField]
    private Vector3 sittingOffset = new Vector3(0f, -0.5f, 0f); // Offset to adjust the person's position when sitting

    [SerializeField]
    private Vector3 sleepingOffset = new Vector3(0f, -0.7f, 0f); // Offset to adjust the person's position when sleeping

    private Dictionary<string, Sprite> poseSprites;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Initialize the dictionary with pose sprites
        poseSprites = new Dictionary<string, Sprite>();
        foreach (var poseSprite in poseSpritesList)
        {
            if (!poseSprites.ContainsKey(poseSprite.poseName))
            {
                poseSprites.Add(poseSprite.poseName, poseSprite.poseSprite);
            }
        }
    }

    public void ChangePose(string layerName)
    {
        if (spriteRenderer != null)
        {
            string pose = GetPoseFromLayer(layerName);
            if (poseSprites.TryGetValue(pose, out Sprite newSprite) && newSprite != null)
            {
                spriteRenderer.sprite = newSprite;
            }
            else
            {
                // Default to standing sprite if the specified pose does not exist or is null
                spriteRenderer.sprite = poseSprites["standing"];
            }

            // Adjust position based on pose
            AdjustPositionBasedOnPose(pose);
        }
    }

    private void AdjustPositionBasedOnPose(string pose)
    {
        if (pose == "sitting")
        {
            transform.position += sittingOffset;
        }
        else if (pose == "sleeping")
        {
            transform.position += sleepingOffset;
        }
    }

    public override void StopFallingAndReturnToNormalScale(Collider2D collider = null)
    {
        base.StopFallingAndReturnToNormalScale(collider);

        if (collider != null)
        {
            string layerName = LayerMask.LayerToName(collider.gameObject.layer);
            ChangePose(layerName);

            // Set sorting order 1 above the collider object
            SpriteRenderer colliderSpriteRenderer = collider.GetComponent<SpriteRenderer>();
            SpriteRenderer draggableSpriteRenderer = GetComponent<SpriteRenderer>();
            if (colliderSpriteRenderer != null)
            {
                draggableSpriteRenderer.sortingOrder = colliderSpriteRenderer.sortingOrder + 1;
            }
            else
            {
                AdjustOrderInLayer();
            }
        }
    }

    private string GetPoseFromLayer(string layerName)
    {
        switch (layerName)
        {
            case "SitFriendly":
                return "sitting";
            case "SleepFriendly":
                return "sleeping";
            // Add more cases for additional poses
            default:
                return "standing";
        }
    }
}