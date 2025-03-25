using UnityEngine;

public class HitDetector : MonoBehaviour
{
    private SmashGameController gameController;

    private void Start()
    {
        // Buscar el controlador del juego en la escena
        gameController = FindObjectOfType<SmashGameController>();

        if (gameController == null)
        {
            Debug.LogError("No se encontró SmashGameController en la escena");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Verificar si el objeto que golpeó tiene el tag "Bat"
        if (collision.gameObject.CompareTag("Bat") && gameController != null)
        {
            // Notificar al controlador que este objeto fue golpeado
            gameController.OnObjectHit(gameObject, collision.gameObject);
        }
    }
}