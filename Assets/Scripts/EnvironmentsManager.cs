using UnityEngine;

public class EnvironmentsManager : MonoBehaviour
{
    public static EnvironmentsManager Instance { get; private set; }

    [Header("Virtual Environments")]
    public GameObject scene1;

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
        scene1.SetActive(false);
    }

    public void ActivateScene1()
    {
        scene1.SetActive(true);
    }

    public void DeactivateScene1()
    {
        scene1.SetActive(false);
    }

}
