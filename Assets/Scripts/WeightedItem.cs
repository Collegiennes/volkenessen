using UnityEngine;

class WeightedItem : MonoBehaviour
{
    public float Mass = 1;

    public AudioClip GroundSound;

    void OnCollisionEnter(Collision collision)
    {
        var mag = Easing.EaseIn(Mathf.Clamp01(collision.impactForceSum.magnitude / 75), EasingType.Quadratic);
        audio.pitch = Mathf.Clamp(1 / rigidbody.mass, 0.25f, 2);
        audio.PlayOneShot(GroundSound, mag * Easing.EaseOut(Mathf.Clamp01(rigidbody.mass / 10), EasingType.Quadratic));
    }
}
