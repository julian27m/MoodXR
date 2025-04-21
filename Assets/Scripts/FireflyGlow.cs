using UnityEngine;
using System.Collections;

public class FireflyGlow : MonoBehaviour
{
    private Light fireflyLight;
    public float minGlowTime = 1.5f;    // Tiempo mínimo entre parpadeos (más largo)
    public float maxGlowTime = 4f;      // Tiempo máximo entre parpadeos (más largo)
    public float initialIntensity = 0.01f;
    public float initialRange = 0.5f;
    public float maxIntensity = 2f;     // Intensidad máxima reducida
    public float maxRange = 3f;         // Rango máximo reducido
    public float fadeInDuration = 1.5f; // Tiempo que tarda en encenderse
    public float fadeOutDuration = 2f;  // Tiempo que tarda en apagarse

    private bool isActive = false;
    private Coroutine glowCoroutine;

    void Start()
    {
        fireflyLight = GetComponentInChildren<Light>();

        // Inicialmente apagado o muy tenue
        if (fireflyLight != null)
        {
            fireflyLight.intensity = 0;
            fireflyLight.range = initialRange;
        }
    }

    public void Activate()
    {
        if (!isActive && fireflyLight != null)
        {
            isActive = true;

            // Primero aumenta gradualmente el brillo
            StartCoroutine(InitialGlow());
        }
    }

    private IEnumerator InitialGlow()
    {
        // Espera un tiempo aleatorio antes de comenzar a brillar
        yield return new WaitForSeconds(Random.Range(0.5f, 3f));

        // Aumenta gradualmente desde 0
        float startTime = Time.time;
        float duration = 3f; // Aparición lenta en 3 segundos

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            fireflyLight.intensity = Mathf.Lerp(0, initialIntensity, t);
            fireflyLight.range = Mathf.Lerp(0, initialRange, t);
            yield return null;
        }

        // Asegúrate de que los valores finales sean exactos
        fireflyLight.intensity = initialIntensity;
        fireflyLight.range = initialRange;

        // Inicia el parpadeo normal
        glowCoroutine = StartCoroutine(GlowEffect());
    }

    IEnumerator GlowEffect()
    {
        while (true)
        {
            // Espera más tiempo entre parpadeos
            float waitTime = Random.Range(minGlowTime, maxGlowTime);
            yield return new WaitForSeconds(waitTime);

            // Intensidad y rango aleatorios dentro de los límites
            float targetIntensity = Random.Range(initialIntensity * 1.5f, maxIntensity);
            float targetRange = Random.Range(initialRange * 1.2f, maxRange);

            // Suaviza el parpadeo (encendido gradual)
            float startTime = Time.time;
            while (Time.time < startTime + fadeInDuration)
            {
                float t = (Time.time - startTime) / fadeInDuration;
                fireflyLight.intensity = Mathf.Lerp(initialIntensity, targetIntensity, t);
                fireflyLight.range = Mathf.Lerp(initialRange, targetRange, t);
                yield return null;
            }

            // Mantiene el brillo por un momento
            yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

            // Suaviza el parpadeo (apagado gradual)
            startTime = Time.time;
            while (Time.time < startTime + fadeOutDuration)
            {
                float t = (Time.time - startTime) / fadeOutDuration;
                fireflyLight.intensity = Mathf.Lerp(targetIntensity, initialIntensity, t);
                fireflyLight.range = Mathf.Lerp(targetRange, initialRange, t);
                yield return null;
            }

            // Asegura que los valores finales sean exactos
            fireflyLight.intensity = initialIntensity;
            fireflyLight.range = initialRange;
        }
    }
}