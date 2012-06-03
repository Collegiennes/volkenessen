using UnityEngine;

class OpenDoors : MonoBehaviour
{
    void Start()
    {
        var OuterLeft = GameObject.Find("OuterLeft");
        var OuterRight = GameObject.Find("OuterRight");
        var InnerLeft = GameObject.Find("InnerLeft");
        var InnerRight = GameObject.Find("InnerRight");
        var Logo = GameObject.Find("Logo");

        const float SlideDuration = 0.5f;

        Logo.renderer.material.color = new Color(1, 1, 1, 1f);

        Wait.Until(e => e > 1, () =>
                                   {
                                       Wait.Until(elapsed =>
                                       {
                                           var step = Easing.EaseIn(Mathf.Clamp01(elapsed / SlideDuration), EasingType.Quadratic);
                                           InnerLeft.transform.localPosition = Vector3.Lerp(new Vector3(0, 0, 0.125f), new Vector3(6.17f, 0, 0.125f), 1 - step);
                                           InnerRight.transform.localPosition = Vector3.Lerp(new Vector3(18.5f, 0, 0.125f), new Vector3(12.34f, 0, 0.125f), 1 - step);
                                           Logo.renderer.material.color = new Color(1, 1, 1, 1 - step);
                                           return step >= 1;
                                       }, () =>
            Wait.Until(elapsed =>
            {
                var step = Easing.EaseIn(Mathf.Clamp01(elapsed / SlideDuration), EasingType.Quadratic);
                InnerLeft.transform.localPosition = OuterLeft.transform.localPosition =
                    Vector3.Lerp(new Vector3(-6, 0, 0), new Vector3(0, 0, 0), 1 - step);
                OuterRight.transform.localPosition = InnerRight.transform.localPosition =
                    Vector3.Lerp(new Vector3(25f, 0, 0), new Vector3(18.5f, 0, 0), 1 - step);
                return step >= 1;
            })
        );
                                   });
    }
}
