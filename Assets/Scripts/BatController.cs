using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatController : MonoBehaviour
{
    [Header("Configuración de Fuerza")]
    [SerializeField] private float hitForce = 10.0f; // Fuerza base que se aplica al golpear
    [SerializeField] private float upwardForce = 2.0f; // Componente hacia arriba para hacer que los objetos salten un poco
    [SerializeField] private float torqueMultiplier = 3.0f; // Multiplicador para la rotación

    [Header("Feedback")]
    [SerializeField] private bool debugMode = false; // Para ver líneas de debug en el editor

    // Lista para evitar golpear el mismo objeto varias veces en un corto período
    private Dictionary<Rigidbody, float> recentlyHit = new Dictionary<Rigidbody, float>();
    private float cooldownTime = 0.2f; // Tiempo mínimo entre golpes al mismo objeto

    private void Update()
    {
        // Limpia la lista de objetos golpeados recientemente
        List<Rigidbody> toRemove = new List<Rigidbody>();
        foreach (var entry in recentlyHit)
        {
            if (Time.time - entry.Value > cooldownTime)
            {
                toRemove.Add(entry.Key);
            }
        }

        foreach (var rb in toRemove)
        {
            recentlyHit.Remove(rb);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();

        if (rb != null && !rb.isKinematic)
        {
            // Verifica si el objeto fue golpeado recientemente
            if (recentlyHit.ContainsKey(rb))
            {
                return; // Salta este golpe si fue golpeado hace poco
            }

            // Añade a la lista de objetos golpeados recientemente
            recentlyHit[rb] = Time.time;

            // Obtener velocidad del bate para golpes más naturales
            Rigidbody batRb = GetComponent<Rigidbody>();
            Vector3 batVelocity = batRb != null ? batRb.velocity : transform.forward;

            // Calcular dirección del golpe (desde el punto de contacto)
            Vector3 contactPoint = collision.contacts[0].point;
            Vector3 objectCenter = collision.collider.bounds.center;

            // Dirección desde el punto de contacto hacia el centro del objeto, 
            // esto hace que el golpe sea más realista
            Vector3 direction = (objectCenter - contactPoint).normalized;

            // Asegurarnos de que el objeto se mueva hacia arriba un poco también
            direction += Vector3.up * upwardForce;
            direction.Normalize();

            // Calcula velocidad del bate como factor de fuerza
            float velocityMagnitude = batVelocity.magnitude;
            float impactForce = hitForce * (velocityMagnitude > 0.1f ? velocityMagnitude : 1f);

            // Aplicar la fuerza al objeto
            rb.velocity = Vector3.zero; // Resetea la velocidad actual
            rb.AddForce(direction * impactForce, ForceMode.Impulse);

            // Añadir torque (rotación) para que el golpe se vea más natural
            Vector3 torqueDir = Vector3.Cross(batVelocity.normalized, direction).normalized;
            rb.AddTorque(torqueDir * impactForce * torqueMultiplier, ForceMode.Impulse);

            if (debugMode)
            {
                // Dibuja líneas de debug para ver la dirección de la fuerza
                Debug.DrawRay(contactPoint, direction * impactForce * 0.1f, Color.red, 1.0f);
                Debug.DrawRay(objectCenter, torqueDir * impactForce * 0.1f, Color.blue, 1.0f);
            }
        }
    }
}