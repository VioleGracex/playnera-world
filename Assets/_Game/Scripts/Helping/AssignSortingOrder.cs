using UnityEngine;
using UnityEngine.U2D;

public class AssignSortingOrder : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private SpriteShapeRenderer spriteShapeRenderer;
    public float sortingOrderOffset = 0; // Offset for sorting order

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteShapeRenderer = GetComponent<SpriteShapeRenderer>();
        if (spriteRenderer != null)
        {
            // Assign sorting order based on the y position and add an offset
            spriteRenderer.sortingOrder = Mathf.RoundToInt((-transform.position.y + sortingOrderOffset) * 100);
        }
        else if( spriteShapeRenderer != null)
        {
            spriteShapeRenderer.sortingOrder = Mathf.RoundToInt((-transform.position.y + sortingOrderOffset) * 100);
        }
    }
}