using System.Collections;
using UnityEngine;
using TMPro;

public class CloseSceneControllerAnger : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Campo de texto donde el usuario ingresa su código")]
    public TextMeshProUGUI txtCodigoUsuario;

    // Referencia al TelemetriaManagerAnger
    private TelemetriaManagerAnger telemetriaManager;

    private bool codigoYaGuardado = false;

    private void Awake()
    {
        // Buscar el TelemetriaManagerAnger
        telemetriaManager = FindObjectOfType<TelemetriaManagerAnger>();
        if (telemetriaManager == null)
        {
            Debug.LogWarning("No se ha encontrado el TelemetriaManagerAnger. La telemetría no será registrada.");
        }
        else
        {
            telemetriaManager.RegistrarEvento("ESCENA_CLOSE_INICIADA", "La escena Close ha sido cargada");
        }
    }

    /// <summary>
    /// Guarda el código del usuario en el sistema de telemetría
    /// Esta función puede ser llamada desde otros componentes
    /// </summary>
    public void GuardarCodigo()
    {
        if (codigoYaGuardado)
        {
            Debug.Log("El código ya ha sido guardado previamente. Ignorando llamada duplicada.");
            return;
        }

        // Verificar que tengamos el input field
        if (txtCodigoUsuario == null)
        {
            Debug.LogError("No se puede guardar el código: txtCodigoUsuario no está asignado");
            return;
        }

        // Obtener el código ingresado por el usuario
        string codigo = txtCodigoUsuario.text.Trim();

        // Si el código está vacío, usar "00" como valor predeterminado
        if (string.IsNullOrEmpty(codigo))
        {
            codigo = "00";
            Debug.Log("Código de usuario no proporcionado, se usará el valor predeterminado '00'");
        }

        // Verificar que tengamos el TelemetriaManagerAnger
        if (telemetriaManager != null)
        {
            // Registrar el código del usuario en la telemetría
            telemetriaManager.RegistrarCodigoUsuario(codigo);
            telemetriaManager.RegistrarEvento("CODIGO_USUARIO_GUARDADO", $"Código: {codigo}");

            // Logging adicional para depuración
            Debug.Log($"GuardarCodigo - Código a registrar: '{codigo}'");

            // Generar un resumen actualizado que incluya el código del usuario
            telemetriaManager.GenerarYGuardarResumen();

            Debug.Log($"Código de usuario guardado: {codigo} y resumen generado");

            codigoYaGuardado = true;
        }
        else
        {
            Debug.LogError("No se pudo guardar el código: TelemetriaManagerAnger no encontrado");
        }
    }
}