using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script que conecta el BalloonGame con el TelemetriaManagerAnger
/// </summary>
[RequireComponent(typeof(BalloonGame))]
public class BalloonGameTelemetry : MonoBehaviour
{
    private BalloonGame balloonGame;
    private int lastScore = 0;

    private void Awake()
    {
        balloonGame = GetComponent<BalloonGame>();

        if (balloonGame == null)
        {
            Debug.LogError("No se encontró el BalloonGame en este GameObject");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        // Registrar que estamos en la escena
        if (TelemetriaManagerAnger.Instance != null)
        {
            TelemetriaManagerAnger.Instance.RegistrarEvento("INICIO_MINIJUEGO_BALLOON", "El minijuego de Balloon ha sido iniciado");
        }
        else
        {
            Debug.LogWarning("No se encontró el TelemetriaManagerAnger en la escena");
        }
    }

    private void Update()
    {
        // Verificar si la puntuación ha cambiado
        CheckForScoreChanges();
    }

    private void CheckForScoreChanges()
    {
        if (balloonGame == null || balloonGame.scoreText == null) return;

        // Obtener la puntuación actual desde el texto
        if (int.TryParse(balloonGame.scoreText.text, out int currentScore))
        {
            // Si ha aumentado, registrar el evento
            if (currentScore > lastScore)
            {
                int balloonsPopped = currentScore - lastScore;

                for (int i = 0; i < balloonsPopped; i++)
                {
                    if (TelemetriaManagerAnger.Instance != null)
                    {
                        TelemetriaManagerAnger.Instance.RegistrarGloboGolpeado();
                        Debug.Log("Globo golpeado registrado. Puntuación: " + currentScore);
                    }
                }

                lastScore = currentScore;
            }
        }
    }

    // Método que puede ser llamado desde BalloonGame.BalloonPopped()
    // Este método necesitaría ser agregado como llamada en el BalloonGame
    public void OnBalloonPoppedForTelemetry(GameObject balloon)
    {
        if (TelemetriaManagerAnger.Instance != null)
        {
            TelemetriaManagerAnger.Instance.RegistrarGloboGolpeado();
            Debug.Log($"Globo golpeado registrado: {balloon.name}");
        }
    }
}

/// <summary>
/// Extensión para los globos individuales
/// Si se necesita un seguimiento más detallado de cada globo específico
/// </summary>
public class BalloonControllerTelemetry : MonoBehaviour
{
    private BalloonController balloonController;
    private bool wasActive = false;

    private void Awake()
    {
        balloonController = GetComponent<BalloonController>();

        if (balloonController == null)
        {
            Debug.LogError("No se encontró el BalloonController en este GameObject");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        // Cuando el globo se activa
        if (TelemetriaManagerAnger.Instance != null)
        {
            TelemetriaManagerAnger.Instance.RegistrarEvento("GLOBO_ACTIVADO",
                $"ID: {gameObject.GetInstanceID()}, Nombre: {gameObject.name}");
        }

        wasActive = true;
    }

    private void OnDisable()
    {
        // Solo registrar cuando un globo activo se desactiva (no al inicio)
        if (wasActive)
        {
            if (TelemetriaManagerAnger.Instance != null)
            {
                // No usamos RegistrarGloboGolpeado porque eso ya lo hace el BalloonGame principal
                // Este es solo un registro adicional con información específica de este globo
                TelemetriaManagerAnger.Instance.RegistrarEvento("GLOBO_DESACTIVADO",
                    $"ID: {gameObject.GetInstanceID()}, Nombre: {gameObject.name}");
            }

            wasActive = false;
        }
    }
}