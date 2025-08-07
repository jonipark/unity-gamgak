using UnityEngine;
using System.Collections;
using TMPro;

public class UIMode : MonoBehaviour
{
    public static UIMode Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        uiGroup.SetActive(false);
    }

    [Header ("UI List")]
    [SerializeField] public GameObject uiGroup;
    [SerializeField] private GameObject startUI;
    [SerializeField] private GameObject feelingUI;
    [SerializeField] private GameObject experienceUI;

    [Header ("UI Text List")]
    [SerializeField] private TMP_Text startText;
    [SerializeField] private TMP_Text feelingText;
    [SerializeField] private TMP_Text experienceText;
    
    public float uiCurrentScale;

    private GameObject selectedUI;
    private TMP_Text selectedText;

    private void Start()
    {
        startUI.SetActive(true);
        feelingUI.SetActive(false);
        experienceUI.SetActive(false);
        selectedUI = startUI;
        selectedText = startText;
    }

    public void ChangeUI(string mode)
    {
        if (mode == "start")
        {
            startUI.SetActive(true);
            feelingUI.SetActive(false);
            experienceUI.SetActive(false);
            selectedUI = startUI;
            selectedText = startText;
        }
        else if (mode == "feeling")
        {
            feelingUI.SetActive(true);
            startUI.SetActive(false);
            experienceUI.SetActive(false);
            selectedUI = feelingUI;
            selectedText = feelingText;
        }
        else if (mode == "experience")
        {
            experienceUI.SetActive(true);
            feelingUI.SetActive(false);
            startUI.SetActive(false);
            selectedUI = experienceUI;
            selectedText = experienceText;
        }
    }

    public void SetInteractable(bool flag)
    {
        CanvasGroup group = selectedUI.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.interactable = flag;
        }
    }

    public IEnumerator DisappearUI()
    {
        uiCurrentScale = uiGroup.transform.localScale.x;
        yield return StartCoroutine(FadeCanvas(uiGroup, uiCurrentScale, 0, 1.5f));
        uiGroup.SetActive(false);
    }

    public IEnumerator AppearUI()
    {
        uiCurrentScale = uiGroup.transform.localScale.x;
        uiGroup.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(FadeCanvas(uiGroup, uiCurrentScale, 1, 1.5f));
    }

    public void SetTextOpacity(float textOpacity)
    {
        selectedText.color = new Color(0, 0, 0, textOpacity);
    }
    public void SetText(string text)
    {
        selectedText.text = text;
    }
    public void SetUIScale(float scale)
    {
        uiGroup.transform.localScale = new Vector3(scale, 1, 1);
    }
    
    public IEnumerator PushText(string text)
    {
        yield return StartCoroutine(DisappearText());
        selectedText.text = text;
        yield return StartCoroutine(AppearText());
    }
    public IEnumerator DisappearText()
    {
        float currAlpha = selectedText.alpha;
        float fadeDuration = 0.5f;
        float t = 0f;
        float curr;

        while (t < fadeDuration)
        {
            curr = Mathf.Lerp(currAlpha, 0, t / fadeDuration);
            selectedText.color = new Color(0, 0, 0, curr);
            t += Time.deltaTime;
            yield return null;
        }
        selectedText.color = new Color(0, 0, 0, 0f);
    }
    public IEnumerator AppearText()
    {
        float currAlpha = selectedText.alpha; 
        float fadeDuration = 0.5f;
        float t = 0f;
        float curr;

        while (t < fadeDuration)
        {
            curr = Mathf.Lerp(currAlpha, 1, t / fadeDuration);
            selectedText.color = new Color(0, 0, 0, curr);
            t += Time.deltaTime;
            yield return null;
        }
        selectedText.color = new Color(0, 0, 0, 1f);
    }

    private IEnumerator FadeCanvas(GameObject uiObject, float from, float to, float fadeDuration)
    {
        Transform uiTransform = uiObject.transform;
        float t = 0f;
        float curr;
        uiTransform.localScale = new Vector3(from, 1f, 1f);
        while (t < fadeDuration)
        {
            curr = Mathf.Lerp(from, to, t / fadeDuration);
            uiTransform.localScale = new Vector3(curr, 1f, 1f);
            t += Time.deltaTime;
            yield return null;
        }
        uiTransform.localScale = new Vector3(to, 1f, 1f);
    }
}
