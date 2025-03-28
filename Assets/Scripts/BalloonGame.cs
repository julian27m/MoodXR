using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BalloonGame : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject[] balloons; // Array para almacenar los 40 globos en la escena
    [SerializeField] private TextMeshPro scoreText;
    [SerializeField] private AudioClip popSound;

    [Header("Configuración")]
    [SerializeField] private float gameDuration = 60f; // Duración del juego en segundos

    private GameObject currentActiveBalloon;
    private int score = 0;
    private bool gameRunning = false;
    private AudioSource audioSource;
    private List<GameObject> availableBalloons = new List<GameObject>();

    private void Awake()
    {
        // Asegurarse de tener un AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Inicializar la lista de globos disponibles
        if (balloons != null && balloons.Length > 0)
        {
            availableBalloons.AddRange(balloons);
        }
        else
        {
            Debug.LogError("No hay globos asignados al script BalloonGame. Por favor, asigna los globos en el Inspector.");
        }
    }

    private void Start()
    {
        // Desactivar todos los globos al inicio
        DeactivateAllBalloons();
    }

    // Método público para iniciar el juego desde otros scripts
    public void StartGame()
    {
        if (gameRunning) return;

        // Reiniciar valores
        score = 0;
        UpdateScoreDisplay();
        gameRunning = true;

        // Restablecer la lista de globos disponibles
        availableBalloons.Clear();
        availableBalloons.AddRange(balloons);

        // Activar el primer globo
        ActivateRandomBalloon();

        // Comenzar temporizador
        StartCoroutine(GameTimerRoutine());
    }

    // Rutina para el tiempo de juego
    private IEnumerator GameTimerRoutine()
    {
        yield return new WaitForSeconds(gameDuration);
        EndGame();
    }

    // Terminar el juego
    private void EndGame()
    {
        gameRunning = false;

        // Desactivar el globo actual si existe
        if (currentActiveBalloon != null)
        {
            currentActiveBalloon.SetActive(false);
        }

        // Aquí podrías añadir lógica adicional para el final del juego
        // como mostrar una pantalla de fin o un mensaje
    }

    // Desactivar todos los globos
    private void DeactivateAllBalloons()
    {
        foreach (GameObject balloon in balloons)
        {
            if (balloon != null)
            {
                // Verificar y añadir componentes necesarios para las colisiones
                if (balloon.GetComponent<Collider>() == null)
                {
                    balloon.AddComponent<SphereCollider>();
                }

                // Añadir Rigidbody para mejorar la detección de colisiones, pero mantenerlo estático
                Rigidbody rb = balloon.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = balloon.AddComponent<Rigidbody>();
                }
                rb.isKinematic = true; // Para que no se mueva pero detecte colisiones
                rb.useGravity = false; // Para que no caiga

                // Asegurarse de que cada globo tenga el script de controlador
                if (balloon.GetComponent<BalloonController>() == null)
                {
                    balloon.AddComponent<BalloonController>().Initialize(this);
                }
                else
                {
                    balloon.GetComponent<BalloonController>().Initialize(this);
                }

                balloon.SetActive(false);
            }
        }
    }

    // Activar un globo aleatorio de la lista de disponibles
    private void ActivateRandomBalloon()
    {
        if (!gameRunning || availableBalloons.Count == 0) return;

        // Seleccionar un globo aleatorio de la lista
        int randomIndex = Random.Range(0, availableBalloons.Count);
        currentActiveBalloon = availableBalloons[randomIndex];

        // Activar el globo seleccionado
        if (currentActiveBalloon != null)
        {
            currentActiveBalloon.SetActive(true);
        }
    }

    // Método llamado cuando un globo es explotado
    public void BalloonPopped(GameObject poppedBalloon)
    {
        if (!gameRunning) return;

        // Reproducir sonido
        if (popSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popSound);
        }

        // Incrementar puntuación
        score++;
        UpdateScoreDisplay();

        // Desactivar el globo explotado
        poppedBalloon.SetActive(false);

        // Remover el globo de la lista de disponibles
        availableBalloons.Remove(poppedBalloon);

        // Si no quedan globos disponibles, reactivamos todos
        if (availableBalloons.Count == 0)
        {
            availableBalloons.AddRange(balloons);
        }

        // Activar otro globo aleatorio
        ActivateRandomBalloon();
    }

    // Actualizar el texto de puntuación
    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }
}

// Clase para controlar cada globo individual
public class BalloonController : MonoBehaviour
{
    private BalloonGame gameManager;

    public void Initialize(BalloonGame manager)
    {
        gameManager = manager;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Verificar si la colisión fue con una pelota de ping pong
        if (collision.gameObject.CompareTag("PingPongBall"))
        {
            // Informar al gestor del juego
            if (gameManager != null)
            {
                gameManager.BalloonPopped(this.gameObject);
            }
        }
    }

    // Añadir detección con trigger por si los colliders están configurados como triggers
    private void OnTriggerEnter(Collider other)
    {
        // Verificar si la colisión fue con una pelota de ping pong
        if (other.CompareTag("PingPongBall"))
        {
            // Informar al gestor del juego
            if (gameManager != null)
            {
                gameManager.BalloonPopped(this.gameObject);
            }
        }
    }
}