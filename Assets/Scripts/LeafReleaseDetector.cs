using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class LeafReleaseDetector : MonoBehaviour
{
    [Header("Target Points")]
    [SerializeField] private Transform target1;
    [SerializeField] private Transform target2;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed1 = 2.0f;
    [SerializeField] private float moveSpeed2 = 1.0f;
    [SerializeField] private float arrivalDistance = 0.05f;

    [Header("Components To Disable On Release")]
    [SerializeField] private MonoBehaviour[] componentsToDisable;
    [SerializeField] private GameObject[] gameObjectsToDisable;

    private Rigidbody rb;
    private int leafInstanceID;
    private bool animationStarted = false;
    private Vector3 lastFramePosition;
    private Vector3 lastHandPosition;
    private bool wasGrabbed = false;
    private float releaseTimer = 0f;
    private float releaseThreshold = 1.0f; // Time to confirm release

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastFramePosition = transform.position;

        leafInstanceID = GetInstanceID();

        // Ensure targets are assigned
        if (target1 == null || target2 == null)
        {
            Debug.LogError("Target points not assigned in LeafReleaseDetector!");
        }

        // Use a Collider trigger to detect hand proximity
        Collider leafCollider = GetComponent<Collider>();
        if (leafCollider != null)
        {
            leafCollider.isTrigger = true;
        }
    }

    void Update()
    {
        // Check if object is being moved (likely grabbed)
        float movement = Vector3.Distance(transform.position, lastFramePosition);

        if (movement > 0.001f) // Object is moving
        {
            // Si es la primera vez que se detecta que fue agarrada
            if (!wasGrabbed)
            {
                wasGrabbed = true;
                // Registrar en telemetría que se agarró la hoja
                TelemetriaManager.Instance.RegistrarHojaAgarrada(leafInstanceID);
            }

            releaseTimer = 0f; // Reset release timer
            lastHandPosition = transform.position;
        }
        // If it was grabbed but now seems stationary
        else if (wasGrabbed && !animationStarted)
        {
            // Start counting time since potential release
            releaseTimer += Time.deltaTime;

            // After threshold, consider it released
            if (releaseTimer >= releaseThreshold)
            {
                StartLeafAnimation();
                wasGrabbed = false;
            }
        }

        lastFramePosition = transform.position;
    }

    // This gets called by a Physics trigger when hands enter the object's collider
    void OnTriggerEnter(Collider other)
    {
        // Check if the collider is a hand
        if (other.gameObject.name.Contains("Hand") ||
            other.gameObject.tag == "Hand" ||
            other.transform.root.name.Contains("Hand"))
        {
            Debug.Log("Hand entered leaf collider");
        }
    }

    // This gets called when hands exit the object's collider
    void OnTriggerExit(Collider other)
    {
        // Check if the collider is a hand
        if ((other.gameObject.name.Contains("Hand") ||
             other.gameObject.tag == "Hand" ||
             other.transform.root.name.Contains("Hand")) &&
            wasGrabbed && !animationStarted)
        {
            Debug.Log("Hand exited leaf collider - likely released");
            StartLeafAnimation();
        }
    }

    void StartLeafAnimation()
    {
        if (animationStarted) return; // Prevent double activation

        TelemetriaManager.Instance.RegistrarHojaSoltada(leafInstanceID);

        Debug.Log("Starting leaf animation sequence");
        animationStarted = true;

        // Disable grabbing components
        foreach (MonoBehaviour component in componentsToDisable)
        {
            if (component != null)
            {
                component.enabled = false;
                Debug.Log($"Disabled component: {component.GetType().Name}");
            }
        }

        foreach (GameObject obj in gameObjectsToDisable)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                Debug.Log($"Disabled GameObject: {obj.name}");
            }
        }

        // Start the movement sequence
        StartCoroutine(MoveSequence());
    }

    IEnumerator MoveSequence()
    {
        // Make sure physics doesn't interfere
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Move to first target
        Debug.Log($"Moving to target 1: {target1.position}");
        yield return StartCoroutine(MoveToTarget(target1.position, moveSpeed1));

        // Short pause
        yield return new WaitForSeconds(0.2f);

        // Move to second target
        Debug.Log($"Moving to target 2: {target2.position}");
        yield return StartCoroutine(MoveToTarget(target2.position, moveSpeed2));

        Debug.Log("Leaf animation sequence completed");
    }

    IEnumerator MoveToTarget(Vector3 targetPosition, float speed)
    {
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float journeyLength = distance;
        float startTime = Time.time;

        while (Vector3.Distance(transform.position, targetPosition) > arrivalDistance)
        {
            float distCovered = (Time.time - startTime) * speed;
            float fractionOfJourney = distCovered / journeyLength;

            transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);

            yield return null;
        }

        // Ensure we reach exactly the target position
        transform.position = targetPosition;
    }
}