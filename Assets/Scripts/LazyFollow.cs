using UnityEngine;

class LazyFollow : MonoBehaviour
{
    Quaternion TargetRotation;
    Quaternion ActualRotation;
    Quaternion LocalRotation;

    void Start()
    {
        LocalRotation = transform.localRotation;
        ActualRotation = TargetRotation = transform.rotation;
    }

    void FixedUpdate()
    {
        TargetRotation = transform.parent.rotation * LocalRotation;
        ActualRotation = Quaternion.Slerp(ActualRotation, TargetRotation, 0.225f);
        transform.rotation = ActualRotation;
    }
}
