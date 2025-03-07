using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DragAndDropManager : MonoBehaviour
{
    #region Singleton
    public static DragAndDropManager Instance;

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
    #endregion

    #region Variables
    private Camera cam;
    private CameraController camController;
    private bool isDragging = false;
    private Vector3 offset;
    private Draggable currentDraggable;
    private Vector3 originalPosition;
    private Vector3 targetScale;
    private Vector3 originalScale;
    private DraggablePerson lastHoveredPerson = null;
    [SerializeField] private float scaleSpeed = 8f;
    [SerializeField] private LayerMask draggableLayer;
    private int originalSortingOrder;
    private Coroutine scaleCoroutine;
    #endregion

    #region Unity Methods
    private void Start()
    {
        cam = Camera.main;
        camController = cam.GetComponent<CameraController>(); // Connect to CameraController
    }

    private void Update()
    {
        HandleTouchInput();
        HandleScaling();
    }
    #endregion

    #region Drag and Drop Logic
    private void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchPosition = cam.ScreenToWorldPoint(touch.position);
            touchPosition.z = 0;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleTouchBegan(touchPosition);
                    break;
                case TouchPhase.Moved:
                    HandleTouchMoved(touchPosition);
                    break;
                case TouchPhase.Ended:
                    HandleTouchEnded();
                    break;
            }
        }
    }

    private void HandleTouchBegan(Vector3 touchPosition)
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

    private void HandleTouchMoved(Vector3 touchPosition)
    {
        if (isDragging)
        {
            Vector3 newPosition = touchPosition + offset;
            currentDraggable.transform.position = newPosition;

            // Inform camera controller to check position
            camController.HandleEdgeScrolling(currentDraggable.transform.position);

            // Check if hovering over a collider that the draggable can interact with
            if (currentDraggable.Type == DraggableType.Person)
            {
                CheckHoverPerson();
            }
            else if (currentDraggable.Type == DraggableType.Item && currentDraggable.GetComponent<DraggableItem>().canBeHeld)
            {
                CheckHoverItem();
            }
        }
    }

    private void HandleTouchEnded()
    {
        if (isDragging)
        {
            StopDragging();
        }
    }

    private void HandleScaling()
    {
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
            if (currentDraggable.Type == DraggableType.Person)
            {
                SetHeldItemSortingOrder(((DraggablePerson)currentDraggable));
            }
            else if (currentDraggable.Type == DraggableType.Item)
            {
                ReleaseParentItem();
            }
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
        LayerMask targetLayerMask = currentDraggable.validPlacementLayerMask;
        Vector2 overlapPosition = (Vector2)currentDraggable.transform.position + Vector2.Scale(currentDraggable.OverlapOffset, currentDraggable.transform.localScale);
        Vector2 overlapSize = Vector2.Scale(currentDraggable.OverlapSize, currentDraggable.transform.localScale);
        Collider2D[] colliders = Physics2D.OverlapBoxAll(overlapPosition, overlapSize, 0f, targetLayerMask);

        // Sort colliders by their layer priority
        var sortedColliders = colliders.OrderBy(c => CharacterPoseInfo.Instance.layerPriority[LayerMask.LayerToName(c.gameObject.layer)]).ToArray();

        if (sortedColliders.Length > 0)
        {
            Collider2D collider = sortedColliders[0];
            if (collider.OverlapPoint(currentDraggable.transform.position))
            {
                string layerName = LayerMask.LayerToName(collider.gameObject.layer);

                if (currentDraggable.Type == DraggableType.Item && collider.GetComponent<DraggablePerson>() != null)
                {
                    var draggablePerson = collider.GetComponent<DraggablePerson>();
                    if (draggablePerson.HeldItem == null && ((DraggableItem)currentDraggable).canBeHeld && 
                        CharacterPoseInfo.Instance.IsPoseAllowedToHoldItem(draggablePerson.currentPose))
                    {
                        draggablePerson.HoldItem((DraggableItem)currentDraggable);
                    }
                }
                return true;
            }
        }
        return false;
    }
    #endregion

    #region Hover Checks
    private void CheckHoverPerson()
    {
        LayerMask targetLayerMask = currentDraggable.validPlacementLayerMask;
        Vector2 overlapPosition = (Vector2)currentDraggable.transform.position + Vector2.Scale(currentDraggable.OverlapOffset, currentDraggable.transform.localScale);
        Vector2 overlapSize = Vector2.Scale(currentDraggable.OverlapSize, currentDraggable.transform.localScale);
        Collider2D[] colliders = Physics2D.OverlapBoxAll(overlapPosition, overlapSize, 0f, targetLayerMask);

        var sortedColliders = colliders.OrderBy(c => CharacterPoseInfo.Instance.layerPriority[LayerMask.LayerToName(c.gameObject.layer)]).ToArray();

        if (sortedColliders.Length > 0)
        {
            Collider2D collider = sortedColliders[0];
            string layerName = LayerMask.LayerToName(collider.gameObject.layer);
            ((DraggablePerson)currentDraggable).ChangePoseByLayer(layerName);
            HandleScaleForHover(layerName);
        }
        else if (currentDraggable.transform.localScale != targetScale)
        {
            // Scale bigger when hovering over a non-valid area
            if (scaleCoroutine != null)
                StopCoroutine(scaleCoroutine);
            scaleCoroutine = StartCoroutine(SmoothScale(currentDraggable.transform, targetScale, scaleSpeed));
            ((DraggablePerson)currentDraggable).ChangePoseTo("standing");
        }
    }

    private void CheckHoverItem()
    {
        Vector2 overlapPosition = (Vector2)currentDraggable.transform.position + Vector2.Scale(currentDraggable.OverlapOffset, currentDraggable.transform.localScale);
        Vector2 overlapSize = Vector2.Scale(currentDraggable.OverlapSize, currentDraggable.transform.localScale);
        Collider2D[] colliders = Physics2D.OverlapBoxAll(overlapPosition, overlapSize, 0f, currentDraggable.validPlacementLayerMask);

        var personCollider = colliders.FirstOrDefault(c => LayerMask.LayerToName(c.gameObject.layer) == "Person");

        if (personCollider != null)
        {
            var draggablePerson = personCollider.GetComponent<DraggablePerson>();
            if (draggablePerson != null && draggablePerson.HeldItem == null && CharacterPoseInfo.Instance.IsPoseAllowedToHoldItem(draggablePerson.currentPose))
            {
                draggablePerson.ValidateHoveredOnPose((DraggableItem)currentDraggable);

                if (lastHoveredPerson != null && lastHoveredPerson != draggablePerson)
                {
                    lastHoveredPerson.ValidatePose();
                }
                lastHoveredPerson = draggablePerson;
            }
        }
        else if (lastHoveredPerson != null && lastHoveredPerson.HeldItem == null)
        {
            lastHoveredPerson.ValidatePose();
            lastHoveredPerson = null;
        }
    }
    #endregion

    #region Utilities
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

    private void SetHeldItemSortingOrder(DraggablePerson draggablePerson)
    {
        if (draggablePerson.HeldItem != null)
        {
            draggablePerson.HeldItem.GetComponent<SpriteRenderer>().sortingOrder = 1001;
        }
    }

    private void ReleaseParentItem()
    {
        if (currentDraggable.transform.parent != null)
        {
            if (currentDraggable.transform.parent.GetComponent<DraggablePerson>() != null)
            {
                DraggablePerson itemParent = currentDraggable.transform.parent.GetComponent<DraggablePerson>();
                itemParent.ReleaseItem();
            }
        }
    }

    private void HandleScaleForHover(string layerName)
    {
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
    }
    #endregion
}