using UnityEngine;

[ExecuteAlways]
public class FogByXRotation : MonoBehaviour
{
    [SerializeField] Transform rotationSource;

    [Header("X각 범위 -> t(0~1)")]
    [Tooltip("이 각도에서 포그가 가장 강함(수평선 근처)")]
    public float angleFogMax = -30f;
    [Tooltip("이 각도에서 포그가 가장 약함(머리 위로 올라갔을 때)")]
    public float angleFogMin = -160f;

    [Header("Linear Fog 값(Mode=Linear일 때)")]
    public bool useLinear = true;
    public float startAtStrong = 0f;     // t=0 (강함)일 때 시작
    public float startAtWeak   = 0f;     // t=1 (약함)일 때 시작
    public float endAtStrong   = 300f;   // t=0일 때 End (짧을수록 진함)
    public float endAtWeak     = 1100f;  // t=1일 때 End

    [Header("Exponential/Exp2일 때")]
    public float densityAtStrong = 0.02f;
    public float densityAtWeak   = 0.002f;

    [Header("부드럽게 전환")]
    [Range(0f,10f)] public float smooth = 4f;
    public AnimationCurve curve = AnimationCurve.Linear(0,0,1,1);

    float _curStart, _curEnd, _curDensity;

    void Reset() { rotationSource = transform; }

    void Update()
    {
        if (!rotationSource) rotationSource = transform;

        // X 각도 [-180,180]
        float ax = rotationSource.eulerAngles.x;
        if (ax > 180f) ax -= 360f;

        // angleFogMax(수평선 근처) → angleFogMin(하늘 높이)로 갈수록 t: 0→1
        float t = Mathf.InverseLerp(angleFogMax, angleFogMin, ax);
        t = curve.Evaluate(Mathf.Clamp01(t));

        // 지연보간(부드럽게)
        float dt = Application.isPlaying ? Time.deltaTime : 1f/30f;
        float k = smooth > 0f ? 1f - Mathf.Exp(-smooth * dt) : 1f;

        RenderSettings.fog = true;

        if (useLinear)
        {
            RenderSettings.fogMode = FogMode.Linear;
            float tgtStart = Mathf.Lerp(startAtStrong, startAtWeak, t);
            float tgtEnd   = Mathf.Lerp(endAtStrong,   endAtWeak,   t);
            _curStart = Mathf.Lerp(_curStart, tgtStart, k);
            _curEnd   = Mathf.Lerp(_curEnd,   tgtEnd,   k);
            RenderSettings.fogStartDistance = _curStart;
            RenderSettings.fogEndDistance   = _curEnd;
        }
        else
        {
            // Exp/Exp2일 때는 density만 조절
            float tgtDensity = Mathf.Lerp(densityAtStrong, densityAtWeak, t);
            _curDensity = Mathf.Lerp(_curDensity, tgtDensity, k);
            RenderSettings.fogDensity = _curDensity;
        }
    }
}
