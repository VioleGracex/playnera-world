using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public Transform background;
    private Camera cam;
    public Vector2 minBounds, maxBounds;
    private bool isDragging = false;
    private bool isDraggingSprite = false;
    private Draggable currentDraggable;
    private Vector3 lastTouchPosition;
    [SerializeField] private float edgeScrollSpeed = 1f; 
    [SerializeField] private float edgeThreshold = 50f; // Edge threshold in pixels
    [SerializeField] private float minSwipeDistance = 0.5f; // Minimum swipe distance to move the camera
    [SerializeField] private float cameraSmoothing = 0.1f; // Smoothing factor for camera movement
    [SerializeField] private float swipeScrollSpeed = 1f; // Speed factor for camera movement when swiping
    private Coroutine edgeScrollCoroutine;

    void Start()
    {
        cam = Camera.main;
        AdjustCameraSize();
        CalculateBounds();
    }

    void Update()
    {
        if (!isDraggingSprite && Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchPosition = cam.ScreenToWorldPoint(touch.position);
            touchPosition.z = 0;

            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPosition = touchPosition;
                isDragging = true;
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector3 difference = lastTouchPosition - touchPosition;
                if (difference.magnitude >= minSwipeDistance)
                {
                    MoveCamera(difference * swipeScrollSpeed);
                    lastTouchPosition = touchPosition; // Update lastTouchPosition
                }
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                isDragging = false;
            }
        }

        if (isDraggingSprite && currentDraggable != null)
        {
            HandleEdgeScrolling(currentDraggable.transform.position);
        }
    }

    void AdjustCameraSize()
    {
        if (background == null) return;

        SpriteRenderer bgRenderer = background.GetComponent<SpriteRenderer>();
        if (bgRenderer == null) return;

        float bgHeight = bgRenderer.bounds.size.y;
        cam.orthographicSize = bgHeight / 2f;
    }

    void CalculateBounds()
    {
        if (background == null) return;

        SpriteRenderer bgRenderer = background.GetComponent<SpriteRenderer>();

        float camHeight = cam.orthographicSize * 2;
        float camWidth = camHeight * cam.aspect;

        float bgWidth = bgRenderer.bounds.size.x;
        float bgHeight = bgRenderer.bounds.size.y;

        minBounds = new Vector2(background.position.x - bgWidth / 2 + camWidth / 2, 
                                background.position.y - bgHeight / 2 + camHeight / 2);
        maxBounds = new Vector2(background.position.x + bgWidth / 2 - camWidth / 2, 
                                background.position.y + bgHeight / 2 - camHeight / 2);
    }

    public void MoveCamera(Vector3 difference)
    {
        Vector3 targetPosition = cam.transform.position + difference;
        targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);

        // Smoothly move the camera to the target position
        cam.transform.position = Vector3.Lerp(cam.transform.position, targetPosition, cameraSmoothing);
    }

    public void HandleEdgeScrolling(Vector3 draggablePosition)
    {
        Vector3 screenPosition = cam.WorldToScreenPoint(draggablePosition);
        Vector3 cameraMovement = Vector3.zero;

        if (screenPosition.x < edgeThreshold && cam.transform.position.x > minBounds.x)
        {
            cameraMovement.x = -edgeScrollSpeed * Time.deltaTime;
        }
        else if (screenPosition.x > Screen.width - edgeThreshold && cam.transform.position.x < maxBounds.x)
        {
            cameraMovement.x = edgeScrollSpeed * Time.deltaTime;
        }

        if (screenPosition.y < edgeThreshold && cam.transform.position.y > minBounds.y)
        {
            cameraMovement.y = -edgeScrollSpeed * Time.deltaTime;
        }
        else if (screenPosition.y > Screen.height - edgeThreshold && cam.transform.position.y < maxBounds.y)
        {
            cameraMovement.y = edgeScrollSpeed * Time.deltaTime;
        }

        if (cameraMovement != Vector3.zero)
        {
            MoveCamera(cameraMovement);
        }
    }

    public void StartSpriteDrag(Draggable draggable)
    {
        isDraggingSprite = true;
        currentDraggable = draggable;
        if (edgeScrollCoroutine == null)
        {
            edgeScrollCoroutine = StartCoroutine(EdgeScroll());
        }
    }

    public void EndSpriteDrag()
    {
        isDraggingSprite = false;
        currentDraggable = null;
        if (edgeScrollCoroutine != null)
        {
            StopCoroutine(edgeScrollCoroutine);
            edgeScrollCoroutine = null;
        }
    }

    private IEnumerator EdgeScroll()
    {
        while (isDraggingSprite)
        {
            HandleEdgeScrolling(currentDraggable.transform.position);
            yield return null;
        }
    }
}