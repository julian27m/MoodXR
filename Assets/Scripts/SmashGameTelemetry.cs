using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script que conecta el SmashGameController con el TelemetriaManagerAnger
/// </summary>
[RequireComponent(typeof(SmashGameController))]
public class SmashGameTelemetry : MonoBehaviour
{
    private SmashGameController smashController;

    // Lista de objetos golpeados para evitar registros duplicados
    private HashSet<GameObject> registeredHits = new HashSet<GameObject>();

    private void Awake()
    {
        smashController = GetComponent<SmashGameController>();

        if (smashController == null)
        {
            Debug.LogError("No se encontr� el SmashGameController en este GameObject");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        // Registrar que estamos en la escena
        if (TelemetriaManagerAnger.Instance != null)
        {
            TelemetriaManagerAnger.Instance.RegistrarEvento("INICIO_MINIJUEGO_SMASH", "El minijuego de Smash ha sido iniciado");
        }
        else
        {
            Debug.LogWarning("No se encontr� el TelemetriaManagerAnger en la escena");
        }
    }

    private void OnEnable()
    {
        // Suscribirse a eventos relevantes si el smashController los expone mediante eventos
        // Si no hay eventos disponibles, usamos el m�todo MonoBehaviour.Update() para interceptar
    }

    private void OnDisable()
    {
        // Desuscribirse de eventos si es necesario
    }

    private void Update()
    {
        // Si el smashController no expone eventos, podemos verificar su estado actual
        // y compararlo con el estado anterior para detectar cambios

        // Ejemplo: Verificar puntuaci�n actual vs puntuaci�n anterior
        CheckForScoreChanges();
    }

    private int lastScore = 0;

    private void CheckForScoreChanges()
    {
        if (smashController == null) return;

        // Recuperar la puntuaci�n actual
        // Nota: Como no hay un m�todo p�blico para obtener la puntuaci�n directamente,
        // accedemos al valor de texto. Esto es una soluci�n temporal que se podr�a mejorar
        // modificando SmashGameController para exponer la puntuaci�n o eventos.

        int currentScore = 0;
        if (smashController.scoreText != null)
        {
            if (int.TryParse(smashController.scoreText.text, out int score))
            {
                currentScore = score;
            }
        }

        // Si la puntuaci�n ha aumentado, significa que se ha golpeado un objeto
        if (currentScore > lastScore)
        {
            // Registrar el golpe (utilizamos "Objeto desconocido" porque no tenemos acceso al objeto espec�fico)
            if (TelemetriaManagerAnger.Instance != null)
            {
                TelemetriaManagerAnger.Instance.RegistrarObjetoGolpeado("Objeto desconocido");
                Debug.Log($"Objeto golpeado registrado. Puntuaci�n: {currentScore}");
            }

            lastScore = currentScore;
        }
    }

    // M�todo p�blico que puede ser llamado por SmashGameController cuando un objeto es golpeado
    // Este m�todo debe ser agregado al script SmashGameController o llamado desde all�
    public void OnObjectHitForTelemetry(GameObject hitObject)
    {
        if (TelemetriaManagerAnger.Instance != null && hitObject != null)
        {
            // Registrar el golpe con el nombre del objeto
            string objectName = hitObject.name;
            string objectTag = hitObject.tag;

            TelemetriaManagerAnger.Instance.RegistrarObjetoGolpeado($"{objectTag}_{objectName}");
            Debug.Log($"Objeto golpeado registrado: {objectTag}_{objectName}");
        }
    }
}