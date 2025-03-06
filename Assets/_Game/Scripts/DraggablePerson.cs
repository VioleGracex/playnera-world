using System.Collections.Generic;
using UnityEngine;

public class DraggablePerson : Draggable
{
    public override DraggableType Type => DraggableType.Person;

    [SerializeField]
    private List<PoseSprite> poseSpritesList;

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

    public void ChangePose(string pose)
    {
        if (spriteRenderer != null)
        {
            if (poseSprites.TryGetValue(pose, out Sprite newSprite) && newSprite != null)
            {
                spriteRenderer.sprite = newSprite;
            }
            else
            {
                // Default to standing sprite if the specified pose does not exist or is null
                spriteRenderer.sprite = poseSprites["standing"];
            }
        }
    }
}