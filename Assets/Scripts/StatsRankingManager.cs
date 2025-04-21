using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StatsRankingManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("GameObject que contiene el di�logo de estad�sticas del jugador")]
    public GameObject playerStatsDialog;

    [Tooltip("GameObject que contiene el di�logo del ranking")]
    public GameObject rankingDialog;

    [Header("Textos de Estad�sticas del Jugador")]
    [Tooltip("TextMeshPro para mostrar el nombre del jugador")]
    public TextMeshProUGUI playerNameText;

    [Tooltip("TextMeshPro para mostrar los goles atajados")]
    public TextMeshProUGUI golesAtajadosText;

    [Tooltip("TextMeshPro para mostrar los goles recibidos")]
    public TextMeshProUGUI golesRecibidosText;

    [Header("Textos de Ranking")]
    [Tooltip("Array de TextMeshPro para mostrar los 10 mejores jugadores")]
    public TextMeshProUGUI[] rankingTexts;

    [Header("Configuraci�n de Ranking")]
    [Tooltip("N�mero de mejores partidas a mostrar")]
    public int numeroMejoresPartidas = 10;

    // Datos de la partida actual
    private int currentGolesAtajados = 0;
    private int currentGolesRecibidos = 0;
    private bool initialized = false;

    private void Start()
    {
        // Verificar que tengamos todas las referencias necesarias
        ValidateReferences();

        // Intentar inicializar si a�n no se ha hecho
        if (!initialized && GameController.Instance != null)
        {
            Initialize(0, 0); // Valores por defecto, se sobrescribir�n si GameController tiene datos
        }
    }

    private void ValidateReferences()
    {
        if (playerStatsDialog == null)
        {
            Debug.LogError("Falta referencia al di�logo de estad�sticas del jugador");
        }

        if (rankingDialog == null)
        {
            Debug.LogError("Falta referencia al di�logo de ranking");
        }

        if (playerNameText == null)
        {
            Debug.LogError("Falta referencia al texto del nombre del jugador");
        }

        if (golesAtajadosText == null)
        {
            Debug.LogError("Falta referencia al texto de goles atajados");
        }

        if (golesRecibidosText == null)
        {
            Debug.LogError("Falta referencia al texto de goles recibidos");
        }

        if (rankingTexts == null || rankingTexts.Length < numeroMejoresPartidas)
        {
            Debug.LogWarning($"No hay suficientes campos de texto para el ranking (se necesitan {numeroMejoresPartidas}). " +
                           $"S�lo se mostrar�n {rankingTexts.Length} posiciones.");
        }
    }

    // M�todo para inicializar la escena con los datos de la partida
    public void Initialize(int golesAtajados, int golesRecibidos)
    {
        currentGolesAtajados = golesAtajados;
        currentGolesRecibidos = golesRecibidos;
        initialized = true;

        // Mostrar las estad�sticas y el ranking
        DisplayPlayerStats();
        GenerateAndDisplayRanking();
    }

    // M�todo p�blico para volver a jugar (se asignar� al bot�n desde el Inspector)
    public void PlayAgain()
    {
        Debug.Log("Iniciando nueva partida para el jugador actual...");
        if (GameController.Instance != null)
        {
            GameController.Instance.LoadGameScene();
        }
        else
        {
            Debug.LogError("GameController no encontrado");
        }
    }

    // M�todo p�blico para volver al men� principal (se asignar� al bot�n desde el Inspector)
    public void ReturnToMainMenu()
    {
        Debug.Log("Volviendo al men� principal...");
        if (GameController.Instance != null)
        {
            GameController.Instance.LoadRegistrationScene();
        }
        else
        {
            Debug.LogError("GameController no encontrado");
        }
    }

    private void DisplayPlayerStats()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("No se encontr� PlayerDataManager");
            return;
        }

        // Obtener el nombre del jugador actual
        string playerName = PlayerDataManager.Instance.GetCurrentPlayerName();

        // Actualizar los textos
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        if (golesAtajadosText != null)
        {
            golesAtajadosText.text = "Goles atajados: " + currentGolesAtajados;
        }

        if (golesRecibidosText != null)
        {
            golesRecibidosText.text = "Goles recibidos: " + currentGolesRecibidos;
        }
    }

    private void GenerateAndDisplayRanking()
    {
        if (RankingManager.Instance == null)
        {
            Debug.LogError("No se encontr� RankingManager");
            return;
        }

        // Obtener las mejores sesiones, filtrando duplicados
        var topSessions = RankingManager.Instance.GetTopSessionsFiltered(numeroMejoresPartidas);
        Debug.Log($"Se obtuvieron {topSessions.Count} mejores partidas para mostrar en el ranking");

        // N�mero real de textos a mostrar (el m�nimo entre el n�mero deseado y los disponibles)
        int textsToPopulate = Mathf.Min(rankingTexts.Length, numeroMejoresPartidas);

        // Llenar los textos del ranking
        for (int i = 0; i < rankingTexts.Length; i++)
        {
            if (rankingTexts[i] != null)
            {
                if (i < topSessions.Count && i < textsToPopulate)
                {
                    // Mostrar el jugador y sus atajadas
                    rankingTexts[i].text = $"{i + 1}. {topSessions[i].PlayerName}: {topSessions[i].GolesAtajados} atajadas";
                    rankingTexts[i].gameObject.SetActive(true); // Activar el texto
                }
                else
                {
                    // No hay suficientes sesiones para llenar todas las posiciones
                    rankingTexts[i].gameObject.SetActive(false); // Desactivar el texto
                }
            }
        }
    }
}