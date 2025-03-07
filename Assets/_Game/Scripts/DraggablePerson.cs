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

    [SerializeField]
    private Transform handLocation; // Transform to represent the hand location

    private Dictionary<string, Sprite> poseSprites;
    public DraggableItem HeldItem { get; private set; } // Property to reference the held item
    public string currentPose = "standing"; // Variable to track the current pose

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

    public void ChangePoseByLayer(string layerName)
    {
        string newPose = CharacterPoseInfo.Instance.GetPoseFromLayer(layerName);
        ChangePoseTo(newPose);
    }

    public void ChangePoseTo(string pose)
    {
        currentPose = pose;
        ValidatePose();

        if (spriteRenderer != null && poseSprites.TryGetValue(currentPose, out Sprite newSprite) && newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
            //AdjustPositionBasedOnPose(currentPose);
        }
    }

    public void ValidatePose()
    {
        // Automatically switch to holding pose if an item is held and the current pose has a holding counterpart
        if (HeldItem != null)
        {
            switch (currentPose)
            {
                case "standing":
                    currentPose = "holding";
                    break;
                case "sitting":
                    currentPose = "sittingholding";
                    break;
                // Add more cases for additional poses with holding counterparts
            }
        }
        else
        {
            // Automatically switch back to non-holding pose if no item is held
            switch (currentPose)
            {
                case "holding":
                    currentPose = "standing";
                    break;
                case "sittingholding":
                    currentPose = "sitting";
                    break;
                // Add more cases for additional poses with holding counterparts
            }
        }
        if (spriteRenderer != null && poseSprites.TryGetValue(currentPose, out Sprite newSprite) && newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
        }
    }
    public void ValidateHoveredOnPose(DraggableItem item)
    {
        // Automatically switch to holding pose on hover
        switch (currentPose)
        {
            case "standing":
                item.GetComponent<SpriteRenderer>().sortingOrder = spriteRenderer.sortingOrder + 1;
                currentPose = "holding";
                break;
            case "sitting":
                item.GetComponent<SpriteRenderer>().sortingOrder = spriteRenderer.sortingOrder + 1;
                currentPose = "sittingholding";
                break;
            // Add more cases for additional poses with holding counterparts
        }
        if (spriteRenderer != null && poseSprites.TryGetValue(currentPose, out Sprite newSprite) && newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
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

        // Update held item's position
        if (HeldItem != null)
        {
            HeldItem.transform.position = handLocation.position;
        }
    }

    public void HoldItem(DraggableItem item)
    {
        HeldItem = item;
        item.transform.SetParent(transform);
        item.transform.position = handLocation.position;
        item.GetComponent<SpriteRenderer>().sortingOrder = spriteRenderer.sortingOrder + 1;
        ValidatePose();
    }

    public void ReleaseItem()
    {
        if (HeldItem != null)
        {
            HeldItem.transform.SetParent(null);
            HeldItem = null;
            ValidatePose(); // Reset the pose after releasing the item
        }
    }

    public override void StopFallingAndReturnToNormalScale(Collider2D collider = null)
    {
        base.StopFallingAndReturnToNormalScale(collider);

        if (collider != null)
        {
            string layerName = LayerMask.LayerToName(collider.gameObject.layer);
            ChangePoseByLayer(layerName);

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

    private void OnDrawGizmos()
    {
        // Draw a gizmo to represent the hand location
        if (handLocation != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(handLocation.position, 0.1f);
        }
    }
}