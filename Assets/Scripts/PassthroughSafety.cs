using UnityEngine;

public class PassthroughSafety : MonoBehaviour
{
    public static PassthroughSafety Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Header("Passthrough")]
    public OVRPassthroughLayer overlayPassthrough;

    [Header("Angle Settings")]
    public float fullClearAngle;
    public float fullBlockAngle;

    [Header("Debug")]
    public Transform headTransform;
    public Transform targetTransform; // Target object to face
    public bool VRMode = false;

    public void ConnectTarget(Transform newTarget)
    {
        targetTransform = newTarget;
        VRMode = true;
        overlayPassthrough.enabled = true;
    }

    public void DisconnectTarget()
    {
        targetTransform = null;
        VRMode = false;
    }

    void Update()
    {
        if (overlayPassthrough == null || targetTransform == null || headTransform == null)
            return;

        if (VRMode)
        {
            float angle = GetHeadYawRelativeToTarget();
            float opacity = YawToOpacity(angle);
            overlayPassthrough.textureOpacity = opacity;

            // Debug
            Debug.Log($"Yaw: {angle:F1}¡Æ, Opacity: {opacity:F2}");
        }
    }

    float GetHeadYawRelativeToTarget()
    {
        Vector3 toTarget = (targetTransform.position - headTransform.position);
        toTarget.y = 0f;
        toTarget.Normalize();

        Vector3 viewDir = headTransform.forward;
        viewDir.y = 0f;
        viewDir.Normalize();

        float angle = Vector3.Angle(viewDir, toTarget);
        return angle;
    }

    float YawToOpacity(float angle)
    {
        float opacity;

        if (angle <= fullClearAngle)
        {
            opacity = 0f;
        }
        else if (angle <= fullBlockAngle)
        {
            opacity = Mathf.InverseLerp(fullClearAngle, fullBlockAngle, angle); // 150~200µµ: 0 ¡æ 1
        }
        else
        {
            opacity = 1f;
        }

        return opacity;
    }

}
