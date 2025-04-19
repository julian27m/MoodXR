using UnityEngine;
using System.Collections.Generic;

public class FireflyChest : MonoBehaviour
{
    public Transform chestLid; // La tapa del cofre
    public float movementThreshold = 0.001f;
    public float rotationThreshold = 0.5f;
    public AudioSource narrationAudio;
    public string fireflyTag = "firefly"; // El tag para identificar luciérnagas

    private bool isOpen = false;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private GameObject[] fireflies;

    void Start()
    {
        if (chestLid == null)
        {
            chestLid = transform;
        }

        lastPosition = chestLid.position;
        lastRotation = chestLid.rotation;

        // Encuentra todas las luciérnagas por tag en la escena
        fireflies = GameObject.FindGameObjectsWithTag(fireflyTag);
        Debug.Log("Luciérnagas encontradas por tag: " + fireflies.Length);
    }

    void Update()
    {
        if (isOpen || chestLid == null) return;

        float movement = Vector3.Distance(chestLid.position, lastPosition);
        float rotation = Quaternion.Angle(chestLid.rotation, lastRotation);

        // Depuración en tiempo real
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log("Monitoreando: Movimiento=" + movement + " Rotación=" + rotation);
        }

        if (movement > movementThreshold || rotation > rotationThreshold)
        {
            Debug.Log("¡Detectado movimiento! Movimiento: " + movement + ", Rotación: " + rotation);
            OpenChest();
        }

        lastPosition = chestLid.position;
        lastRotation = chestLid.rotation;
    }

    void OpenChest()
    {
        isOpen = true;
        TelemetriaManager.Instance.RegistrarAperturaCaja();
        Debug.Log("Método OpenChest() ejecutado. Luciérnagas a liberar: " + fireflies.Length);

        if (narrationAudio != null && !narrationAudio.isPlaying)
        {
            narrationAudio.Play();
            Debug.Log("Audio reproducido");
        }

        // Verificamos si hay luciérnagas de nuevo (por si acaso se agregaron después)
        if (fireflies.Length == 0)
        {
            fireflies = GameObject.FindGameObjectsWithTag(fireflyTag);
            Debug.Log("Buscando luciérnagas nuevamente: " + fireflies.Length);
        }

        // Liberar las luciérnagas
        foreach (GameObject fireflyObj in fireflies)
        {
            if (fireflyObj != null)
            {
                FireflyMovement movement = fireflyObj.GetComponent<FireflyMovement>();
                if (movement != null)
                {
                    movement.ReleaseFromChest();
                    Debug.Log("Luciérnaga liberada: " + fireflyObj.name);
                }
                else
                {
                    Debug.LogWarning("La luciérnaga " + fireflyObj.name + " no tiene el componente FireflyMovement");
                }

                FireflyGlow glow = fireflyObj.GetComponent<FireflyGlow>();
                if (glow != null)
                {
                    glow.Activate();
                    Debug.Log("Brillo de luciérnaga activado: " + fireflyObj.name);
                }
                else
                {
                    Debug.LogWarning("La luciérnaga " + fireflyObj.name + " no tiene el componente FireflyGlow");
                }
            }
        }
    }

    // Método para depuración - puedes llamarlo desde el inspector con un botón
    public void ForceOpenChest()
    {
        if (!isOpen)
        {
            Debug.Log("Apertura forzada del cofre");
            OpenChest();
        }
    }
}