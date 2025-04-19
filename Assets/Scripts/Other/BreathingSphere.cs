using System.Collections;
using UnityEngine;

public class BreathingSphere : MonoBehaviour
{
    [Header("Sphere Settings")]
    public Transform sphere;
    public Light sphereLight;
    public float initialScale = 10f;
    public float maxScale = 15f;
    public float minLightIntensity = 0.5f;
    public float maxLightIntensity = 1.5f;


    [Header("Timing Settings")]
    public float initialDelay = 9.0f; // Espera inicial antes de comenzar
    public float inhaleTime = 7.0f;
    public float exhaleTime = 9.0f;
    public int totalBreaths = 10;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip[] inhaleClips;
    public AudioClip[] exhaleClips;
    public AudioClip finalAudio;

    private Vector3 initialSphereScale;
    private bool isBreathingActive = false;
    private int currentBreath = 0;

    private void Start()
    {
        if (sphere != null)
        {
            initialSphereScale = new Vector3(initialScale, initialScale, initialScale);
            sphere.localScale = initialSphereScale;
        }

        if (sphereLight != null)
        {
            sphereLight.intensity = minLightIntensity;
        }
    }

    public void ActivateBreathing()
    {
        if (!isBreathingActive)
        {
            isBreathingActive = true;
            currentBreath = 0;
            StartCoroutine(StartBreathingWithDelay());
        }
    }

    private IEnumerator StartBreathingWithDelay()
    {
        // Esperar el tiempo inicial antes de comenzar
        yield return new WaitForSeconds(initialDelay);

        // Comenzar el ciclo de respiración
        yield return StartCoroutine(BreathingCycle());
    }

    private IEnumerator BreathingCycle()
    {
        while (currentBreath < totalBreaths && isBreathingActive)
        {
            yield return StartCoroutine(Inhale());
            yield return StartCoroutine(Exhale());

            currentBreath++;
        }

        if (finalAudio != null && audioSource != null)
        {
            audioSource.clip = finalAudio;
            audioSource.Play();
        }

        isBreathingActive = false;
    }

    private IEnumerator Inhale()
    {
        if (currentBreath < 4 && inhaleClips.Length > 0 && audioSource != null)
        {
            audioSource.clip = inhaleClips[currentBreath];
            audioSource.Play();
        }

        float elapsedTime = 0f;
        Vector3 startScale = sphere.localScale;
        float startLightIntensity = sphereLight.intensity;

        while (elapsedTime < inhaleTime)
        {
            float t = elapsedTime / inhaleTime;
            t = Mathf.SmoothStep(0, 1, t);

            float currentScale = Mathf.Lerp(initialScale, maxScale, t);
            sphere.localScale = new Vector3(currentScale, currentScale, currentScale);

            if (sphereLight != null)
            {
                sphereLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, t);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        sphere.localScale = new Vector3(maxScale, maxScale, maxScale);
        if (sphereLight != null)
        {
            sphereLight.intensity = maxLightIntensity;
        }
    }

    private IEnumerator Exhale()
    {
        if (currentBreath < 4 && exhaleClips.Length > 0 && audioSource != null)
        {
            audioSource.clip = exhaleClips[currentBreath];
            audioSource.Play();
        }

        float elapsedTime = 0f;

        while (elapsedTime < exhaleTime)
        {
            float t = elapsedTime / exhaleTime;
            t = Mathf.SmoothStep(0, 1, t);

            float currentScale = Mathf.Lerp(maxScale, initialScale, t);
            sphere.localScale = new Vector3(currentScale, currentScale, currentScale);

            if (sphereLight != null)
            {
                sphereLight.intensity = Mathf.Lerp(maxLightIntensity, minLightIntensity, t);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        sphere.localScale = new Vector3(initialScale, initialScale, initialScale);
        if (sphereLight != null)
        {
            sphereLight.intensity = minLightIntensity;
        }
    }

    public void StopBreathing()
    {
        isBreathingActive = false;
        StopAllCoroutines();

        if (sphere != null)
        {
            sphere.localScale = initialSphereScale;
        }

        if (sphereLight != null)
        {
            sphereLight.intensity = minLightIntensity;
        }
    }
}