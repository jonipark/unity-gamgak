using UnityEngine;
using UnityEngine.XR.Hands;
using TMPro;

public class HandVolumeController : MonoBehaviour
{
    public XRHandSubsystem handSubsystem;
    public AudioSource ambientAudioSource;
    public TextMeshProUGUI debugText;

    private float? baseY = 0f;

    private const float minY = -0.4f; // 40cm below base = min volume
    private const float maxY = 0.1f;  // 10cm above base = max volume

    void Start()
    {
        var subsystems = new System.Collections.Generic.List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
        {
            handSubsystem = subsystems[0];
        }
    }

    void Update()
    {
        if (handSubsystem == null || !handSubsystem.running) return;

        XRHand leftHand = handSubsystem.leftHand;
        if (!leftHand.isTracked) return;

        var palm = leftHand.GetJoint(XRHandJointID.Palm);
        if (!palm.TryGetPose(out Pose pose)) return;

        float currentY = pose.position.y;

        float deltaY = currentY - baseY.Value;

        // Normalize deltaY to 0.0 - 1.0 range based on min/max
        float normalized = Mathf.InverseLerp(minY, maxY, deltaY);
        float targetVolume = Mathf.Clamp01(normalized);

        ambientAudioSource.volume = targetVolume;

        debugText.text =
            $"Base Y: {baseY:F3}\n" +
            $"Current Y: {currentY:F3}\n" +
            $"ΔY: {deltaY:F3}\n" +
            $"Volume: {ambientAudioSource.volume:F2}";
    }
}
