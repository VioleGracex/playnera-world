using UnityEngine;

public class AssignSortingOrder : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    public float sortingOrderOffset = 0; // Offset for sorting order

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Assign sorting order based on the y position and add an offset
            spriteRenderer.sortingOrder = Mathf.RoundToInt((-transform.position.y + sortingOrderOffset) * 100);
        }
    }
}