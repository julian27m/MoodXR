using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    [SerializeField] private float transitionDelay = 0.5f; // Tiempo de espera antes de cambiar de escena

    // Singleton para acceder desde cualquier script
    private static SceneController _instance;
    public static SceneController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SceneController>();
                if (_instance == null)
                {
                    Debug.LogError("No se encontró un SceneController en la escena.");
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
            DontDestroyOnLoad(this.gameObject);
        }
    }

    /// <summary>
    /// Cambia a la escena especificada por su nombre
    /// </summary>
    /// <param name="sceneName">Nombre de la escena a cargar</param>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneDelayed(sceneName));
    }

    /// <summary>
    /// Cambia a la escena Close (cierre de la experiencia) 
    /// </summary>
    public void LoadCloseScene()
    {
        LoadScene("Close");
    }

    /// <summary>
    /// Corrutina para cambiar de escena con un pequeño retraso
    /// </summary>
    private IEnumerator LoadSceneDelayed(string sceneName)
    {
        // Verificar si tenemos un TelemetriaManager antes de cambiar de escena
        TelemetriaManager telemetriaManager = FindObjectOfType<TelemetriaManager>();

        if (telemetriaManager != null)
        {
            // Registrar el cambio de escena
            telemetriaManager.RegistrarEvento("CAMBIO_ESCENA", $"Cambio a: {sceneName}");
            telemetriaManager.ForzarGuardado();
        }
        else
        {
            Debug.LogWarning("No se encontró un TelemetriaManager al cambiar de escena");
        }

        // Esperar el tiempo de retraso
        yield return new WaitForSeconds(transitionDelay);

        // Cargar la nueva escena
        SceneManager.LoadScene(sceneName);
    }
}