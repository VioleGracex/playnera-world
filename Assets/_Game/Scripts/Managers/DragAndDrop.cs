/* using UnityEngine;

public class DragAndDrop : MonoBehaviour
{
    private Camera cam;
    private bool isDragging = false;
    private Vector3 offset;
    private Vector3 originalPosition;
    private Vector3 targetScale;
    private Vector3 originalScale;
    private float scaleSpeed = 5f;
    private float placementThreshold = 0.1f;
    public LayerMask draggableLayer; // Layer mask for draggable items

    void Start()
    {
        cam = Camera.main;
        originalPosition = transform.position;
        originalScale = transform.localScale;
        targetScale = originalScale * 1.2f; // Scale up by 20% while dragging
    }

    void Update()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchPosition = cam.ScreenToWorldPoint(touch.position);
            touchPosition.z = 0;

            if (touch.phase == TouchPhase.Began)
            {
                RaycastHit2D hit = Physics2D.Raycast(touchPosition, Vector2.zero, Mathf.Infinity, draggableLayer);
                if (hit.collider != null && hit.collider.gameObject == gameObject)
                {
                    isDragging = true;
                    offset = gameObject.transform.position - touchPosition;
                    CameraController camController = cam.GetComponent<CameraController>();
                    camController.StartSpriteDrag();
                    transform.localScale = targetScale;
                    AdjustOrderInLayer();
                }
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector3 newPosition = touchPosition + offset;
                transform.position = newPosition;
                AdjustOrderInLayer();
            }
            else if (touch.phase == TouchPhase.Ended && isDragging)
            {
                isDragging = false;
                CameraController camController = cam.GetComponent<CameraController>();
                camController.EndSpriteDrag();
                CheckPlacement();
            }
        }

        // Scale object back to original size smoothly
        if (!isDragging && transform.localScale != originalScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * scaleSpeed);
        }
    }

    void CheckPlacement()
    {
        Collider2D[] colliders = Physics2D.OverlapPointAll(transform.position);
        bool validPlacement = false;

        foreach (var collider in colliders)
        {
            if (collider.CompareTag("PlacementZone") && collider.OverlapPoint(transform.position))
            {
                validPlacement = true;
                Debug.Log("Valid placement area.");
                break;
            }
        }

        if (!validPlacement)
        {
            Debug.Log("Invalid placement area. Returning to original position.");
            transform.position = originalPosition;
        }
        else
        {
            originalPosition = transform.position; // Update original position to the new valid placement
        }
    }

    void AdjustOrderInLayer()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
        }
    }
} */