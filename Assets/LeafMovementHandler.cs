using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeafMovementHandler : MonoBehaviour
{
    [Header("Target Points")]
    [SerializeField] private Transform target1;
    [SerializeField] private Transform target2;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed1 = 2.0f;
    [SerializeField] private float moveSpeed2 = 1.0f;
    [SerializeField] private float detectionThreshold = 0.01f;
    [SerializeField] private float arrivalDistance = 0.05f;

    [Header("References")]
    [SerializeField] private GameObject distanceHandGrabObject;

    private Vector3 _lastPosition;
    private bool _hasBeenMoved = false;
    private bool _sequenceStarted = false;
    private Rigidbody _rigidbody;

    void Start()
    {
        // Store initial position
        _lastPosition = transform.position;
        _rigidbody = GetComponent<Rigidbody>();

        if (target1 == null || target2 == null)
        {
            Debug.LogError("Target points not assigned in LeafMovementHandler!");
        }

        if (distanceHandGrabObject == null)
        {
            // Try to find the Distance Hand Grab child if not assigned
            Transform child = transform.Find("DistanceHandGrab");
            if (child != null)
            {
                distanceHandGrabObject = child.gameObject;
            }
            else
            {
                Debug.LogWarning("Distance Hand Grab object not assigned and not found as child!");
            }
        }
    }

    private Vector3 _previousFramePos;
    private Vector3 _currentFramePos;
    private bool _isCurrentlyMoving = false;
    private float _stationaryTime = 0f;

    void Update()
    {
        _currentFramePos = transform.position;

        // Check if the leaf has been moved significantly (grabbed)
        if (!_hasBeenMoved && Vector3.Distance(_currentFramePos, _lastPosition) > detectionThreshold)
        {
            _hasBeenMoved = true;
            Debug.Log("Leaf was grabbed!");
            _isCurrentlyMoving = true;
        }

        // If the leaf has been grabbed, check for release
        if (_hasBeenMoved && !_sequenceStarted)
        {
            float currentMovement = Vector3.Distance(_currentFramePos, _previousFramePos);

            // Check if leaf is currently moving
            if (currentMovement > 0.002f) // Small threshold for movement detection
            {
                _isCurrentlyMoving = true;
                _stationaryTime = 0f;
            }
            else if (_isCurrentlyMoving) // Was moving but stopped
            {
                _stationaryTime += Time.deltaTime;

                // If stationary for enough time, consider it released
                if (_stationaryTime > 0.25f)
                {
                    _isCurrentlyMoving = false;
                    OnLeafReleased();
                }
            }
        }

        // Update positions for next frame
        _previousFramePos = _currentFramePos;
        _lastPosition = transform.position;
    }

    void OnLeafReleased()
    {
        if (_sequenceStarted) return; // Prevent multiple calls

        Debug.Log("Leaf was released! Starting animation sequence.");

        // Disable the Distance Hand Grab component to prevent further grabbing
        if (distanceHandGrabObject != null)
        {
            distanceHandGrabObject.SetActive(false);
        }

        // Start the animation sequence immediately since we've detected the release
        _sequenceStarted = true;
        StartCoroutine(MoveSequence());
    }

    // No longer needed as we're handling release detection directly in Update
    // This removes the WaitForRelease coroutine completely

    IEnumerator MoveSequence()
    {
        Debug.Log("Starting leaf movement sequence");

        // Make sure the rigidbody doesn't interfere with our controlled movement
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        // Move to first target
        yield return StartCoroutine(MoveToTarget(target1.position, moveSpeed1));

        // Small pause at target 1
        yield return new WaitForSeconds(0.2f);

        // Move to second target
        yield return StartCoroutine(MoveToTarget(target2.position, moveSpeed2));

        Debug.Log("Leaf movement sequence completed");

        // You might want to add additional behavior here after the sequence completes
    }

    IEnumerator MoveToTarget(Vector3 targetPosition, float speed)
    {
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float journeyLength = distance;
        float startTime = Time.time;

        while (Vector3.Distance(transform.position, targetPosition) > arrivalDistance)
        {
            // Calculate how far we should have moved by now
            float distCovered = (Time.time - startTime) * speed;
            float fractionOfJourney = distCovered / journeyLength;

            // Set the position using lerp
            transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);

            yield return null;
        }

        // Ensure we're exactly at the target position
        transform.position = targetPosition;
    }
}