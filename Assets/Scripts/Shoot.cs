using System.Collections;
using UnityEngine;
using TMPro;

public class Shoot : MonoBehaviour
{
    [Header("Basic Shooting Parameters")]
    [Tooltip("Time multiplier for the ball trajectory (higher = slower ball)")]
    [Range(0.5f, 5.0f)]
    public float timeScale = 1.0f;

    [Tooltip("Delay in seconds before shooting after spawning")]
    public float shootDelay = 5f;

    [Header("Target Area Settings")]
    [Tooltip("The minimum bounds of the target area")]
    public Vector3 targetAreaMin = new Vector3(-3.48f, 0.261f, 0f);

    [Tooltip("The maximum bounds of the target area")]
    public Vector3 targetAreaMax = new Vector3(3.54f, 2.274f, 0f);

    [Tooltip("If true, draws the target area as a wireframe in the editor")]
    public bool showTargetArea = true;

    [Header("Collision Settings")]
    [Tooltip("Tag of objects that will trigger the disappearance countdown")]
    public string collisionTag = "Collider";

    [Tooltip("Time in seconds before ball disappears after collision")]
    public float disappearDelay = 5f;

    [Header("Respawn Settings")]
    [Tooltip("The position to respawn the ball at")]
    public Vector3 respawnPosition = new Vector3(0f, 0.258f, 11f);

    [Tooltip("Total number of shots before cycle ends")]
    public int totalShots = 3;

    [Header("UI References")]
    [Tooltip("Reference to the TextMeshPro component that will display the countdown")]
    public TextMeshProUGUI countdownText;

    // Reference to components
    private Rigidbody rb;
    private Vector3 currentTargetPoint;
    private bool hasCollided = false;
    private int shotsFired = 0;
    private bool cycleActive = true;

    // Debug variables to visualize the trajectory
    private Vector3 calculatedVelocity;
    private float debugFlightTime;

    void Start()
    {
        // Find all audio listeners in the scene
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();

        // If there's more than one, disable all except the first one
        if (listeners.Length > 1)
        {
            Debug.Log("Found " + listeners.Length + " audio listeners. Keeping only one.");
            for (int i = 1; i < listeners.Length; i++)
            {
                listeners[i].enabled = false;
            }
        }
        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found! Please add a Rigidbody to this GameObject.");
            return;
        }

        // Check if countdown text is assigned
        if (countdownText == null)
        {
            Debug.LogWarning("Countdown TextMeshPro is not assigned! Please assign it in the inspector.");
        }
        else
        {
            // Clear the text initially
            countdownText.text = " ";
        }

        // Make sure the Rigidbody has appropriate settings
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Store initial position as respawn position if not specified
        if (respawnPosition == Vector3.zero)
        {
            respawnPosition = transform.position;
        }

        // Start the first shot
        StartCoroutine(ShootAfterDelay());
    }

    void OnDrawGizmos()
    {
        if (showTargetArea)
        {
            // Draw target area as a wireframe box
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                (targetAreaMin + targetAreaMax) / 2,
                new Vector3(
                    Mathf.Abs(targetAreaMax.x - targetAreaMin.x),
                    Mathf.Abs(targetAreaMax.y - targetAreaMin.y),
                    Mathf.Abs(targetAreaMax.z - targetAreaMin.z)
                )
            );

            // Draw the current target point if available
            if (currentTargetPoint != Vector3.zero)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(currentTargetPoint, 0.2f);

                // Draw predicted trajectory if we have velocity
                if (calculatedVelocity != Vector3.zero && Application.isEditor)
                {
                    Gizmos.color = Color.yellow;
                    Vector3 start = respawnPosition;
                    Vector3 velocity = calculatedVelocity;

                    const int STEPS = 50;
                    const float TIME_STEP = 0.1f; // 0.1 second intervals

                    for (int i = 0; i < STEPS; i++)
                    {
                        float t = i * TIME_STEP;
                        Vector3 nextPos = start + velocity * t + 0.5f * Physics.gravity * t * t;

                        if (i > 0)
                        {
                            Vector3 prevPos = start + velocity * (t - TIME_STEP) +
                                0.5f * Physics.gravity * (t - TIME_STEP) * (t - TIME_STEP);
                            Gizmos.DrawLine(prevPos, nextPos);
                        }
                    }
                }
            }
        }

        // Draw respawn position
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(respawnPosition, 0.3f);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if we collided with an object with the specified tag
        if (!hasCollided && collision.gameObject.CompareTag(collisionTag))
        {
            Debug.Log("Ball collided with an object tagged: " + collisionTag);
            hasCollided = true;

            // Start disappear countdown
            StartCoroutine(DisappearAfterDelay());
        }
    }

    IEnumerator DisappearAfterDelay()
    {
        Debug.Log("Ball will disappear in " + disappearDelay + " seconds...");

        // Wait for the specified delay
        yield return new WaitForSeconds(disappearDelay);

        // If we haven't reached the total shots limit, respawn
        if (shotsFired < totalShots && cycleActive)
        {
            yield return new WaitForSeconds(0.5f); // Brief pause before respawning
            Respawn();
        }
        else
        {
            Debug.Log("Cycle complete. Total shots: " + shotsFired);
        }
    }

    void Respawn()
    {
        // Reset the ball's physics state
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Move to respawn position
        transform.position = respawnPosition;
        transform.rotation = Quaternion.identity;

        // Reset collision state
        hasCollided = false;

        Debug.Log("Ball respawned at " + respawnPosition);

        // Start the countdown to shoot again
        StartCoroutine(ShootAfterDelay());
    }

    IEnumerator ShootAfterDelay()
    {
        Debug.Log("Ball will shoot in " + shootDelay + " seconds... (Shot " + (shotsFired + 1) + " of " + totalShots + ")");

        // Generate a random target point
        GenerateTargetPoint();

        // Wait for the specified delay with countdown
        for (int i = (int)shootDelay; i > 0; i--)
        {
            Debug.Log("Shooting in " + i + " seconds...");

            // Update the TextMeshPro with the countdown number
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
            }

            yield return new WaitForSeconds(1f);
        }

        // Clear the countdown text
        if (countdownText != null)
        {
            countdownText.text = " ";
        }

        // Increment shot counter
        shotsFired++;

        // Shoot the ball
        Debug.Log("Shooting ball now! (Shot " + shotsFired + " of " + totalShots + ")");

        // Make sure the ball isn't kinematic
        rb.isKinematic = false;

        // Calculate the base velocity needed to hit the target in the standard time
        Vector3 baseVelocity = CalculateBaseVelocity();

        // Apply the time scale to slow down the ball while preserving trajectory
        Vector3 scaledVelocity = baseVelocity / timeScale;

        // Apply the velocity to the ball
        rb.velocity = scaledVelocity;
        calculatedVelocity = scaledVelocity; // For debugging visualization

        Debug.Log("Applied initial velocity: " + scaledVelocity + " with magnitude: " + scaledVelocity.magnitude);
        Debug.Log("Time scale: " + timeScale + " (higher = slower ball)");
        Debug.Log("Estimated time to target: " + (CalculateBaseTime() * timeScale) + " seconds");
        Debug.Log("Ball shot towards target: " + currentTargetPoint);
    }

    void GenerateTargetPoint()
    {
        // Generate a random point inside the target area
        currentTargetPoint = new Vector3(
            Random.Range(targetAreaMin.x, targetAreaMax.x),
            Random.Range(targetAreaMin.y, targetAreaMax.y),
            Random.Range(targetAreaMin.z, targetAreaMax.z)
        );

        Debug.Log("Target point generated: " + currentTargetPoint);
    }

    float CalculateBaseTime()
    {
        // Calculate the horizontal distance to target
        Vector3 displacement = currentTargetPoint - respawnPosition;
        float horizontalDistance = new Vector3(displacement.x, 0, displacement.z).magnitude;

        // Calculate a base time using the original formula
        float baseTime = Mathf.Sqrt(horizontalDistance) * 0.3f;
        baseTime = Mathf.Clamp(baseTime, 0.5f, 3.0f);

        return baseTime;
    }

    Vector3 CalculateBaseVelocity()
    {
        // Get the displacement vector from start to target
        Vector3 displacement = currentTargetPoint - respawnPosition;

        // Extract the horizontal and vertical displacements
        float horizontalDistance = new Vector3(displacement.x, 0, displacement.z).magnitude;
        float verticalDistance = displacement.y;

        // Calculate the direction in the horizontal plane
        Vector3 horizontalDirection = new Vector3(displacement.x, 0, displacement.z).normalized;

        // Calculate the base time to reach the target (without timeScale applied)
        float baseTime = CalculateBaseTime();
        debugFlightTime = baseTime * timeScale; // Store the actual flight time

        // Calculate the gravity (positive value)
        float g = Mathf.Abs(Physics.gravity.y);

        // Calculate the vertical velocity component using the formula:
        // Δy = v₀y × t + 0.5 × (-g) × t²
        // Solving for v₀y: v₀y = (Δy + 0.5 × g × t²) / t
        float initialVerticalVelocity = (verticalDistance + 0.5f * g * baseTime * baseTime) / baseTime;

        // Calculate the horizontal velocity components
        float horizontalSpeed = horizontalDistance / baseTime;
        Vector3 horizontalVelocity = horizontalDirection * horizontalSpeed;

        // Combine to get the final velocity vector
        Vector3 velocity = new Vector3(horizontalVelocity.x, initialVerticalVelocity, horizontalVelocity.z);

        return velocity;
    }

    // Public method to restart the cycle
    public void RestartCycle()
    {
        // Stop all running coroutines
        StopAllCoroutines();

        // Reset state
        shotsFired = 0;
        hasCollided = false;
        cycleActive = true;

        // Clear the countdown text
        if (countdownText != null)
        {
            countdownText.text = " ";
        }

        // Respawn the ball
        Respawn();
    }

    // Public method to stop the cycle
    public void StopCycle()
    {
        cycleActive = false;
        StopAllCoroutines();

        // Clear the countdown text
        if (countdownText != null)
        {
            countdownText.text = " ";
        }
    }
}