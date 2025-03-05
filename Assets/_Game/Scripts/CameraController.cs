using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform background; // Assign the background GameObject in Unity
    private Camera cam;
    private Vector2 minBounds, maxBounds;
    private Vector3 lastTouchPosition;
    private bool isDragging = false;

    void Start()
    {
        cam = Camera.main; // Directly get the Main Camera
        AdjustCameraSize();
        CalculateBounds();
    }

    void Update()
    {
        if (Input.touchCount == 1) // Only one finger touch
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPosition = cam.ScreenToWorldPoint(touch.position);
                isDragging = true;
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector3 difference = lastTouchPosition - cam.ScreenToWorldPoint(touch.position);
                MoveCamera(difference);
                lastTouchPosition = cam.ScreenToWorldPoint(touch.position);
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                isDragging = false;
            }
        }
    }

    void AdjustCameraSize()
    {
        if (background == null) return;

        SpriteRenderer bgRenderer = background.GetComponent<SpriteRenderer>();
        if (bgRenderer == null) return;

        float bgHeight = bgRenderer.bounds.size.y;
        cam.orthographicSize = bgHeight / 2f; // Adjust camera size based on background height
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

    void MoveCamera(Vector3 difference)
    {
        Vector3 newPos = cam.transform.position + difference;
        newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
        newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y);
        cam.transform.position = newPos;
    }
}
