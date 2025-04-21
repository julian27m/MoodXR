using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [Header("Scene Management")]
    [Tooltip("Nombre de la escena que contiene el juego de penalties")]
    public string gameSceneName = "GameScene";

    [Tooltip("Nombre de la escena que contiene el registro de usuarios")]
    public string registrationSceneName = "RegistrationScene";

    [Tooltip("Nombre de la escena que muestra estadísticas y ranking")]
    public string statsRankingSceneName = "StatsRanking";

    [Tooltip("Nombre de la escena tutorial")]
    public string tutorialSceneName = "TutorialScene";

    // Para almacenar temporalmente los resultados de la última partida
    private int lastSessionGolesAtajados = 0;
    private int lastSessionGolesRecibidos = 0;

    // Estado del juego
    private bool isTransitioningToGame = false;
    private bool isTransitioningToStats = false;
    private bool isTransitioningToTutorial = false;

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
    }

    private void Start()
    {
        // Suscribirse al evento de carga de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Desuscribirse del evento de carga de escena
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Este método se llama automáticamente cuando se carga una escena
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Escena cargada: " + scene.name);

        // Si estamos cargando la escena del juego
        if (scene.name == gameSceneName && isTransitioningToGame)
        {
            isTransitioningToGame = false;

            // Buscar el componente Shoot en la escena
            Shoot shootComponent = FindObjectOfType<Shoot>();

            if (shootComponent != null)
            {
                Debug.Log("Componente Shoot encontrado en la escena de juego");

                // Verificar si hay un jugador activo en PlayerDataManager
                if (PlayerDataManager.Instance != null &&
                    PlayerDataManager.Instance.GetCurrentPlayer() != null)
                {
                    Debug.Log("Jugador activo encontrado: " +
                              PlayerDataManager.Instance.GetCurrentPlayerName());

                    // Iniciar el juego
                    shootComponent.RestartCycle();
                }
                else
                {
                    Debug.LogError("No hay un jugador activo en PlayerDataManager");
                    // Opcionalmente volver a la escena de registro
                    LoadRegistrationScene();
                }
            }
            else
            {
                Debug.LogError("No se encontró el componente Shoot en la escena del juego");
            }
        }
        // Si estamos cargando la escena de tutorial
        else if (scene.name == tutorialSceneName && isTransitioningToTutorial)
        {
            isTransitioningToTutorial = false;

            // Buscar el componente TutorialShoot en la escena
            TutorialShoot tutorialShootComponent = FindObjectOfType<TutorialShoot>();

            if (tutorialShootComponent != null)
            {
                Debug.Log("Componente TutorialShoot encontrado en la escena tutorial");

                // Iniciar el tutorial (en caso de que no se haya iniciado automáticamente)
                tutorialShootComponent.RestartCycle();
            }
            else
            {
                Debug.LogError("No se encontró el componente TutorialShoot en la escena tutorial");
            }
        }
        // Si estamos cargando la escena de estadísticas
        else if (scene.name == statsRankingSceneName && isTransitioningToStats)
        {
            isTransitioningToStats = false;

            // Buscar el StatsRankingManager en la escena
            StatsRankingManager statsManager = FindObjectOfType<StatsRankingManager>();

            if (statsManager != null)
            {
                Debug.Log("Componente StatsRankingManager encontrado en la escena de estadísticas");

                // Inicializar la escena con los datos de la última partida
                statsManager.Initialize(lastSessionGolesAtajados, lastSessionGolesRecibidos);
            }
            else
            {
                Debug.LogError("No se encontró el StatsRankingManager en la escena de estadísticas");
            }
        }
    }

    // Método para cargar la escena del juego principal
    public void LoadGameScene()
    {
        if (PlayerDataManager.Instance == null ||
            PlayerDataManager.Instance.GetCurrentPlayer() == null)
        {
            Debug.LogError("Se intentó cargar la escena del juego sin un jugador activo");
            return;
        }

        isTransitioningToGame = true;
        Debug.Log("Cargando escena del juego principal: " + gameSceneName);
        SceneManager.LoadScene(gameSceneName);
    }

    // Método para cargar la escena de tutorial
    public void LoadTutorialScene()
    {
        if (PlayerDataManager.Instance == null ||
            PlayerDataManager.Instance.GetCurrentPlayer() == null)
        {
            Debug.LogError("Se intentó cargar la escena tutorial sin un jugador activo");
            return;
        }

        isTransitioningToTutorial = true;
        Debug.Log("Cargando escena tutorial: " + tutorialSceneName);
        SceneManager.LoadScene(tutorialSceneName);
    }

    // Método para cargar la escena de registro
    public void LoadRegistrationScene()
    {
        Debug.Log("Cargando escena de registro: " + registrationSceneName);
        SceneManager.LoadScene(registrationSceneName);
    }

    // Método para cargar la escena de estadísticas y ranking
    public void LoadStatsRankingScene(int golesAtajados, int golesRecibidos)
    {
        // Guardar temporalmente los resultados para pasarlos a la escena de estadísticas
        lastSessionGolesAtajados = golesAtajados;
        lastSessionGolesRecibidos = golesRecibidos;

        // Guardar en la base de datos
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.UpdateCurrentSessionStats(golesAtajados, golesRecibidos);
            Debug.Log("Estadísticas finales guardadas en la base de datos antes de cargar escena de estadísticas.");
        }

        isTransitioningToStats = true;
        Debug.Log("Cargando escena de estadísticas: " + statsRankingSceneName);
        SceneManager.LoadScene(statsRankingSceneName);
    }

    // Método para finalizar el juego y guardar estadísticas
    public void FinishGame(int golesAtajados, int golesRecibidos)
    {
        // Buscar el componente Shoot
        Shoot shootComponent = FindObjectOfType<Shoot>();

        if (shootComponent != null)
        {
            // Detener el ciclo
            shootComponent.StopCycle();
        }

        // Cargar la escena de estadísticas
        LoadStatsRankingScene(golesAtajados, golesRecibidos);
    }
}