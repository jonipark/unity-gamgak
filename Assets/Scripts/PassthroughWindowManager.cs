using UnityEngine;

public class PassthroughWindowManager : MonoBehaviour
{
    public static PassthroughWindowManager Instance { get; private set; }

    public GameObject windowMaterial;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        TurnOffWindow();
    }

    public void TurnOnWindow()
    {
        windowMaterial.SetActive(true);
    }
    public void TurnOffWindow()
    {
        windowMaterial.SetActive(false);
    }
}
