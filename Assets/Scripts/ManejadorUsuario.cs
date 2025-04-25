using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ManejadorUsuario : MonoBehaviour
{
    [Header("Referencias")]
    public TMP_InputField TxtCodigoUsuario; // Referencia al InputField de TMPro

    // Singleton para acceder desde cualquier script
    private static ManejadorUsuario _instance;
    public static ManejadorUsuario Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ManejadorUsuario>();
                if (_instance == null)
                {
                    Debug.LogError("No se encontró un ManejadorUsuario en la escena.");
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

        // Verificar que tengamos la referencia al campo de texto
        if (TxtCodigoUsuario == null)
        {
            TxtCodigoUsuario = GameObject.Find("TxtCodigoUsuario")?.GetComponent<TMP_InputField>();

            if (TxtCodigoUsuario == null)
            {
                Debug.LogError("No se ha asignado o encontrado el InputField TxtCodigoUsuario.");
            }
        }
    }

    private void Start()
    {
        // Verificar que TelemetriaManager esté disponible
        TelemetriaManager telemetriaManager = FindTelemetriaManager();

        if (telemetriaManager == null)
        {
            Debug.LogWarning("No se encontró un TelemetriaManager. Los datos podrían no guardarse correctamente.");
        }
        else
        {
            telemetriaManager.RegistrarEvento("MANEJO_USUARIO_INICIALIZADO", "ManejadorUsuario inicializado en escena Close");
        }
    }

    /// <summary>
    /// Guarda el código del usuario en el archivo de telemetría
    /// </summary>
    public void GuardarCodigo()
    {
        if (TxtCodigoUsuario == null)
        {
            Debug.LogError("No se puede guardar el código: TxtCodigoUsuario no está asignado.");
            return;
        }

        string codigoUsuario = TxtCodigoUsuario.text.Trim();

        // Si el código está vacío, usar "00" como valor predeterminado
        if (string.IsNullOrWhiteSpace(codigoUsuario))
        {
            codigoUsuario = "00";
            Debug.Log("Código de usuario no proporcionado, se usará el valor predeterminado '00'.");
        }

        // Buscar el TelemetriaManager para registrar el código
        TelemetriaManager telemetriaManager = FindTelemetriaManager();

        if (telemetriaManager != null)
        {
            // Registrar el código del usuario en el TelemetriaManager
            telemetriaManager.RegistrarCodigoUsuario(codigoUsuario);
            telemetriaManager.RegistrarEvento("CODIGO_USUARIO_GUARDADO", $"Código guardado: {codigoUsuario}");
            Debug.Log($"Código de usuario guardado: {codigoUsuario}");
        }
        else
        {
            Debug.LogError("No se pudo guardar el código: TelemetriaManager no encontrado.");
        }
    }

    /// <summary>
    /// Busca el TelemetriaManager en la escena actual o en objetos persistentes
    /// </summary>
    private TelemetriaManager FindTelemetriaManager()
    {
        // Buscar en objetos persistentes
        TelemetriaManager telemetriaManager = FindObjectOfType<TelemetriaManager>();

        return telemetriaManager;
    }
}