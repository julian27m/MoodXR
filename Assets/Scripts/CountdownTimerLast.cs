using UnityEngine;
using TMPro;
using System.Collections;

public class CountdownTimerLast : MonoBehaviour
{
    [SerializeField] public TextMeshPro timerText; // Cambiado a TextMeshProUGUI por compatibilidad
    [SerializeField] public float totalTime = 120f; // 2 minutos en segundos
    [SerializeField] public GameObject OutButton;
    [SerializeField] public GameObject FurnitureRed;
    [SerializeField] public GameObject FurnitureWhite;
    [SerializeField] private AudioClip LastAudio;

    private bool isRunning = false;
    private AudioSource audioSource;
    private float timeRemaining;
    private float lastUpdateTime;

    private void Awake()
    {
        // Asegurarse de tener un AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        // Verificar que todas las referencias estén asignadas
        if (timerText == null) Debug.LogError("Timer Text no asignado en " + gameObject.name);
        if (OutButton == null) Debug.LogError("OutButton no asignado en " + gameObject.name);
        if (FurnitureRed == null) Debug.LogError("FurnitureRed no asignado en " + gameObject.name);
        if (FurnitureWhite == null) Debug.LogError("FurnitureWhite no asignado en " + gameObject.name);
        if (LastAudio == null) Debug.LogWarning("LastAudio no asignado en " + gameObject.name);

        // Inicializar el texto a "2:00"
        if (timerText != null) timerText.text = "2:00";
        timeRemaining = totalTime;

        // Asegurar estado inicial
        if (OutButton != null) OutButton.SetActive(false);
        if (FurnitureWhite != null) FurnitureWhite.SetActive(false);
    }

    // Método que será llamado al presionar el botón
    public void StartTimer()
    {
        if (!isRunning)
        {
            isRunning = true;
            timeRemaining = totalTime;
            lastUpdateTime = Time.time;
            StartCoroutine(UpdateTimer());
            Debug.Log("Timer iniciado");
        }
    }

    private IEnumerator UpdateTimer()
    {
        while (timeRemaining > 0 && isRunning)
        {
            // Calcular tiempo transcurrido desde la última actualización
            float deltaTime = Time.time - lastUpdateTime;
            lastUpdateTime = Time.time;

            // Reducir el tiempo restante con precisión
            timeRemaining -= deltaTime;

            // Actualizar el texto del temporizador
            UpdateTimerDisplay();

            // Si el tiempo llega a cero
            if (timeRemaining <= 0)
            {
                OnTimerEnd();
                yield break;
            }

            // Esperar al siguiente frame para mejorar precisión
            yield return null;
        }
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);

        // Formatear el texto como "M:SS"
        if (timerText != null)
        {
            timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
        }
    }

    private void OnTimerEnd()
    {
        isRunning = false;

        if (timerText != null) timerText.text = "0:00";

        // Activar/desactivar objetos con verificación de nulos
        if (FurnitureRed != null) FurnitureRed.SetActive(false);
        if (FurnitureWhite != null) FurnitureWhite.SetActive(true);
        if (OutButton != null) OutButton.SetActive(true);

        // Reproducir sonido
        if (LastAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(LastAudio);
            Debug.Log("Audio final reproducido");
        }

        Debug.Log("Timer finalizado");
    }

    public void StopTimer()
    {
        if (isRunning)
        {
            isRunning = false;
            Debug.Log("Timer detenido");
        }
    }

    public void ResetTimer()
    {
        isRunning = false;
        timeRemaining = totalTime;
        if (timerText != null) timerText.text = "2:00";  // Actualizado a "2:00"
        Debug.Log("Timer reiniciado");
    }
}