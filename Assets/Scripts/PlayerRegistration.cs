using UnityEngine;
using TMPro;

public class PlayerRegistration : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("TextMeshPro que contiene el nombre ingresado por el usuario")]
    public TextMeshProUGUI textoUsuario;

    [Tooltip("TextMeshPro para mostrar mensajes de error o ayuda")]
    public TextMeshProUGUI textoAyuda;

    [Tooltip("Objeto que contiene el diálogo de registro que desaparecerá tras un registro exitoso")]
    public GameObject DialogoRegistroNombre;

    [Tooltip("Objeto que contiene el diálogo de login para usuarios existentes")]
    public GameObject DialogoLoginNombre;

    [Tooltip("Objeto que contiene el diálogo de instrucciones")]
    public GameObject DialogoInstrucciones;

    [Tooltip("TextMeshPro que mostrará el nombre del usuario en la pantalla de login")]
    public TextMeshProUGUI textoUsuarioLogin;

    [Header("Settings")]
    [Tooltip("Longitud máxima permitida para el nombre")]
    public int maxNameLength = 20;

    private void Start()
    {
        // Verificar que tengamos todas las referencias necesarias
        if (textoUsuario == null || textoAyuda == null)
        {
            Debug.LogError("Faltan referencias en el script PlayerRegistration. Por favor, configura todas las referencias en el Inspector.");
        }

        // Ocultar el mensaje de ayuda al inicio
        if (textoAyuda != null)
        {
            textoAyuda.gameObject.SetActive(false);
        }

        // Asegurarse que el diálogo de login esté desactivado al inicio
        if (DialogoLoginNombre != null)
        {
            DialogoLoginNombre.SetActive(false);
        }

        // Asegurarse que el diálogo de instrucciones esté desactivado al inicio
        if (DialogoInstrucciones != null)
        {
            DialogoInstrucciones.SetActive(false);
        }
    }

    // Método público que será llamado desde el botón en el inspector
    public void OnGuardarButtonClicked()
    {
        // Verificar que tengamos la referencia al texto del usuario
        if (textoUsuario == null)
        {
            Debug.LogError("No se ha asignado la referencia al TextMeshPro del usuario");
            return;
        }

        // Obtener el nombre ingresado y eliminar espacios en blanco al inicio y final
        string playerName = textoUsuario.text.Trim();

        // Verificación 1: Comprobar que no esté vacío
        if (string.IsNullOrEmpty(playerName) || string.IsNullOrWhiteSpace(playerName))
        {
            ShowError("Tu nombre no puede estar vacío.");
            return;
        }

        // Verificación 2: Comprobar longitud máxima
        if (playerName.Length > maxNameLength)
        {
            ShowError($"Tu nombre no puede tener más de {maxNameLength} caracteres.");
            return;
        }

        // Verificación 3: Comprobar si el usuario ya existe (para mostrar pantalla de login)
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.PlayerExists(playerName))
        {
            // Mostrar diálogo de login en lugar de error
            ShowLoginDialog(playerName);
            return;
        }

        // Si llegamos aquí, el nombre es válido y nuevo - crear el jugador
        if (PlayerDataManager.Instance != null)
        {
            bool success = PlayerDataManager.Instance.CreateNewPlayer(playerName); // Método modificado que inicia con 1 partida

            if (success)
            {
                // Éxito al crear el jugador
                Debug.Log($"Jugador '{playerName}' registrado exitosamente");

                // Ocultar mensaje de error/ayuda
                if (textoAyuda != null)
                {
                    textoAyuda.gameObject.SetActive(false);
                }

                // Desactivar el diálogo de registro tras registro exitoso
                if (DialogoRegistroNombre != null)
                {
                    DialogoRegistroNombre.SetActive(false);
                    Debug.Log("Diálogo de registro ocultado tras registro exitoso");
                }
                else
                {
                    Debug.LogWarning("No se ha asignado la referencia al DialogoRegistroNombre");
                }

                // Activar dialogo con las instrucciones
                if (DialogoInstrucciones != null)
                {
                    DialogoInstrucciones.SetActive(true);
                }
            }
            else
            {
                // Error al crear el jugador (aunque este caso es improbable si ya pasamos las validaciones)
                ShowError("Error al registrar el jugador. Inténtalo de nuevo.");
            }
        }
        else
        {
            Debug.LogError("No se pudo encontrar el PlayerDataManager en la escena. Asegúrate de que existe.");
        }
    }

    // Método para mostrar el diálogo de login para usuarios existentes
    private void ShowLoginDialog(string playerName)
    {
        // Ocultar diálogo de registro
        if (DialogoRegistroNombre != null)
        {
            DialogoRegistroNombre.SetActive(false);
        }

        // Mostrar diálogo de login
        if (DialogoLoginNombre != null)
        {
            DialogoLoginNombre.SetActive(true);
        }

        // Actualizar el texto con el nombre de usuario
        if (textoUsuarioLogin != null)
        {
            textoUsuarioLogin.text = playerName;
        }

        Debug.Log($"Se mostró el diálogo de login para el usuario existente: '{playerName}'");
    }

    // Método para el botón de login (continuar con usuario existente)
    public void OnLoginButtonClicked()
    {
        // Verificar que tengamos la referencia al texto del usuario de login
        if (textoUsuarioLogin == null)
        {
            Debug.LogError("No se ha asignado la referencia al TextMeshPro del usuario de login");
            return;
        }

        // Obtener el nombre desde el texto de login
        string playerName = textoUsuarioLogin.text.Trim();

        if (PlayerDataManager.Instance != null)
        {
            // Seleccionar el jugador e incrementar sus partidas jugadas
            bool success = PlayerDataManager.Instance.LoginExistingPlayer(playerName);

            if (success)
            {
                // Éxito al iniciar sesión con el jugador existente
                Debug.Log($"Sesión iniciada con éxito para el jugador '{playerName}'");

                // Ocultar el diálogo de login
                if (DialogoLoginNombre != null)
                {
                    DialogoLoginNombre.SetActive(false);
                }

                // Activar dialogo con las instrucciones
                if (DialogoInstrucciones != null)
                {
                    DialogoInstrucciones.SetActive(true);
                }
            }
            else
            {
                Debug.LogError($"Error al iniciar sesión con el jugador '{playerName}'");

                // Volver a la pantalla de registro como fallback
                if (DialogoLoginNombre != null)
                {
                    DialogoLoginNombre.SetActive(false);
                }

                if (DialogoRegistroNombre != null)
                {
                    DialogoRegistroNombre.SetActive(true);
                }

                ShowError("Error al iniciar sesión. Inténtalo de nuevo.");
            }
        }
        else
        {
            Debug.LogError("No se pudo encontrar el PlayerDataManager en la escena");
        }
    }

    // Método para mostrar un mensaje de error/ayuda
    private void ShowError(string message)
    {
        if (textoAyuda != null)
        {
            textoAyuda.text = message;
            textoAyuda.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("No se ha asignado la referencia al TextMeshPro de ayuda");
        }
    }

    // Método de utilidad para probar la lista de jugadores
    public void TestDatabase()
    {
        if (PlayerDataManager.Instance != null)
        {
            var players = PlayerDataManager.Instance.GetRanking();
            Debug.Log($"Total de jugadores registrados: {players.Count}");

            foreach (var player in players)
            {
                // Obtener los totales acumulados de todas las partidas
                int totalGolesAtajados = 0;
                int totalGolesRecibidos = 0;

                // Recorrer todas las partidas del jugador
                foreach (var partida in player.partidasJugadas.Values)
                {
                    totalGolesAtajados += partida.golesAtajados;
                    totalGolesRecibidos += partida.golesRecibidos;
                }

                Debug.Log($"Jugador: '{player.playerName}', " +
                          $"Partidas jugadas: {player.partidasJugadas.Count}, " +
                          $"Total goles atajados: {totalGolesAtajados}, " +
                          $"Total goles recibidos: {totalGolesRecibidos}");
            }
        }
        else
        {
            Debug.LogError("No se pudo encontrar el PlayerDataManager en la escena");
        }
    }

    // Método para borrar la base de datos completa (puedes asignarlo a un botón de administrador)
    public void DeleteAllData()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.DeleteAllData();
            Debug.Log("Base de datos eliminada completamente");
        }
        else
        {
            Debug.LogError("No se pudo encontrar el PlayerDataManager en la escena");
        }
    }

    // Método para iniciar el juego (asígnalo al botón de "Jugar" en el diálogo de instrucciones)
    public void StartGame()
    {
        // Verificar si hay un jugador actual seleccionado
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.GetCurrentPlayer() != null)
        {
            // Cerrar diálogos actuales
            if (DialogoInstrucciones != null)
            {
                DialogoInstrucciones.SetActive(false);
            }

            // Usar el GameController para cargar la escena de juego
            if (GameController.Instance != null)
            {
                GameController.Instance.LoadGameScene();
            }
            else
            {
                Debug.LogError("No se encontró el GameController en la escena");
            }
        }
        else
        {
            Debug.LogError("No hay un jugador seleccionado para iniciar el juego");
            ShowError("Error: No hay un jugador seleccionado para iniciar el juego");
        }
    }
}