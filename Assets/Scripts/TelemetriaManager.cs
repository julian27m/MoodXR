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

    private string logFilePath;
    private StringBuilder logBuffer = new StringBuilder();
    private float startTime;
    private bool isLogFileCreated = false;
    private bool isLogReady = false; // Para evitar logs prematuros

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

    private float tiempoPrimerAudio = 0f;
    private string nombrePrimerAudio = "";

    private int vecesAudioHojas = 0;
    private int vecesAudioFogata = 0;
    private int vecesAudioRespiracion = 0;

    private List<float> tiemposAudioHojas = new List<float>();
    private List<float> tiemposAudioFogata = new List<float>();
    private List<float> tiemposAudioRespiracion = new List<float>();

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

        // Crear el archivo de log
        CreateLogFile();

        // Debug info
        Debug.Log("TelemetriaManager inicializado");
    }

    void Start()
    {
        startTime = Time.time;
        Log("INICIO_APLICACION", "");
        isLogReady = true; // Marcar que estamos listos para registrar eventos
        Debug.Log("TelemetriaManager: isLogReady = true");
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

        logFilePath = Path.Combine(directory, $"log_{timestamp}.txt");

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
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        float tiempoTranscurrido = Time.time - startTime;

        string logEntry = $"{timestamp},{tiempoTranscurrido},{evento},{datos}\n";
        logBuffer.Append(logEntry);

        // Guardar en el archivo cada cierto tiempo o cantidad de entradas
        if (logBuffer.Length > 1000)
        {
            SaveLogBuffer();
        }

        Debug.Log($"[TELEMETRÍA] {evento}: {datos}");
    }

    private void SaveLogBuffer()
    {
        if (!isLogFileCreated) return;

        try
        {
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.Write(logBuffer.ToString());
                writer.Flush();
            }

            logBuffer.Clear();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al guardar datos de telemetría: {e.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        // Si el primer fin no fue activado, registrarlo aquí
        if (!primerFinActivado)
        {
            RegistrarPrimerFin();
        }

        // Si el segundo fin no fue activado, registrarlo aquí
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
        resumen.AppendLine($"Tiempo total de experiencia: {Time.time - startTime} segundos");

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

        if (audioHojasActivado || audioFogataActivado || audioRespiracionActivado)
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

            // Determinar secuencia de activación de audios
            resumen.AppendLine("\nSecuencia de audio según primer uso:");
            List<KeyValuePair<string, float>> secuencia = new List<KeyValuePair<string, float>>();

            if (audioHojasActivado && tiemposAudioHojas.Count > 0)
                secuencia.Add(new KeyValuePair<string, float>("Hojas", tiemposAudioHojas[0]));

            if (audioFogataActivado && tiemposAudioFogata.Count > 0)
                secuencia.Add(new KeyValuePair<string, float>("Fogata", tiemposAudioFogata[0]));

            if (audioRespiracionActivado && tiemposAudioRespiracion.Count > 0)
                secuencia.Add(new KeyValuePair<string, float>("Respiración", tiemposAudioRespiracion[0]));

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
    }

    // ==== Métodos públicos para registrar eventos desde otros scripts ====

    // Eventos de la caja
    public void RegistrarAperturaCaja()
    {
        if (!cajaAbierta)
        {
            cajaAbierta = true;
            tiempoAperturaCaja = Time.time - startTime;
            Log("CAJA_ABIERTA", $"Tiempo: {tiempoAperturaCaja}");
        }
    }

    // Eventos de la fogata
    public void RegistrarPiedraColocada(int totalPiedras)
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

    public void RegistrarFogataEncendida()
    {
        if (!isLogReady) return;

        vecesEncendida++;
        Log("FOGATA_ENCENDIDA", $"Veces: {vecesEncendida}");
    }

    public void RegistrarFogataApagada()
    {
        if (!isLogReady) return;

        vecesApagada++;
        Log("FOGATA_APAGADA", $"Veces: {vecesApagada}");
    }

    // Eventos de las hojas
    public void RegistrarHojaAgarrada(int instanceID)
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

    public void RegistrarHojaSoltada(int instanceID)
    {
        if (!isLogReady) return;

        float tiempoActual = Time.time - startTime;
        tiemposSoltarHojas.Add(tiempoActual);
        Log("HOJA_SOLTADA", $"InstanceID: {instanceID}, Tiempo: {tiempoActual}");
    }

    // Eventos de los botones de fin
    public void RegistrarPrimerFin()
    {
        if (!isLogReady)
        {
            Debug.LogWarning("TelemetriaManager: RegistrarPrimerFin() llamado pero isLogReady es false");
            return;
        }

        Debug.Log("TelemetriaManager: RegistrarPrimerFin() llamado");

        if (!primerFinActivado)
        {
            primerFinActivado = true;
            tiempoPrimerFin = Time.time - startTime;
            Log("PRIMER_FIN", $"Tiempo: {tiempoPrimerFin}");
        }
    }

    public void RegistrarSegundoFin()
    {
        if (!isLogReady)
        {
            Debug.LogWarning("TelemetriaManager: RegistrarSegundoFin() llamado pero isLogReady es false");
            return;
        }

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
                Log("SEGUNDO_FIN", $"Tiempo: {tiempoSegundoFin}, AVISO: Segundo fin activado sin haber activado el primero");
            }
        }
    }

    // Eventos de los botones de audio
    public void RegistrarAudioHojas()
    {
        if (!isLogReady)
        {
            Debug.LogWarning("TelemetriaManager: RegistrarAudioHojas() llamado pero isLogReady es false");
            return;
        }

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

    public void RegistrarAudioFogata()
    {
        if (!isLogReady)
        {
            Debug.LogWarning("TelemetriaManager: RegistrarAudioFogata() llamado pero isLogReady es false");
            return;
        }

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

    public void RegistrarAudioRespiracion()
    {
        if (!isLogReady)
        {
            Debug.LogWarning("TelemetriaManager: RegistrarAudioRespiracion() llamado pero isLogReady es false");
            return;
        }

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

    // Para registrar eventos genéricos
    public void RegistrarEvento(string nombreEvento, string datos)
    {
        if (!isLogReady && nombreEvento != "INICIO_APLICACION")
        {
            Debug.LogWarning($"TelemetriaManager: RegistrarEvento({nombreEvento}) llamado pero isLogReady es false");
            return;
        }

        Log(nombreEvento, datos);
    }
}