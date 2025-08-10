// SunriseController.cs  (sphere-driven)
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SunriseController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The sky dome or background sphere whose X rotation will be animated")]
    public Transform sphere;                // Drag 'Background Sphere' here
    [Tooltip("Optional UI Button that triggers the sunrise")]
    public Button triggerButton;

    [Header("Sphere Rotation (degrees)")]
    [Tooltip("Start X angle (below horizon)")]
    public float startX = -10f;
    [Tooltip("End X angle (above horizon)")]
    public float endX   = 45f;
    [Tooltip("Use localRotation (recommended since the sphere is usually parented)")]
    public bool useLocalRotation = true;

    [Header("Timing")]
    [Min(0.1f)] public float duration = 8f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Optional Light Ramp")]
    [Tooltip("Optional: light to ramp intensity/color while the sphere rotates")]
    public Light sunLight;                  // Leave empty if you don't want light changes
    [Min(0f)] public float startIntensity = 0.0f;
    [Min(0f)] public float endIntensity   = 1.2f;
    [Tooltip("Optional: gradient over time 0..1; leave empty to keep the light color")]
    public Gradient sunColor;

    [Header("Options")]
    [Tooltip("Set sphere to startX when the scene starts")]
    public bool resetToStartOnPlay = true;
    [Tooltip("Ignore new triggers while the animation is playing")]
    public bool ignoreIfPlaying = false;

    Coroutine animCo;

    // Cached rotations preserving the sphere's current Y/Z so only X changes
    Quaternion startRot, endRot;
    float baseY, baseZ;

    void Awake()
    {
        if (sphere == null) sphere = transform; // fallback

        CacheBaseYZ();
        ComputeRotations();

        if (resetToStartOnPlay) ApplyState(0f);

        if (triggerButton != null)
            triggerButton.onClick.AddListener(StartSunrise);
    }

    void OnValidate()
    {
        if (sphere != null)
        {
            // Recompute using latest inspector values
            CacheBaseYZ();
            ComputeRotations();
        }
    }

    void CacheBaseYZ()
    {
        if (sphere == null) return;
        Vector3 e = useLocalRotation ? sphere.localEulerAngles : sphere.eulerAngles;
        baseY = e.y;
        baseZ = e.z;
    }

    void ComputeRotations()
    {
        startRot = Quaternion.Euler(startX, baseY, baseZ);
        endRot   = Quaternion.Euler(endX,   baseY, baseZ);
    }

    void ApplyState(float t01)
    {
        // Rotate the sphere only on X (Y/Z preserved via cached quaternions)
        if (sphere != null)
        {
            Quaternion q = Quaternion.Slerp(startRot, endRot, t01);
            if (useLocalRotation) sphere.localRotation = q;
            else                  sphere.rotation      = q;
        }

        // Optional: ramp light intensity/color alongside the rotation
        if (sunLight != null)
        {
            sunLight.intensity = Mathf.Lerp(startIntensity, endIntensity, t01);
            if (sunColor != null && sunColor.colorKeys != null && sunColor.colorKeys.Length > 0)
                sunLight.color = sunColor.Evaluate(t01);
        }
    }

    public void StartSunrise()
    {
        if (ignoreIfPlaying && animCo != null) return;

        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(CoSunrise());
    }

    IEnumerator CoSunrise()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            ApplyState(curve.Evaluate(k));
            yield return null;
        }
        ApplyState(1f);
        animCo = null;
    }
}
