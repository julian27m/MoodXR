using System.Collections;
using UnityEngine;
using TMPro;

public class CloseSceneControllerAnger : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Campo de texto donde el usuario ingresa su c�digo")]
    public TextMeshProUGUI txtCodigoUsuario;

    // Referencia al TelemetriaManagerAnger
    private TelemetriaManagerAnger telemetriaManager;

    private void Awake()
    {
        // Buscar el TelemetriaManagerAnger
        telemetriaManager = FindObjectOfType<TelemetriaManagerAnger>();
        if (telemetriaManager == null)
        {
            Debug.LogWarning("No se ha encontrado el TelemetriaManagerAnger. La telemetr�a no ser� registrada.");
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

        // Verificar que tengamos el TelemetriaManagerAnger
        if (telemetriaManager != null)
        {
            // Registrar el c�digo del usuario en la telemetr�a
            telemetriaManager.RegistrarCodigoUsuario(codigo);
            telemetriaManager.RegistrarEvento("CODIGO_USUARIO_GUARDADO", $"C�digo: {codigo}");

            // Generar un resumen actualizado que incluya el c�digo del usuario
            telemetriaManager.GenerarResumenFinal();

            Debug.Log($"C�digo de usuario guardado: {codigo} y resumen generado");
        }
        else
        {
            Debug.LogError("No se pudo guardar el c�digo: TelemetriaManagerAnger no encontrado");
        }
    }
}