using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using TMPro;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using UnityEngine.SubsystemsImplementation;

public class FreezeOnClench : MonoBehaviour
{
    [Header("XR Hands")]
    XRHandSubsystem _hands;
    float _nextHandsSearchTime; // XRHands 늦게 뜰 때 재탐색 간격 제어

    [Tooltip("손을 풀면 다시 시작 같은 문구가 있는 CanvasGroup")]
    public CanvasGroup freezeMsg; // alpha 0~1로 표시/숨김

    [Header("Things to stop")]
    public List<ParticleSystem> particleSystems = new();
    public List<VisualEffect> vfxGraphs = new();
    public List<Animator> animators = new();
    [Tooltip("Freeze 동안 비활성화할 스크립트들(예: SkyRotator, WaveScroller 등)")]
    public List<MonoBehaviour> behavioursToDisable = new();

    [System.Serializable]
    public class MaterialSpeed
    {
        public Material material;
        public string floatProperty = "_Speed"; // 물/하늘 셰이더의 속도 파라미터 이름
        public float normalValue = 1f;
        public float frozenValue = 0f;
    }
    public List<MaterialSpeed> materials = new();

    [Header("클렌치 판정")]
    [Tooltip("손등이 카메라쪽일 때 오검출 줄이려면 값 좀 올리기")]
    [Range(0.5f, 0.9f)] public float curlThreshold = 0.9f; // 작을수록 ‘더 말아쥔’ 손가락만 인식
    [Tooltip("모든 손가락이 임계 이하일 때를 주먹으로 간주")]
    public bool requireAllFingers = true;

    [Header("디버그 (TMP 전용)")]
    public bool showDebugOnScreen = true;
    public bool showDebugInConsole = true;
    [Tooltip("화면에 디버그를 표시할 TMP_Text")]
    public TMP_Text debugTMP;

    // 내부 상태
    bool _isFrozen;
    float _fadeVel;

    // 디버그 캐시
    string _lastDebug = "";
    float _lastLogTime;
    bool _loggedMissingTMP;

    // 컬러풀 표시
    const string OK = "<color=#3AD46B>✔</color>";
    const string NO = "<color=#FF5555>✖</color>";
    const string YS = "<color=#3AD46B>YES</color>";
    const string NOPE = "<color=#FF5555>NO</color>";

    void Awake()
    {
        TryAcquireHandsSubsystem(); // 초기 시도
        if (_hands == null)
            Debug.LogWarning("[FreezeOnClench] XRHandSubsystem 초기 획득 실패. 설정/초기화 중일 수 있음. 재탐색합니다.");
    }

    // XR 초기화 타이밍 문제/안드로이드 빌드에서 진단 & 로더 강제 스타트
    IEnumerator Start()
    {
        if (debugTMP)
        {
            debugTMP.raycastTarget = false;
            debugTMP.enableWordWrapping = true;
            debugTMP.alignment = TextAlignmentOptions.TopLeft;
        }

        var mgr = XRGeneralSettings.Instance?.Manager;
        Debug.Log($"[XRDiag] initComplete={mgr?.isInitializationComplete}");

        // 자동 초기화가 꺼져 있거나 지연될 수 있으므로 보강
        if (mgr != null && !mgr.isInitializationComplete)
            yield return mgr.InitializeLoader();

        Debug.Log($"[XRDiag] activeLoader={mgr?.activeLoader?.GetType().Name ?? "null"}");

        mgr?.StartSubsystems();
        Debug.Log("[XRDiag] StartSubsystems called");

        // 디스크립터/인스턴스 존재 여부 확인 (스트리핑 이슈 진단용)
        var descs = new List<XRHandSubsystemDescriptor>();
        SubsystemManager.GetSubsystemDescriptors(descs);
        Debug.Log($"[XRDiag] XRHandSubsystemDescriptor count={descs.Count}");

        var hands = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(hands);
        Debug.Log($"[XRDiag] XRHandSubsystem instances={hands.Count}");

        TryAcquireHandsSubsystem(); // 한 번 더 붙잡기
    }

    void Update()
    {
        // 아직 못 잡았으면 1초 간격으로 재탐색
        if (_hands == null && Time.unscaledTime >= _nextHandsSearchTime)
        {
            TryAcquireHandsSubsystem();
            _nextHandsSearchTime = Time.unscaledTime + 1.0f;
        }

        bool clenched = EvaluateRightFist(out _lastDebug);

        // 상태 전이 로그
        if (clenched && !_isFrozen) Debug.Log("[FreezeOnClench] State -> FROZEN (clench detected)");
        else if (!clenched && _isFrozen) Debug.Log("[FreezeOnClench] State -> UNFROZEN (released)");

        if (clenched && !_isFrozen) SetFrozen(true);
        else if (!clenched && _isFrozen) SetFrozen(false);

        // 메시지 페이드 (timeScale=0에서도 보이도록 unscaledDeltaTime 사용)
        if (freezeMsg)
        {
            float target = _isFrozen ? 1f : 0f;
            freezeMsg.alpha = Mathf.SmoothDamp(freezeMsg.alpha, target, ref _fadeVel, 0.08f, Mathf.Infinity, Time.unscaledDeltaTime);
            freezeMsg.blocksRaycasts = _isFrozen;
            freezeMsg.interactable = _isFrozen;
        }

        // 화면 디버그 (TMP)
        if (showDebugOnScreen)
        {
            if (debugTMP) debugTMP.text = _lastDebug;
            else if (!_loggedMissingTMP)
            {
                Debug.LogError("[FreezeOnClench] debugTMP가 비어 있습니다. TMP_Text를 할당하세요.");
                _loggedMissingTMP = true;
            }
        }

        // 콘솔 디버그 (스팸 방지)
        if (showDebugInConsole && (Time.unscaledTime - _lastLogTime) > 0.25f)
        {
            Debug.Log(_lastDebug);
            _lastLogTime = Time.unscaledTime;
        }
    }

    // XRHandSubsystem 획득: activeLoader 우선, 그다음 열거 방식
    void TryAcquireHandsSubsystem()
    {
        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        if (loader != null)
        {
            var fromLoader = loader.GetLoadedSubsystem<XRHandSubsystem>();
            if (fromLoader != null)
            {
                _hands = fromLoader;
                Debug.Log("[FreezeOnClench] XRHandSubsystem 연결됨 (activeLoader).");
                return;
            }
        }

        var list = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(list);
        if (list.Count > 0)
        {
            _hands = list[0];
            Debug.Log("[FreezeOnClench] XRHandSubsystem 연결됨 (enumerate).");
        }
    }

    // 오른손 클렌치 판정 + 디버그 문자열 생성
    bool EvaluateRightFist(out string debugText)
    {
        if (_hands == null)
        {
            debugText = "<b>[FreezeOnClench]</b>\nXRHandSubsystem = <color=#FF5555>null</color>";
            return false;
        }

        var right = _hands.rightHand;
        if (!right.isTracked)
        {
            debugText =
                $"<b>[FreezeOnClench]</b>\n" +
                $"RightHand: <color=#FFAA00>NOT TRACKED</color>\n" +
                $"isFrozen={_isFrozen}  threshold={curlThreshold:F2}  requireAll={requireAllFingers}";
            return false;
        }

        if (!TryGet(right, XRHandJointID.Wrist, out var wrist))
        {
            debugText = "<b>[FreezeOnClench]</b>\nRightHand: tracked, BUT <color=#FF5555>wrist pose missing</color>";
            return false;
        }

        // 각 손가락 말림 측정 (tip↔wrist / proximal↔wrist 비율)
        bool idx = FingerCurled(right, XRHandJointID.IndexProximal,  XRHandJointID.IndexTip,  wrist, curlThreshold, out float rIdx);
        bool mid = FingerCurled(right, XRHandJointID.MiddleProximal, XRHandJointID.MiddleTip, wrist, curlThreshold, out float rMid);
        bool rng = FingerCurled(right, XRHandJointID.RingProximal,   XRHandJointID.RingTip,   wrist, curlThreshold, out float rRng);
        bool ltl = FingerCurled(right, XRHandJointID.LittleProximal, XRHandJointID.LittleTip, wrist, curlThreshold, out float rLtl);

        int curledCount = CountTrue(idx, mid, rng, ltl);
        bool clenched = requireAllFingers ? (curledCount == 4) : (curledCount >= 3);

        debugText =
            $"<b>[FreezeOnClench]</b>\n" +
            $"RightHand: <color=#3AD46B>tracked</color>\n" +
            $"threshold={curlThreshold:F2}, requireAll={requireAllFingers}\n" +
            $"Idx: {(idx ? OK : NO)}  ratio={rIdx:F2}\n" +
            $"Mid: {(mid ? OK : NO)}  ratio={rMid:F2}\n" +
            $"Rng: {(rng ? OK : NO)}  ratio={rRng:F2}\n" +
            $"Ltl: {(ltl ? OK : NO)}  ratio={rLtl:F2}\n" +
            $"CurledCount={curledCount}  ->  CLENCHED={(clenched ? YS : NOPE)}\n" +
            $"isFrozen={_isFrozen}  timeScale={Time.timeScale}";

        return clenched;
    }

    static int CountTrue(params bool[] arr) { int c = 0; foreach (var b in arr) if (b) c++; return c; }

    bool FingerCurled(XRHand hand, XRHandJointID proximal, XRHandJointID tip, Pose wrist, float threshold, out float ratio)
    {
        ratio = -1f;
        if (!TryGet(hand, proximal, out var prox)) return false;
        if (!TryGet(hand, tip, out var tipPose)) return false;

        float tipToWrist  = Vector3.Distance(tipPose.position, wrist.position);
        float baseToWrist = Vector3.Distance(prox.position,     wrist.position) + 1e-5f;

        ratio = tipToWrist / baseToWrist;              // 낮을수록 더 말림
        return (ratio < threshold);
    }

    static bool TryGet(XRHand hand, XRHandJointID id, out Pose pose)
    {
        var j = hand.GetJoint(id);
        return j.TryGetPose(out pose);
    }

    void SetFrozen(bool value)
    {
        _isFrozen = value;

        // 1) 전체 시간 정지 (오디오 재생은 기본적으로 계속됨)
        Time.timeScale = value ? 0f : 1f;

        // 2) 파티클 & VFX & 애니메이터 정지/재개
        foreach (var ps in particleSystems) { if (!ps) continue; if (value) ps.Pause(true); else ps.Play(true); }
        foreach (var vfx in vfxGraphs) { if (!vfx) continue; vfx.pause = value; }
        foreach (var an in animators) { if (!an) continue; an.speed = value ? 0f : 1f; }

        // 3) 스크립트 껐다 켜기
        foreach (var b in behavioursToDisable) if (b) b.enabled = !value;

        // 4) 셰이더 속도 파라미터 제어
        foreach (var m in materials)
        {
            if (m.material && !string.IsNullOrEmpty(m.floatProperty))
                m.material.SetFloat(m.floatProperty, value ? m.frozenValue : m.normalValue);
        }
    }
}
