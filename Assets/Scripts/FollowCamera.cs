#define ARCADE_MODE

using System.Linq;
using UnityEngine;

class FollowCamera : MonoBehaviour
{
    public GameObject[] Players;

    Vector3 CurrentCenter;

    static bool Muted;

    void Start()
    {
        Vector3 center = Players.Aggregate(Vector3.zero, (a, b) => a + b.transform.position) / Players.Length;
        CurrentCenter = center / 20 + new Vector3(0, -3, 0);
        transform.LookAt(CurrentCenter);

        if (Muted && audio != null)
            audio.volume = 0;
    }

    void Update()
    {
        Vector3 center = Players.Aggregate(Vector3.zero, (a, b) => a + b.transform.position) / Players.Length;

        if (CurrentCenter.sqrMagnitude == 0)
            CurrentCenter = center;
        else
            CurrentCenter = Vector3.Lerp(CurrentCenter, VectorEx.Modulate(center, new Vector3(1 / 5f, 1 / 20f, 1 / 5f)) + new Vector3(0, -3, 0), 0.025f);

        Quaternion curRotation = transform.rotation;
        transform.LookAt(CurrentCenter);
        transform.rotation = Quaternion.Slerp(curRotation, transform.rotation, 0.025f);

#if !ARCADE_MODE
        if (audio != null && Input.GetKeyDown(KeyCode.M))
        {
            Muted = !Muted;
            audio.volume = !Muted ? 1 : 0;
        }
#endif
    }
}
