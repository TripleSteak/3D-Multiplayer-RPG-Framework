using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingEffect : MonoBehaviour
{
    public GameObject MainCamera; // camera with which the object should be aligned

    void Update()
    {
        gameObject.transform.rotation = Quaternion.Euler(gameObject.transform.rotation.eulerAngles.x, MainCamera.transform.rotation.eulerAngles.y, gameObject.transform.rotation.eulerAngles.z); // update game object facing direction
    }
}
