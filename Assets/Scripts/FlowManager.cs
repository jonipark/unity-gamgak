using UnityEngine;
using System.Collections;

public class FlowManager : MonoBehaviour
{
    public static FlowManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        interactionObjectsTransform = new TransformData[interactionObjects.Length];
        for (int i = 0; i < interactionObjects.Length; i++)
        {
            interactionObjects[i].enabled = false;
        }
    }
    // ----------- Singleton 선언 종료 --------------

    [Header("Curation - UI")]
    public CanvasGroup buttonGroup;
    public RectTransform contentsPlane;

    [Header("Experience - Interactable Objects")]
    public Collider[] interactionObjects;
    private TransformData[] interactionObjectsTransform;

    // Flow Step으로 현재 Scene 단계를 조정
    private int flowStep = 0;
    private int prevStep = 0;
    public int GetFlowStep()
    {
        return flowStep;
    }
    public void SetFlowStep(int stepNum)
    {
        prevStep = flowStep;
        flowStep = stepNum;
        if (flowStep == 0)
            StartCoroutine(InitialFlow());
        else if (flowStep == 1)
            StartCoroutine(FirstFlow());
        else if (flowStep == 2)
            StartCoroutine(SecondFlow());
        else if (flowStep == 3)
            StartCoroutine(ThirdFlow());
        else if (flowStep == 4)
            StartCoroutine(FourthFlow());
    }

    private void Start()
    {
        StartCoroutine(InitialFlow());
    }

    // 초기화 Flow
    private IEnumerator InitialFlow()
    {
        // 시작하는 초기화 단계가 아닌, 다른 장소에서 불러온 Initial인 경우
        print("reset flow");
        if (prevStep != 0) // 단계에 따라 세팅을 다르게 초기화
        {
            if (prevStep == 2) // 이전 단계가 큐레이션 모드
            {

            }
            else if (prevStep == 3) // 이전 단계가 VR 감상 모드
            {
                // SeamlessMR (VR to MR)
                yield return StartCoroutine(PassthroughManager.Instance.StartVRtoMRTransition());
            }
            else if (prevStep == 4) // 이전 단계가 체험 모드
            {
                // SeamlessMR (VR to MR)
                yield return StartCoroutine(PassthroughManager.Instance.StartVRtoMRTransition());
            }
            StartCoroutine(UIMode.Instance.DisappearText());
            yield return StartCoroutine(UIMode.Instance.DisappearUI());
        }

        print("initializing flow");
        PassthroughSafety.Instance.DisconnectTarget();
        EnvironmentsManager.Instance.DeactivateScene1();
        EnvironmentsManager.Instance.scene1.transform.position = Vector3.zero;
        EnvironmentsManager.Instance.scene1.transform.rotation = Quaternion.Euler(Vector3.zero);
        UIMode.Instance.ChangeUI("start");
        UIMode.Instance.SetUIScale(0);
        UIMode.Instance.SetText("");
        UIMode.Instance.SetTextOpacity(0);
    }

    // 경험 Zone 진입 Flow (WallArtTrigger.cs 참고) 큐레이션 UI 등장
    private IEnumerator FirstFlow()
    {
        print("first flow");
        UIMode.Instance.SetInteractable(false);
        yield return StartCoroutine(UIMode.Instance.AppearUI());
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(UIMode.Instance.PushText("감각공작소에 오신 것을 환영합니다."));
        yield return new WaitForSeconds(2.0f);
        yield return StartCoroutine(UIMode.Instance.PushText("화면에 보이는 버튼을 눌러 '설명 듣기' 또는 'VR 감상'을 할 수 있습니다."));
        yield return new WaitForSeconds(3.0f);
        yield return StartCoroutine(UIMode.Instance.PushText("유화 - 1872년 르아브르 항구"));
        UIMode.Instance.SetInteractable(true);
    }

    // 큐레이션 모드 선택
    private IEnumerator SecondFlow()
    {
        print("second flow");
        yield return StartCoroutine(UIMode.Instance.DisappearText());
        yield return StartCoroutine(FadeCanvasGroup(buttonGroup, 1f, 0f));
        yield return StartCoroutine(AnimateWidth(17f, 25f));
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(UIMode.Instance.PushText("첫 번째 큐레이팅 문장"));
        yield return new WaitForSeconds(5f);
        yield return StartCoroutine(UIMode.Instance.PushText("두 번째 큐레이팅 문장"));
        yield return new WaitForSeconds(5f);
        yield return StartCoroutine(UIMode.Instance.PushText("세 번째 큐레이팅 문장"));
        yield return new WaitForSeconds(5f);
        yield return StartCoroutine(UIMode.Instance.PushText("네 번째 큐레이팅 문장"));
        yield return new WaitForSeconds(5f);
        yield return StartCoroutine(UIMode.Instance.PushText("다섯 번째 큐레이팅 문장"));
        yield return new WaitForSeconds(5f);
        StartCoroutine(UIMode.Instance.DisappearText());
        yield return StartCoroutine(AnimateWidth(25f, 17f));
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(FadeCanvasGroup(buttonGroup, 0f, 1f));
        SetFlowStep(1);
    }
    private IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float endAlpha)
    {
        float time = 0f;
        while (time < 1f)
        {
            float t = time / 1f;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            time += Time.deltaTime;
            yield return null;
        }
        group.alpha = endAlpha;
    }
    private IEnumerator AnimateWidth(float from, float to)
    {
        float duration = 1f;
        float time = 0f;
        Vector2 originalSize = contentsPlane.sizeDelta;

        while (time < duration)
        {
            float t = time / duration;
            float newX = Mathf.Lerp(from, to, t);
            contentsPlane.sizeDelta = new Vector2(newX, originalSize.y);
            time += Time.deltaTime;
            yield return null;
        }
        // 보정
        contentsPlane.sizeDelta = new Vector2(to, originalSize.y);
    }

    //  VR 감상 모드 선택 (Feeling)
    private IEnumerator ThirdFlow()
    {
        print("third flow");
        // 켜져있던 UI 사라지기
        yield return StartCoroutine(UIMode.Instance.DisappearUI());
        UIMode.Instance.ChangeUI("feeling");
        UIMode.Instance.SetTextOpacity(0);

        // 사용자 시점 기준으로 VR 맵 정렬
        Transform user = Camera.main.transform;
        float yRotation = user.eulerAngles.y;
        Quaternion horizontalRotation = Quaternion.Euler(0, yRotation, 0);
        EnvironmentsManager.Instance.scene1.transform.position = user.position;
        EnvironmentsManager.Instance.scene1.transform.rotation = horizontalRotation;

        // SeamlessMR (MR to VR)
        yield return StartCoroutine(PassthroughManager.Instance.StartMRtoVRTransition());

        // 안전 모드 설정 (뒤돌면 MR 보이기)
        PassthroughSafety.Instance.ConnectTarget(buttonGroup.transform);

        // UI 나타난 뒤 멘트 이후 사라지기
        yield return StartCoroutine(UIMode.Instance.AppearUI());
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(UIMode.Instance.PushText("이 곳은 모네가 그림을 그린 르아브르 항구입니다."));
        yield return new WaitForSeconds(2.0f);
        yield return StartCoroutine(UIMode.Instance.PushText("빛의 변화를 눈으로 느껴보세요."));
        yield return new WaitForSeconds(2.0f);
        StartCoroutine(UIMode.Instance.DisappearText());
        yield return StartCoroutine(UIMode.Instance.DisappearUI());

        // ==================================================
        // 이 곳에서 n초간 감상 이벤트가 실행 될 예정
        // ==================================================
        yield return new WaitForSeconds(10.0f);

        SetFlowStep(4);
    }

    // 인터랙션을 활용한 VR 체험 모드 (Experience)
    private IEnumerator FourthFlow()
    { 
        yield return null;

    }
    private IEnumerator FifthFlow()
    {
        yield return null;
    }



    // 작품 감상하기
    public void StartFeelingFlow()
    {
        StartCoroutine(FeelingFlow());
    }
    IEnumerator FeelingFlow()
    {
        // 감상 모드로 진입하여 UI도 변환
        yield return StartCoroutine(UIMode.Instance.DisappearUI());
        UIMode.Instance.SetTextOpacity(0);
        UIMode.Instance.ChangeUI("feeling");
        yield return StartCoroutine(UIMode.Instance.AppearUI());

        // 큐레이팅 시퀀스에 의해 제어될 예정
        yield return StartCoroutine(UIMode.Instance.PushText("Hello, Welcome to Monet - Sunrise"));
    }

    // 공간 경험 즐기기
    public void StartExperienceFlow()
    {
        StartCoroutine(ExperienceFlow());
    }
    IEnumerator ExperienceFlow()
    {
        // 경험 모드로 진입하여 UI도 변환
        yield return StartCoroutine(UIMode.Instance.DisappearUI());
        UIMode.Instance.ChangeUI("experience");
        yield return StartCoroutine(UIMode.Instance.AppearUI());
        
        // 인터랙션 활성화 및 초기 위치 저장
        for (int i = 0; i < interactionObjects.Length; i++)
        {
            interactionObjects[i].GetComponent<Collider>().enabled = true;
            interactionObjectsTransform[i] = new TransformData(interactionObjects[i].transform);
        }
    }

    // Flow 탈출하기
    public void ExitFlow()
    {
        ResetFlow();
    }

    // 모든 변수 값 원상 복구
    public IEnumerator ResetFlow()
    {
        yield return StartCoroutine(UIMode.Instance.DisappearText());
        UIMode.Instance.ChangeUI("start");

        // 감상 모드 단계 (또는 시간) 초기화

        // 인터랙션 비활성화 및 원위치
        if (interactionObjectsTransform != null)
        {
            for (int i = 0; i < interactionObjects.Length; i++)
            {
                interactionObjectsTransform[i].ApplyTo(interactionObjects[i].transform);
                interactionObjects[i].GetComponent<Collider>().enabled = false;
            }
            interactionObjectsTransform = null;
        }
    }
}


[System.Serializable]
public struct TransformData
{
    public Vector3 localPosition;
    public Quaternion localRotation;
    public Vector3 localScale;

    public TransformData(Transform transform)
    {
        localPosition = transform.localPosition;
        localRotation = transform.localRotation;
        localScale = transform.localScale;
    }

    public void ApplyTo(Transform transform)
    {
        transform.localPosition = localPosition;
        transform.localRotation = localRotation;
        transform.localScale = localScale;
    }
}