using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona la activación y desactivación de botones de audio y el botón de fin
/// según una secuencia y tiempos específicos.
/// </summary>
public class GestorBotonesAudio : MonoBehaviour
{
    [Header("Referencias de botones")]
    public GameObject botonArbol;
    public GameObject botonFogata;
    public GameObject botonRespiracion;
    public GameObject botonFin;

    [Header("Configuración de tiempos")]
    [Tooltip("Tiempo en segundos para la aparición inicial de los botones de audio")]
    public float tiempoAparicionInicial = 25f;

    [Tooltip("Duración de desactivación tras seleccionar el audio de Árbol")]
    public float duracionAudioArbol = 19f;

    [Tooltip("Duración de desactivación tras seleccionar el audio de Fogata")]
    public float duracionAudioFogata = 30f;

    [Tooltip("Duración de desactivación tras seleccionar el audio de Respiración")]
    public float duracionAudioRespiracion = 73f;

    // Variables de control interno
    private bool audioArbolReproducido = false;
    private bool audioFogataReproducido = false;
    private bool audioRespiracionReproducido = false;
    private bool todosLosAudiosReproducidos = false;

    private void Awake()
    {
        // Asegurarse de que todos los botones estén desactivados al inicio
        if (botonArbol != null) botonArbol.SetActive(false);
        if (botonFogata != null) botonFogata.SetActive(false);
        if (botonRespiracion != null) botonRespiracion.SetActive(false);
        if (botonFin != null) botonFin.SetActive(false);
    }

    private void Start()
    {
        // Iniciar la corrutina para mostrar los botones de audio después del tiempo especificado
        StartCoroutine(MostrarBotonesIniciales());

        // Verificar continuamente si todos los audios han sido reproducidos
        StartCoroutine(VerificarEstadoAudios());
    }

    /// <summary>
    /// Muestra los botones de audio después del tiempo de espera inicial
    /// </summary>
    private IEnumerator MostrarBotonesIniciales()
    {
        // Esperar el tiempo configurado
        yield return new WaitForSeconds(tiempoAparicionInicial);

        // Activar los botones de audio
        if (botonArbol != null) botonArbol.SetActive(true);
        if (botonFogata != null) botonFogata.SetActive(true);
        if (botonRespiracion != null) botonRespiracion.SetActive(true);

        Debug.Log("Botones de audio activados después de " + tiempoAparicionInicial + " segundos");
    }

    /// <summary>
    /// Verifica continuamente si todos los audios han sido reproducidos
    /// </summary>
    private IEnumerator VerificarEstadoAudios()
    {
        while (!todosLosAudiosReproducidos)
        {
            // Verificar si todos los audios han sido reproducidos
            if (audioArbolReproducido && audioFogataReproducido && audioRespiracionReproducido && !todosLosAudiosReproducidos)
            {
                todosLosAudiosReproducidos = true;
                MostrarBotonFin();
            }

            // Esperar un poco antes de la siguiente verificación
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// Muestra el botón de fin cuando se han reproducido todos los audios
    /// </summary>
    private void MostrarBotonFin()
    {
        if (botonFin != null)
        {
            botonFin.SetActive(true);
            Debug.Log("Botón de fin activado después de reproducir todos los audios");
        }
    }

    /// <summary>
    /// Función para llamar cuando se selecciona el audio del árbol
    /// </summary>
    public void SeleccionAudioArbol()
    {
        // Marcar como reproducido
        audioArbolReproducido = true;

        // Desactivar los botones
        if (botonArbol != null) botonArbol.SetActive(false);
        if (botonFogata != null) botonFogata.SetActive(false);
        if (botonRespiracion != null) botonRespiracion.SetActive(false);

        Debug.Log("Audio del árbol seleccionado. Botones desactivados durante " + duracionAudioArbol + " segundos");

        // Iniciar la corrutina para reactivar los botones después del tiempo especificado
        StartCoroutine(ReactivarBotonesTrasDuracion(duracionAudioArbol));
    }

    /// <summary>
    /// Función para llamar cuando se selecciona el audio de la fogata
    /// </summary>
    public void SeleccionAudioFogata()
    {
        // Marcar como reproducido
        audioFogataReproducido = true;

        // Desactivar los botones
        if (botonArbol != null) botonArbol.SetActive(false);
        if (botonFogata != null) botonFogata.SetActive(false);
        if (botonRespiracion != null) botonRespiracion.SetActive(false);

        Debug.Log("Audio de la fogata seleccionado. Botones desactivados durante " + duracionAudioFogata + " segundos");

        // Iniciar la corrutina para reactivar los botones después del tiempo especificado
        StartCoroutine(ReactivarBotonesTrasDuracion(duracionAudioFogata));
    }

    /// <summary>
    /// Función para llamar cuando se selecciona el audio de respiración
    /// </summary>
    public void SeleccionAudioRespiracion()
    {
        // Marcar como reproducido
        audioRespiracionReproducido = true;

        // Desactivar los botones
        if (botonArbol != null) botonArbol.SetActive(false);
        if (botonFogata != null) botonFogata.SetActive(false);
        if (botonRespiracion != null) botonRespiracion.SetActive(false);

        Debug.Log("Audio de respiración seleccionado. Botones desactivados durante " + duracionAudioRespiracion + " segundos");

        // Iniciar la corrutina para reactivar los botones después del tiempo especificado
        StartCoroutine(ReactivarBotonesTrasDuracion(duracionAudioRespiracion));
    }

    /// <summary>
    /// Reactivar todos los botones de audio después de la duración especificada
    /// </summary>
    private IEnumerator ReactivarBotonesTrasDuracion(float duracion)
    {
        // Esperar la duración especificada
        yield return new WaitForSeconds(duracion);

        // Reactivar todos los botones, sin importar si ya se han usado
        if (botonArbol != null) botonArbol.SetActive(true);
        if (botonFogata != null) botonFogata.SetActive(true);
        if (botonRespiracion != null) botonRespiracion.SetActive(true);

        Debug.Log("Todos los botones de audio reactivados después de " + duracion + " segundos");
    }
}