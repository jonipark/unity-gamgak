using UnityEngine;
using System.Collections;

public class PassthroughManager : MonoBehaviour
{
    public static PassthroughManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        underlayPassthrough.enabled = true;
        overlayPassthrough.enabled = false;
    }

    [Header("Passthrough")]
    public OVRPassthroughLayer underlayPassthrough;
    public OVRPassthroughLayer overlayPassthrough;

    [Header("Fade Settings")]
    public float fadeDuration = 1.0f;
    public float waitBetweenStages = 0.3f;

    private Coroutine transitionCoroutine;

    public IEnumerator StartMRtoVRTransition()
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(MRtoVRSequence());
        yield return transitionCoroutine;
    }

    public IEnumerator StartVRtoMRTransition()
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(VRtoMRSequence());
        yield return transitionCoroutine;
    }

    private IEnumerator MRtoVRSequence()
    {
        // 1. turn on overlay (overlay ON / underlay ON)
        overlayPassthrough.textureOpacity = 1f;
        overlayPassthrough.enabled = true;
        yield return new WaitForSeconds(waitBetweenStages);

        // 2. turn off underlay (overlay ON / underlay OFF)
        underlayPassthrough.enabled = false;

        // 3. turn on vr scene environments (but, still invisible)
        EnvironmentsManager.Instance.ActivateScene1();
        yield return new WaitForSeconds(waitBetweenStages);

        // 4. (SeamlessMR) overlay opacity 1 → 0 (it looks like vr scene appear smoothly)
        yield return StartCoroutine(FadeOverlayOpacity(1f, 0f));

        // 5. turn off overlay (overlay OFF / underlay OFF) 
        overlayPassthrough.enabled = false;
    }

    private IEnumerator VRtoMRSequence()
    {
        // 2. turn on overlay (overlay ON / underlay OFF) but still vr scene
        overlayPassthrough.textureOpacity = 0f;
        overlayPassthrough.enabled = true;
        yield return new WaitForSeconds(waitBetweenStages);

        // 3. (SeamlessMR) overlay opacity 0 → 1 (it looks like vr scene disappear smoothly)
        yield return StartCoroutine(FadeOverlayOpacity(0f, 1f));

        // 4. turn off vr scene environments
        EnvironmentsManager.Instance?.DeactivateScene1();
        yield return new WaitForSeconds(waitBetweenStages);

        // 5. turn on underlay (overlay ON / underlay ON)
        underlayPassthrough.textureOpacity = 1f;
        underlayPassthrough.enabled = true;
        while (!underlayPassthrough.isActiveAndEnabled)
        {
            yield return null;
        }

        // 5. turn off overlay (overlay OFF / underlay ON)
        overlayPassthrough.enabled = false;

        // -------- mr scene change complete --------
    }

    private IEnumerator FadeOverlayOpacity(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            overlayPassthrough.textureOpacity = Mathf.Lerp(from, to, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }
        overlayPassthrough.textureOpacity = to;
    }

    private IEnumerator FadeUnderlayOpacity(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            underlayPassthrough.textureOpacity = Mathf.Lerp(from, to, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }
        underlayPassthrough.textureOpacity = to;
    }
}


/** Code Storage
 * 
 *
        MR to VR에 있던 코드
        //7. Underlay 다시 켜고 opacity 0 → 1 (Passthrough Window 활성화)
        underlayPassthrough.textureOpacity = 0f;
        underlayPassthrough.enabled = true;
        yield return StartCoroutine(FadeUnderlayOpacity(0f, 1f));

        VR to MR에 있던 코드
        // 1. Underlay opacity 줄이기
        yield return StartCoroutine(FadeUnderlayOpacity(1f, 0f));
        // 3. Underlay 끄기 (Passthrough Window 사라짐)
        underlayPassthrough.enabled = false;

 * 
 */