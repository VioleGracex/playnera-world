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
    [SerializeField] private float fallSpeed = 5f; // Initial speed at which the object falls
    [SerializeField] private float fallAcceleration = 2f; // Acceleration of the fall
    [SerializeField] private LayerMask itemFriendlyLayer; // Layer mask for item-friendly places
    [SerializeField] private LayerMask personFriendlyLayer; // Layer mask for person-friendly places
    [SerializeField] private LayerMask draggableLayer;
    private Coroutine scaleCoroutine;
    private Coroutine fallCoroutine;
    private int originalSortingOrder;

    private Dictionary<string, int> layerPriority = new Dictionary<string, int>
    {
        { "SitFriendly", 1 },
        { "PersonFriendly", 2 },
        { "ItemFriendly", 3 },
        { "Floor", 4 }
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
                camController.CheckEdgeScrolling(currentDraggable.transform.position);

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

        // Stop the fall coroutine if it's running
        if (fallCoroutine != null)
        {
            StopCoroutine(fallCoroutine);
            fallCoroutine = null;
        }

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

        CheckPlacement();
    }

    private void CheckPlacement()
    {
        LayerMask targetLayerMask = currentDraggable.Type == DraggableType.Person ? personFriendlyLayer : itemFriendlyLayer;
        Collider2D[] colliders = Physics2D.OverlapPointAll(currentDraggable.transform.position, targetLayerMask);

        // Sort colliders by their layer priority
        var sortedColliders = colliders.OrderBy(c => layerPriority[LayerMask.LayerToName(c.gameObject.layer)]).ToArray();

        bool validPlacement = false;

        foreach (var collider in sortedColliders)
        {
            if (collider.CompareTag("PlacementZone") && collider.OverlapPoint(currentDraggable.transform.position))
            {
                validPlacement = true;
                if (currentDraggable.Type == DraggableType.Person)
                {
                    HandlePersonPlacement(collider);
                }
                else
                {
                    HandleItemPlacement(collider);
                    //currentDraggable.AdjustOrderInLayer();
                }
                break;
            }
        }

        if (!validPlacement)
        {
            if (fallCoroutine != null)
                StopCoroutine(fallCoroutine);
            fallCoroutine = StartCoroutine(FallToValidPlacement(currentDraggable));
        }
        else
        {
            // Stop existing coroutine before starting a new one
            if (scaleCoroutine != null)
                StopCoroutine(scaleCoroutine);

            scaleCoroutine = StartCoroutine(SmoothScale(currentDraggable.transform, originalScale, scaleSpeed));
            currentDraggable = null;
        }
    }

    private void HandlePersonPlacement(Collider2D collider)
    {
        string pose = GetPoseFromLayer(collider.gameObject.layer);
        currentDraggable.GetComponent<DraggablePerson>().ChangePose(pose);

        // Set sorting order 1 above the collider object
        SpriteRenderer colliderSpriteRenderer = collider.GetComponent<SpriteRenderer>();
        SpriteRenderer draggableSpriteRenderer = currentDraggable.GetComponent<SpriteRenderer>();
        if (colliderSpriteRenderer != null && draggableSpriteRenderer != null)
        {
            draggableSpriteRenderer.sortingOrder = colliderSpriteRenderer.sortingOrder + 1;
        }
        else
        {
            currentDraggable.AdjustOrderInLayer();
        }
    }

    private void HandleItemPlacement(Collider2D collider)
    {
        // Set sorting order 1 above the collider object
        SpriteRenderer colliderSpriteRenderer = collider.GetComponent<SpriteRenderer>();
        SpriteRenderer draggableSpriteRenderer = currentDraggable.GetComponent<SpriteRenderer>();
        if (colliderSpriteRenderer != null && draggableSpriteRenderer != null)
        {
            Debug.Log("sort order " +colliderSpriteRenderer.sortingOrder);
            draggableSpriteRenderer.sortingOrder = colliderSpriteRenderer.sortingOrder + 1;
        }
        else
        {
            currentDraggable.AdjustOrderInLayer();
        }
    }

    private string GetPoseFromLayer(int layer)
    {
        switch (layer)
        {
            case int l when l == LayerMask.NameToLayer("SitFriendly"):
                return "sitting";
            // Add more cases for additional poses
            default:
                return "standing";
        }
    }

    private void CheckHoverPose()
    {
        LayerMask targetLayerMask = currentDraggable.Type == DraggableType.Person ? personFriendlyLayer : itemFriendlyLayer;
        Collider2D[] colliders = Physics2D.OverlapPointAll(currentDraggable.transform.position, targetLayerMask);

        bool isHoveringOverPlacementZone = false;

        foreach (var collider in colliders)
        {
            if (collider.OverlapPoint(currentDraggable.transform.position))
            {
                if (currentDraggable.Type == DraggableType.Person)
                {
                    isHoveringOverPlacementZone = true;
                    string pose = GetPoseFromLayer(collider.gameObject.layer);
                    currentDraggable.GetComponent<DraggablePerson>().ChangePose(pose);
                    if (pose == "sitting")
                    {
                        // Return scale to normal when hovering over a sitting place
                        if (scaleCoroutine != null)
                            StopCoroutine(scaleCoroutine);
                        scaleCoroutine = StartCoroutine(SmoothScale(currentDraggable.transform, originalScale, scaleSpeed));
                        currentDraggable.GetComponent<DraggablePerson>().ChangePose("sitting");
                    }
                    else
                    {
                        // Scale bigger when hovering over a non-sitting place
                        if (scaleCoroutine != null)
                            StopCoroutine(scaleCoroutine);
                        scaleCoroutine = StartCoroutine(SmoothScale(currentDraggable.transform, targetScale, scaleSpeed));
                        currentDraggable.GetComponent<DraggablePerson>().ChangePose("standing");
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
            currentDraggable.GetComponent<DraggablePerson>().ChangePose("standing");
        }
    }

    private IEnumerator FallToValidPlacement(Draggable draggable)
    {
        float currentFallSpeed = fallSpeed; // Initialize fall speed

        while (true)
        {
            // Increase fall speed over time to simulate acceleration
            currentFallSpeed += fallAcceleration * Time.deltaTime;
            draggable.transform.position += Vector3.down * currentFallSpeed * Time.deltaTime;

            LayerMask targetLayerMask = draggable.Type == DraggableType.Person ? personFriendlyLayer : itemFriendlyLayer;
            Collider2D[] colliders = Physics2D.OverlapPointAll(draggable.transform.position, targetLayerMask);

            // Sort colliders by their layer priority
            var sortedColliders = colliders.OrderBy(c => layerPriority[LayerMask.LayerToName(c.gameObject.layer)]).ToArray();

            foreach (var collider in sortedColliders)
            {
                if (collider.OverlapPoint(draggable.transform.position))
                {
                    if (draggable.Type == DraggableType.Person)
                    {
                        HandlePersonPlacement(collider);
                    }
                    else
                    {
                        HandleItemPlacement(collider);
                        //currentDraggable.AdjustOrderInLayer();
                    }

                    // Stop falling when a valid placement is found
                    if (scaleCoroutine != null)
                        StopCoroutine(scaleCoroutine);

                    scaleCoroutine = StartCoroutine(SmoothScale(draggable.transform, originalScale, scaleSpeed));
                    currentDraggable = null;
                    yield break;
                }
            }

            // Check if the draggable has fallen out of camera bounds
            Vector3 screenPosition = cam.WorldToScreenPoint(draggable.transform.position);
            if (screenPosition.y < 0)
            {
                draggable.transform.position = originalPosition;

                // Stop existing coroutine before starting a new one
                if (scaleCoroutine != null)
                    StopCoroutine(scaleCoroutine);

                scaleCoroutine = StartCoroutine(SmoothScale(draggable.transform, originalScale, scaleSpeed));
                currentDraggable = null;
                yield break;
            }

            yield return null;
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
}