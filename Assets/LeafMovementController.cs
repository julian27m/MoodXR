using System.Collections;
using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// This script handles the movement of a leaf object after it is released from being grabbed.
/// It moves the leaf through two target points with configurable speeds.
/// </summary>
public class LeafMovementController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform targetPoint1;
    [SerializeField] private Transform targetPoint2;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed1 = 1.0f; // Speed to first target
    [SerializeField] private float moveSpeed2 = 0.5f; // Speed to second target
    [SerializeField] private float arrivalThreshold = 0.05f; // How close to consider "arrived"

    [Header("Optional Animation Settings")]
    [SerializeField] private bool rotateWhileMoving = true;
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    // Reference to the Grabbable component
    private Grabbable grabbable;
    private Rigidbody rb;

    // Movement state flags
    private bool isMovingToTarget1 = false;
    private bool isMovingToTarget2 = false;
    private bool isBeingGrabbed = false;

    private void Awake()
    {
        // Get references to components
        grabbable = GetComponent<Grabbable>();
        rb = GetComponent<Rigidbody>();

        if (grabbable == null)
        {
            Debug.LogError("LeafMovementController requires a Grabbable component");
        }

        if (rb == null)
        {
            Debug.LogError("LeafMovementController requires a Rigidbody component");
        }
    }

    private void OnEnable()
    {
        // Subscribe to the Grabbable's pointer events
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised += OnPointerEventRaised;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from the Grabbable's pointer events
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= OnPointerEventRaised;
        }
    }

    private void OnPointerEventRaised(PointerEvent evt)
    {
        // Handle different pointer events
        switch (evt.Type)
        {
            case PointerEventType.Select:
                // Object is being grabbed
                isBeingGrabbed = true;
                StopAllMovement();
                break;

            case PointerEventType.Unselect:
                // Object is released - start the movement sequence
                isBeingGrabbed = false;
                StartMovingToTargets();
                break;

            case PointerEventType.Cancel:
                // Grab was cancelled
                isBeingGrabbed = false;
                break;
        }
    }

    private void StopAllMovement()
    {
        // Stop any ongoing movement
        isMovingToTarget1 = false;
        isMovingToTarget2 = false;
        StopAllCoroutines();
    }

    private void StartMovingToTargets()
    {
        if (targetPoint1 == null || targetPoint2 == null)
        {
            Debug.LogWarning("Target points not assigned in LeafMovementController");
            return;
        }

        // Start the movement sequence
        StopAllMovement();
        StartCoroutine(MoveSequence());
    }

    private IEnumerator MoveSequence()
    {
        // Make sure physics doesn't interfere with our movement
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Move to first target
        isMovingToTarget1 = true;
        isMovingToTarget2 = false;
        yield return StartCoroutine(MoveToTarget(targetPoint1.position, moveSpeed1));

        // Small pause at first target
        yield return new WaitForSeconds(0.2f);

        // Move to second target
        isMovingToTarget1 = false;
        isMovingToTarget2 = true;
        yield return StartCoroutine(MoveToTarget(targetPoint2.position, moveSpeed2));

        // Done moving
        isMovingToTarget1 = false;
        isMovingToTarget2 = false;

        // Return control to physics system if needed
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }

    private IEnumerator MoveToTarget(Vector3 targetPosition, float speed)
    {
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float journeyLength = distance;
        float startTime = Time.time;

        // Keep moving until we reach the target or are interrupted
        while (Vector3.Distance(transform.position, targetPosition) > arrivalThreshold &&
              !isBeingGrabbed)
        {
            // Calculate progress based on time and speed
            float distanceCovered = (Time.time - startTime) * speed;
            float journeyFraction = distanceCovered / journeyLength;

            // Move the object
            transform.position = Vector3.Lerp(startPosition, targetPosition, journeyFraction);

            // Optional rotation
            if (rotateWhileMoving)
            {
                transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
            }

            yield return null;
        }

        // Make sure we're exactly at the target
        if (!isBeingGrabbed)
        {
            transform.position = targetPosition;
        }
    }

    private void Update()
    {
        // Debug visualization
        if (targetPoint1 != null && targetPoint2 != null)
        {
            // Draw lines to visualize the path in the Scene view
            Debug.DrawLine(transform.position, targetPoint1.position, isMovingToTarget1 ? Color.green : Color.gray);
            Debug.DrawLine(targetPoint1.position, targetPoint2.position, isMovingToTarget2 ? Color.green : Color.gray);
        }
    }
}