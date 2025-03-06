using System.Collections;
using UnityEngine;

public abstract class Draggable : MonoBehaviour
{
    public Vector3 InitialScale { get; private set; }
    public abstract DraggableType Type { get; }

    protected SpriteRenderer spriteRenderer;
    private bool isBeingDragged = false;
    private Coroutine gravityCoroutine;
    private Coroutine scaleCoroutine;

    [SerializeField] private float fallSpeed = 5f; // Initial speed at which the object falls
    [SerializeField] private float fallAcceleration = 2f; // Acceleration of the fall
    [SerializeField] private float scaleSpeed = 8f; // Speed at which the object scales
    [SerializeField] private LayerMask validPlacementLayerMask; // Layer mask for valid placement
    [SerializeField] private Vector2 overlapSize = new Vector2(1f, 1f); // Size of the overlap area for placement check
    [SerializeField] private Vector2 overlapOffset = Vector2.zero; // Offset of the overlap area for placement check

    public Vector2 OverlapSize => overlapSize; // Public property to access overlap size
    public Vector2 OverlapOffset => overlapOffset; // Public property to access overlap offset

    private Vector3 originalPosition; // To store the original position of the draggable

    private void Start()
    {
        InitialScale = transform.localScale; // Save the initial scale
        spriteRenderer = GetComponent<SpriteRenderer>();
        AdjustOrderInLayer();
        originalPosition = transform.position; // Save the original position
        gravityCoroutine = StartCoroutine(ApplyGravity());
    }

    private void OnDrawGizmos()
    {
        // Draw the overlap area for placement check
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube((Vector2)transform.position + overlapOffset, overlapSize);
    }

    public void AdjustOrderInLayer()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
        }
    }

    public void StartDragging()
    {
        isBeingDragged = true;
        if (gravityCoroutine != null)
        {
            StopCoroutine(gravityCoroutine);
            gravityCoroutine = null;
        }
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
    }

    public void StopDragging(bool validPlacement)
    {
        isBeingDragged = false;
        if (!validPlacement)
        {
            gravityCoroutine = StartCoroutine(ApplyGravity());
        }
    }

    private IEnumerator ApplyGravity()
    {
        float currentFallSpeed = fallSpeed;
        Vector3 targetScale = InitialScale * 0.8f; // Scale down while falling

        while (true)
        {
            currentFallSpeed += fallAcceleration * Time.deltaTime;
            transform.position += Vector3.down * currentFallSpeed * Time.deltaTime;

            if (scaleCoroutine == null)
            {
                scaleCoroutine = StartCoroutine(SmoothScale(transform, targetScale, scaleSpeed));
            }

            // Check for collision with valid placement layer mask
            Collider2D[] colliders = Physics2D.OverlapBoxAll((Vector2)transform.position + overlapOffset, overlapSize, 0f, validPlacementLayerMask);
            if (colliders.Length > 0)
            {
                StopFallingAndReturnToNormalScale(colliders[0]);
                AdjustOrderInLayer();
                yield break;
            }

            // Check if the draggable has fallen out of camera bounds
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position);
            if (screenPosition.y < 0)
            {
                transform.position = originalPosition;
                StopFallingAndReturnToNormalScale();
                yield break;
            }

            yield return null;
        }
    }

    public IEnumerator SmoothScale(Transform target, Vector3 targetScale, float speed)
    {
        while (!Mathf.Approximately(target.localScale.x, targetScale.x) ||
               !Mathf.Approximately(target.localScale.y, targetScale.y))
        {
            target.localScale = Vector3.Lerp(target.localScale, targetScale, Time.deltaTime * speed);
            yield return null;
        }
        target.localScale = targetScale;
    }

    public virtual void StopFallingAndReturnToNormalScale(Collider2D collider = null)
    {
        if (gravityCoroutine != null)
        {
            StopCoroutine(gravityCoroutine);
            gravityCoroutine = null;
        }
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }

        scaleCoroutine = StartCoroutine(SmoothScale(transform, InitialScale, scaleSpeed));
    }
}

public enum DraggableType
{
    Item,
    Person
}