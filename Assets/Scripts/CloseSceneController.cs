using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CloseSceneController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Campo de texto donde el usuario ingresa su c�digo")]
    public TextMeshProUGUI txtCodigoUsuario;

    // Referencia al TelemetriaManager
    private TelemetriaManager telemetriaManager;

    private bool codigoYaGuardado = false;

    private void Awake()
    {
        

        // Buscar el TelemetriaManager
        telemetriaManager = FindObjectOfType<TelemetriaManager>();
        if (telemetriaManager == null)
        {
            Debug.LogWarning("No se ha encontrado el TelemetriaManager. La telemetr�a no ser� registrada.");
        }
        else
        {
            telemetriaManager.RegistrarEvento("ESCENA_CLOSE_INICIADA", "La escena Close ha sido cargada");
        }
    }

    /// <summary>
    /// Guarda el c�digo del usuario en el sistema de telemetr�a
    /// Esta funci�n puede ser llamada desde otros componentes
    /// </summary>
    public void GuardarCodigo()
    {
        if (codigoYaGuardado)
        {
            Debug.Log("El c�digo ya ha sido guardado previamente. Ignorando llamada duplicada.");
            return;
        }
        // Verificar que tengamos el input field
        if (txtCodigoUsuario == null)
        {
            Debug.LogError("No se puede guardar el c�digo: txtCodigoUsuario no est� asignado");
            return;
        }

        // Obtener el c�digo ingresado por el usuario
        string codigo = txtCodigoUsuario.text.Trim();

        // Si el c�digo est� vac�o, usar "00" como valor predeterminado
        if (string.IsNullOrEmpty(codigo))
        {
            codigo = "00";
            Debug.Log("C�digo de usuario no proporcionado, se usar� el valor predeterminado '00'");
        }

        // Verificar que tengamos el TelemetriaManager
        if (telemetriaManager != null)
        {
            // Registrar el c�digo del usuario en la telemetr�a
            telemetriaManager.RegistrarCodigoUsuario(codigo);
            telemetriaManager.RegistrarEvento("CODIGO_USUARIO_GUARDADO", $"C�digo: {codigo}");

            // Logging adicional para depuraci�n
            Debug.Log($"GuardarCodigo - C�digo a registrar: '{codigo}'");

            // Generar un resumen actualizado que incluya el c�digo del usuario
            telemetriaManager.GenerarYGuardarResumen();

            Debug.Log($"C�digo de usuario guardado: {codigo} y resumen generado");

            codigoYaGuardado = true;
        }
        else
        {
            Debug.LogError("No se pudo guardar el c�digo: TelemetriaManager no encontrado");
        }
    }
}