using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona la activaci�n y desactivaci�n de botones de audio y el bot�n de fin
/// seg�n una secuencia y tiempos espec�ficos.
/// </summary>
public class GestorBotonesAudio : MonoBehaviour
{
    [Header("Referencias de botones")]
    public GameObject botonArbol;
    public GameObject botonFogata;
    public GameObject botonRespiracion;
    public GameObject botonFin;

    [Header("Configuraci�n de tiempos")]
    [Tooltip("Tiempo en segundos para la aparici�n inicial de los botones de audio")]
    public float tiempoAparicionInicial = 25f;

    [Tooltip("Duraci�n de desactivaci�n tras seleccionar el audio de �rbol")]
    public float duracionAudioArbol = 19f;

    [Tooltip("Duraci�n de desactivaci�n tras seleccionar el audio de Fogata")]
    public float duracionAudioFogata = 30f;

    [Tooltip("Duraci�n de desactivaci�n tras seleccionar el audio de Respiraci�n")]
    public float duracionAudioRespiracion = 73f;

    // Variables de control interno
    private bool audioArbolReproducido = false;
    private bool audioFogataReproducido = false;
    private bool audioRespiracionReproducido = false;
    private bool todosLosAudiosReproducidos = false;

    private void Awake()
    {
        // Asegurarse de que todos los botones est�n desactivados al inicio
        if (botonArbol != null) botonArbol.SetActive(false);
        if (botonFogata != null) botonFogata.SetActive(false);
        if (botonRespiracion != null) botonRespiracion.SetActive(false);
        if (botonFin != null) botonFin.SetActive(false);
    }

    private void Start()
    {
        // Iniciar la corrutina para mostrar los botones de audio despu�s del tiempo especificado
        StartCoroutine(MostrarBotonesIniciales());

        // Verificar continuamente si todos los audios han sido reproducidos
        StartCoroutine(VerificarEstadoAudios());
    }

    /// <summary>
    /// Muestra los botones de audio despu�s del tiempo de espera inicial
    /// </summary>
    private IEnumerator MostrarBotonesIniciales()
    {
        // Esperar el tiempo configurado
        yield return new WaitForSeconds(tiempoAparicionInicial);

        // Activar los botones de audio
        if (botonArbol != null) botonArbol.SetActive(true);
        if (botonFogata != null) botonFogata.SetActive(true);
        if (botonRespiracion != null) botonRespiracion.SetActive(true);

        Debug.Log("Botones de audio activados despu�s de " + tiempoAparicionInicial + " segundos");
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

            // Esperar un poco antes de la siguiente verificaci�n
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// Muestra el bot�n de fin cuando se han reproducido todos los audios
    /// </summary>
    private void MostrarBotonFin()
    {
        if (botonFin != null)
        {
            botonFin.SetActive(true);
            Debug.Log("Bot�n de fin activado despu�s de reproducir todos los audios");
        }
    }

    /// <summary>
    /// Funci�n para llamar cuando se selecciona el audio del �rbol
    /// </summary>
    public void SeleccionAudioArbol()
    {
        // Marcar como reproducido
        audioArbolReproducido = true;

        // Desactivar los botones
        if (botonArbol != null) botonArbol.SetActive(false);
        if (botonFogata != null) botonFogata.SetActive(false);
        if (botonRespiracion != null) botonRespiracion.SetActive(false);

        Debug.Log("Audio del �rbol seleccionado. Botones desactivados durante " + duracionAudioArbol + " segundos");

        // Iniciar la corrutina para reactivar los botones despu�s del tiempo especificado
        StartCoroutine(ReactivarBotonesTrasDuracion(duracionAudioArbol));
    }

    /// <summary>
    /// Funci�n para llamar cuando se selecciona el audio de la fogata
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

        // Iniciar la corrutina para reactivar los botones despu�s del tiempo especificado
        StartCoroutine(ReactivarBotonesTrasDuracion(duracionAudioFogata));
    }

    /// <summary>
    /// Funci�n para llamar cuando se selecciona el audio de respiraci�n
    /// </summary>
    public void SeleccionAudioRespiracion()
    {
        // Marcar como reproducido
        audioRespiracionReproducido = true;

        // Desactivar los botones
        if (botonArbol != null) botonArbol.SetActive(false);
        if (botonFogata != null) botonFogata.SetActive(false);
        if (botonRespiracion != null) botonRespiracion.SetActive(false);

        Debug.Log("Audio de respiraci�n seleccionado. Botones desactivados durante " + duracionAudioRespiracion + " segundos");

        // Iniciar la corrutina para reactivar los botones despu�s del tiempo especificado
        StartCoroutine(ReactivarBotonesTrasDuracion(duracionAudioRespiracion));
    }

    /// <summary>
    /// Reactivar todos los botones de audio despu�s de la duraci�n especificada
    /// </summary>
    private IEnumerator ReactivarBotonesTrasDuracion(float duracion)
    {
        // Esperar la duraci�n especificada
        yield return new WaitForSeconds(duracion);

        // Reactivar todos los botones, sin importar si ya se han usado
        if (botonArbol != null) botonArbol.SetActive(true);
        if (botonFogata != null) botonFogata.SetActive(true);
        if (botonRespiracion != null) botonRespiracion.SetActive(true);

        Debug.Log("Todos los botones de audio reactivados despu�s de " + duracion + " segundos");
    }
}