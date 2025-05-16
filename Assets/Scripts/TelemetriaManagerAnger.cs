using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Globalization;

public class TelemetriaManagerAnger : MonoBehaviour
{
    // Singleton
    private static TelemetriaManagerAnger _instance;
    public static TelemetriaManagerAnger Instance { get { return _instance; } }

    [Header("Configuración de Seguimiento de Mirada")]
    [Tooltip("La posición entre las cámaras izquierda y derecha (cabeza del usuario)")]
    public Transform playerHead;

    [Tooltip("Umbral de alineación para considerar que el usuario está mirando al collider")]
    [Range(0.7f, 1.0f)]
    public float alignmentThreshold = 0.9f;

    [Tooltip("Tiempo en segundos que el usuario debe mirar antes de considerar foco de atención")]
    public float focusDelay = 0.5f;

    [Tooltip("Intervalo en segundos para registrar la mirada del usuario")]
    public float gazeLogInterval = 3.0f;

    [Header("Colliders de Dirección")]
    [Tooltip("Collider Norte (Mesa principal)")]
    public Transform norteCollider;

    [Tooltip("Collider Sur")]
    public Transform surCollider;

    [Tooltip("Collider Este")]
    public Transform esteCollider;

    [Tooltip("Collider Oeste")]
    public Transform oesteCollider;

    [Header("Configuración de Escenas")]
    [Tooltip("Nombre de la escena del primer minijuego (Smash)")]
    public string escenaSmashGame = "Anger";

    [Tooltip("Nombre de la escena del segundo minijuego (Reaction)")]
    public string escenaReactionGame = "AngerTwo";

    [Tooltip("Nombre de la escena del tercer minijuego (Balloon)")]
    public string escenaBalloonGame = "AngerThree";

    [Header("Duraciones Fijas")]
    [Tooltip("Duración fija del minijuego Smash en segundos")]
    public float smashGameDuration = 120f;

    [Tooltip("Duración fija del minijuego Reaction en segundos")]
    public float reactionGameDuration = 120f;

    [Tooltip("Duración fija del minijuego Balloon en segundos")]
    public float balloonGameDuration = 120f;

    [Header("Identificación de Experiencia")]
    [Tooltip("ID del equipo (se genera automáticamente si está vacío)")]
    [SerializeField] private string equipoID = "";
    [Tooltip("Archivo para almacenar el contador de experiencias")]
    [SerializeField] private string contadorFilePath = "experiencia_contador_anger.txt";

    // Variables para identificación
    private string experienciaID = "";
    private string codigoUsuario = "00"; // Valor por defecto
    private string experienciaCompleta = "";
    private bool resumenGuardadoConCodigo = false;

    // Información del dispositivo
    private string deviceName = "Desconocido";

    // Variables privadas para el manejo de logs
    private string logFilePath;
    private StringBuilder logBuffer = new StringBuilder();
    private float startTime;
    private bool isLogFileCreated = false;
    private bool isLogReady = false;
    private float lastSaveTime = 0f;
    private float saveInterval = 5f; // Guardar cada 5 segundos

    // Variables para seguimiento de mirada
    private string currentColliderName = "Ninguno";
    private float lastGazeLogTime = 0f;
    private float currentColliderStartTime = 0f;

    // Diccionarios para almacenar tiempos totales de mirada por escena
    private Dictionary<string, Dictionary<string, float>> colliderTotalLookTimesByScene = new Dictionary<string, Dictionary<string, float>>();

    // Variables para la escena actual
    private string currentSceneName;
    private int currentSceneIndex = 0;
    private float currentSceneStartTime = 0f;

    // Datos específicos del minijuego Smash
    private int smashObjectsHit = 0;
    private List<float> smashHitTimes = new List<float>();
    private List<string> smashHitObjects = new List<string>();

    // Datos específicos del minijuego Reaction
    private int reactionButtonsPressed = 0;
    private List<float> reactionPressTimes = new List<float>();

    // Datos específicos del minijuego Balloon
    private int balloonsPopped = 0;
    private List<float> balloonPopTimes = new List<float>();

    // Control de transiciones de escena
    private List<string> sceneHistory = new List<string>();
    private List<float> sceneTransitionTimes = new List<float>();

    private float ultimoRegistroButton = -5f; // Tiempo del último registro de botón
    private float ultimoRegistroGlobo = -5f; // Tiempo del último registro de globo

    // Referencia al EncryptionManager
    private EncryptionManager encryptionManager;

    private void Awake()
    {
        // Implementación del Singleton
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(this.gameObject);

        // Generar o leer el ID único del equipo
        GenerarEquipoID();

        // Obtener el ID de la experiencia
        GenerarExperienciaID();

        // Generar el ID completo de la experiencia (sin código de usuario todavía)
        experienciaCompleta = $"{equipoID}{experienciaID}00";

        // Obtener el nombre del dispositivo
        ObtenerDeviceName();

        // Inicializar diccionarios para cada escena
        InitializeSceneDictionaries();

        // Crear el archivo de log
        CreateLogFile();

        // Registrar la escena inicial
        currentSceneName = SceneManager.GetActiveScene().name;
        sceneHistory.Add(currentSceneName);
        sceneTransitionTimes.Add(0f);
        currentSceneStartTime = 0f;

        // Buscar o crear el EncryptionManager
        encryptionManager = FindObjectOfType<EncryptionManager>();
        if (encryptionManager == null)
        {
            GameObject encryptionObj = new GameObject("EncryptionManager");
            encryptionManager = encryptionObj.AddComponent<EncryptionManager>();
            DontDestroyOnLoad(encryptionObj);
            Debug.Log("EncryptionManager creado automáticamente para TelemetriaManagerAnger");
        }

        Debug.Log("TelemetriaManagerAnger inicializado en escena: " + currentSceneName);
        Debug.Log($"ID de Experiencia: {experienciaCompleta}");

        // Suscribirse al evento de cambio de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// Genera un ID único para el equipo o lo carga si ya existe
    /// </summary>
    private void GenerarEquipoID()
    {
        try
        {
            // Si ya tenemos un ID de equipo definido, usarlo
            if (!string.IsNullOrEmpty(equipoID))
            {
                Debug.Log($"Usando ID de equipo definido: {equipoID}");
                return;
            }

            // Ruta para guardar el ID del equipo
            string idFilePath = Path.Combine(Application.persistentDataPath, "equipo_id_anger.txt");

            // Verificar si ya existe un ID guardado
            if (File.Exists(idFilePath))
            {
                // Leer el ID existente
                equipoID = File.ReadAllText(idFilePath).Trim();
                Debug.Log($"ID de equipo cargado: {equipoID}");
            }
            else
            {
                // Generar un nuevo ID basado en el dispositivo y un número aleatorio
                string deviceID = SystemInfo.deviceUniqueIdentifier;
                // Tomar solo los primeros 8 caracteres para mantenerlo corto
                string shortDeviceID = deviceID.Length > 8 ? deviceID.Substring(0, 8) : deviceID;

                // Generar un ID de 2 dígitos para el equipo
                System.Random random = new System.Random();
                equipoID = random.Next(1, 100).ToString("00");

                // Guardar el ID para uso futuro
                File.WriteAllText(idFilePath, equipoID);
                Debug.Log($"Nuevo ID de equipo generado y guardado: {equipoID}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error generando ID de equipo: {e.Message}");
            equipoID = "00"; // Valor por defecto en caso de error
        }
    }

    /// <summary>
    /// Genera un ID consecutivo para cada experiencia en este equipo
    /// </summary>
    private void GenerarExperienciaID()
    {
        try
        {
            // Ruta para guardar el contador de experiencias
            string counterPath = Path.Combine(Application.persistentDataPath, contadorFilePath);
            int contador = 1;

            // Verificar si ya existe un contador guardado
            if (File.Exists(counterPath))
            {
                // Leer el contador existente
                string counterStr = File.ReadAllText(counterPath).Trim();
                if (int.TryParse(counterStr, out int savedCounter))
                {
                    contador = savedCounter + 1;
                }
            }

            // Formatear el ID de la experiencia como un número de 3 dígitos
            experienciaID = contador.ToString("000");

            // Guardar el contador actualizado
            File.WriteAllText(counterPath, contador.ToString());
            Debug.Log($"ID de experiencia generado: {experienciaID} (Contador: {contador})");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error generando ID de experiencia: {e.Message}");
            experienciaID = "001"; // Valor por defecto en caso de error
        }
    }

    /// <summary>
    /// Obtiene el nombre del dispositivo utilizando SystemInfo
    /// </summary>
    private void ObtenerDeviceName()
    {
        try
        {
            deviceName = SystemInfo.deviceName;
            Debug.Log($"Nombre del dispositivo obtenido: {deviceName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al obtener nombre del dispositivo: {e.Message}");
            deviceName = "Error_" + SystemInfo.deviceType.ToString();
        }
    }

    private void InitializeSceneDictionaries()
    {
        // Inicializar diccionarios para cada escena posible
        // Incluir todas las escenas conocidas, incluyendo "Anger" (ya que parece estar duplicada)
        // y "Close" (para registrar la atención en la escena final)
        string[] sceneNames = new string[] {
            "Anger",
            escenaSmashGame,
            escenaReactionGame,
            escenaBalloonGame,
            "Close"
        };

        foreach (string sceneName in sceneNames)
        {
            // Solo inicializar si no existe ya
            if (!colliderTotalLookTimesByScene.ContainsKey(sceneName))
            {
                colliderTotalLookTimesByScene[sceneName] = new Dictionary<string, float>() {
                    {"Norte", 0f},
                    {"Sur", 0f},
                    {"Este", 0f},
                    {"Oeste", 0f},
                    {"Ninguno", 0f}
                };
            }
        }

        Debug.Log("Diccionarios de escenas inicializados: " + string.Join(", ", colliderTotalLookTimesByScene.Keys));
    }

 

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Cuando se carga una nueva escena
        string newSceneName = scene.name;

        // Finalizar el seguimiento de mirada en la escena anterior
        if (currentColliderName != "Ninguno")
        {
            float tiempoMiradaFinal = Time.time - currentColliderStartTime;
            if (colliderTotalLookTimesByScene.ContainsKey(currentSceneName))
            {
                colliderTotalLookTimesByScene[currentSceneName][currentColliderName] += tiempoMiradaFinal;
            }
            Log($"FIN_COLLIDER_{currentColliderName.ToUpper()}", $"Escena: {currentSceneName}, Duración final: {tiempoMiradaFinal:F2} segundos");
        }

        // Registrar la transición
        float transitionTime = Time.time - startTime;
        Log("CAMBIO_ESCENA", $"De {currentSceneName} a {newSceneName} en {transitionTime:F2} segundos");
        sceneHistory.Add(newSceneName);
        sceneTransitionTimes.Add(transitionTime);

        // Actualizar la escena actual
        currentSceneName = newSceneName;
        currentSceneStartTime = Time.time - startTime;
        currentSceneIndex++;

        // Reiniciar el collider actual
        currentColliderName = "Ninguno";
        currentColliderStartTime = Time.time;

        // Guardar los cambios
        SaveLogBuffer();

        Debug.Log($"Transición a escena: {newSceneName}. Índice: {currentSceneIndex}");

        // Asegurarse de que el diccionario para la nueva escena exista
        if (!colliderTotalLookTimesByScene.ContainsKey(newSceneName))
        {
            colliderTotalLookTimesByScene[newSceneName] = new Dictionary<string, float>() {
                {"Norte", 0f},
                {"Sur", 0f},
                {"Este", 0f},
                {"Oeste", 0f},
                {"Ninguno", 0f}
            };

            Debug.Log($"Creado nuevo diccionario para la escena: {newSceneName}");
        }

        // Buscar referencias a los colliders en la escena actual
        FindCollidersInScene();

        // Si la nueva escena es Close, generar el resumen automáticamente
        if (newSceneName == "Close")
        {
            Debug.Log("Escena Close detectada. Generando resumen...");
            // Esperar un pequeño tiempo para asegurar que todos los datos estén guardados
            //StartCoroutine(GenerarResumenConRetraso(1.0f));
        }

        // Registrar inicio del minijuego correspondiente
        if (newSceneName == escenaSmashGame)
        {
            RegistrarEvento("INICIO_MINIJUEGO_SMASH", "El minijuego de Smash ha sido iniciado");
        }
        else if (newSceneName == escenaReactionGame)
        {
            RegistrarEvento("INICIO_MINIJUEGO_REACTION", "El minijuego de Reaction ha sido iniciado");
        }
        else if (newSceneName == escenaBalloonGame)
        {
            RegistrarEvento("INICIO_MINIJUEGO_BALLOON", "El minijuego de Balloon ha sido iniciado");
        }
    }

    private void FindCollidersInScene()
    {
        Debug.Log("Buscando colliders en la escena: " + currentSceneName);

        // Buscar colliders por etiquetas (tags) primero
        GameObject norteObj = GameObject.FindGameObjectWithTag("ColliderNorte");
        if (norteObj)
        {
            norteCollider = norteObj.transform;
            Debug.Log("Encontrado ColliderNorte por tag: " + norteObj.name);
        }

        GameObject surObj = GameObject.FindGameObjectWithTag("ColliderSur");
        if (surObj)
        {
            surCollider = surObj.transform;
            Debug.Log("Encontrado ColliderSur por tag: " + surObj.name);
        }

        GameObject esteObj = GameObject.FindGameObjectWithTag("ColliderEste");
        if (esteObj)
        {
            esteCollider = esteObj.transform;
            Debug.Log("Encontrado ColliderEste por tag: " + esteObj.name);
        }

        GameObject oesteObj = GameObject.FindGameObjectWithTag("ColliderOeste");
        if (oesteObj)
        {
            oesteCollider = oesteObj.transform;
            Debug.Log("Encontrado ColliderOeste por tag: " + oesteObj.name);
        }

        // Si no se encontraron por tag, buscar por nombre
        if (norteCollider == null || surCollider == null || esteCollider == null || oesteCollider == null)
        {
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                // Buscar por nombres que contengan las palabras clave
                if (norteCollider == null && (obj.name.Contains("Norte") || obj.name.Contains("North")))
                {
                    norteCollider = obj.transform;
                    Debug.Log("Encontrado ColliderNorte por nombre: " + obj.name);
                }
                else if (surCollider == null && (obj.name.Contains("Sur") || obj.name.Contains("South")))
                {
                    surCollider = obj.transform;
                    Debug.Log("Encontrado ColliderSur por nombre: " + obj.name);
                }
                else if (esteCollider == null && (obj.name.Contains("Este") || obj.name.Contains("East")))
                {
                    esteCollider = obj.transform;
                    Debug.Log("Encontrado ColliderEste por nombre: " + obj.name);
                }
                else if (oesteCollider == null && (obj.name.Contains("Oeste") || obj.name.Contains("West")))
                {
                    oesteCollider = obj.transform;
                    Debug.Log("Encontrado ColliderOeste por nombre: " + obj.name);
                }
            }
        }

        // También buscar la cámara o cabeza del jugador
        if (playerHead == null)
        {
            GameObject mainCamera = Camera.main?.gameObject;
            if (mainCamera)
            {
                playerHead = mainCamera.transform;
                Debug.Log("Usando cámara principal como referencia de mirada: " + mainCamera.name);
            }
            else
            {
                // Intentar encontrar otra cámara si la principal no está disponible
                Camera[] cameras = Camera.allCameras;
                if (cameras.Length > 0)
                {
                    playerHead = cameras[0].transform;
                    Debug.Log("Usando cámara alternativa como referencia de mirada: " + cameras[0].name);
                }
            }
        }

        // Crear colliders virtuales si no se encuentran
        CreateVirtualColliderIfMissing(ref norteCollider, "Norte", new Vector3(0, 0, 10));
        CreateVirtualColliderIfMissing(ref surCollider, "Sur", new Vector3(0, 0, -10));
        CreateVirtualColliderIfMissing(ref esteCollider, "Este", new Vector3(10, 0, 0));
        CreateVirtualColliderIfMissing(ref oesteCollider, "Oeste", new Vector3(-10, 0, 0));

        ValidarColliders();
    }

    private void CreateVirtualColliderIfMissing(ref Transform collider, string name, Vector3 direction)
    {
        if (collider == null)
        {
            // Crear un objeto virtual para representar la dirección
            GameObject virtualCollider = new GameObject("Virtual_Collider_" + name);

            // Posicionar el collider en relación a la cámara
            if (playerHead != null)
            {
                virtualCollider.transform.position = playerHead.position + direction;
            }
            else
            {
                // Si no hay referencia a la cámara, usar una posición predeterminada
                virtualCollider.transform.position = direction;
            }

            // Asignar la referencia
            collider = virtualCollider.transform;

            // Hacer que el objeto sea hijo del TelemetriaManager para que persista entre escenas
            virtualCollider.transform.parent = this.transform;

            Debug.Log("Creado collider virtual para dirección " + name);
        }
    }

    private void ValidarColliders()
    {
        if (playerHead == null)
        {
            Debug.LogError("TelemetriaManagerAnger: No se ha asignado la cabeza del jugador (playerHead)");
        }

        if (norteCollider == null)
        {
            Debug.LogWarning("TelemetriaManagerAnger: No se ha asignado el collider Norte en la escena " + currentSceneName);
        }

        if (surCollider == null)
        {
            Debug.LogWarning("TelemetriaManagerAnger: No se ha asignado el collider Sur en la escena " + currentSceneName);
        }

        if (esteCollider == null)
        {
            Debug.LogWarning("TelemetriaManagerAnger: No se ha asignado el collider Este en la escena " + currentSceneName);
        }

        if (oesteCollider == null)
        {
            Debug.LogWarning("TelemetriaManagerAnger: No se ha asignado el collider Oeste en la escena " + currentSceneName);
        }
    }

    void Start()
    {
        startTime = Time.time;
        lastGazeLogTime = Time.time;
        Log("INICIO_APLICACION", $"Dispositivo: {deviceName}, ID Experiencia: {experienciaCompleta}");
        isLogReady = true;
        Debug.Log("TelemetriaManagerAnger: isLogReady = true");

        // Iniciar la corrutina para verificar la mirada periódicamente
        StartCoroutine(VerificarMiradaPeriodicamente());
    }

    void Update()
    {
        // Guardar periódicamente los logs para evitar pérdida de datos
        if (isLogReady && Time.time - lastSaveTime > saveInterval)
        {
            lastSaveTime = Time.time;
            SaveLogBuffer();
        }

        // Verificar continuamente hacia dónde está mirando el usuario
        VerificarMirada();
    }

    private IEnumerator VerificarMiradaPeriodicamente()
    {
        while (true)
        {
            // Esperar el intervalo configurado
            yield return new WaitForSeconds(gazeLogInterval);

            // Registrar el collider actual en el log si ya ha pasado suficiente tiempo
            if (Time.time - lastGazeLogTime >= gazeLogInterval)
            {
                lastGazeLogTime = Time.time;

                // Registrar el collider que está mirando actualmente
                if (currentColliderName != "Ninguno")
                {
                    if (colliderTotalLookTimesByScene.ContainsKey(currentSceneName))
                    {
                        Log($"CONTINUE_COLLIDER_{currentColliderName.ToUpper()}",
                            $"Escena: {currentSceneName}, Tiempo acumulado: {colliderTotalLookTimesByScene[currentSceneName][currentColliderName]:F2} segundos");
                    }
                }
                else
                {
                    Log("NO_COLLIDER_VIEW", $"Escena: {currentSceneName}, El usuario no está mirando a ningún collider");
                }
            }
        }
    }

    private void VerificarMirada()
    {
        if (playerHead == null || !isLogReady) return;

        // Comprueba hacia qué collider está mirando el usuario
        string colliderActual = ObtenerColliderEnFoco();

        // Si cambió el collider, registramos el tiempo y el cambio
        if (colliderActual != currentColliderName)
        {
            // Registrar el tiempo que estuvo mirando al collider anterior
            if (currentColliderName != "Ninguno")
            {
                float tiempoMiradaAnterior = Time.time - currentColliderStartTime;
                if (colliderTotalLookTimesByScene.ContainsKey(currentSceneName))
                {
                    colliderTotalLookTimesByScene[currentSceneName][currentColliderName] += tiempoMiradaAnterior;
                }
                Log($"FIN_COLLIDER_{currentColliderName.ToUpper()}",
                    $"Escena: {currentSceneName}, Duración: {tiempoMiradaAnterior:F2} segundos, Acumulado: {colliderTotalLookTimesByScene[currentSceneName][currentColliderName]:F2} segundos");
            }

            // Actualizar al nuevo collider
            currentColliderName = colliderActual;
            currentColliderStartTime = Time.time;

            // Registrar el nuevo collider
            if (currentColliderName != "Ninguno")
            {
                Log($"INICIO_COLLIDER_{currentColliderName.ToUpper()}",
                    $"Escena: {currentSceneName}, Tiempo desde inicio: {Time.time - startTime:F2} segundos");
            }
        }
    }

    private string ObtenerColliderEnFoco()
    {
        // Verifica si está mirando alguno de los colliders
        if (EstaLookingAt(norteCollider)) return "Norte";
        if (EstaLookingAt(surCollider)) return "Sur";
        if (EstaLookingAt(esteCollider)) return "Este";
        if (EstaLookingAt(oesteCollider)) return "Oeste";

        // Si no está mirando ninguno, devuelve "Ninguno"
        return "Ninguno";
    }

    private bool EstaLookingAt(Transform targetTransform)
    {
        if (playerHead == null || targetTransform == null) return false;

        // Actualizar la posición del collider virtual si es uno de nuestros colliders virtuales
        if (targetTransform.name.StartsWith("Virtual_Collider_"))
        {
            Vector3 direction = Vector3.zero;

            if (targetTransform.name.Contains("Norte"))
                direction = new Vector3(0, 0, 10);
            else if (targetTransform.name.Contains("Sur"))
                direction = new Vector3(0, 0, -10);
            else if (targetTransform.name.Contains("Este"))
                direction = new Vector3(10, 0, 0);
            else if (targetTransform.name.Contains("Oeste"))
                direction = new Vector3(-10, 0, 0);

            targetTransform.position = playerHead.position + direction;
        }

        // Calcula el vector desde la cabeza del usuario hasta el objeto
        Vector3 viewVector = Vector3.Normalize(targetTransform.position - playerHead.position);

        // Calcula el producto escalar (dot product) para determinar la alineación
        float dotView = Vector3.Dot(playerHead.forward, viewVector);

        // Si estamos usando colliders virtuales, ajustar el umbral para ser menos estricto
        float thresholdToUse = targetTransform.name.StartsWith("Virtual_Collider_")
            ? alignmentThreshold * 0.9f  // Reducir el umbral un 10% para los colliders virtuales
            : alignmentThreshold;

        // Retorna true si el dot product es mayor que el umbral
        return dotView >= thresholdToUse;
    }

    private void CreateLogFile()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string directory = Path.Combine(Application.persistentDataPath, "Logs");

        // Crear el directorio si no existe
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Incluir el ID de la experiencia en el nombre del archivo
        //logFilePath = Path.Combine(directory, $"log_anger_{experienciaCompleta}_{timestamp}.txt");

        try
        {
            // Escribir encabezado del archivo
            using (StreamWriter writer = new StreamWriter(logFilePath, false))
            {
                writer.WriteLine("Timestamp,TiempoDesdeInicio,Escena,Evento,Datos");
                writer.Flush();
            }

            isLogFileCreated = true;
            Debug.Log($"Archivo de telemetría creado en: {logFilePath}");

            // Registrar inmediatamente el ID de la experiencia y el dispositivo
            RegistrarEvento("DISPOSITIVO", deviceName);
            RegistrarEvento("EXPERIENCIA_ID", experienciaCompleta);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al crear archivo de telemetría: {e.Message}");
        }
    }

    private void Log(string evento, string datos)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            float tiempoTranscurrido = Time.time - startTime;

            string logEntry = $"{timestamp},{tiempoTranscurrido},{currentSceneName},{evento},{datos}\n";
            logBuffer.Append(logEntry);

            // Guardar en el archivo cada cierto tiempo o cantidad de entradas
            if (logBuffer.Length > 500)
            {
                SaveLogBuffer();
            }

            Debug.Log($"[TELEMETRÍA] {evento}: {datos}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en Log: {e.Message}");
        }
    }

    private void SaveLogBuffer()
    {
        if (!isLogFileCreated || logBuffer.Length == 0) return;

        try
        {
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.Write(logBuffer.ToString());
                writer.Flush();
            }

            string savedContent = logBuffer.ToString();
            logBuffer.Clear();
            Debug.Log($"Guardados {savedContent.Split('\n').Length - 1} registros de telemetría");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al guardar datos de telemetría: {e.Message}\n{e.StackTrace}");
            try
            {
                // Intento alternativo de guardado
                string emergencyPath = Path.Combine(Application.persistentDataPath, $"emergency_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(emergencyPath, logBuffer.ToString());
                Debug.Log($"Guardado de emergencia realizado en: {emergencyPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error en guardado de emergencia: {ex.Message}");
            }
        }
    }

    public void ForzarGuardado()
    {
        SaveLogBuffer();
    }

    private void OnApplicationQuit()
    {
        // Registrar el tiempo final del último collider que se estaba mirando
        if(currentColliderName != "Ninguno")
    {
            float tiempoMiradaFinal = Time.time - currentColliderStartTime;
            if (colliderTotalLookTimesByScene.ContainsKey(currentSceneName))
            {
                colliderTotalLookTimesByScene[currentSceneName][currentColliderName] += tiempoMiradaFinal;
            }
            Log($"FIN_COLLIDER_{currentColliderName.ToUpper()}",
                $"Escena: {currentSceneName}, Duración final: {tiempoMiradaFinal:F2} segundos, Total acumulado: {colliderTotalLookTimesByScene[currentSceneName][currentColliderName]:F2} segundos");
        }

        // Asegurarse de guardar todos los datos antes de cerrar
        Log("FIN_APLICACION", $"Tiempo total: {Time.time - startTime}");
        SaveLogBuffer();

        // Solo generar el resumen si no se ha guardado uno con código
        if (!resumenGuardadoConCodigo)
        {
            GenerarYGuardarResumen();
        }
        else
        {
            Debug.Log("No se generará un nuevo resumen en OnApplicationQuit porque ya existe uno con código de usuario.");
        }
    }

    public void GenerarYGuardarResumen()
    {
        try
        {
            // Si ya guardamos un resumen con código, no lo volvemos a hacer
            if (resumenGuardadoConCodigo)
            {
                Debug.Log("No se generará un nuevo resumen porque ya existe uno con el código de usuario.");
                return;
            }

            // Generar el JSON
            string jsonData = GenerarJSONResumen();

            // Guardar los datos encriptados con EncryptionManager
            GuardarResumenEncriptado(jsonData);

            // Marcar como guardado para evitar duplicados
            resumenGuardadoConCodigo = true;
            Debug.Log($"Resumen JSON generado y guardado con código de usuario: {codigoUsuario}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al generar el resumen manualmente: {e.Message}");
        }
    }

    private void GuardarResumenEncriptado(string jsonData)
    {
        try
        {
            // Asegurarse de que el directorio exista
            string directory = Path.Combine(Application.persistentDataPath, "Logs");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log($"Creando directorio para resumen: {directory}");
            }

            // Usar SIEMPRE el mismo nombre de archivo para sobrescribir cualquier versión anterior
            string encryptedPath = Path.Combine(directory, $"resumen_anger{DateTime.Now:yyyyMMdd_HHmmss}.json");

            // Intentar guardar versión encriptada usando EncryptionManager
            bool encriptacionExitosa = false;

            if (encryptionManager != null)
            {
                try
                {
                    encriptacionExitosa = encryptionManager.GuardarJSONEncriptado(jsonData, encryptedPath);

                    if (encriptacionExitosa)
                    {
                        Debug.Log($"Resumen encriptado guardado exitosamente en: {encryptedPath}");
                        Log("RESUMEN_GUARDADO", $"Encriptado: {encriptacionExitosa}, Ruta: {encryptedPath}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error al encriptar con EncryptionManager: {ex.Message}");
                    encriptacionExitosa = false;
                }
            }
            else
            {
                Debug.LogWarning("EncryptionManager no disponible, no se pudo encriptar");
            }

            // No guardar nada si falla la encriptación
            if (!encriptacionExitosa)
            {
                Debug.LogWarning("No se pudo guardar el resumen encriptado. No se guardará ningún archivo.");
                Log("ERROR_RESUMEN", "No se pudo guardar el resumen encriptado");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al guardar resumen: {e.Message}\n{e.StackTrace}");
            Log("ERROR_GUARDAR_RESUMEN", e.Message);
        }
    }

    /// <summary>
    /// Método para registrar el código del usuario y actualizar el ID de la experiencia
    /// </summary>
    /// <param name="codigo">Código del usuario</param>
    public void RegistrarCodigoUsuario(string codigo)
    {
        try
        {
            // Si el código es válido, registrarlo
            if (!string.IsNullOrEmpty(codigo))
            {
                // Guardar el código anterior para reportar cambios
                string codigoAnterior = codigoUsuario;
                codigoUsuario = codigo.Trim(); // Asegurarse de eliminar espacios en blanco

                // Actualizar el ID completo de la experiencia
                string antiguoID = experienciaCompleta;
                experienciaCompleta = $"{equipoID}{experienciaID}{codigoUsuario}";

                // Registrar el evento
                Log("CODIGO_USUARIO", codigoUsuario);
                Log("EXPERIENCIA_ID_ACTUALIZADA", $"Anterior: {antiguoID}, Nueva: {experienciaCompleta}");

                Debug.Log($"Código de usuario registrado: {codigoUsuario}");
                Debug.Log($"ID de experiencia actualizado: {experienciaCompleta}");

                // Resetear la bandera para que se genere un nuevo resumen con el código
                resumenGuardadoConCodigo = false;

                // Guardar inmediatamente
                SaveLogBuffer();
            }
            else
            {
                Debug.LogWarning("Se intentó registrar un código de usuario vacío");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al registrar código de usuario: {e.Message}");
        }
    }

    // Métodos auxiliares para calcular estadísticas

    private float CalcularTiempoTotalExperiencia()
    {
        // Si la última escena es "Close", usar su tiempo de transición
        if (sceneHistory.Count > 0 && sceneHistory[sceneHistory.Count - 1] == "Close")
        {
            return sceneTransitionTimes[sceneTransitionTimes.Count - 1];
        }

        // Si no, usar el tiempo actual
        return Time.time - startTime;
    }

    private float CalcularTiempoTotalEscena(string nombreEscena)
    {
        // Usar duración fija para los minijuegos
        if (nombreEscena == escenaSmashGame)
            return smashGameDuration;

        if (nombreEscena == escenaReactionGame)
            return reactionGameDuration;

        if (nombreEscena == escenaBalloonGame)
            return balloonGameDuration;

        // Para otras escenas, calcular el tiempo real
        float tiempoTotal = 0f;

        for (int i = 0; i < sceneHistory.Count; i++)
        {
            if (sceneHistory[i] == nombreEscena)
            {
                float inicioTiempo = sceneTransitionTimes[i];
                float finTiempo;

                if (i < sceneHistory.Count - 1)
                {
                    finTiempo = sceneTransitionTimes[i + 1];
                }
                else
                {
                    finTiempo = CalcularTiempoTotalExperiencia();
                }

                tiempoTotal += (finTiempo - inicioTiempo);
            }
        }

        // Asegurarnos de que el tiempo nunca sea cero para evitar divisiones por cero
        tiempoTotal = Mathf.Max(0.001f, tiempoTotal);

        return tiempoTotal;
    }

    private float CalcularEfectividadSmash()
    {
        // Si no hay objetos golpeados, devolver 0
        if (smashObjectsHit == 0) return 0f;

        // Usar tiempo fijo de 120 segundos
        return smashObjectsHit / 120f;
    }

    private float CalcularEfectividadReaction()
    {
        // Si no hay botones presionados, devolver 0
        if (reactionButtonsPressed == 0) return 0f;

        // Usar tiempo fijo de 120 segundos
        return reactionButtonsPressed / 120f;
    }

    private float CalcularEfectividadBalloon()
    {
        // Si no hay globos reventados, devolver 0
        if (balloonsPopped == 0) return 0f;

        // Usar tiempo fijo de 120 segundos
        return balloonsPopped / 120f;
    }

    private string GenerarJSONResumen()
    {
        try
        {
            // Añadir método auxiliar para formatear números con punto decimal
            System.Globalization.CultureInfo invCulture = System.Globalization.CultureInfo.InvariantCulture;

            // Calcular estadísticas principales
            float efectividadSmash = CalcularEfectividadSmash();
            float efectividadReaction = CalcularEfectividadReaction();
            float efectividadBalloon = CalcularEfectividadBalloon();
            float tiempoTotalExperiencia = CalcularTiempoTotalExperiencia();

            // Crear un objeto JSON para almacenar los datos del resumen
            System.Text.StringBuilder jsonBuilder = new System.Text.StringBuilder();
            jsonBuilder.AppendLine("{");

            // Añadir el código del usuario al inicio del JSON
            if (!string.IsNullOrEmpty(codigoUsuario) && codigoUsuario != "00")
            {
                // Asegurarnos de que el código esté dentro de comillas si es un string
                bool esNumero = int.TryParse(codigoUsuario, out _);

                if (esNumero)
                {
                    // Si es un número, no necesita comillas
                    jsonBuilder.AppendLine("  \"Código Uniandes\": " + codigoUsuario + ",");
                }
                else
                {
                    // Si es texto, necesita comillas
                    jsonBuilder.AppendLine("  \"Código Uniandes\": \"" + codigoUsuario + "\",");
                }
            }

            // Información general
            jsonBuilder.AppendLine("  \"dispositivo\": \"" + deviceName + "\",");
            jsonBuilder.AppendLine("  \"experienciaID\": \"" + experienciaCompleta + "\",");
            jsonBuilder.AppendLine("  \"tiempoTotal\": " + tiempoTotalExperiencia.ToString("F2", invCulture) + ",");

            // Historial de escenas
            jsonBuilder.AppendLine("  \"secuenciaEscenas\": [");
            for (int i = 0; i < sceneHistory.Count; i++)
            {
                string escena = sceneHistory[i];
                float tiempo = sceneTransitionTimes[i];

                // Calcular duración de cada escena
                float duracionEscena;
                if (i < sceneHistory.Count - 1)
                {
                    duracionEscena = sceneTransitionTimes[i + 1] - tiempo;
                }
                else
                {
                    duracionEscena = tiempoTotalExperiencia - tiempo;
                }

                jsonBuilder.AppendLine("    {");
                jsonBuilder.AppendLine("      \"escena\": \"" + escena + "\",");
                jsonBuilder.AppendLine("      \"tiempoInicio\": " + tiempo.ToString("F2", invCulture) + ",");
                jsonBuilder.AppendLine("      \"duracion\": " + duracionEscena.ToString("F2", invCulture));
                if (i < sceneHistory.Count - 1)
                    jsonBuilder.AppendLine("    },");
                else
                    jsonBuilder.AppendLine("    }");
            }
            jsonBuilder.AppendLine("  ],");

            // Atención del usuario
            jsonBuilder.AppendLine("  \"atencionUsuario\": {");

            // Crear una lista de escenas únicas a procesar (excluyendo "Close" y duplicados)
            List<string> escenasUnicas = new List<string>();
            foreach (string escena in sceneHistory)
            {
                if (escena != "Close" && !escenasUnicas.Contains(escena))
                {
                    escenasUnicas.Add(escena);
                }
            }

            // Procesar cada escena única que fue visitada
            for (int escenaIndex = 0; escenaIndex < escenasUnicas.Count; escenaIndex++)
            {
                string escena = escenasUnicas[escenaIndex];

                if (colliderTotalLookTimesByScene.ContainsKey(escena))
                {
                    Dictionary<string, float> tiemposMirada = colliderTotalLookTimesByScene[escena];

                    // Calcular el tiempo real de la escena
                    float tiempoRealEscena = CalcularTiempoTotalEscena(escena);

                    jsonBuilder.AppendLine("    \"" + escena + "\": {");
                    jsonBuilder.AppendLine("      \"tiempoTotal\": " + tiempoRealEscena.ToString("F2", invCulture) + ",");
                    jsonBuilder.AppendLine("      \"tiemposPorDireccion\": {");
                    jsonBuilder.AppendLine("        \"norte\": " + tiemposMirada["Norte"].ToString("F2", invCulture) + ",");
                    jsonBuilder.AppendLine("        \"sur\": " + tiemposMirada["Sur"].ToString("F2", invCulture) + ",");
                    jsonBuilder.AppendLine("        \"este\": " + tiemposMirada["Este"].ToString("F2", invCulture) + ",");
                    jsonBuilder.AppendLine("        \"oeste\": " + tiemposMirada["Oeste"].ToString("F2", invCulture) + ",");
                    jsonBuilder.AppendLine("        \"ninguno\": " + tiemposMirada["Ninguno"].ToString("F2", invCulture));
                    jsonBuilder.AppendLine("      },");

                    // Calcular porcentajes si hay tiempo
                    if (tiempoRealEscena > 0)
                    {
                        jsonBuilder.AppendLine("      \"porcentajes\": {");
                        jsonBuilder.AppendLine("        \"norte\": " + (tiemposMirada["Norte"] / tiempoRealEscena * 100).ToString("F2", invCulture) + ",");
                        jsonBuilder.AppendLine("        \"sur\": " + (tiemposMirada["Sur"] / tiempoRealEscena * 100).ToString("F2", invCulture) + ",");
                        jsonBuilder.AppendLine("        \"este\": " + (tiemposMirada["Este"] / tiempoRealEscena * 100).ToString("F2", invCulture) + ",");
                        jsonBuilder.AppendLine("        \"oeste\": " + (tiemposMirada["Oeste"] / tiempoRealEscena * 100).ToString("F2", invCulture) + ",");
                        jsonBuilder.AppendLine("        \"ninguno\": " + (tiemposMirada["Ninguno"] / tiempoRealEscena * 100).ToString("F2", invCulture));
                        jsonBuilder.AppendLine("      }");
                    }
                    else
                    {
                        jsonBuilder.AppendLine("      \"porcentajes\": {}");
                    }

                    // Añadir una coma si NO es la última escena
                    if (escenaIndex < escenasUnicas.Count - 1)
                    {
                        jsonBuilder.AppendLine("    },");
                    }
                    else
                    {
                        jsonBuilder.AppendLine("    }");
                    }
                }
            }
            jsonBuilder.AppendLine("  },");

            // Minijuegos
            jsonBuilder.AppendLine("  \"minijuegos\": {");

            // Smash
            jsonBuilder.AppendLine("    \"smash\": {");
            jsonBuilder.AppendLine("      \"objetosGolpeados\": " + smashObjectsHit + ",");
            jsonBuilder.AppendLine("      \"duracion\": 120.00,");
            jsonBuilder.AppendLine("      \"efectividad\": " + efectividadSmash.ToString("F2", invCulture));
            if (smashHitTimes.Count > 0)
            {
                jsonBuilder.Append("      ,\"tiemposGolpes\": [");
                for (int i = 0; i < smashHitTimes.Count; i++)
                {
                    jsonBuilder.Append(smashHitTimes[i].ToString("F2", invCulture));
                    if (i < smashHitTimes.Count - 1)
                        jsonBuilder.Append(", ");
                }
                jsonBuilder.AppendLine("]");
            }
            jsonBuilder.AppendLine("    },");

            // Reaction
            jsonBuilder.AppendLine("    \"reaction\": {");
            jsonBuilder.AppendLine("      \"botonesPresionados\": " + reactionButtonsPressed + ",");
            jsonBuilder.AppendLine("      \"duracion\": 120.00,");
            jsonBuilder.AppendLine("      \"efectividad\": " + efectividadReaction.ToString("F2", invCulture));
            if (reactionPressTimes.Count > 0)
            {
                jsonBuilder.Append("      ,\"tiemposPresion\": [");
                for (int i = 0; i < reactionPressTimes.Count; i++)
                {
                    jsonBuilder.Append(reactionPressTimes[i].ToString("F2", invCulture));
                    if (i < reactionPressTimes.Count - 1)
                        jsonBuilder.Append(", ");
                }
                jsonBuilder.AppendLine("]");
            }
            jsonBuilder.AppendLine("    },");

            // Balloon
            jsonBuilder.AppendLine("    \"balloon\": {");
            jsonBuilder.AppendLine("      \"globosReventados\": " + balloonsPopped + ",");
            jsonBuilder.AppendLine("      \"duracion\": 120.00,");
            jsonBuilder.AppendLine("      \"efectividad\": " + efectividadBalloon.ToString("F2", invCulture));
            if (balloonPopTimes.Count > 0)
            {
                jsonBuilder.Append("      ,\"tiemposGlobos\": [");
                for (int i = 0; i < balloonPopTimes.Count; i++)
                {
                    jsonBuilder.Append(balloonPopTimes[i].ToString("F2", invCulture));
                    if (i < balloonPopTimes.Count - 1)
                        jsonBuilder.Append(", ");
                }
                jsonBuilder.AppendLine("]");
            }
            jsonBuilder.AppendLine("    }");
            jsonBuilder.AppendLine("  },");

            // Resumen general
            jsonBuilder.AppendLine("  \"resumenGeneral\": {");
            int totalAcciones = smashObjectsHit + reactionButtonsPressed + balloonsPopped;
            float tiempoTotalMinijuegos = 360f;
            float efectividadGeneral = totalAcciones > 0 ? totalAcciones / tiempoTotalMinijuegos : 0;

            jsonBuilder.AppendLine("    \"totalAcciones\": " + totalAcciones + ",");
            jsonBuilder.AppendLine("    \"tiempoTotalMinijuegos\": 360.00,");
            jsonBuilder.AppendLine("    \"efectividadGeneral\": " + efectividadGeneral.ToString("F2", invCulture));
            jsonBuilder.AppendLine("  }");

            // Cerrar el objeto JSON
            jsonBuilder.AppendLine("}");

            return jsonBuilder.ToString();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al generar JSON resumen: {e.Message}\n{e.StackTrace}");

            // Devolver un JSON de error básico pero válido
            return "{ \"error\": \"Error generando datos de telemetría\", \"mensaje\": \"" + e.Message.Replace("\"", "'") + "\" }";
        }
    }

    // ==== Métodos públicos para registrar eventos desde otros scripts ====

    // Eventos del minijuego Smash
    public void RegistrarObjetoGolpeado(string nombreObjeto)
    {
        try
        {
            if (!isLogReady) return;

            // Verificar si el nombre del objeto contiene "desconocido"
            // Si es así, ignorarlo para evitar contar dos veces
            if (nombreObjeto.Contains("desconocido"))
            {
                Debug.Log($"Ignorando registro duplicado: {nombreObjeto}");
                return;
            }

            smashObjectsHit++;
            float tiempoActual = Time.time - startTime;
            smashHitTimes.Add(tiempoActual);
            smashHitObjects.Add(nombreObjeto);

            Log("OBJETO_GOLPEADO", $"Objeto: {nombreObjeto}, Número: {smashObjectsHit}, Tiempo: {tiempoActual:F2}");
            SaveLogBuffer(); // Guardar inmediatamente
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarObjetoGolpeado: {e.Message}");
        }
    }

    // Eventos del minijuego Reaction
    public void RegistrarBotonPresionado()
    {
        try
        {
            if (!isLogReady) return;

            // Obtener el tiempo actual
            float tiempoActual = Time.time - startTime;

            // Rechazar registros demasiado cercanos al anterior
            // Usamos un umbral más amplio de 0.05 segundos
            if (tiempoActual - ultimoRegistroButton < 0.05f)
            {
                Debug.Log($"Ignorando registro duplicado de botón. Tiempo desde último: {tiempoActual - ultimoRegistroButton:F4}s");
                return;
            }

            // Actualizar el tiempo del último registro
            ultimoRegistroButton = tiempoActual;

            // Registrar el evento
            reactionButtonsPressed++;
            reactionPressTimes.Add(tiempoActual);

            Log("OPRIME_BOTON", $"Número: {reactionButtonsPressed}, Tiempo: {tiempoActual:F2}");
            SaveLogBuffer();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarBotonPresionado: {e.Message}");
        }
    }

    // Eventos del minijuego Balloon
    public void RegistrarGloboGolpeado()
    {
        try
        {
            if (!isLogReady) return;

            // Obtener el tiempo actual
            float tiempoActual = Time.time - startTime;

            // Rechazar registros demasiado cercanos al anterior
            // Usamos un umbral más amplio de 0.05 segundos
            if (tiempoActual - ultimoRegistroGlobo < 0.05f)
            {
                Debug.Log($"Ignorando registro duplicado de globo. Tiempo desde último: {tiempoActual - ultimoRegistroGlobo:F4}s");
                return;
            }

            // Actualizar el tiempo del último registro
            ultimoRegistroGlobo = tiempoActual;

            // Registrar el evento
            balloonsPopped++;
            balloonPopTimes.Add(tiempoActual);

            Log("GLOBO_GOLPEADO", $"Número: {balloonsPopped}, Tiempo: {tiempoActual:F2}");
            SaveLogBuffer();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarGloboGolpeado: {e.Message}");
        }
    }

    // Método para registrar eventos genéricos
    public void RegistrarEvento(string nombreEvento, string datos)
    {
        try
        {
            if (!isLogReady) return;

            Log(nombreEvento, datos);
            SaveLogBuffer(); // Guardar inmediatamente
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarEvento: {e.Message}");
        }
    }

    // Método para limpiar todos los datos (llamar cuando se reinicia la aplicación)
    public void Reset()
    {
        smashObjectsHit = 0;
        reactionButtonsPressed = 0;
        balloonsPopped = 0;
        smashHitTimes.Clear();
        reactionPressTimes.Clear();
        balloonPopTimes.Clear();
        smashHitObjects.Clear();
        ultimoRegistroButton = -5f;
        ultimoRegistroGlobo = -5f;

        Debug.Log("Datos de telemetría reiniciados");
    }
}