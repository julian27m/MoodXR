using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ReactionButtonGame : MonoBehaviour
{
    [Header("Materiales")]
    [SerializeField] private Material initialMaterial; // Material cuando está inactivo
    [SerializeField] private Material changedMaterial; // Material cuando está activo

    [Header("Configuración del Juego")]
    [SerializeField] private float gameDuration = 60f; // Duración del juego (1 minuto)
    [SerializeField] private float minTimeToChange = 1f; // Tiempo mínimo antes de cambiar a activo
    [SerializeField] private float maxTimeToChange = 4f; // Tiempo máximo antes de cambiar a activo
    [SerializeField] private bool addPointOnDeactivation = true; // ¿Añadir punto cuando el botón se desactiva?

    [Header("Referencias")]
    [SerializeField] private Renderer buttonRenderer; // Renderer para cambiar el material

    // Estado público que define si el botón está activo o inactivo
    public bool isInactive = true;
    private bool wasActive = false; // Para rastrear el cambio de estado

    // Variables de control del juego
    private bool gameRunning = false;
    private float gameTimer = 0f;
    private Coroutine gameCoroutine;
    private Coroutine changeStateCoroutine;

    private void Awake()
    {
        // Intentar encontrar el renderer en el Awake
        FindRenderer();
    }

    private void Start()
    {
        // Verificar referencias y aplicar el material inicial
        CheckReferences();

        // Inicializar el botón como inactivo
        isInactive = true;
        wasActive = false;
        UpdateButtonVisual();

        Debug.Log("ReactionButtonGame inicializado en: " + gameObject.name);
    }

    // Intenta encontrar un renderer válido
    private void FindRenderer()
    {
        if (buttonRenderer == null)
        {
            // Intentar buscar en este objeto
            buttonRenderer = GetComponent<Renderer>();

            // Si no lo encuentra, buscar en hijos directos
            if (buttonRenderer == null)
            {
                buttonRenderer = GetComponentInChildren<Renderer>();

                // Buscar en toda la jerarquía si es necesario
                if (buttonRenderer == null)
                {
                    Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length > 0)
                    {
                        buttonRenderer = renderers[0];
                        Debug.Log("Renderer encontrado en la jerarquía: " + buttonRenderer.gameObject.name);
                    }
                }
            }

            if (buttonRenderer != null)
            {
                Debug.Log("Renderer encontrado: " + buttonRenderer.gameObject.name);
            }
        }
    }

    // Verifica que todas las referencias necesarias estén asignadas
    private void CheckReferences()
    {
        if (buttonRenderer == null)
        {
            Debug.LogError("No se encontró un Renderer en " + gameObject.name + " o sus hijos. Por favor, asigna uno en el Inspector.");
            return;
        }

        if (initialMaterial == null || changedMaterial == null)
        {
            Debug.LogError("Por favor, asigna los materiales inicial y cambiado en " + gameObject.name);
            return;
        }

        // Guardar una referencia al material original si no se especificó uno inicial
        if (initialMaterial == null && buttonRenderer.material != null)
        {
            initialMaterial = buttonRenderer.material;
            Debug.Log("Usando el material actual como material inicial");
        }
    }

    /// <summary>
    /// Inicia el juego de reacción
    /// </summary>
    public void StartGame()
    {
        if (gameRunning)
            return;

        Debug.Log("Intentando iniciar juego en: " + gameObject.name);

        // Verificar que tenemos todo lo necesario
        if (buttonRenderer == null)
        {
            Debug.LogError("No hay un Renderer asignado en " + gameObject.name);
            FindRenderer(); // Intentar encontrar uno

            if (buttonRenderer == null)
                return;
        }

        if (initialMaterial == null || changedMaterial == null)
        {
            Debug.LogError("Faltan materiales en " + gameObject.name);
            return;
        }

        // Reiniciar la puntuación en el GameManager al iniciar el juego
        // Sólo el primer botón que se active debería reiniciar el puntaje
        if (ReactionGameManager.Instance != null && transform.GetSiblingIndex() == 0)
        {
            ReactionGameManager.Instance.ResetScore();
        }

        gameRunning = true;
        gameTimer = gameDuration;
        isInactive = true;
        wasActive = false;
        UpdateButtonVisual();

        // Detener corrutinas anteriores si existen
        StopAllCoroutines();

        // Iniciar las corrutinas del juego
        gameCoroutine = StartCoroutine(GameTimerRoutine());
        changeStateCoroutine = StartCoroutine(RandomStateChangeRoutine());

        Debug.Log("¡Juego iniciado en " + gameObject.name + "! Duración: " + gameDuration + " segundos");
    }

    /// <summary>
    /// Detiene el juego
    /// </summary>
    public void StopGame()
    {
        if (!gameRunning)
            return;

        gameRunning = false;

        // Detener corrutinas
        StopAllCoroutines();

        // Restablecer el botón a inactivo
        isInactive = true;
        UpdateButtonVisual();

        Debug.Log("Juego detenido en: " + gameObject.name);
    }

    /// <summary>
    /// Activa el botón (cambia a estado activo)
    /// </summary>
    public void ActivateButton()
    {
        // Guardar el estado anterior
        bool previousState = isInactive;

        // Cambiar el estado
        isInactive = false;
        wasActive = true;

        // Actualizar visual
        UpdateButtonVisual();

        Debug.Log("Botón activado en: " + gameObject.name);
    }

    /// <summary>
    /// Desactiva el botón (cambia a estado inactivo)
    /// </summary>
    public void DeactivateButton()
    {
        // Solo sumar puntos si el botón estaba activo antes
        if (!isInactive && wasActive && addPointOnDeactivation && gameRunning)
        {
            // Añadir un punto en el GameManager
            if (ReactionGameManager.Instance != null)
            {
                ReactionGameManager.Instance.AddPoint();
                Debug.Log("Punto añadido por desactivar el botón: " + gameObject.name);
            }
        }

        // Cambiar el estado
        isInactive = true;
        wasActive = false;

        // Actualizar visual
        UpdateButtonVisual();

        Debug.Log("Botón desactivado en: " + gameObject.name);
    }

    /// <summary>
    /// Cambia el estado actual del botón
    /// </summary>
    public void ToggleButtonState()
    {
        // Si va a cambiar de activo a inactivo y estaba activo, sumar punto
        if (!isInactive && wasActive && addPointOnDeactivation && gameRunning)
        {
            // Añadir un punto en el GameManager
            if (ReactionGameManager.Instance != null)
            {
                ReactionGameManager.Instance.AddPoint();
                Debug.Log("Punto añadido por desactivar el botón: " + gameObject.name);
            }
        }

        // Cambiar el estado
        isInactive = !isInactive;

        // Actualizar el estado de seguimiento
        wasActive = !isInactive;

        // Actualizar visual
        UpdateButtonVisual();

        Debug.Log("Estado del botón cambiado a: " + (isInactive ? "inactivo" : "activo") + " en: " + gameObject.name);
    }

    /// <summary>
    /// Actualiza el material del botón según su estado
    /// </summary>
    private void UpdateButtonVisual()
    {
        if (buttonRenderer == null)
        {
            Debug.LogError("No hay Renderer para actualizar en: " + gameObject.name);
            return;
        }

        if (initialMaterial == null || changedMaterial == null)
        {
            Debug.LogError("Faltan materiales para actualizar en: " + gameObject.name);
            return;
        }

        // Aplicar el material correspondiente
        Material materialToApply = isInactive ? initialMaterial : changedMaterial;

        // Asegurarse de que estamos aplicando un material diferente
        if (buttonRenderer.sharedMaterial != materialToApply)
        {
            buttonRenderer.material = materialToApply;
            Debug.Log("Material actualizado a: " + (isInactive ? "inicial" : "cambiado") + " en: " + gameObject.name);
        }
    }

    /// <summary>
    /// Corrutina que maneja el temporizador del juego
    /// </summary>
    private IEnumerator GameTimerRoutine()
    {
        Debug.Log("Temporizador iniciado en: " + gameObject.name);
        while (gameTimer > 0 && gameRunning)
        {
            yield return null; // Esperar un frame
            gameTimer -= Time.deltaTime;
        }

        // El tiempo se acabó
        Debug.Log("Temporizador finalizado en: " + gameObject.name);
        EndGame();
    }

    /// <summary>
    /// Corrutina que cambia aleatoriamente el estado del botón
    /// </summary>
    private IEnumerator RandomStateChangeRoutine()
    {
        Debug.Log("Rutina de cambio aleatorio iniciada en: " + gameObject.name);
        while (gameRunning)
        {
            // Esperar un tiempo aleatorio antes de activar el botón
            float waitTime = Random.Range(minTimeToChange, maxTimeToChange);
            Debug.Log("Esperando " + waitTime + " segundos para cambiar en: " + gameObject.name);
            yield return new WaitForSeconds(waitTime);

            // Solo cambiar a activo si actualmente está inactivo y el juego sigue corriendo
            if (isInactive && gameRunning)
            {
                Debug.Log("Cambiando a activo en: " + gameObject.name);
                ActivateButton();
            }
        }
    }

    /// <summary>
    /// Finaliza el juego
    /// </summary>
    private void EndGame()
    {
        gameRunning = false;

        // Detener las corrutinas
        StopAllCoroutines();

        // Restablecer el botón a inactivo
        isInactive = true;
        wasActive = false;
        UpdateButtonVisual();

        Debug.Log("¡Juego terminado en: " + gameObject.name + "!");
    }

    /// <summary>
    /// Limpia las corrutinas al destruir el objeto
    /// </summary>
    private void OnDestroy()
    {
        // Asegurarse de detener todas las corrutinas
        StopAllCoroutines();
    }

    // Para depuración: mostrar información en el Inspector
    private void OnValidate()
    {
        if (buttonRenderer == null)
        {
            FindRenderer();
        }
    }

    // Para depuración: forzar actualización visual
    private void OnEnable()
    {
        UpdateButtonVisual();
    }
}