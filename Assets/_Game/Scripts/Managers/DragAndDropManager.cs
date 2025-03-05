using System.Collections;
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
    [SerializeField] private LayerMask draggableLayer;
    private Coroutine scaleCoroutine;
    private Coroutine fallCoroutine;

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
                currentDraggable.AdjustOrderInLayer();

                // Inform camera controller to check position
                camController.CheckEdgeScrolling(currentDraggable.transform.position);
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
        draggable.AdjustOrderInLayer();

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

        CheckPlacement();
    }

    private void CheckPlacement()
    {
        Collider2D[] colliders = Physics2D.OverlapPointAll(currentDraggable.transform.position);
        bool validPlacement = false;

        foreach (var collider in colliders)
        {
            if (collider.CompareTag("PlacementZone") && collider.OverlapPoint(currentDraggable.transform.position))
            {
                validPlacement = true;
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

    private IEnumerator FallToValidPlacement(Draggable draggable)
    {
        float currentFallSpeed = fallSpeed; // Initialize fall speed

        while (true)
        {
            // Increase fall speed over time to simulate acceleration
            currentFallSpeed += fallAcceleration * Time.deltaTime;
            draggable.transform.position += Vector3.down * currentFallSpeed * Time.deltaTime;

            Collider2D[] colliders = Physics2D.OverlapPointAll(draggable.transform.position);
            foreach (var collider in colliders)
            {
                if (collider.CompareTag("PlacementZone") && collider.OverlapPoint(draggable.transform.position))
                {
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