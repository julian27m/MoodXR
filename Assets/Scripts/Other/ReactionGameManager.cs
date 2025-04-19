using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ReactionGameManager : MonoBehaviour
{
    [SerializeField] private TextMeshPro scoreText; // Referencia al TextMeshPro para mostrar los puntos

    private int score = 0; // Puntuación actual

    // Singleton para acceder desde cualquier script
    private static ReactionGameManager _instance;
    public static ReactionGameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ReactionGameManager>();
                if (_instance == null)
                {
                    GameObject managerObject = new GameObject("ReactionGameManager");
                    _instance = managerObject.AddComponent<ReactionGameManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        // Asegurar que solo existe una instancia
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    private void Start()
    {
        // Validar la referencia al TextMeshPro
        if (scoreText == null)
        {
            Debug.LogWarning("No se ha asignado un TextMeshPro para mostrar la puntuación.");
        }

        // Inicializar puntuación
        ResetScore();
    }

    /// <summary>
    /// Añade puntos a la puntuación actual
    /// </summary>
    /// <param name="points">Cantidad de puntos a añadir</param>
    public void AddPoints(int points)
    {
        score += points;
        UpdateScoreText();
        Debug.Log("Puntuación actual: " + score);
    }

    /// <summary>
    /// Añade un punto a la puntuación
    /// </summary>
    public void AddPoint()
    {
        AddPoints(1);
        GetComponent<ReactionGameTelemetry>()?.OnButtonPressedForTelemetry();
    }

    /// <summary>
    /// Reinicia la puntuación a cero
    /// </summary>
    public void ResetScore()
    {
        score = 0;
        UpdateScoreText();
    }

    /// <summary>
    /// Actualiza el TextMeshPro con la puntuación actual
    /// </summary>
    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    /// <summary>
    /// Obtiene la puntuación actual
    /// </summary>
    public int GetScore()
    {
        return score;
    }
}