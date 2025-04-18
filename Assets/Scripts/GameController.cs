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

    // Estado del juego
    private bool isTransitioningToGame = false;

    private void Awake()
    {
        // Implementaci�n del patr�n Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Este objeto persistir� entre escenas
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

    // Este m�todo se llama autom�ticamente cuando se carga una escena
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
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
                    // LoadRegistrationScene();
                }
            }
            else
            {
                Debug.LogError("No se encontr� el componente Shoot en la escena del juego");
            }
        }
    }

    // M�todo para cargar la escena del juego desde el registro (tras crear/seleccionar un jugador)
    public void LoadGameScene()
    {
        if (PlayerDataManager.Instance == null ||
            PlayerDataManager.Instance.GetCurrentPlayer() == null)
        {
            Debug.LogError("Se intent� cargar la escena del juego sin un jugador activo");
            return;
        }

        isTransitioningToGame = true;
        SceneManager.LoadScene(gameSceneName);
    }

    // M�todo para cargar la escena de registro
    public void LoadRegistrationScene()
    {
        SceneManager.LoadScene(registrationSceneName);
    }

    // M�todo para finalizar el juego y guardar estad�sticas
    public void FinishGame()
    {
        // Buscar el componente Shoot
        Shoot shootComponent = FindObjectOfType<Shoot>();

        if (shootComponent != null)
        {
            // Detener el ciclo y guardar estad�sticas
            shootComponent.StopCycle();

            // Opcionalmente volver a la escena de registro o mostrar resultados
            // LoadRegistrationScene();
        }
    }
}