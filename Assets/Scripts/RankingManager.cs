using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RankingManager : MonoBehaviour
{
    public static RankingManager Instance { get; private set; }

    private void Awake()
    {
        // Implementación del patrón Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Estructura para almacenar información de cada sesión para el ranking
    public class SessionRankInfo
    {
        public string PlayerName { get; set; }
        public int GolesAtajados { get; set; }
        public int GolesRecibidos { get; set; }
        public DateTime Fecha { get; set; }

        public override string ToString()
        {
            return $"{PlayerName}: {GolesAtajados} atajadas";
        }
    }

    // Obtener todas las sesiones de todos los jugadores
    public List<SessionRankInfo> GetAllSessions()
    {
        List<SessionRankInfo> allSessions = new List<SessionRankInfo>();

        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("PlayerDataManager no encontrado");
            return allSessions;
        }

        // Obtener todos los jugadores
        List<PlayerData> players = PlayerDataManager.Instance.GetRanking();

        foreach (var player in players)
        {
            if (player == null) continue;

            // Asegurar que el diccionario esté sincronizado
            player.SyncToDictionary();

            // Recorrer todas las partidas del jugador
            foreach (var partida in player.partidasJugadas)
            {
                if (partida.Value == null) continue;

                // Crear un objeto SessionRankInfo por cada partida
                allSessions.Add(new SessionRankInfo
                {
                    PlayerName = player.playerName,
                    GolesAtajados = partida.Value.golesAtajados,
                    GolesRecibidos = partida.Value.golesRecibidos,
                    Fecha = DateTime.Parse(partida.Value.fecha)
                });
            }
        }

        return allSessions;
    }

    // Obtener las N mejores sesiones por goles atajados
    public List<SessionRankInfo> GetTopSessions(int count)
    {
        var allSessions = GetAllSessions();

        // Ordenar por goles atajados (descendente) y luego por fecha (ascendente)
        return allSessions
            .OrderByDescending(s => s.GolesAtajados)
            .ThenBy(s => s.Fecha)
            .Take(count)
            .ToList();
    }

    // Obtener las mejores sesiones pero filtrando duplicados (mismo jugador con mismo número de atajadas)
    public List<SessionRankInfo> GetTopSessionsFiltered(int count)
    {
        var topSessions = GetTopSessions(count * 2); // Obtener más sesiones de las necesarias para filtrar
        var filteredSessions = new List<SessionRankInfo>();

        foreach (var session in topSessions)
        {
            bool duplicateFound = false;

            // Comprobar si ya tenemos una sesión de este jugador con el mismo número de atajadas
            foreach (var existingSession in filteredSessions)
            {
                if (existingSession.PlayerName == session.PlayerName &&
                    existingSession.GolesAtajados == session.GolesAtajados)
                {
                    duplicateFound = true;
                    break;
                }
            }

            // Si no es duplicado y no hemos alcanzado el límite, añadirlo
            if (!duplicateFound && filteredSessions.Count < count)
            {
                filteredSessions.Add(session);
            }
        }

        return filteredSessions;
    }
}