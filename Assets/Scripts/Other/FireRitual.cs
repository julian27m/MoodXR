using UnityEngine;

public class FireRitual : MonoBehaviour
{
    [Header("Fire Settings")]
    public GameObject touchParticleFlame;
    public AudioSource fireAudioSource;
    
    //[Header("Telemetry")]
    //public TelemetryManager telemetry;
    
    [SerializeField]
    private int totalSpots = 4;
    private int occupiedSpots = 0;
    private bool fireIsLit = false;
    private float ritualStartTime = 0f;
    private float ritualCompletionTime = 0f;

    private void Start()
    {
        // Asegurarnos de que la fogata esté apagada al inicio
        touchParticleFlame.SetActive(false);
        
        // Si estás usando telemetría, puedes inicializar aquí
        //if (telemetry != null)
        //{
        //    telemetry.LogEvent("FireRitualStarted", 0, 0);
        //}
    }

    public void UpdateRockCount(int change)
    {
        occupiedSpots += change;
        
        // Asegurarnos de que no haya valores negativos o mayores al total
        occupiedSpots = Mathf.Clamp(occupiedSpots, 0, totalSpots);
        
        // Registrar en telemetría
        //if (telemetry != null)
        //{
        //    telemetry.LogEvent("RockPlaced", occupiedSpots, Time.time);
        //}
        
        // Verificar si todos los spots están ocupados para encender la fogata
        CheckFireStatus();
    }

    private void CheckFireStatus()
    {
        // Si todos los spots están ocupados, encender la fogata
        if (occupiedSpots >= totalSpots && !fireIsLit)
        {
            LightFire();
        }
        // Si falta algún spot y la fogata está encendida, apagarla
        else if (occupiedSpots < totalSpots && fireIsLit)
        {
            ExtinguishFire();
        }
    }

    private void LightFire()
    {
        fireIsLit = true;
        touchParticleFlame.SetActive(true);
        
        if (fireAudioSource != null && !fireAudioSource.isPlaying)
        {
            fireAudioSource.Play();
        }
        
        // Registrar el tiempo de completado para telemetría
        ritualCompletionTime = Time.time;
        float duration = ritualCompletionTime - ritualStartTime;
        
        //if (telemetry != null)
        //{
        //    telemetry.LogEvent("FireLit", 1, duration);
        //}
        
        // Aquí podrías activar el audio de diálogo sobre la transformación
        PlayTransformationDialog();
    }

    private void ExtinguishFire()
    {
        fireIsLit = false;
        touchParticleFlame.SetActive(false);
        
        if (fireAudioSource != null && fireAudioSource.isPlaying)
        {
            fireAudioSource.Stop();
        }
        
        // Reiniciar el tiempo para la siguiente vez
        ritualStartTime = Time.time;
        
        //if (telemetry != null)
        //{
        //    telemetry.LogEvent("FireExtinguished", 0, Time.time);
        //}
    }
    
    private void PlayTransformationDialog()
    {
        // Aquí puedes reproducir el audio de la narración sobre la transformación
        // Por ejemplo:
        // transformationAudioSource.Play();
        
        // Para fines de prueba, puedes usar Debug.Log
        Debug.Log("Mientras enciendes esta fogata, observa cómo el fuego transforma la madera en luz y calor. Del mismo modo, nuestras dificultades pueden transformarse en sabiduría y fortaleza.");
    }
}