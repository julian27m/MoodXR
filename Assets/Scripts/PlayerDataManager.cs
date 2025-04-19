using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Clase que almacena datos de una partida individual
[Serializable]
public class GameSession
{
    public int golesAtajados;
    public int golesRecibidos;
    public string fecha;

    public GameSession()
    {
        golesAtajados = 0;
        golesRecibidos = 0;
        fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

// Clase para almacenar los datos de un jugador individual
[Serializable]
public class PlayerData
{
    public string playerName;
    [NonSerialized]
    public Dictionary<string, GameSession> partidasJugadas = new Dictionary<string, GameSession>();
    public string ultimaPartida;

    // Propiedad para serializar el diccionario a JSON
    [Serializable]
    public class SessionEntry
    {
        public string key;
        public GameSession value;
    }

    // Este campo se serializará
    public List<SessionEntry> partidasList = new List<SessionEntry>();

    public PlayerData(string name)
    {
        playerName = name;
        ultimaPartida = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Crear la primera partida
        GameSession primeraPartida = new GameSession();
        partidasJugadas = new Dictionary<string, GameSession>();
        partidasJugadas.Add("partida1", primeraPartida);

        // Sincronizar con la lista para serialización
        SyncFromDictionary();
    }

    // Método para sincronizar el diccionario con la lista serializable
    public void SyncFromDictionary()
    {
        partidasList.Clear();
        foreach (var kvp in partidasJugadas)
        {
            partidasList.Add(new SessionEntry { key = kvp.Key, value = kvp.Value });
        }
    }

    // Método para reconstruir el diccionario desde la lista serializable
    public void SyncToDictionary()
    {
        partidasJugadas = new Dictionary<string, GameSession>();
        foreach (var entry in partidasList)
        {
            if (entry != null && entry.key != null && entry.value != null)
            {
                partidasJugadas[entry.key] = entry.value;
            }
        }
    }

    // Agregar una nueva partida al jugador
    public GameSession AddNewSession()
    {
        // Asegurarse de que el diccionario esté actualizado
        if (partidasJugadas == null || partidasJugadas.Count == 0)
        {
            SyncToDictionary();
        }

        int partidaNum = partidasJugadas.Count + 1;
        string partidaKey = "partida" + partidaNum;

        GameSession nuevaPartida = new GameSession();
        partidasJugadas.Add(partidaKey, nuevaPartida);

        // Actualizar la fecha de última partida
        ultimaPartida = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Sincronizar para serialización
        SyncFromDictionary();

        return nuevaPartida;
    }

    // Obtener la partida actual (la última)
    public GameSession GetCurrentSession()
    {
        // Asegurarse de que el diccionario esté actualizado
        if (partidasJugadas == null || partidasJugadas.Count == 0)
        {
            SyncToDictionary();

            // Si aún está vacío, crear una nueva partida
            if (partidasJugadas.Count == 0)
            {
                return AddNewSession();
            }
        }

        string lastKey = "partida" + partidasJugadas.Count;
        if (partidasJugadas.ContainsKey(lastKey))
        {
            return partidasJugadas[lastKey];
        }
        else
        {
            Debug.LogError($"No se encontró la partida con clave {lastKey}");
            return AddNewSession();
        }
    }

    // Calcula la puntuación total (todos los goles atajados menos todos los goles recibidos)
    public int GetPuntuacionTotal()
    {
        // Asegurarse de que el diccionario esté actualizado
        if (partidasJugadas == null || partidasJugadas.Count == 0)
        {
            SyncToDictionary();
        }

        int totalAtajados = 0;
        int totalRecibidos = 0;

        foreach (var partida in partidasJugadas.Values)
        {
            totalAtajados += partida.golesAtajados;
            totalRecibidos += partida.golesRecibidos;
        }

        return totalAtajados * 10 - totalRecibidos * 5;
    }

    // Obtener la mejor partida (con más goles atajados)
    public GameSession GetBestSession()
    {
        // Asegurarse de que el diccionario esté actualizado
        if (partidasJugadas == null || partidasJugadas.Count == 0)
        {
            SyncToDictionary();
        }

        if (partidasJugadas.Count == 0)
            return null;

        return partidasJugadas.Values.OrderByDescending(p => p.golesAtajados).First();
    }
}

// Clase para almacenar todos los datos del juego
[Serializable]
public class GameData
{
    public List<PlayerData> players = new List<PlayerData>();

    // Método para reconstruir diccionarios después de cargar de JSON
    public void RebuildDictionaries()
    {
        foreach (var player in players)
        {
            if (player != null)
            {
                player.SyncToDictionary();
            }
        }
    }
}

// Clase singleton para gestionar la persistencia de datos
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    // Ruta donde se guardará el archivo de datos
    private string dataPath;

    // Datos del juego
    private GameData gameData;

    // Jugador actual
    private PlayerData currentPlayer;

    // Partida actual
    private GameSession currentSession;

    // Evento que se dispara cuando se actualiza el ranking
    public event Action OnRankingUpdated;

    // Bandera para indicar si ya se han cargado los datos
    private bool dataLoaded = false;

    private void Awake()
    {
        // Implementación del patrón Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Este objeto persistirá entre escenas

        // Definir la ruta del archivo de datos
        dataPath = Path.Combine(Application.persistentDataPath, "playerdata.json");

        // Cargar datos existentes o crear nuevos
        LoadData();

        // Marcar los datos como cargados
        dataLoaded = true;
    }

    private void OnApplicationQuit()
    {
        // Guardar los datos antes de cerrar la aplicación
        SaveData();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // Guardar datos cuando la aplicación se pausa (importante para dispositivos móviles)
        if (pauseStatus)
        {
            SaveData();
        }
    }

    // Cargar datos desde el archivo
    public void LoadData()
    {
        try
        {
            if (File.Exists(dataPath))
            {
                string json = File.ReadAllText(dataPath);
                gameData = JsonUtility.FromJson<GameData>(json);

                // Reconstruir diccionarios (no se serializan directamente)
                gameData.RebuildDictionaries();

                Debug.Log($"Datos cargados: {gameData.players.Count} jugadores encontrados");

                // Validar los datos cargados por seguridad
                ValidateData();
            }
            else
            {
                gameData = new GameData();
                Debug.Log("No se encontró archivo de datos. Se creó uno nuevo.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al cargar datos: {e.Message}\nStack trace: {e.StackTrace}");
            gameData = new GameData();
        }
    }

    // Validar datos para evitar problemas con nombres nulos
    private void ValidateData()
    {
        if (gameData == null || gameData.players == null)
        {
            gameData = new GameData();
            return;
        }

        // Eliminar cualquier entrada con nombre nulo o vacío
        for (int i = gameData.players.Count - 1; i >= 0; i--)
        {
            if (gameData.players[i] == null || string.IsNullOrEmpty(gameData.players[i].playerName))
            {
                gameData.players.RemoveAt(i);
            }
        }
    }

    // Guardar datos en el archivo
    public void SaveData()
    {
        try
        {
            // Asegurar que todos los diccionarios estén sincronizados con las listas serializables
            foreach (var player in gameData.players)
            {
                if (player != null)
                {
                    player.SyncFromDictionary();
                }
            }

            string json = JsonUtility.ToJson(gameData, true);
            File.WriteAllText(dataPath, json);
            Debug.Log("Datos guardados exitosamente en: " + dataPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al guardar datos: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    // Verificar si un nombre de jugador ya existe
    public bool PlayerExists(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            return false;
        }

        return gameData.players.Any(p =>
            p != null && p.playerName != null &&
            p.playerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));
    }

    // Crear un nuevo jugador
    public bool CreateNewPlayer(string playerName)
    {
        // Validar el nombre (no puede estar vacío ni duplicado)
        if (string.IsNullOrEmpty(playerName) || string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("El nombre del jugador no puede estar vacío");
            return false;
        }

        if (PlayerExists(playerName))
        {
            Debug.LogWarning($"Ya existe un jugador con el nombre {playerName}");
            return false;
        }

        // Crear el nuevo jugador
        currentPlayer = new PlayerData(playerName);
        currentSession = currentPlayer.GetCurrentSession(); // Obtener la primera partida

        gameData.players.Add(currentPlayer);
        SaveData();

        Debug.Log($"Jugador {playerName} creado exitosamente con su primera partida");
        return true;
    }

    // Login para un jugador existente y crear una nueva partida
    public bool LoginExistingPlayer(string playerName)
    {
        // Validar que el nombre no esté vacío
        if (string.IsNullOrEmpty(playerName) || string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("El nombre del jugador no puede estar vacío");
            return false;
        }

        // Encontrar el jugador en la lista
        currentPlayer = gameData.players.FirstOrDefault(p =>
            p != null && p.playerName != null &&
            p.playerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));

        if (currentPlayer != null)
        {
            // Asegurarse de que el diccionario esté sincronizado
            currentPlayer.SyncToDictionary();

            // Crear una nueva sesión para el jugador
            currentSession = currentPlayer.AddNewSession();

            // Guardar los cambios
            SaveData();

            Debug.Log($"Jugador {playerName} ha iniciado sesión con una nueva partida. Total partidas: {currentPlayer.partidasJugadas.Count}");
            return true;
        }

        Debug.LogWarning($"No se encontró el jugador {playerName}");
        return false;
    }

    // Seleccionar un jugador existente sin crear nueva partida (para operaciones de consulta)
    public bool SelectPlayer(string playerName)
    {
        currentPlayer = gameData.players.FirstOrDefault(p =>
            p != null && p.playerName != null &&
            p.playerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));

        if (currentPlayer != null)
        {
            // Asegurarse de que el diccionario esté sincronizado
            currentPlayer.SyncToDictionary();

            currentSession = currentPlayer.GetCurrentSession();
            Debug.Log($"Jugador {playerName} seleccionado");
            return true;
        }

        Debug.LogWarning($"No se encontró el jugador {playerName}");
        return false;
    }

    // Obtener el jugador actual
    public PlayerData GetCurrentPlayer()
    {
        return currentPlayer;
    }

    // Obtener la partida actual
    public GameSession GetCurrentSession()
    {
        return currentSession;
    }

    // Incrementar los goles recibidos para la partida actual
    public void IncrementGolesRecibidos(int amount = 1)
    {
        if (currentPlayer == null || currentSession == null)
        {
            Debug.LogError("No hay jugador o partida seleccionada");
            return;
        }

        currentSession.golesRecibidos += amount;
        SaveData();

        Debug.Log($"Goles recibidos incrementados para {currentPlayer.playerName}. " +
                 $"Total en esta partida: {currentSession.golesRecibidos}");
    }

    // Incrementar los goles atajados para la partida actual
    public void IncrementGolesAtajados(int amount = 1)
    {
        if (currentPlayer == null || currentSession == null)
        {
            Debug.LogError("No hay jugador o partida seleccionada");
            return;
        }

        currentSession.golesAtajados += amount;
        SaveData();

        Debug.Log($"Goles atajados incrementados para {currentPlayer.playerName}. " +
                 $"Total en esta partida: {currentSession.golesAtajados}");
    }

    // Actualizar la sesión actual con los resultados de una partida
    public void UpdateCurrentSessionStats(int golesAtajados, int golesRecibidos)
    {
        if (currentPlayer == null || currentSession == null)
        {
            Debug.LogError("No hay jugador o partida seleccionada");
            return;
        }

        currentSession.golesAtajados = golesAtajados;
        currentSession.golesRecibidos = golesRecibidos;
        currentSession.fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        SaveData();

        // Notificar a los observadores que el ranking se ha actualizado
        if (OnRankingUpdated != null)
        {
            OnRankingUpdated.Invoke();
        }

        Debug.Log($"Estadísticas actualizadas para {currentPlayer.playerName} en partida actual: " +
                 $"Goles atajados={currentSession.golesAtajados}, " +
                 $"Goles recibidos={currentSession.golesRecibidos}");
    }

    // Obtener el nombre del jugador actual
    public string GetCurrentPlayerName()
    {
        return currentPlayer?.playerName ?? "Sin Jugador";
    }

    // Obtener el ranking de jugadores ordenado por puntuación
    public List<PlayerData> GetRanking()
    {
        return gameData.players
            .Where(p => p != null)
            .OrderByDescending(p => p.GetPuntuacionTotal())
            .ToList();
    }

    // Borrar todos los datos (función de administrador)
    public void DeleteAllData()
    {
        gameData = new GameData();
        currentPlayer = null;
        currentSession = null;
        SaveData();

        // Notificar a los observadores que el ranking se ha actualizado
        if (OnRankingUpdated != null)
        {
            OnRankingUpdated.Invoke();
        }

        Debug.Log("Todos los datos han sido eliminados");
    }
}