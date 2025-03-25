using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SmashGameController : MonoBehaviour
{
    [Header("Game Objects")]
    [SerializeField] private GameObject[] smashableObjects; // Los 11 objetos que se pueden golpear
    [SerializeField] private TextMeshPro scoreText; // Texto para mostrar la puntuación

    [Header("Game Settings")]
    [SerializeField] private float gameTime = 60f; // Duración del juego en segundos
    [SerializeField] private float spawnInterval = 1.5f; // Tiempo entre apariciones de objetos
    [SerializeField] private float spawnHeight = 0.774800003f; // Altura de aparición (Y)
    [SerializeField] private float fallThreshold = 0.5f; // Altura por debajo de la cual se considera que cayó
    [SerializeField, Tooltip("Si el objeto tiene Rigidbody, hacerlo cinemático durante estos frames después de aparecer")]
    private int sleepFrames = 3;

    [Header("Spawn Area")]
    [SerializeField] private Vector3 spawnAreaMin = new Vector3(-0.672699988f, 0.774800003f, 0.130999997f);
    [SerializeField] private Vector3 spawnAreaMax = new Vector3(0.66900003f, 0.774800003f, 0.86559999f);

    [Header("Audio")]
    [SerializeField] private AudioClip mugAudio;
    [SerializeField] private AudioClip plantAudio;
    [SerializeField] private AudioClip guitarAudio;
    [SerializeField] private AudioClip carAudio;
    [SerializeField] private AudioClip bottleAudio;
    [SerializeField] private AudioClip computerAudio;
    [SerializeField] private AudioClip stoneAudio;

    // Variables privadas para el control del juego
    private int score = 0;
    private bool gameRunning = false;
    private List<GameObject> activeObjects = new List<GameObject>();
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> originalRotations = new Dictionary<GameObject, Quaternion>();
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
    private Dictionary<Rigidbody, int> sleepCountdowns = new Dictionary<Rigidbody, int>();
    private AudioSource audioSource;

    private void Awake()
    {
        // Añadir un AudioSource si no existe
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Guardar posiciones, rotaciones y escalas originales
        foreach (GameObject obj in smashableObjects)
        {
            originalPositions[obj] = obj.transform.position;
            originalRotations[obj] = obj.transform.rotation;
            originalScales[obj] = obj.transform.localScale;

            // Inicializar diccionario de countdowns para Rigidbodies
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                sleepCountdowns[rb] = 0;
            }

            // Desactivar inicialmente todos los objetos
            obj.SetActive(false);
        }

        // Inicializar contador de puntuación
        scoreText.text = "0";
    }

    public void StartGame()
    {
        if (!gameRunning)
        {
            gameRunning = true;
            score = 0;
            scoreText.text = "0";

            // Iniciar la cuenta atrás y el spawner
            StartCoroutine(GameTimer());
            StartCoroutine(SpawnObjects());
        }
    }

    private IEnumerator GameTimer()
    {
        yield return new WaitForSeconds(gameTime);
        EndGame();
    }

    private void EndGame()
    {
        gameRunning = false;

        // Desactivar todos los objetos activos
        foreach (GameObject obj in activeObjects)
        {
            obj.SetActive(false);
        }
        activeObjects.Clear();

        // Aquí podrías añadir lo que sucede al final del juego
        Debug.Log("Juego terminado. Puntuación final: " + score);
    }

    private IEnumerator SpawnObjects()
    {
        while (gameRunning)
        {
            // Esperar el intervalo de tiempo
            yield return new WaitForSeconds(spawnInterval);

            if (gameRunning)
            {
                SpawnRandomObject();
            }
        }
    }

    private void SpawnRandomObject()
    {
        // Elegir un objeto aleatorio de los disponibles
        if (smashableObjects.Length > 0)
        {
            int randomIndex = Random.Range(0, smashableObjects.Length);
            GameObject objectToSpawn = smashableObjects[randomIndex];

            // Verificar si el objeto ya está activo
            if (!objectToSpawn.activeSelf)
            {
                // Generar posición aleatoria dentro del área definida
                float randomX = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
                float randomZ = Random.Range(spawnAreaMin.z, spawnAreaMax.z);
                Vector3 spawnPosition = new Vector3(randomX, spawnHeight, randomZ);

                // Colocar y activar el objeto
                objectToSpawn.transform.position = spawnPosition;
                objectToSpawn.transform.rotation = originalRotations[objectToSpawn];
                objectToSpawn.transform.localScale = originalScales[objectToSpawn];

                // Manejar el Rigidbody si existe
                Rigidbody rb = objectToSpawn.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;

                    // Hacer el objeto temporalmente cinemático para evitar colisiones fantasma
                    if (sleepFrames > 0)
                    {
                        rb.isKinematic = true;
                        sleepCountdowns[rb] = sleepFrames;
                    }
                }

                objectToSpawn.SetActive(true);

                // Añadir a la lista de objetos activos
                if (!activeObjects.Contains(objectToSpawn))
                {
                    activeObjects.Add(objectToSpawn);
                }
            }
        }
    }

    private void Update()
    {
        if (gameRunning)
        {
            CheckForFallenObjects();
        }
    }

    private void FixedUpdate()
    {
        // Manejar los countdowns para objetos que están temporalmente cinemáticos
        List<Rigidbody> keysToProcess = new List<Rigidbody>(sleepCountdowns.Keys);

        foreach (Rigidbody rb in keysToProcess)
        {
            if (sleepCountdowns[rb] > 0)
            {
                sleepCountdowns[rb]--;

                if (sleepCountdowns[rb] == 0 && rb != null)
                {
                    rb.isKinematic = false;
                }
            }
        }
    }

    private void CheckForFallenObjects()
    {
        // Crear una copia de la lista para evitar errores de modificación durante la iteración
        List<GameObject> objectsToCheck = new List<GameObject>(activeObjects);

        foreach (GameObject obj in objectsToCheck)
        {
            if (obj.activeSelf && obj.transform.position.y < fallThreshold)
            {
                // El objeto ha caído por debajo del umbral
                HandleFallenObject(obj);
            }
        }
    }

    private void HandleFallenObject(GameObject obj)
    {
        // Incrementar puntuación
        score++;
        scoreText.text = score.ToString();

        // Desactivar objeto
        obj.SetActive(false);
        activeObjects.Remove(obj);
    }

    // Este método debe ser llamado cuando un objeto es golpeado por el bate
    public void OnObjectHit(GameObject hitObject, GameObject hitter)
    {
        if (gameRunning && hitter.CompareTag("Bat"))
        {
            PlaySoundBasedOnTag(hitObject);
        }
    }

    private void PlaySoundBasedOnTag(GameObject hitObject)
    {
        AudioClip clipToPlay = null;

        // Seleccionar el audio según el tag
        if (hitObject.CompareTag("Mug"))
            clipToPlay = mugAudio;
        else if (hitObject.CompareTag("Plant"))
            clipToPlay = plantAudio;
        else if (hitObject.CompareTag("Guitar"))
            clipToPlay = guitarAudio;
        else if (hitObject.CompareTag("Car"))
            clipToPlay = carAudio;
        else if (hitObject.CompareTag("Bottle"))
            clipToPlay = bottleAudio;
        else if (hitObject.CompareTag("Computer"))
            clipToPlay = computerAudio;
        else if (hitObject.CompareTag("Stone"))
            clipToPlay = stoneAudio;

        // Reproducir el sonido si hay un clip asignado
        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }
}