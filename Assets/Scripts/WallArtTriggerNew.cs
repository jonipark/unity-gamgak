using UnityEngine;
using System.Collections;

public class WallArtTriggerNew : MonoBehaviour
{
    public string playerTag = "MainCamera";
    public ParticleSystem particle;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            gameObject.GetComponent<CapsuleCollider>().radius = 2f;
            StartCoroutine(HandleEnter());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            gameObject.GetComponent<CapsuleCollider>().radius = 1.5f;
            StartCoroutine(HandleExit());
        }
    }

    // Enter Event
    private IEnumerator HandleEnter()
    {
        // 1. 안내 파티클 종료
        particle.Stop();

        // 2. 큐레이션 UI 등장 및 설명
        FlowManager.Instance.SetFlowStep(1);
        yield return null;
    }

    // Exit Event
    private IEnumerator HandleExit()
    {
        // 1. 경험 및 UI 종료 & 변수 정리
        FlowManager.Instance.SetFlowStep(0);

        // 2. 안내 파티클 시작
        particle.Play();
        yield return null;
    }
}