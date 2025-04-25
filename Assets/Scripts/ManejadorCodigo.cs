using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ManejadorCodigo : MonoBehaviour
{
    // Singleton para acceder desde cualquier parte
    private static ManejadorCodigo _instance;
    public static ManejadorCodigo Instance { get { return _instance; } }

    // Variable estática para guardar el código entre escenas
    private static string codigoEstudiante = "";

    // Para acceder al código desde cualquier escena sin necesidad del componente
    public static string CodigoEstudiante { get { return codigoEstudiante; } }

    [Header("Referencias")]
    [Tooltip("Campo de texto donde el estudiante introduce su código")]
    public TMP_InputField campoCodigoInput;

    [Tooltip("Alternativa: Texto donde se muestra el código (si no es InputField)")]
    public TextMeshProUGUI textoCodigoOutput;

    [Header("Configuración")]
    [Tooltip("¿Buscar automáticamente TelemetriaManager al cambiar a otra escena?")]
    public bool buscarTelemetriaAlCambiarEscena = true;

    [Tooltip("Nombre de la escena donde está TelemetriaManager")]
    public string escenaTelemetria = "MainScene";

    [Tooltip("¿Destruir este objeto al cambiar a la escena de telemetría?")]
    public bool destruirAlCambiar = false;

    private bool codigoGuardado = false;

    private void Awake()
    {
        // Implementación del patrón Singleton
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
        // Verificar si ya tenemos un código guardado (de sesión previa)
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
            // Si estamos en la escena de telemetría, enviar el código
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

        // Buscar telemetría manager y enviar código si no lo hemos hecho ya
        if (!codigoGuardado && !string.IsNullOrEmpty(codigoEstudiante))
        {
            EnviarCodigoATelemetria();
        }
    }

    /// <summary>
    /// Guarda el código del estudiante desde el campo de texto y lo mantiene entre escenas
    /// </summary>
    public void GuardarCodigo()
    {
        // Obtener código desde el input field si está disponible
        if (campoCodigoInput != null)
        {
            codigoEstudiante = campoCodigoInput.text.Trim();
        }
        // Si no, verificar si hay un texto asignado
        else if (textoCodigoOutput != null && !string.IsNullOrEmpty(textoCodigoOutput.text))
        {
            codigoEstudiante = textoCodigoOutput.text.Trim();
        }

        // Enviar a telemetría si está disponible en la escena actual
        EnviarCodigoATelemetria();

        // Actualizar visualización
        ActualizarVisualizacion();

        Debug.Log($"Código de estudiante guardado: {codigoEstudiante}");
    }

    /// <summary>
    /// Establece el código manualmente desde otro script
    /// </summary>
    public void EstablecerCodigo(string codigo)
    {
        codigoEstudiante = codigo.Trim();

        // Actualizar visualización
        ActualizarVisualizacion();

        // Intentar enviar a telemetría
        EnviarCodigoATelemetria();

        Debug.Log($"Código de estudiante establecido externamente: {codigoEstudiante}");
    }

    private void ActualizarVisualizacion()
    {
        // Actualizar el input field si está disponible
        if (campoCodigoInput != null)
        {
            campoCodigoInput.text = codigoEstudiante;
        }

        // Actualizar el texto si está disponible
        if (textoCodigoOutput != null)
        {
            textoCodigoOutput.text = codigoEstudiante;
        }
    }

    /// <summary>
    /// Envía el código al TelemetriaManager si está disponible
    /// </summary>
    public void EnviarCodigoATelemetria()
    {
        // Si no hay código, no hacer nada
        if (string.IsNullOrEmpty(codigoEstudiante))
        {
            Debug.LogWarning("No hay código de estudiante para enviar a telemetría");
            return;
        }

        // Buscar TelemetriaManager
        TelemetriaManager telemetria = FindObjectOfType<TelemetriaManager>();

        if (telemetria != null)
        {

            // Llamar directamente al método con el código como parámetro
            telemetria.CodigoGuardado(codigoEstudiante);
            codigoGuardado = true;
            Debug.Log("Código enviado correctamente a TelemetriaManager");

                
            
        }
        else
        {
            // Intentar buscar TelemetriaManagerAnger si está disponible
            TelemetriaManagerAnger telemetriaAnger = FindObjectOfType<TelemetriaManagerAnger>();

            if (telemetriaAnger != null)
            {
                // Registrar el código como evento
                telemetriaAnger.RegistrarEvento("CODIGO_ESTUDIANTE", codigoEstudiante);
                codigoGuardado = true;
                Debug.Log("Código enviado correctamente a TelemetriaManagerAnger");
            }
            else
            {
                Debug.LogWarning("No se encontró ningún sistema de telemetría en la escena actual");
            }
        }
    }

    /// <summary>
    /// Método para cambiar de escena y llevar el código
    /// </summary>
    public void CambiarAEscenaConCodigo(string nombreEscena)
    {
        // Guardar el código primero
        GuardarCodigo();

        // Cambiar a la escena deseada
        SceneManager.LoadScene(nombreEscena);
    }

    /// <summary>
    /// Método para crear un TextMeshProUGUI con el código en otra escena
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