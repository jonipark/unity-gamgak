using UnityEngine;
using System.Collections;

public class WallArtTrigger : MonoBehaviour
{
    public string playerTag = "MainCamera";
    public ParticleSystem particle;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            StartCoroutine(HandleEnter());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            StartCoroutine(HandleExit());
        }
    }

    // VR Scene Enter Event
    private IEnumerator HandleEnter()
    {
        // 1. find user position
        Transform user = Camera.main.transform;

        // 2. navigate particle off
        particle.Stop(); 
        while (particle.IsAlive(true))
        {
            yield return null;
        }

        // 3. settign vr scene view (align to user view)
        Vector3 pos = new Vector3(0, 0, 0.5f);
        //Vector3 rot = new Vector3(0, 0, 0);
        float yRotation = user.eulerAngles.y;
        Quaternion horizontalRotation = Quaternion.Euler(0, yRotation, 0);
        EnvironmentsManager.Instance.scene1.transform.position = user.position + pos;
        EnvironmentsManager.Instance.scene1.transform.rotation = horizontalRotation;
        //EnvironmentsManager.Instance.scene1.transform.rotation = user.rotation * Quaternion.Euler(rot);

        // 4. SeamlessMR (MR to VR)
        yield return StartCoroutine(PassthroughManager.Instance.StartMRtoVRTransition());

        // 5. connect passthrough safety event
        PassthroughSafety.Instance.ConnectTarget(transform);

        // (Optional) turn on passthrough window
        //PassthroughWindowManager.Instance.TurnOnWindow();
    }

    // VR Scene Exit Event
    private IEnumerator HandleExit()
    {
        // (Optional) turn off passthrough window
        //PassthroughWindowManager.Instance.TurnOffWindow();

        // 1. disconnect all dependency
        PassthroughSafety.Instance.DisconnectTarget();
        yield return StartCoroutine(FlowManager.Instance.ResetFlow());
        
        // 2. SeamlessMR (VR to MR)
        yield return StartCoroutine(PassthroughManager.Instance.StartVRtoMRTransition());

        // 3. reset vr scene view
        EnvironmentsManager.Instance.scene1.transform.position = Vector3.zero;
        EnvironmentsManager.Instance.scene1.transform.rotation = Quaternion.Euler(Vector3.zero);

        // 4. navigate particle on
        particle.Play();
    }
}