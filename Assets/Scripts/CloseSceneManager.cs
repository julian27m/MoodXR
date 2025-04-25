using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CloseSceneManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TMP_InputField codigoUsuarioInput;
    [SerializeField] private Button btnSubmit;
    [SerializeField] private Button btnExit;
    [SerializeField] private TMP_Text mensajeConfirmacion;

    private ManejadorUsuario manejadorUsuario;
    private TelemetriaManager telemetriaManager;

    private void Awake()
    {
        // Intentar encontrar componentes
        if (codigoUsuarioInput == null)
        {
            codigoUsuarioInput = GameObject.Find("TxtCodigoUsuario")?.GetComponent<TMP_InputField>();
            if (codigoUsuarioInput == null)
            {
                Debug.LogError("No se ha asignado o encontrado el input field para el código de usuario");
            }
        }

        // Inicializar ManejadorUsuario
        GameObject manejadorObj = new GameObject("ManejadorUsuario");
        manejadorUsuario = manejadorObj.AddComponent<ManejadorUsuario>();
        manejadorUsuario.TxtCodigoUsuario = codigoUsuarioInput;

        // Buscar el TelemetriaManager
        telemetriaManager = FindObjectOfType<TelemetriaManager>();
        if (telemetriaManager == null)
        {
            Debug.LogError("No se encontró un TelemetriaManager activo");
        }
        else
        {
            // Registrar el evento de llegada a la escena Close
            telemetriaManager.RegistrarEvento("ESCENA_CLOSE", "Ingreso a la escena de cierre");
        }
    }

    private void Start()
    {
        if (mensajeConfirmacion != null)
        {
            mensajeConfirmacion.gameObject.SetActive(false);
        }

        // Asignar eventos a los botones
        if (btnSubmit != null)
        {
            btnSubmit.onClick.AddListener(SubmitCodigo);
        }

        if (btnExit != null)
        {
            btnExit.onClick.AddListener(ExitApplication);
        }
    }

    /// <summary>
    /// Envía el código del usuario al sistema de telemetría
    /// </summary>
    public void SubmitCodigo()
    {
        if (manejadorUsuario != null)
        {
            manejadorUsuario.GuardarCodigo();

            // Mostrar mensaje de confirmación
            if (mensajeConfirmacion != null)
            {
                mensajeConfirmacion.text = "¡Código registrado correctamente!";
                mensajeConfirmacion.gameObject.SetActive(true);
            }

            // Registrar evento en la telemetría
            if (telemetriaManager != null)
            {
                telemetriaManager.RegistrarEvento("CODIGO_CONFIRMADO", "El usuario confirmó su código");
            }

            StartCoroutine(OcultarMensajeConfirmacion(3f));
        }
        else
        {
            Debug.LogError("No se encontró el ManejadorUsuario para guardar el código");
        }
    }

    /// <summary>
    /// Cierra la aplicación
    /// </summary>
    public void ExitApplication()
    {
        // Asegurarse de guardar el código del usuario si existe
        if (manejadorUsuario != null && telemetriaManager != null && codigoUsuarioInput != null)
        {
            string codigo = codigoUsuarioInput.text.Trim();
            if (!string.IsNullOrEmpty(codigo))
            {
                manejadorUsuario.GuardarCodigo();
            }

            // Registrar evento de cierre
            telemetriaManager.RegistrarEvento("CIERRE_APLICACION", "El usuario cerró la aplicación desde el botón");
            telemetriaManager.ForzarGuardado();
        }

        // Esperar un momento para asegurar que los datos se guarden
        StartCoroutine(SalirConRetraso(1f));
    }

    private IEnumerator OcultarMensajeConfirmacion(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (mensajeConfirmacion != null)
        {
            mensajeConfirmacion.gameObject.SetActive(false);
        }
    }

    private IEnumerator SalirConRetraso(float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log("Cerrando aplicación...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}