using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameStatistics : MonoBehaviour
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
    [Tooltip("Array de TextMeshPro para mostrar los 5 mejores jugadores")]
    public TextMeshProUGUI[] rankingTexts;

    [Header("Configuraci�n")]
    [Tooltip("Breve delay en segundos antes de mostrar el ranking (incluso si es 0, sigue habiendo un frame de diferencia)")]
    public float rankingShowDelay = 0.1f;

    private void Start()
    {
        // Ocultar di�logos al inicio
        if (playerStatsDialog != null)
        {
            playerStatsDialog.SetActive(false);
        }

        if (rankingDialog != null)
        {
            rankingDialog.SetActive(false);
        }

        // Verificar que tengamos todas las referencias necesarias
        ValidateReferences();
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

        if (rankingTexts == null || rankingTexts.Length < 5)
        {
            Debug.LogError("No hay suficientes campos de texto para el ranking (se necesitan 5)");
        }
    }

    // M�todo p�blico para mostrar las estad�sticas y el ranking
    public void ShowEndGameStatistics(int golesAtajados, int golesRecibidos)
    {
        StartCoroutine(DisplayStatisticsSequence(golesAtajados, golesRecibidos));
    }

    private IEnumerator DisplayStatisticsSequence(int golesAtajados, int golesRecibidos)
    {
        // Mostrar estad�sticas del jugador
        DisplayPlayerStats(golesAtajados, golesRecibidos);

        // Peque�a espera para asegurar que la UI se actualice correctamente
        yield return new WaitForSeconds(rankingShowDelay);

        // Generar y mostrar el ranking
        GenerateAndDisplayRanking();

        // No hay m�s corrutinas para ocultar los di�logos, se mantendr�n visibles
    }

    // M�todo p�blico para ocultar ambos di�logos (puedes llamarlo desde otro script si necesitas)
    public void HideAllDialogs()
    {
        if (playerStatsDialog != null)
        {
            playerStatsDialog.SetActive(false);
        }

        if (rankingDialog != null)
        {
            rankingDialog.SetActive(false);
        }
    }

    private void DisplayPlayerStats(int golesAtajados, int golesRecibidos)
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
            golesAtajadosText.text = "Goles atajados: " + golesAtajados;
        }

        if (golesRecibidosText != null)
        {
            golesRecibidosText.text = "Goles recibidos: " + golesRecibidos;
        }

        // Mostrar el di�logo
        if (playerStatsDialog != null)
        {
            playerStatsDialog.SetActive(true);
        }
    }

    private void GenerateAndDisplayRanking()
    {
        if (RankingManager.Instance == null)
        {
            Debug.LogError("No se encontr� RankingManager");
            return;
        }

        // Obtener las 5 mejores sesiones, filtrando duplicados
        var topSessions = RankingManager.Instance.GetTopSessionsFiltered(5);

        // Llenar los textos del ranking
        for (int i = 0; i < rankingTexts.Length; i++)
        {
            if (rankingTexts[i] != null)
            {
                if (i < topSessions.Count)
                {
                    // Mostrar el jugador y sus atajadas
                    rankingTexts[i].text = $"{topSessions[i].PlayerName}: {topSessions[i].GolesAtajados} atajadas";
                    rankingTexts[i].gameObject.SetActive(true); // Activar el texto
                }
                else
                {
                    // No hay suficientes sesiones para llenar todas las posiciones
                    rankingTexts[i].gameObject.SetActive(false); // Desactivar el texto
                }
            }
        }

        // Verificar si hay al menos una entrada para mostrar
        bool hasAnyEntry = topSessions.Count > 0;

        // Mostrar el di�logo de ranking solo si hay al menos una entrada
        if (rankingDialog != null)
        {
            rankingDialog.SetActive(hasAnyEntry);
        }
    }
}