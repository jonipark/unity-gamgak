using NUnit;
using UnityEngine;

public class Art_Initializer : MonoBehaviour
{
    [SerializeField] GameObject DetectArea;
    [SerializeField] GameObject Environments;

    private GameObject detArea;
    private GameObject envObject;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        detArea = Instantiate(DetectArea, transform);

        // Ensure sphere collider is trigger-enabled
        CapsuleCollider sc = detArea.GetComponent<CapsuleCollider>();
        if (sc == null)
        {
            sc = detArea.AddComponent<CapsuleCollider>();
        }
        sc.isTrigger = true;

        // Optional: Adjust sphere radius or position
        sc.radius = 2.0f;
        detArea.transform.localPosition = Vector3.zero;

        // Add Rigidbody if needed for trigger events
        Rigidbody rb = detArea.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = detArea.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        envObject = Instantiate(Environments, transform);
        envObject.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        envObject.SetActive(false); // start inactive
    }

    private void OnTriggerEnter(Collider other)
    {
        print("Trigger Enter: " + other.name);
        if (other.CompareTag("MainCamera"))
        {
            if (envObject != null)
            {
                envObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        print("Trigger Exit: " + other.name);
        if (other.CompareTag("MainCamera"))
        {
            if (envObject != null)
            {
                envObject.SetActive(false);
            }
        }
    }
}
