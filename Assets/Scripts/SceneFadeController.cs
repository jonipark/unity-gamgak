using System.Collections;
using UnityEngine;

public class SceneFadeController : MonoBehaviour
{
    [System.Serializable]
    public class FadeTarget
    {
        public Renderer renderer;
        public string opacityProperty = "_Opacity";
        public float fadeDuration = 1f;
    }

    public FadeTarget background;
    public FadeTarget floor;
    public FadeTarget obj;

    void OnEnable()
    {
        ClearOpacity();
        StartCoroutine(FadeInSequence());
    }

    void ClearOpacity()
    {
        background.renderer.material.SetFloat(background.opacityProperty, 0f);
        floor.renderer.material.SetFloat(floor.opacityProperty, 0f);
        obj.renderer.material.SetFloat(obj.opacityProperty, 0f);
    }

    IEnumerator FadeInSequence()
    {   
        yield return StartCoroutine(FadeIn(background));
        yield return StartCoroutine(FadeIn(floor));
        yield return StartCoroutine(FadeIn(obj));
    }

    public void FadeOutAndDisable()
    {
        StartCoroutine(FadeOutSequence(() =>
        {
            gameObject.SetActive(false);
        }));
    }

    public IEnumerator FadeOutSequence(System.Action onComplete = null)
    {
        // 동시에 FadeOut 실행
        IEnumerator co1 = FadeOut(background);
        IEnumerator co2 = FadeOut(floor);
        IEnumerator co3 = FadeOut(obj);

        StartCoroutine(co1);
        StartCoroutine(co2);
        StartCoroutine(co3);

        // 최대 fadeDuration 만큼 기다린 후 종료 (모두 같은 duration이라 가정)
        yield return new WaitForSeconds(background.fadeDuration);

        onComplete?.Invoke();
    }

    IEnumerator FadeIn(FadeTarget target)
    {
        if (target.renderer == null) yield break;

        Material mat = target.renderer.material;
        float timer = 0f;

        // 초기값 0
        mat.SetFloat(target.opacityProperty, 0f);

        while (timer < target.fadeDuration)
        {
            timer += Time.deltaTime;
            float opacity = Mathf.Clamp01(timer / target.fadeDuration);
            mat.SetFloat(target.opacityProperty, opacity);
            yield return null;
        }

        // 최종값 1
        mat.SetFloat(target.opacityProperty, 1f);
    }

    IEnumerator FadeOut(FadeTarget target)
    {
        if (target.renderer == null) yield break;

        Material mat = target.renderer.material;
        float timer = 0f;
        mat.SetFloat(target.opacityProperty, 1f);

        while (timer < target.fadeDuration)
        {
            timer += Time.deltaTime;
            float opacity = Mathf.Clamp01(1f - (timer / target.fadeDuration));
            mat.SetFloat(target.opacityProperty, opacity);
            yield return null;
        }

        mat.SetFloat(target.opacityProperty, 0f);
    }
}
