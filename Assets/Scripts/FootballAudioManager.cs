using System.Collections;
using UnityEngine;

public class FootballAudioManager : MonoBehaviour
{
    public static FootballAudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("Audio source para la afici�n (debe estar en loop)")]
    public AudioSource crowdAudioSource;

    [Header("Audio Clips")]
    [Tooltip("Sonido cuando se marca un gol")]
    public AudioClip goalSound;

    [Tooltip("Sonido cuando el bal�n golpea el poste")]
    public AudioClip postSound;

    [Tooltip("Sonido cuando el arquero ataja el bal�n")]
    public AudioClip saveSound;

    [Tooltip("Sonido de la patada al lanzar el bal�n")]
    public AudioClip kickSound;

    [Header("Volumen de Afici�n")]
    [Tooltip("Volumen est�ndar de la afici�n")]
    [Range(0f, 1f)]
    public float standardCrowdVolume = 0.5f;

    [Tooltip("Volumen de la afici�n cuando el arquero ataja")]
    [Range(0f, 1f)]
    public float highCrowdVolume = 0.8f;

    [Tooltip("Volumen de la afici�n cuando le hacen gol al arquero")]
    [Range(0f, 1f)]
    public float lowCrowdVolume = 0.3f;

    [Tooltip("Tiempo que tarda en cambiar el volumen de la afici�n")]
    public float volumeTransitionTime = 1f;

    private AudioSource effectsAudioSource;
    private Coroutine crowdVolumeCoroutine;

    private void Awake()
    {
        // Implementaci�n del patr�n Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Crear un AudioSource para efectos de sonido
        effectsAudioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        // Verificar que tenemos todas las referencias necesarias
        if (crowdAudioSource == null)
        {
            Debug.LogError("Crowd AudioSource no asignado en FootballAudioManager");
            return;
        }

        // Configurar el audio de la afici�n
        crowdAudioSource.volume = standardCrowdVolume;

        // Asegurarse de que el audio de afici�n est� en loop
        if (!crowdAudioSource.loop)
        {
            Debug.LogWarning("El AudioSource de la afici�n deber�a estar configurado en loop. Configurando autom�ticamente.");
            crowdAudioSource.loop = true;
        }

        // Iniciar el audio de la afici�n si no est� reproduciendo
        if (!crowdAudioSource.isPlaying)
        {
            crowdAudioSource.Play();
        }
    }

    // M�todo para reproducir el sonido de gol
    public void PlayGoalSound()
    {
        if (goalSound != null)
        {
            PlayOneShot(goalSound);
            ChangeCrowdVolume(lowCrowdVolume);
        }
    }

    // M�todo para reproducir el sonido de poste
    public void PlayPostSound()
    {
        if (postSound != null)
        {
            PlayOneShot(postSound);
        }
    }

    // M�todo para reproducir el sonido de atajada
    public void PlaySaveSound()
    {
        if (saveSound != null)
        {
            PlayOneShot(saveSound);
            ChangeCrowdVolume(highCrowdVolume);
        }
    }

    // M�todo para reproducir el sonido de patada
    public void PlayKickSound()
    {
        if (kickSound != null)
        {
            PlayOneShot(kickSound);
        }
    }

    // M�todo para restaurar el volumen est�ndar de la afici�n
    public void RestoreStandardCrowdVolume()
    {
        ChangeCrowdVolume(standardCrowdVolume);
    }

    // M�todo para cambiar el volumen de la afici�n con transici�n suave
    private void ChangeCrowdVolume(float targetVolume)
    {
        // Detener la corrutina anterior si existe
        if (crowdVolumeCoroutine != null)
        {
            StopCoroutine(crowdVolumeCoroutine);
        }

        // Iniciar la nueva transici�n
        crowdVolumeCoroutine = StartCoroutine(ChangeCrowdVolumeCoroutine(targetVolume));
    }

    // Corrutina para cambiar el volumen gradualmente
    private IEnumerator ChangeCrowdVolumeCoroutine(float targetVolume)
    {
        float startVolume = crowdAudioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < volumeTransitionTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / volumeTransitionTime;
            crowdAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        crowdAudioSource.volume = targetVolume;
        crowdVolumeCoroutine = null;
    }

    // M�todo para reproducir un efecto de sonido
    private void PlayOneShot(AudioClip clip)
    {
        effectsAudioSource.PlayOneShot(clip);
    }
}