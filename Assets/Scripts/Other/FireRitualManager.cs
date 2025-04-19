using UnityEngine;
using System.Collections.Generic;

public class FireRitualManager : MonoBehaviour
{
    [Header("References")]
    public GameObject fireEffect;        // Efecto visual de la fogata
    public AudioSource fireAudioSource;  // Sonido de la fogata
    public AudioSource fireOutroSource;  // Sonido de la fogata
    public GameObject[] rockSpots;       // Array de los 4 spots para las piedras

    private Dictionary<GameObject, GameObject> spotsOccupied = new Dictionary<GameObject, GameObject>();
    private bool fireIsLit = false;
    private bool oneTime = false;

    private static int previousOccupiedCount = 0;

    void Start()
    {
        oneTime = true;
        // Asegurarse de que la fogata comience apagada
        if (fireEffect != null)
        {
            fireEffect.SetActive(false);
        }

        if (fireAudioSource != null)
        {
            fireAudioSource.Stop();
        }

        // Inicializar el seguimiento de spots
        foreach (GameObject spot in rockSpots)
        {
            spotsOccupied[spot] = null;
        }
    }

    void Update()
    {
        CheckRockPlacements();
    }

    void CheckRockPlacements()
    {
        int occupiedCount = 0;

        // Revisar cada spot
        foreach (GameObject spot in rockSpots)
        {
            // Si el spot está ocupado en nuestro registro
            if (spotsOccupied[spot] != null)
            {
                // Verificar si el objeto sigue estando físicamente dentro del spot
                if (IsRockStillInSpot(spot, spotsOccupied[spot]))
                {
                    occupiedCount++;
                }
                else
                {
                    // La piedra ya no está en el spot
                    spotsOccupied[spot] = null;
                }
            }
            else
            {
                // Buscar si hay alguna piedra en este spot
                GameObject rock = FindRockInSpot(spot);
                if (rock != null)
                {
                    spotsOccupied[spot] = rock;
                    occupiedCount++;
                }
            }
        }

        // Actualizar estado de la fogata
        if (occupiedCount >= rockSpots.Length && !fireIsLit)
        {
            // Todos los spots están ocupados, encender la fogata
            LightFire();
        }
        else if (occupiedCount < rockSpots.Length && fireIsLit)
        {
            // Al menos un spot está vacío, apagar la fogata
            ExtinguishFire();
        }

        // Para depuración
        Debug.Log($"Spots ocupados: {occupiedCount} de {rockSpots.Length}");
        if (occupiedCount != previousOccupiedCount)
        {
            if (TelemetriaManager.Instance != null)  // Añadir esta verificación
            {
                TelemetriaManager.Instance.RegistrarPiedraColocada(occupiedCount);
                previousOccupiedCount = occupiedCount;
            }
        }
    }

    GameObject FindRockInSpot(GameObject spot)
    {
        // Obtener el collider del spot
        Collider spotCollider = spot.GetComponent<Collider>();
        if (spotCollider == null) return null;

        // Encontrar todos los colliders que se solapan con este spot
        Collider[] overlappingColliders = Physics.OverlapBox(
            spotCollider.bounds.center,
            spotCollider.bounds.extents,
            spot.transform.rotation
        );

        // Buscar entre los colliders solapados alguno con tag "Rock"
        foreach (Collider col in overlappingColliders)
        {
            if (col.CompareTag("Rock"))
            {
                return col.gameObject;
            }
        }

        return null;
    }

    bool IsRockStillInSpot(GameObject spot, GameObject rock)
    {
        if (rock == null) return false;

        Collider spotCollider = spot.GetComponent<Collider>();
        Collider rockCollider = rock.GetComponent<Collider>();

        if (spotCollider == null || rockCollider == null) return false;

        // Verificar si los colliders siguen solapándose
        return spotCollider.bounds.Intersects(rockCollider.bounds);
    }

    void LightFire()
    {
        fireIsLit = true;
        TelemetriaManager.Instance.RegistrarFogataEncendida();
        Debug.Log("¡Fogata encendida!");

        if (fireEffect != null)
        {
            fireEffect.SetActive(true);
        }

        if (fireAudioSource != null && !fireAudioSource.isPlaying)
        {
            fireAudioSource.Play();
        }
        if (fireOutroSource != null && !fireOutroSource.isPlaying && oneTime == true)
        {
            fireOutroSource.Play();
            oneTime = false;
        }

        // Aquí puedes activar el audio del diálogo sobre la transformación
    }

    void ExtinguishFire()
    {
        fireIsLit = false;
        TelemetriaManager.Instance.RegistrarFogataApagada();
        Debug.Log("Fogata apagada");

        if (fireEffect != null)
        {
            fireEffect.SetActive(false);
        }

        if (fireAudioSource != null && fireAudioSource.isPlaying)
        {
            fireAudioSource.Stop();
        }
    }
}