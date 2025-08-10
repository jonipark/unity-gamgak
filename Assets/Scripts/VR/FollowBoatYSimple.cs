using UnityEngine;

// Rower 전체를 감싸는 "빈 부모"에 붙이기
public class FollowBoatYSimple : MonoBehaviour
{
    public Transform boat;
    float yOffset;

    void Start() {
        yOffset = transform.position.y - boat.position.y;
    }

    void LateUpdate() { // 보트가 끝난 뒤 따라가기
        var p = transform.position;
        p.y = boat.position.y + yOffset;
        transform.position = p;
    }
}
