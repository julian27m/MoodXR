using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script que conecta el ReactionGameManager con el TelemetriaManagerAnger
/// </summary>
[RequireComponent(typeof(ReactionGameManager))]
public class ReactionGameTelemetry : MonoBehaviour
{
    private ReactionGameManager reactionManager;
    private int lastScore = 0;

    private void Awake()
    {
        reactionManager = GetComponent<ReactionGameManager>();

        if (reactionManager == null)
        {
            Debug.LogError("No se encontró el ReactionGameManager en este GameObject");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        // Registrar que estamos en la escena
        if (TelemetriaManagerAnger.Instance != null)
        {
            TelemetriaManagerAnger.Instance.RegistrarEvento("INICIO_MINIJUEGO_REACTION", "El minijuego de Reaction ha sido iniciado");
        }
        else
        {
            Debug.LogWarning("No se encontró el TelemetriaManagerAnger en la escena");
        }

        // Inicializar el valor de la puntuación
        lastScore = reactionManager.GetScore();
    }

    private void Update()
    {
        // Verificar si la puntuación ha cambiado
        CheckForScoreChanges();
    }

    private void CheckForScoreChanges()
    {
        if (reactionManager == null) return;

        // Obtener la puntuación actual
        int currentScore = reactionManager.GetScore();

        // Si ha aumentado, registrar el evento
        if (currentScore > lastScore)
        {
            int buttonsPressedThisFrame = currentScore - lastScore;

            for (int i = 0; i < buttonsPressedThisFrame; i++)
            {
                if (TelemetriaManagerAnger.Instance != null)
                {
                    TelemetriaManagerAnger.Instance.RegistrarBotonPresionado();
                    Debug.Log("Botón presionado registrado. Puntuación: " + currentScore);
                }
            }

            lastScore = currentScore;
        }
    }

    // Método que puede ser llamado directamente desde ReactionGameManager.AddPoint()
    // Este método necesitaría ser agregado como llamada en el ReactionGameManager
    public void OnButtonPressedForTelemetry()
    {
        if (TelemetriaManagerAnger.Instance != null)
        {
            TelemetriaManagerAnger.Instance.RegistrarBotonPresionado();
            Debug.Log("Botón presionado registrado mediante llamada directa");
        }
    }
}

/// <summary>
/// Extension para los botones individuales de reacción
/// </summary>
[RequireComponent(typeof(ReactionButtonGame))]
public class ReactionButtonTelemetry : MonoBehaviour
{
    private ReactionButtonGame buttonGame;
    private bool wasInactive = true;

    private void Awake()
    {
        buttonGame = GetComponent<ReactionButtonGame>();

        if (buttonGame == null)
        {
            Debug.LogError("No se encontró el ReactionButtonGame en este GameObject");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        // Verificar cambios en el estado del botón
        if (buttonGame != null)
        {
            // Detectar cuando el botón cambia de activo a inactivo (fue presionado)
            if (wasInactive && !buttonGame.isInactive)
            {
                // El botón se activó (cambió a rojo)
                wasInactive = false;
            }
            else if (!wasInactive && buttonGame.isInactive)
            {
                // El botón se desactivó (el usuario lo presionó)
                if (TelemetriaManagerAnger.Instance != null)
                {
                    // No llamamos RegistrarBotonPresionado aquí porque eso lo hace el ReactionGameManager
                    // Solo registramos el evento específico de este botón
                    TelemetriaManagerAnger.Instance.RegistrarEvento("BOTON_INDIVIDUAL_PRESIONADO",
                        $"ID: {gameObject.GetInstanceID()}, Nombre: {gameObject.name}");
                }

                wasInactive = true;
            }
        }
    }
}