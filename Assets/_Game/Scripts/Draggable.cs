using UnityEngine;

public class Draggable : MonoBehaviour
{
    public Vector3 InitialScale { get; private set; }

    private void Start()
    {
        InitialScale = transform.localScale; // Save the initial scale
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