using UnityEngine;
using UnityEngine.XR;
using System.Collections;

public class PlayerCalibration : MonoBehaviour
{
    [Header("Calibration Settings")]
    [Tooltip("Transform de referencia para calibrar (normalmente la c?mara principal de VR)")]
    public Transform headsetTransform;

    [Tooltip("Posici?n deseada en X, Y, Z para el usuario en el espacio del mundo")]
    public Vector3 desiredPosition = new Vector3(0f, 0f, 0f);

    [Tooltip("Rotaci?n deseada en Y (yaw) para que el usuario mire hacia adelante")]
    public float desiredForwardAngle = 0f;

    [Tooltip("Retraso antes de calibrar (segundos)")]
    public float calibrationDelay = 0.5f;

    [Tooltip("Realizar calibraci?n autom?tica al inicio")]
    public bool calibrateOnStart = true;

    // Referencias privadas
    private Transform playspaceTransform;

    private void Start()
    {
        // Encontrar el transform que representa el playspace
        playspaceTransform = GetPlayspaceTransform();

        if (playspaceTransform == null)
        {
            Debug.LogError("No se pudo encontrar el transform del playspace. La calibraci?n no funcionar?.");
            return;
        }

        if (headsetTransform == null)
        {
            // Intentar encontrar la c?mara principal como referencia
            headsetTransform = Camera.main?.transform;

            if (headsetTransform == null)
            {
                Debug.LogError("Headset transform no asignado y no se pudo encontrar la c?mara principal. Por favor asigna la referencia en el inspector.");
                return;
            }
        }

        // Calibrar autom?ticamente despu?s de un peque?o retraso
        if (calibrateOnStart)
        {
            StartCoroutine(CalibrateAfterDelay());
        }
    }

    // M?todo p?blico para calibrar manualmente (puede ser llamado desde un bot?n)
    public void CalibrateNow()
    {
        if (playspaceTransform == null || headsetTransform == null)
        {
            Debug.LogError("Faltan referencias necesarias para la calibraci?n.");
            return;
        }

        Debug.Log("Iniciando calibraci?n del jugador...");

        // Obtener la posici?n y rotaci?n actuales del headset
        Vector3 headsetPosition = headsetTransform.position;
        float headsetYRotation = headsetTransform.eulerAngles.y;

        // Calcular el ajuste necesario para la posici?n
        Vector3 positionOffset = new Vector3(
            desiredPosition.x - headsetPosition.x,
            0f, // No ajustamos altura para no causar mareos
            desiredPosition.z - headsetPosition.z
        );

        // Calcular el ajuste necesario para la rotaci?n
        float rotationOffset = desiredForwardAngle - headsetYRotation;

        // Aplicar ajustes al playspace
        playspaceTransform.position += positionOffset;
        playspaceTransform.RotateAround(headsetPosition, Vector3.up, rotationOffset);

        Debug.Log($"Calibraci?n completada. Ajustes aplicados: Posici?n {positionOffset}, Rotaci?n Y: {rotationOffset}");
    }

    // Obtener la referencia al transform del espacio de juego
    private Transform GetPlayspaceTransform()
    {
        // Buscar por jerarqu?a com?n en Unity XR Rig
        Transform xrRig = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>()?.transform;
        if (xrRig != null)
        {
            return xrRig;
        }

        // Buscar por nombres comunes de objetos XR
        string[] possibleNames = {
            "XRRig", "XR Rig", "OVRCameraRig", "TrackingSpace", "XROrigin", "VRPlayArea", "PlayArea", "[CameraRig]"
        };

        foreach (string name in possibleNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                return obj.transform;
            }
        }

        // Alternativa: el padre del headset puede ser el playspace
        if (headsetTransform != null && headsetTransform.parent != null)
        {
            return headsetTransform.parent;
        }

        // Como ?ltimo recurso, crear un nuevo objeto para usarlo como playspace
        Debug.LogWarning("No se encontr? un objeto de playspace existente. Creando uno nuevo.");
        GameObject newPlayspace = new GameObject("VRPlayspace");

        // Si tenemos headset, colocar el nuevo playspace adecuadamente
        if (headsetTransform != null)
        {
            newPlayspace.transform.position = new Vector3(
                headsetTransform.position.x,
                0,
                headsetTransform.position.z
            );

            if (headsetTransform.parent != null)
            {
                headsetTransform.parent.parent = newPlayspace.transform;
            }
        }

        return newPlayspace.transform;
    }

    // Corrutina para retrasar ligeramente la calibraci?n inicial
    private IEnumerator CalibrateAfterDelay()
    {
        yield return new WaitForSeconds(calibrationDelay);
        CalibrateNow();
    }
}