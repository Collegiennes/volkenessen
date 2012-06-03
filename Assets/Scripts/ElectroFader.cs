using UnityEngine;

class ElectroFader : MonoBehaviour
{
    float startZ;

    void Start()
    {
        startZ = transform.rigidbody.worldCenterOfMass.z;
    }

    void Update()
    {
        var lp = (transform.rigidbody.worldCenterOfMass.z - startZ);
        var factor = (-6.5f) - lp;
        renderer.material.color = new Color(1, 1, 1, Mathf.Lerp(0.5f, 0.4f, Mathf.Clamp01(factor)));
    }
}
