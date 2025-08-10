using UnityEngine;

[ExecuteAlways]
public class SunColorByXRotation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Renderer sunRenderer;            // 해 메시의 Renderer
    [SerializeField] Transform rotationSource;        // 각도 읽는 대상(비우면 이 오브젝트)

    [Header("Shader Properties")]
    public string colorProperty = "_BaseColor";       // URP/Lit: _BaseColor
    public bool alsoSetEmission = true;
    [Range(0f, 3f)] public float emissionMultiplier = 1.0f;

    [Header("Colors")]
    [ColorUsage(true, true)] public Color red   = new Color(1.00f, 0.18f, 0.10f);
    [ColorUsage(true, true)] public Color orange= new Color(1.00f, 0.55f, 0.15f);
    [ColorUsage(true, true)] public Color yellow= new Color(1.00f, 0.92f, 0.40f);
    [ColorUsage(true, true)] public Color warmWhite = new Color(1.00f, 0.98f, 0.92f);

    [Header("Angle Stops (deg)")]
    [Tooltip("5~-6: 빨강, -6~-16: 주황, -16~-45: 노랑, -45~-180: 따뜻한 흰색")]
    public float redEnd = 5f;
    public float orangeEnd = -16f;
    public float yellowEnd = -45f;
    public float dayEnd = -180f;

    [Header("Fade (visibility)")]
    [Tooltip("지평선 위로 막 뜰 때 페이드인 시작/끝(+각도)")]
    public float fadeInStartAboveHorizon = 6f;   // +6°에서 완전히 안 보임
    public float fadeInEndAtHorizon = 0.5f;      // +0.5° 근처에서 보이기 시작
    [Tooltip("뒤쪽으로 넘어갈 때 페이드아웃 시작/끝")]
    public float fadeOutStartBack = -175f;
    public float fadeOutEndBack = -179f;

    [Header("Smoothing")]
    [Range(0f, 10f)] public float smooth = 4f;   // 시간 보간

    // 내부 상태
    MaterialPropertyBlock _mpb;
    int _idColor, _idEmission;
    Color _curColor, _tgtColor;
    float _curEm, _tgtEm;

    void Reset()
    {
        if (!sunRenderer) TryGetComponent(out sunRenderer);
        rotationSource = transform;
    }

    void OnEnable()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        if (!rotationSource) rotationSource = transform;

        _idColor = Shader.PropertyToID(colorProperty);
        _idEmission = Shader.PropertyToID("_EmissionColor");

        // 초깃값 강제 적용
        UpdateOnce(true);
    }

    void Update()
    {
        UpdateOnce(false);
    }

    void UpdateOnce(bool instant)
    {
        if (!sunRenderer) return;

        float a = GetSignedX(rotationSource ? rotationSource : transform); // [-180,180]

        // 목표 색/밝기 계산
        Evaluate(a, out _tgtColor, out _tgtEm);

        // 시간 보간
        float dt = Application.isPlaying ? Time.deltaTime : 1f / 30f;
        float k = instant ? 1f : (smooth > 0f ? 1f - Mathf.Exp(-smooth * dt) : 1f);

        _curColor = Color.Lerp(_curColor, _tgtColor, k);
        _curEm    = Mathf.Lerp(_curEm, _tgtEm, k);

        Apply(_curColor, _curEm);
    }

    static float GetSignedX(Transform t)
    {
        float ax = t.eulerAngles.x;
        return (ax > 180f) ? ax - 360f : ax; // [-180,180]
    }

    // 각도 a(도) -> 색상/에미션 세기
    void Evaluate(float a, out Color color, out float emission)
    {
        if (a > -5f)
        {
            color = red;
            emission = 1.0f * emissionMultiplier; // 원하면 0.8f~1.2f 사이로 조절
            return;
        }

        // 1) 기본 색 그라데이션 (지평선 위 구간)
        if (a <= -5f && a >= dayEnd) // 0 ~ -180
        {
            if (a >= redEnd) // 0 ~ -6 : 빨강
            {
                float t = InverseLerp(redEnd, 0f, a); // -6→0 : 0→1
                color = Color.Lerp(red, orange, t);   // red→orange
            }
            else if (a >= orangeEnd) // -6 ~ -16 : 주황
            {
                float t = InverseLerp(orangeEnd, redEnd, a); // -16→-6 : 0→1
                color = Color.Lerp(orange, yellow, t);       // orange→yellow
            }
            else if (a >= yellowEnd) // -16 ~ -45 : 노랑
            {
                float t = InverseLerp(yellowEnd, orangeEnd, a); // -45→-16 : 0→1
                color = Color.Lerp(yellow, warmWhite, t);       // yellow→warmWhite
            }
            else // -45 ~ -160 : 따뜻한 흰색 유지
            {
                color = warmWhite;
            }
        }
        // 일몰 구간: -160 ~ -180
        else if (a < dayEnd && a >= -180f)
        {
            if (a >= -170f) // -160 ~ -170 : 흰→주황/노랑
            {
                float t = InverseLerp(-170f, dayEnd, a); // -170→-160 : 0→1
                color = Color.Lerp(orange, warmWhite, t); // 역방향 살짝
            }
            else            // -170 ~ -180 : 주황→빨강
            {
                float t = InverseLerp(-180f, -170f, a); // -180→-170 : 0→1
                color = Color.Lerp(red, orange, t);
            }
        }
        else // 그 외(지평선 아래/머리 뒤)
        {
            // color = Color.red; // 어차피 페이드에서 거의 0이 됨
            float t = InverseLerp(redEnd, 0f, a); // -6→0 : 0→1
            color = Color.Lerp(red, orange, t);
        }

        // 2) 에미션 밝기: 가운데(-90°)에서 가장 밝고, 경계로 갈수록 감소
        float middayPeak = 1f - Mathf.Abs(Mathf.Abs(a) - 90f) / 90f; // -90에서 1
        float baseEm = Mathf.Lerp(0.7f, 1.3f, Smooth01(middayPeak)); // 0.7~1.3
        emission = baseEm * emissionMultiplier;

        // 3) 가시 페이드(지평선 위/뒤쪽)
        float vis = 1f;

        // 지평선 위로 막 올라올 때(양수 각도에서 0도로 올 때) 페이드 인
        if (a > 0f)
        {
            vis *= 1f - Mathf.Clamp01((a - fadeInEndAtHorizon) / Mathf.Max(0.0001f, (fadeInStartAboveHorizon - fadeInEndAtHorizon)));
        }

        // 뒤로 넘어갈 때 페이드 아웃
        if (a < fadeOutStartBack)
        {
            vis *= Mathf.Clamp01((a - fadeOutEndBack) / Mathf.Max(0.0001f, (fadeOutStartBack - fadeOutEndBack)));
        }

        // 일출/일몰 붉은기 보정: 낮을수록(0도에 가까울수록) 색 포화도 증가
        float warmBoost = Mathf.Clamp01(1f - Mathf.InverseLerp(yellowEnd, 0f, Mathf.Clamp(a, dayEnd, 0f)));
        color = Saturation(color, 0.85f + 0.25f * warmBoost); // 0.85~1.1 배

        emission *= vis;
        color *= Mathf.Max(vis, 0.0001f); // 거의 안 보일 땐 색도 약하게
    }

    void Apply(Color c, float e)
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        sunRenderer.GetPropertyBlock(_mpb);

        // _BaseColor가 없으면 _Color로 시도
        int id = _idColor;
        var mat = sunRenderer.sharedMaterial;
        if (mat != null && !mat.HasProperty(id))
            id = Shader.PropertyToID("_Color");

        _mpb.SetColor(id, c);

        if (alsoSetEmission)
        {
            // 에미션은 따뜻한 색 위주로 보이게 약간 더 노랗게 치우침
            Color warmBias = Color.Lerp(c, warmWhite, 0.25f);
            _mpb.SetColor(_idEmission, warmBias * e);
            if (mat != null) mat.EnableKeyword("_EMISSION");
        }

        sunRenderer.SetPropertyBlock(_mpb);
    }

    // --- helpers ---
    static float InverseLerp(float a, float b, float v)
    {
        if (Mathf.Approximately(a, b)) return 0f;
        return Mathf.Clamp01((v - a) / (b - a));
    }
    static float Smooth01(float x) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x));

    static Color Saturation(Color c, float sMul)
    {
        // 간단 HSV-like: 회색 성분과 섞기
        float g = (c.r + c.g + c.b) / 3f;
        return new Color(
            Mathf.Clamp01(g + (c.r - g) * sMul),
            Mathf.Clamp01(g + (c.g - g) * sMul),
            Mathf.Clamp01(g + (c.b - g) * sMul),
            1f
        );
    }
}
