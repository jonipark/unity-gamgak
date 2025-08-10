using UnityEngine;

public class FollowBoatY : MonoBehaviour
{
    public Transform boat;
    float yOffset;

    void Start() {
        yOffset = transform.position.y - boat.position.y;
    }

    void LateUpdate() {
        var p = transform.position;
        p.y = boat.position.y + yOffset;
        transform.position = p;
    }
}
