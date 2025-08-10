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
    float _nextHandsSearchTime;

    [Tooltip("손을 풀면 다시 시작 같은 문구가 있는 CanvasGroup")]
    public CanvasGroup freezeMsg;

    [Header("Things to stop")]
    public List<ParticleSystem> particleSystems = new();
    public List<VisualEffect>  vfxGraphs       = new();
    public List<Animator>      animators       = new();
    [Tooltip("Freeze 동안 비활성화할 스크립트들(예: SkyRotator, WaveScroller 등)")]
    public List<MonoBehaviour> behavioursToDisable = new();

    [System.Serializable]
    public class MaterialSpeed
    {
        public Material material;
        public string floatProperty = "_Speed";
        public float normalValue = 1f;
        public float frozenValue = 0f;
    }
    public List<MaterialSpeed> materials = new();

    [Header("Freeze 옵션")]
    public bool freezeTimeScale = true;   // 전체 게임 시간 멈춤
    public bool pauseAllBoats   = true;   // 장면의 모든 BoatBuoyancy 일시정지

    [Header("클렌치 판정")]
    [Range(0.5f, 0.99f)] public float curlThreshold = 0.95f;
    public bool requireAllFingers = true;

    [Header("디버그 (TMP 전용)")]
    public bool showDebugOnScreen = true;
    public bool showDebugInConsole = true;
    public TMP_Text debugTMP;

    // 내부 상태
    bool _isFrozen;
    float _fadeVel;

    // 디버그 캐시
    string _lastDebug = "";
    float _lastLogTime;
    bool _loggedMissingTMP;

    // 보트 캐시
    readonly List<BoatBuoyancy> _boats = new();

    // 컬러풀 표시
    const string OK = "<color=#3AD46B>✔</color>";
    const string NO = "<color=#FF5555>✖</color>";
    const string YS = "<color=#3AD46B>YES</color>";
    const string NOPE = "<color=#FF5555>NO</color>";

    void Awake()
    {
        TryAcquireHandsSubsystem();
        RefreshBoatsList(); // 장면의 BoatBuoyancy 캐시
    }

    IEnumerator Start()
    {
        if (debugTMP)
        {
            debugTMP.raycastTarget = false;
            debugTMP.enableWordWrapping = true;
            debugTMP.alignment = TextAlignmentOptions.TopLeft;
        }

        var mgr = XRGeneralSettings.Instance?.Manager;
        if (mgr != null && !mgr.isInitializationComplete)
            yield return mgr.InitializeLoader();

        mgr?.StartSubsystems();

        var descs = new List<XRHandSubsystemDescriptor>();
        SubsystemManager.GetSubsystemDescriptors(descs);

        var hands = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(hands);

        TryAcquireHandsSubsystem();
    }

    void Update()
    {
        // XR Hands 재탐색
        if (_hands == null && Time.unscaledTime >= _nextHandsSearchTime)
        {
            TryAcquireHandsSubsystem();
            _nextHandsSearchTime = Time.unscaledTime + 1.0f;
        }

        bool clenched = EvaluateRightFist(out _lastDebug);

        if (clenched && !_isFrozen) SetFrozen(true);
        else if (!clenched && _isFrozen) SetFrozen(false);

        // 메시지 페이드 (timeScale=0에서도 보이도록 unscaledDeltaTime)
        if (freezeMsg)
        {
            float target = _isFrozen ? 1f : 0f;
            freezeMsg.alpha = Mathf.SmoothDamp(
                freezeMsg.alpha, target, ref _fadeVel,
                0.08f, Mathf.Infinity, Time.unscaledDeltaTime
            );
            freezeMsg.blocksRaycasts = _isFrozen;
            freezeMsg.interactable  = _isFrozen;
        }

        // 화면/콘솔 디버그
        if (showDebugOnScreen)
        {
            if (debugTMP) debugTMP.text = _lastDebug;
            else if (!_loggedMissingTMP) { Debug.LogError("[FreezeOnClench] TMP_Text 미할당"); _loggedMissingTMP = true; }
        }
        if (showDebugInConsole && (Time.unscaledTime - _lastLogTime) > 0.25f)
        {
            Debug.Log(_lastDebug);
            _lastLogTime = Time.unscaledTime;
        }
    }

    void TryAcquireHandsSubsystem()
    {
        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        if (loader != null)
        {
            var fromLoader = loader.GetLoadedSubsystem<XRHandSubsystem>();
            if (fromLoader != null) { _hands = fromLoader; return; }
        }
        var list = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(list);
        if (list.Count > 0) _hands = list[0];
    }

    bool EvaluateRightFist(out string debugText)
    {
        if (_hands == null) { debugText = "<b>[FreezeOnClench]</b>\nXRHandSubsystem = <color=#FF5555>null</color>"; return false; }

        var right = _hands.rightHand;
        if (!right.isTracked)
        {
            debugText = $"<b>[FreezeOnClench]</b>\nRightHand: <color=#FFAA00>NOT TRACKED</color>\n" +
                        $"isFrozen={_isFrozen}  threshold={curlThreshold:F2}  requireAll={requireAllFingers}";
            return false;
        }

        if (!TryGet(right, XRHandJointID.Wrist, out var wrist))
        {
            debugText = "<b>[FreezeOnClench]</b>\nRightHand: tracked, BUT <color=#FF5555>wrist pose missing</color>";
            return false;
        }

        bool idx = FingerCurled(right, XRHandJointID.IndexProximal,  XRHandJointID.IndexTip,  wrist, curlThreshold, out float rIdx);
        bool mid = FingerCurled(right, XRHandJointID.MiddleProximal, XRHandJointID.MiddleTip, wrist, curlThreshold, out float rMid);
        bool rng = FingerCurled(right, XRHandJointID.RingProximal,   XRHandJointID.RingTip,   wrist, curlThreshold, out float rRng);
        bool ltl = FingerCurled(right, XRHandJointID.LittleProximal, XRHandJointID.LittleTip, wrist, curlThreshold, out float rLtl);

        int curledCount = CountTrue(idx, mid, rng, ltl);
        bool clenched = requireAllFingers ? (curledCount == 4) : (curledCount >= 3);

        debugText =
            $"<b>[FreezeOnClench]</b>\nRightHand: <color=#3AD46B>tracked</color>\n" +
            $"threshold={curlThreshold:F2}, requireAll={requireAllFingers}\n" +
            $"Idx:{(idx?OK:NO)}({rIdx:F2})  Mid:{(mid?OK:NO)}({rMid:F2})  " +
            $"Rng:{(rng?OK:NO)}({rRng:F2})  Ltl:{(ltl?OK:NO)}({rLtl:F2})\n" +
            $"CurledCount={curledCount}  ->  CLENCHED={(clenched?YS:NOPE)}\n" +
            $"isFrozen={_isFrozen}  timeScale={Time.timeScale}";
        return clenched;
    }

    static int CountTrue(params bool[] arr) { int c = 0; foreach (var b in arr) if (b) c++; return c; }

    bool FingerCurled(XRHand hand, XRHandJointID proximal, XRHandJointID tip, Pose wrist, float threshold, out float ratio)
    {
        ratio = -1f;
        if (!TryGet(hand, proximal, out var prox)) return false;
        if (!TryGet(hand, tip, out var tipPose))   return false;

        float tipToWrist  = Vector3.Distance(tipPose.position, wrist.position);
        float baseToWrist = Vector3.Distance(prox.position,     wrist.position) + 1e-5f;

        ratio = tipToWrist / baseToWrist; // 낮을수록 더 말림
        return (ratio < threshold);
    }

    static bool TryGet(XRHand hand, XRHandJointID id, out Pose pose)
    {
        var j = hand.GetJoint(id);
        return j.TryGetPose(out pose);
    }

    // === Freeze 토글 ===
    void SetFrozen(bool value)
    {
        _isFrozen = value;

        if (freezeTimeScale) Time.timeScale = value ? 0f : 1f;

        foreach (var ps in particleSystems) if (ps) { if (value) ps.Pause(true); else ps.Play(true); }
        foreach (var vfx in vfxGraphs)      if (vfx) vfx.pause = value;
        foreach (var an  in animators)      if (an)  an.speed = value ? 0f : 1f;
        foreach (var b   in behavioursToDisable) if (b) b.enabled = !value;

        foreach (var m in materials)
            if (m.material && !string.IsNullOrEmpty(m.floatProperty))
                m.material.SetFloat(m.floatProperty, value ? m.frozenValue : m.normalValue);

        if (pauseAllBoats)
        {
            RefreshBoatsList();
            foreach (var boat in _boats) if (boat) boat.SetPaused(value, snapToWater:true);
        }
    }

    void RefreshBoatsList()
    {
        _boats.Clear();
        _boats.AddRange(FindObjectsOfType<BoatBuoyancy>(true));
    }
}
