using UnityEngine;

[ExecuteAlways]
public class GradientSkyController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public Light directionalLight;

    [Header("Sky Colors")]
    public Color sunriseColor = new Color(1f, 0.5f, 0.2f, 0f); // 주황빛
    public Color noonColor = new Color(0.5f, 0.8f, 1f, 0f);     // 밝은 하늘색
    public Color sunsetColor = new Color(1f, 0.3f, 0.3f, 0f);   // 붉은 하늘
    public Color nightColor = new Color(0.05f, 0.05f, 0.1f, 0f); // 어두운 남색

    void Update()
    {
        if (mainCamera == null || directionalLight == null) return;

        // Directional Light의 X축 회전값 (0~360도)
        float angle = directionalLight.transform.eulerAngles.x;

        // 낮/밤 구간을 나눠서 색 그라데이션
        Color targetColor;

        if (angle < 90f)
        {
            // 새벽~정오: sunrise → noon
            float t = angle / 90f;
            targetColor = Color.Lerp(sunriseColor, noonColor, t);
        }
        else if (angle < 180f)
        {
            // 정오~석양: noon → sunset
            float t = (angle - 90f) / 90f;
            targetColor = Color.Lerp(noonColor, sunsetColor, t);
        }
        else if (angle < 270f)
        {
            // 석양~밤: sunset → night
            float t = (angle - 180f) / 90f;
            targetColor = Color.Lerp(sunsetColor, nightColor, t);
        }
        else
        {
            // 밤~새벽: night → sunrise
            float t = (angle - 270f) / 90f;
            targetColor = Color.Lerp(nightColor, sunriseColor, t);
        }

        // 항상 알파는 0 (완전 투명)
        targetColor.a = 0f;

        // 카메라 배경색 설정
        mainCamera.backgroundColor = targetColor;
    }
}
