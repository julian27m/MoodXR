using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public string escenaSmashGame = "MinijuegoSmash";

    [Tooltip("Nombre de la escena del segundo minijuego (Reaction)")]
    public string escenaReactionGame = "MinijuegoReaction";

    [Tooltip("Nombre de la escena del tercer minijuego (Balloon)")]
    public string escenaBalloonGame = "MinijuegoBalloon";

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

        // Inicializar diccionarios para cada escena
        InitializeSceneDictionaries();

        // Crear el archivo de log
        CreateLogFile();

        // Registrar la escena inicial
        currentSceneName = SceneManager.GetActiveScene().name;
        sceneHistory.Add(currentSceneName);
        sceneTransitionTimes.Add(0f);
        currentSceneStartTime = 0f;

        Debug.Log("TelemetriaManagerAnger inicializado en escena: " + currentSceneName);

        // Suscribirse al evento de cambio de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void InitializeSceneDictionaries()
    {
        // Inicializar diccionarios para cada escena posible
        string[] sceneNames = new string[] { escenaSmashGame, escenaReactionGame, escenaBalloonGame };

        foreach (string sceneName in sceneNames)
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

    private IEnumerator GenerarResumenConRetraso(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        GenerarResumenFinal();
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
        if (newSceneName == "Close")
        {
            Debug.Log("Escena Close detectada. Generando resumen...");
            // Esperar un pequeño tiempo para asegurar que todos los datos estén guardados
            StartCoroutine(GenerarResumenConRetraso(1.0f));
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

        // Buscar referencias a los colliders en la escena actual
        FindCollidersInScene();
    }

    private void FindCollidersInScene()
    {
        // En cada cambio de escena, necesitamos encontrar los colliders correspondientes
        // Esta función debería implementarse según la estructura específica de tus escenas

        Debug.Log("Buscando colliders en la escena: " + currentSceneName);

        // Ejemplo: buscar colliders por etiquetas (tags)
        GameObject norteObj = GameObject.FindGameObjectWithTag("ColliderNorte");
        if (norteObj) norteCollider = norteObj.transform;

        GameObject surObj = GameObject.FindGameObjectWithTag("ColliderSur");
        if (surObj) surCollider = surObj.transform;

        GameObject esteObj = GameObject.FindGameObjectWithTag("ColliderEste");
        if (esteObj) esteCollider = esteObj.transform;

        GameObject oesteObj = GameObject.FindGameObjectWithTag("ColliderOeste");
        if (oesteObj) oesteCollider = oesteObj.transform;

        // También podríamos buscar la cámara o cabeza del jugador si cambia entre escenas
        GameObject cameraObj = Camera.main?.gameObject;
        if (cameraObj) playerHead = cameraObj.transform;

        ValidarColliders();
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
        Log("INICIO_APLICACION", "");
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

        // Calcula el vector desde la cabeza del usuario hasta el objeto
        Vector3 viewVector = Vector3.Normalize(targetTransform.position - playerHead.position);

        // Calcula el producto escalar (dot product) para determinar la alineación
        float dotView = Vector3.Dot(playerHead.forward, viewVector);

        // Retorna true si el dot product es mayor que el umbral
        return dotView >= alignmentThreshold;
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

        logFilePath = Path.Combine(directory, $"log_anger_{timestamp}.txt");

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
        if (currentColliderName != "Ninguno")
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

        // Generar resumen
        GuardarResumen();
    }

    public void GenerarResumenFinal()
    {
        Debug.Log("Generando resumen final de telemetría...");

        // Guardar datos pendientes
        SaveLogBuffer();

        // Generar resumen
        GuardarResumen();
    }

    private void GuardarResumen()
    {
        try
        {
            // Calcular estadísticas de efectividad para cada minijuego
            float efectividadSmash = CalcularEfectividadSmash();
            float efectividadReaction = CalcularEfectividadReaction();
            float efectividadBalloon = CalcularEfectividadBalloon();

            // Calcular tiempo total de la experiencia
            float tiempoTotalExperiencia = CalcularTiempoTotalExperiencia();

            StringBuilder resumen = new StringBuilder();
            resumen.AppendLine("RESUMEN DE LA EXPERIENCIA");
            resumen.AppendLine($"Tiempo total de experiencia: {tiempoTotalExperiencia:F4} segundos");

            // Historial de escenas visitadas
            resumen.AppendLine("\nSECUENCIA DE ESCENAS:");
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

                resumen.AppendLine($"{i + 1}. {escena} - Inicio: {tiempo:F2} segundos, Duración: {duracionEscena:F2} segundos");
            }

            // Resumen de atención del usuario por escena
            resumen.AppendLine("\nATENCIÓN DEL USUARIO:");

            // Procesar cada escena que fue visitada (excepto Close si existe)
            foreach (string escena in sceneHistory.ToArray())
            {
                // Ignorar escena de cierre
                if (escena == "Close")
                    continue;

                if (colliderTotalLookTimesByScene.ContainsKey(escena))
                {
                    Dictionary<string, float> tiemposMirada = colliderTotalLookTimesByScene[escena];

                    // Calcular tiempos totales para esta escena
                    float tiempoTotalEscena = CalcularTiempoTotalEscena(escena);

                    resumen.AppendLine($"\nEscena: {escena}");
                    resumen.AppendLine($"Tiempo total en esta escena: {tiempoTotalEscena:F2} segundos");
                    resumen.AppendLine($"Tiempo mirando al norte: {tiemposMirada["Norte"]:F2} segundos");
                    resumen.AppendLine($"Tiempo mirando al sur: {tiemposMirada["Sur"]:F2} segundos");
                    resumen.AppendLine($"Tiempo mirando al este: {tiemposMirada["Este"]:F2} segundos");
                    resumen.AppendLine($"Tiempo mirando al oeste: {tiemposMirada["Oeste"]:F2} segundos");
                    resumen.AppendLine($"Tiempo sin mirar a ningún elemento: {tiemposMirada["Ninguno"]:F2} segundos");

                    // Calcular tiempo total registrado en miradas
                    float tiempoTotalRegistrado = tiemposMirada["Norte"] + tiemposMirada["Sur"] +
                                                tiemposMirada["Este"] + tiemposMirada["Oeste"] +
                                                tiemposMirada["Ninguno"];

                    // Calcular porcentajes solo si hay tiempo total
                    if (tiempoTotalEscena > 0)
                    {
                        resumen.AppendLine("Porcentajes de atención:");
                        resumen.AppendLine($"Norte: {(tiemposMirada["Norte"] / tiempoTotalEscena * 100):F2}%");
                        resumen.AppendLine($"Sur: {(tiemposMirada["Sur"] / tiempoTotalEscena * 100):F2}%");
                        resumen.AppendLine($"Este: {(tiemposMirada["Este"] / tiempoTotalEscena * 100):F2}%");
                        resumen.AppendLine($"Oeste: {(tiemposMirada["Oeste"] / tiempoTotalEscena * 100):F2}%");
                        resumen.AppendLine($"Sin foco específico: {(tiemposMirada["Ninguno"] / tiempoTotalEscena * 100):F2}%");
                    }

                    // Advertencia si hay discrepancia significativa en los tiempos registrados
                    if (tiempoTotalRegistrado > 0 && Math.Abs(tiempoTotalRegistrado - tiempoTotalEscena) > 5.0f)
                    {
                        resumen.AppendLine($"Nota: Hay una diferencia de {(tiempoTotalEscena - tiempoTotalRegistrado):F2} segundos " +
                                          $"entre el tiempo total de la escena y el tiempo total registrado en miradas.");
                    }
                }
            }

            // Resumen de minijuegos
            resumen.AppendLine("\nRESUMEN DE MINIJUEGOS:");

            // Minijuego Smash
            resumen.AppendLine("\nMINIJUEGO SMASH (Golpear objetos):");
            resumen.AppendLine($"Objetos golpeados: {smashObjectsHit}");
            if (smashObjectsHit > 0)
            {
                float tiempoTotalSmash = CalcularTiempoTotalEscena(escenaSmashGame);
                resumen.AppendLine($"Duración del minijuego: {tiempoTotalSmash:F2} segundos");
                resumen.AppendLine($"Efectividad media: {efectividadSmash:F2} objetos por segundo");
                resumen.AppendLine($"Tiempo medio entre golpes: {(1 / efectividadSmash):F2} segundos por objeto");
                // Añadir estadística en formato más legible
                resumen.AppendLine($"Ritmo: 1 objeto golpeado cada {(1 / efectividadSmash):F2} segundos");

                // Análisis de consistencia (opcional)
                if (smashHitTimes.Count > 1)
                {
                    float tiempoMasRapido = float.MaxValue;
                    float tiempoMasLento = 0f;

                    for (int i = 1; i < smashHitTimes.Count; i++)
                    {
                        float tiempoEntreGolpes = smashHitTimes[i] - smashHitTimes[i - 1];
                        tiempoMasRapido = Mathf.Min(tiempoMasRapido, tiempoEntreGolpes);
                        tiempoMasLento = Mathf.Max(tiempoMasLento, tiempoEntreGolpes);
                    }

                    resumen.AppendLine($"Golpe más rápido: {tiempoMasRapido:F2} segundos");
                    resumen.AppendLine($"Golpe más lento: {tiempoMasLento:F2} segundos");
                }
            }

            // Minijuego Reaction
            resumen.AppendLine("\nMINIJUEGO REACTION (Botones de reacción):");
            resumen.AppendLine($"Botones presionados: {reactionButtonsPressed}");
            if (reactionButtonsPressed > 0)
            {
                float tiempoTotalReaction = CalcularTiempoTotalEscena(escenaReactionGame);
                resumen.AppendLine($"Duración del minijuego: {tiempoTotalReaction:F2} segundos");
                resumen.AppendLine($"Efectividad media: {efectividadReaction:F2} botones por segundo");
                resumen.AppendLine($"Tiempo medio entre pulsaciones: {(1 / efectividadReaction):F2} segundos por botón");
                // Añadir estadística en formato más legible
                resumen.AppendLine($"Ritmo: 1 botón presionado cada {(1 / efectividadReaction):F2} segundos");

                // Análisis de consistencia (opcional)
                if (reactionPressTimes.Count > 1)
                {
                    float tiempoMasRapido = float.MaxValue;
                    float tiempoMasLento = 0f;

                    for (int i = 1; i < reactionPressTimes.Count; i++)
                    {
                        float tiempoEntrePulsaciones = reactionPressTimes[i] - reactionPressTimes[i - 1];
                        tiempoMasRapido = Mathf.Min(tiempoMasRapido, tiempoEntrePulsaciones);
                        tiempoMasLento = Mathf.Max(tiempoMasLento, tiempoEntrePulsaciones);
                    }

                    resumen.AppendLine($"Reacción más rápida: {tiempoMasRapido:F2} segundos");
                    resumen.AppendLine($"Reacción más lenta: {tiempoMasLento:F2} segundos");
                }
            }

            // Minijuego Balloon
            resumen.AppendLine("\nMINIJUEGO BALLOON (Reventar globos):");
            resumen.AppendLine($"Globos reventados: {balloonsPopped}");
            if (balloonsPopped > 0)
            {
                float tiempoTotalBalloon = CalcularTiempoTotalEscena(escenaBalloonGame);
                resumen.AppendLine($"Duración del minijuego: {tiempoTotalBalloon:F2} segundos");
                resumen.AppendLine($"Efectividad media: {efectividadBalloon:F2} globos por segundo");
                resumen.AppendLine($"Tiempo medio entre globos: {(1 / efectividadBalloon):F2} segundos por globo");
                // Añadir estadística en formato más legible
                resumen.AppendLine($"Ritmo: 1 globo reventado cada {(1 / efectividadBalloon):F2} segundos");

                // Análisis de consistencia (opcional)
                if (balloonPopTimes.Count > 1)
                {
                    float tiempoMasRapido = float.MaxValue;
                    float tiempoMasLento = 0f;

                    for (int i = 1; i < balloonPopTimes.Count; i++)
                    {
                        float tiempoEntreGlobos = balloonPopTimes[i] - balloonPopTimes[i - 1];
                        tiempoMasRapido = Mathf.Min(tiempoMasRapido, tiempoEntreGlobos);
                        tiempoMasLento = Mathf.Max(tiempoMasLento, tiempoEntreGlobos);
                    }

                    resumen.AppendLine($"Reventado más rápido: {tiempoMasRapido:F2} segundos");
                    resumen.AppendLine($"Reventado más lento: {tiempoMasLento:F2} segundos");
                }
            }

            // Resumen general de la experiencia
            resumen.AppendLine("\nRESUMEN GENERAL DE EFECTIVIDAD:");
            float tiempoTotalMinijuegos = CalcularTiempoTotalEscena(escenaSmashGame) +
                                         CalcularTiempoTotalEscena(escenaReactionGame) +
                                         CalcularTiempoTotalEscena(escenaBalloonGame);

            int totalAcciones = smashObjectsHit + reactionButtonsPressed + balloonsPopped;

            if (tiempoTotalMinijuegos > 0 && totalAcciones > 0)
            {
                float efectividadGeneral = totalAcciones / tiempoTotalMinijuegos;
                resumen.AppendLine($"Total acciones en minijuegos: {totalAcciones}");
                resumen.AppendLine($"Tiempo total en minijuegos: {tiempoTotalMinijuegos:F2} segundos");
                resumen.AppendLine($"Efectividad general: {efectividadGeneral:F2} acciones por segundo");
                resumen.AppendLine($"Ritmo general: 1 acción cada {(1 / efectividadGeneral):F2} segundos");
            }

            // Guardar resumen
            Log("RESUMEN", resumen.ToString());
            SaveLogBuffer();

            // Guardar archivo separado de resumen para consulta más fácil
            string summaryPath = Path.Combine(
                Application.persistentDataPath,
                "Logs",
                $"resumen_anger_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt");

            File.WriteAllText(summaryPath, resumen.ToString());
            Debug.Log($"Resumen guardado en: {summaryPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al generar resumen: {e.Message}");
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
                    finTiempo = Time.time - startTime;
                }

                tiempoTotal += (finTiempo - inicioTiempo);
            }
        }

        return tiempoTotal;
    }

    private float CalcularEfectividadSmash()
    {
        // Si no hay objetos golpeados, devolver 0
        if (smashObjectsHit == 0) return 0f;

        // Calcular el tiempo total del minijuego Smash
        float tiempoTotalSmash = 0f;
        for (int i = 0; i < sceneHistory.Count; i++)
        {
            if (sceneHistory[i] == escenaSmashGame)
            {
                float finTiempo;
                if (i < sceneHistory.Count - 1)
                {
                    finTiempo = sceneTransitionTimes[i + 1];
                }
                else
                {
                    finTiempo = Time.time - startTime;
                }

                tiempoTotalSmash += (finTiempo - sceneTransitionTimes[i]);
            }
        }

        // Si el tiempo es 0 (no se jugó), devolver 0
        if (tiempoTotalSmash <= 0f) return 0f;

        // Calcular objetos por segundo
        return smashObjectsHit / tiempoTotalSmash;
    }

    private float CalcularEfectividadReaction()
    {
        // Si no hay botones presionados, devolver 0
        if (reactionButtonsPressed == 0) return 0f;

        // Calcular el tiempo total del minijuego Reaction
        float tiempoTotalReaction = 0f;
        for (int i = 0; i < sceneHistory.Count; i++)
        {
            if (sceneHistory[i] == escenaReactionGame)
            {
                float finTiempo;
                if (i < sceneHistory.Count - 1)
                {
                    finTiempo = sceneTransitionTimes[i + 1];
                }
                else
                {
                    finTiempo = Time.time - startTime;
                }

                tiempoTotalReaction += (finTiempo - sceneTransitionTimes[i]);
            }
        }

        // Si el tiempo es 0 (no se jugó), devolver 0
        if (tiempoTotalReaction <= 0f) return 0f;

        // Calcular botones por segundo
        return reactionButtonsPressed / tiempoTotalReaction;
    }

    private float CalcularEfectividadBalloon()
    {
        // Si no hay globos reventados, devolver 0
        if (balloonsPopped == 0) return 0f;

        // Calcular el tiempo total del minijuego Balloon
        float tiempoTotalBalloon = 0f;
        for (int i = 0; i < sceneHistory.Count; i++)
        {
            if (sceneHistory[i] == escenaBalloonGame)
            {
                float finTiempo;
                if (i < sceneHistory.Count - 1)
                {
                    finTiempo = sceneTransitionTimes[i + 1];
                }
                else
                {
                    finTiempo = Time.time - startTime;
                }

                tiempoTotalBalloon += (finTiempo - sceneTransitionTimes[i]);
            }
        }

        // Si el tiempo es 0 (no se jugó), devolver 0
        if (tiempoTotalBalloon <= 0f) return 0f;

        // Calcular globos por segundo
        return balloonsPopped / tiempoTotalBalloon;
    }

    // ==== Métodos públicos para registrar eventos desde otros scripts ====

    // Eventos del minijuego Smash
    public void RegistrarObjetoGolpeado(string nombreObjeto)
    {
        try
        {
            if (!isLogReady) return;

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

            reactionButtonsPressed++;
            float tiempoActual = Time.time - startTime;
            reactionPressTimes.Add(tiempoActual);

            Log("OPRIME_BOTON", $"Número: {reactionButtonsPressed}, Tiempo: {tiempoActual:F2}");
            SaveLogBuffer(); // Guardar inmediatamente
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

            balloonsPopped++;
            float tiempoActual = Time.time - startTime;
            balloonPopTimes.Add(tiempoActual);

            Log("GLOBO_GOLPEADO", $"Número: {balloonsPopped}, Tiempo: {tiempoActual:F2}");
            SaveLogBuffer(); // Guardar inmediatamente
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
}