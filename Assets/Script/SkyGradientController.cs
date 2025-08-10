using UnityEngine;

public class SkyGradientController : MonoBehaviour
{
    public Light directionalLight;
    public Material skyMaterial;

    void Update()
    {
        if (directionalLight == null || skyMaterial == null) return;

        float angle = directionalLight.transform.eulerAngles.x;
        float t = Mathf.InverseLerp(0, 180, angle); // Normalize 0~180 degrees

        // Define day-night gradient colors
        Color topDay = new Color(0.5f, 0.7f, 1f, 0f);     // Bright sky
        Color bottomDay = new Color(1f, 0.6f, 0.4f, 0f);  // Warm bottom

        Color topNight = new Color(0.0f, 0.05f, 0.1f, 0f); // Dark blue
        Color bottomNight = new Color(0.05f, 0.0f, 0.1f, 0f); // Purple

        // Lerp based on time of day
        Color topColor = Color.Lerp(topNight, topDay, Mathf.Sin(t * Mathf.PI));
        Color bottomColor = Color.Lerp(bottomNight, bottomDay, Mathf.Sin(t * Mathf.PI));

        topColor.a = 0f;
        bottomColor.a = 0f;

        skyMaterial.SetColor("_TopColor", topColor);
        skyMaterial.SetColor("_BottomColor", bottomColor);
    }
}
