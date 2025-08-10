using UnityEngine;

[DisallowMultipleComponent]
public class BoatBuoyancy : MonoBehaviour
{
    [Header("References")]
    public Transform water;                  // 바다 Transform
    public bool autoFindWater = true;        // Tag=Water 자동 연결

    [Header("Float")]
    [Range(0f, 10f)] public float strength = 4f;   // 스프링
    [Range(0f,  2f)] public float damping  = 0.8f; // 감쇠
    public float heightOffset = 0.0f;              // 드래프트

    [Header("Waves (shader와 값 동일)")]
    public float waveSpeed  = 1f;
    public float waveScale  = 0.15f;
    public float waveHeight = 0.4f;

    [Header("Physics (선택)")]
    public bool useRigidbody = false;
    Rigidbody rb;

    float velY;
    bool _paused;

    public bool IsPaused => _paused;

    void Awake()
    {
        if (autoFindWater && !water)
        {
            var w = GameObject.FindWithTag("Water");
            if (w) water = w.transform;
        }

        if (useRigidbody)
        {
            rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Start() { SnapToWater(); }

    void FixedUpdate()
    {
        if (useRigidbody && !_paused) Tick(Time.fixedDeltaTime);
    }

    void Update()
    {
        if (!useRigidbody && !_paused) Tick(Time.deltaTime);
    }

    void Tick(float dt)
    {
        if (!water) return;

        var p = transform.position;

        float targetY = water.position.y + QueryWaveY(p, Time.time) + heightOffset;

        // 스프링-댐퍼 적분 (dt 필수)
        float error = targetY - p.y;
        velY += error * strength * dt;
        velY *= Mathf.Clamp01(1f - damping * dt);

        float newY = p.y + velY;

        if (useRigidbody && rb) rb.MovePosition(new Vector3(p.x, newY, p.z));
        else                    transform.position = new Vector3(p.x, newY, p.z);
    }

    float QueryWaveY(Vector3 worldPos, float time)
    {
        float t = time * waveSpeed;
        return Mathf.Sin(worldPos.x * waveScale + t) *
               Mathf.Sin(worldPos.z * waveScale + t) *
               waveHeight;
    }

    [ContextMenu("Snap to Water Now")]
    public void SnapToWater()
    {
        if (!water) return;

        var p = transform.position;
        float y = water.position.y + QueryWaveY(p, Time.time) + heightOffset;

        if (useRigidbody && rb) rb.position = new Vector3(p.x, y, p.z);
        else                    transform.position = new Vector3(p.x, y, p.z);

        velY = 0f;
    }

    /// <summary>
    /// Freeze에서 호출. snapToWater=true면 즉시 수면으로 붙이고 속도 0.
    /// </summary>
    public void SetPaused(bool pause, bool snapToWater)
    {
        _paused = pause;

        // 튐 방지: 속도/각속도 초기화
        velY = 0f;
        if (useRigidbody && rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (snapToWater) SnapToWater();
    }
}
