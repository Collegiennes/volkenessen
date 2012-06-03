using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class ShadowAlign : MonoBehaviour
{
    public GameObject Player;

    Vector3 initScale;
    float initHeight;

    void Start()
    {
        initScale = transform.localScale;
        initHeight = Player.transform.position.y;
    }

    void Update()
    {
        transform.position = new Vector3(Player.transform.position.x, -6.53f, Player.transform.position.z);
        transform.localScale = initScale * Mathf.Lerp((Player.transform.position.y / initHeight), 1, 0.85f);
    }
}
