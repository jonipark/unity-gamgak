using UnityEngine;

[ExecuteAlways]
public class LightTemperatureByXRotation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light sun;                 // Directional Light
    [SerializeField] private Transform rotationSource;  // 각도 읽을 대상(비우면 이 오브젝트)

    [Header("Kelvin (adjust to taste)")]
    [Tooltip("일출/일몰: 진한 붉은")]
    public float kelvinRed = 800f;
    [Tooltip("주황~노랑")]
    public float kelvinOrangeYellowMin = 3200f;
    public float kelvinOrangeYellowMax = 4800f;
    [Tooltip("한낮: 푸른빛")]
    public float kelvinDayBlue = 8200f;

    [Header("Intensity (optional)")]
    public bool alsoSetIntensity = true;
    [Tooltip("일출/일몰 강도")]
    public float intensityDawnDusk = 0.7f;
    [Tooltip("낮 강도")]
    public float intensityDay = 1.1f;
    [Tooltip("밤(지평선 아래) 강도")]
    public float intensityNight = 0.2f;

    [Header("Smoothing")]
    [Range(0f, 10f)] public float smooth = 4f;

    float _curKelvin, _tgtKelvin;
    float _curIntensity, _tgtIntensity;

    void Reset()
    {
        rotationSource = transform;
        if (!sun) sun = FindFirstObjectByType<Light>();
    }

    void OnEnable()
    {
        if (!rotationSource) rotationSource = transform;
        if (sun)
        {
            // 온도 사용 강제로 켬 + 필터는 흰색(온도 영향만 보이게)
            sun.useColorTemperature = true;
            if (sun.color != Color.white) sun.color = Color.white;
            _curKelvin = sun.colorTemperature;
            _curIntensity = sun.intensity;
        }
    }

    void Update()
    {
        if (!sun) return;

        float a = GetSignedX(rotationSource ? rotationSource : transform);

        Evaluate(a, out _tgtKelvin, out _tgtIntensity);

        float dt = Application.isPlaying ? Time.deltaTime : 1f / 30f;
        float k = smooth > 0f ? 1f - Mathf.Exp(-smooth * dt) : 1f;

        _curKelvin = Mathf.Lerp(_curKelvin, _tgtKelvin, k);
        _curKelvin = Mathf.Clamp(_curKelvin, 1500f, 12000f);
        sun.useColorTemperature = true;
        sun.colorTemperature = _curKelvin;

        if (alsoSetIntensity)
        {
            _curIntensity = Mathf.Lerp(_curIntensity, _tgtIntensity, k);
            sun.intensity = _curIntensity;
        }
    }

    static float GetSignedX(Transform t)
    {
        float ax = t.eulerAngles.x;
        return (ax > 180f) ? ax - 360f : ax; // [-180,180]
    }

    /// <summary>
    /// 각도 a(도) → 목표 Kelvin/Intensity
    /// 구간:
    ///  10 ~ -10   : 빨강
    /// -10 ~ -30  : 주황/노랑
    /// -30 ~ -160 : 푸른 낮
    /// -160 ~ -170: 주황/노랑
    /// -170 ~ -180: 빨강
    /// 나머지(지평선 아래)는 밤으로 처리
    /// </summary>
    void Evaluate(float a, out float kelvin, out float intensity)
    {
        // 기본(밤)
        kelvin = kelvinDayBlue; // 의미 없지만 초기화
        intensity = intensityNight;

        // 일출: 17 → -10 (빨강)
        if (a <= 17f && a >= -10f)
        {
            float t = Smooth01(Mathf.InverseLerp(-10f, 0f, a)); // -10→0 : 0→1
            kelvin = Mathf.Lerp(3000f, kelvinRed, t);           // 3000→red
            intensity = Mathf.Lerp(intensityDawnDusk, intensityDawnDusk * 0.9f, t);
            return;
        }

        // 일출 상단: -10 → -30 (주황/노랑)
        if (a < -10f && a >= -30f)
        {
            float t = Smooth01(Mathf.InverseLerp(-30f, -10f, a)); // -30→-10 : 0→1
            kelvin = Mathf.Lerp(kelvinOrangeYellowMin, kelvinOrangeYellowMax, t);
            intensity = Mathf.Lerp(intensityDawnDusk, Mathf.Lerp(intensityDawnDusk, intensityDay, 0.5f), t);
            return;
        }

        // 낮: -30 → -160 (푸른빛)
        if (a < -30f && a >= -160f)
        {
            // 높이에 따라 약간만 변동(필요 없으면 상수로)
            float t = Mathf.InverseLerp(-30f, -160f, a);
            kelvin = Mathf.Lerp(7000f, kelvinDayBlue, t); // 7000→dayBlue
            intensity = Mathf.Lerp(Mathf.Max(intensityDawnDusk, 0.8f), intensityDay, t);
            return;
        }

        // 일몰 상단: -160 → -170 (주황/노랑)
        if (a < -160f && a >= -170f)
        {
            float t = Smooth01(Mathf.InverseLerp(-170f, -160f, a)); // -170→-160 : 0→1
            kelvin = Mathf.Lerp(kelvinOrangeYellowMax, kelvinOrangeYellowMin, t);
            intensity = Mathf.Lerp(intensityDay * 0.85f, intensityDawnDusk, t);
            return;
        }

        // 일몰: -170 → -180 (빨강)
        if (a < -170f && a >= -180f)
        {
            float t = Smooth01(Mathf.InverseLerp(-180f, -170f, a)); // -180→-170 : 0→1
            kelvin = Mathf.Lerp(kelvinRed, 3000f, t);               // red→3000
            intensity = Mathf.Lerp(intensityDawnDusk * 0.9f, intensityDawnDusk, t);
            return;
        }

        // 그 외(지평선 아래 = 밤)
        kelvin = 6500f; // 의미 없지만 중립값
        intensity = intensityNight;
    }

    static float Smooth01(float x) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x));
}
