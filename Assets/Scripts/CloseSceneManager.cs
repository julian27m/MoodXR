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
                Debug.LogError("No se ha asignado o encontrado el input field para el c�digo de usuario");
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
            Debug.LogError("No se encontr� un TelemetriaManager activo");
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
    /// Env�a el c�digo del usuario al sistema de telemetr�a
    /// </summary>
    public void SubmitCodigo()
    {
        if (manejadorUsuario != null)
        {
            manejadorUsuario.GuardarCodigo();

            // Mostrar mensaje de confirmaci�n
            if (mensajeConfirmacion != null)
            {
                mensajeConfirmacion.text = "�C�digo registrado correctamente!";
                mensajeConfirmacion.gameObject.SetActive(true);
            }

            // Registrar evento en la telemetr�a
            if (telemetriaManager != null)
            {
                telemetriaManager.RegistrarEvento("CODIGO_CONFIRMADO", "El usuario confirm� su c�digo");
            }

            StartCoroutine(OcultarMensajeConfirmacion(3f));
        }
        else
        {
            Debug.LogError("No se encontr� el ManejadorUsuario para guardar el c�digo");
        }
    }

    /// <summary>
    /// Cierra la aplicaci�n
    /// </summary>
    public void ExitApplication()
    {
        // Asegurarse de guardar el c�digo del usuario si existe
        if (manejadorUsuario != null && telemetriaManager != null && codigoUsuarioInput != null)
        {
            string codigo = codigoUsuarioInput.text.Trim();
            if (!string.IsNullOrEmpty(codigo))
            {
                manejadorUsuario.GuardarCodigo();
            }

            // Registrar evento de cierre
            telemetriaManager.RegistrarEvento("CIERRE_APLICACION", "El usuario cerr� la aplicaci�n desde el bot�n");
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

        Debug.Log("Cerrando aplicaci�n...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}