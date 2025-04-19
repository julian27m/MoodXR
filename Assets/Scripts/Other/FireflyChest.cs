using UnityEngine;
using System.Collections.Generic;

public class FireflyChest : MonoBehaviour
{
    public Transform chestLid; // La tapa del cofre
    public float movementThreshold = 0.001f;
    public float rotationThreshold = 0.5f;
    public AudioSource narrationAudio;
    public string fireflyTag = "firefly"; // El tag para identificar luci�rnagas

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

        // Encuentra todas las luci�rnagas por tag en la escena
        fireflies = GameObject.FindGameObjectsWithTag(fireflyTag);
        Debug.Log("Luci�rnagas encontradas por tag: " + fireflies.Length);
    }

    void Update()
    {
        if (isOpen || chestLid == null) return;

        float movement = Vector3.Distance(chestLid.position, lastPosition);
        float rotation = Quaternion.Angle(chestLid.rotation, lastRotation);

        // Depuraci�n en tiempo real
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log("Monitoreando: Movimiento=" + movement + " Rotaci�n=" + rotation);
        }

        if (movement > movementThreshold || rotation > rotationThreshold)
        {
            Debug.Log("�Detectado movimiento! Movimiento: " + movement + ", Rotaci�n: " + rotation);
            OpenChest();
        }

        lastPosition = chestLid.position;
        lastRotation = chestLid.rotation;
    }

    void OpenChest()
    {
        isOpen = true;
        TelemetriaManager.Instance.RegistrarAperturaCaja();
        Debug.Log("M�todo OpenChest() ejecutado. Luci�rnagas a liberar: " + fireflies.Length);

        if (narrationAudio != null && !narrationAudio.isPlaying)
        {
            narrationAudio.Play();
            Debug.Log("Audio reproducido");
        }

        // Verificamos si hay luci�rnagas de nuevo (por si acaso se agregaron despu�s)
        if (fireflies.Length == 0)
        {
            fireflies = GameObject.FindGameObjectsWithTag(fireflyTag);
            Debug.Log("Buscando luci�rnagas nuevamente: " + fireflies.Length);
        }

        // Liberar las luci�rnagas
        foreach (GameObject fireflyObj in fireflies)
        {
            if (fireflyObj != null)
            {
                FireflyMovement movement = fireflyObj.GetComponent<FireflyMovement>();
                if (movement != null)
                {
                    movement.ReleaseFromChest();
                    Debug.Log("Luci�rnaga liberada: " + fireflyObj.name);
                }
                else
                {
                    Debug.LogWarning("La luci�rnaga " + fireflyObj.name + " no tiene el componente FireflyMovement");
                }

                FireflyGlow glow = fireflyObj.GetComponent<FireflyGlow>();
                if (glow != null)
                {
                    glow.Activate();
                    Debug.Log("Brillo de luci�rnaga activado: " + fireflyObj.name);
                }
                else
                {
                    Debug.LogWarning("La luci�rnaga " + fireflyObj.name + " no tiene el componente FireflyGlow");
                }
            }
        }
    }

    // M�todo para depuraci�n - puedes llamarlo desde el inspector con un bot�n
    public void ForceOpenChest()
    {
        if (!isOpen)
        {
            Debug.Log("Apertura forzada del cofre");
            OpenChest();
        }
    }
}