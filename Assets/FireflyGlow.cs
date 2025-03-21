using UnityEngine;

public class FireflyGlow : MonoBehaviour
{
    private Light fireflyLight;
    private float glowTimer = 0f;
    public float minGlowTime = 0.5f;
    public float maxGlowTime = 2f;

    void Start()
    {
        fireflyLight = GetComponentInChildren<Light>(); // Obtiene la luz hija
        StartCoroutine(GlowEffect());
    }

    System.Collections.IEnumerator GlowEffect()
    {
        while (true)
        {
            float waitTime = Random.Range(minGlowTime, maxGlowTime);
            yield return new WaitForSeconds(waitTime);
            float targetIntensity = Random.Range(1.5f, 4f); // Brillo aleatorio

            // Suaviza el parpadeo
            for (float t = 0; t < 1; t += Time.deltaTime / 0.5f)
            {
                fireflyLight.intensity = Mathf.Lerp(0, targetIntensity, t);
                yield return null;
            }

            yield return new WaitForSeconds(Random.Range(0.2f, 0.5f));

            // Suaviza el apagado
            for (float t = 0; t < 1; t += Time.deltaTime / 0.5f)
            {
                fireflyLight.intensity = Mathf.Lerp(targetIntensity, 0, t);
                yield return null;
            }
        }
    }
}
