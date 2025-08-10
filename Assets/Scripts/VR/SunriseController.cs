// SunriseController.cs  (sphere-driven, autoplay + loop)
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SunriseController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The sky dome or background sphere whose X rotation will be animated")]
    public Transform sphere;                // Drag 'Background Sphere' here

    [Header("Sphere Rotation (degrees)")]
    [Tooltip("Start X angle (below horizon)")]
    public float startX = -10f;
    [Tooltip("End X angle (above horizon)")]
    public float endX   = 45f;
    [Tooltip("Use localRotation (recommended since the sphere is usually parented)")]
    public bool useLocalRotation = true;

    [Header("Timing")]
    [Min(0.1f)] public float duration = 10f;
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

    [Header("Autoplay & Loop")]
    [Tooltip("자동 재생 여부 (씬 시작 시)")]
    public bool playOnStart = true;
    [Tooltip("무한 반복")]
    public bool loop = true;
    [Tooltip("끝에서 되돌아가는 핑퐁 모드(일출↔일몰)")]
    public bool pingPong = true;
    [Tooltip("루프 사이 일시정지(초)")]
    [Min(0f)] public float loopPause = 0f;

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
    }

    void Start()
    {
        // 씬 시작 시 자동재생
        if (playOnStart) StartSunrise();
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
        animCo = StartCoroutine(loop ? CoSunriseLoop() : CoSunriseOnce(true));
    }

    IEnumerator CoSunriseOnce(bool forward)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);   // 0..1 진행도
            float t01 = forward ? k : (1f - k);      // 방향에 따라 0→1 또는 1→0
            ApplyState(curve.Evaluate(t01));
            yield return null;
        }
        ApplyState(forward ? 1f : 0f);
    }

    IEnumerator CoSunriseLoop()
    {
        bool forward = true;
        while (true)
        {
            yield return CoSunriseOnce(forward);
            if (!loop) break;

            if (loopPause > 0f) yield return new WaitForSeconds(loopPause);

            // 핑퐁이면 방향 반전, 아니면 다시 처음부터
            forward = pingPong ? !forward : true;

            if (!pingPong && resetToStartOnPlay)
                ApplyState(0f); // 다음 사이클을 위해 0으로 스냅(옵션)
        }
    }
}
