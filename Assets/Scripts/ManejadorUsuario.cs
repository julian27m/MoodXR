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
                    Debug.LogError("No se encontr� un ManejadorUsuario en la escena.");
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
        // Verificar que TelemetriaManager est� disponible
        TelemetriaManager telemetriaManager = FindTelemetriaManager();

        if (telemetriaManager == null)
        {
            Debug.LogWarning("No se encontr� un TelemetriaManager. Los datos podr�an no guardarse correctamente.");
        }
        else
        {
            telemetriaManager.RegistrarEvento("MANEJO_USUARIO_INICIALIZADO", "ManejadorUsuario inicializado en escena Close");
        }
    }

    /// <summary>
    /// Guarda el c�digo del usuario en el archivo de telemetr�a
    /// </summary>
    public void GuardarCodigo()
    {
        if (TxtCodigoUsuario == null)
        {
            Debug.LogError("No se puede guardar el c�digo: TxtCodigoUsuario no est� asignado.");
            return;
        }

        string codigoUsuario = TxtCodigoUsuario.text.Trim();

        // Si el c�digo est� vac�o, usar "00" como valor predeterminado
        if (string.IsNullOrWhiteSpace(codigoUsuario))
        {
            codigoUsuario = "00";
            Debug.Log("C�digo de usuario no proporcionado, se usar� el valor predeterminado '00'.");
        }

        // Buscar el TelemetriaManager para registrar el c�digo
        TelemetriaManager telemetriaManager = FindTelemetriaManager();

        if (telemetriaManager != null)
        {
            // Registrar el c�digo del usuario en el TelemetriaManager
            telemetriaManager.RegistrarCodigoUsuario(codigoUsuario);
            telemetriaManager.RegistrarEvento("CODIGO_USUARIO_GUARDADO", $"C�digo guardado: {codigoUsuario}");
            Debug.Log($"C�digo de usuario guardado: {codigoUsuario}");
        }
        else
        {
            Debug.LogError("No se pudo guardar el c�digo: TelemetriaManager no encontrado.");
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