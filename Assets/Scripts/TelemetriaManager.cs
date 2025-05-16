using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class TelemetriaManager : MonoBehaviour
{
    private static TelemetriaManager _instance;
    public static TelemetriaManager Instance { get { return _instance; } }

    // Configuración de seguimiento de mirada
    [Header("Configuración de Seguimiento de Mirada")]
    [Tooltip("La posición entre las cámaras izquierda y derecha (cabeza del usuario)")]
    public Transform playerHead;

    [Tooltip("Umbral de alineación para considerar que el usuario está mirando al objeto")]
    [Range(0.7f, 1.0f)]
    public float alignmentThreshold = 0.9f;

    [Tooltip("Tiempo en segundos que el usuario debe mirar antes de considerar foco de atención")]
    public float focusDelay = 0.5f;

    [Tooltip("Intervalo en segundos para registrar la mirada del usuario")]
    public float gazeLogInterval = 3.0f;

    [Tooltip("Collider del árbol")]
    public Transform arbolCollider;

    [Tooltip("Collider de la caja")]
    public Transform cajaCollider;

    [Tooltip("Collider de la fogata")]
    public Transform fogataCollider;

    [Tooltip("Collider de la respiración")]
    public Transform respiracionCollider;

    // Variables para ID único de experiencia
    [Header("Identificación de Experiencia")]
    [Tooltip("ID del equipo (se genera automáticamente si está vacío)")]
    [SerializeField] private string equipoID = "";
    [Tooltip("Archivo para almacenar el contador de experiencias")]
    [SerializeField] private string contadorFilePath = "experiencia_contador.txt";

    // Variables para identificación
    private string experienciaID = "";
    private string codigoUsuario = ""; // Valor por defecto vacío
    private string experienciaBasica = ""; // Sin código de usuario

    private string logFilePath;
    private StringBuilder logBuffer = new StringBuilder();
    private float startTime;
    private bool isLogFileCreated = false;
    private bool isLogReady = false; // Para evitar logs prematuros
    private float lastSaveTime = 0f;
    private float saveInterval = 5f; // Guardar cada 5 segundos

    // Variables para seguimiento de mirada
    private string currentColliderName = "Ninguno";
    private float lastGazeLogTime = 0f;
    private float currentColliderStartTime = 0f;

    // Información del dispositivo
    private string deviceName = "Desconocido";

    // Diccionario para almacenar tiempos totales de mirada
    private Dictionary<string, float> colliderTotalLookTimes = new Dictionary<string, float>() {
        {"Arbol", 0f},
        {"Caja", 0f},
        {"Fogata", 0f},
        {"Respiracion", 0f},
        {"Ninguno", 0f}
    };

    // Datos de la caja
    private bool cajaAbierta = false;
    private float tiempoAperturaCaja = 0f;

    // Datos de la fogata
    private int piedrasColocadas = 0;
    private float tiempoPrimeraPiedra = 0f;
    private float tiempoTodasPiedras = 0f;
    private int vecesEncendida = 0;
    private int vecesApagada = 0;

    // Datos de las hojas
    private int hojasAgarradas = 0;
    private float tiempoPrimeraHoja = 0f;
    private List<float> tiemposAgarreHojas = new List<float>();
    private List<float> tiemposSoltarHojas = new List<float>();
    private HashSet<int> hojasInstanciasContadas = new HashSet<int>();

    // Datos para botones de fin
    private bool primerFinActivado = false;
    private float tiempoPrimerFin = 0f;
    private float tiempoSegundoFin = 0f;
    private float tiempoEntreFines = 0f;

    // Datos para botones de audio
    private bool audioHojasActivado = false;
    private bool audioFogataActivado = false;
    private bool audioRespiracionActivado = false;
    private bool audioArbolActivado = false;

    private float tiempoPrimerAudio = 0f;
    private string nombrePrimerAudio = "";

    private int vecesAudioHojas = 0;
    private int vecesAudioFogata = 0;
    private int vecesAudioRespiracion = 0;
    private int vecesAudioArbol = 0;

    private List<float> tiemposAudioHojas = new List<float>();
    private List<float> tiemposAudioFogata = new List<float>();
    private List<float> tiemposAudioRespiracion = new List<float>();
    private List<float> tiemposAudioArbol = new List<float>();

    private bool resumenGuardadoConCodigo = false;


    // Referencia al EncryptionManager
    private EncryptionManager encryptionManager;

    private void Awake()
    {
        // Singleton pattern
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

        // Generar el ID básico de la experiencia (sin código de usuario)
        experienciaBasica = $"{equipoID}{experienciaID}";

        // Obtener el nombre del dispositivo
        ObtenerDeviceName();

        // Crear el archivo de log en memoria (ahora solo crearemos un buffer en memoria)
        PreparaLog();

        // Buscar o crear el EncryptionManager
        encryptionManager = FindObjectOfType<EncryptionManager>();
        if (encryptionManager == null)
        {
            GameObject encryptionObj = new GameObject("EncryptionManager");
            encryptionManager = encryptionObj.AddComponent<EncryptionManager>();
            DontDestroyOnLoad(encryptionObj);
            Debug.Log("EncryptionManager creado automáticamente");
        }

        // Debug info
        Debug.Log("TelemetriaManager inicializado");
        Debug.Log($"ID básico de Experiencia: {experienciaBasica}");

        // Verificar que tengamos los colliders necesarios
        ValidarColliders();
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
            string idFilePath = Path.Combine(Application.persistentDataPath, "equipo_id.txt");

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

            // Registrar el nombre del dispositivo para telemetría
            RegistrarEvento("DEVICE_NAME", deviceName);
            RegistrarEvento("EXPERIENCIA_ID", experienciaBasica);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al obtener nombre del dispositivo: {e.Message}");
            deviceName = "Error_" + SystemInfo.deviceType.ToString();
        }
    }

    void Start()
    {
        startTime = Time.time;
        lastGazeLogTime = Time.time; // Inicializar el tiempo del último registro de mirada
        Log("INICIO_APLICACION", $"Dispositivo: {deviceName}, ID Experiencia: {experienciaBasica}");
        isLogReady = true; // Marcar que estamos listos para registrar eventos
        Debug.Log("TelemetriaManager: isLogReady = true");

        // Iniciar la corrutina para verificar la mirada periódicamente
        StartCoroutine(VerificarMiradaPeriodicamente());
    }

    void Update()
    {
        // Verificar continuamente hacia dónde está mirando el usuario
        VerificarMirada();
    }

    private void ValidarColliders()
    {
        if (playerHead == null)
        {
            Debug.LogError("TelemetriaManager: No se ha asignado la cabeza del jugador (playerHead)");
        }

        if (arbolCollider == null)
        {
            Debug.LogWarning("TelemetriaManager: No se ha asignado el collider del árbol");
        }

        if (cajaCollider == null)
        {
            Debug.LogWarning("TelemetriaManager: No se ha asignado el collider de la caja");
        }

        if (fogataCollider == null)
        {
            Debug.LogWarning("TelemetriaManager: No se ha asignado el collider de la fogata");
        }

        if (respiracionCollider == null)
        {
            Debug.LogWarning("TelemetriaManager: No se ha asignado el collider de la respiración");
        }
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
                    Log($"CONTINUE_COLLIDER_{currentColliderName.ToUpper()}", $"Tiempo acumulado: {colliderTotalLookTimes[currentColliderName]:F2} segundos");
                }
                else
                {
                    Log("NO_COLLIDER_VIEW", "El usuario no está mirando a ningún collider");
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
                colliderTotalLookTimes[currentColliderName] += tiempoMiradaAnterior;
                Log($"FIN_COLLIDER_{currentColliderName.ToUpper()}", $"Duración: {tiempoMiradaAnterior:F2} segundos, Acumulado: {colliderTotalLookTimes[currentColliderName]:F2} segundos");
            }

            // Actualizar al nuevo collider
            currentColliderName = colliderActual;
            currentColliderStartTime = Time.time;

            // Registrar el nuevo collider
            if (currentColliderName != "Ninguno")
            {
                Log($"INICIO_COLLIDER_{currentColliderName.ToUpper()}", $"Tiempo desde inicio: {Time.time - startTime:F2} segundos");
            }
        }
    }

    private string ObtenerColliderEnFoco()
    {
        // Verifica si está mirando alguno de los colliders
        if (EstaLookingAt(arbolCollider)) return "Arbol";
        if (EstaLookingAt(cajaCollider)) return "Caja";
        if (EstaLookingAt(fogataCollider)) return "Fogata";
        if (EstaLookingAt(respiracionCollider)) return "Respiracion";

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

    private void PreparaLog()
    {
        // Ya no creamos un archivo, solo preparamos el buffer en memoria
        isLogFileCreated = true;
        Debug.Log("Buffer de telemetría en memoria preparado");
    }

    private void Log(string evento, string datos)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            float tiempoTranscurrido = Time.time - startTime;

            string logEntry = $"{timestamp},{tiempoTranscurrido},{evento},{datos}\n";
            logBuffer.Append(logEntry);

            Debug.Log($"[TELEMETRÍA] {evento}: {datos}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en Log: {e.Message}");
        }
    }

    

    private void OnApplicationQuit()
    {
        // Registrar el tiempo final del último collider que se estaba mirando
        if (currentColliderName != "Ninguno")
        {
            float tiempoMiradaFinal = Time.time - currentColliderStartTime;
            colliderTotalLookTimes[currentColliderName] += tiempoMiradaFinal;
            Log($"FIN_COLLIDER_{currentColliderName.ToUpper()}", $"Duración final: {tiempoMiradaFinal:F2} segundos, Total acumulado: {colliderTotalLookTimes[currentColliderName]:F2} segundos");
        }

        // Si el primer fin no fue activado, registrarlo aquí como dato automático
        if (!primerFinActivado)
        {
            RegistrarPrimerFin();
        }

        // Si el segundo fin no fue activado, registrarlo aquí como dato automático
        if (tiempoSegundoFin == 0f)
        {
            RegistrarSegundoFin();
        }

        // Asegurarse de guardar todos los datos antes de cerrar
        Log("FIN_APLICACION", $"Tiempo total: {Time.time - startTime}");

        try
        {
            // Solo generar el resumen final si aún no hemos guardado uno con código de usuario
            if (!resumenGuardadoConCodigo)
            {
                // Generar el archivo JSON final con el resumen
                GuardarResumen();
                Debug.Log("Resumen JSON generado en OnApplicationQuit");
            }
            else
            {
                Debug.Log("No se generó resumen en OnApplicationQuit porque ya existía uno con código de usuario");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al generar el resumen en OnApplicationQuit: {e.Message}");
        }
    }

    public string GenerarJSONResumen()
    {
        try
        {
            // Añadir método auxiliar para formatear números con punto decimal
            System.Globalization.CultureInfo invCulture = System.Globalization.CultureInfo.InvariantCulture;

            // Calcular promedio de tiempo entre agarrar y soltar hojas
            float tiempoPromedioInteraccionHojas = 0f;
            int contadorTiempos = 0;

            for (int i = 0; i < tiemposAgarreHojas.Count && i < tiemposSoltarHojas.Count; i++)
            {
                tiempoPromedioInteraccionHojas += tiemposSoltarHojas[i] - tiemposAgarreHojas[i];
                contadorTiempos++;
            }

            if (contadorTiempos > 0)
            {
                tiempoPromedioInteraccionHojas /= contadorTiempos;
            }

            // Crear un objeto JSON para almacenar los datos del resumen
            System.Text.StringBuilder jsonBuilder = new System.Text.StringBuilder();
            jsonBuilder.AppendLine("{");

            // Añadir el código del usuario al inicio del JSON
            if (!string.IsNullOrEmpty(codigoUsuario))
            {
                // Cambio: Asegurarnos de que el código esté dentro de comillas si es un string
                // Esto es importante para que sea un JSON válido
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
            jsonBuilder.AppendLine("  \"tiempoTotal\": " + (Time.time - startTime).ToString("F2", invCulture) + ",");

            // Atención del usuario
            jsonBuilder.AppendLine("  \"atencionUsuario\": {");
            jsonBuilder.AppendLine("    \"tiempoArbol\": " + colliderTotalLookTimes["Arbol"].ToString("F2", invCulture) + ",");
            jsonBuilder.AppendLine("    \"tiempoCaja\": " + colliderTotalLookTimes["Caja"].ToString("F2", invCulture) + ",");
            jsonBuilder.AppendLine("    \"tiempoFogata\": " + colliderTotalLookTimes["Fogata"].ToString("F2", invCulture) + ",");
            jsonBuilder.AppendLine("    \"tiempoRespiracion\": " + colliderTotalLookTimes["Respiracion"].ToString("F2", invCulture) + ",");
            jsonBuilder.AppendLine("    \"tiempoSinFoco\": " + colliderTotalLookTimes["Ninguno"].ToString("F2", invCulture) + ",");

            // Porcentajes de atención
            float tiempoTotal = Time.time - startTime;
            if (tiempoTotal > 0)
            {
                jsonBuilder.AppendLine("    \"porcentajes\": {");
                jsonBuilder.AppendLine("      \"arbol\": " + (colliderTotalLookTimes["Arbol"] / tiempoTotal * 100).ToString("F2", invCulture) + ",");
                jsonBuilder.AppendLine("      \"caja\": " + (colliderTotalLookTimes["Caja"] / tiempoTotal * 100).ToString("F2", invCulture) + ",");
                jsonBuilder.AppendLine("      \"fogata\": " + (colliderTotalLookTimes["Fogata"] / tiempoTotal * 100).ToString("F2", invCulture) + ",");
                jsonBuilder.AppendLine("      \"respiracion\": " + (colliderTotalLookTimes["Respiracion"] / tiempoTotal * 100).ToString("F2", invCulture) + ",");
                jsonBuilder.AppendLine("      \"sinFoco\": " + (colliderTotalLookTimes["Ninguno"] / tiempoTotal * 100).ToString("F2", invCulture));
                jsonBuilder.AppendLine("    }");
            }
            else
            {
                jsonBuilder.AppendLine("    \"porcentajes\": {}");
            }
            jsonBuilder.AppendLine("  },");

            // Información de la caja
            jsonBuilder.AppendLine("  \"caja\": {");
            jsonBuilder.AppendLine("    \"abierta\": " + cajaAbierta.ToString().ToLower() +
                (cajaAbierta ? ",\n    \"tiempoApertura\": " + tiempoAperturaCaja.ToString("F2", invCulture) : ""));
            jsonBuilder.AppendLine("  },");

            // Información de la fogata
            jsonBuilder.AppendLine("  \"fogata\": {");
            jsonBuilder.AppendLine("    \"piedrasColocadas\": " + piedrasColocadas + ",");
            if (piedrasColocadas > 0)
            {
                jsonBuilder.AppendLine("    \"tiempoPrimeraPiedra\": " + tiempoPrimeraPiedra.ToString("F2", invCulture) + ",");
            }
            if (piedrasColocadas == 4)
            {
                jsonBuilder.AppendLine("    \"tiempoTodasPiedras\": " + tiempoTodasPiedras.ToString("F2", invCulture) + ",");
            }
            jsonBuilder.AppendLine("    \"vecesEncendida\": " + vecesEncendida + ",");
            jsonBuilder.AppendLine("    \"vecesApagada\": " + vecesApagada);
            jsonBuilder.AppendLine("  },");

            // Información de las hojas
            jsonBuilder.AppendLine("  \"hojas\": {");
            jsonBuilder.AppendLine("    \"manipuladas\": " + hojasAgarradas);
            if (hojasAgarradas > 0)
            {
                jsonBuilder.AppendLine("    ,\"tiempoPrimeraHoja\": " + tiempoPrimeraHoja.ToString("F2", invCulture) + ",");
                jsonBuilder.AppendLine("    \"tiempoPromedioInteraccion\": " + tiempoPromedioInteraccionHojas.ToString("F2", invCulture));
            }
            jsonBuilder.AppendLine("  },");

            // Información de cierre de experiencia
            jsonBuilder.AppendLine("  \"cierreExperiencia\": {");
            if (primerFinActivado)
            {
                jsonBuilder.AppendLine("    \"tiempoPrimerFin\": " + tiempoPrimerFin.ToString("F2", invCulture));

                if (tiempoSegundoFin > 0f)
                {
                    jsonBuilder.AppendLine("    ,\"tiempoSegundoFin\": " + tiempoSegundoFin.ToString("F2", invCulture) + ",");
                    jsonBuilder.AppendLine("    \"tiempoEntreFines\": " + tiempoEntreFines.ToString("F2", invCulture));
                }
            }
            else
            {
                jsonBuilder.AppendLine("    \"notificacion\": \"Ningún botón de fin fue activado (la aplicación se cerró de otra manera)\"");
            }
            jsonBuilder.AppendLine("  },");

            // Información de botones de audio
            jsonBuilder.AppendLine("  \"botonesAudio\": {");

            if (audioHojasActivado || audioFogataActivado || audioRespiracionActivado || audioArbolActivado)
            {
                jsonBuilder.AppendLine("    \"primerAudio\": {");
                jsonBuilder.AppendLine("      \"nombre\": \"" + nombrePrimerAudio + "\",");
                jsonBuilder.AppendLine("      \"tiempo\": " + tiempoPrimerAudio.ToString("F2", invCulture));
                jsonBuilder.AppendLine("    },");

                // Audio Hojas
                jsonBuilder.AppendLine("    \"audioHojas\": {");
                jsonBuilder.AppendLine("      \"vecesActivado\": " + vecesAudioHojas);
                if (vecesAudioHojas > 0)
                {
                    jsonBuilder.Append("      ,\"tiemposActivacion\": [");
                    for (int i = 0; i < tiemposAudioHojas.Count; i++)
                    {
                        jsonBuilder.Append(tiemposAudioHojas[i].ToString("F2", invCulture));
                        if (i < tiemposAudioHojas.Count - 1)
                            jsonBuilder.Append(", ");
                    }
                    jsonBuilder.AppendLine("]");
                }
                jsonBuilder.AppendLine("    },");

                // Audio Fogata
                jsonBuilder.AppendLine("    \"audioFogata\": {");
                jsonBuilder.AppendLine("      \"vecesActivado\": " + vecesAudioFogata);
                if (vecesAudioFogata > 0)
                {
                    jsonBuilder.Append("      ,\"tiemposActivacion\": [");
                    for (int i = 0; i < tiemposAudioFogata.Count; i++)
                    {
                        jsonBuilder.Append(tiemposAudioFogata[i].ToString("F2", invCulture));
                        if (i < tiemposAudioFogata.Count - 1)
                            jsonBuilder.Append(", ");
                    }
                    jsonBuilder.AppendLine("]");
                }
                jsonBuilder.AppendLine("    },");

                // Audio Respiración
                jsonBuilder.AppendLine("    \"audioRespiracion\": {");
                jsonBuilder.AppendLine("      \"vecesActivado\": " + vecesAudioRespiracion);
                if (vecesAudioRespiracion > 0)
                {
                    jsonBuilder.Append("      ,\"tiemposActivacion\": [");
                    for (int i = 0; i < tiemposAudioRespiracion.Count; i++)
                    {
                        jsonBuilder.Append(tiemposAudioRespiracion[i].ToString("F2", invCulture));
                        if (i < tiemposAudioRespiracion.Count - 1)
                            jsonBuilder.Append(", ");
                    }
                    jsonBuilder.AppendLine("]");
                }
                jsonBuilder.AppendLine("    },");

                // Audio Árbol
                jsonBuilder.AppendLine("    \"audioArbol\": {");
                jsonBuilder.AppendLine("      \"vecesActivado\": " + vecesAudioArbol);
                if (vecesAudioArbol > 0)
                {
                    jsonBuilder.Append("      ,\"tiemposActivacion\": [");
                    for (int i = 0; i < tiemposAudioArbol.Count; i++)
                    {
                        jsonBuilder.Append(tiemposAudioArbol[i].ToString("F2", invCulture));
                        if (i < tiemposAudioArbol.Count - 1)
                            jsonBuilder.Append(", ");
                    }
                    jsonBuilder.AppendLine("]");
                }
                jsonBuilder.AppendLine("    },");

                // Secuencia de activación de audios
                jsonBuilder.AppendLine("    \"secuenciaAudio\": [");
                List<KeyValuePair<string, float>> secuencia = new List<KeyValuePair<string, float>>();

                if (audioHojasActivado && tiemposAudioHojas.Count > 0)
                    secuencia.Add(new KeyValuePair<string, float>("Hojas", tiemposAudioHojas[0]));

                if (audioFogataActivado && tiemposAudioFogata.Count > 0)
                    secuencia.Add(new KeyValuePair<string, float>("Fogata", tiemposAudioFogata[0]));

                if (audioRespiracionActivado && tiemposAudioRespiracion.Count > 0)
                    secuencia.Add(new KeyValuePair<string, float>("Respiración", tiemposAudioRespiracion[0]));

                if (audioArbolActivado && tiemposAudioArbol.Count > 0)
                    secuencia.Add(new KeyValuePair<string, float>("Árbol", tiemposAudioArbol[0]));

                secuencia.Sort((x, y) => x.Value.CompareTo(y.Value));

                for (int i = 0; i < secuencia.Count; i++)
                {
                    jsonBuilder.AppendLine("      {");
                    jsonBuilder.AppendLine("        \"posicion\": " + (i + 1) + ",");
                    jsonBuilder.AppendLine("        \"audio\": \"" + secuencia[i].Key + "\",");
                    jsonBuilder.AppendLine("        \"tiempo\": " + secuencia[i].Value.ToString("F2", invCulture));
                    jsonBuilder.Append("      }");
                    if (i < secuencia.Count - 1)
                        jsonBuilder.AppendLine(",");
                    else
                        jsonBuilder.AppendLine("");
                }
                jsonBuilder.AppendLine("    ]");
            }
            else
            {
                jsonBuilder.AppendLine("    \"notificacion\": \"No se activó ningún audio de instrucciones\"");
            }
            jsonBuilder.AppendLine("  }");

            // Cerrar el objeto JSON
            jsonBuilder.AppendLine("}");

            // Devolver el JSON como cadena
            return jsonBuilder.ToString();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al generar JSON: {e.Message}\n{e.StackTrace}");

            // Devolver un JSON de error básico pero válido
            return "{ \"error\": \"Error generando datos de telemetría\", \"mensaje\": \"" + e.Message.Replace("\"", "'") + "\" }";
        }
    }

    private void GuardarResumen()
    {
        try
        {
            // Generar el JSON
            string jsonData = GenerarJSONResumen();

            // Asegurarse de que el directorio exista
            string directory = Path.Combine(Application.persistentDataPath, "Logs");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log($"Creando directorio para resumen: {directory}");
            }

            // Generar nombres de archivos
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string encryptedPath = Path.Combine(directory, $"resumen_{equipoID}_{timestamp}.json");

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

            // Si la encriptación falló o no hay EncryptionManager, guardar sin encriptar
            if (!encriptacionExitosa)
            {
                string plainPath = Path.Combine(directory, $"plain_resumen_{equipoID}_{timestamp}.json");
                File.WriteAllText(plainPath, jsonData);
                Debug.Log($"Resumen sin encriptar guardado en: {plainPath}");
                Log("RESUMEN_GUARDADO", $"Sin encriptar, Ruta: {plainPath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al guardar resumen: {e.Message}\n{e.StackTrace}");
            Log("ERROR_GUARDAR_RESUMEN", e.Message);

            // Última opción: intentar guardar en una ubicación de emergencia
            try
            {
                string emergencyPath = Path.Combine(
                    Application.persistentDataPath,
                    $"emergency_resumen_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.json");

                File.WriteAllText(emergencyPath, "{ \"error\": \"Error guardando resumen\" }");
                Debug.LogWarning($"Se intentó guardar un resumen de emergencia en: {emergencyPath}");
            }
            catch { }
        }
    }

    public void GenerarYGuardarResumen()
    {
        try
        {
            // Si ya guardamos un resumen con código, no lo volvemos a hacer
            if (resumenGuardadoConCodigo && !string.IsNullOrEmpty(codigoUsuario))
            {
                Debug.Log("No se generará un nuevo resumen porque ya existe uno con el código de usuario.");
                return;
            }

            GuardarResumen();

            // Si tenemos código de usuario, marcar que ya se guardó
            if (!string.IsNullOrEmpty(codigoUsuario))
            {
                resumenGuardadoConCodigo = true;
                Debug.Log($"Resumen JSON generado con código de usuario: {codigoUsuario}");
            }
            else
            {
                Debug.Log("Resumen JSON generado sin código de usuario (código no registrado aún)");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al generar el resumen manualmente: {e.Message}");
        }
    }

    /// <summary>
    /// Registra el código del usuario y actualiza el ID de la experiencia
    /// </summary>
    /// <param name="codigo">Código del usuario</param>
    public void RegistrarCodigoUsuario(string codigo)
    {
        try
        {
            // Si el código es válido, registrarlo
            if (!string.IsNullOrEmpty(codigo))
            {
                codigoUsuario = codigo.Trim(); // Asegurarse de eliminar espacios en blanco

                // Registrar el evento
                Log("CODIGO_USUARIO", codigoUsuario);
                Log("CODIGO_USUARIO_GUARDADO", $"Código: {codigoUsuario}");

                Debug.Log($"Código de usuario registrado: {codigoUsuario}");

                // Resetear la bandera para que se genere un nuevo resumen con el código
                resumenGuardadoConCodigo = false;
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

    // ==== Métodos públicos para registrar eventos desde otros scripts ====

    // Eventos de la caja
    public void RegistrarAperturaCaja()
    {
        try
        {
            if (!cajaAbierta)
            {
                cajaAbierta = true;
                tiempoAperturaCaja = Time.time - startTime;
                Log("CAJA_ABIERTA", $"Tiempo: {tiempoAperturaCaja}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarAperturaCaja: {e.Message}");
        }
    }

    // Eventos de la fogata
    public void RegistrarPiedraColocada(int totalPiedras)
    {
        try
        {
            // No registrar nada hasta que el sistema esté listo
            if (!isLogReady) return;

            // Solo registrar cuando cambia el número de piedras
            if (totalPiedras != piedrasColocadas)
            {
                int piedrasAnteriores = piedrasColocadas;
                piedrasColocadas = totalPiedras;

                // Registrar el cambio
                Log("PIEDRAS_COLOCADAS", $"Cambio de {piedrasAnteriores} a {piedrasColocadas}");

                // Registro de eventos especiales
                if (piedrasColocadas == 1 && piedrasAnteriores == 0 && tiempoPrimeraPiedra == 0f)
                {
                    tiempoPrimeraPiedra = Time.time - startTime;
                    Log("PRIMERA_PIEDRA", $"Tiempo: {tiempoPrimeraPiedra}");
                }

                if (piedrasColocadas == 4 && piedrasAnteriores != 4 && tiempoTodasPiedras == 0f)
                {
                    tiempoTodasPiedras = Time.time - startTime - tiempoPrimeraPiedra;
                    Log("TODAS_PIEDRAS", $"Tiempo desde primera piedra: {tiempoTodasPiedras}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarPiedraColocada: {e.Message}");
        }
    }

    public void RegistrarFogataEncendida()
    {
        try
        {
            if (!isLogReady) return;

            vecesEncendida++;
            Log("FOGATA_ENCENDIDA", $"Veces: {vecesEncendida}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarFogataEncendida: {e.Message}");
        }
    }

    public void RegistrarFogataApagada()
    {
        try
        {
            if (!isLogReady) return;

            vecesApagada++;
            Log("FOGATA_APAGADA", $"Veces: {vecesApagada}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarFogataApagada: {e.Message}");
        }
    }

    // Eventos de las hojas
    public void RegistrarHojaAgarrada(int instanceID)
    {
        try
        {
            if (!isLogReady) return;

            // Verificar si esta instancia ya fue contada para evitar duplicados
            if (!hojasInstanciasContadas.Contains(instanceID))
            {
                hojasInstanciasContadas.Add(instanceID);
                hojasAgarradas++;

                float tiempoActual = Time.time - startTime;
                tiemposAgarreHojas.Add(tiempoActual);

                if (hojasAgarradas == 1)
                {
                    tiempoPrimeraHoja = tiempoActual;
                    Log("PRIMERA_HOJA", $"Tiempo: {tiempoPrimeraHoja}");
                }

                Log("HOJA_AGARRADA", $"Número: {hojasAgarradas}, InstanceID: {instanceID}, Tiempo: {tiempoActual}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarHojaAgarrada: {e.Message}");
        }
    }

    public void RegistrarHojaSoltada(int instanceID)
    {
        try
        {
            if (!isLogReady) return;

            float tiempoActual = Time.time - startTime;
            tiemposSoltarHojas.Add(tiempoActual);
            Log("HOJA_SOLTADA", $"InstanceID: {instanceID}, Tiempo: {tiempoActual}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarHojaSoltada: {e.Message}");
        }
    }

    // Eventos de los botones de fin
    public void RegistrarPrimerFin()
    {
        try
        {
            Debug.Log("TelemetriaManager: RegistrarPrimerFin() llamado");

            if (!primerFinActivado)
            {
                primerFinActivado = true;
                tiempoPrimerFin = Time.time - startTime;
                Log("PRIMER_FIN", $"Tiempo: {tiempoPrimerFin}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarPrimerFin: {e.Message}");
        }
    }

    public void RegistrarSegundoFin()
    {
        try
        {
            Debug.Log("TelemetriaManager: RegistrarSegundoFin() llamado");

            if (tiempoSegundoFin == 0f)  // Evitar registros duplicados
            {
                tiempoSegundoFin = Time.time - startTime;

                // Si el primer fin fue activado, calcular la diferencia
                if (primerFinActivado)
                {
                    tiempoEntreFines = tiempoSegundoFin - tiempoPrimerFin;
                    Log("SEGUNDO_FIN", $"Tiempo: {tiempoSegundoFin}, Tiempo desde primer fin: {tiempoEntreFines}");
                }
                else
                {
                    // Si no se registró el primer fin, registrarlo ahora
                    primerFinActivado = true;
                    tiempoPrimerFin = tiempoSegundoFin; // Mismo tiempo que el segundo
                    tiempoEntreFines = 0f;
                    Log("PRIMER_FIN", $"Tiempo: {tiempoPrimerFin} (Registrado automáticamente)");
                    Log("SEGUNDO_FIN", $"Tiempo: {tiempoSegundoFin}, AVISO: Registrado inmediatamente después del primer fin");
                }

            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarSegundoFin: {e.Message}");
        }
    }

    // Eventos de los botones de audio
    public void RegistrarAudioHojas()
    {
        try
        {
            Debug.Log("TelemetriaManager: RegistrarAudioHojas() llamado");

            float tiempoActual = Time.time - startTime;
            vecesAudioHojas++;
            tiemposAudioHojas.Add(tiempoActual);

            Log("AUDIO_HOJAS", $"Activación #{vecesAudioHojas}, Tiempo: {tiempoActual}");

            // Si es la primera vez que se activa este audio
            if (!audioHojasActivado)
            {
                audioHojasActivado = true;

                // Si este es el primer audio activado de todos
                if (string.IsNullOrEmpty(nombrePrimerAudio))
                {
                    nombrePrimerAudio = "Hojas";
                    tiempoPrimerAudio = tiempoActual;
                    Log("PRIMER_AUDIO", $"Audio Hojas, Tiempo: {tiempoActual}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarAudioHojas: {e.Message}");
        }
    }

    public void RegistrarAudioFogata()
    {
        try
        {
            Debug.Log("TelemetriaManager: RegistrarAudioFogata() llamado");

            float tiempoActual = Time.time - startTime;
            vecesAudioFogata++;
            tiemposAudioFogata.Add(tiempoActual);

            Log("AUDIO_FOGATA", $"Activación #{vecesAudioFogata}, Tiempo: {tiempoActual}");

            // Si es la primera vez que se activa este audio
            if (!audioFogataActivado)
            {
                audioFogataActivado = true;

                // Si este es el primer audio activado de todos
                if (string.IsNullOrEmpty(nombrePrimerAudio))
                {
                    nombrePrimerAudio = "Fogata";
                    tiempoPrimerAudio = tiempoActual;
                    Log("PRIMER_AUDIO", $"Audio Fogata, Tiempo: {tiempoActual}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarAudioFogata: {e.Message}");
        }
    }

    public void RegistrarAudioRespiracion()
    {
        try
        {
            Debug.Log("TelemetriaManager: RegistrarAudioRespiracion() llamado");

            float tiempoActual = Time.time - startTime;
            vecesAudioRespiracion++;
            tiemposAudioRespiracion.Add(tiempoActual);

            Log("AUDIO_RESPIRACION", $"Activación #{vecesAudioRespiracion}, Tiempo: {tiempoActual}");

            // Si es la primera vez que se activa este audio
            if (!audioRespiracionActivado)
            {
                audioRespiracionActivado = true;

                // Si este es el primer audio activado de todos
                if (string.IsNullOrEmpty(nombrePrimerAudio))
                {
                    nombrePrimerAudio = "Respiración";
                    tiempoPrimerAudio = tiempoActual;
                    Log("PRIMER_AUDIO", $"Audio Respiración, Tiempo: {tiempoActual}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarAudioRespiracion: {e.Message}");
        }
    }

    public void RegistrarAudioArbol()
    {
        try
        {
            Debug.Log("TelemetriaManager: RegistrarAudioArbol() llamado");

            float tiempoActual = Time.time - startTime;
            vecesAudioArbol++;
            tiemposAudioArbol.Add(tiempoActual);

            Log("AUDIO_ARBOL", $"Activación #{vecesAudioArbol}, Tiempo: {tiempoActual}");

            // Si es la primera vez que se activa este audio
            if (!audioArbolActivado)
            {
                audioArbolActivado = true;

                // Si este es el primer audio activado de todos
                if (string.IsNullOrEmpty(nombrePrimerAudio))
                {
                    nombrePrimerAudio = "Árbol";
                    tiempoPrimerAudio = tiempoActual;
                    Log("PRIMER_AUDIO", $"Audio Árbol, Tiempo: {tiempoActual}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarAudioArbol: {e.Message}");
        }
    }

    // Para registrar eventos genéricos
    public void RegistrarEvento(string nombreEvento, string datos)
    {
        try
        {
            Log(nombreEvento, datos);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarEvento: {e.Message}");
        }
    }
}