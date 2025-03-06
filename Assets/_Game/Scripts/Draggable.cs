using UnityEngine;

public abstract class Draggable : MonoBehaviour
{
    public Vector3 InitialScale { get; private set; }
    public abstract DraggableType Type { get; }

    protected SpriteRenderer spriteRenderer;

    private void Start()
    {
        InitialScale = transform.localScale; // Save the initial scale
        spriteRenderer = GetComponent<SpriteRenderer>();
        AdjustOrderInLayer();
    }

    public void AdjustOrderInLayer()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
        }
    }

}


public enum DraggableType
{
    Item,
    Person
}