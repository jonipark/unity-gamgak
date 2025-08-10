using UnityEngine;

/// <summary>
/// X 로테이션 각도에 따라 배경 스피어의 하늘을
/// "수직 그라데이션 텍스처"로 동적으로 칠한다(셰이더 추가 불필요).
/// 0~-10: 빨강 위주, -10~-20: 주황/노랑, -30~-160: 하늘색,
/// -160~-170: 주황/노랑, -170~-180: 빨강.
/// 해가 수평선 근처일수록 그라데이션 밴드 폭을 넓혀 "하늘 대부분"이 따뜻하게 물든다.
/// </summary>
[ExecuteAlways]
public class SkyGradientByXRotation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer skyRenderer;
    [Tooltip("회전 각도를 읽어올 트랜스폼(비우면 이 스크립트가 붙은 오브젝트)")]
    [SerializeField] private Transform rotationSource;

    [Header("Colors (HDR 가능)")]
    [ColorUsage(true, true)] public Color night = new Color(0.04f, 0.06f, 0.10f);
    [ColorUsage(true, true)] public Color dawnRed = new Color(0.85f, 0.25f, 0.20f);
    [ColorUsage(true, true)] public Color sunriseOrange = new Color(1.00f, 0.75f, 0.25f);
    [ColorUsage(true, true)] public Color skyBlue = new Color(0.35f, 0.58f, 0.95f);

    [Header("Material Properties")]
    [Tooltip("URP/Lit의 메인 텍스처 슬롯")]
    public string baseMapProperty = "_BaseMap";
    [Tooltip("URP/Lit의 베이스 컬러(틴트) 슬롯")]
    public string baseColorProperty = "_BaseColor";
    [Tooltip("Emission도 같이 넣기(URP/Lit)")]
    public bool alsoSetEmission = true;
    [Range(0f, 2f)] public float emissionMultiplier = 0.2f;

    [Header("Gradient Texture")]
    [Tooltip("그라데이션 해상도(세로 픽셀). 256~1024 권장")]
    [Range(64, 2048)] public int gradientHeight = 512;
    [Tooltip("수평선의 V 위치(0=바닥, 1=천정). 기본 0.5=적도")]
    [Range(0f, 1f)] public float horizonV = 0.5f;
    [Tooltip("그라데이션 V 뒤집기(메시 UV가 반대일 때)")]
    public bool flipV = false;

    [Header("Band Width (coverage)")]
    [Tooltip("해가 막 뜨고/질 때 따뜻한색이 퍼지는 최대 폭(가우시안 시그마)")]
    [Range(0.05f, 0.6f)] public float sigmaWide = 0.38f;
    [Tooltip("해가 조금 올라갔을 때의 기본 폭")]
    [Range(0.02f, 0.45f)] public float sigmaMedium = 0.22f;
    [Tooltip("낮/밤에 남는 아주 얇은 폭(사실상 0으로 두면 꺼짐)")]
    [Range(0f, 0.2f)] public float sigmaNarrow = 0.04f;

    [Header("Blend & Smoothing")]
    [Tooltip("경계 구간 부드럽게(지수 스무딩 강도)")]
    [Range(0f, 10f)] public float smooth = 3f;
    [Tooltip("따뜻한색이 하늘 ‘전반’에 베이는 양(0=밴드만, 1=하늘 전체에 틴트)")]
    [Range(0f, 1f)] public float globalWarmTint = 0.45f;

    // 내부
    private MaterialPropertyBlock _mpb;
    private Texture2D _gradTex;
    private Color[] _row;     // 한 줄 버퍼
    private float _lastRenderedAngle = 9999f; // 최초 강제 업데이트
    private Color _lastBaseTint, _curBaseTint;

    // 캐시된 프로퍼티 ID
    private int _idBaseMap, _idBaseColor, _idEmission;

    void Reset()
    {
        if (!skyRenderer) TryGetComponent(out skyRenderer);
        rotationSource = transform;
    }

    void OnEnable()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        if (!rotationSource) rotationSource = transform;

        _idBaseMap = Shader.PropertyToID(baseMapProperty);
        _idBaseColor = Shader.PropertyToID(baseColorProperty);
        _idEmission = Shader.PropertyToID("_EmissionColor");

        AllocateTexture();
        ForceUpdate(true);
    }

    void OnDisable()
    {
        if (_gradTex)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(_gradTex);
            else Destroy(_gradTex);
#else
            Destroy(_gradTex);
#endif
            _gradTex = null;
        }
    }

    void AllocateTexture()
    {
        if (_gradTex && _gradTex.height == gradientHeight) return;

        if (_gradTex)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(_gradTex);
            else Destroy(_gradTex);
#else
            Destroy(_gradTex);
#endif
        }

        _gradTex = new Texture2D(2, Mathf.Max(64, gradientHeight), TextureFormat.RGBA32, false, true);
        _gradTex.name = "SkyGradientRuntime";
        _gradTex.wrapMode = TextureWrapMode.Clamp;
        _gradTex.filterMode = FilterMode.Bilinear;

        _row = new Color[_gradTex.width];
    }

    void Update()
    {
        if (!skyRenderer) return;

        float a = GetSignedX();

        // 성능: 각도 변화 작으면 텍스처 갱신 생략(에디터에서는 30fps 정도로)
        float threshold = Application.isPlaying ? 0.25f : 1.0f;
        if (Mathf.Abs(Mathf.DeltaAngle(_lastRenderedAngle, a)) > threshold)
        {
            UpdateGradient(a);
            _lastRenderedAngle = a;
        }

        // 베이스 컬러(틴트)도 부드럽게 보간
        float dt = Application.isPlaying ? Time.deltaTime : 1f / 30f;
        float k = smooth > 0f ? 1f - Mathf.Exp(-smooth * dt) : 1f;
        _lastBaseTint = Color.Lerp(_lastBaseTint, _curBaseTint, k);

        ApplyToMaterial(_lastBaseTint);
    }

    // [-180, 180] 부호 있는 X 각
    float GetSignedX()
    {
        var t = rotationSource ? rotationSource : transform;
        float ax = t.eulerAngles.x;
        return (ax > 180f) ? ax - 360f : ax;
    }

    /// <summary>
    /// 각도에 맞춰 그라데이션 텍스처를 다시 채움
    /// </summary>
    void UpdateGradient(float a)
    {
        AllocateTexture();

        // 1) 각도→색/강도/폭 계산
        EvaluatePhase(a, out Color warmColor, out float coverageSigma, out float bandIntensity, out Color baseTint);

        _curBaseTint = baseTint;

        // 2) 수직 그라데이션 채우기 (가우시안 밴드 + 전역 틴트)
        int h = _gradTex.height;
        for (int y = 0; y < h; y++)
        {
            // v(0..1), 수평선까지의 거리
            float v01 = flipV ? (1f - (y + 0.5f) / h) : ((y + 0.5f) / h);
            float d = Mathf.Abs(v01 - horizonV);

            // 가우시안 프로파일: 수평선 근처에서 가장 강하고, coverageSigma에 따라 퍼짐
            float sigma = Mathf.Max(0.0001f, coverageSigma);
            float g = Mathf.Exp(-0.5f * (d * d) / (sigma * sigma));

            // 따뜻한 밴드 강도(0..1)
            float band = Mathf.Clamp01(g * bandIntensity);

            // 전역 틴트: 하늘 전체를 살짝 따뜻하게
            float global = globalWarmTint * bandIntensity; // 밴드가 강할수록 전역도 강해짐
            global = Mathf.Clamp01(global);

            // 밴드 색과 하늘색, 전역 틴트를 조합
            Color cold = skyBlue;
            Color warm = warmColor;

            // 먼저 전역 틴트로 기본 하늘색을 살짝 데운 다음,
            // 수평선 밴드를 더함.
            Color warmedSky = Color.Lerp(cold, warm, global);
            Color finalRow = Color.Lerp(warmedSky, warm, band);

            // 최소/최대 밝기 클램프(너무 하얗게 씻기는 것 방지)
            finalRow = ClampLuminance(finalRow, 0.02f, 0.98f);

            // 2픽셀 폭 전부 같은 색
            for (int x = 0; x < _row.Length; x++) _row[x] = finalRow;
            _gradTex.SetPixels(0, y, _gradTex.width, 1, _row);
        }

        _gradTex.Apply(false, false);
    }

    /// <summary>
    /// 각도에 따른 단계 평가:
    /// - 따뜻한 대표색(빨강/주황) 선택
    /// - 밴드 폭(sigma)와 강도, 베이스 틴트 반환
    /// </summary>
    void EvaluatePhase(float a, out Color warmColor, out float sigma, out float intensity, out Color baseTint)
    {
        // 공통 유틸
        float RiseT(float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-40f, 20f, x));      // -40→20
        float SetT (float x) => Mathf.SmoothStep(0f, 1f, 1f - Mathf.InverseLerp(-180f, -160f, Mathf.Clamp(x, -180f, -160f))); // -180→-160

        // 1) 구간별 컬러 보간
        // 해 뜰 때: -40↗20로 갈수록 주황/노랑 → 빨강
        float tRise = RiseT(a);
        Color riseColor = Color.Lerp(sunriseOrange, dawnRed, tRise);

        // 해 질 때: -160↘-180로 갈수록 주황/노랑 → 빨강
        float tSet = SetT(a);
        Color setColor = Color.Lerp(sunriseOrange, dawnRed, tSet);

        // 낮/밤 기본 하늘색
        Color dayBase = skyBlue;
        Color nightBase = night;

        // 2) 강도(각도 가까울수록 크고, 경계 지나도 서서히 감소)
        float riseIntensity = EaseOutCubic(tRise); // 0..1
        float setIntensity  = EaseOutCubic(tSet);

        // 낮 구간에서 남는 잔향(아주 약한 밴드)
        float dayTail = Mathf.SmoothStep(0f, 0.25f, Mathf.InverseLerp(-160f, -30f, a)); // -160→-30로 갈수록 0→0.25

        // 밤 구간 잔향
        float nightTail = Mathf.SmoothStep(0f, 0.15f, Mathf.InverseLerp(-30f, -180f, a < -180f ? -180f : a));

        // 두 강도 중 큰 쪽을 채택(해 뜨는 쪽/지는 쪽 대칭)
        if (riseIntensity >= setIntensity)
        {
            warmColor = riseColor;
            intensity = Mathf.Max(riseIntensity, dayTail);
        }
        else
        {
            warmColor = setColor;
            intensity = Mathf.Max(setIntensity, nightTail);
        }

        // 3) 밴드 폭(하늘 대부분 물들이기 ↔ 얇게)
        // 해가 수평선에 가까울수록 폭을 넓힘
        float wideFactor = Mathf.Max(riseIntensity, setIntensity);
        sigma = Mathf.Lerp(sigmaMedium, sigmaWide, wideFactor);
        // 아주 낮/밤에는 좁힘
        if (wideFactor < 0.05f) sigma = Mathf.Lerp(sigmaNarrow, sigmaMedium, dayTail + nightTail);

        // 4) 베이스 틴트(머티리얼 BaseColor)
        // 하늘 전체를 살짝 데우기(너무 밝아지지 않게 0.4 가중치)
        baseTint = Color.Lerp(dayBase, warmColor, 0.4f * intensity);
    }

    static float EaseOutCubic(float x)
    {
        // 0..1 → 느리게 시작, 빠르게 줄어드는 형상
        float o = 1f - x;
        return 1f - o * o * o;
    }

    static Color ClampLuminance(Color c, float minLuma, float maxLuma)
    {
        // 단순 Y' = 0.2126R + 0.7152G + 0.0722B
        float y = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        float yClamped = Mathf.Clamp(y, minLuma, maxLuma);
        if (Mathf.Approximately(y, 0f)) return c;
        float k = yClamped / y;
        return new Color(c.r * k, c.g * k, c.b * k, 1f);
    }

    void ApplyToMaterial(Color baseTint)
    {
        skyRenderer.GetPropertyBlock(_mpb);

        // 베이스 텍스처/컬러 적용
        if (_gradTex) _mpb.SetTexture(_idBaseMap, _gradTex);
        _mpb.SetColor(_idBaseColor, baseTint);

        if (alsoSetEmission)
        {
            // 에미션은 너무 과도하지 않도록 평균 밝기 기반으로 스케일
            float avg = (baseTint.r + baseTint.g + baseTint.b) / 3f;
            Color e = baseTint * (emissionMultiplier * Mathf.Lerp(0.5f, 1.2f, avg));
            _mpb.SetColor(_idEmission, e);

            // 키워드는 머티리얼 쪽에서 한 번만 켜주면 더 확실(Inspector에서 Emission 체크)
            // 여기선 PropertyBlock만 설정
        }

        skyRenderer.SetPropertyBlock(_mpb);
    }

    /// <summary>외부에서 강제 갱신하고 싶을 때 호출</summary>
    public void ForceUpdate(bool rebuildTexture)
    {
        if (rebuildTexture) AllocateTexture();
        float a = GetSignedX();
        UpdateGradient(a);
        _lastRenderedAngle = a;
        _lastBaseTint = _curBaseTint;
        ApplyToMaterial(_curBaseTint);
    }
}
