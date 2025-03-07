using System.Collections;
using System.Linq;
using UnityEngine;

public abstract class Draggable : MonoBehaviour
{
    #region Properties
    public Vector3 InitialScale { get; private set; }
    public abstract DraggableType Type { get; }
    #endregion

    #region Serialized Fields
    [SerializeField] private float fallSpeed = 5f; // Initial speed at which the object falls
    [SerializeField] private float fallAcceleration = 2f; // Acceleration of the fall
    [SerializeField] private float scaleSpeed = 8f; // Speed at which the object scales
    [SerializeField] public LayerMask validPlacementLayerMask; // Layer mask for valid placement
    [SerializeField] private Vector2 overlapSize = new Vector2(1f, 1f); // Size of the overlap area for placement check
    [SerializeField] private Vector2 overlapOffset = Vector2.zero; // Offset of the overlap area for placement check
    #endregion

    #region Public Properties
    public Vector2 OverlapSize => overlapSize; // Public property to access overlap size
    public Vector2 OverlapOffset => overlapOffset; // Public property to access overlap offset
    #endregion

    #region Private Fields
    protected SpriteRenderer spriteRenderer;
    private bool isBeingDragged = false;
    private Coroutine gravityCoroutine;
    private Coroutine scaleCoroutine;
    private Vector3 originalPosition; // To store the original position of the draggable
    #endregion

    #region Unity Methods
    private void Start()
    {
        InitialScale = transform.localScale; // Save the initial scale
        spriteRenderer = GetComponent<SpriteRenderer>();
        AdjustOrderInLayer();
        originalPosition = transform.position; // Save the original position
        StartGravity();
    }

    private void OnDrawGizmos()
    {
        // Draw the overlap area for placement check
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube((Vector2)transform.position + overlapOffset, overlapSize);
    }
    #endregion

    #region Public Methods
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
        StopGravity();
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
            StartGravity();
        }
    }

    public virtual void StopFallingAndReturnToNormalScale(Collider2D collider = null)
    {
        StopGravity();
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }

        scaleCoroutine = StartCoroutine(SmoothScale(transform, InitialScale, scaleSpeed));
    }
    #endregion

    #region Private Methods
    private void StartGravity()
    {
        if (gravityCoroutine == null)
        {
            gravityCoroutine = StartCoroutine(ApplyGravity());
        }
    }

    private void StopGravity()
    {
        if (gravityCoroutine != null)
        {
            StopCoroutine(gravityCoroutine);
            gravityCoroutine = null;
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
            var sortedColliders = colliders.OrderBy(c => CharacterPoseInfo.Instance.layerPriority[LayerMask.LayerToName(c.gameObject.layer)]).ToArray();
            if (sortedColliders.Length > 0)
            {
                StopFallingAndReturnToNormalScale(sortedColliders[0]);
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
    #endregion
}

public enum DraggableType
{
    Item,
    Person
}