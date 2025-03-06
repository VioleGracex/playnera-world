using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DragAndDropManager : MonoBehaviour
{
    public static DragAndDropManager Instance;

    private Camera cam;
    private CameraController camController;
    private bool isDragging = false;
    private Vector3 offset;
    private Draggable currentDraggable;
    private Vector3 originalPosition;
    private Vector3 targetScale;
    private Vector3 originalScale;
    [SerializeField] private float scaleSpeed = 8f;
    [SerializeField] private LayerMask itemFriendlyLayer; // Layer mask for item-friendly places
    [SerializeField] private LayerMask personFriendlyLayer; // Layer mask for person-friendly places
    [SerializeField] private LayerMask draggableLayer;
    private int originalSortingOrder;
    private Coroutine scaleCoroutine;

    private Dictionary<string, int> layerPriority = new Dictionary<string, int>
    {
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

    private void Start()
    {
        cam = Camera.main;
        camController = cam.GetComponent<CameraController>(); // Connect to CameraController
    }

    private void Update()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchPosition = cam.ScreenToWorldPoint(touch.position);
            touchPosition.z = 0;

            if (touch.phase == TouchPhase.Began)
            {
                RaycastHit2D hit = Physics2D.Raycast(touchPosition, Vector2.zero, Mathf.Infinity, draggableLayer);
                if (hit.collider != null)
                {
                    currentDraggable = hit.collider.GetComponent<Draggable>();
                    if (currentDraggable != null)
                    {
                        StartDragging(currentDraggable, touchPosition);
                    }
                }
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector3 newPosition = touchPosition + offset;
                currentDraggable.transform.position = newPosition;

                // Inform camera controller to check position
                camController.HandleEdgeScrolling(currentDraggable.transform.position);

                // Check if hovering over a collider that the draggable can interact with
                if (currentDraggable.Type == DraggableType.Person)
                {
                    CheckHoverPose();
                }
            }
            else if (touch.phase == TouchPhase.Ended && isDragging)
            {
                StopDragging();
            }
        }

        // Scale object back to original size smoothly
        if (currentDraggable != null && !isDragging && currentDraggable.transform.localScale != originalScale)
        {
            currentDraggable.transform.localScale = Vector3.Lerp(currentDraggable.transform.localScale, originalScale, Time.deltaTime * scaleSpeed);
        }
    }

    private void StartDragging(Draggable draggable, Vector3 touchPosition)
    {
        isDragging = true;
        currentDraggable = draggable;
        originalPosition = draggable.transform.position;
        originalScale = draggable.InitialScale;
        targetScale = originalScale * 1.2f;
        offset = draggable.transform.position - touchPosition;

        draggable.StartDragging();

        // Save the original sorting order and set a high sorting order for dragging
        SpriteRenderer spriteRenderer = draggable.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalSortingOrder = spriteRenderer.sortingOrder;
            spriteRenderer.sortingOrder = 1000; // Set a high sorting order
        }

        camController.StartSpriteDrag(draggable); // Inform CameraController

        // Stop existing coroutine before starting a new one
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(SmoothScale(draggable.transform, targetScale, scaleSpeed));
    }

    private void StopDragging()
    {
        isDragging = false;
        camController.EndSpriteDrag(); // Inform CameraController to stop tracking

        // Reset the sorting order based on the y-position
        SpriteRenderer spriteRenderer = currentDraggable.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-currentDraggable.transform.position.y * 100);
        }

        bool validPlacement = CheckPlacement();
        currentDraggable.StopDragging(validPlacement);

        // Stop existing coroutine before starting a new one
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(SmoothScale(currentDraggable.transform, originalScale, scaleSpeed));
        currentDraggable = null;
    }

    private bool CheckPlacement()
    {
        LayerMask targetLayerMask = currentDraggable.Type == DraggableType.Person ? personFriendlyLayer : itemFriendlyLayer;
        Vector2 overlapPosition = (Vector2)currentDraggable.transform.position + Vector2.Scale(currentDraggable.OverlapOffset, currentDraggable.transform.localScale);
        Vector2 overlapSize = Vector2.Scale(currentDraggable.OverlapSize, currentDraggable.transform.localScale);
        Collider2D[] colliders = Physics2D.OverlapBoxAll(overlapPosition, overlapSize, 0f, targetLayerMask);

        // Sort colliders by their layer priority
        var sortedColliders = colliders.OrderBy(c => layerPriority[LayerMask.LayerToName(c.gameObject.layer)]).ToArray();

        foreach (var collider in sortedColliders)
        {
            if (collider.OverlapPoint(currentDraggable.transform.position))
            {
                string layerName = LayerMask.LayerToName(collider.gameObject.layer);
                if (currentDraggable.Type == DraggableType.Person)
                {
                    ((DraggablePerson)currentDraggable).ChangePose(layerName);
                }
                return true;
            }
        }
        return false;
    }

    private void CheckHoverPose()
    {
        LayerMask targetLayerMask = currentDraggable.Type == DraggableType.Person ? personFriendlyLayer : itemFriendlyLayer;
        Vector2 overlapPosition = (Vector2)currentDraggable.transform.position + Vector2.Scale(currentDraggable.OverlapOffset, currentDraggable.transform.localScale);
        Vector2 overlapSize = Vector2.Scale(currentDraggable.OverlapSize, currentDraggable.transform.localScale);
        Collider2D[] colliders = Physics2D.OverlapBoxAll(overlapPosition, overlapSize, 0f, targetLayerMask);

        bool isHoveringOverPlacementZone = false;

        foreach (var collider in colliders)
        {
            if (collider.OverlapPoint(currentDraggable.transform.position))
            {
                if (currentDraggable.Type == DraggableType.Person)
                {
                    isHoveringOverPlacementZone = true;
                    string layerName = LayerMask.LayerToName(collider.gameObject.layer);
                    ((DraggablePerson)currentDraggable).ChangePose(layerName);
                    if (layerName == "SitFriendly" || layerName == "SleepFriendly")
                    {
                        // Return scale to normal when hovering over a sit or sleep place
                        if (scaleCoroutine != null)
                            StopCoroutine(scaleCoroutine);
                        scaleCoroutine = StartCoroutine(SmoothScale(currentDraggable.transform, originalScale, scaleSpeed));
                    }
                    else
                    {
                        // Scale bigger when hovering over a non-sit or non-sleep place
                        if (scaleCoroutine != null)
                            StopCoroutine(scaleCoroutine);
                        scaleCoroutine = StartCoroutine(SmoothScale(currentDraggable.transform, targetScale, scaleSpeed));
                    }
                    break;
                }
            }
        }

        if (!isHoveringOverPlacementZone && currentDraggable.transform.localScale != targetScale)
        {
            // Scale bigger when hovering over a non-valid area
            if (scaleCoroutine != null)
                StopCoroutine(scaleCoroutine);
            scaleCoroutine = StartCoroutine(SmoothScale(currentDraggable.transform, targetScale, scaleSpeed));
            ((DraggablePerson)currentDraggable).ChangePose("standing");
        }
    }

    private IEnumerator SmoothScale(Transform target, Vector3 targetScale, float speed)
    {
        while (!Mathf.Approximately(target.localScale.x, targetScale.x) ||
               !Mathf.Approximately(target.localScale.y, targetScale.y))
        {
            target.localScale = Vector3.Lerp(target.localScale, targetScale, Time.deltaTime * speed);
            yield return null;
        }
        target.localScale = targetScale;
    }

    private void OnDrawGizmos()
    {
        // Draw the overlap area for the currently grabbed item
        if (currentDraggable != null)
        {
            Gizmos.color = Color.green;
            Vector2 position = (Vector2)currentDraggable.transform.position + Vector2.Scale(currentDraggable.OverlapOffset, currentDraggable.transform.localScale);
            Vector2 size = Vector2.Scale(currentDraggable.OverlapSize, currentDraggable.transform.localScale);
            Gizmos.DrawWireCube(position, size);
        }
    }
}