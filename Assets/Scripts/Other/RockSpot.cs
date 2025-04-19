using UnityEngine;

public class RockSpot : MonoBehaviour
{
    private bool isOccupied = false;
    public FireRitual fireRitualManager;

    private void OnTriggerEnter(Collider other)
    {
        // Verificar si el objeto que entr� es una piedra
        if (other.CompareTag("Rock") && !isOccupied)
        {
            isOccupied = true;
            // Informar al gestor de la fogata que este spot est� ocupado
            fireRitualManager.UpdateRockCount(1);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Verificar si la piedra que sali� es la que estaba ocupando el spot
        if (other.CompareTag("Rock") && isOccupied)
        {
            isOccupied = false;
            // Informar al gestor de la fogata que este spot ya no est� ocupado
            fireRitualManager.UpdateRockCount(-1);
        }
    }
}
