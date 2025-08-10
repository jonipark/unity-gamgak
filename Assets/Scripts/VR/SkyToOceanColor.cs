using UnityEngine;

public class SkyToOceanColor : MonoBehaviour
{
    public Light sun;
    public Renderer waterRenderer;
    public Color baseShallowColor;
    public Color baseDeepColor;
    [Range(0f, 1f)] public float blendFactor = 0.5f;
    public float transitionSpeed = 1.5f; // 트랜지션 속도 조절

    private Material runtimeMaterial;

    private Color currentShallowColor;
    private Color currentDeepColor;

    const float THRESHOLD_DAY = 12f;
    const float THRESHOLD_YELLOW = 4f;
    const float THRESHOLD_RED = -5f;

    private void Start()
    {
        runtimeMaterial = waterRenderer.material;

        // 초기 색상 설정
        currentShallowColor = baseShallowColor;
        currentDeepColor = baseDeepColor;
    }

    private void Update()
    {
        UpdateWaterColor();
    }

    void UpdateWaterColor()
    {
        if (runtimeMaterial == null || sun == null) return;

        float sunRotX = sun.transform.rotation.eulerAngles.x;
        if (sunRotX > 180f) sunRotX -= 360f;

        Color targetShallow = baseShallowColor;
        Color targetDeep = baseDeepColor;

        if (sunRotX > THRESHOLD_DAY)
        {
            // ☀️ 낮
            targetShallow = baseShallowColor;
            targetDeep = baseDeepColor;
        }
        else if (sunRotX > THRESHOLD_YELLOW)
        {
            // 🌅 노란 하늘
            Color yellow = new Color(1.0f, 0.85f, 0.3f);
            targetShallow = Color.Lerp(baseShallowColor, yellow, 1.0f);
            targetDeep = Color.Lerp(baseDeepColor, yellow, 0.9f);
        }
        else if (sunRotX > THRESHOLD_RED)
        {
            // 🌇 빨간 하늘
            Color red = new Color(1.0f, 0.1f, 0.1f);
            targetShallow = Color.Lerp(baseShallowColor, red, 1.0f);
            targetDeep = Color.Lerp(baseDeepColor, red, 0.95f);
        }
        else
        {
            // 🌌 밤
            targetShallow = new Color(0.01f, 0.01f, 0.05f);
            targetDeep = new Color(0.005f, 0.005f, 0.02f);
        }

        // 점진적 트랜지션
        currentShallowColor = Color.Lerp(currentShallowColor, targetShallow, Time.deltaTime * transitionSpeed);
        currentDeepColor = Color.Lerp(currentDeepColor, targetDeep, Time.deltaTime * transitionSpeed);

        runtimeMaterial.SetColor("_ShallowColor", currentShallowColor);
        runtimeMaterial.SetColor("_DeepColor", currentDeepColor);
        runtimeMaterial.SetColor("_HorizonColor", currentShallowColor);
        runtimeMaterial.SetColor("_WaterColor", currentShallowColor);

        waterRenderer.sharedMaterial.SetColor("_ShallowColor", currentShallowColor);
        waterRenderer.sharedMaterial.SetColor("_DeepColor", currentDeepColor);
    }
}


// using UnityEngine;
// using UnityEngine.InputSystem;

// public class SkyToOceanColor : MonoBehaviour
// {
//     public Light sun;
//     public Renderer waterRenderer;
//     public Color baseShallowColor;
//     public Color baseDeepColor;
//     [Range(0f, 1f)] public float blendFactor = 0.5f;

//     private Material runtimeMaterial;

//     const float THRESHOLD_DAY = 12f;
//     const float THRESHOLD_YELLOW = 4f;
//     const float THRESHOLD_RED = -5f;

//     private void Start()
//     {
//         runtimeMaterial = waterRenderer.material;
//     }

//     private void Update()
//     {
//         UpdateWaterColor();
//     }

//     void UpdateWaterColor()
//     {
//         if (runtimeMaterial == null || sun == null) return;

//         float sunRotX = sun.transform.rotation.eulerAngles.x;
//         if (sunRotX > 180f) sunRotX -= 360f;

//         Color shallow = baseShallowColor;
//         Color deep = baseDeepColor;

//         if (sunRotX > THRESHOLD_DAY)
//         {
//             // ☀️ 낮
//             shallow = baseShallowColor;
//             deep = baseDeepColor;
//         }
//         else if (sunRotX > THRESHOLD_YELLOW)
//         {
//             // 🌅 노란 하늘
//             Color yellow = new Color(1.0f, 0.85f, 0.3f); // 더 강한 노란색
//             shallow = Color.Lerp(baseShallowColor, yellow, 1.0f);
//             deep = Color.Lerp(baseDeepColor, yellow, 0.9f);
//         }
//         else if (sunRotX > THRESHOLD_RED)
//         {
//             // 🌇 빨간 하늘
//             Color red = new Color(1.0f, 0.1f, 0.1f); // 더 진한 빨강
//             shallow = Color.Lerp(baseShallowColor, red, 1.0f);
//             deep = Color.Lerp(baseDeepColor, red, 0.95f);
//         }
//         else
//         {
//             // 🌌 밤
//             Color nightShallow = new Color(0.01f, 0.01f, 0.05f);
//             Color nightDeep = new Color(0.005f, 0.005f, 0.02f);
//             shallow = nightShallow;
//             deep = nightDeep;
//         }

//         // 머티리얼에 색상 반영
//         runtimeMaterial.SetColor("_ShallowColor", shallow);
//         runtimeMaterial.SetColor("_DeepColor", deep);
//         runtimeMaterial.SetColor("_HorizonColor", shallow);
//         runtimeMaterial.SetColor("_WaterColor", shallow);


//         waterRenderer.sharedMaterial.SetColor("_ShallowColor", shallow);
//         waterRenderer.sharedMaterial.SetColor("_DeepColor", deep);
//     }
// }
