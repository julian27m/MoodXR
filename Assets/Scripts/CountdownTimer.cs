using UnityEngine;
using TMPro;
using System.Collections;

public class CountdownTimer : MonoBehaviour
{
    [SerializeField] public TextMeshPro timerText;
    [SerializeField] public float totalTime = 60f; // 1 minuto en segundos
    [SerializeField] public GameObject NextButton;

    private bool isRunning = false;
    private float timeRemaining;

    private void Start()
    {
        // Inicializar el texto a "1:00"
        timerText.text = "1:00";
        timeRemaining = totalTime;
    }

    // Método que será llamado al presionar el botón
    public void StartTimer()
    {
        if (!isRunning)
        {
            isRunning = true;
            timeRemaining = totalTime;
            StartCoroutine(UpdateTimer());
        }
    }

    private IEnumerator UpdateTimer()
    {
        while (timeRemaining > 0 && isRunning)
        {
            // Esperar un segundo
            yield return new WaitForSeconds(1f);

            // Reducir el tiempo restante
            timeRemaining -= 1f;

            // Actualizar el texto del temporizador
            UpdateTimerDisplay();

            // Si el tiempo llega a cero
            if (timeRemaining <= 0)
            {
                OnTimerEnd();
            }
        }
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);

        // Formatear el texto como "M:SS"
        timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
    }

    private void OnTimerEnd()
    {
        isRunning = false;
        timerText.text = "0:00";

        NextButton.gameObject.SetActive(true);

        // Llamar a otro método para finalizar el mini juego
        // FinishMiniGame();
    }


    public void StopTimer()
    {
        isRunning = false;
    }


    public void ResetTimer()
    {
        isRunning = false;
        timeRemaining = totalTime;
        timerText.text = "1:00";
    }
}