using UnityEngine;

public class FireflyMovement : MonoBehaviour
{
    public float speed = 1f;
    public float moveRadius = 2f;
    private Vector3 initialPosition;
    private Vector3 targetPosition;

    void Start()
    {
        initialPosition = transform.position;
        GetNewTargetPosition();
    }

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            GetNewTargetPosition();
        }
    }

    void GetNewTargetPosition()
    {
        targetPosition = initialPosition + new Vector3(
            Random.Range(-moveRadius, moveRadius),
            Random.Range(-moveRadius, moveRadius),
            Random.Range(-moveRadius, moveRadius)
        );
    }
}
