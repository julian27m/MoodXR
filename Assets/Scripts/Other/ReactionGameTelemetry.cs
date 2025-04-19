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
            Debug.LogError("No se encontr� el ReactionGameManager en este GameObject");
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
            Debug.LogWarning("No se encontr� el TelemetriaManagerAnger en la escena");
        }

        // Inicializar el valor de la puntuaci�n
        lastScore = reactionManager.GetScore();
    }

    private void Update()
    {
        // Verificar si la puntuaci�n ha cambiado
        CheckForScoreChanges();
    }

    private void CheckForScoreChanges()
    {
        if (reactionManager == null) return;

        // Obtener la puntuaci�n actual
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
                    Debug.Log("Bot�n presionado registrado. Puntuaci�n: " + currentScore);
                }
            }

            lastScore = currentScore;
        }
    }

    // M�todo que puede ser llamado directamente desde ReactionGameManager.AddPoint()
    // Este m�todo necesitar�a ser agregado como llamada en el ReactionGameManager
    public void OnButtonPressedForTelemetry()
    {
        if (TelemetriaManagerAnger.Instance != null)
        {
            TelemetriaManagerAnger.Instance.RegistrarBotonPresionado();
            Debug.Log("Bot�n presionado registrado mediante llamada directa");
        }
    }
}

/// <summary>
/// Extension para los botones individuales de reacci�n
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
            Debug.LogError("No se encontr� el ReactionButtonGame en este GameObject");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        // Verificar cambios en el estado del bot�n
        if (buttonGame != null)
        {
            // Detectar cuando el bot�n cambia de activo a inactivo (fue presionado)
            if (wasInactive && !buttonGame.isInactive)
            {
                // El bot�n se activ� (cambi� a rojo)
                wasInactive = false;
            }
            else if (!wasInactive && buttonGame.isInactive)
            {
                // El bot�n se desactiv� (el usuario lo presion�)
                if (TelemetriaManagerAnger.Instance != null)
                {
                    // No llamamos RegistrarBotonPresionado aqu� porque eso lo hace el ReactionGameManager
                    // Solo registramos el evento espec�fico de este bot�n
                    TelemetriaManagerAnger.Instance.RegistrarEvento("BOTON_INDIVIDUAL_PRESIONADO",
                        $"ID: {gameObject.GetInstanceID()}, Nombre: {gameObject.name}");
                }

                wasInactive = true;
            }
        }
    }
}