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

    // Información del dispositivo
    private string deviceName = "Desconocido";


    // Variable para almacenar el código del usuario una vez guardado
    private string codigoUsuario = "";

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

        // Obtener el nombre del dispositivo
        ObtenerDeviceName();

        // Crear el archivo de log
        CreateLogFile();

        // Debug info
        Debug.Log("TelemetriaManager inicializado");

        // Verificar que tengamos los colliders necesarios
        ValidarColliders();
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
        Log("INICIO_APLICACION", $"Dispositivo: {deviceName}");
        isLogReady = true; // Marcar que estamos listos para registrar eventos
        Debug.Log("TelemetriaManager: isLogReady = true");

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
            Debug.Log("Guardado periódico de telemetría");
        }

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

    private void CreateLogFile()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string directory = Path.Combine(Application.persistentDataPath, "Logs");

        // Crear el directorio si no existe
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Incluir el nombre del dispositivo en el nombre del archivo
        string deviceNameSafe = deviceName.Replace(" ", "_").Replace(":", "").Replace("/", "");
        if (deviceNameSafe.Length > 20) deviceNameSafe = deviceNameSafe.Substring(0, 20);

        logFilePath = Path.Combine(directory, $"log_{deviceNameSafe}_{timestamp}.txt");

        try
        {
            // Escribir encabezado del archivo
            using (StreamWriter writer = new StreamWriter(logFilePath, false))
            {
                writer.WriteLine("Timestamp,TiempoDesdeInicio,Evento,Datos");
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

            string logEntry = $"{timestamp},{tiempoTranscurrido},{evento},{datos}\n";
            logBuffer.Append(logEntry);

            // Guardar en el archivo cada cierto tiempo o cantidad de entradas
            if (logBuffer.Length > 500) // Reducido para guardar más frecuentemente
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
        SaveLogBuffer();

        // Generar resumen
        GuardarResumen();
    }

    private void GuardarResumen()
    {
        try
        {
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

            StringBuilder resumen = new StringBuilder();
            resumen.AppendLine("RESUMEN DE LA EXPERIENCIA");

            // Añadir información del dispositivo al resumen
            resumen.AppendLine($"Dispositivo: {deviceName}");

            // Añadir código de usuario al resumen
            if (!string.IsNullOrEmpty(codigoUsuario))
            {
                resumen.AppendLine($"Código de usuario: {codigoUsuario}");
            }
            else
            {
                resumen.AppendLine("Código de usuario: No especificado");
            }

            resumen.AppendLine($"Tiempo total de experiencia: {Time.time - startTime} segundos");

            // Resumen de atención del usuario
            resumen.AppendLine("\nATENCIÓN DEL USUARIO:");
            resumen.AppendLine($"Tiempo mirando al árbol: {colliderTotalLookTimes["Arbol"]:F2} segundos");
            resumen.AppendLine($"Tiempo mirando a la caja: {colliderTotalLookTimes["Caja"]:F2} segundos");
            resumen.AppendLine($"Tiempo mirando a la fogata: {colliderTotalLookTimes["Fogata"]:F2} segundos");
            resumen.AppendLine($"Tiempo mirando a la respiración: {colliderTotalLookTimes["Respiracion"]:F2} segundos");
            resumen.AppendLine($"Tiempo sin mirar a ningún elemento: {colliderTotalLookTimes["Ninguno"]:F2} segundos");

            // Calcular porcentajes de atención
            float tiempoTotal = Time.time - startTime;
            if (tiempoTotal > 0)
            {
                resumen.AppendLine("\nPorcentajes de atención:");
                resumen.AppendLine($"Árbol: {(colliderTotalLookTimes["Arbol"] / tiempoTotal * 100):F2}%");
                resumen.AppendLine($"Caja: {(colliderTotalLookTimes["Caja"] / tiempoTotal * 100):F2}%");
                resumen.AppendLine($"Fogata: {(colliderTotalLookTimes["Fogata"] / tiempoTotal * 100):F2}%");
                resumen.AppendLine($"Respiración: {(colliderTotalLookTimes["Respiracion"] / tiempoTotal * 100):F2}%");
                resumen.AppendLine($"Sin foco específico: {(colliderTotalLookTimes["Ninguno"] / tiempoTotal * 100):F2}%");
            }

            // Resumen Caja
            resumen.AppendLine("\nCAJA:");
            resumen.AppendLine($"Caja abierta: {cajaAbierta}");
            if (cajaAbierta)
            {
                resumen.AppendLine($"Tiempo hasta abrir la caja: {tiempoAperturaCaja} segundos");
            }

            // Resumen Fogata
            resumen.AppendLine("\nFOGATA:");
            resumen.AppendLine($"Piedras colocadas: {piedrasColocadas}/4");
            if (piedrasColocadas > 0)
            {
                resumen.AppendLine($"Tiempo hasta primera piedra: {tiempoPrimeraPiedra} segundos");
            }
            if (piedrasColocadas == 4)
            {
                resumen.AppendLine($"Tiempo para completar todas las piedras desde la primera: {tiempoTodasPiedras} segundos");
            }
            resumen.AppendLine($"Veces encendida la fogata: {vecesEncendida}");
            resumen.AppendLine($"Veces apagada la fogata: {vecesApagada}");

            // Resumen Hojas
            resumen.AppendLine("\nHOJAS:");
            resumen.AppendLine($"Hojas manipuladas: {hojasAgarradas}");
            if (hojasAgarradas > 0)
            {
                resumen.AppendLine($"Tiempo hasta primera hoja: {tiempoPrimeraHoja} segundos");
                resumen.AppendLine($"Tiempo promedio de interacción con hoja: {tiempoPromedioInteraccionHojas} segundos");
            }

            // Resumen Botones de Fin
            resumen.AppendLine("\nCIERRE DE EXPERIENCIA:");
            if (primerFinActivado)
            {
                resumen.AppendLine($"Tiempo hasta primer botón de fin: {tiempoPrimerFin} segundos");

                if (tiempoSegundoFin > 0f)
                {
                    resumen.AppendLine($"Tiempo hasta segundo botón de fin: {tiempoSegundoFin} segundos");
                    resumen.AppendLine($"Tiempo entre primer y segundo fin: {tiempoEntreFines} segundos");
                }
                else
                {
                    resumen.AppendLine("El segundo botón de fin no fue activado");
                }
            }
            else
            {
                resumen.AppendLine("Ningún botón de fin fue activado (la aplicación se cerró de otra manera)");
            }

            // Resumen de botones de audio
            resumen.AppendLine("\nBOTONES DE AUDIO:");

            if (audioHojasActivado || audioFogataActivado || audioRespiracionActivado || audioArbolActivado)
            {
                resumen.AppendLine($"Primer audio activado: {nombrePrimerAudio} a los {tiempoPrimerAudio} segundos");

                resumen.AppendLine("\nAudio Hojas:");
                resumen.AppendLine($"  Veces activado: {vecesAudioHojas}");
                if (vecesAudioHojas > 0)
                {
                    resumen.AppendLine($"  Tiempos de activación (segundos): {String.Join(", ", tiemposAudioHojas)}");
                }

                resumen.AppendLine("\nAudio Fogata:");
                resumen.AppendLine($"  Veces activado: {vecesAudioFogata}");
                if (vecesAudioFogata > 0)
                {
                    resumen.AppendLine($"  Tiempos de activación (segundos): {String.Join(", ", tiemposAudioFogata)}");
                }

                resumen.AppendLine("\nAudio Respiración:");
                resumen.AppendLine($"  Veces activado: {vecesAudioRespiracion}");
                if (vecesAudioRespiracion > 0)
                {
                    resumen.AppendLine($"  Tiempos de activación (segundos): {String.Join(", ", tiemposAudioRespiracion)}");
                }

                resumen.AppendLine("\nAudio Árbol:");
                resumen.AppendLine($"  Veces activado: {vecesAudioArbol}");
                if (vecesAudioArbol > 0)
                {
                    resumen.AppendLine($"  Tiempos de activación (segundos): {String.Join(", ", tiemposAudioArbol)}");
                }

                // Determinar secuencia de activación de audios
                resumen.AppendLine("\nSecuencia de audio según primer uso:");
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
                    resumen.AppendLine($"  {i + 1}. Audio {secuencia[i].Key} a los {secuencia[i].Value} segundos");
                }
            }
            else
            {
                resumen.AppendLine("No se activó ningún audio de instrucciones");
            }

            // Guardar resumen
            Log("RESUMEN", resumen.ToString());
            SaveLogBuffer();

            // Incluir el nombre del dispositivo y el código de usuario en el nombre del archivo de resumen
            string deviceNameSafe = deviceName.Replace(" ", "_").Replace(":", "").Replace("/", "");
            if (deviceNameSafe.Length > 20) deviceNameSafe = deviceNameSafe.Substring(0, 20);

            string codigoSeguro = "";
            if (!string.IsNullOrEmpty(codigoUsuario))
            {
                codigoSeguro = codigoUsuario.Replace(" ", "_").Replace(":", "").Replace("/", "");
                if (codigoSeguro.Length > 20) codigoSeguro = codigoSeguro.Substring(0, 20);
            }

            // Guardar archivo separado de resumen para consulta más fácil
            string summaryPath = Path.Combine(
                Application.persistentDataPath,
                "Logs",
                $"resumen_{deviceNameSafe}_{codigoSeguro}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt");

            File.WriteAllText(summaryPath, resumen.ToString());
            Debug.Log($"Resumen guardado en: {summaryPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al generar resumen: {e.Message}");
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
                SaveLogBuffer(); // Guardar inmediatamente 
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

                SaveLogBuffer(); // Guardar inmediatamente
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
            SaveLogBuffer(); // Guardar inmediatamente
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
            SaveLogBuffer(); // Guardar inmediatamente
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
                SaveLogBuffer(); // Guardar inmediatamente
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
            SaveLogBuffer(); // Guardar inmediatamente
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
                SaveLogBuffer(); // Guardar inmediatamente
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

                // Guardar todo inmediatamente y generar el resumen
                SaveLogBuffer();
                GuardarResumen();
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

            SaveLogBuffer(); // Guardar inmediatamente
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

            SaveLogBuffer(); // Guardar inmediatamente
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

            SaveLogBuffer(); // Guardar inmediatamente
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

            SaveLogBuffer(); // Guardar inmediatamente
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
            SaveLogBuffer(); // Guardar inmediatamente
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en RegistrarEvento: {e.Message}");
        }
    }

    /// <summary>
    /// Guarda el código de usuario actual del texto asignado
    /// Esta función debe ser llamada desde otro script cuando el usuario haya ingresado su código
    /// </summary>
    public void CodigoGuardado(string codigo)
    {
        try
        {
            // Obtener el texto del código
            codigoUsuario = codigo.Trim();

            // Registrar en la telemetría
            if (!string.IsNullOrEmpty(codigoUsuario))
            {
                RegistrarEvento("CODIGO_USUARIO", codigoUsuario);
                Debug.Log($"Código de usuario guardado: {codigoUsuario}");
            }
            else
            {
                RegistrarEvento("CODIGO_USUARIO", "No especificado");
                Debug.LogWarning("El código de usuario está vacío");
            }

            // Guardar inmediatamente
            SaveLogBuffer();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al guardar código de usuario: {e.Message}");
        }
    }

    /// <summary>
    /// Método para asegurar que el código del usuario se incluya en el resumen final
    /// incluso si se establece después de que se haya detenido la recolección de datos.
    /// </summary>
    public void AsegurarCodigoEnResumen(string codigo)
    {
        // Si ya tenemos el código, no hacer nada
        if (!string.IsNullOrEmpty(codigoUsuario) && codigoUsuario == codigo)
        {
            return;
        }

        // Establecer el código directamente en la variable
        codigoUsuario = codigo;

        // Registrar el código aunque la recolección de datos esté detenida
        try
        {
            // Intentar registrar el evento sin importar el estado de isLogReady
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            float tiempoTranscurrido = Time.time - startTime;
            string logEntry = $"{timestamp},{tiempoTranscurrido},CODIGO_USUARIO_FINAL,{codigo}\n";

            // Añadir directamente al buffer
            logBuffer.Append(logEntry);

            // Forzar guardado
            SaveLogBuffer();

            Debug.Log($"[TELEMETRÍA] Código de usuario final registrado: {codigo}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al registrar código de usuario final: {e.Message}");
        }
    }
}