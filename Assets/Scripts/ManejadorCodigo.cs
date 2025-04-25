using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ManejadorCodigo : MonoBehaviour
{
    // Singleton para acceder desde cualquier parte
    private static ManejadorCodigo _instance;
    public static ManejadorCodigo Instance { get { return _instance; } }

    // Variable est�tica para guardar el c�digo entre escenas
    private static string codigoEstudiante = "";

    // Para acceder al c�digo desde cualquier escena sin necesidad del componente
    public static string CodigoEstudiante { get { return codigoEstudiante; } }

    [Header("Referencias")]
    [Tooltip("Campo de texto donde el estudiante introduce su c�digo")]
    public TMP_InputField campoCodigoInput;

    [Tooltip("Alternativa: Texto donde se muestra el c�digo (si no es InputField)")]
    public TextMeshProUGUI textoCodigoOutput;

    [Header("Configuraci�n")]
    [Tooltip("�Buscar autom�ticamente TelemetriaManager al cambiar a otra escena?")]
    public bool buscarTelemetriaAlCambiarEscena = true;

    [Tooltip("Nombre de la escena donde est� TelemetriaManager")]
    public string escenaTelemetria = "MainScene";

    [Tooltip("�Destruir este objeto al cambiar a la escena de telemetr�a?")]
    public bool destruirAlCambiar = false;

    private bool codigoGuardado = false;

    private void Awake()
    {
        // Implementaci�n del patr�n Singleton
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;

        // No destruir al cambiar de escena
        DontDestroyOnLoad(this.gameObject);

        // Suscribirse al evento de cambio de escena
        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log("ManejadorCodigo inicializado");
    }

    private void Start()
    {
        // Verificar si ya tenemos un c�digo guardado (de sesi�n previa)
        if (!string.IsNullOrEmpty(codigoEstudiante))
        {
            ActualizarVisualizacion();
        }
    }

    private void OnDestroy()
    {
        // Asegurarnos de desuscribirnos del evento
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (buscarTelemetriaAlCambiarEscena)
        {
            // Si estamos en la escena de telemetr�a, enviar el c�digo
            if (scene.name == escenaTelemetria || scene.name == "Close")
            {
                StartCoroutine(BuscarTelemetriaManagerConRetraso(1.0f));
            }
        }
    }

    private IEnumerator BuscarTelemetriaManagerConRetraso(float segundos)
    {
        // Esperar a que la escena termine de cargar completamente
        yield return new WaitForSeconds(segundos);

        // Buscar telemetr�a manager y enviar c�digo si no lo hemos hecho ya
        if (!codigoGuardado && !string.IsNullOrEmpty(codigoEstudiante))
        {
            EnviarCodigoATelemetria();
        }
    }

    /// <summary>
    /// Guarda el c�digo del estudiante desde el campo de texto y lo mantiene entre escenas
    /// </summary>
    public void GuardarCodigo()
    {
        // Obtener c�digo desde el input field si est� disponible
        if (campoCodigoInput != null)
        {
            codigoEstudiante = campoCodigoInput.text.Trim();
        }
        // Si no, verificar si hay un texto asignado
        else if (textoCodigoOutput != null && !string.IsNullOrEmpty(textoCodigoOutput.text))
        {
            codigoEstudiante = textoCodigoOutput.text.Trim();
        }

        // Enviar a telemetr�a si est� disponible en la escena actual
        EnviarCodigoATelemetria();

        // Actualizar visualizaci�n
        ActualizarVisualizacion();

        Debug.Log($"C�digo de estudiante guardado: {codigoEstudiante}");
    }

    /// <summary>
    /// Establece el c�digo manualmente desde otro script
    /// </summary>
    public void EstablecerCodigo(string codigo)
    {
        codigoEstudiante = codigo.Trim();

        // Actualizar visualizaci�n
        ActualizarVisualizacion();

        // Intentar enviar a telemetr�a
        EnviarCodigoATelemetria();

        Debug.Log($"C�digo de estudiante establecido externamente: {codigoEstudiante}");
    }

    private void ActualizarVisualizacion()
    {
        // Actualizar el input field si est� disponible
        if (campoCodigoInput != null)
        {
            campoCodigoInput.text = codigoEstudiante;
        }

        // Actualizar el texto si est� disponible
        if (textoCodigoOutput != null)
        {
            textoCodigoOutput.text = codigoEstudiante;
        }
    }

    /// <summary>
    /// Env�a el c�digo al TelemetriaManager si est� disponible
    /// </summary>
    public void EnviarCodigoATelemetria()
    {
        // Si no hay c�digo, no hacer nada
        if (string.IsNullOrEmpty(codigoEstudiante))
        {
            Debug.LogWarning("No hay c�digo de estudiante para enviar a telemetr�a");
            return;
        }

        // Buscar TelemetriaManager
        TelemetriaManager telemetria = FindObjectOfType<TelemetriaManager>();

        if (telemetria != null)
        {

            // Llamar directamente al m�todo con el c�digo como par�metro
            telemetria.CodigoGuardado(codigoEstudiante);
            codigoGuardado = true;
            Debug.Log("C�digo enviado correctamente a TelemetriaManager");

                
            
        }
        else
        {
            // Intentar buscar TelemetriaManagerAnger si est� disponible
            TelemetriaManagerAnger telemetriaAnger = FindObjectOfType<TelemetriaManagerAnger>();

            if (telemetriaAnger != null)
            {
                // Registrar el c�digo como evento
                telemetriaAnger.RegistrarEvento("CODIGO_ESTUDIANTE", codigoEstudiante);
                codigoGuardado = true;
                Debug.Log("C�digo enviado correctamente a TelemetriaManagerAnger");
            }
            else
            {
                Debug.LogWarning("No se encontr� ning�n sistema de telemetr�a en la escena actual");
            }
        }
    }

    /// <summary>
    /// M�todo para cambiar de escena y llevar el c�digo
    /// </summary>
    public void CambiarAEscenaConCodigo(string nombreEscena)
    {
        // Guardar el c�digo primero
        GuardarCodigo();

        // Cambiar a la escena deseada
        SceneManager.LoadScene(nombreEscena);
    }

    /// <summary>
    /// M�todo para crear un TextMeshProUGUI con el c�digo en otra escena
    /// </summary>
    public TextMeshProUGUI CrearTextoConCodigo(Transform padre)
    {
        GameObject nuevoObj = new GameObject("TextoCodigo");
        nuevoObj.transform.SetParent(padre, false);

        // Crear un componente RectTransform
        RectTransform rect = nuevoObj.AddComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(200, 50);

        // Crear y configurar el TextMeshProUGUI
        TextMeshProUGUI texto = nuevoObj.AddComponent<TextMeshProUGUI>();
        texto.text = codigoEstudiante;
        texto.fontSize = 24;
        texto.alignment = TextAlignmentOptions.Center;

        return texto;
    }
}