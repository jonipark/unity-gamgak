// using UnityEngine;

// public class BoatBuoyancy : MonoBehaviour
// {
//     public Transform waterPlane; // Assign in Inspector
//     public float floatStrength = 1f;
//     public float damping = 0.9f;

//     private float currentVelocity = 0f;

//     void Update()
//     {
//         if (waterPlane == null) return;

//         Vector3 worldPos = transform.position;
//         Vector3 localPos = waterPlane.InverseTransformPoint(worldPos);

//         // These must match your water shader settings
//         float waveSpeed = 1f;
//         float waveScale = 0.2f;
//         float waveHeight = 0.5f;

//         float time = Time.time * waveSpeed;

//         // Safe wave Y calculation
//         float waveY = Mathf.Sin(localPos.x * waveScale + time) *
//                       Mathf.Sin(localPos.z * waveScale + time) *
//                       waveHeight;

//         float targetY = waterPlane.position.y + waveY;
//         float currentY = transform.position.y;
//         float difference = targetY - currentY;

//         // Add safety to damping and floatStrength
//         floatStrength = Mathf.Clamp(floatStrength, 0f, 10f);
//         damping = Mathf.Clamp(damping, 0.01f, 0.99f);

//         currentVelocity = (currentVelocity + difference * floatStrength) * damping;

//         float newY = currentY + currentVelocity + 50f;

//         // Clamp to avoid invalid position
//         if (float.IsInfinity(newY) || float.IsNaN(newY)) return;

//         transform.position = new Vector3(transform.position.x, newY, transform.position.z);
//     }
// }

using UnityEngine;

[DisallowMultipleComponent]
public class BoatBuoyancy : MonoBehaviour
{
    [Header("References")]
    public Transform water;                  // 물 오브젝트(바다) Transform
    public bool autoFindWater = true;        // 물 오브젝트 자동 탐색 (Tag=Water 권장)

    [Header("Float")]
    [Range(0f, 10f)] public float strength = 4f;   // 떠오르는 힘(스프링)
    [Range(0f, 2f)]  public float damping  = 0.8f; // 감쇠(댐퍼)
    public float heightOffset = 0.0f;              // 배마다 드래프트(물 위로 얼마나 더)

    [Header("Waves (shader와 값 맞추기)")]
    public float waveSpeed  = 1f;
    public float waveScale  = 0.15f;   // 주파수(빈도)
    public float waveHeight = 0.4f;    // 파고(진폭)

    [Header("Physics (선택)")]
    public bool useRigidbody = false;  // Rigidbody 쓰면 true
    Rigidbody rb;

    float velY;

    void Awake()
    {
        if (autoFindWater && !water)
        {
            var w = GameObject.FindWithTag("Water");
            if (w) water = w.transform;
        }

        if (useRigidbody)
        {
            rb = GetComponent<Rigidbody>();
            if (!rb) rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;     // 부력으로만 위아래 이동
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Start() { SnapToWater(); }

    void FixedUpdate()
    {
        // Rigidbody를 쓸 때는 물리 프레임에서
        if (useRigidbody) Tick(Time.fixedDeltaTime);
    }

    void Update()
    {
        // Transform만 쓸 때는 일반 프레임에서
        if (!useRigidbody) Tick(Time.deltaTime);
    }

    void Tick(float dt)
    {
        if (!water) return;

        var p = transform.position;

        // 월드좌표로 파고 계산 (로컬X/Z 쓰지 않음)
        float t = Time.time * waveSpeed;
        float waveY =
            Mathf.Sin(p.x * waveScale + t) *
            Mathf.Sin(p.z * waveScale + t) *
            waveHeight;

        float targetY = water.position.y + waveY + heightOffset;

        // 스프링-댐퍼
        float error = targetY - p.y;
        velY += error * strength * dt;
        velY *= Mathf.Clamp01(1f - damping * dt);

        float newY = p.y + velY;

        if (useRigidbody && rb)
            rb.MovePosition(new Vector3(p.x, newY, p.z));
        else
            transform.position = new Vector3(p.x, newY, p.z);
    }

    [ContextMenu("Snap to Water Now")]
    public void SnapToWater()
    {
        if (!water) return;

        var p = transform.position;
        float t = Time.time * waveSpeed;
        float waveY =
            Mathf.Sin(p.x * waveScale + t) *
            Mathf.Sin(p.z * waveScale + t) *
            waveHeight;

        transform.position = new Vector3(p.x, water.position.y + waveY + heightOffset, p.z);
        velY = 0f;
    }
}
