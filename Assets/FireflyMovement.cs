using UnityEngine;

public class FireflyMovement : MonoBehaviour
{
    public enum FireflyState { InChest, Emerging, Flying }

    public float speed = 0.3f;                  // Reducido para movimiento más lento
    public float emergingSpeed = 0.5f;          // Velocidad al salir del cofre (más lenta)
    public float moveRadius = 1.5f;             // Radio de movimiento general
    public float playerRadius = 3f;             // Radio alrededor del jugador
    public float hoverTime = 3f;                // Tiempo que permanece cerca de un punto objetivo
    public Transform player;                    // Referencias al jugador

    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private FireflyState currentState = FireflyState.InChest;
    private Vector3 emergingDirection;
    private float hoverTimer = 0f;

    void Start()
    {
        initialPosition = transform.position;

        // Al principio no hacemos nada, esperamos a que el cofre se abra
        if (GetComponent<Rigidbody>() != null)
        {
            GetComponent<Rigidbody>().useGravity = false;
            GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case FireflyState.InChest:
                // No hace nada, espera en el cofre
                break;

            case FireflyState.Emerging:
                // Sale volando muy lentamente en una dirección aleatoria
                transform.position += emergingDirection * emergingSpeed * Time.deltaTime;

                // Después de cierta distancia, cambia al estado de vuelo
                if (Vector3.Distance(transform.position, initialPosition) > 2f)
                {
                    currentState = FireflyState.Flying;

                    // Al entrar en modo Flying, define una posición inicial cerca del jugador
                    if (player != null)
                    {
                        initialPosition = player.position + Vector3.up * Random.Range(1f, 2f);
                    }
                    GetNewTargetPosition();
                    hoverTimer = 0f;
                }
                break;

            case FireflyState.Flying:
                // Incrementa el temporizador de vuelo estacionario
                hoverTimer += Time.deltaTime;

                // Si ha estado cerca del objetivo por suficiente tiempo, busca un nuevo objetivo
                if (Vector3.Distance(transform.position, targetPosition) < 0.2f || hoverTimer > hoverTime)
                {
                    GetNewTargetPosition();
                    hoverTimer = 0f;
                }

                // Movimiento suave y lento hacia el objetivo
                transform.position = Vector3.Lerp(transform.position, targetPosition, speed * Time.deltaTime);

                // Actualiza la posición inicial si el jugador se mueve
                if (player != null && Vector3.Distance(initialPosition, player.position) > playerRadius)
                {
                    initialPosition = player.position + Vector3.up * Random.Range(1f, 2f);
                    GetNewTargetPosition();
                }
                break;
        }
    }

    void GetNewTargetPosition()
    {
        // Si está volando, genera posiciones alrededor del jugador/posición inicial
        if (currentState == FireflyState.Flying)
        {
            // Crea un movimiento más aleatorio pero dentro de ciertos límites
            targetPosition = initialPosition + new Vector3(
                Random.Range(-playerRadius, playerRadius),
                Random.Range(0, playerRadius * 0.8f), // Principalmente por encima
                Random.Range(-playerRadius, playerRadius)
            );

            // Añade un pequeño movimiento sinusoidal para más naturalidad
            float time = Time.time * 0.5f; // Factor para ralentizar la oscilación
            targetPosition += new Vector3(
                Mathf.Sin(time) * 0.3f,
                Mathf.Cos(time * 0.7f) * 0.2f,
                Mathf.Sin(time * 0.5f) * 0.3f
            );
        }
        else
        {
            targetPosition = initialPosition + new Vector3(
                Random.Range(-moveRadius, moveRadius),
                Random.Range(-moveRadius, moveRadius),
                Random.Range(-moveRadius, moveRadius)
            );
        }
    }

    public void ReleaseFromChest()
    {
        currentState = FireflyState.Emerging;

        // Dirección aleatoria hacia arriba y hacia afuera, pero más lenta
        emergingDirection = new Vector3(
            Random.Range(-0.5f, 0.5f),
            Random.Range(0.5f, 0.8f), // Principalmente hacia arriba
            Random.Range(-0.5f, 0.5f)
        ).normalized;

        // Desactiva los efectos de física
        if (GetComponent<Rigidbody>() != null)
        {
            GetComponent<Rigidbody>().useGravity = false;
            GetComponent<Rigidbody>().isKinematic = true;
        }

        // Asegúrate de que el objeto esté activo
        gameObject.SetActive(true);
    }
}