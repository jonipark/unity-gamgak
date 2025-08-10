using UnityEngine;

public class BoatBuoyancy : MonoBehaviour
{
    public Transform waterPlane; // Assign in Inspector
    public float floatStrength = 1f;
    public float damping = 0.9f;

    private float currentVelocity = 0f;

    void Update()
    {
        if (waterPlane == null) return;

        Vector3 worldPos = transform.position;
        Vector3 localPos = waterPlane.InverseTransformPoint(worldPos);

        // These must match your water shader settings
        float waveSpeed = 1f;
        float waveScale = 0.2f;
        float waveHeight = 0.5f;

        float time = Time.time * waveSpeed;

        // Safe wave Y calculation
        float waveY = Mathf.Sin(localPos.x * waveScale + time) *
                      Mathf.Sin(localPos.z * waveScale + time) *
                      waveHeight;

        float targetY = waterPlane.position.y + waveY;
        float currentY = transform.position.y;
        float difference = targetY - currentY;

        // Add safety to damping and floatStrength
        floatStrength = Mathf.Clamp(floatStrength, 0f, 10f);
        damping = Mathf.Clamp(damping, 0.01f, 0.99f);

        currentVelocity = (currentVelocity + difference * floatStrength) * damping;

        float newY = currentY + currentVelocity + 20f;

        // Clamp to avoid invalid position
        if (float.IsInfinity(newY) || float.IsNaN(newY)) return;

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
